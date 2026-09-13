// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Mcp.Tests.Commands;
using Azure.Mcp.Tools.VmDiagnostics.Commands.Health;
using Azure.Mcp.Tools.VmDiagnostics.Models;
using Azure.Mcp.Tools.VmDiagnostics.Services;
using NSubstitute;
using Xunit;

namespace Azure.Mcp.Tools.VmDiagnostics.Tests.Health;

public class HealthGetCommandTests : SubscriptionCommandUnitTestsBase<HealthGetCommand, IVmDiagnosticsService>
{
    [Fact]
    public async Task ExecuteAsync_ReturnsHealthSummary_WhenVmAndRgProvided()
    {
        var subId = "12345678-1234-1234-1234-123456789012";
        var expected = new VmHealthSummaryResult("vm1", "rg1", "eastus", "PowerState/running", "Succeeded", "2.8.0", "Ready", []);

        Service.GetVmHealthAsync("vm1", "rg1", subId, null, Arg.Any<CancellationToken>())
            .Returns(expected);

        var response = await ExecuteCommandAsync("--vm", "vm1", "--resource-group", "rg1", "--subscription", subId);

        var result = ValidateAndDeserializeResponse(response, VmDiagnosticsJsonContext.Default.HealthGetCommandResult);

        Assert.NotNull(result.Result);
        Assert.Equal("vm1", result.Result.VmName);
        Assert.Equal("PowerState/running", result.Result.PowerState);
    }
}
