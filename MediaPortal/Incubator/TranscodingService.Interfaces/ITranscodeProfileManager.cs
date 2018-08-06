#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System.Collections.Generic;
using System.Threading.Tasks;
using MediaPortal.Plugins.Transcoding.Interfaces.Metadata;
using MediaPortal.Plugins.Transcoding.Interfaces.Profiles.Setup;
using MediaPortal.Plugins.Transcoding.Interfaces.Transcoding;

namespace MediaPortal.Plugins.Transcoding.Interfaces
{
  public interface ITranscodeProfileManager
  {
    void ClearTranscodeProfiles(string section);

    void AddTranscodingProfile(string section, string profileName, TranscodingSetup profile);

    Task LoadTranscodeProfilesAsync(string section, string profileFile);

    TranscodingSetup GetTranscodeProfile(string section, string profile);

    /// <summary>
    /// Get the video transcoding profile that best matches the source video.
    /// </summary>
    VideoTranscoding GetVideoTranscoding(string section, string profile, IEnumerable<MetadataContainer> infos, IEnumerable<string> preferedAudioLanguages, bool liveStreaming, string transcodeId);

    /// <summary>
    /// Get the audio transcoding profile that best matches the source audio.
    /// </summary>
    AudioTranscoding GetAudioTranscoding(string section, string profile, MetadataContainer info, bool liveStreaming, string transcodeId);

    /// <summary>
    /// Get the image transcoding profile that best matches the source image.
    /// </summary>
    ImageTranscoding GetImageTranscoding(string section, string profile, MetadataContainer info, string transcodeId);

    /// <summary>
    /// Get a video transcoding profile that adds subtitles the source video.
    /// </summary>
    VideoTranscoding GetVideoSubtitleTranscoding(string section, string profile, IEnumerable<MetadataContainer> infos, bool live, string transcodeId);

    /// <summary>
    /// Get a video transcoding profile that streams the live source video.
    /// </summary>
    VideoTranscoding GetLiveVideoTranscoding(MetadataContainer info, IEnumerable<string> preferedAudioLanguages, string transcodeId);

    /// <summary>
    /// Get an audio transcoding profile that streams the live source audio.
    /// </summary>
    AudioTranscoding GetLiveAudioTranscoding(MetadataContainer info, string transcodeId);
  }
}
