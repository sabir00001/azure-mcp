// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using Azure.Mcp.Core.Services.Azure;
using Azure.Mcp.Tools.VmDiagnostics.Models;
using Azure.ResourceManager;
using Azure.ResourceManager.Compute;
using Azure.ResourceManager.Compute.Models;
using Azure.ResourceManager.Resources;

namespace Azure.Mcp.Tools.VmDiagnostics.Services;

public class VmDiagnosticsService(IAzureService azureService)
    : BaseAzureResourceService(azureService), IVmDiagnosticsService
{
    private static readonly string[] WindowsLogTypes = ["System", "Application"];
    private static readonly string[] LinuxLogTypes = ["syslog"];
    private static readonly HashSet<string> WindowsServiceAllowlist = new(StringComparer.OrdinalIgnoreCase)
    {
        "HealthService", "AzureMonitorAgent", "W32Time", "Spooler", "W3SVC", "WAS"
    };
    private static readonly HashSet<string> LinuxServiceAllowlist = new(StringComparer.Ordinal)
    {
        "rsyslog", "chronyd", "azuremonitoragent", "nginx", "php-fpm"
    };

    private const int EventLookbackHours = 24;

    private const string TopProcessesWindowsScript = @"
$ErrorActionPreference = 'Stop'
$top = [int]{0}
$interval = 2
$cores = (Get-CimInstance Win32_ComputerSystem).NumberOfLogicalProcessors
if (-not $cores -or $cores -lt 1) {{ $cores = 1 }}

$first = @{{}}
foreach ($p in Get-Process) {{
    if ($null -ne $p.CPU) {{ $first[$p.Id] = $p.CPU }}
}}

Start-Sleep -Seconds $interval

$rows = New-Object System.Collections.ArrayList
foreach ($p in Get-Process) {{
    if ($null -eq $p.CPU) {{ continue }}
    if (-not $first.ContainsKey($p.Id)) {{ continue }}
    $delta = $p.CPU - $first[$p.Id]
    if ($delta -lt 0) {{ $delta = 0 }}
    $pct = [math]::Round((($delta / ($interval * $cores)) * 100), 2)
    $null = $rows.Add([pscustomobject]@{{
        processId       = [int]$p.Id
        processName     = [string]$p.ProcessName
        cpuPercent      = [double]$pct
        workingSetBytes = [int64]$p.WorkingSet64
    }})
}}

$out = @($rows | Sort-Object -Property cpuPercent, workingSetBytes -Descending |
                 Select-Object -First $top)
ConvertTo-Json -InputObject $out -Depth 3 -Compress
";

    private const string TopProcessesLinuxScript = @"
set -e
TOP={0}
ps -eo pid=,comm=,pcpu=,rss= --sort=-pcpu 2>/dev/null | head -n ""$TOP"" | awk '
BEGIN {{ printf ""["" }}
{{
    gsub(/""/, """", $2);
    printf ""%s{{""processId"":%d,""processName"":""%s"",""cpuPercent"":%.2f,""workingSetBytes"":%d}}"", (NR>1 ? "","" : """"), $1, $2, $3, $4*1024
}}
END {{ printf ""]"" }}'
";

    private const string EventLogsWindowsScript = @"
$ErrorActionPreference = 'Stop'
$log = '{0}'
$max = [int]{1}
$since = (Get-Date).AddHours(-{2})
$levelNames = @{{ 1 = 'Critical'; 2 = 'Error'; 3 = 'Warning'; 4 = 'Information'; 5 = 'Verbose' }}
$events = @()
try {{
    $events = @(Get-WinEvent -FilterHashtable @{{ LogName = $log; Level = 1,2,3; StartTime = $since }} -MaxEvents $max -ErrorAction Stop)
}} catch {{
    $events = @()
}}
$rows = New-Object System.Collections.ArrayList
foreach ($e in $events) {{
    $level = $e.LevelDisplayName
    if ([string]::IsNullOrWhiteSpace($level)) {{ $level = $levelNames[[int]$e.Level] }}
    if ([string]::IsNullOrWhiteSpace($level)) {{ $level = 'Unknown' }}
    $msg = $e.Message
    if ($null -eq $msg) {{ $msg = '' }}
    $msg = ($msg -replace '\s+', ' ').Trim()
    if ($msg.Length -gt 1000) {{ $msg = $msg.Substring(0, 1000) + '... [truncated]' }}
    $null = $rows.Add([pscustomobject]@{{
        logName = [string]$e.LogName
        level = [string]$level
        timeCreated = $e.TimeCreated.ToUniversalTime().ToString('o')
        source = [string]$e.ProviderName
        eventId = [int]$e.Id
        message = $msg
    }})
}}
ConvertTo-Json -InputObject @($rows) -Depth 3 -Compress
";

    private const string EventLogsLinuxScript = @"
MAX={0}
HOURS={1}
if command -v journalctl >/dev/null 2>&1; then
    journalctl -p 3 -n ""$MAX"" --since ""$HOURS hours ago"" --no-pager -o json 2>/dev/null || true
elif [ -r /var/log/syslog ]; then
    tail -n ""$MAX"" /var/log/syslog
elif [ -r /var/log/messages ]; then
    tail -n ""$MAX"" /var/log/messages
fi
";

    private const string ServiceStatusWindowsScript = @"
$ErrorActionPreference = 'Stop'
$service = Get-Service -Name '{0}' -ErrorAction Stop
$dependent = @($service.DependentServices | Select-Object -ExpandProperty Name)
@{{name=$service.Name;status=$service.Status.ToString();startType=$service.StartType.ToString();dependentServices=$dependent}} | ConvertTo-Json -Depth 3 -Compress
";

    private const string ServiceStatusLinuxScript = @"
set -e
NAME='{0}'
ACTIVE=$(systemctl show ""$NAME"" --property=ActiveState --value)
START=$(systemctl show ""$NAME"" --property=UnitFileState --value)
SUB=$(systemctl show ""$NAME"" --property=SubState --value)
printf '{""name"":""%s"",""status"":""%s"",""startType"":""%s"",""subState"":""%s"",""dependentServices"":[]}' ""$NAME"" ""$ACTIVE"" ""$START"" ""$SUB""
";

    public async Task<VmHealthSummaryResult> GetVmHealthAsync(
        string vmName,
        string resourceGroup,
        string subscription,
        string? tenant = null,
        CancellationToken cancellationToken = default)
    {
        ValidateRequiredParameters(
            (nameof(vmName), vmName),
            (nameof(resourceGroup), resourceGroup),
            (nameof(subscription), subscription));

        var subResource = await AzureService.GetSubscription(subscription, tenant, cancellationToken: cancellationToken);
        var rg = await subResource.GetResourceGroupAsync(resourceGroup, cancellationToken);
        var vmResponse = await rg.Value.GetVirtualMachineAsync(vmName, cancellationToken: cancellationToken);
        var vm = vmResponse.Value;
        var instanceViewResponse = await vm.InstanceViewAsync(cancellationToken);
        var instanceView = instanceViewResponse.Value;

        var powerStatus = instanceView.Statuses?
            .FirstOrDefault(status => status.Code?.StartsWith("PowerState/", StringComparison.OrdinalIgnoreCase) == true);
        var powerState = powerStatus?.DisplayStatus ?? powerStatus?.Code ?? "Unknown";
        var provisioningStatus = instanceView.Statuses?
            .FirstOrDefault(status => status.Code?.StartsWith("ProvisioningState/", StringComparison.OrdinalIgnoreCase) == true);
        var provisioningState = provisioningStatus?.DisplayStatus
            ?? provisioningStatus?.Code
            ?? vm.Data.ProvisioningState
            ?? "Unknown";
        var agentVersion = instanceView.VmAgent?.VmAgentVersion;
        var agentStatus = instanceView.VmAgent?.Statuses?.FirstOrDefault()?.DisplayStatus;

        List<ExtensionHealthSummary> extensions = [];
        if (instanceView.Extensions != null)
        {
            foreach (var ext in instanceView.Extensions)
            {
                var extStatus = ext.Statuses?.FirstOrDefault();
                extensions.Add(new ExtensionHealthSummary(
                    ext.Name,
                    null,
                    ext.VirtualMachineExtensionInstanceViewType,
                    extStatus?.DisplayStatus ?? extStatus?.Code,
                    extStatus?.Message));
            }
        }

        return new VmHealthSummaryResult(
            vmName,
            resourceGroup,
            vm.Data.Location.Name,
            powerState,
            provisioningState,
            agentVersion,
            agentStatus,
            extensions);
    }

    public Task<VmMetricsReviewResult> ReviewVmMetricsAsync(
        string vmName,
        string resourceGroup,
        string subscription,
        string timespan = "PT1H",
        string? tenant = null,
        CancellationToken cancellationToken = default)
    {
        ValidateRequiredParameters(
            (nameof(vmName), vmName),
            (nameof(resourceGroup), resourceGroup),
            (nameof(subscription), subscription));

        return Task.FromException<VmMetricsReviewResult>(
            new NotImplementedException("VM metrics collection is not implemented. Use the monitor namespace: monitor_metrics_query, monitor_metrics_batchquery, or monitor_metrics_definitions."));
    }

    private static async Task<string> RunGuestScriptAsync(
        VirtualMachineResource vm,
        bool isWindows,
        string script,
        CancellationToken cancellationToken)
    {
        var input = new RunCommandInput(isWindows ? "RunPowerShellScript" : "RunShellScript");
        input.Script.Add(script);

        var operation = await vm.RunCommandAsync(WaitUntil.Completed, input, cancellationToken);
        var message = operation.Value?.Value?.FirstOrDefault()?.Message ?? string.Empty;
        var stdoutIndex = message.IndexOf("[stdout]", StringComparison.Ordinal);
        var stderrIndex = message.IndexOf("[stderr]", StringComparison.Ordinal);

        var stdout = message;
        if (stdoutIndex >= 0)
        {
            var start = stdoutIndex + "[stdout]".Length;
            stdout = stderrIndex > stdoutIndex
                ? message[start..stderrIndex]
                : message[start..];
        }

        var stderr = stderrIndex >= 0
            ? message[(stderrIndex + "[stderr]".Length)..].Trim()
            : string.Empty;

        if (!string.IsNullOrWhiteSpace(stderr))
        {
            throw new InvalidOperationException($"Guest script reported errors: {stderr}");
        }

        return stdout.Trim();
    }

    public async Task<VmProcessListResult> ListTopProcessesAsync(
        string vmName,
        string resourceGroup,
        string subscription,
        int top = 10,
        string? tenant = null,
        CancellationToken cancellationToken = default)
    {
        ValidateRequiredParameters(
            (nameof(vmName), vmName),
            (nameof(resourceGroup), resourceGroup),
            (nameof(subscription), subscription));

        top = Math.Clamp(top <= 0 ? 10 : top, 1, 50);

        var subResource = await AzureService.GetSubscription(subscription, tenant, cancellationToken: cancellationToken);
        var rg = await subResource.GetResourceGroupAsync(resourceGroup, cancellationToken);
        var vmResponse = await rg.Value.GetVirtualMachineAsync(vmName, cancellationToken: cancellationToken);
        var vm = vmResponse.Value;
        var instanceView = (await vm.InstanceViewAsync(cancellationToken)).Value;

        var powerState = instanceView.Statuses?
            .FirstOrDefault(status => status.Code?.StartsWith("PowerState/", StringComparison.OrdinalIgnoreCase) == true)
            ?.DisplayStatus;
        if (!string.Equals(powerState, "VM running", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"VM '{vmName}' is not running (power state: {powerState ?? "Unknown"}). Run Command cannot execute.");
        }

        var agentStatus = instanceView.VmAgent?.Statuses?.FirstOrDefault()?.DisplayStatus;
        if (!string.Equals(agentStatus, "Ready", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"VM '{vmName}' guest agent is not Ready (status: {agentStatus ?? "Unknown"}). Run Command cannot execute.");
        }

        var isWindows = vm.Data.StorageProfile?.OSDisk?.OSType == SupportedOperatingSystemType.Windows;
        var script = string.Format(isWindows ? TopProcessesWindowsScript : TopProcessesLinuxScript, top);
        var raw = await RunGuestScriptAsync(vm, isWindows, script, cancellationToken);

        List<ProcessInfo> processes = [];
        var executionStatus = "Succeeded";
        var summary = $"Retrieved top {top} processes by CPU via VM Run Command ({(isWindows ? "Windows" : "Linux")}).";

        try
        {
            var trimmed = raw.TrimStart();
            if (trimmed.StartsWith('['))
            {
                processes = JsonSerializer.Deserialize(trimmed, VmDiagnosticsJsonContext.Default.ListProcessInfo) ?? [];
            }
            else if (trimmed.StartsWith('{'))
            {
                var single = JsonSerializer.Deserialize(trimmed, VmDiagnosticsJsonContext.Default.ProcessInfo);
                if (single is not null)
                {
                    processes = [single];
                }
            }
            else
            {
                executionStatus = "PartialSuccess";
                summary = $"Run Command succeeded but returned no parsable JSON. Raw output: {Truncate(raw, 2000)}";
            }
        }
        catch (JsonException ex)
        {
            executionStatus = "PartialSuccess";
            summary = $"Run Command succeeded but the output could not be parsed ({ex.Message}). Raw output: {Truncate(raw, 2000)}";
        }

        return new VmProcessListResult(vmName, resourceGroup, executionStatus, processes, summary);

        static string Truncate(string value, int max) =>
            value.Length <= max ? value : value[..max] + "... [truncated]";
    }

    public async Task<VmEventLogsQueryResult> QueryEventLogsAsync(
        string vmName,
        string resourceGroup,
        string subscription,
        string logType = "System",
        int maxEntries = 50,
        string? tenant = null,
        CancellationToken cancellationToken = default)
    {
        ValidateRequiredParameters(
            (nameof(vmName), vmName),
            (nameof(resourceGroup), resourceGroup),
            (nameof(subscription), subscription));

        maxEntries = Math.Clamp(maxEntries <= 0 ? 50 : maxEntries, 1, 200);

        var subResource = await AzureService.GetSubscription(subscription, tenant, cancellationToken: cancellationToken);
        var rg = await subResource.GetResourceGroupAsync(resourceGroup, cancellationToken);
        var vmResponse = await rg.Value.GetVirtualMachineAsync(vmName, cancellationToken: cancellationToken);
        var vm = vmResponse.Value;
        var instanceView = (await vm.InstanceViewAsync(cancellationToken)).Value;

        var powerState = instanceView.Statuses?
            .FirstOrDefault(status => status.Code?.StartsWith("PowerState/", StringComparison.OrdinalIgnoreCase) == true)
            ?.DisplayStatus;
        if (!string.Equals(powerState, "VM running", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"VM '{vmName}' is not running (power state: {powerState ?? "Unknown"}). Run Command cannot execute.");
        }

        var agentStatus = instanceView.VmAgent?.Statuses?.FirstOrDefault()?.DisplayStatus;
        if (!string.Equals(agentStatus, "Ready", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"VM '{vmName}' guest agent is not Ready (status: {agentStatus ?? "Unknown"}). Run Command cannot execute.");
        }

        var isWindows = vm.Data.StorageProfile?.OSDisk?.OSType == SupportedOperatingSystemType.Windows;
        var allowedLogTypes = isWindows ? WindowsLogTypes : LinuxLogTypes;
        var resolvedLogType = allowedLogTypes.FirstOrDefault(
            value => string.Equals(value, logType, StringComparison.OrdinalIgnoreCase));
        if (resolvedLogType is null)
        {
            throw new ArgumentException(
                $"Log type '{logType}' is not permitted on this {(isWindows ? "Windows" : "Linux")} VM. Allowed values: {string.Join(", ", allowedLogTypes)}.",
                nameof(logType));
        }

        var script = isWindows
            ? string.Format(EventLogsWindowsScript, resolvedLogType, maxEntries, EventLookbackHours)
            : string.Format(EventLogsLinuxScript, maxEntries, EventLookbackHours);
        var raw = await RunGuestScriptAsync(vm, isWindows, script, cancellationToken);
        var entries = isWindows
            ? ParseWindowsEvents(raw, resolvedLogType)
            : ParseLinuxEvents(raw, resolvedLogType);

        return new VmEventLogsQueryResult(vmName, resourceGroup, resolvedLogType, entries);
    }

    public async Task<VmServiceStatusResult> GetServiceStatusAsync(
        string vmName,
        string resourceGroup,
        string service,
        string subscription,
        string? tenant = null,
        CancellationToken cancellationToken = default)
    {
        ValidateRequiredParameters(
            (nameof(vmName), vmName),
            (nameof(resourceGroup), resourceGroup),
            (nameof(service), service),
            (nameof(subscription), subscription));

        var subResource = await AzureService.GetSubscription(subscription, tenant, cancellationToken: cancellationToken);
        var rg = await subResource.GetResourceGroupAsync(resourceGroup, cancellationToken);
        var vmResponse = await rg.Value.GetVirtualMachineAsync(vmName, cancellationToken: cancellationToken);
        var vm = vmResponse.Value;
        var instanceView = (await vm.InstanceViewAsync(cancellationToken)).Value;
        var powerState = instanceView.Statuses?
            .FirstOrDefault(status => status.Code?.StartsWith("PowerState/", StringComparison.OrdinalIgnoreCase) == true)
            ?.DisplayStatus;
        if (!string.Equals(powerState, "VM running", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"VM '{vmName}' is not running (power state: {powerState ?? "Unknown"}). Run Command cannot execute.");
        }

        var agentStatus = instanceView.VmAgent?.Statuses?.FirstOrDefault()?.DisplayStatus;
        if (!string.Equals(agentStatus, "Ready", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"VM '{vmName}' guest agent is not Ready (status: {agentStatus ?? "Unknown"}). Run Command cannot execute.");
        }

        var isWindows = vm.Data.StorageProfile?.OSDisk?.OSType == SupportedOperatingSystemType.Windows;
        var allowlist = isWindows ? WindowsServiceAllowlist : LinuxServiceAllowlist;
        var resolvedService = allowlist.FirstOrDefault(item => string.Equals(item, service, StringComparison.OrdinalIgnoreCase));
        if (resolvedService is null)
        {
            throw new ArgumentException(
                $"Service '{service}' is not on the {(isWindows ? "Windows" : "Linux")} diagnostics allowlist.",
                nameof(service));
        }

        var script = string.Format(isWindows ? ServiceStatusWindowsScript : ServiceStatusLinuxScript, resolvedService);
        var raw = await RunGuestScriptAsync(vm, isWindows, script, cancellationToken);

        try
        {
            using var document = JsonDocument.Parse(raw);
            var root = document.RootElement;
            var dependentServices = root.TryGetProperty("dependentServices", out var dependencies)
                && dependencies.ValueKind == JsonValueKind.Array
                ? dependencies.EnumerateArray()
                    .Where(item => item.ValueKind == JsonValueKind.String)
                    .Select(item => item.GetString()!)
                    .ToList()
                : [];
            return new VmServiceStatusResult(
                vmName,
                resourceGroup,
                GetJsonString(root, "name") ?? resolvedService,
                GetJsonString(root, "status") ?? "Unknown",
                GetJsonString(root, "startType") ?? "Unknown",
                dependentServices,
                isWindows ? "Windows" : "Linux",
                raw);
        }
        catch (JsonException ex)
        {
            return new VmServiceStatusResult(
                vmName,
                resourceGroup,
                resolvedService,
                "Unknown",
                "Unknown",
                [],
                isWindows ? "Windows" : "Linux",
                $"{raw}\nParser error: {ex.Message}");
        }
    }

    private static string? GetJsonString(JsonElement element, string propertyName)
        => element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static List<EventLogEntry> ParseWindowsEvents(string raw, string logType)
    {
        var entries = new List<EventLogEntry>();
        var trimmed = raw.Trim();
        if (trimmed.Length == 0)
        {
            return entries;
        }

        try
        {
            using var document = JsonDocument.Parse(trimmed);
            var items = document.RootElement.ValueKind == JsonValueKind.Array
                ? document.RootElement.EnumerateArray().ToList()
                : [document.RootElement];
            foreach (var item in items)
            {
                if (item.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                entries.Add(new EventLogEntry(
                    GetString(item, "logName") ?? logType,
                    GetString(item, "level") ?? "Unknown",
                    ParseTime(GetString(item, "timeCreated")),
                    GetString(item, "source") ?? "Unknown",
                    item.TryGetProperty("eventId", out var eventId) && eventId.TryGetInt32(out var id) ? id : 0,
                    GetString(item, "message") ?? string.Empty));
            }
        }
        catch (JsonException ex)
        {
            entries.Add(new EventLogEntry(
                logType,
                "Unknown",
                DateTimeOffset.UtcNow,
                "azmcp-parser",
                0,
                $"Run Command succeeded but the output could not be parsed ({ex.Message}). Raw output: {Truncate(raw, 2000)}"));
        }

        return entries;
    }

    private static List<EventLogEntry> ParseLinuxEvents(string raw, string logType)
    {
        var entries = new List<EventLogEntry>();
        foreach (var line in raw.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (line.StartsWith('{'))
            {
                try
                {
                    using var document = JsonDocument.Parse(line);
                    var root = document.RootElement;
                    var time = DateTimeOffset.UtcNow;
                    if (long.TryParse(GetString(root, "__REALTIME_TIMESTAMP"), out var micros))
                    {
                        time = DateTimeOffset.FromUnixTimeMilliseconds(micros / 1000);
                    }

                    entries.Add(new EventLogEntry(
                        logType,
                        GetLinuxLevel(GetString(root, "PRIORITY")),
                        time,
                        GetString(root, "SYSLOG_IDENTIFIER") ?? GetString(root, "_COMM") ?? "Unknown",
                        0,
                        Truncate(GetString(root, "MESSAGE") ?? string.Empty, 1000)));
                    continue;
                }
                catch (JsonException)
                {
                }
            }

            entries.Add(new EventLogEntry(logType, "Unknown", DateTimeOffset.UtcNow, "syslog", 0, Truncate(line, 1000)));
        }

        return entries;
    }

    private static string GetLinuxLevel(string? priority) => priority switch
    {
        "0" => "Emergency",
        "1" => "Alert",
        "2" => "Critical",
        "3" => "Error",
        "4" => "Warning",
        "5" => "Notice",
        "6" => "Information",
        "7" => "Debug",
        _ => "Unknown"
    };

    private static string? GetString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static DateTimeOffset ParseTime(string? value) =>
        DateTimeOffset.TryParse(value, out var parsed) ? parsed : DateTimeOffset.UtcNow;

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max] + "... [truncated]";
}
