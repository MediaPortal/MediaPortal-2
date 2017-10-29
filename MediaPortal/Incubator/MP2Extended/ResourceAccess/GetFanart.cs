using MediaPortal.Common.FanArt;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.MAS.General;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MP2Extended.ResourceAccess
{
  internal static class GetFanart
  {
    private static readonly Dictionary<WebMediaType, string> _fanArtMediaTypeMapping = new Dictionary<WebMediaType, string>
    {
      { WebMediaType.Movie, FanArtMediaTypes.Movie },
      { WebMediaType.TVEpisode, FanArtMediaTypes.Episode },
      { WebMediaType.TVSeason, FanArtMediaTypes.SeriesSeason },
      { WebMediaType.TVShow, FanArtMediaTypes.Series },
      { WebMediaType.MusicTrack, FanArtMediaTypes.Audio },
      { WebMediaType.MusicAlbum, FanArtMediaTypes.Album },
      { WebMediaType.MusicArtist, FanArtMediaTypes.Artist },
      { WebMediaType.Picture, FanArtMediaTypes.Image },
      { WebMediaType.TV, FanArtMediaTypes.ChannelTv },
      { WebMediaType.Radio, FanArtMediaTypes.ChannelRadio },
      { WebMediaType.Recording, FanArtMediaTypes.Undefined },
    };

    private static readonly Dictionary<WebFileType, string> _fanArtTypeMapping = new Dictionary<WebFileType, string>
    {
      { WebFileType.Backdrop, FanArtTypes.FanArt },
      { WebFileType.Banner, FanArtTypes.Banner },
      { WebFileType.Content, FanArtTypes.Thumbnail },
      { WebFileType.Cover, FanArtTypes.Poster },
      { WebFileType.Logo, FanArtTypes.Logo }, // ??
      { WebFileType.Poster, FanArtTypes.Poster },
    };

    public static IList<WebArtwork> GetArtwork(Guid id, WebMediaType mediatype)
    {
      string fanArtMediaType;
      if (!_fanArtMediaTypeMapping.TryGetValue(mediatype, out fanArtMediaType))
        return new List<WebArtwork>();

      string name = id.ToString();
      return _fanArtTypeMapping.SelectMany(kv => FanArtCache.GetFanArtFiles(name, kv.Value).Select((p, i) => Artwork(p, kv.Key, i))).ToList();
    }

    private static WebArtwork Artwork(string path, WebFileType fileType, int offset)
    {
      return new WebArtworkDetailed
      {
        Filetype = Path.GetExtension(path).Substring(1),
        Id = path.GetHashCode().ToString(),
        Offset = offset,
        Path = path,
        Type = fileType
      };
    }
  }
}
