// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Mcp.Tools.VmDiagnostics.Models;

public record MetricValuePoint(
    DateTimeOffset TimeStamp,
    double? Average,
    double? Maximum);

public record VmMetricSeries(
    string MetricName,
    string Unit,
    List<MetricValuePoint> Points);

public record VmMetricsReviewResult(
    string VmName,
    string ResourceGroup,
    string Timespan,
    List<VmMetricSeries> Metrics);
