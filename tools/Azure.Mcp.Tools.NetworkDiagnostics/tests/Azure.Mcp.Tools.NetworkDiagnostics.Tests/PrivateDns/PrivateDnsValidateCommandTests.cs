// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Mcp.Tests.Commands;
using Azure.Mcp.Tools.NetworkDiagnostics.Commands.PrivateDns;
using Azure.Mcp.Tools.NetworkDiagnostics.Models;
using Azure.Mcp.Tools.NetworkDiagnostics.Services;
using NSubstitute;
using Xunit;

namespace Azure.Mcp.Tools.NetworkDiagnostics.Tests.PrivateDns;

public class PrivateDnsValidateCommandTests : SubscriptionCommandUnitTestsBase<PrivateDnsValidateCommand, INetworkDiagnosticsService>
{
    [Fact]
    public async Task ExecuteAsync_ReturnsPrivateDnsValidation_WhenSubscriptionProvided()
    {
        var subId = "12345678-1234-1234-1234-123456789012";
        var expected = new PrivateDnsValidationResult(subId, [
            new PrivateDnsZoneSummary("privatelink.blob.core.windows.net", "rg1", 1, 1, [])
        ]);

        Service.ValidatePrivateDnsAsync(subId, null, null, Arg.Any<CancellationToken>())
            .Returns(expected);

        var response = await ExecuteCommandAsync("--subscription", subId);

        var result = ValidateAndDeserializeResponse(response, NetworkDiagnosticsJsonContext.Default.PrivateDnsValidateCommandResult);

        Assert.NotNull(result.Result);
        Assert.Single(result.Result.PrivateDnsZones);
        Assert.Equal("privatelink.blob.core.windows.net", result.Result.PrivateDnsZones[0].ZoneName);
    }
}
