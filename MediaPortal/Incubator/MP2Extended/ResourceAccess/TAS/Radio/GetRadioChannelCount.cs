using System.Collections.Generic;
using System.Linq;
using HttpServer;
using HttpServer.Exceptions;
using HttpServer.Sessions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.Plugins.SlimTv.Interfaces.UPnP.Items;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Radio
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "groupId", Type = typeof(int), Nullable = false)]
  internal class GetRadioChannelCount : IRequestMicroModuleHandler
  {
    public dynamic Process(IHttpRequest request, IHttpSession session)
    {
      HttpParam httpParam = request.Param;
      string groupId = httpParam["groupId"].Value;
     

      if (!ServiceRegistration.IsRegistered<ITvProvider>())
        throw new BadRequestException("GetRadioChannelCount: ITvProvider not found");

      IChannelAndGroupInfo channelAndGroupInfo = ServiceRegistration.Get<ITvProvider>() as IChannelAndGroupInfo;
        

      IList<IChannelGroup> channelGroups = new List<IChannelGroup>();
      if (groupId == null)
        channelAndGroupInfo.GetChannelGroups(out channelGroups);
      else
      {
        int channelGroupIdInt;
        if (!int.TryParse(groupId, out channelGroupIdInt))
          throw new BadRequestException(string.Format("GetRadioChannelCount: Couldn't convert groupId to int: {0}", groupId));
        channelGroups.Add(new ChannelGroup() { ChannelGroupId = channelGroupIdInt });
      }

      int output = 0;

      foreach (var group in channelGroups.Where(x => x.MediaType == MediaType.Radio))
      {
        // get channel for goup
        IList<IChannel> channels = new List<IChannel>();
        if (!channelAndGroupInfo.GetChannels(group, out channels))
          continue;

        output += channels.Count;
      }

      return new WebIntResult { Result = output };
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}