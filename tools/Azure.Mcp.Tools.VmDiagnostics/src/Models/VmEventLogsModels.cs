// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Mcp.Tools.VmDiagnostics.Models;

public record EventLogEntry(
    string LogName,
    string Level,
    DateTimeOffset TimeCreated,
    string Source,
    int EventId,
    string Message);

public record VmEventLogsQueryResult(
    string VmName,
    string ResourceGroup,
    string LogType,
    List<EventLogEntry> Entries);
