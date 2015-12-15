using System;
using System.Collections.Generic;
using System.Linq;
using HttpServer;
using HttpServer.Exceptions;
using HttpServer.Sessions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.EPG.BaseClasses;
using MediaPortal.Plugins.MP2Extended.TAS;
using MediaPortal.Plugins.MP2Extended.TAS.Tv;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using Newtonsoft.Json;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.EPG
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "groupId", Type = typeof(int), Nullable = false)]
  [ApiFunctionParam(Name = "startTime", Type = typeof(DateTime), Nullable = false)]
  [ApiFunctionParam(Name = "endTime", Type = typeof(DateTime), Nullable = false)]
  internal class GetProgramsBasicForGroup : BaseProgramBasic, IRequestMicroModuleHandler
  {
    public dynamic Process(IHttpRequest request, IHttpSession session)
    {
      HttpParam httpParam = request.Param;
      string groupId = httpParam["groupId"].Value;
      string startTime = httpParam["startTime"].Value;
      string endTime = httpParam["endTime"].Value;

      if (groupId == null)
        throw new BadRequestException("GetProgramsBasicForGroup: groupId is null");
      if (startTime == null)
        throw new BadRequestException("GetProgramsBasicForGroup: startTime is null");
      if (endTime == null)
        throw new BadRequestException("GetProgramsBasicForGroup: endTime is null");

      int groupIdInt;
      if (!int.TryParse(groupId, out groupIdInt))
        throw new BadRequestException(string.Format("GetProgramsBasicForGroup: Couldn't parse programId to int: {0}", groupId));
      DateTime startDateTime;
      if (!DateTime.TryParse(startTime, out startDateTime))
        throw new BadRequestException(string.Format("GetProgramsBasicForGroup: Couldn't parse startTime to DateTime: {0}", startTime));
      DateTime endDateTime;
      if (!DateTime.TryParse(endTime, out endDateTime))
        throw new BadRequestException(string.Format("GetProgramsBasicForGroup: Couldn't parse endTime to DateTime: {0}", endTime));

      if (!ServiceRegistration.IsRegistered<ITvProvider>())
        throw new BadRequestException("GetProgramsBasicForGroup: ITvProvider not found");

      IChannelAndGroupInfo channelAndGroupInfo = ServiceRegistration.Get<ITvProvider>() as IChannelAndGroupInfo;
      IProgramInfo programInfo = ServiceRegistration.Get<ITvProvider>() as IProgramInfo;

      IList<IChannelGroup> grouList;
      if (!channelAndGroupInfo.GetChannelGroups(out grouList))
        throw new BadRequestException(string.Format("GetProgramsDetailedForGroup: Couldn't get channel with Id: {0}", groupIdInt));

      IChannelGroup group = grouList.Single(x => x.ChannelGroupId == groupIdInt);

      IList<IProgram> programList;
      if (!programInfo.GetProgramsGroup(group, startDateTime, endDateTime, out programList))
        Logger.Warn("GetProgramsDetailedForGroup: Couldn't get Now/Next Info for channel with Id: {0}", groupIdInt);

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