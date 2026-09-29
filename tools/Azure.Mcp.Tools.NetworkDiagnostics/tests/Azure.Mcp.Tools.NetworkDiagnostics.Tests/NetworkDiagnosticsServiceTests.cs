// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Mcp.Core.Services.Azure;
using Azure.Mcp.Tools.NetworkDiagnostics.Services;
using NSubstitute;
using Xunit;

namespace Azure.Mcp.Tools.NetworkDiagnostics.Tests;

public class NetworkDiagnosticsServiceTests
{
    private readonly NetworkDiagnosticsService _service = new(Substitute.For<IAzureService>());

    [Fact]
    public async Task ValidateNsgRulesAsync_ValidatesDirection()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.ValidateNsgRulesAsync(
                "nsg1", "rg1", "sub1", "Sideways", "Tcp", "10.0.0.4", 443,
                cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ValidatePrivateDnsAsync_ThrowsNotImplementedException()
    {
        await Assert.ThrowsAsync<NotImplementedException>(() =>
            _service.ValidatePrivateDnsAsync(
                "sub1", cancellationToken: TestContext.Current.CancellationToken));
    }
}