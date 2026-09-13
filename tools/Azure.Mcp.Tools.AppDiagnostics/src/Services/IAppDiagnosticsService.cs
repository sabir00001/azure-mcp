// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Mcp.Tools.AppDiagnostics.Models;

namespace Azure.Mcp.Tools.AppDiagnostics.Services;

public interface IAppDiagnosticsService
{
    Task<AppHealthCheckResult> CheckAppHealthAsync(
        string appName,
        string resourceGroup,
        string subscription,
        string? tenant = null,
        CancellationToken cancellationToken = default);

    Task<AppDeploymentTriageResult> TriageDeploymentAsync(
        string appName,
        string resourceGroup,
        string subscription,
        string? tenant = null,
        CancellationToken cancellationToken = default);

    Task<AppLogsAnalysisResult> AnalyzeAppLogsAsync(
        string appName,
        string resourceGroup,
        string subscription,
        string timeWindow = "PT2H",
        string? tenant = null,
        CancellationToken cancellationToken = default);
}
