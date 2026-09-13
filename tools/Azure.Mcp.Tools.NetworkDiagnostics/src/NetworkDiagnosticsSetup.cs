// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Mcp.Tools.NetworkDiagnostics.Commands.Nsg;
using Azure.Mcp.Tools.NetworkDiagnostics.Commands.Peering;
using Azure.Mcp.Tools.NetworkDiagnostics.Commands.PrivateDns;
using Azure.Mcp.Tools.NetworkDiagnostics.Commands.Topology;
using Azure.Mcp.Tools.NetworkDiagnostics.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Mcp.Core.Areas;
using Microsoft.Mcp.Core.Commands;

namespace Azure.Mcp.Tools.NetworkDiagnostics;

public class NetworkDiagnosticsSetup : IAreaSetup
{
    public string Name => "networkdiagnostics";

    public string Title => "Azure Network Diagnostics";

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<INetworkDiagnosticsService, NetworkDiagnosticsService>();
        services.AddSingleton<TopologySummaryCommand>();
        services.AddSingleton<NsgValidateCommand>();
        services.AddSingleton<PrivateDnsValidateCommand>();
        services.AddSingleton<PeeringCheckCommand>();
    }

    public CommandGroup RegisterCommands(IServiceProvider serviceProvider)
    {
        var networkDiagnostics = new CommandGroup(Name,
            "Network Diagnostics operations - Commands to diagnose Azure network topology, NSG rules, Private DNS zones, and VNet peerings.", Title);

        var topology = new CommandGroup("topology", "Network topology commands.");
        var nsg = new CommandGroup("nsg", "NSG rule validation commands.");
        var privateDns = new CommandGroup("privatedns", "Private DNS validation commands.");
        var peering = new CommandGroup("peering", "VNet peering check commands.");

        networkDiagnostics.AddSubGroup(topology);
        networkDiagnostics.AddSubGroup(nsg);
        networkDiagnostics.AddSubGroup(privateDns);
        networkDiagnostics.AddSubGroup(peering);

        topology.AddCommand<TopologySummaryCommand>(serviceProvider);
        nsg.AddCommand<NsgValidateCommand>(serviceProvider);
        privateDns.AddCommand<PrivateDnsValidateCommand>(serviceProvider);
        peering.AddCommand<PeeringCheckCommand>(serviceProvider);

        return networkDiagnostics;
    }
}
