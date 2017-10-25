using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.MP2Extended.MAS.Movie;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Movie.BaseClasses;
using System;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Movie
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "id", Type = typeof(string), Nullable = false)]
  internal class GetMovieDetailedById : BaseMovieDetailed
  {
    public WebMovieDetailed Process(Guid id)
    {
      MediaItem item = GetMediaItems.GetMediaItemById(id, BasicNecessaryMIATypeIds, BasicOptionalMIATypeIds);

      if (item == null)
        throw new BadRequestException(String.Format("GetMovieDetailedById: No MediaItem found with id: {0}", id));

      return MovieDetailed(item);
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
