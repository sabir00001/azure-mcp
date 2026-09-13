// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Mcp.Tests.Commands;
using Azure.Mcp.Tools.NetworkDiagnostics.Commands.Nsg;
using Azure.Mcp.Tools.NetworkDiagnostics.Models;
using Azure.Mcp.Tools.NetworkDiagnostics.Services;
using NSubstitute;
using Xunit;

namespace Azure.Mcp.Tools.NetworkDiagnostics.Tests.Nsg;

public class NsgValidateCommandTests : SubscriptionCommandUnitTestsBase<NsgValidateCommand, INetworkDiagnosticsService>
{
    [Fact]
    public async Task ExecuteAsync_ReturnsValidation_WhenValidArgumentsProvided()
    {
        var subId = "12345678-1234-1234-1234-123456789012";
        var expected = new NsgValidationResult(
            "nsg1", "rg1", "AllowHTTPS", "Allow", 100, "Inbound", "TCP", "Matched rule AllowHTTPS", []);

        Service.ValidateNsgRulesAsync("nsg1", "rg1", subId, "Inbound", "TCP", "10.0.0.4", 443, null, Arg.Any<CancellationToken>())
            .Returns(expected);

        var response = await ExecuteCommandAsync(
            "--nsg", "nsg1",
            "--resource-group", "rg1",
            "--subscription", subId,
            "--direction", "Inbound",
            "--protocol", "TCP",
            "--source-address", "10.0.0.4",
            "--destination-port", "443");

        var result = ValidateAndDeserializeResponse(response, NetworkDiagnosticsJsonContext.Default.NsgValidateCommandResult);

        Assert.NotNull(result.Result);
        Assert.Equal("AllowHTTPS", result.Result.MatchedRuleName);
        Assert.Equal("Allow", result.Result.Decision);
    }
}
