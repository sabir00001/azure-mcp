// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Mcp.Core.Options;
using Microsoft.Mcp.Core.Options;

namespace Azure.Mcp.Tools.VmDiagnostics.Options.EventLogs;

public class EventLogsQueryOptions : ISubscriptionOption
{
    [Option(Description = "The name of the Virtual Machine.")]
    public required string Vm { get; set; }

    [Option(Description = "Log type to query: System, Application, or syslog.")]
    public string LogType { get; set; } = "System";

    [Option(Description = "Maximum number of event log entries to return.")]
    public int MaxEntries { get; set; } = 50;

    [Option(Description = OptionDescriptions.ResourceGroup)]
    public required string ResourceGroup { get; set; }

    [Option(Description = OptionDescriptions.Subscription)]
    public string? Subscription { get; set; }

    [Option(Description = OptionDescriptions.Tenant)]
    public string? Tenant { get; set; }
}
