using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Extensions.TranscodingService.Interfaces;
using MediaPortal.Extensions.TranscodingService.Interfaces.Metadata;
using MediaPortal.Plugins.SlimTv.Interfaces.LiveTvMediaItem;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Extensions.TranscodingService.Interfaces.Metadata.Streams;

namespace Tests.Server.MediaServer
{
  class TestAudioAnalyzer : IMediaAnalyzer
  {
    public Task DeleteAnalysisAsync(Guid mediaItemId)
    {
      return Task.CompletedTask;
    }

    public ICollection<Guid> GetAllAnalysisIds()
    {
      return new List<Guid>();
    }

    public Task<MetadataContainer> ParseChannelStreamAsync(int channelId, LiveTvMediaItem channelMediaItem)
    {
      var info = new MetadataContainer();
      var edition = 0;
      info.AddEdition(edition);
      info.Metadata[edition].VideoContainerType = VideoContainer.Mp4;
      info.Video[edition] = new VideoStream
      {
        Codec = VideoCodec.H264,
        AspectRatio = 1.777777777F,
        Width = 1920,
        Height = 1080,
        Framerate = 25,
      };
      info.Audio[edition].Add(new AudioStream
      {
        Codec = AudioCodec.Aac,
        Channels = 2
      });
      return Task.FromResult(info);
    }

    public Task<MetadataContainer> ParseMediaItemAsync(MediaItem media, int? mediaPartSetId = null, bool cache = true)
    {
      var info = new MetadataContainer();
      var edition = mediaPartSetId ?? 0;
      info.AddEdition(edition);
      if (media.Aspects.ContainsKey(AudioAspect.ASPECT_ID))
      {
        info.Metadata[edition].AudioContainerType = AudioContainer.Mp3;
        info.Audio[edition].Add(new AudioStream
        {
          Codec = AudioCodec.Mp3,
        });
      }
      else if (media.Aspects.ContainsKey(VideoAspect.ASPECT_ID))
      {
        info.Metadata[edition].VideoContainerType = VideoContainer.Mp4;
        info.Video[edition] = new VideoStream
        {
          Codec = VideoCodec.H264,
          AspectRatio = 1.777777777F,
          Width = 1920,
          Height = 1080,
          Framerate = 25,
        };
        info.Audio[edition].Add(new AudioStream
        {
          Codec = AudioCodec.Aac,
          Channels = 2
        });
      }
      else if (media.Aspects.ContainsKey(ImageAspect.ASPECT_ID))
      {
        info.Metadata[edition].ImageContainerType = ImageContainer.Jpeg;
        info.Image[edition] = new ImageStream
        {
          Width = 1920,
          Height = 1080,
          Orientation = 0
        };
      }
      return Task.FromResult(info);
    }

    public Task<MetadataContainer> ParseMediaStreamAsync(IEnumerable<IResourceAccessor> mediaResources)
    {
      var info = new MetadataContainer();
      info.AddEdition(0);
      return Task.FromResult(info);
    }
  }
}
