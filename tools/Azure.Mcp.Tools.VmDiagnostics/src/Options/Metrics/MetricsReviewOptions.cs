// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Mcp.Core.Options;
using Microsoft.Mcp.Core.Options;

namespace Azure.Mcp.Tools.VmDiagnostics.Options.Metrics;

public class MetricsReviewOptions : ISubscriptionOption
{
    [Option(Description = "The name of the Virtual Machine.")]
    public required string Vm { get; set; }

    [Option(Description = "The time window for metrics review, e.g. PT1H, PT6H, P1D.")]
    public string Timespan { get; set; } = "PT1H";

    [Option(Description = OptionDescriptions.ResourceGroup)]
    public required string ResourceGroup { get; set; }

    [Option(Description = OptionDescriptions.Subscription)]
    public string? Subscription { get; set; }

    [Option(Description = OptionDescriptions.Tenant)]
    public string? Tenant { get; set; }
}
