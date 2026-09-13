// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Mcp.Core.Options;
using Microsoft.Mcp.Core.Options;

namespace Azure.Mcp.Tools.AppDiagnostics.Options.Logs;

public class LogsAnalyzeOptions : ISubscriptionOption
{
    [Option(Description = "The name of the Application.")]
    public required string App { get; set; }

    [Option(Description = "Time window for log analysis, e.g. PT1H, PT2H, PT6H.")]
    public string TimeWindow { get; set; } = "PT2H";

    [Option(Description = OptionDescriptions.ResourceGroup)]
    public required string ResourceGroup { get; set; }

    [Option(Description = OptionDescriptions.Subscription)]
    public string? Subscription { get; set; }

    [Option(Description = OptionDescriptions.Tenant)]
    public string? Tenant { get; set; }
}
