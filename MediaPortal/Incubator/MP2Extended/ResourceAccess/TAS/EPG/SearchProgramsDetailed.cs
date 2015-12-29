using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HttpServer;
using HttpServer.Exceptions;
using HttpServer.Sessions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.PluginManager;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.EPG.BaseClasses;
using MediaPortal.Plugins.MP2Extended.TAS.Tv;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.EPG
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "searchTerm", Type = typeof(string), Nullable = false)]
  internal class SearchProgramsDetailed : BaseProgramDetailed, IRequestMicroModuleHandler
  {
    public dynamic Process(IHttpRequest request, IHttpSession session)
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

      List<WebProgramDetailed> output = new List<WebProgramDetailed>();

      // works for TVE3, but not TVE3.5
      bool TVE3 = ServiceRegistration.Get<IPluginManager>().AvailablePlugins.ContainsKey(Guid.Parse("796C1294-38BA-4C9C-8E56-AA299558A59B"));
      Logger.Debug("SearchProgramsDetailed: TVE3 ? '{0}'", TVE3);

      if (TVE3)
      {
        IList<IProgram> programs;
        programInfo.GetPrograms(searchTerm, DateTime.Now, DateTime.Now.AddMonths(2), out programs);

        foreach (var program in programs)
        {
          output.Add(ProgramDetailed(program));
        }
      }
      // TVE 3.5 does an exact string compare and doesn't use RegEx
      else
      {
        IList<IChannelGroup> channelGroups = new List<IChannelGroup>();
        channelAndGroupInfo.GetChannelGroups(out channelGroups);


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
      }

      return output;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}