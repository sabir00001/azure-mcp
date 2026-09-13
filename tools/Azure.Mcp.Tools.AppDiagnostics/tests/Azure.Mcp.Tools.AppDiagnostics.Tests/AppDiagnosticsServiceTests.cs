// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Mcp.Core.Services.Azure;
using Azure.Mcp.Tools.AppDiagnostics.Services;
using NSubstitute;
using Xunit;

namespace Azure.Mcp.Tools.AppDiagnostics.Tests;

public class AppDiagnosticsServiceTests
{
    private readonly AppDiagnosticsService _service = new(Substitute.For<IAzureService>());

    [Fact]
    public async Task TriageDeploymentAsync_ThrowsNotImplementedException()
    {
        await Assert.ThrowsAsync<NotImplementedException>(() =>
            _service.TriageDeploymentAsync("app1", "rg1", "sub1", cancellationToken: CancellationToken.None));
    }

    [Fact]
    public async Task AnalyzeAppLogsAsync_ThrowsNotImplementedException()
    {
        await Assert.ThrowsAsync<NotImplementedException>(() =>
            _service.AnalyzeAppLogsAsync("app1", "rg1", "sub1", cancellationToken: CancellationToken.None));
    }
}