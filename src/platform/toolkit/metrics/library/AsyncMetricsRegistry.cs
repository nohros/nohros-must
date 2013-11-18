﻿using System;
using Nohros.Concurrent;

namespace Nohros.Metrics
{
  public class AsyncMetricsRegistry : AbstractMetricsRegistry,
                                      IMetricsRegistry
  {
    readonly IExecutor executor_;

    #region .ctor
    public AsyncMetricsRegistry(IExecutor executor) {
      executor_ = executor;
    }
    #endregion

    public IHistogram GetHistogram(string name, bool biased) {
      IAsyncHistogram histogram;
      if (!TryGetMetric(name, out histogram)) {
        histogram = (biased)
          ? (IAsyncHistogram) Histograms.Biased(executor_)
          : (IAsyncHistogram) Histograms.Uniform(executor_);
        Add(name, histogram);
      }
      return histogram;
    }

    public IMeter GetMeter(string name, string event_type,
      TimeUnit rate_unit) {
      IAsyncMeter meter;
      if (!TryGetMetric(name, out meter)) {
        meter = new AsyncMeter(event_type, rate_unit, executor_);
        Add(name, meter);
      }
      return meter;
    }

    public ITimer GetTimer(string name, TimeUnit duration_unit) {
      AsyncTimer timer;
      if (!TryGetMetric(name, out timer)) {
        timer = new AsyncTimer(duration_unit,
          new Meter("calls", TimeUnit.Seconds),
          Histograms.Biased(), executor_);
        Add(name, timer);
      }
      return timer;
    }

    public bool TryGetHistogram(string name, out IHistogram histogram) {
      return TryGetMetric(name, out histogram);
    }

    public bool TryGetTimer(string name, out ITimer timer) {
      return TryGetMetric(name, out timer);
    }

    public bool TryGetMeter(string name, out IMeter meter) {
      return TryGetMetric(name, out meter);
    }
  }
}
