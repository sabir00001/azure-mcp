// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using Azure;
using Azure.Mcp.Core.Services.Azure;
using Azure.Mcp.Tools.VmRemediation.Models;
using Azure.ResourceManager.Compute;
using Azure.ResourceManager.Compute.Models;
using Azure.ResourceManager.Resources;

namespace Azure.Mcp.Tools.VmRemediation.Services;

public sealed class VmRemediationService(
    IAzureService azureService,
    IRemediationApprovalProvider approvalProvider)
    : BaseAzureResourceService(azureService), IVmRemediationService
{
    private const string RunCommandOperation = "Microsoft.Compute/virtualMachines/runCommand/action";
    private const string RestartOperation = "Microsoft.Compute/virtualMachines/restart/action";
    private const int MaxIncidentAttempts = 2;
    private const int MaxActionsPerDay = 3;

    private static readonly IReadOnlyDictionary<RemediationOs, IReadOnlySet<string>> s_allowlist =
        new Dictionary<RemediationOs, IReadOnlySet<string>>
        {
            [RemediationOs.Windows] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "HealthService", "AzureMonitorAgent", "W32Time", "Spooler", "W3SVC", "WAS"
            },
            [RemediationOs.Linux] = new HashSet<string>(StringComparer.Ordinal)
            {
                "rsyslog", "chronyd", "azuremonitoragent", "nginx", "php-fpm"
            }
        };

    private static readonly string[] s_permanentDenyPatterns =
    [
        "MSSQLSERVER", "MSSQL$", "postgresql", "mysqld", "mariadb", "sshd", "WinRM",
        "WindowsAzureGuestAgent", "RdAgent", "walinuxagent", "waagent", "CSFalconService",
        "QualysAgent", "McAfee", "mfemms", "docker", "containerd", "kubelet"
    ];

    public async Task<RemediationResult> RestartServiceAsync(
        string vm,
        string resourceGroup,
        string service,
        string incidentId,
        string subscription,
        string? tenant = null,
        CancellationToken cancellationToken = default)
    {
        ValidateRequiredParameters(
            (nameof(vm), vm),
            (nameof(resourceGroup), resourceGroup),
            (nameof(service), service),
            (nameof(incidentId), incidentId),
            (nameof(subscription), subscription));
        ValidateIncidentId(incidentId);

        var (vmResource, beforeVm) = await GetVmAndInstanceViewAsync(vm, resourceGroup, subscription, tenant, cancellationToken);
        RequireVmReady(beforeVm);
        var os = GetOperatingSystem(vmResource);
        ValidateService(service, os);
        var limits = await GetActivityCountsAsync(vmResource.Id, tenant, cancellationToken);
        if (limits.ActionsLastHour >= 1 || limits.ActionsLast24Hours >= MaxActionsPerDay)
        {
            throw new InvalidOperationException($"Remediation rate limit exceeded for VM '{vm}'. Actions in last hour: {limits.ActionsLastHour}; last 24 hours: {limits.ActionsLast24Hours}.");
        }

        var before = await ReadServiceStateAsync(vmResource, service, os, cancellationToken);
        var incidentAttempts = 0;
        if (!before.Exists)
        {
            throw new InvalidOperationException($"Service '{service}' was not found on VM '{vm}'.");
        }

        if (IsStopped(before.State))
        {
            await RunServiceActionAsync(vmResource, service, os, startStoppedService: true, cancellationToken);
        }
        else
        {
            var approval = await approvalProvider.GetApprovalAsync(
                incidentId,
                vmResource.Id.ToString(),
                $"restart service {service} on VM {vm}",
                tenant,
                cancellationToken);
            EnforceIncidentAttemptLimit(approval.IncidentAttempts);
            incidentAttempts = approval.IncidentAttempts;
            if (!approval.Approved)
            {
                return CreateApprovalRequiredResult(
                    incidentId,
                    vm,
                    service,
                    before,
                    limits,
                    approval,
                    $"restart service {service} on VM {vm}");
            }

            await RunServiceActionAsync(vmResource, service, os, startStoppedService: false, cancellationToken);
        }

        await Task.Delay(TimeSpan.FromSeconds(60), cancellationToken);
        var after = await ReadServiceStateAsync(vmResource, service, os, cancellationToken);
        var status = IsStopped(after.State) ? "CRASHLOOP_SUSPECTED" : "completed";

        return new RemediationResult(
            status,
            "service_restart",
            null,
            incidentId,
            vm,
            service,
            before,
            after,
            null,
            after.RawOutput,
            limits.ActionsLastHour,
            limits.ActionsLast24Hours,
            incidentAttempts + 1);
    }

    public async Task<RemediationResult> RestartVmAsync(
        string vm,
        string resourceGroup,
        string incidentId,
        string subscription,
        string? tenant = null,
        CancellationToken cancellationToken = default)
    {
        ValidateRequiredParameters(
            (nameof(vm), vm),
            (nameof(resourceGroup), resourceGroup),
            (nameof(incidentId), incidentId),
            (nameof(subscription), subscription));
        ValidateIncidentId(incidentId);

        var (vmResource, beforeVm) = await GetVmAndInstanceViewAsync(vm, resourceGroup, subscription, tenant, cancellationToken);
        RequireVmReady(beforeVm);
        var limits = await GetActivityCountsAsync(vmResource.Id, tenant, cancellationToken);
        if (limits.ActionsLastHour >= 1 || limits.ActionsLast24Hours >= MaxActionsPerDay)
        {
            throw new InvalidOperationException($"Remediation rate limit exceeded for VM '{vm}'. Actions in last hour: {limits.ActionsLastHour}; last 24 hours: {limits.ActionsLast24Hours}.");
        }

        var before = ToVmState(beforeVm);
        var approval = await approvalProvider.GetApprovalAsync(
            incidentId,
            vmResource.Id.ToString(),
            $"restart VM {vm}",
            tenant,
            cancellationToken);
        EnforceIncidentAttemptLimit(approval.IncidentAttempts);
        if (!approval.Approved)
        {
            return CreateApprovalRequiredResult(
                incidentId,
                vm,
                null,
                before,
                limits,
                approval,
                $"restart VM {vm}");
        }

        await vmResource.RestartAsync(WaitUntil.Completed, cancellationToken);
        await Task.Delay(TimeSpan.FromSeconds(60), cancellationToken);
        var afterVm = await GetInstanceViewAsync(vmResource, cancellationToken);
        var after = ToVmState(afterVm);
        var status = IsVmRunning(after.State) ? "completed" : "CRASHLOOP_SUSPECTED";

        return new RemediationResult(
            status,
            "vm_restart",
            null,
            incidentId,
            vm,
            null,
            before,
            after,
            null,
            null,
            limits.ActionsLastHour,
            limits.ActionsLast24Hours,
            1);
    }

    private async Task<(VirtualMachineResource Resource, VirtualMachineInstanceView InstanceView)> GetVmAndInstanceViewAsync(
        string vm,
        string resourceGroup,
        string subscription,
        string? tenant,
        CancellationToken cancellationToken)
    {
        var subscriptionResource = await AzureService.GetSubscription(subscription, tenant, cancellationToken: cancellationToken);
        var resourceGroupResponse = await subscriptionResource.GetResourceGroupAsync(resourceGroup, cancellationToken);
        var vmResponse = await resourceGroupResponse.Value.GetVirtualMachineAsync(vm, cancellationToken: cancellationToken);
        var instanceView = await GetInstanceViewAsync(vmResponse.Value, cancellationToken);
        return (vmResponse.Value, instanceView);
    }

    private static async Task<VirtualMachineInstanceView> GetInstanceViewAsync(
        VirtualMachineResource vmResource,
        CancellationToken cancellationToken)
    {
        var response = await vmResource.InstanceViewAsync(cancellationToken);
        return response.Value;
    }

    private async Task<RemediationState> ReadServiceStateAsync(
        VirtualMachineResource vmResource,
        string service,
        RemediationOs os,
        CancellationToken cancellationToken)
    {
        var output = await RunCommandAsync(vmResource, os, GetStateScript(os, service), cancellationToken);
        try
        {
            using var document = JsonDocument.Parse(output.Stdout);
            var root = document.RootElement;
            return new RemediationState(
                root.TryGetProperty("state", out var state) ? state.GetString() ?? "Unknown" : "Unknown",
                !root.TryGetProperty("exists", out var exists) || exists.GetBoolean(),
                output.RawOutput);
        }
        catch (JsonException)
        {
            return new RemediationState("Unknown", true, output.RawOutput);
        }
    }

    private async Task RunServiceActionAsync(
        VirtualMachineResource vmResource,
        string service,
        RemediationOs os,
        bool startStoppedService,
        CancellationToken cancellationToken)
    {
        await RunCommandAsync(vmResource, os, GetActionScript(os, service, startStoppedService), cancellationToken);
    }

    private async Task<RunCommandOutput> RunCommandAsync(
        VirtualMachineResource vmResource,
        RemediationOs os,
        string script,
        CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMinutes(2));
        var input = new RunCommandInput(os == RemediationOs.Windows ? "RunPowerShellScript" : "RunShellScript");
        input.Script.Add(script);

        try
        {
            var response = await vmResource.RunCommandAsync(WaitUntil.Completed, input, timeout.Token);
            var messages = response.Value.Value ?? [];
            var stdout = string.Join(Environment.NewLine, messages
                .Where(message => message.Message?.StartsWith("[stdout]", StringComparison.OrdinalIgnoreCase) == true)
                .Select(message => message.Message![8..]));
            var stderr = string.Join(Environment.NewLine, messages
                .Where(message => message.Message?.StartsWith("[stderr]", StringComparison.OrdinalIgnoreCase) == true)
                .Select(message => message.Message![8..]));
            var raw = string.Join(Environment.NewLine, messages.Select(message => message.Message ?? string.Empty));
            if (!string.IsNullOrWhiteSpace(stderr))
            {
                throw new InvalidOperationException($"VM Run Command returned stderr: {stderr}. Raw output: {raw}");
            }

            return new RunCommandOutput(stdout, stderr, raw);
        }
        catch (RequestFailedException ex) when (ex.Status == 409)
        {
            throw new InvalidOperationException("Another VM Run Command is already running for this VM. No retry was attempted.", ex);
        }
    }

    private async Task<ActivityCounts> GetActivityCountsAsync(
        Azure.Core.ResourceIdentifier vmResourceId,
        string? tenant,
        CancellationToken cancellationToken)
    {
        var serverClientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID");
        if (string.IsNullOrWhiteSpace(serverClientId))
        {
            throw new InvalidOperationException("AZURE_CLIENT_ID is required to enforce remediation Activity Log caller matching.");
        }

        var subscriptionId = vmResourceId.SubscriptionId
            ?? throw new InvalidOperationException("Unable to resolve the VM subscription ID for Activity Log rate limiting.");
        var token = await GetArmAccessTokenAsync(tenant, cancellationToken);
        var start = DateTimeOffset.UtcNow.AddHours(-24);
        var filter = $"eventTimestamp ge '{start:yyyy-MM-ddTHH:mm:ss.fffZ}' and resourceId eq '{vmResourceId}'";
        var uri = $"https://management.azure.com/subscriptions/{subscriptionId}/providers/microsoft.insights/eventtypes/management/values?api-version=2017-03-01-preview&$filter={Uri.EscapeDataString(filter)}";
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Authorization = new("Bearer", token.Token);
        using var response = await AzureService.GetClient().SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        var hour = DateTimeOffset.UtcNow.AddHours(-1);
        var dayCount = 0;
        var hourCount = 0;
        foreach (var item in document.RootElement.GetProperty("value").EnumerateArray())
        {
            var caller = item.TryGetProperty("caller", out var callerProperty) ? callerProperty.GetString() : null;
            if (!string.Equals(caller, serverClientId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var operation = item.GetProperty("operationName").GetProperty("value").GetString();
            if (!string.Equals(operation, RunCommandOperation, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(operation, RestartOperation, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            dayCount++;
            if (item.TryGetProperty("eventTimestamp", out var timestamp)
                && DateTimeOffset.TryParse(timestamp.GetString(), out var eventTime)
                && eventTime >= hour)
            {
                hourCount++;
            }
        }

        return new ActivityCounts(hourCount, dayCount);
    }

    private static RemediationOs GetOperatingSystem(VirtualMachineResource vmResource)
        => string.Equals(vmResource.Data.StorageProfile?.OSDisk?.OSType?.ToString(), "Windows", StringComparison.OrdinalIgnoreCase)
            ? RemediationOs.Windows
            : RemediationOs.Linux;

    private static void ValidateService(string service, RemediationOs os)
    {
        if (s_permanentDenyPatterns.Any(pattern => pattern.EndsWith('*')
                ? service.StartsWith(pattern[..^1], StringComparison.OrdinalIgnoreCase)
                : string.Equals(service, pattern, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException($"Service '{service}' is permanently excluded from remediation.");
        }

        if (!s_allowlist[os].Contains(service))
        {
            throw new ArgumentException($"Service '{service}' is not on the remediation allowlist.");
        }
    }

    private static void ValidateIncidentId(string incidentId)
    {
        if (string.IsNullOrWhiteSpace(incidentId))
        {
            throw new ArgumentException("incident-id is required for every remediation action.", nameof(incidentId));
        }
    }

    private static void RequireVmReady(VirtualMachineInstanceView instanceView)
    {
        var powerState = instanceView.Statuses?.FirstOrDefault(status => status.Code?.StartsWith("PowerState/", StringComparison.OrdinalIgnoreCase) == true)?.DisplayStatus ?? "Unknown";
        var agentStatus = instanceView.VmAgent?.Statuses?.FirstOrDefault()?.DisplayStatus ?? "Unknown";
        if (!IsVmRunning(powerState) || !string.Equals(agentStatus, "Ready", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"VM pre-check failed. Actual powerState: '{powerState}', guest agent status: '{agentStatus}'.");
        }
    }

    private static RemediationResult CreateApprovalRequiredResult(
        string incidentId,
        string vm,
        string? service,
        RemediationState before,
        ActivityCounts limits,
        RemediationApproval approval,
        string proposedAction)
        => new(
            "APPROVAL_REQUIRED",
            service is null ? "vm_restart" : "service_restart",
            approval.Reason,
            incidentId,
            vm,
            service,
            before,
            null,
            proposedAction,
            null,
            limits.ActionsLastHour,
            limits.ActionsLast24Hours,
            approval.IncidentAttempts);

    private static void EnforceIncidentAttemptLimit(int attempts)
    {
        if (attempts >= MaxIncidentAttempts)
        {
            throw new InvalidOperationException($"Incident remediation attempt limit reached: {attempts} attempts already recorded; the third attempt is refused.");
        }
    }

    private static bool IsStopped(string state) => string.Equals(state, "Stopped", StringComparison.OrdinalIgnoreCase) || string.Equals(state, "Inactive", StringComparison.OrdinalIgnoreCase);
    private static bool IsVmRunning(string state) => string.Equals(state, "VM running", StringComparison.OrdinalIgnoreCase);
    private static RemediationState ToVmState(VirtualMachineInstanceView view)
    {
        var status = view.Statuses?.FirstOrDefault(item => item.Code?.StartsWith("PowerState/", StringComparison.OrdinalIgnoreCase) == true);
        return new RemediationState(status?.DisplayStatus ?? status?.Code ?? "Unknown", true, string.Empty);
    }

    private static string GetStateScript(RemediationOs os, string service) => os == RemediationOs.Windows
        ? $"$s = Get-Service -Name '{service}' -ErrorAction SilentlyContinue; if ($null -eq $s) {{ @{{exists=$false;state='NotFound';name='{service}'}} | ConvertTo-Json -Compress }} else {{ @{{exists=$true;state=$s.Status.ToString();name=$s.Name}} | ConvertTo-Json -Compress }}"
        : $"$name='{service}'; $active = systemctl is-active --quiet -- $name; $state = if ($active) {{ 'Running' }} else {{ 'Stopped' }}; Write-Output (@{{exists=$true;state=$state;name=$name}} | ConvertTo-Json -Compress)";

    private static string GetActionScript(RemediationOs os, string service, bool startStoppedService) => os == RemediationOs.Windows
        ? startStoppedService
            ? $"Start-Service -Name '{service}' -ErrorAction Stop; @{{action='start';name='{service}'}} | ConvertTo-Json -Compress"
            : $"Restart-Service -Name '{service}' -Force -ErrorAction Stop; @{{action='restart';name='{service}'}} | ConvertTo-Json -Compress"
        : startStoppedService
            ? $"systemctl start -- '{service}'; Write-Output (@{{action='start';name='{service}'}} | ConvertTo-Json -Compress)"
            : $"systemctl restart -- '{service}'; Write-Output (@{{action='restart';name='{service}'}} | ConvertTo-Json -Compress)";

    private sealed record RunCommandOutput(string Stdout, string Stderr, string RawOutput);
    private sealed record ActivityCounts(int ActionsLastHour, int ActionsLast24Hours);
}

