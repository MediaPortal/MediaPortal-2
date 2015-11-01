using MediaPortal.Plugins.Transcoding.Service.Transcoders.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.Plugins.Transcoding.Service.Transcoders.FFMpeg
{
  internal class FFMpegTranscodeThreadData
  {
    public FFMpegTranscodeData TranscodeData { get; set; }
    public TranscodeContext Context { get; set; }
  }
}
