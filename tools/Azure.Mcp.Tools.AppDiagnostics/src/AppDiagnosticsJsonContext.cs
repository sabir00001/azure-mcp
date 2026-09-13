// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Azure.Mcp.Tools.AppDiagnostics.Commands.Deployment;
using Azure.Mcp.Tools.AppDiagnostics.Commands.Health;
using Azure.Mcp.Tools.AppDiagnostics.Commands.Logs;
using Azure.Mcp.Tools.AppDiagnostics.Models;

namespace Azure.Mcp.Tools.AppDiagnostics;

[JsonSerializable(typeof(HealthCheckCommand.HealthCheckCommandResult))]
[JsonSerializable(typeof(DeploymentTriageCommand.DeploymentTriageCommandResult))]
[JsonSerializable(typeof(LogsAnalyzeCommand.LogsAnalyzeCommandResult))]
[JsonSerializable(typeof(AppHealthCheckResult))]
[JsonSerializable(typeof(AppDeploymentTriageResult))]
[JsonSerializable(typeof(DeploymentRecord))]
[JsonSerializable(typeof(AppLogsAnalysisResult))]
[JsonSerializable(typeof(AppTraceEntry))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal partial class AppDiagnosticsJsonContext : JsonSerializerContext;
