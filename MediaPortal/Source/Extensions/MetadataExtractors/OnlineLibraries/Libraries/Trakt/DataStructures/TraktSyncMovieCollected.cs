using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktSyncMovieCollected : TraktMovie
  {
    [DataMember(Name = "collected_at")]
    public string CollectedAt { get; set; }

    [DataMember(Name = "media_type")]
    public string MediaType { get; set; }

    [DataMember(Name = "resolution")]
    public string Resolution { get; set; }

    [DataMember(Name = "audio")]
    public string AudioCodec { get; set; }

    [DataMember(Name = "audio_channels")]
    public string AudioChannels { get; set; }

    [DataMember(Name = "3d")]
    public bool Is3D { get; set; }
  }
}