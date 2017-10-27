using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.Extensions;
using MediaPortal.Plugins.MP2Extended.MAS.Music;
using System.Collections.Generic;
using System.Linq;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Music
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "start", Type = typeof(int), Nullable = false)]
  [ApiFunctionParam(Name = "end", Type = typeof(int), Nullable = false)]
  [ApiFunctionParam(Name = "filter", Type = typeof(string), Nullable = true)]
  [ApiFunctionParam(Name = "sort", Type = typeof(WebSortField), Nullable = true)]
  [ApiFunctionParam(Name = "order", Type = typeof(WebSortOrder), Nullable = true)]
  internal class GetMusicArtistsDetailedByRange : GetMusicArtistsDetailed
  {
    public IList<WebMusicArtistDetailed> Process(int start, int end, string filter, WebSortField? sort, WebSortOrder? order)
    {
      var output = Process(filter, sort, order);

      output = output.TakeRange(start, end).ToList();

      return output;
    }
  }
}
