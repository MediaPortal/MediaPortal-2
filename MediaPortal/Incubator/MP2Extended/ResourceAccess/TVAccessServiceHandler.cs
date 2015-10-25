using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using HttpServer;
using HttpServer.Exceptions;
using HttpServer.Sessions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.BaseClasses;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Channels;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.EPG;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Misc;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Radio;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Recording;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Schedule;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Timeshiftings;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Tv;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess
{
  internal class TVAccessServiceHandler : BaseJsonHeader, IRequestModuleHandler
  {
    private readonly Dictionary<string, IRequestMicroModuleHandler> _requestModuleHandlers = new Dictionary<string, IRequestMicroModuleHandler>
    {
      // Misc
      { "GetActiveCards", new GetActiveCards()},
      { "GetActiveUsers", new GetActiveUsers()},
      { "GetCards", new GetCards()},
      { "GetLocalDiskInformation", new GetLocalDiskInformation()},
      { "GetServiceDescription", new GetServiceDescription()},
      { "TestConnectionToTVService", new TestConnectionToTVService()},
      // Channels
      { "GetAllChannelStatesForGroup", new GetAllChannelStatesForGroup()},
      { "GetChannelState", new GetChannelState()},
      // Tv
      { "GetChannelBasicById", new GetChannelBasicById()},
      { "GetChannelCount", new GetChannelCount()},
      { "GetChannelDetailedById", new GetChannelDetailedById()},
      { "GetChannelsBasic", new GetChannelsBasic()},
      { "GetChannelsBasicByRange", new GetChannelsBasicByRange()},
      { "GetChannelsDetailed", new GetChannelsDetailed()},
      { "GetChannelsDetailedByRange", new GetChannelsDetailedByRange()},
      { "GetGroupById", new GetGroupById()},
      { "GetGroupCount", new GetGroupCount()},
      { "GetGroups", new GetGroups()},
      { "GetGroupsByRange", new GetGroupsByRange()},
      // Radio
      { "GetRadioChannelCount", new GetRadioChannelCount()},
      { "GetRadioChannelsBasic", new GetRadioChannelsBasic()},
      { "GetRadioChannelsBasicByRange", new GetRadioChannelsBasicByRange()},
      { "GetRadioChannelsDetailed", new GetRadioChannelsDetailed()},
      { "GetRadioChannelsDetailedByRange", new GetRadioChannelsDetailedByRange()},
      { "GetRadioGroupById", new GetRadioGroupById()},
      { "GetRadioGroupCount", new GetRadioGroupCount()},
      { "GetRadioGroups", new GetRadioGroups()},
      { "GetRadioGroupsByRange", new GetRadioGroupsByRange()},
      // Timeshiftings
      { "CancelCurrentTimeShifting", new CancelCurrentTimeShifting()},
      { "SwitchTVServerToChannelAndGetStreamingUrl", new SwitchTVServerToChannelAndGetStreamingUrl()},
      // Schedule
      { "AddSchedule", new AddSchedule()},
      { "CancelSchedule", new CancelSchedule()},
      { "DeleteSchedule", new DeleteSchedule()},
      { "GetProgramIsScheduled", new GetProgramIsScheduled()},
      { "GetScheduleById", new GetScheduleById()},
      { "GetScheduleCount", new GetScheduleCount()},
      { "GetScheduledRecordingsForDate", new GetScheduledRecordingsForDate()},
      { "GetScheduledRecordingsForToday", new GetScheduledRecordingsForToday()},
      { "GetSchedules", new GetSchedules()},
      { "GetSchedulesByRange", new GetSchedulesByRange()},
      // Recording
      { "GetAllRecordingDiskInformation", new GetAllRecordingDiskInformation()},
      { "GetRecordingById", new GetRecordingById()},
      { "GetRecordingCount", new GetRecordingCount()},
      { "GetRecordingDiskInformationForCard", new GetRecordingDiskInformationForCard()},
      { "GetRecordings", new GetRecordings()},
      { "GetRecordingsByRange", new GetRecordingsByRange()},
      // EPG
      { "GetCurrentProgramOnChannel", new GetCurrentProgramOnChannel()},
      { "GetNextProgramOnChannel", new GetNextProgramOnChannel()},
      { "GetNowNextWebProgramBasicForChannel", new GetNowNextWebProgramBasicForChannel()},
      { "GetNowNextWebProgramDetailedForChannel", new GetNowNextWebProgramDetailedForChannel()},
      { "GetProgramBasicById", new GetProgramBasicById()},
      { "GetProgramDetailedById", new GetProgramDetailedById()},
      { "GetProgramsBasicForChannel", new GetProgramsBasicForChannel()},
      { "GetProgramsBasicForGroup", new GetProgramsBasicForGroup()},
      { "GetProgramsDetailedForChannel", new GetProgramsDetailedForChannel()},
      { "GetProgramsDetailedForGroup", new GetProgramsDetailedForGroup()},
      { "SearchProgramsDetailed", new SearchProgramsDetailed()},
    };

    public bool Process(IHttpRequest request, IHttpResponse response, IHttpSession session)
    {
      string[] uriParts = request.Uri.AbsolutePath.Split('/');
      string action = uriParts.Last();

      Logger.Info("TAS: AbsolutePath: {0}, uriParts.Length: {1}, Lastpart: {2}", request.Uri.AbsolutePath, uriParts.Length, action);

      // pass on to the micro processors
      IRequestMicroModuleHandler requestModuleHandler;
      dynamic returnValue = null;
      if (_requestModuleHandlers.TryGetValue(action, out requestModuleHandler))
        returnValue = requestModuleHandler.Process(request);

      if (returnValue == null)
      {
        Logger.Warn("MAS: Micromodule not found: {0}", action);
        throw new BadRequestException(String.Format("MAS: Micromodule not found: {0}", action));
      }

      byte[] output = ResourceAccessUtils.GetBytesFromDynamic(returnValue);

      // Send the response
      SendHeader(response, output.Length);

      response.SendBody(output);

      return true;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}