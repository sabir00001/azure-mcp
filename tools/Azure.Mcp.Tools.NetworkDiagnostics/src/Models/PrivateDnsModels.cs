// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Mcp.Tools.NetworkDiagnostics.Models;

public record PrivateDnsRecordSummary(
    string Name,
    string RecordType,
    List<string> Records,
    long Ttl);

public record PrivateDnsZoneSummary(
    string ZoneName,
    string ResourceGroup,
    int LinkedVnetCount,
    int RecordSetCount,
    List<PrivateDnsRecordSummary> Records);

public record PrivateDnsValidationResult(
    string Subscription,
    List<PrivateDnsZoneSummary> PrivateDnsZones);
