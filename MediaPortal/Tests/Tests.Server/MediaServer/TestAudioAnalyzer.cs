using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Extensions.TranscodingService.Interfaces;
using MediaPortal.Extensions.TranscodingService.Interfaces.Metadata;
using MediaPortal.Plugins.SlimTv.Interfaces.LiveTvMediaItem;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Tests.Server.MediaServer
{
  class TestAudioAnalyzer : IMediaAnalyzer
  {
    public Task<MetadataContainer> ParseChannelStreamAsync(int ChannelId, LiveTvMediaItem ChannelMediaItem)
    {
      return Task.FromResult(new MetadataContainer
      {
        Audio = new List<MediaPortal.Extensions.TranscodingService.Interfaces.Metadata.Streams.AudioStream>(),
        Video = new MediaPortal.Extensions.TranscodingService.Interfaces.Metadata.Streams.VideoStream(),
        Metadata = new MediaPortal.Extensions.TranscodingService.Interfaces.Metadata.Streams.MetadataStream()
      });
    }

    public Task<IDictionary<int, IList<MetadataContainer>>> ParseMediaItemAsync(MediaItem Media, int? MediaPartSetId = null)
    {
      IDictionary<int, IList<MetadataContainer>> dictionary = new Dictionary<int, IList<MetadataContainer>>();
      List<MetadataContainer> list = new List<MetadataContainer>();
      list.Add(new MetadataContainer
      {
        Audio = new List<MediaPortal.Extensions.TranscodingService.Interfaces.Metadata.Streams.AudioStream>(),
        Metadata = new MediaPortal.Extensions.TranscodingService.Interfaces.Metadata.Streams.MetadataStream()
      });
      dictionary.Add(1, list);
      return Task.FromResult(dictionary);
    }

    public Task<MetadataContainer> ParseMediaStreamAsync(IResourceAccessor MediaResource, string AnalysisName = null)
    {
      return Task.FromResult(new MetadataContainer
      {
        Audio = new List<MediaPortal.Extensions.TranscodingService.Interfaces.Metadata.Streams.AudioStream>(),
        Metadata = new MediaPortal.Extensions.TranscodingService.Interfaces.Metadata.Streams.MetadataStream()
      });
    }
  }
}
