// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Mcp.Tests.Commands;
using Azure.Mcp.Tools.AppDiagnostics.Commands.Health;
using Azure.Mcp.Tools.AppDiagnostics.Models;
using Azure.Mcp.Tools.AppDiagnostics.Services;
using NSubstitute;
using Xunit;

namespace Azure.Mcp.Tools.AppDiagnostics.Tests.Health;

public class HealthCheckCommandTests : SubscriptionCommandUnitTestsBase<HealthCheckCommand, IAppDiagnosticsService>
{
    [Fact]
    public async Task ExecuteAsync_ReturnsAppHealth_WhenAppAndRgProvided()
    {
        var subId = "12345678-1234-1234-1234-123456789012";
        var expected = new AppHealthCheckResult("app1", "rg1", "WebApp", "Running", "Normal", true, true, "Healthy");

        Service.CheckAppHealthAsync("app1", "rg1", subId, null, Arg.Any<CancellationToken>())
            .Returns(expected);

        var response = await ExecuteCommandAsync("--app", "app1", "--resource-group", "rg1", "--subscription", subId);

        var result = ValidateAndDeserializeResponse(response, AppDiagnosticsJsonContext.Default.HealthCheckCommandResult);

        Assert.NotNull(result.Result);
        Assert.Equal("app1", result.Result.AppName);
        Assert.Equal("Running", result.Result.State);
    }
}
