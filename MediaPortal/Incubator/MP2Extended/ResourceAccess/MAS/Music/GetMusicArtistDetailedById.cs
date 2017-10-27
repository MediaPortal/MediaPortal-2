using HttpServer.Exceptions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.MAS.Music;
using MP2Extended.ResourceAccess.MAS.Music.BaseClasses;
using System;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Music
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "id", Type = typeof(Guid), Nullable = false)]
  internal class GetMusicArtistDetailedById : BaseMusicArtistDetailed
  {
    public WebMusicArtistDetailed Process(Guid id)
    {
      MediaItem item = GetMediaItems.GetMediaItemById(id, BasicNecessaryMIATypeIds, BasicOptionalMIATypeIds);

      if (item == null)
        throw new BadRequestException($"No artist found with id {id}");

      return MusicArtistDetailed(item);
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
