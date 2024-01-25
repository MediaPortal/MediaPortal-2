#region Copyright (C) 2007-2023 Team MediaPortal

/*
    Copyright (C) 2007-2023 Team MediaPortal
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

using DirectShow.Helper;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.UI.Players;
using MediaPortal.UI.Players.Video;
using MediaPortal.UI.Players.Video.Tools;
using MediaPortal.UI.Presentation.Players;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace Cinema.Player
{
  public class CinemaPlayer : VideoPlayer, IUIContributorPlayer
  {
    public const string CINEMA_MIMETYPE = "cinema/stream";

    protected DotNetStreamSourceFilter _videoStreamFilter;
    protected Stream _videoStream;

    protected DotNetStreamSourceFilter _audioStreamFilter;
    protected Stream _audioStream;

    public Type UIContributorType
    {
      get { return typeof(CinemaUiContributor); }
    }

    protected override void AddSourceFilter()
    {
      var networkResourceAccessor = _resourceAccessor as INetworkResourceAccessor;
      if (networkResourceAccessor == null || !TryAddYouTubeStreams(networkResourceAccessor.URL).Result)
      {
        base.AddSourceFilter();
        return;
      }

      int hr;
      using (DSFilter source2 = new DSFilter(_videoStreamFilter))
        hr = source2.OutputPin.Render();
      new HRESULT(hr).Throw();

      if (_audioStreamFilter != null)
      {
        using (DSFilter source2 = new DSFilter(_audioStreamFilter))
          hr = source2.OutputPin.Render();
        new HRESULT(hr).Throw();
      }
    }

    protected override void FreeCodecs()
    {
      if (_graphBuilder != null)
      {
        if (_videoStreamFilter != null)
        {
          _graphBuilder.RemoveFilter(_videoStreamFilter);
          FilterGraphTools.TryDispose(ref _videoStreamFilter);
          FilterGraphTools.TryDispose(ref _videoStream);

        }
        if (_audioStreamFilter != null)
        {
          _graphBuilder.RemoveFilter(_audioStreamFilter);
          FilterGraphTools.TryDispose(ref _audioStreamFilter);
          FilterGraphTools.TryDispose(ref _audioStream);
        }
      }

      base.FreeCodecs();
    }

    private async Task<bool> TryAddYouTubeStreams(string url)
    {
      if (!IsYouTubeUrl(url))
        return false;

      var youtube = new YoutubeClient();
      var streamManifest = await youtube.Videos.Streams.GetManifestAsync(url);

      // Try and get the highest quality video stream
      var videoStream = streamManifest.GetVideoStreams().TryGetWithHighestVideoQuality();
      if (videoStream == null)
      {
        ServiceRegistration.Get<ILogger>().Error("{0}: Unable to find a video stream for YouTube url '{1}'", PlayerTitle, url);
        return false;
      }

      // If the stream is muxed and therefore contains audio, simply add it and return
      AddVideoStream(await youtube.Videos.Streams.GetAsync(videoStream), videoStream.Url);
      if (videoStream is MuxedStreamInfo)
        return true;

      // else the video stream does not contain audio so try and get the highest quality audio stream.
      // WebM audio streams don't seem to work so limit the restults to Mp4 streams
      ServiceRegistration.Get<ILogger>().Debug("{0}: Initializing separate audio stream for Youtube url '{1}'", PlayerTitle, url);
      var audioStream = streamManifest.GetAudioOnlyStreams().Where(s => s.Container == Container.Mp4).TryGetWithHighestBitrate();
      if (audioStream == null)
      {
        ServiceRegistration.Get<ILogger>().Warn("{0}: Unable to find an audio stream for YouTube url '{1}'", PlayerTitle, url);
        // There's still a video stream, so at least allow that to play
        return true;
      }

      AddAudioStream(await youtube.Videos.Streams.GetAsync(audioStream), audioStream.Url);
      return true;
    }

    protected void AddVideoStream(Stream stream, string fileName)
    {
      if (stream == null)
        return;
      _videoStream = stream;
      _videoStreamFilter = new DotNetStreamSourceFilter();
      _videoStreamFilter.SetSourceStream(_videoStream, fileName);
      int hr = _graphBuilder.AddFilter(_videoStreamFilter, _videoStreamFilter.Name);
      new HRESULT(hr).Throw();
    }

    protected void AddAudioStream(Stream stream, string fileName)
    {
      if (stream == null)
        return;
      _audioStream = stream;
      _audioStreamFilter = new DotNetStreamSourceFilter();
      _audioStreamFilter.SetSourceStream(_audioStream, fileName);
      int hr = _graphBuilder.AddFilter(_audioStreamFilter, _audioStreamFilter.Name);
      new HRESULT(hr).Throw();
    }

    private static bool IsYouTubeUrl(string url)
    {
      return url?.StartsWith("https://youtu.be/") ?? false;
    }
  }
}
