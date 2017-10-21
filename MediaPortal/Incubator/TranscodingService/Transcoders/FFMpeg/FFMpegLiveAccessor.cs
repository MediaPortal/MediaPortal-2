using System;
using MediaPortal.Common.ResourceAccess;

namespace MediaPortal.Plugins.Transcoding.Service.Transcoders.FFMpeg
{
  public class FFMpegLiveAccessor : IFFMpegLiveAccessor
  {
    private Guid RESOURCE_PROVIDER_ID = new Guid("{231DC783-090E-4E4E-8BD6-4DFA2B7EB484}");
    private int _channelId = 0;

    public FFMpegLiveAccessor(int ChannelId)
    {
      _channelId = ChannelId;
    }

    public ResourcePath CanonicalLocalResourcePath
    {
      get
      {
        return ResourcePath.BuildBaseProviderPath(RESOURCE_PROVIDER_ID, "");
      }
    }

    public IResourceProvider ParentProvider
    {
      get
      {
        return null;
      }
    }

    public string Path
    {
      get
      {
        return null;
      }
    }

    public string ResourceName
    {
      get
      {
        return "Live Channel " + _channelId;
      }
    }

    public string ResourcePathName
    {
      get
      {
        return null;
      }
    }

    int IFFMpegLiveAccessor.ChannelId
    {
      get
      {
        return _channelId;
      }
    }

    public IResourceAccessor Clone()
    {
      return new FFMpegLiveAccessor(_channelId);
    }

    public void Dispose()
    {
    }
  }

  public interface IFFMpegLiveAccessor : IResourceAccessor
  {
    int ChannelId { get; }
  }
}
