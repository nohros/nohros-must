using System;
using Nohros.Configuration;

namespace Nohros.Toolkit.RestQL
{
  public partial class QuerySettings
  {
    public class Loader : AbstractConfigurationLoader<QuerySettings>
    {
      /// <summary>
      /// 
      /// </summary>
      public const string kConfigFileName = Strings.kConfigFileName;

      /// <summary>
      /// 
      /// </summary>
      public const string kConfigRootNode = Strings.kConfigRootNode;
    }
  }
}