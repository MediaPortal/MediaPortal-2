using Emulators.Common.FanartProvider;
using MediaPortal.Common;
using MediaPortal.Common.FanArt;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess;
using MediaPortal.Extensions.UserServices.FanArtService.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Emulators.Server
{
  public class GameFanartProvider : IFanArtProvider
  {
    public FanArtProviderSource Source
    {
      get { return FanArtProviderSource.Cache; }
    }

    public bool TryGetFanArt(string mediaType, string fanArtType, string name, int maxWidth, int maxHeight, bool singleRandom, out IList<IResourceLocator> result)
    {
      result = null;
      if (mediaType != GameFanartTypes.MEDIA_TYPE_GAME && mediaType != FanArtMediaTypes.Undefined && fanArtType == FanArtTypes.Thumbnail)
        return false;

      Guid mediaItemId;
      if (Guid.TryParse(name, out mediaItemId) == false)
        return false;

      IFanArtCache fanArtCache = ServiceRegistration.Get<IFanArtCache>();

      List<string> fanArtFiles = new List<string>();
      fanArtFiles.AddRange(fanArtCache.GetFanArtFiles(mediaItemId, fanArtType));

      // Try fallback
      if (fanArtFiles.Count == 0 && fanArtType == FanArtTypes.Thumbnail)
        fanArtFiles.AddRange(fanArtCache.GetFanArtFiles(mediaItemId, FanArtTypes.Poster));

      List<IResourceLocator> files = new List<IResourceLocator>();
      try
      {
        files.AddRange(fanArtFiles
          .Select(fileName => new ResourceLocator(ResourcePath.BuildBaseProviderPath(LocalFsResourceProviderBase.LOCAL_FS_RESOURCE_PROVIDER_ID, fileName)))
        );
        result = files;
        return result.Count > 0;
      }
      catch (Exception) { }
      return false;
    }
  }
}
