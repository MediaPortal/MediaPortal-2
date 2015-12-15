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
  [ApiFunctionParam(Name = "channelId", Type = typeof(int), Nullable = false)]
  [ApiFunctionParam(Name = "startTime", Type = typeof(DateTime), Nullable = false)]
  [ApiFunctionParam(Name = "endTime", Type = typeof(DateTime), Nullable = false)]
  internal class GetProgramsDetailedForChannel : BaseProgramDetailed, IRequestMicroModuleHandler
  {
    public dynamic Process(IHttpRequest request, IHttpSession session)
    {
      HttpParam httpParam = request.Param;
      string channelId = httpParam["channelId"].Value;
      string startTime = httpParam["startTime"].Value;
      string endTime = httpParam["endTime"].Value;

      if (channelId == null)
        throw new BadRequestException("GetProgramsDetailedForChannel: channelId is null");
      if (startTime == null)
        throw new BadRequestException("GetProgramsDetailedForChannel: startTime is null");
      if (endTime == null)
        throw new BadRequestException("GetProgramsDetailedForChannel: endTime is null");

      int channelIdInt;
      if (!int.TryParse(channelId, out channelIdInt))
        throw new BadRequestException(string.Format("GetProgramsDetailedForChannel: Couldn't parse programId to int: {0}", channelId));
      DateTime startDateTime;
      if (!DateTime.TryParse(startTime, out startDateTime))
        throw new BadRequestException(string.Format("GetProgramsDetailedForChannel: Couldn't parse startTime to DateTime: {0}", startTime));
      DateTime endDateTime;
      if (!DateTime.TryParse(endTime, out endDateTime))
        throw new BadRequestException(string.Format("GetProgramsDetailedForChannel: Couldn't parse endTime to DateTime: {0}", endTime));

      if (!ServiceRegistration.IsRegistered<ITvProvider>())
        throw new BadRequestException("GetProgramsDetailedForChannel: ITvProvider not found");

      IChannelAndGroupInfo channelAndGroupInfo = ServiceRegistration.Get<ITvProvider>() as IChannelAndGroupInfo;
      IProgramInfo programInfo = ServiceRegistration.Get<ITvProvider>() as IProgramInfo;

      IChannel channel;
      if (!channelAndGroupInfo.GetChannel(channelIdInt, out channel))
        throw new BadRequestException(string.Format("GetProgramsDetailedForChannel: Couldn't get channel with Id: {0}", channelIdInt));

      IList<IProgram> programList;
      if (!programInfo.GetPrograms(channel, startDateTime, endDateTime, out programList))
        Logger.Warn("GetProgramsDetailedForChannel: Couldn't get Now/Next Info for channel with Id: {0}", channelIdInt);


      return programList.Select(program => ProgramDetailed(program)).ToList();
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}