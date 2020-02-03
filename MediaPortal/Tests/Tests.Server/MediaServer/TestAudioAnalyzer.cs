using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Extensions.TranscodingService.Interfaces;
using MediaPortal.Extensions.TranscodingService.Interfaces.Metadata;
using MediaPortal.Plugins.SlimTv.Interfaces.LiveTvMediaItem;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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
      info.AddEdition(0);
      return Task.FromResult(info);
    }

    public Task<MetadataContainer> ParseMediaItemAsync(MediaItem media, int? mediaPartSetId = null, bool cache = true)
    {
      var info = new MetadataContainer();
      info.AddEdition(mediaPartSetId ?? 0);
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
