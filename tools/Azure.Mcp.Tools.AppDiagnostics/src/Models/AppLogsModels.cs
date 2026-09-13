// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Mcp.Tools.AppDiagnostics.Models;

public record AppTraceEntry(
    DateTimeOffset Timestamp,
    string SeverityLevel,
    string Message,
    string? ExceptionType,
    string? OuterMessage);

public record AppLogsAnalysisResult(
    string AppName,
    string ResourceGroup,
    string TimeWindow,
    int TotalErrorCount,
    List<AppTraceEntry> RecentExceptions,
    string CorrelationSummary);
