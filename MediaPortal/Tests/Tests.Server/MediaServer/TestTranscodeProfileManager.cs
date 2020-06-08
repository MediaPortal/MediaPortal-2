using MediaPortal.Extensions.TranscodingService.Interfaces;
using MediaPortal.Extensions.TranscodingService.Interfaces.Metadata;
using MediaPortal.Extensions.TranscodingService.Interfaces.Profiles.Setup;
using MediaPortal.Extensions.TranscodingService.Interfaces.Transcoding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests.Server.MediaServer
{
  class TestTranscodeProfileManager : ITranscodeProfileManager
  {
    public void AddTranscodingProfile(string section, string profileName, TranscodingSetup profile)
    {
    }

    public void ClearTranscodeProfiles(string section)
    {
    }

    public AudioTranscoding GetAudioTranscoding(string section, string profile, MetadataContainer info, int edition, bool liveStreaming, string transcodeId)
    {
      return null;
    }

    public ImageTranscoding GetImageTranscoding(string section, string profile, MetadataContainer info, int edition, string transcodeId)
    {
      return null;
    }

    public AudioTranscoding GetLiveAudioTranscoding(MetadataContainer info, string transcodeId)
    {
      return null;
    }

    public VideoTranscoding GetLiveVideoTranscoding(MetadataContainer info, int audioStreamIndex, string transcodeId)
    {
      return null;
    }

    public VideoTranscoding GetLiveVideoTranscoding(MetadataContainer info, IEnumerable<string> preferedAudioLanguages, string transcodeId)
    {
      return null;
    }

    public TranscodingSetup GetTranscodeProfile(string section, string profile)
    {
      return null;
    }

    public VideoTranscoding GetVideoSubtitleTranscoding(string section, string profile, MetadataContainer info, int edition, bool live, string transcodeId)
    {
      return null;
    }

    public VideoTranscoding GetVideoTranscoding(string section, string profile, MetadataContainer info, int edition, int audioStreamIndex, int? subtitleStreamIndex, bool liveStreaming, string transcodeId)
    {
      return null;
    }

    public VideoTranscoding GetVideoTranscoding(string section, string profile, MetadataContainer info, int edition, IEnumerable<string> preferedAudioLanguages, bool liveStreaming, string transcodeId)
    {
      return null;
    }

    public Task LoadTranscodeProfilesAsync(string section, string profileFile)
    {
      return Task.CompletedTask;
    }
  }
}
