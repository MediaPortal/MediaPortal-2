using MediaPortal.Plugins.MP2Extended.TAS.Tv;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Tv.BaseClasses
{
  class BaseChannelBasic
  {
    internal static WebChannelBasic ChannelBasic(IChannel channel)
    {
      WebChannelBasic webChannelBasic = new WebChannelBasic
      {
        Id = channel.ChannelId,
        IsRadio = channel.MediaType == MediaType.Radio,
        IsTv = channel.MediaType == MediaType.TV,
        Title = channel.Name,
      };

      return webChannelBasic;
    }
  }
}
