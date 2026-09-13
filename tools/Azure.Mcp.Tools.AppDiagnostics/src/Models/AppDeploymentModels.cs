// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Mcp.Tools.AppDiagnostics.Models;

public record DeploymentRecord(
    string DeploymentId,
    string Status,
    DateTimeOffset EndTime,
    string Author,
    string Message);

public record AppDeploymentTriageResult(
    string AppName,
    string ResourceGroup,
    string ActiveSlotOrRevision,
    List<DeploymentRecord> RecentDeployments,
    string AnalysisFinding);
