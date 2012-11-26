﻿using System;
using System.Collections.Generic;

namespace Nohros.Metrics
{
  /// <summary>
  /// A central registry and factory for metric instances.
  /// </summary>
  public class MetricsRegistry : AbstractMetricsRegistry, IMetricsRegistry
  {
    readonly Clock clock_;

    #region .ctor
    /// <summary>
    /// Initializes a new instance of the <see cref="MetricsRegistry"/> that
    /// uses the default clock to mark the passage of time.
    /// </summary>
    public MetricsRegistry() : this(new UserTimeClock()) {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MetricsRegistry"/> that
    /// uses the given clock to mark the passage of time.
    /// </summary>
    /// <param name="clock">
    /// The <see cref="Clock"/> used to mark the passage of time.
    /// </param>
    public MetricsRegistry(Clock clock) {
      clock_ = clock;
    }
    #endregion

    /// <summary>
    /// Gets the counter that is associated with the specified
    /// <see cref="MetricName"/> or create a new one if no association exists.
    /// </summary>
    /// <param name="name">
    /// The name of the metric.
    /// </param>
    /// <param name="biased">
    /// Whether or not the histogram should be biased.
    /// </param>
    /// <returns>
    /// A <see cref="IHistogram"/> that could be identified by the
    /// specified <see cref="MetricName"/>.
    /// </returns>
    public IHistogram GetHistogram(MetricName name, bool biased) {
      IHistogram histogram;
      if (!TryGetMetric(name, out histogram)) {
        histogram = (biased)
          ? (IHistogram) Histograms.Biased()
          : (IHistogram) Histograms.Uniform();
        Add(name, histogram);
      }
      return histogram;
    }

    /// <summary>
    /// Gets the meter that is associaed with the specified
    /// <see cref="MetricName"/> or create a new one if no association exists.
    /// </summary>
    /// <param name="name">
    /// The name of the metric.
    /// </param>
    /// <param name="rate_unit">
    /// The rate unit of the new meter.
    /// </param>
    /// <param name="event_type">
    /// The plural name of the event meter is measuring
    /// <example>
    /// <code>
    /// "requests"
    /// </code>
    /// </example>
    /// </param>
    /// <returns></returns>
    public IMetered GetMeter(MetricName name, string event_type,
      TimeUnit rate_unit) {
      IMetered meter;
      if (!TryGetMetric(name, out meter)) {
        meter = new Meter(event_type, rate_unit);
        Add(name, meter);
      }
      return meter;
    }

    /// <summary>
    /// Gets the timer that is associated with the specified
    /// <see cref="MetricName"/> or create a new one if no association exists.
    /// </summary>
    /// <param name="name">
    /// The name of the metric.
    /// </param>
    /// <param name="duration_unit">
    /// The duration unit of the new timer.
    /// </param>
    /// <returns>
    /// The timer associated with the key <paramref name="name"/>.
    /// </returns>
    public Timer GetTimer(MetricName name, TimeUnit duration_unit) {
      Timer timer;
      if (!TryGetMetric(name, out timer)) {
        timer = new Timer(duration_unit, new Meter("calls", TimeUnit.Seconds),
          Histograms.Biased(), clock_);
        Add(name, timer);
      }
      return timer;
    }

    /// <summary>
    /// Gets the value associated with the specified metric name.
    /// </summary>
    /// <param name="name">
    /// The name of the metric to get.
    /// </param>
    /// <param name="histogram">
    /// When this method returns, contains the value associated with the
    /// specified metric name, if a metric name is found; otherwise, the
    /// <c>null</c>.
    /// </param>
    /// <returns>
    /// <c>true</c> if a <see cref="IHistogram"/> associated with the
    /// <paramref name="name"/> exists; otherwise, <c>false</c>.
    /// </returns>
    public bool TryGetHistogram(MetricName name, out IHistogram histogram) {
      return TryGetMetric(name, out histogram);
    }

    /// <param name="name">
    /// The name of the metric to get.
    /// </param>
    /// <param name="timer">
    /// When this method returns, contains the timer associated with the
    /// specified metric name, if a metric name is found; otherwise, the
    /// <c>null</c>.
    /// </param>
    /// <returns>
    /// <c>true</c> if a <see cref="Counter"/> associated with the
    /// <paramref name="name"/> exists; otherwise, <c>false</c>.
    /// </returns>
    public bool TryGetTimer(MetricName name, out Timer timer) {
      return TryGetMetric(name, out timer);
    }

    /// <param name="name">
    /// The name of the metric to get.
    /// </param>
    /// <param name="meter">
    /// When this method returns, contains the meter associated with the
    /// specified metric name, if a metric name is found; otherwise, the
    /// <c>null</c>.
    /// </param>
    /// <returns>
    /// <c>true</c> if a <see cref="IMetered"/> associated with the
    /// <paramref name="name"/> exists; otherwise, <c>false</c>.
    /// </returns>
    public bool TryGetMeter(MetricName name, out IMeter meter) {
      return TryGetMetric(name, out meter);
    }

    public IHistogram GetHistogram(Type klass, string name, bool biased) {
      return GetHistogram(new MetricName(klass, name), biased);
    }

    public IHistogram GetHistogram(Type klass, string name, string scope,
      bool biased) {
      return GetHistogram(new MetricName(klass, name, scope), biased);
    }

    public IHistogram GetHistogram(string group, string type, string name,
      bool biased) {
      return GetHistogram(new MetricName(group, type, name), biased);
    }

    public IHistogram GetHistogram(string group, string type, string name,
      string scope, bool biased) {
      return GetHistogram(new MetricName(group, type, name, scope), biased);
    }

    public IMetered GetMeter(Type klass, string name, string event_type,
      TimeUnit rate_unit) {
      return GetMeter(new MetricName(klass, name), event_type, rate_unit);
    }

    public IMetered GetMeter(Type klass, string name, string scope,
      string event_type,
      TimeUnit rate_unit) {
      return GetMeter(new MetricName(klass, name, scope), event_type, rate_unit);
    }

    public IMetered GetMeter(string group, string type, string name,
      string event_type,
      TimeUnit rate_unit) {
      return GetMeter(new MetricName(group, type, name), event_type, rate_unit);
    }

    public IMetered GetMeter(string group, string type, string name,
      string scope, string event_type,
      TimeUnit rate_unit) {
      return GetMeter(new MetricName(group, type, name, scope), event_type,
        rate_unit);
    }

    public Timer GetTimer(Type klass, string name, TimeUnit duration_unit) {
      return GetTimer(new MetricName(klass, name), duration_unit);
    }

    public Timer GetTimer(Type klass, string name, string scope,
      TimeUnit duration_unit) {
      return GetTimer(new MetricName(klass, name, scope), duration_unit);
    }

    public Timer GetTimer(string group, string type, string name,
      TimeUnit duration_unit) {
      return GetTimer(new MetricName(group, type, name), duration_unit);
    }

    public Timer GetTimer(string group, string type, string name, string scope,
      TimeUnit duration_unit) {
      return GetTimer(new MetricName(group, type, name, scope), duration_unit);
    }

    public bool TryGetHistogram(Type klass, string name,
      out IHistogram histogram) {
      return TryGetHistogram(new MetricName(klass, name), out histogram);
    }

    public bool TryGetHistogram(Type klass, string name, string scope,
      out IHistogram histogram) {
      return TryGetHistogram(new MetricName(klass, name, scope), out histogram);
    }

    public bool TryGetHistogram(string group, string type, string name,
      out IHistogram histogram) {
      return TryGetHistogram(new MetricName(group, type, name), out histogram);
    }

    public bool TryGetHistogram(string group, string type, string name,
      string scope, out IHistogram histogram) {
      return TryGetHistogram(new MetricName(group, type, name, scope),
        out histogram);
    }

    public bool TryGetTimer(Type klass, string name, out Timer timer) {
      return TryGetTimer(new MetricName(klass, name), out timer);
    }

    public bool TryGetTimer(Type klass, string name, string scope,
      out Timer timer) {
      return TryGetTimer(new MetricName(klass, name, scope), out timer);
    }

    public bool TryGetTimer(string group, string type, string name,
      out Timer timer) {
      return TryGetTimer(new MetricName(group, type, name), out timer);
    }

    public bool TryGetTimer(string group, string type, string name, string scope,
      out Timer timer) {
      return TryGetTimer(new MetricName(group, type, name, scope), out timer);
    }

    public bool TryGetMeter(Type klass, string name, out IMeter timer) {
      return TryGetMeter(new MetricName(klass, name), out timer);
    }

    public bool TryGetMeter(Type klass, string name, string scope,
      out IMeter timer) {
      return TryGetMeter(new MetricName(klass, name, scope), out timer);
    }

    public bool TryGetMeter(string group, string type, string name,
      out IMeter timer) {
      return TryGetMeter(new MetricName(group, type, name), out timer);
    }

    public bool TryGetMeter(string group, string type, string name, string scope,
      out IMeter timer) {
      return TryGetMeter(new MetricName(group, type, name, scope), out timer);
    }
  }
}