using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Common;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Music
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "filter", Type = typeof(string), Nullable = true)]
  internal class GetMusicArtistCount : GetMusicArtistsBasic
  {
    public WebIntResult Process(string filter)
    {
      var output = Process(filter, null, null);

      return new WebIntResult { Result = output.Count};
    }
  }
}
