using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HttpServer;
using HttpServer.Exceptions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.EPG.BaseClasses;
using MediaPortal.Plugins.MP2Extended.TAS;
using MediaPortal.Plugins.MP2Extended.TAS.Tv;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using Newtonsoft.Json;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.EPG
{
  internal class SearchProgramsDetailed : BaseProgramDetailed, IRequestMicroModuleHandler
  {
    public dynamic Process(IHttpRequest request)
    {
      HttpParam httpParam = request.Param;
      string searchTerm = httpParam["searchTerm"].Value;

      if (searchTerm == null)
        throw new BadRequestException("SearchProgramsDetailed: searchTerm is null");



      if (!ServiceRegistration.IsRegistered<ITvProvider>())
        throw new BadRequestException("SearchProgramsDetailed: ITvProvider not found");

      Regex regex = new Regex(@searchTerm);

      IChannelAndGroupInfo channelAndGroupInfo = ServiceRegistration.Get<ITvProvider>() as IChannelAndGroupInfo;
      IProgramInfo programInfo = ServiceRegistration.Get<ITvProvider>() as IProgramInfo;

      IList<IChannelGroup> channelGroups = new List<IChannelGroup>();
      channelAndGroupInfo.GetChannelGroups(out channelGroups);

      List<WebProgramDetailed> output = new List<WebProgramDetailed>();

      foreach (var group in channelGroups.Where(x => x.MediaType == MediaType.TV))
      {
        // get channel for goup
        IList<IChannel> channels = new List<IChannel>();
        if (!channelAndGroupInfo.GetChannels(group, out channels))
          continue;

        foreach (var channel in channels)
        {
          IList<IProgram> programs;
          programInfo.GetPrograms(channel, DateTime.Now, DateTime.Now.AddMonths(2), out programs);
          foreach (var program in programs)
          {
            if (regex.IsMatch(program.Title))
              output.Add(ProgramDetailed(program));
          }
        }
      }

      return output;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}