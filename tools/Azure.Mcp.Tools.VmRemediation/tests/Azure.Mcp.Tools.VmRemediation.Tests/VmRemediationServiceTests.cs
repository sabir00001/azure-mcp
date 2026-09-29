// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Mcp.Core.Services.Azure;
using Azure.Mcp.Tools.VmRemediation.Services;
using NSubstitute;
using Xunit;

namespace Azure.Mcp.Tools.VmRemediation.Tests;

public sealed class VmRemediationServiceTests
{
    [Fact]
    public async Task DefaultApprovalProvider_FailsClosed()
    {
        var provider = new FailClosedRemediationApprovalProvider();
        var result = await provider.GetApprovalAsync("INC-1", "/subscriptions/sub/resourceGroups/rg/providers/Microsoft.Compute/virtualMachines/vm", "restart VM vm", null, TestContext.Current.CancellationToken);
        Assert.False(result.Approved);
        Assert.Equal(0, result.IncidentAttempts);
    }

    [Fact]
    public void ServiceCanBeConstructed()
    {
        var service = new VmRemediationService(
            Substitute.For<IAzureService>(),
            new FailClosedRemediationApprovalProvider());
        Assert.NotNull(service);
    }
}
