﻿using System;
using System.Collections.Generic;
using Nohros.Collections;
using Nohros.Concurrent;

namespace Nohros.Metrics
{
  /// <summary>
  /// An exponentially-decaying random sample of longs. Uses Cormode et al's
  /// forward-decaying priority resevoir sampling method to produce a
  /// statistically representative sample, exponentially biased towards newer
  /// entries.
  /// </summary>
  /// <remarks>
  /// Cormode et al. Forward Decay: A Practical Time Decay Model for
  /// Streaming Systems. ICDE '09: Proceedings of the 2009 IEEE International
  /// Conference on Data Engineering (2009).
  /// http://www.research.att.com/people/Cormode_Graham/library/publications/CormodeShkapenyukSrivastavaXu09.pdf
  /// </remarks>
  public class ExponentiallyDecayingSample : ISample
  {
    const long kRescaleThreshold = 3600000000000;
    readonly double alpha_;
    readonly Clock clock_;
    readonly AndersonTree<double, int> priorities_;
    readonly Random rand_;
    readonly int reservoir_upper_limit_;
    readonly long[] resevoir_;
    readonly int resevoir_size_;
    int count_;
    long next_scale_time_;
    long start_time_;

    #region .ctor
    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="ExponentiallyDecayingSample"/> class by using the specified
    /// resevoir size and exponential decay factor.
    /// </summary>
    /// <param name="resevoir_size">
    /// The number of samples to keep in the sampling resevoir.
    /// </param>
    /// <param name="alpha">
    /// The exponential decay factor; the higher this is, the more biased the
    /// sample will be towards newer values.
    /// </param>
    /// <remarks>
    /// The use of the executor returned by the method
    /// <see cref="Executors.SameThreadExecutor"/> is not encouraged, because
    /// the executor does not returns until the execution list is empty and,
    /// this can cause significant pauses in the thread that is executing the
    /// sample update.
    /// </remarks>
    public ExponentiallyDecayingSample(int resevoir_size, double alpha)
      : this(resevoir_size, alpha, new UserTimeClock()) {
    }

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="ExponentiallyDecayingSample"/> class by using the specified
    /// resevoir size and exponential decay factor.
    /// </summary>
    /// <param name="resevoir_size">
    /// The number of samples to keep in the sampling resevoir.
    /// </param>
    /// <param name="alpha">
    /// The exponential decay factor; the higher this is, the more biased the
    /// sample will be towards newer values.
    /// </param>
    /// <param name="clock">
    /// A <see cref="Clock"/> that can be used to mark the passage of time.
    /// </param>
    /// <remarks>
    /// The use of the executor returned by the method
    /// <see cref="Executors.SameThreadExecutor"/> is not encouraged, because
    /// the executor does not returns until the execution list is empty and,
    /// this can cause significant pauses in the thread that is executing the
    /// sample update.
    /// </remarks>
    public ExponentiallyDecayingSample(int resevoir_size, double alpha,
      Clock clock) {
      count_ = 0;
      clock_ = clock;
      rand_ = new Random();
      next_scale_time_ = 0;
      alpha_ = alpha;
      resevoir_size_ = resevoir_size;
      reservoir_upper_limit_ = resevoir_size - 1;
      priorities_ = new AndersonTree<double, int>();
      resevoir_ = new long[resevoir_size];
      start_time_ = CurrentTimeInSeconds;
      next_scale_time_ = clock_.Tick + kRescaleThreshold;
    }
    #endregion

    /// <inheritdoc/>
    public void Update(long value) {
      Update(value, CurrentTimeInSeconds);
    }

    public Snapshot Snapshot {
      get {
        int size = Size;
        var resevoir = new long[size];
        Array.Copy(resevoir_, resevoir, size);
        return new Snapshot(resevoir);
      }
    }

    /// <inheritdoc/>
    public int Size {
      get { return Math.Min(resevoir_size_, count_); }
    }

    /// <summary>
    /// Adds an old value with fixed timestamp to the sample.
    /// </summary>
    /// <param name="value">The value to be added.</param>
    /// <param name="timestamp">The epoch timestamp of <paramref name="value"/>
    /// in seconds.</param>
    public void Update(long value, long timestamp) {
      double priority = Priority(timestamp);

      // Fills the resevoir with the first "m" values and keep elements
      // with the greatest priorities in the resvoir.
      if (count_ <= reservoir_upper_limit_) {
        priorities_[priority] = count_;
        resevoir_[count_++] = value;
      } else {
        KeyValuePair<double, int> first = priorities_.First;
        if (first.Key < priority) {
          // replace the element associated with the smallest key by the
          // sampled value.
          priorities_.Remove(first.Key);
          priorities_[priority] = first.Value;
          resevoir_[first.Value] = value;
        }
      }

      // If the current landmark becomes old rescale the sample values
      // using a new landmark.
      long now = clock_.Tick;
      if (now >= next_scale_time_) {
        Rescale(now);
      }
    }

    double Priority(long timestamp) {
      return Weight(timestamp - start_time_)/rand_.NextDouble();
    }

    /// <summary>
    /// Rescale the sample to a new landmark.
    /// </summary>
    /// <param name="now">
    /// The current time in seconds.
    /// </param>
    /// <remarks>
    /// A common feature if the above thechniques-indeed, the key technique
    /// that allows us to track the decayed weights efficiently-is that they
    /// maintain counts and other quantities based on g(ti - L), and only scale
    /// by g(t - L) at query time. But while g(ti -L)/g(t - L) is guaranteed to
    /// lie between zero and one, the intermediate values of g(ti - L) could
    /// become very large. For polynomial functions, these values should not
    /// grow too large, and should be effectively represented in practice
    /// by floating point types. For exponential functions, these values could
    /// grow quite large as new values of (ti - L) become large, and potentially
    /// exceed the capacity of common floating point types. However, since the
    /// values stored by the algorithms are linear combinations of g values(
    /// scaled sums), they can be rescaled relative to a new landmark. That is,
    /// by the analysis of exponential decay in Section III-A, the choice of L
    /// does not affect the final result. We can therefore multiply each value
    /// based on L by the factor of exp(-a(L' - L)), and obtain the correct
    /// value as if we had instead computed relative to a new landmark L' (and
    /// then use this new L' ar query time). This can be done with a linear
    /// pass over whatever data struture is beign used.
    /// </remarks>
    void Rescale(long now) {
      next_scale_time_ = now + kRescaleThreshold;
      long old_start_time = start_time_;
      start_time_ = CurrentTimeInSeconds;

      KeyValuePair<double, int>[] priorities = priorities_.ToArray();
      for (int i = 0, j = priorities.Length; i < j; i++) {
        KeyValuePair<double, int> priority = priorities[i];
        priorities_.Remove(priority.Key);
        double new_priority = priority.Key*
          Math.Exp(-alpha_*(start_time_ - old_start_time));
        priorities_.Add(new_priority, priority.Value);
      }
    }

    double Weight(long t) {
      return Math.Exp(alpha_*t);
    }

    public long CurrentTimeInSeconds {
      get { return TimeUnitHelper.ToSeconds(clock_.Time, TimeUnit.Milliseconds); }
    }
  }
}