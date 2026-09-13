// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Core;
using Azure.Mcp.Core.Services.Azure;
using Azure.Mcp.Tools.NetworkDiagnostics.Models;
using Azure.ResourceManager;
using Azure.ResourceManager.Network;
using Azure.ResourceManager.Network.Models;

namespace Azure.Mcp.Tools.NetworkDiagnostics.Services;

public class NetworkDiagnosticsService(IAzureService azureService)
    : BaseAzureResourceService(azureService), INetworkDiagnosticsService
{
    public async Task<NetworkTopologySummaryResult> GetTopologySummaryAsync(
        string subscription,
        string? resourceGroup = null,
        string? tenant = null,
        CancellationToken cancellationToken = default)
    {
        ValidateRequiredParameters((nameof(subscription), subscription));

        var subResource = await AzureService.GetSubscription(subscription, tenant, cancellationToken: cancellationToken);

        List<VirtualNetworkTopology> topologies = [];

        if (!string.IsNullOrEmpty(resourceGroup))
        {
            var rg = await subResource.GetResourceGroupAsync(resourceGroup, cancellationToken);
            await foreach (var vnet in rg.Value.GetVirtualNetworks().GetAllAsync(cancellationToken: cancellationToken))
            {
                topologies.Add(MapTopology(vnet.Data));
            }
        }
        else
        {
            await foreach (var vnet in subResource.GetVirtualNetworksAsync(cancellationToken: cancellationToken))
            {
                topologies.Add(MapTopology(vnet.Data));
            }
        }

        return new NetworkTopologySummaryResult(subscription, topologies);
    }

    public Task<NsgValidationResult> ValidateNsgRulesAsync(
        string nsgName,
        string resourceGroup,
        string subscription,
        string direction,
        string protocol,
        string sourceAddress,
        int destinationPort,
        string? tenant = null,
        CancellationToken cancellationToken = default)
    {
        ValidateRequiredParameters(
            (nameof(nsgName), nsgName),
            (nameof(resourceGroup), resourceGroup),
            (nameof(subscription), subscription),
            (nameof(direction), direction),
            (nameof(protocol), protocol),
            (nameof(sourceAddress), sourceAddress));

        return Task.FromException<NsgValidationResult>(
            new NotImplementedException("NSG flow validation is not implemented yet. The evaluator must account for source and destination prefixes, ports, protocols, directions, service tags, application security groups, and default rules before enabling it."));
    }

    public Task<PrivateDnsValidationResult> ValidatePrivateDnsAsync(
        string subscription,
        string? resourceGroup = null,
        string? tenant = null,
        CancellationToken cancellationToken = default)
    {
        ValidateRequiredParameters((nameof(subscription), subscription));

        return Task.FromException<PrivateDnsValidationResult>(
            new NotImplementedException("Private DNS validation is not implemented yet. The service must query virtual network links and record sets before returning validation results."));
    }

    public async Task<PeeringCheckResult> CheckPeeringStatusAsync(
        string vnetName,
        string resourceGroup,
        string subscription,
        string? tenant = null,
        CancellationToken cancellationToken = default)
    {
        ValidateRequiredParameters(
            (nameof(vnetName), vnetName),
            (nameof(resourceGroup), resourceGroup),
            (nameof(subscription), subscription));

        var subResource = await AzureService.GetSubscription(subscription, tenant, cancellationToken: cancellationToken);
        var rg = await subResource.GetResourceGroupAsync(resourceGroup, cancellationToken);
        var vnetResponse = await rg.Value.GetVirtualNetworkAsync(vnetName, cancellationToken: cancellationToken);
        var vnet = vnetResponse.Value;

        List<PeeringSummary> peerings = [];
        foreach (var peering in vnet.Data.VirtualNetworkPeerings)
        {
            peerings.Add(new PeeringSummary(
                peering.Name,
                vnetName,
                peering.RemoteVirtualNetworkId?.ToString(),
                peering.PeeringState?.ToString() ?? "Unknown",
                peering.PeeringSyncLevel?.ToString() ?? "NotReported",
                peering.AllowVirtualNetworkAccess ?? false,
                peering.AllowForwardedTraffic ?? false,
                peering.AllowGatewayTransit ?? false));
        }

        return new PeeringCheckResult(vnetName, resourceGroup, peerings);
    }

    private static VirtualNetworkTopology MapTopology(VirtualNetworkData vnet)
    {
        List<string> addressPrefixes = vnet.AddressSpace?.AddressPrefixes?.ToList() ?? [];
        List<SubnetSummary> subnets = [];

        foreach (var subnet in vnet.Subnets)
        {
            subnets.Add(new SubnetSummary(
                subnet.Name,
                subnet.AddressPrefix,
                subnet.NetworkSecurityGroup?.Id?.Name,
                subnet.RouteTable?.Id?.Name,
                subnet.PrivateEndpoints?.Count ?? 0));
        }

        return new VirtualNetworkTopology(
            vnet.Name,
            vnet.Id.ResourceGroupName ?? string.Empty,
            vnet.Location?.ToString() ?? string.Empty,
            addressPrefixes,
            subnets,
            vnet.VirtualNetworkPeerings?.Count ?? 0);
    }
}
