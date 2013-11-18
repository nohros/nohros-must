﻿using System;
using System.Collections.Generic;

namespace Nohros.Metrics.Reporting
{
  /// <summary>
  /// A simple reporter which prints out application metrics to the
  /// <see cref="System.Console"/> periodically.
  /// </summary>
  public class ConsoleReporter : AbstractPollingReporter
  {
    #region .ctor
    public ConsoleReporter(IMetricsRegistry registry) : base(registry) {
    }
    #endregion

    /// <inheritdoc/>
    public override void Run(MetricPredicate predicate) {
      var registry = MetricsRegistry;
      var now = DateTime.UtcNow;
      registry.Report(Report, now, predicate);
    }

    /// <inheritdoc/>
    public override void Run() {
      var registry = MetricsRegistry;
      var now = DateTime.UtcNow;
      registry.Report(Report, now);
    }

    void Report(KeyValuePair<string, MetricValue[]> metrics,
      DateTime timestamp) {
      string name = metrics.Key;
      foreach (MetricValue metric in metrics.Value) {
        Console.Write(timestamp.ToString("yyyy-MM-ddTHH:mm:ssZ") + ":"
          + name + ".");
        Console.Write(metric.Name + "=" + metric.Value.ToString("f4"));
        Console.WriteLine(" " + metric.Unit);
      }
    }
  }
}
