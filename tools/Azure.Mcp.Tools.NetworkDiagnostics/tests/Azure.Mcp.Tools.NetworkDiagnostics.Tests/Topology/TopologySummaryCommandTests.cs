// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Mcp.Tests.Commands;
using Azure.Mcp.Tools.NetworkDiagnostics.Commands.Topology;
using Azure.Mcp.Tools.NetworkDiagnostics.Models;
using Azure.Mcp.Tools.NetworkDiagnostics.Services;
using NSubstitute;
using Xunit;

namespace Azure.Mcp.Tools.NetworkDiagnostics.Tests.Topology;

public class TopologySummaryCommandTests : SubscriptionCommandUnitTestsBase<TopologySummaryCommand, INetworkDiagnosticsService>
{
    [Fact]
    public async Task ExecuteAsync_ReturnsTopologySummary_WhenSubscriptionProvided()
    {
        var subId = "12345678-1234-1234-1234-123456789012";
        var expectedSummary = new NetworkTopologySummaryResult(subId, [
            new VirtualNetworkTopology("vnet1", "rg1", "eastus", ["10.0.0.0/16"], [
                new SubnetSummary("subnet1", "10.0.1.0/24", "nsg1", "rt1", 0)
            ], 0)
        ]);

        Service.GetTopologySummaryAsync(subId, null, null, Arg.Any<CancellationToken>())
            .Returns(expectedSummary);

        var response = await ExecuteCommandAsync("--subscription", subId);

        var result = ValidateAndDeserializeResponse(response, NetworkDiagnosticsJsonContext.Default.TopologySummaryCommandResult);

        Assert.NotNull(result.Summary);
        Assert.Equal(subId, result.Summary.Subscription);
        Assert.Single(result.Summary.VirtualNetworks);
        Assert.Equal("vnet1", result.Summary.VirtualNetworks[0].VnetName);
    }
}
