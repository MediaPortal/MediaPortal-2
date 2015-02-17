#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

#define TVE3

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Timers;
using MediaPortal.Backend.Database;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.SystemResolver;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.Plugins.SlimTv.Interfaces.LiveTvMediaItem;
using MediaPortal.Plugins.SlimTv.Interfaces.ResourceProvider;
using MediaPortal.Plugins.SlimTv.Interfaces.UPnP.Items;
using Mediaportal.TV.Server.TVLibrary.IntegrationProvider.Interfaces;
using MediaPortal.Utilities;
using TvControl;
using TvDatabase;
using TvEngine.Events;
using TvLibrary.Interfaces;
using TvLibrary.Interfaces.Integration;
using TvService;
using IChannel = MediaPortal.Plugins.SlimTv.Interfaces.Items.IChannel;
using ILogger = MediaPortal.Common.Logging.ILogger;
using IPathManager = MediaPortal.Common.PathManager.IPathManager;
using ScheduleRecordingType = MediaPortal.Plugins.SlimTv.Interfaces.ScheduleRecordingType;
using Timer = System.Timers.Timer;
#if !TVE3
using MediaPortal.Common.PathManager;
using MediaPortal.Common.Utils;
using MediaPortal.Plugins.SlimTv.Service.Helpers;
using Mediaportal.TV.Server.TVControl;
using Mediaportal.TV.Server.TVControl.Events;
using Mediaportal.TV.Server.TVControl.Interfaces.Events;
using Mediaportal.TV.Server.TVControl.Interfaces.Services;
using Mediaportal.TV.Server.TVControl.ServiceAgents;
using Mediaportal.TV.Server.TVDatabase.Entities;
using Mediaportal.TV.Server.TVDatabase.Entities.Factories;
using Mediaportal.TV.Server.TVDatabase.EntityModel.ObjContext;
using Mediaportal.TV.Server.TVDatabase.TVBusinessLayer;
using Mediaportal.TV.Server.TVDatabase.TVBusinessLayer.Entities;
using Mediaportal.TV.Server.TVLibrary;
using Mediaportal.TV.Server.TVLibrary.Interfaces.Integration;
using Mediaportal.TV.Server.TVService.Interfaces;
using Mediaportal.TV.Server.TVService.Interfaces.Enums;
using Mediaportal.TV.Server.TVService.Interfaces.Services;
using Channel = Mediaportal.TV.Server.TVDatabase.Entities.Channel;
using Program = Mediaportal.TV.Server.TVDatabase.Entities.Program;
using Schedule = Mediaportal.TV.Server.TVDatabase.Entities.Schedule;
#endif

namespace MediaPortal.Plugins.SlimTv.Service
{
  public class SlimTvService : ITvProvider, ITimeshiftControlEx, IProgramInfo, IChannelAndGroupInfo, IScheduleControl
  {
    const int MAX_WAIT_MS = 10000;
    public const string LOCAL_USERNAME = "Local";
    public const string TVDB_NAME = "MP2TVE";
    private TvServiceThread _tvServiceThread;
    protected readonly Dictionary<string, IUser> _tvUsers = new Dictionary<string, IUser>();
    protected Timer _timer;
    protected DbProviderFactory _dbProviderFactory;
    protected string _cloneConnection;

#if TVE3
    protected IController _tvControl;
    protected TvBusinessLayer _tvBusiness;
    protected Thread _serviceThread;
#endif

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

    #region Database and program data initialization

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
      {
        // Prepare TV database if required.
        PrepareTvDatabase(transaction);

#if !TVE3
        if (transaction.Connection.GetCloneFactory(TVDB_NAME, out _dbProviderFactory, out _cloneConnection))
        {
          EntityFrameworkHelper.AssureKnownFactory(_dbProviderFactory);
          // Register our factory to create new cloned connections
          ObjectContextManager.SetDbConnectionCreator(ClonedConnectionFactory);
        }
#endif
      }

      IntegrationProviderHelper.Register(@"Plugins\SlimTv.Service", @"Plugins\SlimTv.Service\castle.config");
      var pm = GlobalServiceProvider.Instance.Get<IIntegrationProvider>().PathManager;

      // Needs to be done after the IntegrationProvider is registered, so the TVCORE folder is defined.
      PrepareProgramData();

      _tvServiceThread = new TvServiceThread(Environment.GetCommandLineArgs()[0]);
#if TVE3
      InitializeGentle();
      Start();
#else
      _tvServiceThread.Start();
#endif
      if (!_tvServiceThread.InitializedEvent.WaitOne(MAX_WAIT_MS))
      {
        ServiceRegistration.Get<ILogger>().Error("SlimTvService: Failed to start TV service thread within {0} seconds.", MAX_WAIT_MS / 1000);
      }

#if TVE3
      InitializeTVE();
#endif
      // Handle events from TvEngine
      if (!RegisterEvents())
      {
        ServiceRegistration.Get<ILogger>().Error("SlimTvService: Failed to register events. This happens only if startup failed. Stopping plugin now.");
        DeInit();
      }
    }

#if TVE3

    public void Start()
    {
      var tvServiceThreadStart = new ThreadStart(() =>
      {
        try
        {
          _tvServiceThread.OnStart();
        }
        catch
        {
          // Only exit the process if the caller forces this behavior.
        }
      });
      _serviceThread = new Thread(tvServiceThreadStart) { IsBackground = false };
      _serviceThread.Start();
    }

    public void Stop(int maxWaitMsecs)
    {
      if (_serviceThread != null && _serviceThread.IsAlive)
      {
        bool joined = _serviceThread.Join(maxWaitMsecs);
        if (!joined)
        {
          _serviceThread.Abort();
          _serviceThread.Join();
        }
        _tvServiceThread = null;
      }
    }

    private void InitializeGentle()
    {
      try
      {
        // Use the same Gentle.config as the TVEngine
        string gentleConfigFile = Path.Combine(ServiceRegistration.Get<IPathManager>().GetPath("<TVCORE>"), "Gentle.config");
        // but be quiet when it doesn't exists, as not everyone has the TV Engine installed
        if (!File.Exists(gentleConfigFile))
        {
          ServiceRegistration.Get<ILogger>().Info("Cannot find Gentle.config file, assuming TVEngine isn't installed...");
          return;
        }
        Gentle.Common.Configurator.AddFileHandler(gentleConfigFile);
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("Failed to connect to TVEngine", ex);
      }
    }

    private void InitializeTVE()
    {
      try
      {
        _tvControl = _tvServiceThread.Controller;
        _tvBusiness = new TvBusinessLayer();
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("Failed to connect to TVEngine", ex);
      }
    }
#endif

    /// <summary>
    /// Prepares the required data folders for first run. The required tuningdetails and other files are extracted to [TVCORE] path.
    /// </summary>
    private void PrepareProgramData()
    {
      // Morpheus_xx, 2014-09-01: As soon as our extension installer is able to place files in different target folders, this code can be removed.
      const string ini = "MPIPTVSource.ini";
      string iniPath = Utilities.FileSystem.FileUtils.BuildAssemblyRelativePath("ProgramData\\" + ini);
      string mp2DataPath = ServiceRegistration.Get<IPathManager>().GetPath("<DATA>");
      try
      {
        var destFileName = Path.Combine(mp2DataPath, ini);
        if (!File.Exists(destFileName) && File.Exists(iniPath))
        {
          ServiceRegistration.Get<ILogger>().Info("SlimTvService: {0} does not exist yet, copy file.", destFileName);
          File.Copy(iniPath, destFileName);
        }
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("SlimTvService: Failed to copy {0}!", ex, ini);
      }

      string dataPath = ServiceRegistration.Get<IPathManager>().GetPath("<TVCORE>");
      string tuningDetails = Path.Combine(dataPath, "TuningParameters");
#if TVE3
      string gentleConfigFile = Path.Combine(ServiceRegistration.Get<IPathManager>().GetPath("<TVCORE>"), "Gentle.config");
      if (File.Exists(gentleConfigFile) && Directory.Exists(tuningDetails))
        return;
#else
      if (Directory.Exists(tuningDetails))
        return;
#endif

      ServiceRegistration.Get<ILogger>().Info("SlimTvService: Tuningdetails folder does not exist yet, extracting default items.");
      try
      {
        ZipFile.ExtractToDirectory(Utilities.FileSystem.FileUtils.BuildAssemblyRelativePath("ProgramData\\ProgramData.zip"), dataPath);
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("SlimTvService: Failed to extract Tuningdetails!", ex);
      }
    }

    /// <summary>
    /// Prepares the database for SlimTV if required. This is only the case for SQLite mode, where we supply an empty template DB.
    /// </summary>
    /// <param name="transaction"></param>
    private void PrepareTvDatabase(ITransaction transaction)
    {
      // We only need custom logic for SQLite here.
      if (!transaction.Connection.GetType().ToString().Contains("SQLite"))
        return;
      string targetPath = ServiceRegistration.Get<IPathManager>().GetPath("<DATABASE>");
      string databaseTemplate = Utilities.FileSystem.FileUtils.BuildAssemblyRelativePath("Database");
      if (!Directory.Exists(databaseTemplate))
        return;

      ServiceRegistration.Get<ILogger>().Info("SlimTvService: Checking database template files.");
      try
      {
        foreach (var file in Directory.GetFiles(databaseTemplate))
        {
          string targetFile = Path.Combine(targetPath, Path.GetFileName(file));
          if (!File.Exists(targetFile))
          {
            File.Copy(file, targetFile);
            ServiceRegistration.Get<ILogger>().Info("SlimTvService: Sucessfully copied database template file {0}", file);
          }
        }
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("SlimTvService: Failed to copy database template!", ex);
      }
    }

#if !TVE3
    /// <summary>
    /// Creates a new <see cref="DbConnection"/> on each request. This is used by the Tve35 EF model handling.
    /// </summary>
    /// <returns>Connection, still closed</returns>
    private DbConnection ClonedConnectionFactory()
    {
      DbConnection connection = _dbProviderFactory.CreateConnection();
      if (connection == null)
        return null;
      connection.ConnectionString = _cloneConnection;
      return connection;
    }
#endif

    public bool DeInit()
    {
#if TVE3
      Stop(MAX_WAIT_MS);
#else
      if (_tvServiceThread != null)
      {
        _tvServiceThread.OnStop();
        _tvServiceThread = null;
      }
#endif
      return true;
    }

    #endregion

    #region Recordings / MediaLibrary synchronization

    protected bool RegisterEvents()
    {
      ITvServerEvent tvServerEvent = GlobalServiceProvider.Instance.TryGet<ITvServerEvent>();
      if (tvServerEvent == null)
        return false;
      tvServerEvent.OnTvServerEvent += OnTvServerEvent;
      return true;
    }

    protected void OnTvServerEvent(object sender, EventArgs eventArgs)
    {
      try
      {
        TvServerEventArgs tvEvent = (TvServerEventArgs)eventArgs;

        if (tvEvent.EventType == TvServerEventType.RecordingEnded)
        {
#if TVE3
          var recording = Recording.Retrieve(tvEvent.Recording.IdRecording);
#else
          var recording = ServiceAgents.Instance.RecordingServiceAgent.GetRecording(tvEvent.Recording);
#endif
          if (recording != null)
          {
            ServiceRegistration.Get<ILogger>().Info("SlimTvService: Recording ended: {0}", recording.FileName);
            ImportRecording(recording.FileName);
          }
        }
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Warn("SlimTvService: Exception while handling TvServerEvent", ex);
      }
    }

    protected void ImportRecording(string fileName)
    {
      ISystemResolver systemResolver = ServiceRegistration.Get<ISystemResolver>();
      IMediaLibrary mediaLibrary = ServiceRegistration.Get<IMediaLibrary>();

      List<Share> possibleShares = new List<Share>(); // Shares can point to different depth, we try to find the deepest one
      foreach (var share in mediaLibrary.GetShares(systemResolver.LocalSystemId).Values)
      {
        var dir = LocalFsResourceProviderBase.ToDosPath(share.BaseResourcePath.LastPathSegment.Path);
        if (dir != null && fileName.StartsWith(dir, StringComparison.InvariantCultureIgnoreCase))
          possibleShares.Add(share);
      }
      if (possibleShares.Count == 0)
      {
        ServiceRegistration.Get<ILogger>().Warn("SlimTvService: Received notifaction of new recording but could not find a media source. Have you added recordings folder as media source? File: {0}", fileName);
        return;
      }

      Share usedShare = possibleShares.OrderByDescending(s => s.BaseResourcePath.LastPathSegment.Path.Length).First();
      IImporterWorker importerWorker = ServiceRegistration.Get<IImporterWorker>();
      importerWorker.ScheduleImport(LocalFsResourceProviderBase.ToResourcePath(fileName), usedShare.MediaCategories, false);
    }

    #endregion

    #region ITvProvider implementation

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

#if TVE3
    private IUser GetUserByUserName(string userName)
    {
      return Card.ListAll()
        .Where(c => c != null && c.Enabled)
        .SelectMany(c => { var users = _tvControl.GetUsersForCard(c.IdCard); return users ?? new IUser[] { }; })
        .FirstOrDefault(u => u.Name == userName);
    }
#endif

    public bool StopTimeshift(string userName, int slotIndex)
    {
      IUser user;
#if TVE3
      user = GetUserByUserName(GetUserName(userName, slotIndex));
      if (user == null)
        return false;
      return _tvControl.StopTimeShifting(ref user);
#else
      IInternalControllerService control = GlobalServiceProvider.Instance.Get<IInternalControllerService>();
      return control.StopTimeShifting(GetUserName(userName, slotIndex), out user);
#endif
    }

    public MediaItem CreateMediaItem(int slotIndex, string streamUrl, IChannel channel)
    {
      // Channel is usually only passed as placeholder with ID only, so query the details here
#if TVE3
      TvDatabase.Channel fullChannel = TvDatabase.Channel.Retrieve(channel.ChannelId);
      bool isTv = fullChannel.IsTv;
#else
      IChannelService channelService = GlobalServiceProvider.Instance.Get<IChannelService>();
      Channel fullChannel = channelService.GetChannel(channel.ChannelId);
      bool isTv = fullChannel.MediaType == 0;
#endif

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
#if TVE3
      var tvChannel = TvDatabase.Channel.Retrieve(channel.ChannelId);
      programNow = tvChannel.CurrentProgram.ToProgram();
      programNext = tvChannel.NextProgram.ToProgram();
#else
      IProgramService programService = GlobalServiceProvider.Instance.Get<IProgramService>();
      var programs = programService.GetNowAndNextProgramsForChannel(channel.ChannelId).Select(p => p.ToProgram()).Distinct(ProgramComparer.Instance).ToList();
      var count = programs.Count;
      if (count >= 1)
        programNow = programs[0];
      if (count >= 2)
        programNext = programs[1];
#endif
      return programNow != null || programNext != null;
    }

    public bool GetNowAndNextForChannelGroup(IChannelGroup channelGroup, out IDictionary<int, IProgram[]> nowNextPrograms)
    {
      nowNextPrograms = new Dictionary<int, IProgram[]>();
      IList<IChannel> channels;
      if (!GetChannels(channelGroup, out channels))
        return false;

      foreach (IChannel channel in channels)
      {
        IProgram programNow;
        IProgram programNext;
        if (GetNowNextProgram(channel, out programNow, out programNext))
          nowNextPrograms[channel.ChannelId] = new[] { programNow, programNext };
      }
      return true;
    }

    public bool GetPrograms(IChannel channel, DateTime from, DateTime to, out IList<IProgram> programs)
    {
#if TVE3
      programs = _tvBusiness.GetPrograms(TvDatabase.Channel.Retrieve(channel.ChannelId), from, to)
#else
      IProgramService programService = GlobalServiceProvider.Instance.Get<IProgramService>();
      programs = programService.GetProgramsByChannelAndStartEndTimes(channel.ChannelId, from, to)
#endif
.Select(tvProgram => tvProgram.ToProgram(true))
        .Distinct(ProgramComparer.Instance)
        .ToList();
      return programs.Count > 0;
    }

    public bool GetPrograms(string title, DateTime from, DateTime to, out IList<IProgram> programs)
    {
#if TVE3
      programs = _tvBusiness.SearchPrograms(title).Where(p => p.StartTime >= from && p.StartTime <= to || p.EndTime >= from && p.EndTime <= to)
#else
      IProgramService programService = GlobalServiceProvider.Instance.Get<IProgramService>();
      programs = programService.GetProgramsByTitleAndStartEndTimes(title, from, to)
#endif
.Select(tvProgram => tvProgram.ToProgram(true))
        .Distinct(ProgramComparer.Instance)
        .ToList();
      return programs.Count > 0;
    }

    public bool GetProgramsGroup(IChannelGroup channelGroup, DateTime from, DateTime to, out IList<IProgram> programs)
    {
#if TVE3
      programs = new List<IProgram>();
      foreach (var channel in _tvBusiness.GetTVGuideChannelsForGroup(channelGroup.ChannelGroupId))
        CollectionUtils.AddAll(programs, _tvBusiness.GetPrograms(TvDatabase.Channel.Retrieve(channel.IdChannel), from, to).Select(p => p.ToProgram()));
#else
      IProgramService programService = GlobalServiceProvider.Instance.Get<IProgramService>();
      IChannelGroupService channelGroupService = GlobalServiceProvider.Instance.Get<IChannelGroupService>();

      var channels = channelGroupService.GetChannelGroup(channelGroup.ChannelGroupId).GroupMaps.Select(groupMap => groupMap.Channel);
      IDictionary<int, IList<Program>> programEntities = programService.GetProgramsForAllChannels(from, to, channels);

      programs = programEntities.Values.SelectMany(x => x).Select(p => p.ToProgram()).Distinct(ProgramComparer.Instance).ToList();
#endif
      return programs.Count > 0;
    }

    public bool GetProgramsForSchedule(ISchedule schedule, out IList<IProgram> programs)
    {
#if TVE3
      programs = new List<IProgram>();
      var tvSchedule = TvDatabase.Schedule.Retrieve(schedule.ScheduleId);
      if (tvSchedule == null)
        return false;

      programs = TvDatabase.Schedule.GetProgramsForSchedule(tvSchedule).Select(p => p.ToProgram()).ToList();
      return programs.Count > 0;
#else
      Schedule scheduleEntity = ScheduleManagement.GetSchedule(schedule.ScheduleId);
      if (scheduleEntity == null)
        return false;
      IList<Program> programEntities = ProgramManagement.GetProgramsForSchedule(scheduleEntity);
      programs = programEntities.Select(p => p.ToProgram()).Distinct(ProgramComparer.Instance).ToList();
      return true;
#endif
    }

    public bool GetScheduledPrograms(IChannel channel, out IList<IProgram> programs)
    {
      throw new NotImplementedException();
    }

    public bool GetChannel(IProgram program, out IChannel channel)
    {
#if TVE3
      channel = TvDatabase.Channel.Retrieve(program.ChannelId).ToChannel();
#else
      IChannelService channelService = GlobalServiceProvider.Instance.Get<IChannelService>();
      channel = channelService.GetChannel(program.ChannelId).ToChannel();
#endif
      return true;
    }

    public bool GetProgram(int programId, out IProgram program)
    {
#if TVE3
      program = TvDatabase.Program.Retrieve(programId).ToProgram();
#else
      IProgramService programService = GlobalServiceProvider.Instance.Get<IProgramService>();
      program = programService.GetProgram(programId).ToProgram();
#endif
      return program != null;
    }

    public bool GetChannelGroups(out IList<IChannelGroup> groups)
    {
#if TVE3
      // TODO: only TV groups affected here
      groups = TvDatabase.ChannelGroup.ListAll()
        .OrderBy(tvGroup => tvGroup.SortOrder)
#else
      IChannelGroupService channelGroupService = GlobalServiceProvider.Instance.Get<IChannelGroupService>();
      groups = channelGroupService.ListAllChannelGroups()
        .OrderBy(tvGroup => tvGroup.MediaType)
        .ThenBy(tvGroup => tvGroup.SortOrder)
#endif
.Select(tvGroup => tvGroup.ToChannelGroup())
        .ToList();
      return true;
    }

    public bool GetChannel(int channelId, out IChannel channel)
    {
#if TVE3
      channel = TvDatabase.Channel.Retrieve(channelId).ToChannel();
#else
      IChannelService channelGroupService = GlobalServiceProvider.Instance.Get<IChannelService>();
      channel = channelGroupService.GetChannel(channelId).ToChannel();
#endif
      return true;
    }

    public bool GetChannels(IChannelGroup group, out IList<IChannel> channels)
    {
#if TVE3
      channels = _tvBusiness.GetChannelsInGroup(TvDatabase.ChannelGroup.Retrieve(group.ChannelGroupId))
        // Bug? SortOrder contains logical channel number, not the group sort order?
        // .OrderBy(c => c.SortOrder)
        .Where(c=> c.VisibleInGuide)
        .Select(c => c.ToChannel())
        .ToList();
#else
      IChannelGroupService channelGroupService = GlobalServiceProvider.Instance.Get<IChannelGroupService>();
      channels = channelGroupService.GetChannelGroup(group.ChannelGroupId).GroupMaps
        .Where(groupMap => groupMap.Channel.VisibleInGuide)
        .OrderBy(groupMap => groupMap.SortOrder)
        .Select(groupMap => groupMap.Channel.ToChannel())
        .ToList();
#endif
      return true;
    }

    // This property applies only to client side management and is not used in server!
    public int SelectedChannelId { get; set; }

    // This property applies only to client side management and is not used in server!
    public int SelectedChannelGroupId { get; set; }

    public bool GetSchedules(out IList<ISchedule> schedules)
    {
#if TVE3
      schedules = TvDatabase.Schedule.ListAll().Select(s => s.ToSchedule()).ToList();
#else
      IScheduleService scheduleService = GlobalServiceProvider.Instance.Get<IScheduleService>();
      schedules = scheduleService.ListAllSchedules().Select(s => s.ToSchedule()).ToList();
#endif
      return true;
    }

    public bool CreateSchedule(IProgram program, ScheduleRecordingType recordingType, out ISchedule schedule)
    {
#if TVE3
      TvDatabase.Schedule tvSchedule = _tvBusiness.AddSchedule(program.ChannelId, program.Title, program.StartTime, program.EndTime, (int)recordingType);
      tvSchedule.ScheduleType = (int)recordingType;
      tvSchedule.PreRecordInterval = Int32.Parse(_tvBusiness.GetSetting("preRecordInterval", "5").Value);
      tvSchedule.PostRecordInterval = Int32.Parse(_tvBusiness.GetSetting("postRecordInterval", "5").Value);
      tvSchedule.Persist();
      _tvControl.OnNewSchedule();
      schedule = tvSchedule.ToSchedule();
      return true;
#else
      IScheduleService scheduleService = GlobalServiceProvider.Instance.Get<IScheduleService>();
      Schedule tvschedule = ScheduleFactory.CreateSchedule(program.ChannelId, program.Title, program.StartTime, program.EndTime);
      tvschedule.PreRecordInterval = ServiceAgents.Instance.SettingServiceAgent.GetValue("preRecordInterval", 5);
      tvschedule.PostRecordInterval = ServiceAgents.Instance.SettingServiceAgent.GetValue("postRecordInterval", 5);
      tvschedule.ScheduleType = (int)recordingType;
      scheduleService.SaveSchedule(tvschedule);
      schedule = tvschedule.ToSchedule();
      return true;
#endif
    }


    public bool CreateScheduleByTime(IChannel channel, DateTime from, DateTime to, out ISchedule schedule)
    {
#if TVE3
      TvDatabase.Schedule tvSchedule = _tvBusiness.AddSchedule(channel.ChannelId, "Manual", from, to, (int)ScheduleRecordingType.Once);
      tvSchedule.PreRecordInterval = Int32.Parse(_tvBusiness.GetSetting("preRecordInterval", "5").Value);
      tvSchedule.PostRecordInterval = Int32.Parse(_tvBusiness.GetSetting("postRecordInterval", "5").Value);
      tvSchedule.Persist();
      _tvControl.OnNewSchedule();
#else
      IScheduleService scheduleService = GlobalServiceProvider.Get<IScheduleService>();
      Schedule tvschedule = ScheduleFactory.CreateSchedule(channel.ChannelId, "Manual", from, to);
      tvschedule.PreRecordInterval = ServiceAgents.Instance.SettingServiceAgent.GetValue("preRecordInterval", 5);
      tvschedule.PostRecordInterval = ServiceAgents.Instance.SettingServiceAgent.GetValue("postRecordInterval", 5);
      tvschedule.ScheduleType = (int)ScheduleRecordingType.Once;
      scheduleService.SaveSchedule(tvschedule);
#endif
      schedule = tvSchedule.ToSchedule();
      return true;
    }

    public bool RemoveScheduleForProgram(IProgram program, ScheduleRecordingType recordingType)
    {
#if TVE3
      var canceledProgram = TvDatabase.Program.Retrieve(program.ProgramId);
      if (canceledProgram == null)
        return false;
      foreach (TvDatabase.Schedule schedule in TvDatabase.Schedule.ListAll().Where(schedule => schedule.IsRecordingProgram(canceledProgram, true)))
      {
        switch (schedule.ScheduleType)
        {
          case (int)ScheduleRecordingType.Once:
            schedule.Delete();
            _tvControl.OnNewSchedule();
            break;
          default:
            CanceledSchedule canceledSchedule = new CanceledSchedule(schedule.IdSchedule, schedule.IdChannel, schedule.StartTime);
            canceledSchedule.Persist();
            _tvControl.OnNewSchedule();
            break;
        }
      }
      return true;
#else
      IScheduleService scheduleService = GlobalServiceProvider.Instance.Get<IScheduleService>();
      IProgramService programService = GlobalServiceProvider.Instance.Get<IProgramService>();
      var canceledProgram = programService.GetProgram(program.ProgramId);
      if (canceledProgram == null)
        return false;

      foreach (Schedule schedule in scheduleService.ListAllSchedules().Where(schedule => new ScheduleBLL(schedule).IsRecordingProgram(canceledProgram, true)))
      {
        switch (schedule.ScheduleType)
        {
          case (int)ScheduleRecordingType.Once:
            scheduleService.DeleteSchedule(schedule.IdSchedule);
            break;
          default:
            // If only single program should be canceled
            if (recordingType == ScheduleRecordingType.Once)
            {
              CancelSingleSchedule(schedule, canceledProgram);
            }
            // Full schedule is canceled, including all programs
            else
            {
              CancelFullSchedule(schedule);
            }
            break;
        }
      }
      return true;
#endif
    }

    public bool RemoveSchedule(ISchedule schedule)
    {
#if TVE3
      TvDatabase.Schedule tvSchedule = TvDatabase.Schedule.Retrieve(schedule.ScheduleId);
      _tvControl.StopRecordingSchedule(tvSchedule.IdSchedule);
      // delete canceled schedules first
      foreach (var cs in CanceledSchedule.ListAll().Where(x => x.IdSchedule == tvSchedule.IdSchedule))
        cs.Remove();
      tvSchedule.Remove();
      _tvControl.OnNewSchedule(); // I don't think this is needed, but doesn't hurt either
#else
      IScheduleService scheduleService = GlobalServiceProvider.Instance.Get<IScheduleService>();
      if (scheduleService == null)
        return false;

      scheduleService.DeleteSchedule(schedule.ScheduleId);
#endif
      return true;
    }

#if !TVE3
    private static void CancelSingleSchedule(Schedule schedule, Program canceledProgram)
    {
      ICanceledScheduleService canceledScheduleService = GlobalServiceProvider.Instance.Get<ICanceledScheduleService>();

      CanceledSchedule canceledSchedule = CanceledScheduleFactory.CreateCanceledSchedule(schedule.IdSchedule, canceledProgram.IdChannel, canceledProgram.StartTime);
      canceledScheduleService.SaveCanceledSchedule(canceledSchedule);
      StopRecording(schedule);
    }

    private void CancelFullSchedule(Schedule schedule)
    {
      Schedule currentSchedule = schedule;
      Schedule parentSchedule = null;
      GetParentAndSpawnSchedule(ref currentSchedule, out parentSchedule);
      StopRecording(currentSchedule);
      DeleteEntireOrOnceSchedule(currentSchedule, parentSchedule);
    }

    private static void GetParentAndSpawnSchedule(ref Schedule schedule, out Schedule parentSchedule)
    {
      parentSchedule = schedule.ParentSchedule;
      if (parentSchedule != null)
        return;

      parentSchedule = schedule;
      Schedule spawn = ServiceAgents.Instance.ScheduleServiceAgent.RetrieveSpawnedSchedule(parentSchedule.IdSchedule, parentSchedule.StartTime);
      if (spawn != null)
        schedule = spawn;
    }

    private static bool StopRecording(Schedule schedule)
    {
      bool stoppedRec = false;
      bool isRec = ServiceAgents.Instance.ScheduleServiceAgent.IsScheduleRecording(schedule.IdSchedule);
      if (isRec)
      {
        ServiceAgents.Instance.ControllerServiceAgent.StopRecordingSchedule(schedule.IdSchedule);
        stoppedRec = true;
      }
      return stoppedRec;
    }

    private bool DeleteEntireOrOnceSchedule(Schedule schedule, Schedule parentSchedule)
    {
      //is the schedule recording, then stop it now.
      bool wasDeleted = false;
      foreach (var currentSchedule in new List<Schedule> { schedule, parentSchedule })
      {
        try
        {
          if (currentSchedule != null)
            wasDeleted |= DeleteSchedule(currentSchedule.IdSchedule);
        }
        catch (Exception ex)
        {
          ServiceRegistration.Get<ILogger>().Error("Error deleting schedule with ID '{0}'", ex,
            currentSchedule != null ? currentSchedule.IdSchedule.ToString() : "<null>");
        }
      }
      return wasDeleted;
    }

    private bool DeleteSchedule(int idSchedule)
    {
      Schedule schedule = ServiceAgents.Instance.ScheduleServiceAgent.GetSchedule(idSchedule);
      if (schedule == null)
        return false;

      ServiceAgents.Instance.ScheduleServiceAgent.DeleteSchedule(schedule.IdSchedule);
      return true;
    }
#endif

    public bool GetRecordingStatus(IProgram program, out RecordingStatus recordingStatus)
    {
#if TVE3
      var tvProgram = (IProgramRecordingStatus)TvDatabase.Program.Retrieve(program.ProgramId).ToProgram(true);
      recordingStatus = tvProgram.RecordingStatus;
#else
      IProgramService programService = GlobalServiceProvider.Instance.Get<IProgramService>();
      IProgramRecordingStatus recProgram = (IProgramRecordingStatus)programService.GetProgram(program.ProgramId).ToProgram(true);
      recordingStatus = recProgram.RecordingStatus;
#endif
      return true;
    }

    public bool GetRecordingFileOrStream(IProgram program, out string fileOrStream)
    {
      fileOrStream = null;
      Recording recording;
      if (!GetRecording(program, out recording))
        return false;

      fileOrStream = recording.FileName; // FileName represents a local filesystem path on the server. It cannot be used directly in multiseat (RTSP required).
      return true;
    }

    private static bool GetRecording(IProgram program, out Recording recording)
    {
#if TVE3
      recording = TvDatabase.Recording.ListAll().FirstOrDefault(r => r.IsRecording && r.IdChannel == program.ChannelId && r.Title == program.Title);
#else
      IRecordingService recordingService = GlobalServiceProvider.Instance.Get<IRecordingService>();
      recording = recordingService.GetActiveRecordingByTitleAndChannel(program.Title, program.ChannelId);
#endif
      return recording != null;
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

#if TVE3
      // actually start timeshifting
      VirtualCard card;
      TvResult result = _tvControl.StartTimeShifting(ref currentUser, channelId, out card);
      // make sure result is correct and return
      if (result != TvResult.Succeeded)
      {
        ServiceRegistration.Get<ILogger>().Error("Starting timeshifting failed with result {0}", result);
        return null;
      }
      if (card == null)
      {
        ServiceRegistration.Get<ILogger>().Error("Couldn't get virtual card");
        return null;
      }
#else
      IInternalControllerService control = GlobalServiceProvider.Instance.Get<IInternalControllerService>();

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
#endif
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

#if TVE3
      if (!_tvUsers.ContainsKey(userName) && create)
        _tvUsers.Add(userName, new User(userName, false));
#else
      if (!_tvUsers.ContainsKey(userName) && create)
        _tvUsers.Add(userName, new User(userName, UserType.Normal));
#endif
      return _tvUsers[userName];
    }

    private static string GetUserName(string clientName, int slotIndex)
    {
      return string.Format("{0}-{1}", clientName, slotIndex);
    }

    #endregion
  }
}
