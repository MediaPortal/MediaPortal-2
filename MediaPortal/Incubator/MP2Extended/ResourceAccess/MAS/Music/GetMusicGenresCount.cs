using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.Extensions;
using System.Linq;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Music
{
  internal class GetMusicGenresCount : GetMusicGenres
  {
    public WebIntResult Process(string filter)
    {
      var output = Process(null, null)
        .Filter(filter);

      return new WebIntResult() { Result = output.Count() };
    }
  }
}
