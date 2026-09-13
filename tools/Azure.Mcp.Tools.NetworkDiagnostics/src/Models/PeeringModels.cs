// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Mcp.Tools.NetworkDiagnostics.Models;

public record PeeringSummary(
    string PeeringName,
    string SourceVnet,
    string? RemoteVnetId,
    string PeeringState,
    string SyncState,
    bool AllowVirtualNetworkAccess,
    bool AllowForwardedTraffic,
    bool AllowGatewayTransit);

public record PeeringCheckResult(
    string VnetName,
    string ResourceGroup,
    List<PeeringSummary> Peerings);
