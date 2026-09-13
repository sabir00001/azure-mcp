// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Mcp.Tests.Commands;
using Azure.Mcp.Tools.AppDiagnostics.Commands.Logs;
using Azure.Mcp.Tools.AppDiagnostics.Models;
using Azure.Mcp.Tools.AppDiagnostics.Services;
using NSubstitute;
using Xunit;

namespace Azure.Mcp.Tools.AppDiagnostics.Tests.Logs;

public class LogsAnalyzeCommandTests : SubscriptionCommandUnitTestsBase<LogsAnalyzeCommand, IAppDiagnosticsService>
{
    [Fact]
    public async Task ExecuteAsync_ReturnsLogsAnalysis_WhenAppAndRgProvided()
    {
        var subId = "12345678-1234-1234-1234-123456789012";
        var expected = new AppLogsAnalysisResult("app1", "rg1", "PT2H", 1, [
            new AppTraceEntry(DateTimeOffset.UtcNow, "Error", "NullRef", "System.NullReferenceException", "Null exception")
        ], "Found 1 error");

        Service.AnalyzeAppLogsAsync("app1", "rg1", subId, "PT2H", null, Arg.Any<CancellationToken>())
            .Returns(expected);

        var response = await ExecuteCommandAsync("--app", "app1", "--resource-group", "rg1", "--subscription", subId);

        var result = ValidateAndDeserializeResponse(response, AppDiagnosticsJsonContext.Default.LogsAnalyzeCommandResult);

        Assert.NotNull(result.Result);
        Assert.Equal(1, result.Result.TotalErrorCount);
        Assert.Single(result.Result.RecentExceptions);
    }
}
