// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Mcp.Tools.AppDiagnostics.Models;

public record AppHealthCheckResult(
    string AppName,
    string ResourceGroup,
    string AppType,
    string State,
    string? AvailabilityState,
    bool HttpsOnly,
    bool VnetIntegrated,
    string OutputSummary);
