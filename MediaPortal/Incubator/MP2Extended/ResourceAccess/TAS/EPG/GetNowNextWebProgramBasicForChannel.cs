using System.Collections.Generic;
using HttpServer;
using HttpServer.Exceptions;
using HttpServer.Sessions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.EPG.BaseClasses;
using MediaPortal.Plugins.MP2Extended.TAS.Tv;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.EPG
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "channelId", Type = typeof(int), Nullable = false)]
  internal class GetNowNextWebProgramBasicForChannel : BaseProgramBasic, IRequestMicroModuleHandler
  {
    public dynamic Process(IHttpRequest request, IHttpSession session)
    {
      HttpParam httpParam = request.Param;
      string channelId = httpParam["channelId"].Value;

      if (channelId == null)
        throw new BadRequestException("GetNowNextWebProgramBasicForChannel: channelId is null");

      int channelIdInt;
      if (!int.TryParse(channelId, out channelIdInt))
        throw new BadRequestException(string.Format("GetNowNextWebProgramBasicForChannel: Couldn't parse channelId to int: {0}", channelId));

      if (!ServiceRegistration.IsRegistered<ITvProvider>())
        throw new BadRequestException("GetNowNextWebProgramBasicForChannel: ITvProvider not found");

      IChannelAndGroupInfo channelAndGroupInfo = ServiceRegistration.Get<ITvProvider>() as IChannelAndGroupInfo;
      IProgramInfo programInfo = ServiceRegistration.Get<ITvProvider>() as IProgramInfo;


      IChannel channel;
      if (!channelAndGroupInfo.GetChannel(channelIdInt, out channel))
        throw new BadRequestException(string.Format("GetNowNextWebProgramBasicForChannel: Couldn't get channel with Id: {0}", channelIdInt));


      IProgram programNow;
      IProgram programNext;
      if (!programInfo.GetNowNextProgram(channel, out programNow, out programNext))
        Logger.Warn("GetNowNextWebProgramBasicForChannel: Couldn't get Now/Next Info for channel with Id: {0}", channelIdInt);

      List<WebProgramBasic> output = new List<WebProgramBasic>
      {
        ProgramBasic(programNow),
        ProgramBasic(programNext)
      };


      return output;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}