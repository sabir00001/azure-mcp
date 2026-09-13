// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Mcp.Tools.NetworkDiagnostics.Models;

public record SubnetSummary(
    string Name,
    string? AddressPrefix,
    string? NetworkSecurityGroup,
    string? RouteTable,
    int PrivateEndpointCount);

public record VirtualNetworkTopology(
    string VnetName,
    string ResourceGroup,
    string Location,
    List<string> AddressPrefixes,
    List<SubnetSummary> Subnets,
    int PeeringCount);

public record NetworkTopologySummaryResult(
    string Subscription,
    List<VirtualNetworkTopology> VirtualNetworks);
