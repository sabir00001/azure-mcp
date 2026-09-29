// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Azure.Mcp.Tools.VmDiagnostics.Commands.EventLogs;
using Azure.Mcp.Tools.VmDiagnostics.Commands.Health;
using Azure.Mcp.Tools.VmDiagnostics.Commands.Metrics;
using Azure.Mcp.Tools.VmDiagnostics.Commands.Process;
using Azure.Mcp.Tools.VmDiagnostics.Commands.Service;
using Azure.Mcp.Tools.VmDiagnostics.Models;

namespace Azure.Mcp.Tools.VmDiagnostics;

[JsonSerializable(typeof(HealthGetCommand.HealthGetCommandResult))]
[JsonSerializable(typeof(MetricsReviewCommand.MetricsReviewCommandResult))]
[JsonSerializable(typeof(ProcessListCommand.ProcessListCommandResult))]
[JsonSerializable(typeof(EventLogsQueryCommand.EventLogsQueryCommandResult))]
[JsonSerializable(typeof(ServiceStatusCommand.ServiceStatusCommandResult))]
[JsonSerializable(typeof(VmHealthSummaryResult))]
[JsonSerializable(typeof(ExtensionHealthSummary))]
[JsonSerializable(typeof(VmMetricsReviewResult))]
[JsonSerializable(typeof(VmMetricSeries))]
[JsonSerializable(typeof(MetricValuePoint))]
[JsonSerializable(typeof(VmProcessListResult))]
[JsonSerializable(typeof(ProcessInfo))]
[JsonSerializable(typeof(List<ProcessInfo>))]
[JsonSerializable(typeof(VmEventLogsQueryResult))]
[JsonSerializable(typeof(EventLogEntry))]
[JsonSerializable(typeof(VmServiceStatusResult))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal partial class VmDiagnosticsJsonContext : JsonSerializerContext;
