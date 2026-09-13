// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Mcp.Core.Commands.Subscription;
using Azure.Mcp.Core.Services.Azure.Subscription;
using Azure.Mcp.Tools.VmDiagnostics.Models;
using Azure.Mcp.Tools.VmDiagnostics.Options.Metrics;
using Azure.Mcp.Tools.VmDiagnostics.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Mcp.Core.Commands;
using Microsoft.Mcp.Core.Models.Command;

namespace Azure.Mcp.Tools.VmDiagnostics.Commands.Metrics;

[CommandMetadata(
    Id = "6f3d8a75-c937-8665-d016-734e16fa6006",
    Name = "review",
    Title = "VM Performance Metrics Review",
    Description = "Reviews CPU percentage, available memory, disk IOPS, and network throughput metrics for a virtual machine.",
    OperationPlane = ToolOperationPlane.Control,
    Destructive = false,
    Idempotent = true,
    OpenWorld = false,
    ReadOnly = true,
    Secret = false,
    LocalRequired = false)]
public sealed class MetricsReviewCommand(
    ILogger<MetricsReviewCommand> logger,
    IVmDiagnosticsService vmDiagnosticsService,
    ISubscriptionResolver subscriptionResolver)
    : SubscriptionCommand<MetricsReviewOptions, MetricsReviewCommand.MetricsReviewCommandResult>(subscriptionResolver)
{
    private readonly ILogger<MetricsReviewCommand> _logger = logger;
    private readonly IVmDiagnosticsService _vmDiagnosticsService = vmDiagnosticsService;

    public override async Task<CommandResponse> ExecuteAsync(
        CommandContext context,
        MetricsReviewOptions options,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _vmDiagnosticsService.ReviewVmMetricsAsync(
                options.Vm,
                options.ResourceGroup,
                options.Subscription!,
                options.Timespan,
                options.Tenant,
                cancellationToken);

            context.Response.Results = ResponseResult.Create(
                new MetricsReviewCommandResult(result),
                VmDiagnosticsJsonContext.Default.MetricsReviewCommandResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing VM Metrics Review command. VM: {Vm}, Subscription: {Subscription}", options.Vm, options.Subscription);
            HandleException(context, ex);
        }

        return context.Response;
    }

    public record MetricsReviewCommandResult(VmMetricsReviewResult Result);
}
