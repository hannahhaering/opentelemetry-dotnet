// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Tests;
using Xunit;

namespace OpenTelemetry.Metrics.Tests;

public class MetricProducerTests
{
    [Fact]
    public void OnCollect_IncludesMetricsFromProducer()
    {
        using var reader = new TestMetricReader();
        var testProducer = new TestMetricProducer();
        reader.RegisterMetricProducer(testProducer);

        var success = reader.CollectForTest();

        Assert.True(success);
        Assert.NotNull(reader.LastProcessedBatch);
        var metrics = BatchToArray(reader.LastProcessedBatch.Value);

        Assert.Single(metrics);
        Assert.Equal("test_counter", metrics[0].Name);
    }

    [Fact]
    public void OnCollect_MergesMetricsFromMultipleProducers()
    {
        using var reader = new TestMetricReader();
        var producer1 = new TestMetricProducer("counter1");
        var producer2 = new TestMetricProducer("counter2");
        reader.AddMetricProducer(producer1);
        reader.AddMetricProducer(producer2);

        var success = reader.CollectForTest();

        Assert.True(success);
        Assert.NotNull(reader.LastProcessedBatch);
        var metrics = BatchToArray(reader.LastProcessedBatch.Value);

        Assert.Equal(2, metrics.Length);
        Assert.Contains(metrics, m => m.Name == "counter1");
        Assert.Contains(metrics, m => m.Name == "counter2");
    }

    [Fact]
    public void OnCollect_IncludesSdkMetrics()
    {
        using var reader = new TestMetricReader();
        using var meter = new Meter(Utils.GetCurrentMethodName());
        var counter = meter.CreateCounter<long>("sdk_counter");
        reader.AddMetricProducer(new TestMetricProducer());
        var metrics = new List<Metric>();

        using var meterProvider = Sdk.CreateMeterProviderBuilder()
            .AddMeter(Utils.GetCurrentMethodName())
            .AddReader(reader)
            .AddInMemoryExporter(metrics)
            .Build();
        counter.Add(1);

        meterProvider.ForceFlush();

        Assert.Equal(2, metrics.Count);
    }

    [Fact]
    public void OnCollect_HandlesProducerExceptionGracefully()
    {
        using var reader = new TestMetricReader();
        var faultyProducer = new FaultyMetricProducer();
        reader.AddMetricProducer(faultyProducer);

        var success = reader.CollectForTest();

        Assert.True(success);
        Assert.NotNull(reader.LastProcessedBatch);
        Assert.Equal(0, reader.LastProcessedBatch.Value.Count);
    }

    private static Metric[] BatchToArray(in Batch<Metric> batch)
    {
        var list = new List<Metric>((int)batch.Count);
        foreach (var m in batch)
        {
            list.Add(m);
        }

        return list.ToArray();
    }

    private class TestMetricProducer : IMetricProducer
    {
        private readonly string metricName;

        public TestMetricProducer(string metricName = "test_counter")
        {
            this.metricName = metricName;
        }

        public IReadOnlyCollection<Metric> Produce(Resource resource)
        {
            using var meter = new Meter(Utils.GetCurrentMethodName());

            var counter = meter.CreateCounter<long>(this.metricName);

            var metricStreamIdentity = new MetricStreamIdentity(counter, null);

            var metric = new Metric(metricStreamIdentity, AggregationTemporality.Cumulative, 100);

            return new[] { metric };
        }
    }

    private class FaultyMetricProducer : IMetricProducer
    {
        public IReadOnlyCollection<Metric> Produce(Resource resource)
        {
            throw new InvalidOperationException("Simulated failure in metric production.");
        }
    }

    private class TestMetricReader : MetricReader
    {
        internal TestMetricReader()
        {
        }

        public Batch<Metric>? LastProcessedBatch { get; private set; }

        public bool CollectForTest(int timeoutMilliseconds = Timeout.Infinite)
        {
            return this.OnCollect(timeoutMilliseconds);
        }

        internal override bool ProcessMetrics(in Batch<Metric> metrics, int timeoutMilliseconds)
        {
            this.LastProcessedBatch = metrics;
            return true;
        }

        internal void AddMetricProducer(IMetricProducer producer)
        {
            this.RegisterMetricProducer(producer);
        }
    }
}
