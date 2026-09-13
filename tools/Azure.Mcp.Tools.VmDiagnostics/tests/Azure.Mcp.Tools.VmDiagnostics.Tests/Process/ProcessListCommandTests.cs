// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using Azure.Mcp.Tests.Commands;
using Azure.Mcp.Tools.VmDiagnostics.Commands.Process;
using Azure.Mcp.Tools.VmDiagnostics.Services;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Azure.Mcp.Tools.VmDiagnostics.Tests.Process;

public class ProcessListCommandTests : SubscriptionCommandUnitTestsBase<ProcessListCommand, IVmDiagnosticsService>
{
    [Fact]
    public async Task ExecuteAsync_HandlesNotImplemented_WhenVmTelemetryIsUnavailable()
    {
        var subId = "12345678-1234-1234-1234-123456789012";

        Service.WhenForAnyArgs(x => x.ListTopProcessesAsync(default!, default!, default!, default, default, default))
            .Do(_ => throw new NotImplementedException("VM process enumeration is not implemented yet."));

        var response = await ExecuteCommandAsync("--vm", "vm1", "--resource-group", "rg1", "--subscription", subId);

        Assert.Equal(HttpStatusCode.InternalServerError, response.Status);
        Assert.StartsWith("VM process enumeration is not implemented yet.", response.Message);
    }
}
