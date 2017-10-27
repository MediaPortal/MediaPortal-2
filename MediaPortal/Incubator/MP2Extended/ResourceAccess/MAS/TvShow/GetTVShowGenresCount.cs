using MediaPortal.Plugins.MP2Extended.Common;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.TvShow
{
  internal class GetTVShowGenresCount : GetTVShowGenres
  {
    public WebIntResult Process()
    {
      var output = Process(null, null);

      return new WebIntResult { Result = output.Count };
    }
  }
}
