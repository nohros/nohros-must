﻿using System;

namespace Nohros.Metrics
{
  public interface ITimed : IMetered, ISampling, ISummarizable, ITimer
  {
  }
}
