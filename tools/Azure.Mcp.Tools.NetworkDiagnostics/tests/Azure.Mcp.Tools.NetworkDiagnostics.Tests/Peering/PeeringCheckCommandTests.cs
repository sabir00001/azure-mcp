// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Mcp.Tests.Commands;
using Azure.Mcp.Tools.NetworkDiagnostics.Commands.Peering;
using Azure.Mcp.Tools.NetworkDiagnostics.Models;
using Azure.Mcp.Tools.NetworkDiagnostics.Services;
using NSubstitute;
using Xunit;

namespace Azure.Mcp.Tools.NetworkDiagnostics.Tests.Peering;

public class PeeringCheckCommandTests : SubscriptionCommandUnitTestsBase<PeeringCheckCommand, INetworkDiagnosticsService>
{
    [Fact]
    public async Task ExecuteAsync_ReturnsPeeringCheckResult_WhenVnetAndRgProvided()
    {
        var subId = "12345678-1234-1234-1234-123456789012";
        var expected = new PeeringCheckResult("vnet1", "rg1", [
            new PeeringSummary("peering1", "vnet1", "/sub/rg/vnet2", "Connected", "InSync", true, true, false)
        ]);

        Service.CheckPeeringStatusAsync("vnet1", "rg1", subId, null, Arg.Any<CancellationToken>())
            .Returns(expected);

        var response = await ExecuteCommandAsync("--vnet", "vnet1", "--resource-group", "rg1", "--subscription", subId);

        var result = ValidateAndDeserializeResponse(response, NetworkDiagnosticsJsonContext.Default.PeeringCheckCommandResult);

        Assert.NotNull(result.Result);
        Assert.Single(result.Result.Peerings);
        Assert.Equal("peering1", result.Result.Peerings[0].PeeringName);
        Assert.Equal("Connected", result.Result.Peerings[0].PeeringState);
    }
}
