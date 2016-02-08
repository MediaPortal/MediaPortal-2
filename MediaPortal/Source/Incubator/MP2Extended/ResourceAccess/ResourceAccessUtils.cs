using System.Text;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Plugins.MP2Extended.Common;
using Newtonsoft.Json;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess
{
  internal class ResourceAccessUtils
  {
    internal static byte[] GetBytes(string str)
    {
      //ASCIIEncoding enc = new ASCIIEncoding();
      UTF8Encoding enc = new UTF8Encoding();
      return enc.GetBytes(str);
    }

    /// <summary>
    /// Serialize an Object by using Json and converts the resulting string to a byte Array
    /// </summary>
    /// <param name="input">An dynamic object</param>
    /// <returns></returns>
    internal static byte[] GetBytesFromDynamic(dynamic input)
    {
      // We want to use the Miscrosoft.DateTime format (like MPExtended)
      JsonSerializerSettings settings = new JsonSerializerSettings
      {
        DateFormatHandling = DateFormatHandling.MicrosoftDateFormat
      };
      return GetBytes(JsonConvert.SerializeObject(input, settings));
    }

    internal static WebMediaType GetWebMediaType(MediaItem mediaItem)
    {
      if (mediaItem.Aspects.ContainsKey(VideoAspect.ASPECT_ID) || mediaItem.Aspects.ContainsKey(MovieAspect.ASPECT_ID))
      {
        return WebMediaType.Movie;
      }

      if (mediaItem.Aspects.ContainsKey(SeriesAspect.ASPECT_ID))
      {
        return WebMediaType.TVEpisode;
      }

      if (mediaItem.Aspects.ContainsKey(AudioAspect.ASPECT_ID))
      {
        return WebMediaType.MusicTrack;
      }

      return WebMediaType.File;
    }
  }
}