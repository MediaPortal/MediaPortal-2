using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.EPG.BaseClasses;
using MediaPortal.Plugins.MP2Extended.TAS.Tv;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.Plugins.SlimTv.Interfaces.UPnP.Items;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Tv.BaseClasses
{
  class BaseChannelDetailed : BaseProgramDetailed
  {
    internal WebChannelDetailed ChannelDetailed(IChannel channel)
    {
      IProgramInfo programInfo = ServiceRegistration.Get<ITvProvider>() as IProgramInfo;
      IProgram programNow = new Program();
      IProgram programNext = new Program();
      programInfo.GetNowNextProgram(channel, out programNow, out programNext);

      WebChannelDetailed webChannelDetailed = new WebChannelDetailed
      {
        Id = channel.ChannelId,
        IsRadio = channel.MediaType == MediaType.Radio,
        IsTv = channel.MediaType == MediaType.TV,
        Title = channel.Name,
        CurrentProgram = ProgramDetailed(programNow),
        NextProgram = ProgramDetailed(programNext),
        EpgHasGaps = channel.EpgHasGaps,
        ExternalId = channel.ExternalId,
        GrabEpg = channel.GrapEpg,
        GroupNames = channel.GroupNames,
        LastGrabTime = channel.LastGrabTime ?? DateTime.Now,
        TimesWatched = channel.TimesWatched,
        TotalTimeWatched = channel.TotalTimeWatched ?? DateTime.Now,
        VisibleInGuide = channel.VisibleInGuide
      };

      return webChannelDetailed;
    }
  }
}
