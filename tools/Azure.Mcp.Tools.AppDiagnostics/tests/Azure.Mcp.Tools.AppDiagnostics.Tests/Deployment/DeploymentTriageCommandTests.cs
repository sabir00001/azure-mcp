// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Mcp.Tests.Commands;
using Azure.Mcp.Tools.AppDiagnostics.Commands.Deployment;
using Azure.Mcp.Tools.AppDiagnostics.Models;
using Azure.Mcp.Tools.AppDiagnostics.Services;
using NSubstitute;
using Xunit;

namespace Azure.Mcp.Tools.AppDiagnostics.Tests.Deployment;

public class DeploymentTriageCommandTests : SubscriptionCommandUnitTestsBase<DeploymentTriageCommand, IAppDiagnosticsService>
{
    [Fact]
    public async Task ExecuteAsync_ReturnsDeploymentTriage_WhenAppAndRgProvided()
    {
        var subId = "12345678-1234-1234-1234-123456789012";
        var expected = new AppDeploymentTriageResult("app1", "rg1", "production", [
            new DeploymentRecord("dep-1", "Failed", DateTimeOffset.UtcNow, "bot", "Build failed")
        ], "Latest deployment failed");

        Service.TriageDeploymentAsync("app1", "rg1", subId, null, Arg.Any<CancellationToken>())
            .Returns(expected);

        var response = await ExecuteCommandAsync("--app", "app1", "--resource-group", "rg1", "--subscription", subId);

        var result = ValidateAndDeserializeResponse(response, AppDiagnosticsJsonContext.Default.DeploymentTriageCommandResult);

        Assert.NotNull(result.Result);
        Assert.Single(result.Result.RecentDeployments);
        Assert.Equal("Failed", result.Result.RecentDeployments[0].Status);
    }
}
