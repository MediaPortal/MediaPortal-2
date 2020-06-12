using Emulators.Common.FanartProvider;
using Emulators.Common.Games;
using Emulators.Common.Matchers;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.FanArt;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess;
using MediaPortal.Extensions.UserServices.FanArtService.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.Server
{
  public class GameFanartProvider : IFanArtProvider
  {
    protected static readonly Guid[] NECESSARY_MIAS = { ProviderResourceAspect.ASPECT_ID, GameAspect.ASPECT_ID };
    protected static readonly string[] IMAGE_PATTERNS = { "*.jpg", "*.png" };

    public FanArtProviderSource Source
    {
      get { return FanArtProviderSource.File; }
    }

    public bool TryGetFanArt(string mediaType, string fanArtType, string name, int maxWidth, int maxHeight, bool singleRandom, out IList<IResourceLocator> result)
    {
      result = null;
      if (mediaType != GameFanartTypes.MEDIA_TYPE_GAME && mediaType != FanArtMediaTypes.Undefined && fanArtType == FanArtTypes.Thumbnail)
        return false;
      string path;
      if (!TryGetImagePath(name, fanArtType, out path))
        return false;

      List<IResourceLocator> files = new List<IResourceLocator>();
      try
      {
        DirectoryInfo directoryInfo = new DirectoryInfo(path);
        if (directoryInfo.Exists)
        {
          foreach (string pattern in IMAGE_PATTERNS)
          {
            files.AddRange(directoryInfo.GetFiles(pattern)
              .Select(f => f.FullName)
              .Select(fileName => new ResourceLocator(ResourcePath.BuildBaseProviderPath(LocalFsResourceProviderBase.LOCAL_FS_RESOURCE_PROVIDER_ID, fileName)))
              );
            result = files;
            if (result.Count > 0)
              return true;
          }
        }
      }
      catch (Exception) { }
      return false;
    }

    protected bool TryGetImagePath(string name, string fanartType, out string path)
    {
      path = null;
      MediaItem mediaItem;
      if (!TryGetMediaItem(name, out mediaItem))
        return false;

      Guid matcherId;
      string onlineId;
      if (!MediaItemAspect.TryGetAttribute(mediaItem.Aspects, GameAspect.ATTR_MATCHER_ID, out matcherId) ||
        !MediaItemAspect.TryGetAttribute(mediaItem.Aspects, GameAspect.ATTR_ONLINE_ID, out onlineId))
        return false;

      ImageType imageType;
      switch (fanartType)
      {
        case FanArtTypes.Poster:
        case FanArtTypes.Thumbnail:
          imageType = ImageType.FrontCover;
          break;
        case FanArtTypes.FanArt:
          imageType = ImageType.Fanart;
          break;
        case FanArtTypes.Banner:
          imageType = ImageType.Banner;
          break;
        case FanArtTypes.ClearArt:
          imageType = ImageType.ClearLogo;
          break;
        default:
          return false;
      }
      return GameMatcher.Instance.TryGetImagePath(matcherId, onlineId, imageType, out path);
    }

    protected bool TryGetMediaItem(string name, out MediaItem mediaItem)
    {
      mediaItem = null;
      Guid mediaItemId;
      if (!Guid.TryParse(name, out mediaItemId) || mediaItemId == Guid.Empty)
        return false;
      IMediaLibrary mediaLibrary = ServiceRegistration.Get<IMediaLibrary>(false);
      if (mediaLibrary == null)
        return false;
      IFilter filter = new MediaItemIdFilter(mediaItemId);
      IList<MediaItem> items = mediaLibrary.Search(new MediaItemQuery(NECESSARY_MIAS, filter), false, null, true);
      if (items == null || items.Count == 0)
        return false;
      mediaItem = items.First();
      return true;
    }
  }
}