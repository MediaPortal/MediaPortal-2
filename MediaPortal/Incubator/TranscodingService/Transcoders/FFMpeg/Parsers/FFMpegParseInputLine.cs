using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaPortal.Common.ResourceAccess;

namespace MediaPortal.Plugins.Transcoding.Service.Transcoders.FFMpeg.Parsers
{
  public class FFMpegParseInputLine
  {
    internal static void ParseInputLine(string inputLine, ref MetadataContainer info)
    {
      inputLine = inputLine.Trim();
      int inputPos = inputLine.IndexOf("Input #0", StringComparison.InvariantCultureIgnoreCase);
      string ffmContainer = inputLine.Substring(inputPos + 10, inputLine.IndexOf(",", inputPos + 11) - 10).Trim();
      if (info.IsAudio)
      {
        info.Metadata.AudioContainerType = FFMpegParseAudioContainer.ParseAudioContainer(ffmContainer);
      }
      else if (info.IsVideo)
      {
        info.Metadata.VideoContainerType = FFMpegParseVideoContainer.ParseVideoContainer(ffmContainer, (ILocalFsResourceAccessor)info.Metadata.Source);
      }
      else if (info.IsImage)
      {
        info.Metadata.ImageContainerType = FFMpegParseImageContainer.ParseImageContainer(ffmContainer);
      }
      else
      {
        info.Metadata.VideoContainerType = FFMpegParseVideoContainer.ParseVideoContainer(ffmContainer, (ILocalFsResourceAccessor)info.Metadata.Source);
        info.Metadata.AudioContainerType = FFMpegParseAudioContainer.ParseAudioContainer(ffmContainer);
        info.Metadata.ImageContainerType = FFMpegParseImageContainer.ParseImageContainer(ffmContainer);
      }
    }
  }
}
