// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Mcp.Core.Services.Azure;
using Azure.Mcp.Tools.VmDiagnostics.Services;
using NSubstitute;
using Xunit;

namespace Azure.Mcp.Tools.VmDiagnostics.Tests;

public class VmDiagnosticsServiceTests
{
    private readonly VmDiagnosticsService _service = new(Substitute.For<IAzureService>());

    [Fact]
    public async Task ReviewVmMetricsAsync_ThrowsNotImplementedException()
    {
        await Assert.ThrowsAsync<NotImplementedException>(() =>
            _service.ReviewVmMetricsAsync("vm1", "rg1", "sub1", "PT1H", cancellationToken: CancellationToken.None));
    }

    [Fact]
    public async Task ListTopProcessesAsync_ThrowsNotImplementedException()
    {
        await Assert.ThrowsAsync<NotImplementedException>(() =>
            _service.ListTopProcessesAsync("vm1", "rg1", "sub1", 10, cancellationToken: CancellationToken.None));
    }

    [Fact]
    public async Task QueryEventLogsAsync_ThrowsNotImplementedException()
    {
        await Assert.ThrowsAsync<NotImplementedException>(() =>
            _service.QueryEventLogsAsync("vm1", "rg1", "sub1", "System", 20, cancellationToken: CancellationToken.None));
    }
}
