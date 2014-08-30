﻿using System;
using System.Collections.Generic;
using System.Linq;
using Nohros.Caching.Providers;

namespace Nohros.Metrics.Reporting
{
  public class SqlMetricsObserver : IMeasureObserver
  {
    const string kClassName = "Nohros.Metrics.SqlMetricsObserver";

    readonly IMetricsDao metrics_dao_;
    readonly ICacheProvider cache_;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlMetricsObserver"/>
    /// by using the given <see cref="IMetricsDao"/> and
    /// <see cref="ICacheProvider"/>.
    /// </summary>
    /// <param name="metrics_dao">
    /// A <see cref="IMetricsDao"/> object that can be used to access a sql
    /// database.
    /// </param>
    /// <param name="cache">
    /// A <see cref="ICacheProvider"/> object that can be used to cache
    /// objects.
    /// </param>
    public SqlMetricsObserver(IMetricsDao metrics_dao, ICacheProvider cache) {
      metrics_dao_ = metrics_dao;
      cache_ = cache;
    }

    /// <inheritdoc/>
    public void Observe(Measure measure, DateTime timestamp) {
      Tags tags = measure.MetricConfig.Tags;
      long tags_id = GetTagsId(tags);
      metrics_dao_.RegisterMeasure(tags_id, measure.Value, timestamp);
    }

    long GetTagsId(Tags tags) {
      long tags_id;
      string tags_cache_key = CacheKey(tags);
      if (!cache_.Get(tags_cache_key, out tags_id)) {
        tags_id = TagsIdFromDatabase(tags);
        cache_.Set(tags_cache_key, tags_id);
      }
      return tags_id;
    }

    /// <summary>
    /// Get the id of the <paramref name="tags"/> from the database or
    /// create a new id for the <paramref name="tags"/> if an id is not found.
    /// </summary>
    /// <param name="tags">
    /// The tags to get the id.
    /// </param>
    /// <returns>
    /// A number that uniquely identifies the tags within the metrics database.
    /// </returns>
    long TagsIdFromDatabase(Tags tags) {
      int hash = Hash(tags);
      IEnumerable<long> ids = metrics_dao_.GetTagsIds(hash, tags.Count);

      // The |GetTagsIds| return all the ids that has the same hash and
      // number of tags of the given |tags|. We need to find the tags group
      // that contains the same tags as |tags|.
      foreach (var id in ids) {
        if (IsSameTags(id, tags)) {
          return id;
        }
      }

      // A matching tags was not found, lets create a new one.
      long tags_id = metrics_dao_.RegisterTags(hash, tags.Count);
      foreach (Tag tag in tags) {
        metrics_dao_.RegisterTag(tag.Name, tag.Value, tags_id);
      }
      return tags_id;
    }

    bool IsSameTags(long tags_id, Tags tags) {
      foreach (var tag in tags) {
        if (!metrics_dao_.ContainsTag(tag.Name, tag.Value, tags_id)) {
          return false;
        }
      }
      return false;
    }

    string CacheKey(Tags tags) {
      return kClassName + "::tags::" + tags.Id.ToString("N");
    }

    int Hash(Tags tags) {
      var list = tags.ToList();
      list.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
      unchecked {
        int hash = 17;
        foreach (var tag in tags) {
          hash = hash*31 + tag.GetHashCode();
        }
        return hash;
      }
    }
  }
}