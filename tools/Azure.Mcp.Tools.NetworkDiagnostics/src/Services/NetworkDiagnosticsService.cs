// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Core;
using Azure.Mcp.Core.Services.Azure;
using Azure.Mcp.Tools.NetworkDiagnostics.Models;
using Azure.ResourceManager;
using Azure.ResourceManager.Network;
using Azure.ResourceManager.Network.Models;
using System.Net;

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

    public async Task<NsgValidationResult> ValidateNsgRulesAsync(
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

        if (destinationPort is < 1 or > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(destinationPort), "Destination port must be between 1 and 65535.");
        }

        var normalizedDirection = NormalizeDirection(direction);
        var normalizedProtocol = NormalizeProtocol(protocol);
        var subResource = await AzureService.GetSubscription(subscription, tenant, cancellationToken: cancellationToken);
        var rg = await subResource.GetResourceGroupAsync(resourceGroup, cancellationToken);
        var response = await rg.Value.GetNetworkSecurityGroupAsync(nsgName, cancellationToken: cancellationToken);
        var nsg = response.Value.Data;
        var rules = (nsg.SecurityRules ?? [])
            .Concat(nsg.DefaultSecurityRules ?? [])
            .OrderBy(rule => rule.Priority ?? int.MaxValue)
            .ToList();

        var evaluatedRules = rules
            .Where(rule => string.Equals(rule.Direction?.ToString(), normalizedDirection, StringComparison.OrdinalIgnoreCase))
            .Select(MapRule)
            .ToList();
        var matchedRule = rules.FirstOrDefault(rule =>
            string.Equals(rule.Direction?.ToString(), normalizedDirection, StringComparison.OrdinalIgnoreCase)
            && ProtocolMatches(rule.Protocol?.ToString(), normalizedProtocol)
            && AddressMatches(rule, sourceAddress)
            && PortMatches(rule, destinationPort));

        if (matchedRule is null)
        {
            return new NsgValidationResult(
                nsgName,
                resourceGroup,
                "NoMatchingRule",
                "Deny",
                0,
                normalizedDirection,
                normalizedProtocol,
                "No rule matched the requested direction, protocol, source address, and destination port; traffic is denied by default.",
                evaluatedRules);
        }

        var decision = matchedRule.Access?.ToString() ?? "Deny";
        return new NsgValidationResult(
            nsgName,
            resourceGroup,
            matchedRule.Name ?? "Unknown",
            decision,
            matchedRule.Priority ?? 0,
            normalizedDirection,
            normalizedProtocol,
            $"Rule '{matchedRule.Name}' matched at priority {matchedRule.Priority}.",
            evaluatedRules);
    }

    private static string NormalizeDirection(string direction) => direction.Trim().ToLowerInvariant() switch
    {
        "inbound" or "in" => "Inbound",
        "outbound" or "out" => "Outbound",
        _ => throw new ArgumentException("Direction must be Inbound or Outbound.", nameof(direction))
    };

    private static string NormalizeProtocol(string protocol) => protocol.Trim().ToLowerInvariant() switch
    {
        "tcp" => "Tcp",
        "udp" => "Udp",
        "*" or "any" => "*",
        _ => throw new ArgumentException("Protocol must be TCP, UDP, or *.", nameof(protocol))
    };

    private static bool ProtocolMatches(string? ruleProtocol, string protocol)
        => protocol == "*" || ruleProtocol is null || ruleProtocol.Equals("*", StringComparison.OrdinalIgnoreCase)
            || ruleProtocol.Equals(protocol, StringComparison.OrdinalIgnoreCase);

    private static bool AddressMatches(SecurityRuleData rule, string sourceAddress)
    {
        var prefixes = new List<string>();
        if (!string.IsNullOrWhiteSpace(rule.SourceAddressPrefix))
        {
            prefixes.Add(rule.SourceAddressPrefix);
        }

        prefixes.AddRange(rule.SourceAddressPrefixes ?? []);
        if (rule.SourceApplicationSecurityGroups?.Count > 0)
        {
            prefixes.AddRange(rule.SourceApplicationSecurityGroups.Select(group => group.Id?.Name ?? string.Empty));
        }

        return prefixes.Any(prefix => AddressOrTagMatches(prefix, sourceAddress));
    }

    private static bool AddressOrTagMatches(string prefix, string sourceAddress)
    {
        if (prefix == "*" || prefix.Equals(sourceAddress, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (prefix.Equals("VirtualNetwork", StringComparison.OrdinalIgnoreCase)
            || prefix.Equals("Internet", StringComparison.OrdinalIgnoreCase)
            || prefix.Equals("AzureLoadBalancer", StringComparison.OrdinalIgnoreCase))
        {
            return sourceAddress.Equals(prefix, StringComparison.OrdinalIgnoreCase);
        }

        if (IPAddress.TryParse(sourceAddress, out var address)
            && prefix.Contains('/')
            && TryParseCidr(prefix, out var network, out var bits))
        {
            return IsInCidr(address, network, bits);
        }

        return false;
    }

    private static bool PortMatches(SecurityRuleData rule, int destinationPort)
    {
        var ranges = new List<string>();
        if (!string.IsNullOrWhiteSpace(rule.DestinationPortRange))
        {
            ranges.Add(rule.DestinationPortRange);
        }

        ranges.AddRange(rule.DestinationPortRanges ?? []);
        return ranges.Count == 0 || ranges.Any(range => PortRangeMatches(range, destinationPort));
    }

    private static bool PortRangeMatches(string range, int port)
    {
        if (range == "*")
        {
            return true;
        }

        var parts = range.Split('-', 2, StringSplitOptions.TrimEntries);
        return parts.Length == 1
            ? int.TryParse(parts[0], out var single) && single == port
            : int.TryParse(parts[0], out var start) && int.TryParse(parts[1], out var end) && port >= start && port <= end;
    }

    private static SecurityRuleSummary MapRule(SecurityRuleData rule)
        => new(
            rule.Name ?? "Unknown",
            rule.Priority ?? 0,
            rule.Direction?.ToString() ?? "Unknown",
            rule.Access?.ToString() ?? "Unknown",
            rule.Protocol?.ToString() ?? "Unknown",
            FormatValues(rule.SourceAddressPrefix, rule.SourceAddressPrefixes, rule.SourceApplicationSecurityGroups),
            FormatValues(rule.SourcePortRange, rule.SourcePortRanges),
            FormatValues(rule.DestinationAddressPrefix, rule.DestinationAddressPrefixes, rule.DestinationApplicationSecurityGroups),
            FormatValues(rule.DestinationPortRange, rule.DestinationPortRanges));

    private static string FormatValues(string? singular, IEnumerable<string>? plural, IEnumerable<ApplicationSecurityGroupData>? groups = null)
        => string.Join(", ", new[] { singular ?? string.Empty }
            .Concat(plural ?? [])
            .Concat(groups?.Select(group => group.Id?.Name ?? string.Empty) ?? [])
            .Where(value => !string.IsNullOrWhiteSpace(value)));

    private static bool TryParseCidr(string value, out IPAddress network, out int bits)
    {
        network = IPAddress.None;
        bits = 0;
        var parts = value.Split('/', 2);
        if (parts.Length != 2 || !IPAddress.TryParse(parts[0], out var parsedNetwork) || !int.TryParse(parts[1], out bits))
        {
            return false;
        }

        network = parsedNetwork;
        return bits >= 0 && bits <= network.GetAddressBytes().Length * 8;
    }

    private static bool IsInCidr(IPAddress address, IPAddress network, int bits)
    {
        var addressBytes = address.GetAddressBytes();
        var networkBytes = network.GetAddressBytes();
        if (addressBytes.Length != networkBytes.Length)
        {
            return false;
        }

        var fullBytes = bits / 8;
        var remainingBits = bits % 8;
        if (!addressBytes.Take(fullBytes).SequenceEqual(networkBytes.Take(fullBytes)))
        {
            return false;
        }

        return remainingBits == 0
            || (addressBytes[fullBytes] >> (8 - remainingBits)) == (networkBytes[fullBytes] >> (8 - remainingBits));
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
