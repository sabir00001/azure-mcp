// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Azure.Mcp.Tools.NetworkDiagnostics.Commands.Nsg;
using Azure.Mcp.Tools.NetworkDiagnostics.Commands.Peering;
using Azure.Mcp.Tools.NetworkDiagnostics.Commands.PrivateDns;
using Azure.Mcp.Tools.NetworkDiagnostics.Commands.Topology;
using Azure.Mcp.Tools.NetworkDiagnostics.Models;

namespace Azure.Mcp.Tools.NetworkDiagnostics;

[JsonSerializable(typeof(TopologySummaryCommand.TopologySummaryCommandResult))]
[JsonSerializable(typeof(NsgValidateCommand.NsgValidateCommandResult))]
[JsonSerializable(typeof(PrivateDnsValidateCommand.PrivateDnsValidateCommandResult))]
[JsonSerializable(typeof(PeeringCheckCommand.PeeringCheckCommandResult))]
[JsonSerializable(typeof(NetworkTopologySummaryResult))]
[JsonSerializable(typeof(VirtualNetworkTopology))]
[JsonSerializable(typeof(SubnetSummary))]
[JsonSerializable(typeof(NsgValidationResult))]
[JsonSerializable(typeof(SecurityRuleSummary))]
[JsonSerializable(typeof(PrivateDnsValidationResult))]
[JsonSerializable(typeof(PrivateDnsZoneSummary))]
[JsonSerializable(typeof(PrivateDnsRecordSummary))]
[JsonSerializable(typeof(PeeringCheckResult))]
[JsonSerializable(typeof(PeeringSummary))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal partial class NetworkDiagnosticsJsonContext : JsonSerializerContext;
