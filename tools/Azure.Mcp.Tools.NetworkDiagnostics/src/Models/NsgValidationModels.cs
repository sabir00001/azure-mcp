// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Mcp.Tools.NetworkDiagnostics.Models;

public record SecurityRuleSummary(
    string Name,
    int Priority,
    string Direction,
    string Access,
    string Protocol,
    string SourceAddressPrefix,
    string SourcePortRange,
    string DestinationAddressPrefix,
    string DestinationPortRange);

public record NsgValidationResult(
    string NsgName,
    string ResourceGroup,
    string MatchedRuleName,
    string Decision,
    int RulePriority,
    string Direction,
    string Protocol,
    string Reason,
    List<SecurityRuleSummary> EvaluatedRules);
