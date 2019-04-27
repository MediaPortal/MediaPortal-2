using System;
using MediaPortal.Common.ResourceAccess;

namespace MediaPortal.Extensions.TranscodingService.Service.Transcoders
{
  public class TranscodeLiveAccessor : ITranscodeLiveAccessor
  {
    private Guid RESOURCE_PROVIDER_ID = new Guid("{231DC783-090E-4E4E-8BD6-4DFA2B7EB484}");
    private int _channelId = 0;

    public TranscodeLiveAccessor(int ChannelId)
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
