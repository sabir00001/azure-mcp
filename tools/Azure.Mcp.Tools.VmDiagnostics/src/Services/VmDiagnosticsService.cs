// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Mcp.Core.Services.Azure;
using Azure.Mcp.Tools.VmDiagnostics.Models;
using Azure.ResourceManager;
using Azure.ResourceManager.Compute;
using Azure.ResourceManager.Compute.Models;

namespace Azure.Mcp.Tools.VmDiagnostics.Services;

public class VmDiagnosticsService(IAzureService azureService)
    : BaseAzureResourceService(azureService), IVmDiagnosticsService
{
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
            new NotImplementedException("VM metrics collection is not implemented yet. Wire this method to Azure Monitor metrics / VM guest diagnostics before enabling it."));
    }

    public Task<VmProcessListResult> ListTopProcessesAsync(
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

        return Task.FromException<VmProcessListResult>(
            new NotImplementedException("VM process enumeration is not implemented yet. Use Azure VM Run Command or guest extension data collection only when the VM meets the required permissions and prerequisites."));
    }

    public Task<VmEventLogsQueryResult> QueryEventLogsAsync(
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

        return Task.FromException<VmEventLogsQueryResult>(
            new NotImplementedException("VM event log querying is not implemented yet. This tool must call the guest OS diagnostics endpoint or VM Run Command with explicit validation before returning telemetry."));
    }
}
