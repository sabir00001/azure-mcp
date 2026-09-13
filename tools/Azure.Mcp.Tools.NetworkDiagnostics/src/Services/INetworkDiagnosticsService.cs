// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Mcp.Tools.NetworkDiagnostics.Models;

namespace Azure.Mcp.Tools.NetworkDiagnostics.Services;

public interface INetworkDiagnosticsService
{
    Task<NetworkTopologySummaryResult> GetTopologySummaryAsync(
        string subscription,
        string? resourceGroup = null,
        string? tenant = null,
        CancellationToken cancellationToken = default);

    Task<NsgValidationResult> ValidateNsgRulesAsync(
        string nsgName,
        string resourceGroup,
        string subscription,
        string direction,
        string protocol,
        string sourceAddress,
        int destinationPort,
        string? tenant = null,
        CancellationToken cancellationToken = default);

    Task<PrivateDnsValidationResult> ValidatePrivateDnsAsync(
        string subscription,
        string? resourceGroup = null,
        string? tenant = null,
        CancellationToken cancellationToken = default);

    Task<PeeringCheckResult> CheckPeeringStatusAsync(
        string vnetName,
        string resourceGroup,
        string subscription,
        string? tenant = null,
        CancellationToken cancellationToken = default);
}
