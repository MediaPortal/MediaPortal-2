using System;
using System.Collections.Generic;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.Extensions;
using MediaPortal.Plugins.MP2Extended.MAS.TvShow;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.TvShow.BaseClasses;
using Newtonsoft.Json;
using System.Linq;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.TvShow
{
  internal class GetTVEpisodesBasicByRange : BaseEpisodeBasic
  {
    public IList<WebTVEpisodeBasic> Process(int start, int end, WebSortField? sort, WebSortOrder? order)
    {
      var output = new GetTVEpisodesBasic().Process(sort, order);

      // get range
      output = output.TakeRange(start, end).ToList();
      return output;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}