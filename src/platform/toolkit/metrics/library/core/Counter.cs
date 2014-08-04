﻿using System;
using Nohros.Concurrent;

namespace Nohros.Metrics
{
  /// <summary>
  /// An incrementing and decrementing counter metric.
  /// </summary>
  public class Counter : IMetric, ICounter
  {
    readonly Mailbox<Action> mailbox_;
    long count_;

    /// <summary>
    /// Initializes a new instance of the <see cref="Counter"/> class that
    /// uses the specified executor to perform the counter updates (
    /// increment/decrement).
    /// </summary>
    public Counter() {
      count_ = 0;
      mailbox_ = new Mailbox<Action>(runnable => runnable());
    }

#if DEBUG
    public void Run(Action action) {
      mailbox_.Send(action);
    }
#endif

    /// <summary>
    /// Increment the counter by one.
    /// </summary>
    public void Increment() {
      Increment(1);
    }

    /// <summary>
    /// Increments the counter by <paramref name="n"/>.
    /// </summary>
    /// <param name="n">The amount by which the counter will be increased.
    /// </param>
    public void Increment(long n) {
      mailbox_.Send(() => Update(n));
    }

    /// <summary>
    /// Decrements the counter by one.
    /// </summary>
    public void Decrement() {
      Decrement(1);
    }

    /// <summary>
    /// Decrements the counter by <paramref name="n"/>
    /// </summary>
    /// <param name="n">The amount by which the counter will be increased.
    /// </param>
    public void Decrement(long n) {
      mailbox_.Send(() => Update(-n));
    }

    void Update(long delta) {
      count_ += delta;
    }

    public void GetCount(LongMetricCallback callback) {
      DateTime now = DateTime.Now;
      mailbox_.Send(() => callback(count_, now));
    }

    public void Increment(CountedCallback callback) {
      Increment(1, callback);
    }

    public void Increment(long n, CountedCallback callback) {
      mailbox_.Send(() => {
        Update(n);
        callback(this);
      });
    }

    public void Decrement(CountedCallback callback) {
      Decrement(1, callback);
    }

    public void Decrement(long n, CountedCallback callback) {
      mailbox_.Send(() => {
        Update(-n);
        callback(this);
      });
    }

    /// <inheritdoc/>
    public void Report<T>(MetricReportCallback<T> callback, T context) {
      callback(new MetricValueSet(this, Report()), context);
    }

    MetricValue[] Report() {
      return new[] {
        new MetricValue(MetricValueType.Count, count_),
      };
    }
  }
}
