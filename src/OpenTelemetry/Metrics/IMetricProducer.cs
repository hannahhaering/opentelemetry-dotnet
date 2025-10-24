// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.Resources;

namespace OpenTelemetry.Metrics;

/// <summary>
/// Defines a contract for producing metrics.
/// </summary>
public interface IMetricProducer
{
    /// <summary>
    /// Produces metrics for the given resource.
    /// </summary>
    /// <param name="resource">The resource for which to produce metrics.</param>
    /// <returns>A collection of produced metrics.</returns>
    IReadOnlyCollection<Metric> Produce(Resource resource);
}
