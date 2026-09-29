// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Mcp.Tools.VmRemediation.Models;

namespace Azure.Mcp.Tools.VmRemediation.Services;

public interface IRemediationApprovalProvider
{
    Task<RemediationApproval> GetApprovalAsync(
        string incidentId,
        string vmResourceId,
        string proposedAction,
        string? tenant,
        CancellationToken cancellationToken = default);
}
