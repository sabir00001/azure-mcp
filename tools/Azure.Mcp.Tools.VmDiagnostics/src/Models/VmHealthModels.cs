// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Mcp.Tools.VmDiagnostics.Models;

public record ExtensionHealthSummary(
    string Name,
    string? Publisher,
    string? Type,
    string? ProvisioningState,
    string? StatusMessage);

public record VmHealthSummaryResult(
    string VmName,
    string ResourceGroup,
    string Location,
    string PowerState,
    string ProvisioningState,
    string? AgentVersion,
    string? AgentStatus,
    List<ExtensionHealthSummary> Extensions);
