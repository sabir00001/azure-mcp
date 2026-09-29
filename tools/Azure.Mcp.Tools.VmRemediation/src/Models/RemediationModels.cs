// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Mcp.Tools.VmRemediation.Models;

public enum RemediationOs
{
    Windows,
    Linux
}

public sealed record RemediationState(
    string State,
    bool Exists,
    string RawOutput);

public sealed record RemediationResult(
    string Status,
    string Action,
    string? Reason,
    string IncidentId,
    string Vm,
    string? Service,
    RemediationState Before,
    RemediationState? After,
    string? ProposedAction,
    string? RawOutput,
    int ActionsLastHour,
    int ActionsLast24Hours,
    int IncidentAttempts);

public sealed record RemediationApproval(bool Approved, int IncidentAttempts, string? Reason);
