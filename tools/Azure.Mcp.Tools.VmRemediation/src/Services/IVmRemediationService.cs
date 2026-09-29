// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Mcp.Tools.VmRemediation.Models;

namespace Azure.Mcp.Tools.VmRemediation.Services;

public interface IVmRemediationService
{
    Task<RemediationResult> RestartServiceAsync(
        string vm,
        string resourceGroup,
        string service,
        string incidentId,
        string subscription,
        string? tenant = null,
        CancellationToken cancellationToken = default);

    Task<RemediationResult> RestartVmAsync(
        string vm,
        string resourceGroup,
        string incidentId,
        string subscription,
        string? tenant = null,
        CancellationToken cancellationToken = default);
}
