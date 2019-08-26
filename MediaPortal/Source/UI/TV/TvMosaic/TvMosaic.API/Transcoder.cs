///
/// Copyright(c) 2007-2012 DVBLogic (info@dvblogic.com)    
/// All rights reserved                                    
///

using System.Runtime.Serialization;

namespace TvMosaic.API
{
  [DataContract(Name = "transcoder", Namespace = "")]
  public class Transcoder
  {
    [DataMember(Name = "height", EmitDefaultValue = false)]
    public uint Height { get; private set; }

    [DataMember(Name = "width", EmitDefaultValue = false)]
    public uint Width { get; private set; }

    [DataMember(Name = "bitrate", EmitDefaultValue = false)]
    public uint Bitrate { get; private set; }

    [DataMember(Name = "audio_track", EmitDefaultValue = false)]
    public string AudioTrack { get; private set; }

    public Transcoder(uint height, uint width, uint bitrate, string audio_track)
    {
      Height = height;
      Width = width;
      Bitrate = bitrate;
      AudioTrack = audio_track;
    }
  }
}
