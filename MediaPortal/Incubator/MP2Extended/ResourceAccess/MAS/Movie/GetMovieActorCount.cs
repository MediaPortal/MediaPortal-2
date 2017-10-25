using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Common;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Movie
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  internal class GetMovieActorCount : GetMovieActors
  {
    public WebIntResult Process(string filter)
    {
      var output = Process(filter, null, null);

      return new WebIntResult { Result = output.Count };
    }
  }
}
