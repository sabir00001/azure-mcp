// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using System.Text.Json;
using Azure.Mcp.Tests.Commands;
using Azure.Mcp.Tools.VmRemediation.Commands.Service;
using Azure.Mcp.Tools.VmRemediation.Commands.Vm;
using Azure.Mcp.Tools.VmRemediation.Models;
using Azure.Mcp.Tools.VmRemediation.Services;
using NSubstitute;
using Xunit;

namespace Azure.Mcp.Tools.VmRemediation.Tests;

public sealed class VmRemediationCommandTests : IDisposable
{
    [Fact]
    public async Task ServiceRestart_RejectsMissingIncidentId()
    {
        using var harness = new CommandHarness<ServiceRestartCommand, IVmRemediationService>();
        var response = await harness.ExecuteAsync("--vm", "vm1", "--resource-group", "rg1", "--service", "W3SVC", "--subscription", "sub1", "--incident-id", "");
        Assert.Equal(HttpStatusCode.BadRequest, response.Status);
        await harness.MockService.DidNotReceiveWithAnyArgs().RestartServiceAsync(default!, default!, default!, default!, default!, default, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task VmRestart_ReturnsApprovalRequiredResult()
    {
        using var harness = new CommandHarness<VmRestartCommand, IVmRemediationService>();
        harness.MockService.RestartVmAsync("vm1", "rg1", "INC-1", "sub1", null, Arg.Any<CancellationToken>())
            .Returns(new RemediationResult(
                "APPROVAL_REQUIRED",
                "vm_restart",
                "No ITSM approval provider is configured.",
                "INC-1",
                "vm1",
                null,
                new RemediationState("VM running", true, string.Empty),
                null,
                "restart VM vm1",
                null,
                0,
                0,
                0));

        var response = await harness.ExecuteAsync("--vm", "vm1", "--resource-group", "rg1", "--subscription", "sub1", "--incident-id", "INC-1");
        Assert.Equal(HttpStatusCode.OK, response.Status);
        var result = JsonSerializer.Deserialize(
            JsonSerializer.Serialize(response.Results),
            VmRemediationJsonContext.Default.VmRestartCommandResult);
        Assert.Equal("APPROVAL_REQUIRED", result!.Result.Status);
    }

    private sealed class CommandHarness<TCommand, TService> : SubscriptionCommandUnitTestsBase<TCommand, TService>
        where TCommand : class, Microsoft.Mcp.Core.Commands.IBaseCommand
        where TService : class
    {
        public TService MockService => Service;

        public Task<Microsoft.Mcp.Core.Models.Command.CommandResponse> ExecuteAsync(params string[] args)
            => ExecuteCommandAsync(args);
    }

    public void Dispose()
    {
    }
}
