// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.Resources;

namespace OpenTelemetry.Metrics;

public interface IMetricProducer
{
    IReadOnlyCollection<MetricPoint> Produce(Resource resource);
}
