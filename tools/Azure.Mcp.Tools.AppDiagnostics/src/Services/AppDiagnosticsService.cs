// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Mcp.Core.Services.Azure;
using Azure.Mcp.Tools.AppDiagnostics.Models;
using Azure.ResourceManager;
using Azure.ResourceManager.AppService;

namespace Azure.Mcp.Tools.AppDiagnostics.Services;

public class AppDiagnosticsService(IAzureService azureService)
    : BaseAzureResourceService(azureService), IAppDiagnosticsService
{
    public async Task<AppHealthCheckResult> CheckAppHealthAsync(
        string appName,
        string resourceGroup,
        string subscription,
        string? tenant = null,
        CancellationToken cancellationToken = default)
    {
        ValidateRequiredParameters(
            (nameof(appName), appName),
            (nameof(resourceGroup), resourceGroup),
            (nameof(subscription), subscription));

        var subResource = await AzureService.GetSubscription(subscription, tenant, cancellationToken: cancellationToken);
        var rg = await subResource.GetResourceGroupAsync(resourceGroup, cancellationToken);
        var webAppResponse = await rg.Value.GetWebSiteAsync(appName, cancellationToken: cancellationToken);
        var webApp = webAppResponse.Value;

        string state = webApp.Data.State ?? "Unknown";
        string availabilityState = webApp.Data.AvailabilityState?.ToString() ?? "Unknown";
        bool httpsOnly = webApp.Data.IsHttpsOnly ?? false;
        bool vnetIntegrated = !string.IsNullOrEmpty(webApp.Data.VirtualNetworkSubnetId?.ToString());

        return new AppHealthCheckResult(
            appName,
            resourceGroup,
            webApp.Data.Kind ?? "WebApp",
            state,
            availabilityState,
            httpsOnly,
            vnetIntegrated,
            $"App '{appName}' is currently in state '{state}' with AvailabilityState '{availabilityState}'.");
    }

    public Task<AppDeploymentTriageResult> TriageDeploymentAsync(
        string appName,
        string resourceGroup,
        string subscription,
        string? tenant = null,
        CancellationToken cancellationToken = default)
    {
        ValidateRequiredParameters(
            (nameof(appName), appName),
            (nameof(resourceGroup), resourceGroup),
            (nameof(subscription), subscription));

        return Task.FromException<AppDeploymentTriageResult>(
            new NotImplementedException("App deployment triage is not implemented yet. Wire this method to the App Service deployment/Kudu API before enabling it."));
    }

    public Task<AppLogsAnalysisResult> AnalyzeAppLogsAsync(
        string appName,
        string resourceGroup,
        string subscription,
        string timeWindow = "PT2H",
        string? tenant = null,
        CancellationToken cancellationToken = default)
    {
        ValidateRequiredParameters(
            (nameof(appName), appName),
            (nameof(resourceGroup), resourceGroup),
            (nameof(subscription), subscription));

        return Task.FromException<AppLogsAnalysisResult>(
            new NotImplementedException("App log analysis is not implemented yet. Wire this method to Azure Monitor or Application Insights logs before enabling it."));
    }
}
