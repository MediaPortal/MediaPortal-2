using System;
using System.Collections.Generic;
using System.Linq;
using HttpServer;
using HttpServer.Sessions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.EPG.BaseClasses;
using MediaPortal.Plugins.MP2Extended.TAS.Tv;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.EPG
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "groupId", Type = typeof(int), Nullable = false)]
  [ApiFunctionParam(Name = "startTime", Type = typeof(DateTime), Nullable = false)]
  [ApiFunctionParam(Name = "endTime", Type = typeof(DateTime), Nullable = false)]
  internal class GetProgramsBasicForGroup : BaseProgramBasic
  {
    public IList<WebChannelPrograms<WebProgramBasic>> Process(int groupId, DateTime startTime, DateTime endTime)
    {
      if (!ServiceRegistration.IsRegistered<ITvProvider>())
        throw new BadRequestException("GetProgramsBasicForGroup: ITvProvider not found");

      IChannelAndGroupInfo channelAndGroupInfo = ServiceRegistration.Get<ITvProvider>() as IChannelAndGroupInfo;
      IProgramInfo programInfo = ServiceRegistration.Get<ITvProvider>() as IProgramInfo;

      IList<IChannelGroup> grouList;
      if (!channelAndGroupInfo.GetChannelGroups(out grouList))
        throw new BadRequestException(string.Format("GetProgramsDetailedForGroup: Couldn't get channel with Id: {0}", groupId));

      IChannelGroup group = grouList.Single(x => x.ChannelGroupId == groupId);

      IList<IProgram> programList;
      if (!programInfo.GetProgramsGroup(group, startTime, endTime, out programList))
        Logger.Warn("GetProgramsDetailedForGroup: Couldn't get Now/Next Info for channel with Id: {0}", groupId);

      List<WebChannelPrograms<WebProgramBasic>> output = new List<WebChannelPrograms<WebProgramBasic>>();

      foreach (var program in programList)
      {
        if (output.FindIndex(x => x.ChannelId == program.ChannelId) == -1)
          output.Add(new WebChannelPrograms<WebProgramBasic>
          {
            ChannelId = program.ChannelId,
            Programs = programList.Select(y => ProgramBasic(y)).Where(x => x.ChannelId == program.ChannelId).ToList()
          });
      }

      return output;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}