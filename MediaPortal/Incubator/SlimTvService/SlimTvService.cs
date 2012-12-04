using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Timers;
using MediaPortal.Backend.Database;
using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.Utils;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.Plugins.SlimTv.Interfaces.LiveTvMediaItem;
using MediaPortal.Plugins.SlimTv.Interfaces.ResourceProvider;
using MediaPortal.Plugins.SlimTv.Service.Helpers;
using Mediaportal.TV.Server.TVControl;
using Mediaportal.TV.Server.TVControl.Interfaces.Services;
using Mediaportal.TV.Server.TVControl.ServiceAgents;
using Mediaportal.TV.Server.TVDatabase.Entities;
using Mediaportal.TV.Server.TVDatabase.Entities.Enums;
using Mediaportal.TV.Server.TVDatabase.Entities.Factories;
using Mediaportal.TV.Server.TVDatabase.EntityModel.ObjContext;
using Mediaportal.TV.Server.TVLibrary;
using Mediaportal.TV.Server.TVLibrary.Interfaces.Integration;
using Mediaportal.TV.Server.TVService.Interfaces;
using Mediaportal.TV.Server.TVService.Interfaces.Enums;
using Mediaportal.TV.Server.TVService.Interfaces.Services;
using ILogger = MediaPortal.Common.Logging.ILogger;

namespace MediaPortal.Plugins.SlimTv.Service
{
  public class SlimTvService : ITvProvider, ITimeshiftControlEx, IProgramInfo, IChannelAndGroupInfo, IScheduleControl
  {
    const int MAX_WAIT_MS = 2000;
    public const string LOCAL_USERNAME = "Local";
    public const string TVDB_NAME = "MP2TVE";
    private TvServiceThread _tvServiceThread;
    protected readonly Dictionary<string, IUser> _tvUsers = new Dictionary<string, IUser>();
    protected Timer _timer;
    protected DbProviderFactory _dbProviderFactory;
    protected string _cloneConnection;


    public string Name
    {
      get { return "NativeTv Service"; }
    }

    public bool Init()
    {
      _timer = new Timer(500) { AutoReset = true };
      _timer.Elapsed += InitAsync;
      _timer.Start();
      return true;
    }

    private void InitAsync(object sender, ElapsedEventArgs args)
    {
      ISQLDatabase database;
      lock (_timer)
      {
        database = ServiceRegistration.Get<ISQLDatabase>(false);
        if (database == null)
          return;
        _timer.Close();
        _timer.Dispose();
      }

      using (var transaction = database.BeginTransaction())
        if (transaction.Connection.GetCloneFactory(TVDB_NAME, out _dbProviderFactory, out _cloneConnection))
        {
          EntityFrameworkHelper.AssureKnownFactory(_dbProviderFactory);
          // Register our factory to create new cloned connections
          ObjectContextManager.SetDbConnectionCreator(ClonedConnectionFactory);
        }

      IntegrationProviderHelper.Register(@"Plugins\SlimTv.Service", @"Plugins\SlimTv.Service\castle.config");
      _tvServiceThread = new TvServiceThread(Environment.GetCommandLineArgs()[0]);
      _tvServiceThread.Start();
    }

    /// <summary>
    /// Creates a new <see cref="DbConnection"/> on each request. This is used by the Tve35 EF model handling.
    /// </summary>
    /// <returns>Connection, still closed</returns>
    private DbConnection ClonedConnectionFactory ()
    {
      DbConnection connection = _dbProviderFactory.CreateConnection();
      if (connection == null)
        return null;
      connection.ConnectionString = _cloneConnection;
      return connection;
    }

    public bool DeInit()
    {
      if (_tvServiceThread != null)
      {
        _tvServiceThread.Stop(MAX_WAIT_MS);
        _tvServiceThread = null;
      }
      return true;
    }

    public bool StartTimeshift(int slotIndex, IChannel channel, out MediaItem timeshiftMediaItem)
    {
      throw new NotImplementedException("Not available in server side implementation");
    }

    public bool StopTimeshift(int slotIndex)
    {
      throw new NotImplementedException("Not available in server side implementation");
    }

    public bool StartTimeshift(string userName, int slotIndex, IChannel channel, out MediaItem timeshiftMediaItem)
    {
      string timeshiftFile = SwitchTVServerToChannel(GetUserName(userName, slotIndex), channel.ChannelId);
      timeshiftMediaItem = CreateMediaItem(slotIndex, timeshiftFile, channel);
      return true;
    }

    public bool StopTimeshift(string userName, int slotIndex)
    {
      IUser user;
      IInternalControllerService control = GlobalServiceProvider.Get<IInternalControllerService>();
      return control.StopTimeShifting(GetUserName(userName, slotIndex), out user);
    }

    public MediaItem CreateMediaItem(int slotIndex, string streamUrl, IChannel channel)
    {
      // Channel is usually only passed as placeholder with ID only, so query the details here
      IChannelService channelService = GlobalServiceProvider.Get<IChannelService>();
      Channel fullChannel = channelService.GetChannel(channel.ChannelId);

      bool isTv = fullChannel.MediaType == 0;
      LiveTvMediaItem tvStream = isTv
        ? SlimTvMediaItemBuilder.CreateMediaItem(slotIndex, streamUrl, fullChannel.ToChannel())
        : SlimTvMediaItemBuilder.CreateRadioMediaItem(slotIndex, streamUrl, fullChannel.ToChannel());

      if (tvStream != null)
      {
        // Add program infos to the LiveTvMediaItem
        IProgram currentProgram;
        IProgram nextProgram;
        if (GetNowNextProgram(channel, out currentProgram, out nextProgram))
        {
          tvStream.AdditionalProperties[LiveTvMediaItem.CURRENT_PROGRAM] = currentProgram;
          tvStream.AdditionalProperties[LiveTvMediaItem.NEXT_PROGRAM] = nextProgram;
        }
        return tvStream;
      }
      return null;
    }

    public IChannel GetChannel(int slotIndex)
    {
      // We do not manage all client channels here in server, this feature applies only to client side management!
      return null;
    }

    public bool GetNowNextProgram(IChannel channel, out IProgram programNow, out IProgram programNext)
    {
      programNow = null;
      programNext = null;
      IProgramService programService = GlobalServiceProvider.Get<IProgramService>();
      var programs = programService.GetNowAndNextProgramsForChannel(channel.ChannelId);
      var count = programs.Count;
      if (count >= 1)
        programNow = programs[0].ToProgram();
      if (count >= 2)
        programNext = programs[1].ToProgram();

      return programNow != null || programNext != null;
    }

    public bool GetPrograms(IChannel channel, DateTime from, DateTime to, out IList<IProgram> programs)
    {
      IProgramService programService = GlobalServiceProvider.Get<IProgramService>();
      programs = programService.GetProgramsByChannelAndStartEndTimes(channel.ChannelId, from, to)
        .Select(tvProgram => tvProgram.ToProgram(true))
        .ToList();
      return programs.Count > 0;
    }

    public bool GetProgramsGroup(IChannelGroup channelGroup, DateTime from, DateTime to, out IList<IProgram> programs)
    {
      IProgramService programService = GlobalServiceProvider.Get<IProgramService>();
      IChannelGroupService channelGroupService = GlobalServiceProvider.Get<IChannelGroupService>();

      var channels = channelGroupService.GetChannelGroup(channelGroup.ChannelGroupId).GroupMaps.Select(groupMap => groupMap.Channel);
      IDictionary<int, IList<Program>> programEntities = programService.GetProgramsForAllChannels(from, to, channels);

      programs = programEntities.Values.SelectMany(x => x).Select(p => p.ToProgram()).ToList();
      return programs.Count > 0;
    }

    public bool GetProgramsForSchedule(ISchedule schedule, out IList<IProgram> programs)
    {
      throw new NotImplementedException();
    }

    public bool GetScheduledPrograms(IChannel channel, out IList<IProgram> programs)
    {
      throw new NotImplementedException();
    }

    public bool GetChannel(IProgram program, out IChannel channel)
    {
      IChannelService channelService = GlobalServiceProvider.Get<IChannelService>();
      channel = channelService.GetChannel(program.ChannelId).ToChannel();
      return true;
    }

    public bool GetProgram(int programId, out IProgram program)
    {
      IProgramService programService = GlobalServiceProvider.Get<IProgramService>();
      program = programService.GetProgram(programId).ToProgram();
      return program != null;
    }

    public bool GetChannelGroups(out IList<IChannelGroup> groups)
    {
      IChannelGroupService channelGroupService = GlobalServiceProvider.Get<IChannelGroupService>();
      groups = channelGroupService.ListAllChannelGroups()
        .Select(tvGroup => tvGroup.ToChannelGroup())
        .ToList();
      return true;
    }

    public bool GetChannels(IChannelGroup group, out IList<IChannel> channels)
    {
      IChannelGroupService channelGroupService = GlobalServiceProvider.Get<IChannelGroupService>();
      channels = channelGroupService.GetChannelGroup(group.ChannelGroupId).GroupMaps
        .Select(groupMap => groupMap.Channel.ToChannel())
        .ToList();
      return true;
    }

    // This property applies only to client side management and is not used in server!
    public int SelectedChannelId { get; set; }

    // This property applies only to client side management and is not used in server!
    public int SelectedChannelGroupId { get; set; }

    public bool CreateSchedule(IProgram program)
    {
      IScheduleService scheduleService = GlobalServiceProvider.Get<IScheduleService>();
      Schedule schedule = ScheduleFactory.CreateSchedule(program.ChannelId, program.Title, program.StartTime, program.EndTime);
      schedule.PreRecordInterval = Int32.Parse(ServiceAgents.Instance.SettingServiceAgent.GetSettingWithDefaultValue("preRecordInterval", "5").Value);
      schedule.PostRecordInterval = Int32.Parse(ServiceAgents.Instance.SettingServiceAgent.GetSettingWithDefaultValue("postRecordInterval", "5").Value);
      scheduleService.SaveSchedule(schedule);
      return true;
    }

    public bool RemoveSchedule(IProgram program)
    {
      IScheduleService scheduleService = GlobalServiceProvider.Get<IScheduleService>();
      ICanceledScheduleService canceledScheduleService = GlobalServiceProvider.Get<ICanceledScheduleService>();
      var allSchedules = scheduleService.ListAllSchedules()
        .Where(schedule =>
          schedule.IdChannel == program.ChannelId &&
          schedule.StartTime == program.StartTime &&
          schedule.EndTime == program.EndTime);
      foreach (Schedule schedule in allSchedules)
      {
        switch (schedule.ScheduleType)
        {
          case (int) ScheduleRecordingType.Once:
            scheduleService.DeleteSchedule(schedule.IdSchedule);
            break;
          default:
            CanceledSchedule canceledSchedule = CanceledScheduleFactory.CreateCanceledSchedule(schedule.IdSchedule, schedule.IdChannel, schedule.StartTime);
            canceledScheduleService.SaveCanceledSchedule(canceledSchedule);
            break;
        }
      }
      return true;
    }

    public bool GetRecordingStatus(IProgram program, out RecordingStatus recordingStatus)
    {
      IProgramService programService = GlobalServiceProvider.Get<IProgramService>();
      IProgramRecordingStatus recProgram = (IProgramRecordingStatus) programService.GetProgram(program.ProgramId).ToProgram(true);
      recordingStatus = recProgram.RecordingStatus;
      return true;
    }

    private string SwitchTVServerToChannel(string userName, int channelId)
    {
      if (String.IsNullOrEmpty(userName))
      {
        ServiceRegistration.Get<ILogger>().Error("Called SwitchTVServerToChannel with empty userName");
        throw new ArgumentNullException("userName");
      }

      IUser currentUser = UserFactory.CreateBasicUser(userName, -1);
      ServiceRegistration.Get<ILogger>().Debug("Starting timeshifiting with username {0} on channel id {1}", userName, channelId);

      IInternalControllerService control = GlobalServiceProvider.Get<IInternalControllerService>();

      IVirtualCard card;
      IUser user;
      TvResult result = control.StartTimeShifting(currentUser.Name, channelId, out card, out user);
      ServiceRegistration.Get<ILogger>().Debug("Tried to start timeshifting, result {0}", result);

      if (result != TvResult.Succeeded)
      {
        // TODO: should we retry?
        ServiceRegistration.Get<ILogger>().Error("Starting timeshifting failed with result {0}", result);
        throw new Exception("Failed to start tv stream: " + result);
      }
      return userName.StartsWith(LOCAL_USERNAME + "-") ? card.TimeShiftFileName : card.RTSPUrl;
    }

    protected IUser GetUserByUserName(string userName, bool create = false)
    {
      if (userName == null)
      {
        ServiceRegistration.Get<ILogger>().Warn("Used user with null name");
        return null;
      }

      if (!_tvUsers.ContainsKey(userName) && !create)
        return null;

      if (!_tvUsers.ContainsKey(userName) && create)
        _tvUsers.Add(userName, new User(userName, UserType.Normal));

      return _tvUsers[userName];
    }

    private static string GetUserName(string clientName, int slotIndex)
    {
      return string.Format("{0}-{1}", clientName, slotIndex);
    }
  }
}
