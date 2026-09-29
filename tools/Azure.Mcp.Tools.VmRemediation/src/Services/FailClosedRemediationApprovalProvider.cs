// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Mcp.Tools.VmRemediation.Models;

namespace Azure.Mcp.Tools.VmRemediation.Services;

public sealed class FailClosedRemediationApprovalProvider : IRemediationApprovalProvider
{
    public Task<RemediationApproval> GetApprovalAsync(
        string incidentId,
        string vmResourceId,
        string proposedAction,
        string? tenant,
        CancellationToken cancellationToken = default)
        => Task.FromResult(new RemediationApproval(
            Approved: false,
            IncidentAttempts: 0,
            Reason: "No ITSM approval provider is configured."));
}
