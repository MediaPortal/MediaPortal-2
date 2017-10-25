using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Common;
using System.Linq;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.TvShow
{
  internal class GetTVShowGenresCount : GetTVShowGenres
  {
    public WebIntResult Process()
    {
      var output = Process(null, null).ToList();

      return new WebIntResult { Result = output.Count };
    }
  }
}
