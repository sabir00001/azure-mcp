// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Mcp.Core.Options;
using Microsoft.Mcp.Core.Options;

namespace Azure.Mcp.Tools.NetworkDiagnostics.Options.Nsg;

public class NsgValidateOptions : ISubscriptionOption
{
    [Option(Description = "The name of the Network Security Group.")]
    public required string Nsg { get; set; }

    [Option(Description = "Traffic direction: Inbound or Outbound.")]
    public required string Direction { get; set; }

    [Option(Description = "Protocol: TCP, UDP, or *.")]
    public required string Protocol { get; set; }

    [Option(Description = "Source address or CIDR prefix.")]
    public required string SourceAddress { get; set; }

    [Option(Description = "Target destination port number.")]
    public required int DestinationPort { get; set; }

    [Option(Description = OptionDescriptions.ResourceGroup)]
    public required string ResourceGroup { get; set; }

    [Option(Description = OptionDescriptions.Subscription)]
    public string? Subscription { get; set; }

    [Option(Description = OptionDescriptions.Tenant)]
    public string? Tenant { get; set; }
}
