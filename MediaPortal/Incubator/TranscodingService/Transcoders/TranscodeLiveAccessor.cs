using System;
using MediaPortal.Common.ResourceAccess;

namespace MediaPortal.Extensions.TranscodingService.Service.Transcoders
{
  public class TranscodeLiveAccessor : ITranscodeLiveAccessor, INetworkResourceAccessor
  {
    public static readonly Guid TRANSCODE_LIVE_PROVIDER_ID = new Guid("{231DC783-090E-4E4E-8BD6-4DFA2B7EB484}");

    private int _channelId = 0;

    public TranscodeLiveAccessor(int ChannelId)
    {
      _channelId = ChannelId;
    }

    public ResourcePath CanonicalLocalResourcePath
    {
      get
      {
        return ResourcePath.BuildBaseProviderPath(TRANSCODE_LIVE_PROVIDER_ID, _channelId.ToString());
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
        return _channelId.ToString();
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
        return _channelId.ToString();
      }
    }

    public string URL
    {
      get
      {
        return $"rtsp://channel_{_channelId}_stream_placeholder";
      }
    }

    int ITranscodeLiveAccessor.ChannelId
    {
      get
      {
        return _channelId;
      }
    }

    public IResourceAccessor Clone()
    {
      return new TranscodeLiveAccessor(_channelId);
    }

    public void Dispose()
    {
    }
  }

  public interface ITranscodeLiveAccessor : IResourceAccessor
  {
    int ChannelId { get; }
  }
}
