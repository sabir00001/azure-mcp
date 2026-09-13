// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Mcp.Tools.VmDiagnostics.Models;

namespace Azure.Mcp.Tools.VmDiagnostics.Services;

public interface IVmDiagnosticsService
{
    Task<VmHealthSummaryResult> GetVmHealthAsync(
        string vmName,
        string resourceGroup,
        string subscription,
        string? tenant = null,
        CancellationToken cancellationToken = default);

    Task<VmMetricsReviewResult> ReviewVmMetricsAsync(
        string vmName,
        string resourceGroup,
        string subscription,
        string timespan = "PT1H",
        string? tenant = null,
        CancellationToken cancellationToken = default);

    Task<VmProcessListResult> ListTopProcessesAsync(
        string vmName,
        string resourceGroup,
        string subscription,
        int top = 10,
        string? tenant = null,
        CancellationToken cancellationToken = default);

    Task<VmEventLogsQueryResult> QueryEventLogsAsync(
        string vmName,
        string resourceGroup,
        string subscription,
        string logType = "System",
        int maxEntries = 50,
        string? tenant = null,
        CancellationToken cancellationToken = default);
}
