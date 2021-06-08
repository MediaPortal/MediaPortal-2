#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using MediaPortal.Backend.ClientCommunication;
using MediaPortal.Backend.Database;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.Async;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.SystemResolver;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.Plugins.SlimTv.Interfaces.LiveTvMediaItem;
using MediaPortal.Plugins.SlimTv.Interfaces.ResourceProvider;
using MediaPortal.Utilities.FileSystem;
using IChannel = MediaPortal.Plugins.SlimTv.Interfaces.Items.IChannel;
using ILogger = MediaPortal.Common.Logging.ILogger;
using IPathManager = MediaPortal.Common.PathManager.IPathManager;
using ScheduleRecordingType = MediaPortal.Plugins.SlimTv.Interfaces.ScheduleRecordingType;
using MediaPortal.Common.Runtime;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.Services.Settings;
using MediaPortal.Plugins.SlimTv.Interfaces.Settings;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using System.Threading;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.Services.GenreConverter;
using MediaPortal.Plugins.SlimTv.Interfaces.UPnP.Items;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.Settings;
using MediaPortal.Plugins.SlimTv.UPnP;
using MediaPortal.Utilities;
using MediaPortal.Utilities.Threading;

namespace MediaPortal.Plugins.SlimTv.Service
{
  public abstract class AbstractSlimTvService<TChannelGroup, TChannel, TProgram, TSchedule, TScheduleRule, TRecording, TTuningDetail, TConflict> : 
    ITvProvider, ITimeshiftControlEx, IProgramInfoAsync, IChannelAndGroupInfoAsync, IScheduleControlAsync, IScheduleRuleControlAsync, ITunerInfoAsync, IConflictInfoAsync, IMessageReceiver
  {
    public static readonly MediaCategory Series = new MediaCategory("Series", null);
    public static readonly MediaCategory Movie = new MediaCategory("Movie", null);

    #region Conflict management classes

    protected class BaseInfo
    {
      public string Name { get; set; }
    }

    protected class RecordingInfo : BaseInfo
    {
    }

    protected class EpisodeInfo : BaseInfo
    {
      public SeriesInfo Series { get; set; }
      public int SeasonNumber { get; set; }
      public int EpisodeNumber { get; set; }
    }

    protected class SeriesInfo : BaseInfo
    {
      public Guid? Id { get; set; }
      public string AlternateName { get; set; }
      public IList<EpisodeInfo> Episodes { get; set; }
    }

    protected class CardAssignment
    {
      public int CardId { get; set; }
      public ITuningDetail Tuning { get; set; }
      public ISchedule Schedule { get; set; }
      public IProgram Program { get; set; }
      public BaseInfo CreatedInfo { get; set; }
      public IScheduleRule ScheduleRule { get; set; }
    }

    protected class CollectionCache
    {
      public IDictionary<int, IList<IProgram>> Programs { get; } =  new Dictionary<int, IList<IProgram>>();
      public IDictionary<int, IChannel> Channels { get; } = new Dictionary<int, IChannel>();
      public IDictionary<int, IList<IChannel>> Groups { get; } = new Dictionary<int, IList<IChannel>>();
      public IList<ISchedule> PlannedSchedules { get; } = new List<ISchedule>();
      public IList<SeriesInfo> KnownSeries { get; } = new List<SeriesInfo>();
      public IList<RecordingInfo> KnownRecordings { get; } = new List<RecordingInfo>();
      public IDictionary<int, ICard> Cards { get; } = new Dictionary<int, ICard>();
      public IDictionary<int, IDictionary<int, ITuningDetail>> CardChannelTunings { get; } = new Dictionary<int, IDictionary<int, ITuningDetail>>();
      public IList<(IScheduleRule Rule, ISchedule Schedule)> PlannedRuleSchedules { get; } = new List<(IScheduleRule, ISchedule)>();
      public IDictionary<int, IList<CardAssignment>> CardAssignments { get; } = new Dictionary<int, IList<CardAssignment>>();
      public IList<ISchedule> CancelledSchedules { get; } = new List<ISchedule>();
      public IDictionary<int, RecordingStatus> ProgramRecordingStatuses { get; } = new Dictionary<int, RecordingStatus>();
      public IList<(CardAssignment CardAssignment, CardAssignment ConflictingCardAssignment)> Conflicts { get; } = new List<(CardAssignment, CardAssignment)>();
    }

    public class ScheduleComparer : IEqualityComparer<ISchedule>
    {
      public bool Equals(ISchedule x, ISchedule y)
      {
        // Check whether the compared objects reference the same data. 
        if (ReferenceEquals(x, y))
          return true;

        // Check whether any of the compared objects is null. 
        if (ReferenceEquals(x, null) || ReferenceEquals(y, null))
          return false;

        // Check we avoid duplicate schedules
        return x.ChannelId == y.ChannelId && x.StartTime == y.StartTime;
      }

      // If Equals() returns true for a pair of objects
      // then GetHashCode() must return the same value for these objects.

      public int GetHashCode(ISchedule schedule)
      {
        //Check whether the object is null 
        if (ReferenceEquals(schedule, null))
          return 0;

        // Calculate the hash code for the schedule. 
        int hash = 5381;
        hash = ((hash << 5) + hash) ^ schedule.ChannelId.GetHashCode();
        hash = ((hash << 5) + hash) ^ schedule.StartTime.GetHashCode();

        return hash;
      }
    }

    public class ConflictComparer : IEqualityComparer<IConflict>
    {
      public bool Equals(IConflict x, IConflict y)
      {
        // Check whether the compared objects reference the same data. 
        if (ReferenceEquals(x, y))
          return true;

        // Check whether any of the compared objects is null. 
        if (ReferenceEquals(x, null) || ReferenceEquals(y, null))
          return false;

        // Check we avoid duplicate conflicts
        return x.CardId == y.CardId && x.ChannelId == y.ChannelId && x.ScheduleId == y.ScheduleId && x.ProgramStartTime == y.ProgramStartTime;
      }

      // If Equals() returns true for a pair of objects
      // then GetHashCode() must return the same value for these objects.

      public int GetHashCode(IConflict conflict)
      {
        //Check whether the object is null 
        if (ReferenceEquals(conflict, null))
          return 0;

        // Calculate the hash code for the conflict. 
        int hash = 5381;
        hash = ((hash << 5) + hash) ^ conflict.CardId.GetHashCode();
        hash = ((hash << 5) + hash) ^ conflict.ChannelId.GetHashCode();
        hash = ((hash << 5) + hash) ^ conflict.ScheduleId.GetHashCode();
        hash = ((hash << 5) + hash) ^ conflict.ProgramStartTime.GetHashCode();

        return hash;
      }
    }

    #endregion

    protected const int MAX_WAIT_MS = 10000;
    protected const int MAX_INIT_MS = 20000;
    protected const int CHECK_INTERVAL_MS = 30000;
    protected DbProviderFactory _dbProviderFactory;
    protected string _cloneConnection;
    protected string _providerName;
    protected string _serviceName;

    private bool _abortInit = false;
    private IDictionary<int, RecordingStatus> _programsStatuses = new Dictionary<int, RecordingStatus>();
    private AsyncReaderWriterLock _scheduleRuleAccess = new AsyncReaderWriterLock();
    private Timer _checkScheduleTimer;
    private bool _checkingSchedules = false;
    private TimeSpan _checkScheduleMaxSpan = TimeSpan.FromDays(14);

    // Stores a list of connected MP2-Clients. If one disconnects, we can cleanup resources like stopping timeshifting for this client
    protected List<string> _connectedClients = new List<string>();
    protected TaskCompletionSource<bool> _initComplete = new TaskCompletionSource<bool>();
    protected SettingsChangeWatcher<SlimTvGenreColorSettings> _genrColorSettingWatcher;
    protected SettingsChangeWatcher<SlimTvServerSettings> _serverSettingWatcher;
    protected SlimTvGenreColorSettings _epgColorSettings = null;
    protected SlimTvServerSettings _serverSettings = null;
    protected readonly ConcurrentDictionary<EpgGenre, ICollection<string>> _tvGenres = new ConcurrentDictionary<EpgGenre, ICollection<string>>();
    protected bool _tvGenresInited = false;
    protected DateTime _nextSchedule = DateTime.MaxValue;
    protected DateTime _nextFullScheduleCheck = DateTime.MinValue;
    protected DateTime _nowTime = DateTime.Now;
    protected bool _checkCacheUpToDate = false;
    protected ScheduleComparer _scheduleComparer = new ScheduleComparer();
    protected ConflictComparer _conflictComparer = new ConflictComparer();
    protected ProgramComparer _programComparer = ProgramComparer.Instance;
    protected bool _localRuleHandling = false;

    private void GenreColorSettingsChanged(object sender, EventArgs e)
    {
      _epgColorSettings = _genrColorSettingWatcher.Settings;
    }

    private void ServerSettingsChanged(object sender, EventArgs e)
    {
      _serverSettings = _serverSettingWatcher.Settings;
    }

    public string Name
    {
      get { return _providerName; }
    }

    public virtual bool Init()
    {
      _localRuleHandling = typeof(TScheduleRule) == typeof(ScheduleRule);

      ServiceRegistration.Get<IMessageBroker>().RegisterMessageReceiver(SystemMessaging.CHANNEL, this);
      ServiceRegistration.Get<IMessageBroker>().RegisterMessageReceiver(ClientManagerMessaging.CHANNEL, this);

      _genrColorSettingWatcher = new SettingsChangeWatcher<SlimTvGenreColorSettings>();
      _genrColorSettingWatcher.SettingsChanged += GenreColorSettingsChanged;
      _genrColorSettingWatcher.Refresh();

      _serverSettingWatcher = new SettingsChangeWatcher<SlimTvServerSettings>();
      _serverSettingWatcher.SettingsChanged += ServerSettingsChanged;
      _serverSettingWatcher.Refresh();

      _checkScheduleTimer = new Timer(async (s) => await CheckSchedulesAsync(DateTime.Now), null, MAX_INIT_MS, CHECK_INTERVAL_MS);
      return true;
    }

    public virtual bool DeInit()
    {
      _checkScheduleTimer.Dispose();
      return true;
    }

    public void Receive(SystemMessage message)
    {
      if (message.MessageType as SystemMessaging.MessageType? == SystemMessaging.MessageType.SystemStateChanged)
      {
        SystemState newState = (SystemState)message.MessageData[SystemMessaging.NEW_STATE];
        if (newState == SystemState.Running)
        {
          InitAsync();
        }
        else if (newState == SystemState.ShuttingDown)
        {
          _abortInit = true;
        }
      }

      if (message.ChannelName == ClientManagerMessaging.CHANNEL)
      {
        ClientManagerMessaging.MessageType messageType = (ClientManagerMessaging.MessageType)message.MessageType;
        switch (messageType)
        {
          case ClientManagerMessaging.MessageType.ClientAttached:
          case ClientManagerMessaging.MessageType.ClientOnline:
            UpdateClientList();
            break;
          case ClientManagerMessaging.MessageType.ClientDetached:
          case ClientManagerMessaging.MessageType.ClientOffline:
            CheckOrphanedTimeshift();
            break;
        }
      }

      if (message.ChannelName == ClientManagerMessaging.CHANNEL)
      {
        ImporterWorkerMessaging.MessageType messageType = (ImporterWorkerMessaging.MessageType)message.MessageType;
        switch (messageType)
        {
          case ImporterWorkerMessaging.MessageType.ImportCompleted:
            _checkCacheUpToDate = false;
            break;
        }
      }
    }

    #region Handling of client diconnections

    private void CheckOrphanedTimeshift()
    {
      List<string> disconnectedClients;
      if (UpdateClientList(out disconnectedClients))
      {
        StopTimeshiftForClients(disconnectedClients);
      }
    }

    protected virtual void StopTimeshiftForClients(List<string> disconnectedClients)
    {
      foreach (string disconnectedClient in disconnectedClients)
      {
        string client = disconnectedClient;
        if (IsLocal(client))
          client = Consts.LOCAL_USERNAME;

        for (int slotIndex = 0; slotIndex <= 1; slotIndex++)
        {
          if (StopTimeshiftAsync(client, slotIndex).Result)
          {
            ServiceRegistration.Get<ILogger>().Info("SlimTvService: Stopping timeshift for disconnected client '{0}' ({1})", client, slotIndex);
          }
        }
      }
    }

    private void UpdateClientList()
    {
      UpdateClientList(out _);
    }

    private bool UpdateClientList(out List<string> disconnectedClients)
    {
      IClientManager clientManager = ServiceRegistration.Get<IClientManager>();
      ICollection<ClientConnection> clients = clientManager.ConnectedClients;
      ICollection<string> connectedClientSystemIDs = new List<string>(clients.Count);
      foreach (ClientConnection clientConnection in clients)
        connectedClientSystemIDs.Add(clientConnection.Descriptor.System.Address);
      disconnectedClients = _connectedClients.Except(connectedClientSystemIDs).ToList();
      _connectedClients = connectedClientSystemIDs.ToList();
      return disconnectedClients.Count > 0;
    }

    protected static bool IsLocal(string client)
    {
      return client == "127.0.0.1" || client == "::1";
    }

    #endregion

    #region Database and program data initialization

    private void InitAsync()
    {
      ServiceRegistration.Get<ILogger>().Info("SlimTvService: Initializing");
      Task.Delay(MAX_INIT_MS)
        .ContinueWith((t) =>
        {
          if (_initComplete.Task.Status != TaskStatus.RanToCompletion)
          {
            _initComplete.TrySetResult(false);
            ServiceRegistration.Get<ILogger>().Error("SlimTvService: Initialization timed out.");
          }
        });

      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>(false);
      if (database == null)
      {
        ServiceRegistration.Get<ILogger>().Error("SlimTvService: Database not available.");
        _initComplete.TrySetResult(false);
        return;
      }

      using (var transaction = database.BeginTransaction())
      {
        // Prepare TV database if required.
        PrepareTvDatabase(transaction);
        PrepareConnection(transaction);
      }

      // Initialize integration into host system (MP2-Server)
      PrepareIntegrationProvider();

      // Needs to be done after the IntegrationProvider is registered, so the TVCORE folder is defined.
      PrepareProgramData();

      // Register required filters
      PrepareFilterRegistrations();

      // Get all current connected clients, so we can later detect disconnections
      UpdateClientList();

      // Run the actual TV core thread(s)
      InitTvCore();
      if (_abortInit)
      {
        ServiceRegistration.Get<ILogger>().Error("SlimTvService: Initialization aborted.");
        _initComplete.TrySetResult(false);
        DeInit();
        return;
      }

      // Prepare the MP2 integration
      PrepareMediaSources();

      //InitRecordingFoldersAsync().Wait();
      InitProgramCacheAsync().Wait();

      ServiceRegistration.Get<ILogger>().Info("SlimTvService: Initialized");
      _initComplete.TrySetResult(true);
    }

    /// <summary>
    /// Prepares optionally needed filter registrations.
    /// </summary>
    protected abstract void PrepareFilterRegistrations();

    /// <summary>
    /// Runs the actual TV core thread(s).
    /// </summary>
    protected abstract void InitTvCore();

    /// <summary>
    /// Initializes the integration into host system (MP2-Server).
    /// </summary>
    protected abstract void PrepareIntegrationProvider();

    /// <summary>
    /// Executes custom initialization of DB connection.
    /// </summary>
    /// <param name="transaction"></param>
    protected abstract void PrepareConnection(ITransaction transaction);

    /// <summary>
    /// Prepares the required data folders for first run. The required tuning details and other files are extracted to [TVCORE] path.
    /// </summary>
    protected virtual void PrepareProgramData()
    {
      if (!NeedsExtract())
        return;

      ServiceRegistration.Get<ILogger>().Info("SlimTvService: Tuning details folder does not exist yet, extracting default items.");
      try
      {
        // Morpheus_xx, 2014-09-01: As soon as our extension installer is able to place files in different target folders, this code can be removed.
        var mp2DataPath = GetTvCorePath();
        ZipFile.ExtractToDirectory(FileUtils.BuildAssemblyRelativePath("ProgramData\\ProgramData.zip"), mp2DataPath);
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("SlimTvService: Failed to extract Tuning details!", ex);
      }
    }

    /// <summary>
    /// Initializes genre mapping defined in the server settings if any. Needs to be overridden by plug-ins for which the server setup 
    /// supports genre mapping.
    /// </summary>
    protected virtual Task InitGenreMapAsync()
    {
      if (_tvGenresInited)
        return Task.CompletedTask;

      _tvGenresInited = true;
      return Task.CompletedTask;
    }

    /// <summary>
    /// Gets a TV core-version specific folder. This allow to use both TVE3 and TVE3.5 in parallel (only one enabled ofc).
    /// </summary>
    /// <returns></returns>
    protected string GetTvCorePath()
    {
      return ServiceRegistration.Get<IPathManager>().GetPath("<TVCORE>");
    }

    /// <summary>
    /// Checks if the contained defaults should be extracted to ProgramData folder.
    /// </summary>
    /// <returns></returns>
    protected virtual bool NeedsExtract()
    {
      string mp2DataPath = GetTvCorePath();
      return !Directory.Exists(Path.Combine(mp2DataPath, "TuningParameters"));
    }

    /// <summary>
    /// Prepares the database for SlimTV if required. This is only the case for SQLite mode, where we supply an empty template DB.
    /// </summary>
    /// <param name="transaction"></param>
    protected void PrepareTvDatabase(ITransaction transaction)
    {
      // We only need custom logic for SQLite here.
      if (!transaction.Connection.GetType().ToString().Contains("SQLite"))
        return;
      string targetPath = ServiceRegistration.Get<IPathManager>().GetPath("<DATABASE>");
      string databaseTemplate = FileUtils.BuildAssemblyRelativePath("Database");
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
            ServiceRegistration.Get<ILogger>().Info("SlimTvService: Successfully copied database template file {0}", file);
          }
        }
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("SlimTvService: Failed to copy database template!", ex);
      }
    }

    #endregion

    #region Recordings / MediaLibrary synchronization

    protected abstract bool RegisterEvents();

    protected abstract void OnTvServerEvent(object sender, EventArgs eventArgs);

    protected void ImportRecording(string fileName)
    {
      List<Share> possibleShares; // Shares can point to different depth, we try to find the deepest one
      var resourcePath = BuildResourcePath(fileName);
      if (!GetSharesForPath(resourcePath, out possibleShares))
      {
        ServiceRegistration.Get<ILogger>().Warn("SlimTvService: Received notification of new recording but could not find a media source. Have you added recordings folder as media source? File: {0}", fileName);
        return;
      }

      Share usedShare = possibleShares.OrderByDescending(s => s.BaseResourcePath.LastPathSegment.Path.Length).First();
      IImporterWorker importerWorker = ServiceRegistration.Get<IImporterWorker>();
      importerWorker.ScheduleRefresh(usedShare.BaseResourcePath, usedShare.MediaCategories, true);

      _checkCacheUpToDate = false;
    }

    protected bool GetSharesForPath(ResourcePath resourcePath, out List<Share> possibleShares)
    {
      ISystemResolver systemResolver = ServiceRegistration.Get<ISystemResolver>();
      string localSystemId = systemResolver.LocalSystemId;
      return GetSharesForPath(resourcePath, localSystemId, out possibleShares);
    }

    protected bool GetSharesForPath(ResourcePath resourcePath, string localSystemId, out List<Share> possibleShares)
    {
      IMediaLibrary mediaLibrary = ServiceRegistration.Get<IMediaLibrary>();
      possibleShares = new List<Share>();
      foreach (var share in mediaLibrary.GetShares(localSystemId).Values)
      {
        if (resourcePath.ToString().StartsWith(share.BaseResourcePath.ToString(), StringComparison.InvariantCultureIgnoreCase))
          possibleShares.Add(share);
      }

      return possibleShares.Count > 0;
    }

    protected abstract bool GetRecordingConfiguration(out List<string> recordingFolders, out string singlePattern, out string seriesPattern);

    protected abstract string GetRecordingFolderForProgram(int cardId, int programId, bool isSeries);

    protected void PrepareMediaSources()
    {
      Dictionary<string, ICollection<MediaCategory>> checkFolders = new Dictionary<string, ICollection<MediaCategory>>();
      List<string> recordingFolders;
      string singlePattern;
      string seriesPattern;
      if (!GetRecordingConfiguration(out recordingFolders, out singlePattern, out seriesPattern))
      {
        ServiceRegistration.Get<ILogger>().Warn("SlimTvService: Unable to configure MediaSource for recordings, probably TV configuration wasn't run yet.");
        return;
      }

      string movieSubfolder = GetFixedFolderPart(singlePattern);
      string seriesSubfolder = GetFixedFolderPart(seriesPattern);

      foreach (var recordingFolder in recordingFolders)
      {
        if (!string.IsNullOrEmpty(movieSubfolder) && !string.IsNullOrEmpty(seriesSubfolder))
        {
          // If there are different target folders defined, register the media sources with specialized Series/Movie types
          checkFolders.Add(FileUtils.CheckTrailingPathDelimiter(Path.Combine(recordingFolder, movieSubfolder)), new HashSet<MediaCategory> { DefaultMediaCategories.Video, Movie });
          checkFolders.Add(FileUtils.CheckTrailingPathDelimiter(Path.Combine(recordingFolder, seriesSubfolder)), new HashSet<MediaCategory> { DefaultMediaCategories.Video, Series });
        }
        else
        {
          checkFolders.Add(FileUtils.CheckTrailingPathDelimiter(recordingFolder), new HashSet<MediaCategory> { DefaultMediaCategories.Video });
        }
      }

      IMediaLibrary mediaLibrary = ServiceRegistration.Get<IMediaLibrary>();
      int cnt = 1;
      foreach (var folderTypes in checkFolders)
      {
        try
        {
          List<Share> shares;
          // Check if there are already share(s) for the folder
          var path = folderTypes.Key;
          var resourcePath = BuildResourcePath(path);

          if (GetSharesForPath(resourcePath, out shares))
            continue;

          var mediaCategories = folderTypes.Value.Select(mc => mc.CategoryName);

          Share sd = Share.CreateNewLocalShare(resourcePath, string.Format("Recordings ({0})", cnt),
            // Important: don't monitor recording sources by ShareWatcher, we manage them during recording start / end events!
            false,
            mediaCategories);

          mediaLibrary.RegisterShare(sd);
          cnt++;
        }
        catch (Exception ex)
        {
          ServiceRegistration.Get<ILogger>().Error("SlimTvService: Error registering new MediaSource.", ex);
        }
      }
    }

    /// <summary>
    /// Helper method to create a valid <see cref="ResourcePath"/> from the given path. This method supports both local and UNC paths and will use the right ResourceProviderId.
    /// </summary>
    /// <param name="path">Path or file name</param>
    /// <returns></returns>
    protected static ResourcePath BuildResourcePath(string path)
    {
      Guid providerId;
      if (path.StartsWith("\\\\"))
      {
        // NetworkNeighborhoodResourceProvider
        providerId = new Guid("{03DD2DA6-4DA8-4D3E-9E55-80E3165729A3}");
        // Cut-off the first \
        path = path.Substring(1);
      }
      else
        providerId = LocalFsResourceProviderBase.LOCAL_FS_RESOURCE_PROVIDER_ID;

      string folderPath = LocalFsResourceProviderBase.ToProviderPath(path);
      var resourcePath = ResourcePath.BuildBaseProviderPath(providerId, folderPath);
      return resourcePath;
    }

    /// <summary>
    /// Returns only parts of pattern, which don't contain a variable placeholder (%)
    /// </summary>
    /// <param name="singlePattern">Pattern</param>
    /// <returns></returns>
    private static string GetFixedFolderPart(string singlePattern)
    {
      var folderParts = singlePattern.Split(new[] { '\\' });
      var fixedFolderParts = new List<string>();
      foreach (var folderPart in folderParts)
      {
        if (!folderPart.Contains("%"))
          fixedFolderParts.Add(folderPart);
        else
          break;
      }

      return string.Join("\\", fixedFolderParts);
    }

    protected abstract Task<AsyncResult<IList<IRecording>>> GetProviderRecordingsAsync(string name);

    public Task<AsyncResult<IList<IRecording>>> GetRecordingsAsync(string name)
    {
      return GetProviderRecordingsAsync(name);
    }

    protected Task ProgramsChangedAsync()
    {
      _checkCacheUpToDate = false;
      return Task.CompletedTask;
    }

    protected async Task InitProgramCacheAsync()
    {
      var cache = await InitCacheAsync(_checkScheduleMaxSpan.TotalDays, false);
      await UpdateProgramCacheAsync(cache.ProgramRecordingStatuses);
    }

    protected Task UpdateProgramCacheAsync(IDictionary<int, RecordingStatus> recordingStatuses)
    {
      _checkCacheUpToDate = true;
      _programsStatuses = recordingStatuses;
      return Task.CompletedTask;
    }

    protected async Task CheckSchedulesAsync(DateTime now)
    {
      DateTime startUpdate = DateTime.UtcNow;
      try
      {
        if (_checkingSchedules)
          return;
        _checkingSchedules = true;
        _nowTime = now;

        double days = 1;
        if (!_checkCacheUpToDate)
          days = _checkScheduleMaxSpan.TotalDays;

        Logger.Debug($"SlimTvService: Checking schedules for next {days:F2} days");

        var checkTime = new DateTime(_nowTime.Year, _nowTime.Month, _nowTime.Day, Convert.ToInt32(_serverSettings.ScheduleCheckStartTime), 0, 0);
        bool checkBeforeRecord = _nextSchedule.AddMinutes(-_serverSettings.MovedProgramsDetectionOffset) <= _nowTime || !_checkCacheUpToDate;
        bool checkFull = _nextFullScheduleCheck <= _nowTime || !_checkCacheUpToDate;
        if (!checkFull && !checkBeforeRecord && _checkCacheUpToDate)
          return;

        var cache = await InitCacheAsync(days, true);

        //Update next pre-check
        var thresholdDate = _nextSchedule < DateTime.MaxValue ? _nextSchedule : _nowTime;
        var nextSchedules = cache.PlannedSchedules.Where(s => s.StartTime > thresholdDate).ToList();
        var nextCheckTime = checkTime.AddDays(1);
        if (nextSchedules.Any())
          nextCheckTime = nextSchedules.Min(s => s.StartTime);
        _nextSchedule = nextCheckTime;

        //Update next full check
        if (checkFull)
          _nextFullScheduleCheck = checkTime.AddDays(1);

        if (!_checkCacheUpToDate)
        {
          await UpdateProgramCacheAsync(cache.ProgramRecordingStatuses);
          var conflicts = cache.Conflicts.Select(c => (IConflict)new Conflict
            {
              CardId = c.CardAssignment.CardId,
              ChannelId = c.CardAssignment.Schedule.ChannelId,
              ScheduleId = c.CardAssignment.Schedule.ScheduleId,
              ProgramStartTime = c.CardAssignment.Program.StartTime,
              ConflictingScheduleId = c.ConflictingCardAssignment.Schedule.ScheduleId,
            })
            .Distinct(_conflictComparer)
            .ToList();
          var storedConflicts = await ReplaceConflictsAsync(conflicts);
          if (storedConflicts > 0)
            Logger.Info($"SlimTvService: Updated conflicts. Stored {storedConflicts} conflicts");
        }

        if (checkFull || checkBeforeRecord)
        {
          var cancelSchedules = cache.CancelledSchedules.Where(s => _nowTime <= s.StartTime && nextCheckTime > s.StartTime).ToList();
          var canceledRecordings = await CancelExistingEpisodeSchedulesAsync(cancelSchedules);
          if (canceledRecordings > 0)
            Logger.Info($"SlimTvService: Updated schedules. Canceled {canceledRecordings} already recorded episode programs");

          if (_serverSettings.DetectMovedPrograms)
          {
            var movedPrograms = cache.CardAssignments.SelectMany(c => c.Value)
              .Where(a => !IsManualTitle(a.Schedule.Name) &&
                ((_nowTime <= a.Schedule.StartTime && nextCheckTime > a.Schedule.StartTime) || _nowTime <= a.Program.StartTime && nextCheckTime > a.Program.StartTime) && 
                !IsScheduleCoveringProgram(a.Schedule, a.Program))
              .Select(m => (m.Schedule, m.Program))
              .ToList();
            var movedRecordings = await ReplaceMovedSchedulesAsync(movedPrograms);
            if (movedRecordings > 0)
              Logger.Info($"SlimTvService: Updated schedules. Found {movedRecordings} moved programs");
          }
        }

        if (checkFull)
        {
          var nextRuleTime = checkTime.AddDays(1);
          var ruleSchedules = cache.CardAssignments
            .SelectMany(a => a.Value)
            .Where(s => s.Schedule.StartTime >= checkTime && s.Schedule.StartTime < nextRuleTime && s.ScheduleRule != null)
            .Select(s => (s.CardId, s.ScheduleRule, s.Schedule, s.Program))
            .ToList();
          var scheduledRecordings = await CreateRuleSchedulesAsync(ruleSchedules);
          if (scheduledRecordings > 0)
            Logger.Info($"SlimTvService: Updated rule based schedules. Created {scheduledRecordings} new schedules");
        }
      }
      catch (Exception ex)
      {
        Logger.Error($"SlimTvService: Error checking schedules", ex);
      }
      finally
      {
        _checkingSchedules = false;
        Logger.Debug("SlimTvService: Finished checking schedules {0} ms", (DateTime.UtcNow - startUpdate).TotalMilliseconds);
      }
    }

    #endregion

    #region Timeshift control

    public Task<AsyncResult<MediaItem>> StartTimeshiftAsync(int slotIndex, IChannel channel)
    {
      throw new NotImplementedException("Not available in server side implementation");
    }

    public Task<bool> StopTimeshiftAsync(int slotIndex)
    {
      throw new NotImplementedException("Not available in server side implementation");
    }

    public async Task<AsyncResult<MediaItem>> StartTimeshiftAsync(string userName, int slotIndex, IChannel channel)
    {
      await _initComplete.Task;

      string timeshiftFile = await SwitchProviderToChannelAsync(GetUserName(userName, slotIndex), channel.ChannelId);
      var timeshiftMediaItem = await CreateMediaItemAsync(slotIndex, timeshiftFile, channel);
      var result = !string.IsNullOrEmpty(timeshiftFile);
      return new AsyncResult<MediaItem>(result, timeshiftMediaItem);
    }

    protected abstract Task<bool> StopProviderTimeshiftAsync(string userName, int slotIndex);

    public async Task<bool> StopTimeshiftAsync(string userName, int slotIndex)
    {
      await _initComplete.Task;

      return await StopProviderTimeshiftAsync(userName, slotIndex);
    }

    public IChannel GetChannel(int slotIndex)
    {
      // We do not manage all client channels here in server, this feature applies only to client side management!
      return null;
    }

    #endregion

    #region Program info

    protected abstract Task<AsyncResult<IProgram[]>> GetProviderNowNextProgramAsync(IChannel channel);

    public async Task<AsyncResult<IProgram[]>> GetNowNextProgramAsync(IChannel channel)
    {
      await _initComplete.Task;

      return await GetProviderNowNextProgramAsync(channel);
    }

    public virtual async Task<AsyncResult<IDictionary<int, IProgram[]>>> GetNowAndNextForChannelGroupAsync(IChannelGroup channelGroup)
    {
      var nowNextPrograms = new Dictionary<int, IProgram[]>();

      var result = await GetProviderChannelsAsync(channelGroup);
      if (!result.Success)
        return new AsyncResult<IDictionary<int, IProgram[]>>(false, null);

      IList<IChannel> channels = result.Result;
      foreach (IChannel channel in channels)
      {
        var progrResult = await GetProviderNowNextProgramAsync(channel);
        if (progrResult.Success)
          nowNextPrograms[channel.ChannelId] = progrResult.Result;
      }

      return new AsyncResult<IDictionary<int, IProgram[]>>(true, nowNextPrograms);
    }

    protected abstract Task<AsyncResult<IList<IProgram>>> GetProviderProgramsAsync(IChannel channel, DateTime from, DateTime to);

    public async Task<AsyncResult<IList<IProgram>>> GetProgramsAsync(IChannel channel, DateTime from, DateTime to)
    {
      await _initComplete.Task;

      return await GetProviderProgramsAsync(channel, from, to);
    }

    protected abstract Task<AsyncResult<IList<IProgram>>> GetProviderProgramsAsync(string title, DateTime from, DateTime to);

    public async Task<AsyncResult<IList<IProgram>>> GetProgramsAsync(string title, DateTime from, DateTime to)
    {
      await _initComplete.Task;

      return await GetProviderProgramsAsync(title, from, to);
    }

    protected abstract Task<AsyncResult<IList<IProgram>>> GetProviderProgramsGroupAsync(IChannelGroup channelGroup, DateTime from, DateTime to);

    public async Task<AsyncResult<IList<IProgram>>> GetProgramsGroupAsync(IChannelGroup channelGroup, DateTime from, DateTime to)
    {
      await _initComplete.Task;

      return await GetProviderProgramsGroupAsync(channelGroup, from, to);
    }

    protected abstract Task<AsyncResult<IList<IProgram>>> GetProviderProgramsForScheduleAsync(ISchedule schedule);

    public async Task<AsyncResult<IList<IProgram>>> GetProgramsForScheduleAsync(ISchedule schedule)
    {
      await _initComplete.Task;

      double days = _checkScheduleMaxSpan.TotalDays;
      EpisodeManagementScheme seriesCheckScheme = (EpisodeManagementScheme)_serverSettings.DefaultEpisodeManagementScheme;
      if (seriesCheckScheme == EpisodeManagementScheme.None && schedule.ScheduleId != 0)
      {
        return await GetProviderProgramsForScheduleAsync(schedule);
      }
      else
      {
        try
        {
          await _initComplete.Task;

          DateTime startUpdate = DateTime.UtcNow;
          Logger.Info($"SlimTvService: Generating programs for schedule '{schedule.Name}'");

          CollectionCache cache = await InitCacheAsync(days, false, schedule);
          List<IProgram> allPrograms = new List<IProgram>();
          foreach (var ca in cache.CardAssignments)
          {
            foreach (var a in ca.Value)
            {
              if (a.Schedule.ScheduleId == schedule.ScheduleId)
              {
                var prog = GetCachedProgramForSchedule(a.Schedule, cache);
                if (prog != null)
                  allPrograms.Add(prog);
              }
            }
          }
          Logger.Info("SlimTvService: Found {0} programs for schedule '{1}' {2} ms", allPrograms.Count, schedule.Name, (DateTime.UtcNow - startUpdate).TotalMilliseconds);

          if (!allPrograms.Any())
          {
            return new AsyncResult<IList<IProgram>>(true, new List<IProgram>());
          }
          else
          {
            return new AsyncResult<IList<IProgram>>(true, allPrograms
              .Where(p => p != null)
              .Distinct(_programComparer)
              .ToList());
          }
        }
        catch (Exception ex)
        {
          Logger.Error($"SlimTvService: Error generating programs for schedule '{schedule.Name}'", ex);
        }
        return new AsyncResult<IList<IProgram>>(false, null);
      }
    }

    protected abstract Task<AsyncResult<IChannel>> GetProviderChannelAsync(IProgram program);

    public async Task<AsyncResult<IChannel>> GetChannelAsync(IProgram program)
    {
      await _initComplete.Task;

      return await GetProviderChannelAsync(program);
    }

    protected abstract Task<AsyncResult<IProgram>> GetProviderProgramAsync(int programId);

    public async Task<AsyncResult<IProgram>> GetProgramAsync(int programId)
    {
      await _initComplete.Task;

      return await GetProviderProgramAsync(programId);
    }

    /// <summary>
    /// Returns a program with assigned EPG genre data if possible.
    /// </summary>
    protected virtual IProgram GetProgram(TProgram tvProgram, bool includeRecordingStatus = false)
    {
      InitGenreMapAsync().Wait();

      //Convert to IProgram
      var prog = ConvertToProgram(tvProgram, includeRecordingStatus);
      if (prog == null)
        return null;

      if (_localRuleHandling && includeRecordingStatus && _programsStatuses.ContainsKey(prog.ProgramId))
      {
        prog.RecordingStatus |= _programsStatuses[prog.ProgramId];
      }

      //Map genre color if possible
      if (_tvGenres.Count > 0 && !string.IsNullOrEmpty(prog.Genre))
      {
        var genre = _tvGenres.FirstOrDefault(g => g.Value.Contains(prog.Genre));
        if (genre.Key != EpgGenre.Unknown)
        {
          prog.EpgGenreId = (int)genre.Key;
          switch (genre.Key)
          {
            case EpgGenre.Movie:
              prog.EpgGenreColor = _epgColorSettings.MovieGenreColor;
              break;
            case EpgGenre.Series:
              prog.EpgGenreColor = _epgColorSettings.SeriesGenreColor;
              break;
            case EpgGenre.Documentary:
              prog.EpgGenreColor = _epgColorSettings.DocumentaryGenreColor;
              break;
            case EpgGenre.Music:
              prog.EpgGenreColor = _epgColorSettings.MusicGenreColor;
              break;
            case EpgGenre.Kids:
              prog.EpgGenreColor = _epgColorSettings.KidsGenreColor;
              break;
            case EpgGenre.News:
              prog.EpgGenreColor = _epgColorSettings.NewsGenreColor;
              break;
            case EpgGenre.Sport:
              prog.EpgGenreColor = _epgColorSettings.SportGenreColor;
              break;
            case EpgGenre.Special:
              prog.EpgGenreColor = _epgColorSettings.SpecialGenreColor;
              break;
          }
        }
      }

      //If genre is unknown and the program contains series info, mark it as a series genre
      if (prog.EpgGenreId == (int)EpgGenre.Unknown &&
          (!string.IsNullOrWhiteSpace(prog.SeasonNumber) || !string.IsNullOrWhiteSpace(prog.EpisodeNumber) || !string.IsNullOrWhiteSpace(prog.EpisodePart)))
      {
        prog.EpgGenreId = (int)EpgGenre.Series;
        prog.EpgGenreColor = _epgColorSettings.SeriesGenreColor;
      }

      return prog;
    }

    #endregion

    #region Channel and group info

    protected abstract Task<AsyncResult<IList<IChannelGroup>>> GetProviderChannelGroupsAsync();

    public async Task<AsyncResult<IList<IChannelGroup>>> GetChannelGroupsAsync()
    {
      await _initComplete.Task;

      return await GetProviderChannelGroupsAsync();
    }

    protected abstract Task<AsyncResult<IList<IChannel>>> GetProviderChannelsAsync(IChannelGroup group);

    public async Task<AsyncResult<IList<IChannel>>> GetChannelsAsync(IChannelGroup group)
    {
      await _initComplete.Task;

      return await GetProviderChannelsAsync(group);
    }

    protected abstract Task<AsyncResult<IChannel>> GetProviderChannelAsync(int channelId);

    public async Task<AsyncResult<IChannel>> GetChannelAsync(int channelId)
    {
      await _initComplete.Task;

      return await GetProviderChannelAsync(channelId);
    }

    // This property applies only to client side management and is not used in server!
    public int SelectedChannelId { get; set; }

    // This property applies only to client side management and is not used in server!
    public int SelectedChannelGroupId { get; set; }

    // This property applies only to client side management and is not used in server!
    public int SelectedRadioChannelId { get; set; }

    // This property applies only to client side management and is not used in server!
    public int SelectedRadioChannelGroupId { get; set; }

    #endregion

    #region Schedule control

    protected abstract Task<AsyncResult<IList<ISchedule>>> GetProviderSchedulesAsync();

    public async Task<AsyncResult<IList<ISchedule>>> GetSchedulesAsync()
    {
      await _initComplete.Task;

      return await GetProviderSchedulesAsync();
    }

    protected abstract Task<AsyncResult<IList<ISchedule>>> GetProviderCanceledSchedulesAsync();

    public async Task<AsyncResult<IList<IProgram>>> GetCanceledSchedulesAsync()
    {
      await _initComplete.Task;

      List<IProgram> cancelledPrograms = new List<IProgram>();
      var cancelledResult = await GetProviderCanceledSchedulesAsync();
      if (cancelledResult.Success)
      {
        foreach (var schedule in cancelledResult.Result)
        {
          var progResult = await GetProviderProgramsForScheduleAsync(schedule);
          if (progResult.Success)
            cancelledPrograms.AddRange(progResult.Result);
          else
            cancelledPrograms.Add(CreateProgramPlaceholderForSchedule(schedule));
        }
      }
      return new AsyncResult<IList<IProgram>>(cancelledPrograms.Any(), cancelledPrograms.Any() ? cancelledPrograms : null);
    }

    protected abstract Task<AsyncResult<ISchedule>> CreateProviderScheduleAsync(IProgram program, ScheduleRecordingType recordingType);

    public async Task<AsyncResult<ISchedule>> CreateScheduleAsync(IProgram program, ScheduleRecordingType recordingType)
    {
      await _initComplete.Task;

      var task = CreateProviderScheduleAsync(program, recordingType);
      _checkCacheUpToDate = false;
      return await task;
    }

    protected abstract Task<AsyncResult<ISchedule>> CreateProviderScheduleByTimeAsync(IChannel channel, DateTime from, DateTime to, ScheduleRecordingType recordingType);

    public async Task<AsyncResult<ISchedule>> CreateScheduleByTimeAsync(IChannel channel, DateTime from, DateTime to, ScheduleRecordingType recordingType)
    {
      await _initComplete.Task;

      var task = CreateProviderScheduleByTimeAsync(channel, from, to, recordingType);
      _checkCacheUpToDate = false;
      return await task;
    }

    protected abstract Task<AsyncResult<ISchedule>> CreateProviderScheduleByTimeAsync(IChannel channel, string title, DateTime from, DateTime to, ScheduleRecordingType recordingType);

    public async Task<AsyncResult<ISchedule>> CreateScheduleByTimeAsync(IChannel channel, string title, DateTime from, DateTime to, ScheduleRecordingType recordingType)
    {
      await _initComplete.Task;

      var task = CreateProviderScheduleByTimeAsync(channel, title, from, to, recordingType);
      _checkCacheUpToDate = false;
      return await task;
    }

    protected abstract Task<AsyncResult<ISchedule>> CreateProviderScheduleDetailedAsync(IChannel channel, string title, DateTime from, DateTime to, ScheduleRecordingType recordingType, int preRecordInterval, int postRecordInterval, string directory, int priority);

    public async Task<AsyncResult<ISchedule>> CreateScheduleDetailedAsync(IChannel channel, string title, DateTime from, DateTime to, ScheduleRecordingType recordingType, int preRecordInterval, int postRecordInterval, string directory, int priority)
    {
      await _initComplete.Task;

      var task = CreateProviderScheduleDetailedAsync(channel, title, from, to, recordingType, preRecordInterval, postRecordInterval, directory, priority);
      _checkCacheUpToDate = false;
      return await task;
    }

    protected abstract Task<bool> EditProviderScheduleAsync(ISchedule schedule, IChannel channel = null, string title = null, DateTime? from = null, DateTime? to = null, ScheduleRecordingType? recordingType = null, int? preRecordInterval = null, int? postRecordInterval = null, string directory = null, int? priority = null);

    public async Task<bool> EditScheduleAsync(ISchedule schedule, IChannel channel = null, string title = null, DateTime? from = null, DateTime? to = null, ScheduleRecordingType? recordingType = null, int? preRecordInterval = null, int? postRecordInterval = null, string directory = null, int? priority = null)
    {
      await _initComplete.Task;

      var task = EditProviderScheduleAsync(schedule, channel, title, from, to, recordingType, preRecordInterval, postRecordInterval, directory, priority);
      _checkCacheUpToDate = false;
      return await task;
    }

    protected abstract Task<bool> RemoveProviderScheduleForProgramAsync(IProgram program, ScheduleRecordingType recordingType);

    public async Task<bool> RemoveScheduleForProgramAsync(IProgram program, ScheduleRecordingType recordingType)
    {
      await _initComplete.Task;

      var task = RemoveProviderScheduleForProgramAsync(program, recordingType);
      _checkCacheUpToDate = false;
      return await task;
    }

    protected abstract Task<bool> RemoveProviderScheduleAsync(ISchedule schedule);

    public async Task<bool> RemoveScheduleAsync(ISchedule schedule)
    {
      await _initComplete.Task;

      var task = RemoveProviderScheduleAsync(schedule);
      _checkCacheUpToDate = false;
      return await task;
    }

    protected abstract Task<bool> UnCancelProviderScheduleAsync(IProgram program);

    public async Task<bool> UnCancelScheduleAsync(IProgram program)
    {
      await _initComplete.Task;

      var task = UnCancelProviderScheduleAsync(program);
      _checkCacheUpToDate = false;
      return await task;
    }

    protected abstract Task<AsyncResult<RecordingStatus>> GetProviderRecordingStatusAsync(IProgram program);

    public async Task<AsyncResult<RecordingStatus>> GetRecordingStatusAsync(IProgram program)
    {
      await _initComplete.Task;

      if (_localRuleHandling && _programsStatuses.ContainsKey(program.ProgramId))
        return new AsyncResult<RecordingStatus>(true, _programsStatuses[program.ProgramId]);

      return await GetProviderRecordingStatusAsync(program);
    }

    protected abstract Task<AsyncResult<string>> GetProviderRecordingFileOrStreamAsync(IProgram program);

    public async Task<AsyncResult<string>> GetRecordingFileOrStreamAsync(IProgram program)
    {
      await _initComplete.Task;

      return await GetProviderRecordingFileOrStreamAsync(program);
    }

    protected abstract Task<AsyncResult<ISchedule>> IsProviderCurrentlyRecordingAsync(string fileName);

    public async Task<AsyncResult<ISchedule>> IsCurrentlyRecordingAsync(string fileName)
    {
      await _initComplete.Task;

      return await IsProviderCurrentlyRecordingAsync(fileName);
    }

    public async Task<AsyncResult<IList<IProgram>>> GetConflictsForScheduleAsync(ISchedule schedule)
    {
      try
      {
        await _initComplete.Task;

        DateTime startUpdate = DateTime.UtcNow;
        Logger.Info($"SlimTvService: Generating conflicts for schedule '{schedule.Name}'");

        CollectionCache cache = await InitCacheAsync(_checkScheduleMaxSpan.TotalDays, false, schedule);
        var conflicts = cache.Conflicts.Where(c => c.CardAssignment.Schedule.ScheduleId == schedule.ScheduleId)
          .Select(c => c.ConflictingCardAssignment.Program ?? CreateProgramPlaceholderForSchedule(c.ConflictingCardAssignment.Schedule))
          .Distinct(_programComparer)
          .ToList();
        Logger.Info("SlimTvService: Found {0} conflicts for schedule '{1}' {2} ms", conflicts.Count, schedule.Name, (DateTime.UtcNow - startUpdate).TotalMilliseconds);
        return new AsyncResult<IList<IProgram>>(conflicts.Count > 0, conflicts);
      }
      catch (Exception ex)
      {
        Logger.Error($"SlimTvService: Error generating conflicts for schedule '{schedule.Name}'", ex);
      }
      return new AsyncResult<IList<IProgram>>(false, null);
    }

    /// <summary>
    /// Replaces moved schedules
    /// </summary>
    protected async Task<int> ReplaceMovedSchedulesAsync(IList<(ISchedule Schedule, IProgram Program)> movedPrograms)
    {
      if (!movedPrograms.Any())
        return 0;

      try
      {
        await _initComplete.Task;

        DateTime startUpdate = DateTime.UtcNow;
        Logger.Debug($"SlimTvService: Replace moved schedules");

        // Save in tv layer
        int count = 0;
        int failed = 0;
        foreach (var sp in movedPrograms)
        {
          //Remove existing schedule
          if (sp.Schedule.IsSeries)
          {
            var progResult = await GetProgramsForScheduleAsync(sp.Schedule);
            if (!progResult.Success)
            {
              failed++;
              continue;
            }

            bool success = true;
            foreach (var prog in progResult.Result)
            {
              if (!await RemoveScheduleForProgramAsync(prog, sp.Schedule.RecordingType))
                success = false;
            }

            if (!success)
            {
              failed++;
              continue;
            }
          }
          else
          {
            if (!await RemoveProviderScheduleAsync(sp.Schedule))
            {
              failed++;
              continue;
            }
          }
            
          //Add new schedule
          var recordingType = ScheduleRecordingType.Once;
          //if (sp.Schedule.IsSeries)
          //  recordingType = ScheduleRecordingType.WeeklyEveryTimeOnThisChannel; //No way to handle it as an episode?
          var result = await CreateProviderScheduleAsync(sp.Program, recordingType); 
          if (result.Success)
            count++;
          else
            failed++;

          _checkCacheUpToDate = false;
        }
        Logger.Debug("SlimTvService: Stored {0} moved schedules ({1} failed) {2} ms", count, failed, (DateTime.UtcNow - startUpdate).TotalMilliseconds);
        return count;
      }
      catch (Exception ex)
      {
        Logger.Error("SlimTvService: Error replacing moved schedules", ex);
      }

      return 0;
    }

    /// <summary>
    /// Cancel schedules that have already been recorded
    /// </summary>
    protected async Task<int> CancelExistingEpisodeSchedulesAsync(IList<ISchedule> alreadyRecordedEpisodes)
    {
      if (!alreadyRecordedEpisodes.Any())
        return 0;

      try
      {
        await _initComplete.Task;

        DateTime startUpdate = DateTime.UtcNow;
        Logger.Debug($"ConflictManager: Cancel existing episode schedules");

        // Save in tv layer
        int count = 0;
        int failed = 0;
        foreach (var s in alreadyRecordedEpisodes)
        {
          var progResult = await GetProviderProgramsForScheduleAsync(s);
          if (!progResult.Success)
          {
            failed++;
            continue;
          }

          foreach (var prog in progResult.Result)
          {
            if (IsScheduleCoveringProgram(s, prog))
            {
              if (!await RemoveScheduleForProgramAsync(prog, ScheduleRecordingType.Once)) //Only delete episode or movie recording not whole series
                failed++;
              else
                count++;
            }
          }
        }

        if (count > 0)
          _checkCacheUpToDate = false;
        Logger.Debug("ConflictManager: Canceled {0} recorded episode schedules ({1} failed) {2} ms", count, failed, (DateTime.UtcNow - startUpdate).TotalMilliseconds);
        return count;
      }
      catch (Exception ex)
      {
        Logger.Error("ConflictManager: Error canceling recorded episode schedules", ex);
      }

      return 0;
    }

    #endregion

    #region Tuner info

    protected abstract Task<AsyncResult<IList<ICard>>> GetProviderCardsAsync();

    public async Task<AsyncResult<IList<ICard>>> GetCardsAsync()
    {
      await _initComplete.Task;

      return await GetProviderCardsAsync();
    }

    protected abstract Task<AsyncResult<IList<IVirtualCard>>> GetProviderActiveVirtualCardsAsync();

    public async Task<AsyncResult<IList<IVirtualCard>>> GetActiveVirtualCardsAsync()
    {
      await _initComplete.Task;

      return await GetProviderActiveVirtualCardsAsync();
    }

    protected abstract Task<AsyncResult<ITuningDetail>> GetProviderTuningDetailsAsync(ICard card, IChannel channel);

    public async Task<AsyncResult<ITuningDetail>> GetTuningDetailsAsync(ICard card, IChannel channel)
    {
      await _initComplete.Task;

      return await GetProviderTuningDetailsAsync(card, channel);
    }

    #endregion

    #region Schedule rules control

    protected virtual async Task<AsyncResult<IList<IScheduleRule>>> GetProviderScheduleRulesAsync()
    {
      var service = ServiceRegistration.Get<ISettingsManager>(false);
      if (service == null)
        return new AsyncResult<IList<IScheduleRule>>(false, null);

      using(await _scheduleRuleAccess.ReaderLockAsync())
      {
        var settings = service.Load<SlimTvScheduleRulesSettings>();
        return new AsyncResult<IList<IScheduleRule>>(true, settings.ScheduleRules.Select(r => (IScheduleRule)r).ToList());
      }
    }

    public async Task<AsyncResult<IList<IScheduleRule>>> GetScheduleRulesAsync()
    {
      await _initComplete.Task;

      return await GetProviderScheduleRulesAsync();
    }

    protected virtual async Task<AsyncResult<IScheduleRule>> CreateProviderScheduleRuleAsync(IScheduleRule scheduleRule)
    {
      var service = ServiceRegistration.Get<ISettingsManager>(false);
      if (service == null)
        return new AsyncResult<IScheduleRule>(false, null);

      var rule = new ScheduleRule
      {
        RuleId = 1,
        Name = scheduleRule.Name,
        Active = scheduleRule.Active,

        Targets = new List<IScheduleRuleTarget>(),

        ChannelGroupId = scheduleRule.ChannelGroupId,
        ChannelId = scheduleRule.ChannelId,

        IsSeries = scheduleRule.IsSeries,
        SeriesName = scheduleRule.SeriesName,
        SeasonNumber = scheduleRule.SeasonNumber,
        EpisodeNumber = scheduleRule.EpisodeNumber,
        EpisodeTitle = scheduleRule.EpisodeTitle,
        EpisodeInfoFallback = scheduleRule.EpisodeInfoFallback,
        EpisodeInfoFallbackType = scheduleRule.EpisodeInfoFallbackType,

        StartFromTime = scheduleRule.StartFromTime,
        StartToTime = scheduleRule.StartToTime,
        StartOnOrAfterDay = scheduleRule.StartOnOrAfterDay,
        StartOnOrBeforeDay = scheduleRule.StartOnOrBeforeDay,

        Priority = scheduleRule.Priority,

        RecordingType = scheduleRule.RecordingType,
        PreRecordInterval = scheduleRule.PreRecordInterval,
        PostRecordInterval = scheduleRule.PostRecordInterval,

        KeepMethod = scheduleRule.KeepMethod,
        KeepDate = scheduleRule.KeepDate,
      };
      foreach(var target in scheduleRule.Targets)
        rule.Targets.Add(target);

      using (await _scheduleRuleAccess.WriterLockAsync())
      {
        var settings = service.Load<SlimTvScheduleRulesSettings>();
        if (settings.ScheduleRules.Any())
          rule.RuleId = settings.ScheduleRules.Max(r => r.RuleId) + 1;

        settings.ScheduleRules.Add(rule);
        service.Save(settings);
      }

      return new AsyncResult<IScheduleRule>(true, rule);
    }

    public async Task<AsyncResult<IScheduleRule>> CreateScheduleRuleAsync(string title, IList<IScheduleRuleTarget> targets, IChannelGroup channelGroup, IChannel channel, DateTime? from, DateTime? to, DayOfWeek? afterDay, DayOfWeek? beforeDay,
      RuleRecordingType recordingType, int preRecordInterval, int postRecordInterval, int priority, KeepMethodType keepMethod, DateTime? keepDate)
    {
      await _initComplete.Task;

      var rule = new ScheduleRule
      {
        Name = title,
        Active = true,

        Targets = new List<IScheduleRuleTarget>(),

        ChannelGroupId = channelGroup?.ChannelGroupId,
        ChannelId = channel?.ChannelId,

        IsSeries = false,
        SeriesName = null,
        SeasonNumber = null,
        EpisodeNumber = null,
        EpisodeTitle = null,
        EpisodeInfoFallback = null,
        EpisodeInfoFallbackType = RuleEpisodeInfoFallback.None,
        EpisodeManagementScheme = 0,

        StartFromTime = from,
        StartToTime = to,
        StartOnOrAfterDay = afterDay,
        StartOnOrBeforeDay = beforeDay,

        Priority = (PriorityType)priority,

        RecordingType = recordingType,
        PreRecordInterval = TimeSpan.FromMinutes(preRecordInterval),
        PostRecordInterval = TimeSpan.FromMinutes(postRecordInterval),

        KeepMethod = keepMethod,
        KeepDate = keepDate,
      };
      foreach (var target in targets)
        rule.Targets.Add(target);

      var result = await CreateProviderScheduleRuleAsync(rule);
      if (result.Success)
        _checkCacheUpToDate = false;
      return result;
    }

    public async Task<AsyncResult<IScheduleRule>> CreateSeriesScheduleRuleAsync(string title, IList<IScheduleRuleTarget> targets, IChannelGroup channelGroup, IChannel channel, DateTime? from, DateTime? to, DayOfWeek? afterDay, DayOfWeek? beforeDay, 
      string seriesName, string seasonNumber, string episodeNumber, string episodeTitle, string episodeInfoFallback, RuleEpisodeInfoFallback episodeInfoFallbackType, EpisodeManagementScheme episodeManagementScheme, 
      RuleRecordingType recordingType, int preRecordInterval, int postRecordInterval, int priority, KeepMethodType keepMethod, DateTime? keepDate)
    {
      await _initComplete.Task;

      var rule = new ScheduleRule
      {
        Name = title,
        Active = true,

        Targets = new List<IScheduleRuleTarget>(),

        ChannelGroupId = channelGroup?.ChannelGroupId,
        ChannelId = channel?.ChannelId,

        IsSeries = true,
        SeriesName = seriesName,
        SeasonNumber = seasonNumber,
        EpisodeNumber = episodeNumber,
        EpisodeTitle = episodeTitle,
        EpisodeInfoFallback = episodeInfoFallback,
        EpisodeInfoFallbackType = episodeInfoFallbackType,
        EpisodeManagementScheme = episodeManagementScheme,

        StartFromTime = from,
        StartToTime = to,
        StartOnOrAfterDay = afterDay,
        StartOnOrBeforeDay = beforeDay,

        Priority = (PriorityType)priority,

        RecordingType = recordingType,
        PreRecordInterval = TimeSpan.FromMinutes(preRecordInterval),
        PostRecordInterval = TimeSpan.FromMinutes(postRecordInterval),

        KeepMethod = keepMethod,
        KeepDate = keepDate,
      };
      foreach (var target in targets)
        rule.Targets.Add(target);

      var result = await CreateProviderScheduleRuleAsync(rule);
      if (result.Success)
        _checkCacheUpToDate = false;
      return result;
    }

    protected virtual async Task<bool> EditProviderScheduleRuleAsync(IScheduleRule scheduleRule)
    {
      var service = ServiceRegistration.Get<ISettingsManager>(false);
      if (service == null)
        return false;

      var rule = new ScheduleRule
      {
        RuleId = scheduleRule.RuleId,
        Name = scheduleRule.Name,
        Active = scheduleRule.Active,

        Targets = new List<IScheduleRuleTarget>(),

        ChannelGroupId = scheduleRule.ChannelGroupId,
        ChannelId = scheduleRule.ChannelId,

        IsSeries = scheduleRule.IsSeries,
        SeriesName = scheduleRule.SeriesName,
        SeasonNumber = scheduleRule.SeasonNumber,
        EpisodeNumber = scheduleRule.EpisodeNumber,
        EpisodeTitle = scheduleRule.EpisodeTitle,
        EpisodeInfoFallback = scheduleRule.EpisodeInfoFallback,
        EpisodeInfoFallbackType = scheduleRule.EpisodeInfoFallbackType,

        StartFromTime = scheduleRule.StartFromTime,
        StartToTime = scheduleRule.StartToTime,
        StartOnOrAfterDay = scheduleRule.StartOnOrAfterDay,
        StartOnOrBeforeDay = scheduleRule.StartOnOrBeforeDay,

        Priority = scheduleRule.Priority,

        RecordingType = scheduleRule.RecordingType,
        PreRecordInterval = scheduleRule.PreRecordInterval,
        PostRecordInterval = scheduleRule.PostRecordInterval,

        KeepMethod = scheduleRule.KeepMethod,
        KeepDate = scheduleRule.KeepDate,
      };
      foreach (var target in scheduleRule.Targets)
        rule.Targets.Add(target);

      using (await _scheduleRuleAccess.WriterLockAsync())
      {
        var settings = service.Load<SlimTvScheduleRulesSettings>();
        var existing = settings.ScheduleRules.FirstOrDefault(r => r.RuleId == scheduleRule.RuleId);
        if (existing != null)
          settings.ScheduleRules.Remove(existing);
        settings.ScheduleRules.Add(rule);
        service.Save(settings);
      }

      return true;
    }

    public async Task<bool> EditScheduleRuleAsync(IScheduleRule scheduleRule, string title, IList<IScheduleRuleTarget> targets, IChannelGroup channelGroup, IChannel channel, DateTime? from, DateTime? to, DayOfWeek? afterDay, DayOfWeek? beforeDay, 
      bool? isSeries, string seriesName, string seasonNumber, string episodeNumber, string episodeTitle, string episodeInfoFallback, RuleEpisodeInfoFallback? episodeInfoFallbackType, EpisodeManagementScheme? episodeManagementScheme, 
      RuleRecordingType? recordingType, int? preRecordInterval, int? postRecordInterval, int? priority, KeepMethodType? keepMethod, DateTime? keepDate)
    {
      await _initComplete.Task;

      var rule = new ScheduleRule
      {
        RuleId = scheduleRule.RuleId,
        Name = title ?? scheduleRule.Name,
        Active = scheduleRule.Active,

        Targets = new List<IScheduleRuleTarget>(),

        ChannelGroupId = channelGroup?.ChannelGroupId ?? scheduleRule.ChannelGroupId,
        ChannelId = channel?.ChannelId ?? scheduleRule.ChannelId,

        IsSeries = isSeries ?? scheduleRule.IsSeries,
        SeriesName = seriesName ?? scheduleRule.SeriesName,
        SeasonNumber = seasonNumber ?? scheduleRule.SeasonNumber,
        EpisodeNumber = episodeNumber ?? scheduleRule.EpisodeNumber,
        EpisodeTitle = episodeTitle ?? scheduleRule.EpisodeTitle,
        EpisodeInfoFallback = episodeInfoFallback ?? scheduleRule.EpisodeInfoFallback,
        EpisodeInfoFallbackType = episodeInfoFallbackType ?? scheduleRule.EpisodeInfoFallbackType,
        EpisodeManagementScheme = episodeManagementScheme ?? scheduleRule.EpisodeManagementScheme,

        StartFromTime = from ?? scheduleRule.StartFromTime,
        StartToTime = to ?? scheduleRule.StartToTime,
        StartOnOrAfterDay = afterDay ?? scheduleRule.StartOnOrAfterDay,
        StartOnOrBeforeDay = beforeDay ?? scheduleRule.StartOnOrBeforeDay,

        Priority = (PriorityType)(priority ?? (int)scheduleRule.Priority),

        RecordingType = recordingType ?? scheduleRule.RecordingType,
        PreRecordInterval = TimeSpan.FromMinutes(preRecordInterval ?? scheduleRule.PreRecordInterval.TotalMinutes),
        PostRecordInterval = TimeSpan.FromMinutes(postRecordInterval ?? scheduleRule.PostRecordInterval.TotalMinutes),

        KeepMethod = keepMethod ?? scheduleRule.KeepMethod,
        KeepDate = keepDate ?? scheduleRule.KeepDate,
      };
      if (targets != null && targets.Any())
      {
        rule.Targets.Clear();
        foreach (var target in scheduleRule.Targets)
          rule.Targets.Add(target);
      }
      
      var result = await EditProviderScheduleRuleAsync(rule);
      if (result)
        _checkCacheUpToDate = false;
      return result;
    }

    protected virtual async Task<bool> RemoveProviderScheduleRuleAsync(IScheduleRule scheduleRule)
    {
      var service = ServiceRegistration.Get<ISettingsManager>(false);
      if (service == null)
        return false;

      using (await _scheduleRuleAccess.WriterLockAsync())
      {
        var settings = service.Load<SlimTvScheduleRulesSettings>();
        var existing = settings.ScheduleRules.FirstOrDefault(r => r.RuleId == scheduleRule.RuleId);
        if (existing != null)
        {
          settings.ScheduleRules.Remove(existing);
          service.Save(settings);
        }
      }

      return true;
    }

    public async Task<bool> RemoveScheduleRuleAsync(IScheduleRule scheduleRule)
    {
      await _initComplete.Task;

      var result = await RemoveProviderScheduleRuleAsync(scheduleRule);
      if (result)
        _checkCacheUpToDate = false;
      return result;
    }

    /// <summary>
    /// Parses the scheduled recordings and updates the conflicting recordings table
    /// </summary>
    public async Task<AsyncResult<IList<IProgram>>> GetConflictsForScheduleRuleAsync(IScheduleRule scheduleRule)
    {
      try
      {
        await _initComplete.Task;

        DateTime startUpdate = DateTime.UtcNow;
        Logger.Info($"SlimTvService: Generating conflicts for schedule rule '{scheduleRule.Name}'");

        CollectionCache cache = await InitCacheAsync(_checkScheduleMaxSpan.TotalDays, false, scheduleRule);
        var conflicts = cache.Conflicts.Where(c => c.CardAssignment.Schedule.ScheduleId == -scheduleRule.RuleId)
          .Select(c => c.ConflictingCardAssignment.Program ?? CreateProgramPlaceholderForSchedule(c.ConflictingCardAssignment.Schedule))
          .Distinct(_programComparer)
          .ToList();
        Logger.Info("SlimTvService: Found {0} conflicts for schedule rule '{1}' {2} ms", conflicts.Count, scheduleRule.Name, (DateTime.UtcNow - startUpdate).TotalMilliseconds);
        return new AsyncResult<IList<IProgram>>(conflicts.Count > 0, conflicts);
      }
      catch (Exception ex)
      {
        Logger.Error($"SlimTvService: Error generating conflicts for schedule rule '{scheduleRule.Name}'", ex);
      }
      return new AsyncResult<IList<IProgram>>(false, null);
    }

    protected IProgram CreateProgramPlaceholderForSchedule(ISchedule schedule)
    {
      return new Program()
      {
        ChannelId = schedule.ChannelId,
        StartTime = schedule.StartTime,
        EndTime = schedule.EndTime,
        Title = schedule.Name,
      };
    }

    /// <summary>
    /// Parses the scheduled recordings and updates the conflicting recordings table
    /// </summary>
    public async Task<AsyncResult<IList<IProgram>>> GetProgramsForScheduleRuleAsync(IScheduleRule scheduleRule)
    {
      try
      {
        await _initComplete.Task;

        double days = _checkScheduleMaxSpan.TotalDays;
        DateTime startUpdate = DateTime.UtcNow;
        Logger.Info($"SlimTvService: Generating programs for schedule rule '{scheduleRule.Name}'");

        CollectionCache cache = await InitCacheAsync(days, false, scheduleRule);
        List<IProgram> allPrograms = new List<IProgram>();
        foreach (var ca in cache.CardAssignments)
        {
          foreach (var a in ca.Value)
          {
            if (a.ScheduleRule.RuleId == scheduleRule.RuleId)
            {
              var prog = GetCachedProgramForSchedule(a.Schedule, cache);
              if (prog != null)
                allPrograms.Add(prog);
            }
          }
        }
        Logger.Info("SlimTvService: Found {0} programs for schedule rule '{1}' {2} ms", allPrograms.Count, scheduleRule.Name, (DateTime.UtcNow - startUpdate).TotalMilliseconds);

        if (!allPrograms.Any())
        {
          return new AsyncResult<IList<IProgram>>(true, new List<IProgram>());
        }
        else
        {
          return new AsyncResult<IList<IProgram>>(true, allPrograms
            .Where(p => p != null)
            .Distinct(_programComparer)
            .ToList());
        }
      }
      catch (Exception ex)
      {
        Logger.Error($"SlimTvService: Error generating programs for schedule rule '{scheduleRule.Name}'", ex);
      }
      return new AsyncResult<IList<IProgram>>(false, null);
    }

    /// <summary>
    /// Parses the scheduled recordings and updates the conflicting recordings table
    /// </summary>
    protected async Task<int> CreateRuleSchedulesAsync(IList<(int CardId, IScheduleRule ScheduleRule, ISchedule Schedule, IProgram Program)> ruleSchedules)
    {
      if (!ruleSchedules.Any())
        return 0;

      try
      {
        await _initComplete.Task;

        DateTime startUpdate = DateTime.UtcNow;
        Logger.Debug($"ConflictManager: Create rule schedules");

        // Save in tv layer
        int count = 0;
        int failed = 0;
        IDictionary<int, (IScheduleRule ScheduleRule, ISchedule Schedule)> affectedRules = new Dictionary<int, (IScheduleRule, ISchedule)>();
        foreach (var rs in ruleSchedules)
        {
          //Get program
          var channelResult = await GetProviderChannelAsync(rs.Schedule.ChannelId);
          if (!channelResult.Success)
          {
            failed++;
            continue;
          }

          //Try to save in series or movie folder if any
          var recordingType = ScheduleRecordingType.Once;
          var directory = GetRecordingFolderForProgram(rs.CardId, rs.Program.ProgramId, rs.ScheduleRule.IsSeries);

          //Add new schedule
          var result = await CreateProviderScheduleDetailedAsync(channelResult.Result,
            rs.Schedule.Name,
            rs.Schedule.StartTime,
            rs.Schedule.EndTime,
            recordingType,
            Convert.ToInt32(rs.Schedule.PreRecordInterval.TotalMinutes),
            Convert.ToInt32(rs.Schedule.PostRecordInterval.TotalMinutes),
            directory,
            (int)rs.Schedule.Priority);
          if (result.Success)
          {
            count++;
            if (!affectedRules.ContainsKey(rs.ScheduleRule.RuleId))
              affectedRules.Add(rs.ScheduleRule.RuleId, (rs.ScheduleRule, rs.Schedule));
          }
          else
          {
            failed++;
          }

          _checkCacheUpToDate = false;
        }

        foreach (var r in affectedRules)
        {
          ChangeScheduleRule(r.Value);
          await EditProviderScheduleRuleAsync(r.Value.ScheduleRule);
        }

        Logger.Debug("ConflictManager: Stored {0} rule schedules ({1} failed) {2} ms", count, failed, (DateTime.UtcNow - startUpdate).TotalMilliseconds);
        return count;
      }
      catch (Exception ex)
      {
        Logger.Error("ConflictManager: Error creating rule schedules", ex);
      }

      return 0;
    }

    protected virtual async Task<bool> UpdateProviderScheduleRuleActivationAsync(IScheduleRule scheduleRule, bool active)
    {
      var service = ServiceRegistration.Get<ISettingsManager>(false);
      if (service == null)
        return false;

      using (await _scheduleRuleAccess.WriterLockAsync())
      {
        var settings = service.Load<SlimTvScheduleRulesSettings>();
        var existing = settings.ScheduleRules.FirstOrDefault(r => r.RuleId == scheduleRule.RuleId);
        if (existing != null)
        {
          existing.Active = active;
          service.Save(settings);
        }
      }

      return true;
    }

    public async Task<bool> UpdateScheduleRuleActivationAsync(IScheduleRule scheduleRule, bool active)
    {
      await _initComplete.Task;

      var result = await UpdateProviderScheduleRuleActivationAsync(scheduleRule, active);
      if (result)
        _checkCacheUpToDate = false;
      return result;
    }

    #endregion

    #region Conflicts

    protected abstract Task<AsyncResult<IList<IConflict>>> GetProviderConflictsAsync();

    public async Task<AsyncResult<IList<IConflict>>> GetConflictsAsync()
    {
      var conflictsResult = await GetProviderConflictsAsync();
      List<IConflict> conflicts = new List<IConflict>();
      var endDate = _nowTime.AddDays(_checkScheduleMaxSpan.TotalDays);
      if (conflictsResult.Success)
        conflicts.AddRange(conflictsResult.Result.Where(c => c.ProgramStartTime <= endDate));

      return new AsyncResult<IList<IConflict>>(conflicts.Any(), conflicts.Any() ? conflicts : null);
    }

    protected abstract Task<bool> RemoveAllProviderConflictsAsync();

    protected abstract Task<bool> SaveProviderConflictsAsync(IList<IConflict> conflicts);

    /// <summary>
    /// Parses the scheduled recordings and updates the conflicting recordings table
    /// </summary>
    protected async Task<int> ReplaceConflictsAsync(IList<IConflict> conflicts)
    {
      if (!conflicts.Any())
        return 0;

      try
      {
        await _initComplete.Task;

        DateTime startUpdate = DateTime.UtcNow;
        Logger.Debug($"ConflictManager: Replacing conflicts for all schedules");

        // Save conflicts in tv layer
        await RemoveAllProviderConflictsAsync();
        await SaveProviderConflictsAsync(conflicts);
        Logger.Debug("ConflictManager: Stored {0} conflicts {1} ms", conflicts.Count, (DateTime.UtcNow - startUpdate).TotalMilliseconds);

        return conflicts.Count;
      }
      catch (Exception ex)
      {
        Logger.Error("ConflictManager: Error replacing conflicts", ex);
      }
      return 0;
    }

    #endregion

    #region Conversion

    protected abstract Program ConvertToProgram(TProgram tvProgram, bool includeRecordingStatus = false);
    protected abstract Channel ConvertToChannel(TChannel tvChannel);
    protected abstract ChannelGroup ConvertToChannelGroup(TChannelGroup tvGroup);
    protected abstract Schedule ConvertToSchedule(TSchedule schedule);
    protected abstract TuningDetail ConvertToTuningDetail(TTuningDetail tuningDetail);
    protected abstract Recording ConvertToRecording(TRecording recording);
    protected abstract ScheduleRule ConvertToScheduleRule(TScheduleRule scheduleRule);
    protected abstract Conflict ConvertToConflict(TConflict conflict);

    #endregion

    protected abstract Task<string> SwitchProviderToChannelAsync(string userName, int channelId);
    protected abstract Task<MediaItem> CreateMediaItemAsync(int slotIndex, string streamUrl, IChannel channel);

    protected virtual async Task<MediaItem> CreateMediaItemAsync(int slotIndex, string streamUrl, IChannel channel, bool isTv, IChannel fullChannel)
    {
      await _initComplete.Task;

      LiveTvMediaItem tvStream = isTv
        ? SlimTvMediaItemBuilder.CreateMediaItem(slotIndex, streamUrl, fullChannel)
        : SlimTvMediaItemBuilder.CreateRadioMediaItem(slotIndex, streamUrl, fullChannel);

      if (tvStream != null)
      {
        // Add program infos to the LiveTvMediaItem
        var result = await GetProviderNowNextProgramAsync(channel);
        if (result.Success)
        {
          tvStream.AdditionalProperties[LiveTvMediaItem.CURRENT_PROGRAM] = result.Result[0];
          tvStream.AdditionalProperties[LiveTvMediaItem.NEXT_PROGRAM] = result.Result[1];
        }

        return tvStream;
      }

      return null;
    }

    #region Helper methods

    protected static string GetUserName(string clientName, int slotIndex)
    {
      return string.Format("{0}-{1}", clientName, slotIndex);
    }

    protected async Task<CollectionCache> InitCacheAsync(double days, bool autoResolve, params object[] extraSchedules)
    {
      DateTime startUpdate = DateTime.UtcNow;
      CollectionCache cache = new CollectionCache();

      // Initialize
      await InitializeCardsCacheAsync(cache);
      await InitializeSeriesCacheAsync(cache);
      Logger.Debug("SlimTvService: Cache initialized {0} ms", (DateTime.UtcNow - startUpdate).TotalMilliseconds);
      startUpdate = DateTime.UtcNow;

      // Add schedules
      var scheduleResult = await GetProviderSchedulesAsync();
      if (scheduleResult.Success)
      {
        foreach (var schedule in extraSchedules.OfType<ISchedule>())
        {
          var exists = scheduleResult.Result.Any(s => s.ScheduleId == schedule.ScheduleId);
          if (!exists)
            scheduleResult.Result.Add(schedule);
        }

        await AddSchedulesToCacheAsync(scheduleResult.Result, cache, days);
      }
      Logger.Debug("SlimTvService: Planned schedule list built {0} ms", (DateTime.UtcNow - startUpdate).TotalMilliseconds);
      startUpdate = DateTime.UtcNow;

      // Add rule based schedules
      var rulesResult = await GetProviderScheduleRulesAsync();
      if (rulesResult.Success)
      {
        foreach (var rule in extraSchedules.OfType<IScheduleRule>())
        {
          var exists = rulesResult.Result.Any(s => s.RuleId == rule.RuleId);
          if (!exists)
            rulesResult.Result.Add(rule);
        }

        await AddRuleSchedulesToCacheAsync(rulesResult.Result, cache, days);
      }
      Logger.Debug("SlimTvService: Rule based schedule list built {0} ms", (DateTime.UtcNow - startUpdate).TotalMilliseconds);
      startUpdate = DateTime.UtcNow;

      // Try to assign all schedules to existing cards
      await AssignCachedSchedulesToCardsAsync(cache, days, autoResolve);
      Logger.Debug("SlimTvService: Assigned {0} schedules to {1} cards {2} ms", cache.PlannedSchedules.Count + cache.PlannedRuleSchedules.Count, cache.Cards.Count, (DateTime.UtcNow - startUpdate).TotalMilliseconds);
      startUpdate = DateTime.UtcNow;

      return cache;
    }

    protected async Task<CardAssignment> AssignScheduleToCardAsync(ISchedule schedule, IProgram program, CollectionCache cache, IScheduleRule scheduleRule, bool isSeries, bool autoResolve)
    {
      await _initComplete.Task;

      IDictionary<int, IList<CardAssignment>> replaceableAssignments = new Dictionary<int, IList<CardAssignment>>();
      IDictionary<int, IList<(CardAssignment Assignment, CardAssignment ConflictAssignment)>> conflictingAssignments = new Dictionary<int, IList<(CardAssignment, CardAssignment)>>();
      
      bool isAssigned = false;
      CardAssignment cardAssignment = null;
      foreach (ICard card in cache.Cards.Values.OrderByDescending(c => c.Priority))
      {
        if (!cache.CardAssignments.ContainsKey(card.CardId))
          cache.CardAssignments.Add(card.CardId, new List<CardAssignment>());

        if (!conflictingAssignments.ContainsKey(card.CardId))
          conflictingAssignments.Add(card.CardId, new List<(CardAssignment, CardAssignment)>());

        if (!replaceableAssignments.ContainsKey(card.CardId))
          replaceableAssignments.Add(card.CardId, new List<CardAssignment>());

        if (!cache.CardChannelTunings.ContainsKey(card.CardId) || !cache.CardChannelTunings[card.CardId].ContainsKey(schedule.ChannelId))
        {
          var detailResult = await GetProviderTuningDetailsAsync(card, cache.Channels[schedule.ChannelId]);
          if (!detailResult.Success)
            continue;

          if (!cache.CardChannelTunings.ContainsKey(card.CardId))
            cache.CardChannelTunings.Add(card.CardId, new Dictionary<int, ITuningDetail>());

          cache.CardChannelTunings[card.CardId].Add(schedule.ChannelId, detailResult.Result);
        }

        var tuningDetail = cache.CardChannelTunings[card.CardId][schedule.ChannelId];
        cardAssignment = new CardAssignment() { CardId = card.CardId, Tuning = tuningDetail, Schedule = schedule, ScheduleRule = scheduleRule, Program = program };
        bool isFree = true;
        bool isReplace = true;
        foreach (var conflictAssignment in cache.CardAssignments[card.CardId])
        {
          bool isConflict = false;
          if (IsScheduleOverlap(schedule, program, conflictAssignment.Schedule, conflictAssignment.Program))
          {
            if (!card.SupportSubChannels || !IsSameTransmitter(tuningDetail, conflictAssignment.Tuning))
            {
              isFree = false;
              isConflict = true;
            }
            else if (tuningDetail.IsEncrypted && IsCardDecryptLimitReached(card, schedule, program, cache.CardAssignments[card.CardId]))
            {
              isFree = false;
              isConflict = true;
            }

            if (autoResolve && scheduleRule != null && IsSameSchedule(schedule, program, conflictAssignment.Schedule, conflictAssignment.Program))
            {
              //Ignore rule as this program is already being recorded
              isFree = false;
              isConflict = false;
              isAssigned = true;
              cardAssignment = null;
              Logger.Debug($"ConflictManager: Schedule {schedule.Name} was ignored because a non-rule based schedule is already pending");
            }
            else if (conflictAssignment.Schedule.Priority <= schedule.Priority)
            {
              isReplace = false;
              isConflict = true;
            }
            else
            {
              replaceableAssignments[card.CardId].Add(conflictAssignment);
            }

            if (isConflict)
              conflictingAssignments[card.CardId].Add((cardAssignment, conflictAssignment));
          }
        }

        if (isFree)
        {
          cache.CardAssignments[card.CardId].Add(cardAssignment);
          conflictingAssignments.Clear();
          isAssigned = true;
          Logger.Debug($"ConflictManager: Scheduled {schedule.Name} assigned to card {card.Name}");
        }

        if (!isReplace || isAssigned)
          replaceableAssignments[card.CardId].Clear();

        if (isAssigned)
          break;
      }

      if (autoResolve && !isAssigned && replaceableAssignments.Count > 0)
      {
        foreach (ICard card in cache.Cards.Values.OrderByDescending(c => c.Priority))
        {
          if (!cache.CardChannelTunings.ContainsKey(card.CardId) || !cache.CardChannelTunings[card.CardId].ContainsKey(schedule.ChannelId))
            continue;

          if (!replaceableAssignments.ContainsKey(card.CardId) || !replaceableAssignments[card.CardId].Any())
            continue;

          var tuningDetail = cache.CardChannelTunings[card.CardId][schedule.ChannelId];
          cardAssignment = new CardAssignment() { CardId = card.CardId, Tuning = tuningDetail, Schedule = schedule, ScheduleRule = scheduleRule, Program = program };
          cache.CardAssignments[card.CardId].Add(cardAssignment);

          conflictingAssignments.Clear();
          conflictingAssignments[card.CardId] = new List<(CardAssignment, CardAssignment)>();
          foreach (var replaced in replaceableAssignments[card.CardId])
          {
            RemoveCreatedCacheInfo(replaced.CreatedInfo, cache);
            cache.CardAssignments[card.CardId].Remove(replaced);
            conflictingAssignments[card.CardId].Add((replaced, cardAssignment));
            Logger.Debug($"ConflictManager: Schedule {replaced.Schedule.Name} on card {card.Name} was replaced by {schedule.Name}");
          }

          isAssigned = true;
          Logger.Debug($"ConflictManager: Schedule {schedule.Name} assigned to card {card.Name}");
          break;
        }
      }

      if (!isAssigned)
        Logger.Debug($"ConflictManager: Schedule {schedule.Name} could not be assigned to any cards");

      foreach (var conflict in conflictingAssignments.SelectMany(c => c.Value))
        cache.Conflicts.Add(conflict);

      return isAssigned ? cardAssignment : null;
    }

    /// <summary>Assign all schedules to cards</summary>
    protected async Task AssignCachedSchedulesToCardsAsync(CollectionCache cache, double days, bool autoResolve)
    {
      foreach (ISchedule schedule in cache.PlannedSchedules)
        await AssignScheduleCardAssignmentAsync(schedule, cache, null, schedule.IsSeries, schedule.IsSeries, false, autoResolve, days);
      
      foreach (var scheduleRule in cache.PlannedRuleSchedules)
        await AssignScheduleCardAssignmentAsync(scheduleRule.Schedule, cache, scheduleRule.Rule, scheduleRule.Rule.IsSeries, scheduleRule.Rule.IsSeries, true, autoResolve, days);
    }

    protected bool IsSameSchedule(ISchedule ruleSchedule, IProgram ruleProgram, ISchedule normalSchedule, IProgram normalProgram)
    {
      if (ruleSchedule.StartTime == normalSchedule.StartTime && ruleSchedule.EndTime == normalSchedule.EndTime)
        return true;

      if (ruleProgram.ProgramId == normalProgram.ProgramId)
        return true;

      return false;
    }

    protected bool IsScheduleStillValidForRule(ISchedule schedule, IScheduleRule rule)
    {
      if (!rule.Active)
        return false;

      if (rule.RecordingType == RuleRecordingType.AllOnSameChannel || rule.RecordingType == RuleRecordingType.AllOnSameChannelAndDay)
      {
        if (rule.ChannelId > 0 && rule.ChannelId != schedule.ChannelId)
          return false;
      }

      if (rule.RecordingType == RuleRecordingType.AllOnSameDay || rule.RecordingType == RuleRecordingType.AllOnSameChannelAndDay)
      {
        var validDays = GetValidDayOfWeeks(rule.StartOnOrAfterDay, rule.StartOnOrBeforeDay);
        if (!validDays.Contains(schedule.StartTime.DayOfWeek))
          return false;
      }

      return true;
    }

    protected bool ChangeScheduleRule((IScheduleRule Rule, ISchedule Schedule) scheduleRule)
    {
      bool changedRule = false;
      if (scheduleRule.Rule.RecordingType == RuleRecordingType.Once && scheduleRule.Rule.Active)
      {
        scheduleRule.Rule.Active = false;
        changedRule = true;
      }
      if ((scheduleRule.Rule.RecordingType == RuleRecordingType.AllOnSameChannel || scheduleRule.Rule.RecordingType == RuleRecordingType.AllOnSameChannelAndDay) && !scheduleRule.Rule.ChannelId.HasValue)
      {
        scheduleRule.Rule.ChannelId = scheduleRule.Schedule.ChannelId;
        changedRule = true;
      }
      if ((scheduleRule.Rule.RecordingType == RuleRecordingType.AllOnSameDay || scheduleRule.Rule.RecordingType == RuleRecordingType.AllOnSameChannelAndDay) && !scheduleRule.Rule.StartOnOrAfterDay.HasValue)
      {
        scheduleRule.Rule.StartOnOrAfterDay = scheduleRule.Schedule.StartTime.DayOfWeek;
        scheduleRule.Rule.StartOnOrBeforeDay = scheduleRule.Schedule.StartTime.DayOfWeek;
        changedRule = true;
      }

      return changedRule;
    }

    protected async Task<bool> AssignScheduleCardAssignmentAsync(ISchedule schedule, CollectionCache cache, IScheduleRule scheduleRule, bool isSeries, bool skipDuplicateSeries, bool skipDuplicateRecordings, bool autoResolve, double days)
    {
      if (scheduleRule != null && !IsScheduleStillValidForRule(schedule, scheduleRule))
        return false;

      if (!await UpdateChannelCacheAsync(schedule.ChannelId, cache))
        return false;

      await UpdateChannelProgramsCacheAsync(cache.Channels[schedule.ChannelId], cache, days);
      var program = GetCachedProgramForSchedule(schedule, cache);

      BaseInfo info = null;
      if ((isSeries && skipDuplicateSeries) || skipDuplicateRecordings)
      {
        var result = await IsScheduleHandledAsync(schedule, program, scheduleRule?.EpisodeManagementScheme, cache, skipDuplicateSeries, skipDuplicateRecordings);
        if (result.Handled)
        {
          if (scheduleRule == null)
            cache.CancelledSchedules.Add(schedule);
          return false;
        }

        info = result.Info;
      }

      var assignment = await AssignScheduleToCardAsync(schedule, program, cache, scheduleRule, isSeries, autoResolve);
      if (assignment != null)
      {
        if (scheduleRule != null)
        {
          ChangeScheduleRule((scheduleRule, schedule));

          if (_localRuleHandling)
          {
            RecordingStatus status = RecordingStatus.Scheduled;
            if (isSeries)
              status |= RecordingStatus.SeriesScheduled;

            status |= RecordingStatus.RuleScheduled;

            cache.ProgramRecordingStatuses.Add(program.ProgramId, status);
          }
        }

        assignment.CreatedInfo = info;
        return true;
      }

      return false;
    }

    protected async Task<IList<ISchedule>> GetPlannedSchedulesAsync(IList<ISchedule> schedules, CollectionCache cache, double days)
    {
      IList<ISchedule> plannedSchedules = new List<ISchedule>();

      CollectionUtils.AddAll(plannedSchedules, await GetRecordOnceSchedulesAsync(schedules));
      CollectionUtils.AddAll(plannedSchedules, await GetDailySchedulesAsync(schedules, days));
      CollectionUtils.AddAll(plannedSchedules, await GetWeeklySchedulesAsync(schedules, days));
      CollectionUtils.AddAll(plannedSchedules, await GetWeekendSchedulesAsync(schedules, days));
      CollectionUtils.AddAll(plannedSchedules, await GetWorkingDaySchedulesAsync(schedules, days));
      CollectionUtils.AddAll(plannedSchedules, await GetWeeklyEveryTimeOnThisChannelSchedulesAsync(schedules, cache, days));
      CollectionUtils.AddAll(plannedSchedules, await GetEveryTimeOnEveryChannelSchedulesAsync(schedules, cache, days));
      CollectionUtils.AddAll(plannedSchedules, await GetEveryTimeOnThisChannelSchedulesAsync(schedules, cache, days));

      return plannedSchedules.OrderBy(s => s.StartTime).ThenBy(s => s.Priority).ThenBy(s => s.ChannelId).ToList();
    }

    protected async Task AddSchedulesToCacheAsync(IList<ISchedule> schedules, CollectionCache cache, double days)
    {
      // Parses all schedules and add the calculated incoming schedules 
      CollectionUtils.AddAll(cache.PlannedSchedules, await GetPlannedSchedulesAsync(schedules, cache, days));

      await RemoveCachedCanceledSchedulesAsync(cache);
    }

    /// <summary>
    /// Adds "Record Once" Schedules in a list of schedules
    /// </summary>
    /// <param name="schedules">List containing the schedules to parse</param>
    /// <returns>Collection containing the "record once" schedules</returns>
    protected Task<IList<ISchedule>> GetRecordOnceSchedulesAsync(IList<ISchedule> schedules)
    {
      IList<ISchedule> plannedSchedules = new List<ISchedule>();
      foreach (ISchedule schedule in schedules.ToList())
      {
        if (schedule.RecordingType != ScheduleRecordingType.Once)
          continue;

        plannedSchedules.Add(schedule);
        schedules.Remove(schedule);
      }
      return Task.FromResult(plannedSchedules);
    }

    /// <summary>
    /// Adds Daily Schedules in a given list of schedules
    /// </summary>
    /// <param name="schedules">List containing the schedules to parse</param>
    /// <returns>Collection containing the Daily schedules</returns>
    protected Task<IList<ISchedule>> GetDailySchedulesAsync(IList<ISchedule> schedules, double days)
    {
      IList<ISchedule> plannedSchedules = new List<ISchedule>();
      foreach (ISchedule schedule in schedules.ToList())
      {
        if (schedule.RecordingType != ScheduleRecordingType.Daily)
          continue;

        // Create a temporary base schedule with today's date (will be used to calculate incoming schedules)
        Schedule baseSchedule = CreateScheduleClone(schedule, _nowTime);

        // Generate the daily schedules
        for (int i = 0; i <= days; i++)
        {
          var tempDate = _nowTime.AddDays(i);
          if (tempDate.Date >= schedule.StartTime.Date)
          {
            Schedule incomingSchedule = CreateScheduleClone(baseSchedule);
            incomingSchedule.StartTime = incomingSchedule.StartTime.AddDays(i);
            incomingSchedule.EndTime = incomingSchedule.EndTime.AddDays(i);
            plannedSchedules.Add(incomingSchedule);
          }
        }

        schedules.Remove(schedule);
      }
      return Task.FromResult(plannedSchedules);
    }

    /// <summary>
    /// Adds Weekly Schedules in a given list of schedules
    /// </summary>
    /// <param name="schedules">List containing the schedules to parse</param>
    /// <returns>Collection containing the Weekly schedules</returns>
    protected Task<IList<ISchedule>> GetWeeklySchedulesAsync(IList<ISchedule> schedules, double days)
    {
      IList<ISchedule> plannedSchedules = new List<ISchedule>();
      foreach (ISchedule schedule in schedules.ToList())
      {
        if (schedule.RecordingType != ScheduleRecordingType.Weekly)
          continue;

        //  Generate the weekly schedules
        for (int i = 0; i <= days; i++)
        {
          var tempDate = _nowTime.AddDays(i);
          if ((tempDate.DayOfWeek == schedule.StartTime.DayOfWeek) && (tempDate.Date >= schedule.StartTime.Date))
          {
            Schedule tempSchedule = CreateScheduleClone(schedule, tempDate);
            plannedSchedules.Add(tempSchedule);
          }
        }

        schedules.Remove(schedule);
      }
      return Task.FromResult(plannedSchedules);
    }

    /// <summary>
    /// Adds Weekends Schedules in a given list of schedules
    /// </summary>
    /// <param name="schedules">List containing the schedules to parse</param>
    /// <returns>Collection containing the Weekends schedules</returns>
    protected Task<IList<ISchedule>> GetWeekendSchedulesAsync(IList<ISchedule> schedules, double days)
    {
      IList<ISchedule> plannedSchedules = new List<ISchedule>();
      foreach (ISchedule schedule in schedules.ToList())
      {
        if (schedule.RecordingType != ScheduleRecordingType.Weekends)
          continue;

        //  Generate the weekly schedules
        for (int i = 0; i <= days; i++)
        {
          var tempDate = _nowTime.AddDays(i);
          if (IsWeekend(tempDate.DayOfWeek) && (tempDate.Date >= schedule.StartTime.Date))
          {
            Schedule tempSchedule = CreateScheduleClone(schedule, tempDate);
            plannedSchedules.Add(tempSchedule);
          }
        }

        schedules.Remove(schedule);
      }
      return Task.FromResult(plannedSchedules);
    }

    /// <summary>
    /// Adds WorkingDays Schedules in a given list of schedules 
    /// </summary>
    /// <param name="schedules">List containing the schedules to parse</param>
    /// <returns>Collection containing the WorkingDays schedules</returns>
    protected Task<IList<ISchedule>> GetWorkingDaySchedulesAsync(IList<ISchedule> schedules, double days)
    {
      IList<ISchedule> plannedSchedules = new List<ISchedule>();
      foreach (ISchedule schedule in schedules.ToList())
      {
        if (schedule.RecordingType != ScheduleRecordingType.WorkingDays)
          continue;

        //  Generate the weekly schedules
        for (int i = 0; i <= days; i++)
        {
          var tempDate = _nowTime.AddDays(i);
          if ((!IsWeekend(tempDate.DayOfWeek)) && (tempDate.Date >= schedule.StartTime.Date))
          {
            Schedule tempSchedule = CreateScheduleClone(schedule, tempDate);
            plannedSchedules.Add(tempSchedule);
          }
        }

        schedules.Remove(schedule);
      }
      return Task.FromResult(plannedSchedules);
    }

    /// <summary>
    /// Adds incoming "EveryTimeOnThisChannel" type schedules in Program Table
    /// </summary>
    /// <param name="schedules">List containing the schedules to parse</param>
    /// <returns>Collection containing the schedules</returns>
    protected async Task<IList<ISchedule>> GetEveryTimeOnEveryChannelSchedulesAsync(IList<ISchedule> schedules, CollectionCache cache, double days)
    {
      IList<ISchedule> plannedSchedules = new List<ISchedule>();
      foreach (ISchedule schedule in schedules.ToList())
      {
        if (schedule.RecordingType != ScheduleRecordingType.EveryTimeOnEveryChannel)
          continue;

        var progs = await GetAllCachedScheduleProgramsAsync(schedule, cache, days);
        if (progs == null)
          continue;

        foreach (IProgram program in progs)
        {
          Schedule incomingSchedule = CreateScheduleClone(schedule);
          incomingSchedule.ChannelId = program.ChannelId;
          incomingSchedule.Name = program.Title;
          incomingSchedule.StartTime = program.StartTime;
          incomingSchedule.EndTime = program.EndTime;
          incomingSchedule.PreRecordInterval = schedule.PreRecordInterval;
          incomingSchedule.PostRecordInterval = schedule.PostRecordInterval;
          plannedSchedules.Add(incomingSchedule);
        }

        schedules.Remove(schedule);
      }

      return plannedSchedules;
    }

    /// <summary>
    /// Adds the every time on this channel schedules.
    /// </summary>
    /// <param name="schedules">The schedules list.</param>
    /// <returns></returns>
    protected async Task<IList<ISchedule>> GetEveryTimeOnThisChannelSchedulesAsync(IList<ISchedule> schedules, CollectionCache cache, double days)
    {
      IList<ISchedule> plannedSchedules = new List<ISchedule>();
      foreach (ISchedule schedule in schedules.ToList())
      {
        if (schedule.RecordingType != ScheduleRecordingType.EveryTimeOnThisChannel)
          continue;

        var progs = await GetAllCachedScheduleProgramsAsync(schedule, cache, days);
        if (progs == null)
          continue;

        foreach (IProgram program in progs)
        {
          if (program.ChannelId == schedule.ChannelId)
          {
            Schedule incomingSchedule = CreateScheduleClone(schedule);
            incomingSchedule.ChannelId = program.ChannelId;
            incomingSchedule.Name = program.Title;
            incomingSchedule.StartTime = program.StartTime;
            incomingSchedule.EndTime = program.EndTime;
            incomingSchedule.PreRecordInterval = schedule.PreRecordInterval;
            incomingSchedule.PostRecordInterval = schedule.PostRecordInterval;
            plannedSchedules.Add(incomingSchedule);
          }
        }

        schedules.Remove(schedule);
      }

      return plannedSchedules;
    }

    /// <summary>
    /// Adds the Weekly every time on this channel schedules.
    /// </summary>
    /// <param name="schedules">The schedules list.</param>
    /// <returns></returns>
    protected async Task<IList<ISchedule>> GetWeeklyEveryTimeOnThisChannelSchedulesAsync(IList<ISchedule> schedules, CollectionCache cache, double days)
    {
      IList<ISchedule> plannedSchedules = new List<ISchedule>();
      foreach (ISchedule schedule in schedules.ToList())
      {
        if (schedule.RecordingType != ScheduleRecordingType.WeeklyEveryTimeOnThisChannel)
          continue;

        var progs = await GetAllCachedScheduleProgramsAsync(schedule, cache, days);
        if (progs == null)
          continue;

        foreach (IProgram program in progs)
        {
          if (program.ChannelId == schedule.ChannelId && program.StartTime.DayOfWeek == schedule.StartTime.DayOfWeek)
          {
            Schedule incomingSchedule = CreateScheduleClone(schedule);
            incomingSchedule.ChannelId = program.ChannelId;
            incomingSchedule.Name = program.Title;
            incomingSchedule.StartTime = program.StartTime;
            incomingSchedule.EndTime = program.EndTime;
            incomingSchedule.PreRecordInterval = schedule.PreRecordInterval;
            incomingSchedule.PostRecordInterval = schedule.PostRecordInterval;
            plannedSchedules.Add(incomingSchedule);
          }
        }

        schedules.Remove(schedule);
      }

      return plannedSchedules;
    }

    /// <summary>
    /// Removes every canceled schedule.
    /// </summary>
    /// <param name="plannedSchedules">The schedules list.</param>
    /// <returns></returns>
    protected async Task RemoveCachedCanceledSchedulesAsync(CollectionCache cache)
    {
      var canceledResult = await GetProviderCanceledSchedulesAsync();
      if (!canceledResult.Success)
        return;

      foreach (var canceled in canceledResult.Result)
      {
        foreach (ISchedule schedule in cache.PlannedSchedules)
        {
          if (canceled.ScheduleId == schedule.ScheduleId && canceled.StartTime == schedule.StartTime)
          {
            cache.PlannedSchedules.Remove(schedule);
            break;
          }
        }
      }
    }

    protected async Task AddRuleSchedulesToCacheAsync(IList<IScheduleRule> scheduleRules, CollectionCache cache, double days)
    {
      CollectionUtils.AddAll(cache.PlannedRuleSchedules, await GetPlannedRuleSchedulesAsync(scheduleRules, cache, days));
    }

    protected async Task<IList<(IScheduleRule Rule, ISchedule Schedule)>> GetPlannedRuleSchedulesAsync(IList<IScheduleRule> scheduleRules, CollectionCache cache, double days)
    {
      IList<(IScheduleRule Rule, ISchedule Schedule)> ruleSchedules = new List<(IScheduleRule, ISchedule)>();
      foreach (IScheduleRule schedule in scheduleRules.OrderBy(s => s.Priority).ToList())
      {
        if (!schedule.Active)
          continue;

        if (schedule.ChannelGroupId == 0)
          continue;

        // Add channel data
        IList<IChannel> ruleChannels = new List<IChannel>();
        if (!schedule.ChannelId.HasValue)
        {
          ruleChannels = await GetCachedGroupChannelsAsync(schedule.ChannelGroupId, cache);
        }
        else
        {
          if (!await UpdateChannelCacheAsync(schedule.ChannelId.Value, cache))
            continue;

          ruleChannels.Add(cache.Channels[schedule.ChannelId.Value]);
        }
        if (!ruleChannels.Any())
          continue;

        foreach (var channel in ruleChannels)
        {
          await UpdateChannelProgramsCacheAsync(channel, cache, days);

          List<IProgram> validPrograms = cache.Programs[channel.ChannelId].Where(p => IsValidRuleProgram(schedule, p, cache)).ToList();
          foreach (IProgram program in validPrograms)
          {
            if (program.ChannelId == (schedule.ChannelId ?? channel.ChannelId) && program.EndTime >= _nowTime)
            {
              Schedule incomingSchedule = new Schedule
              {
                ScheduleId = -schedule.RuleId,
                ChannelId = program.ChannelId,
                Name = program.Title,
                StartTime = program.StartTime,
                EndTime = program.EndTime,
                PreRecordInterval = schedule.PreRecordInterval,
                PostRecordInterval = schedule.PostRecordInterval,
                RecordingType = schedule.IsSeries ? ScheduleRecordingType.WeeklyEveryTimeOnThisChannel : ScheduleRecordingType.Once,
                KeepDate = schedule.KeepDate,
                KeepMethod = schedule.KeepMethod,
                Priority = schedule.Priority,
              };
              ruleSchedules.Add((schedule, incomingSchedule));
            }
          }
        }

        scheduleRules.Remove(schedule);
      }

      return ruleSchedules.OrderBy(s => s.Schedule.StartTime).ThenBy(s => s.Schedule.Priority).ThenBy(s => s.Schedule.ChannelId).ToList();
    }

    protected void RemoveCreatedCacheInfo(BaseInfo info, CollectionCache cache)
    {
      if (info == null)
        return;

      if (info is EpisodeInfo epInfo)
        epInfo.Series?.Episodes.Remove(epInfo);

      if (info is RecordingInfo recInfo && cache.KnownRecordings.Any(r => r.Name.Equals(recInfo.Name, StringComparison.InvariantCultureIgnoreCase)))
        cache.KnownRecordings.Remove(recInfo);
    }

    protected async Task<(bool Handled, BaseInfo Info)> IsScheduleHandledAsync(ISchedule schedule, IProgram program, EpisodeManagementScheme? episodeManagementScheme, CollectionCache cache, bool skipSeries, bool skipRecordings)
    {
      if (skipSeries)
      {
        EpisodeInfo info = null;
        EpisodeManagementScheme seriesCheckScheme = episodeManagementScheme ?? (EpisodeManagementScheme)_serverSettings.DefaultEpisodeManagementScheme;
        if (program is IProgramSeries programSeries)
        {
          await UpdateSeriesEpisodesCacheAsync(programSeries.Title, cache);

          info = GetCachedEpisodeInfoFromProgram(programSeries, cache);
          if (info != null)
          {
            var seasonNo = ParseSeasonNumber(programSeries.SeasonNumber);
            var episodeNo = ParseEpisodeNumber(programSeries.EpisodeNumber);
            bool episodeNumberFound = seasonNo.HasValue && episodeNo.HasValue;
            if (episodeNumberFound)
            {
              if (seriesCheckScheme == EpisodeManagementScheme.NewEpisodesByEpisodeNumber)
              {
                var latestEpisode = info.Series.Episodes.OrderByDescending(e => e.SeasonNumber).ThenByDescending(e => e.EpisodeNumber).FirstOrDefault();
                if (latestEpisode != null && (latestEpisode.SeasonNumber > seasonNo || (latestEpisode.SeasonNumber == seasonNo && latestEpisode.EpisodeNumber >= episodeNo)))
                {
                  Logger.Debug($"ConflictManager: Scheduled {schedule.Name} was skipped because episode S{seasonNo:D2}E{episodeNo:D2} was lower than latest known episode S{latestEpisode.SeasonNumber:D2}E{latestEpisode.EpisodeNumber:D2}");
                  return (true, null);
                }
              }
              else if (seriesCheckScheme == EpisodeManagementScheme.MissingEpisodesByEpisodeNumber)
              {
                if (info.Series.Episodes.Any(e => e.SeasonNumber == seasonNo && e.EpisodeNumber == episodeNo))
                {
                  Logger.Debug($"ConflictManager: Scheduled {schedule.Name} was skipped because episode S{seasonNo:D2}E{episodeNo:D2} is already present");
                  return (true, null);
                }
              }
            }
            if (!string.IsNullOrWhiteSpace(programSeries.EpisodeTitle))
            {
              if (seriesCheckScheme == EpisodeManagementScheme.MissingEpisodesByEpisodeName)
              {
                if (info.Series.Episodes.Any(e => string.Equals(programSeries.EpisodeTitle, e.Name, StringComparison.InvariantCultureIgnoreCase)))
                {
                  Logger.Debug($"ConflictManager: Scheduled {schedule.Name} was skipped because episode '{programSeries.EpisodeTitle}' is already present");
                  return (true, null);
                }
              }
            }
            Logger.Debug($"ConflictManager: Scheduled {schedule.Name} will be recorded as episode S{seasonNo:D2}E{episodeNo:D2} '{programSeries.EpisodeTitle}'");
          }
          else
          {
            Logger.Debug($"ConflictManager: Scheduled {schedule.Name} will be recorded as an unknown episode");
          }
        }
        else
        {
          Logger.Debug($"ConflictManager: Scheduled {schedule.Name} will be recorded as an unknown episode");
        }

        info?.Series?.Episodes.Add(info);
        return (false, info);
      }

      if (skipRecordings)
      {
        var recording = cache.KnownRecordings.FirstOrDefault(r => r.Name.Equals(schedule.Name, StringComparison.InvariantCultureIgnoreCase));
        if (recording != null)
        {
          Logger.Debug($"ConflictManager: Scheduled {schedule.Name} was skipped because a recording with that name is already present");
          return (true, null);
        }

        var result = await GetRecordingsAsync(schedule.Name);
        if (result.Success)
        {
          recording = result.Result.Select(r => new RecordingInfo() { Name = r.Title }).FirstOrDefault();
          cache.KnownRecordings.Add(recording);
          Logger.Debug($"ConflictManager: Scheduled {schedule.Name} was skipped because a recording with that name is already present");
          return (true, null);
        }

        recording = new RecordingInfo() { Name = schedule.Name };
        cache.KnownRecordings.Add(recording);
        return (false, recording);
      }

      return (false, null);
    }

    protected async Task<IList<IChannel>> GetCachedGroupChannelsAsync(int? groupId, CollectionCache cache)
    {
      int gId = groupId ?? -1;
      if (!cache.Groups.ContainsKey(gId))
      {
        var groupResult = await GetChannelGroupsAsync();
        if (!groupResult.Success)
          return null;

        foreach (var group in groupResult.Result)
        {
          var channelResult = await GetChannelsAsync(group);
          if (channelResult.Success)
            cache.Groups[group.ChannelGroupId] = channelResult.Result;
          else
            cache.Groups[group.ChannelGroupId] = new List<IChannel>();

          foreach (var channel in cache.Groups[group.ChannelGroupId])
            cache.Channels[channel.ChannelId] = channel;
        }

        cache.Groups[-1] = cache.Channels.Values.ToList();
      }

      if (cache.Groups.ContainsKey(gId))
        return cache.Groups[gId];

      return null;
    }

    protected IProgram GetCachedProgramForSchedule(ISchedule schedule, CollectionCache cache)
    {
      if (!cache.Programs.ContainsKey(schedule.ChannelId))
        return null;

      IProgram matchedProgram = null;
      switch (schedule.RecordingType)
      {
        case ScheduleRecordingType.Once:
        case ScheduleRecordingType.EveryTimeOnThisChannel:
        case ScheduleRecordingType.WeeklyEveryTimeOnThisChannel:
          matchedProgram = cache.Programs[schedule.ChannelId].FirstOrDefault(p => p.Title.Equals(schedule.Name) && IsScheduleCoveringProgram(schedule, p) && p.ChannelId == schedule.ChannelId);
          break;
        case ScheduleRecordingType.Daily:
        case ScheduleRecordingType.Weekly:
        case ScheduleRecordingType.Weekends:
        case ScheduleRecordingType.WorkingDays:
          matchedProgram = cache.Programs[schedule.ChannelId].FirstOrDefault(p => IsScheduleCoveringProgram(schedule, p) && p.ChannelId == schedule.ChannelId);
          break;
      }

      if (_serverSettings.DetectMovedPrograms && !IsManualTitle(schedule.Name) && (matchedProgram == null || matchedProgram.EndTime > schedule.EndTime.Add(schedule.PostRecordInterval)))
      {
        var movedPrograms = cache.Programs[schedule.ChannelId]
          .Where(p => p.StartTime > _nowTime &&
            p.StartTime >= schedule.StartTime.AddMinutes(-_serverSettings.MovedProgramsDetectionWindow) &&
            p.StartTime <= schedule.StartTime.AddMinutes(_serverSettings.MovedProgramsDetectionWindow) &&
            IsScheduleProgramValid(schedule, p)).ToList();

        IProgram movedProgram = null;
        if (movedPrograms.Count > 1)
          movedProgram = movedPrograms.FirstOrDefault(p => string.Equals(p?.Title, schedule?.Name)); //Try to match by title
        else if (movedPrograms.Count == 1)
          movedProgram = movedPrograms.First();
        
        if (movedProgram != null)
        {
          matchedProgram = matchedProgram ?? movedProgram;
          Logger.Debug($"SlimTvService: Detected moved program {movedProgram.Title} ({movedProgram.StartTime}-{movedProgram.EndTime}) for schedule {schedule.Name} ({schedule.StartTime}-{schedule.EndTime})");
        }
      }

      return matchedProgram;
    }

    protected async Task<IList<IProgram>> GetAllCachedScheduleProgramsAsync(ISchedule schedule, CollectionCache cache, double days)
    {
      if (!await UpdateChannelCacheAsync(schedule.ChannelId, cache))
        return null;

      await UpdateChannelProgramsCacheAsync(cache.Channels[schedule.ChannelId], cache, days);
      return cache.Programs[schedule.ChannelId]
        .Where(p => p.EndTime >= _nowTime && IsScheduleProgramValid(schedule, p))
        .ToList();
    }

    protected async Task<bool> UpdateChannelCacheAsync(int channelId, CollectionCache cache)
    {
      if (!cache.Channels.ContainsKey(channelId))
      {
        var channelResult = await GetChannelAsync(channelId);
        if (!channelResult.Success)
          return false;

        cache.Channels[channelId] = channelResult.Result;
      };
      return true;
    }

    protected async Task InitializeCardsCacheAsync(CollectionCache cache)
    {
      cache.Cards.Clear();

      var cardResult = await GetProviderCardsAsync();
      if (cardResult.Success)
      {
        foreach(var card in cardResult.Result)
          cache.Cards.Add(card.CardId, card);
      }
    }

    protected async Task UpdateChannelProgramsCacheAsync(IChannel channel, CollectionCache cache, double days)
    {
      if (cache.Programs.ContainsKey(channel.ChannelId))
        return;

      var start = _nowTime;
      var progsResult = await GetProgramsAsync(channel, start, start.AddDays(days));
      if (progsResult.Success)
        cache.Programs[channel.ChannelId] = progsResult.Result;
      else
        cache.Programs[channel.ChannelId] = new List<IProgram>();
    }

    protected Task InitializeSeriesCacheAsync(CollectionCache cache)
    {
      cache.KnownSeries.Clear();

      var items = GetSeriesFromMediaLibrary();
      if (items?.Count > 0)
      {
        foreach (var item in items)
        {
          if (MediaItemAspect.TryGetAttribute(item.Aspects, SeriesAspect.ATTR_SERIES_NAME, out string name))
          {
            SeriesInfo info = new SeriesInfo { Id = item.MediaItemId, Name = name };
            if (MediaItemAspect.TryGetAttribute(item.Aspects, SeriesAspect.ATTR_ORIG_SERIES_NAME, out string alternateName))
              info.AlternateName = alternateName;
            cache.KnownSeries.Add(info);
          }
        }
      }
      return Task.CompletedTask;
    }

    protected virtual IList<MediaItem> GetSeriesFromMediaLibrary()
    {
      IMediaLibrary mediaLibrary = ServiceRegistration.Get<IMediaLibrary>(false);
      if (mediaLibrary != null)
      {
        List<Guid> necessaryMias = new List<Guid>();
        necessaryMias.Add(SeriesAspect.ASPECT_ID);
        MediaItemQuery seriesQuery = new MediaItemQuery(necessaryMias, null);
        return mediaLibrary.Search(seriesQuery, false, null, false);
      }
      return new List<MediaItem>();
    }

    protected SeriesInfo GetCachedSeriesInfo(string name, CollectionCache cache)
    {
      return cache.KnownSeries?.FirstOrDefault(i => string.Equals(name, i.Name, StringComparison.InvariantCultureIgnoreCase) ||
                                              string.Equals(name, i.AlternateName, StringComparison.InvariantCultureIgnoreCase));
    }

    protected EpisodeInfo GetCachedEpisodeInfoFromProgram(IProgramSeries programSeries, CollectionCache cache)
    {
      var seriesInfo = GetCachedSeriesInfo(programSeries.Title, cache);
      var seasonNo = ParseSeasonNumber(programSeries.SeasonNumber);
      var episodeNo = ParseEpisodeNumber(programSeries.EpisodeNumber);
      //bool episodeNumberFound = seasonNo.HasValue && episodeNo.HasValue;
      //if (!episodeNumberFound)
      //  return null;

      return new EpisodeInfo
      {
        Series = seriesInfo,
        Name = programSeries.EpisodeTitle,
        SeasonNumber = seasonNo ?? -1,
        EpisodeNumber = episodeNo ?? -1
      };
    }

    protected async Task UpdateSeriesEpisodesCacheAsync(string name, CollectionCache cache)
    {
      SeriesInfo info = GetCachedSeriesInfo(name, cache);
      if (info == null)
      {
        info = new SeriesInfo { Name = name };
        cache.KnownSeries.Add(info);
      }

      if (info.Episodes == null)
      {
        info.Episodes = new List<EpisodeInfo>();

        if (info.Id.HasValue)
        {
          var items = GetSeriesEpisodesFromMediaLibrary(info.Id.Value);
          if (items?.Count > 0)
          {
            foreach (var item in items)
            {
              if (MediaItemAspect.TryGetAttribute(item.Aspects, EpisodeAspect.ATTR_EPISODE_NAME, out string episodeName) &&
                  MediaItemAspect.TryGetAttribute(item.Aspects, EpisodeAspect.ATTR_EPISODE, out IEnumerable<int> episodeNos) &&
                  MediaItemAspect.TryGetAttribute(item.Aspects, EpisodeAspect.ATTR_SEASON, out int seasonNo))
              {
                foreach (var episodeNo in episodeNos)
                  info.Episodes.Add(new EpisodeInfo { Series = info, Name = episodeName, SeasonNumber = seasonNo, EpisodeNumber = episodeNo });
              }
            }
          }
        }

        var recordingResult = await GetRecordingsAsync(name);
        if (recordingResult.Success)
        {
          foreach (var recording in recordingResult.Result.OfType<IRecordingSeries>())
          {
            var seasonNo = ParseSeasonNumber(recording.SeasonNumber);
            var episodeNo = ParseEpisodeNumber(recording.EpisodeNumber);
            bool episodeNumberFound = seasonNo.HasValue && episodeNo.HasValue;
            if (!string.IsNullOrWhiteSpace(recording.EpisodeTitle) && episodeNumberFound)
            {
              info.Episodes.Add(new EpisodeInfo { Series = info, Name = recording.EpisodeTitle, SeasonNumber = seasonNo.Value, EpisodeNumber = episodeNo.Value });
            }
          }
        }
      }
    }

    protected virtual IList<MediaItem> GetSeriesEpisodesFromMediaLibrary(Guid seriesId)
    {
      IMediaLibrary mediaLibrary = ServiceRegistration.Get<IMediaLibrary>(false);
      if (mediaLibrary != null)
      {
        IFilter filter = new RelationshipFilter(EpisodeAspect.ROLE_EPISODE, SeriesAspect.ROLE_SERIES, seriesId);
        List<Guid> necessaryMias = new List<Guid>();
        necessaryMias.Add(EpisodeAspect.ASPECT_ID);
        MediaItemQuery episodeQuery = new MediaItemQuery(necessaryMias, filter);
        return mediaLibrary.Search(episodeQuery, false, null, false);
      }
      return new List<MediaItem>();
    }

    protected Schedule CreateScheduleClone(ISchedule schedule, DateTime? rebaseTime = null)
    {
      var s = new Schedule()
      {
        ScheduleId = schedule.ScheduleId,
        ParentScheduleId = schedule.ParentScheduleId,
        RecordingType = schedule.RecordingType,
        ChannelId = schedule.ChannelId,
        Name = schedule.Name,
        StartTime = schedule.StartTime,
        EndTime = schedule.EndTime,
        KeepDate = schedule.KeepDate,
        KeepMethod = schedule.KeepMethod,
        PreRecordInterval = schedule.PreRecordInterval,
        PostRecordInterval = schedule.PostRecordInterval,
        Priority = schedule.Priority,
      };
      if (rebaseTime.HasValue)
      {
        if (s.StartTime.Day != s.EndTime.Day)
        {
          // Adjusts end time for schedules that overlap 2 days(eg: 23:00 - 00:30)
          s.StartTime = new DateTime(rebaseTime.Value.Year, rebaseTime.Value.Month, rebaseTime.Value.Day, s.StartTime.Hour, s.StartTime.Minute, s.StartTime.Second);
          s.EndTime = new DateTime(rebaseTime.Value.Year, rebaseTime.Value.Month, rebaseTime.Value.Day, s.EndTime.Hour, s.EndTime.Minute, s.EndTime.Second);
          s.EndTime = s.EndTime.AddDays(1);
        }
        else
        {
          s.StartTime = new DateTime(rebaseTime.Value.Year, rebaseTime.Value.Month, rebaseTime.Value.Day, s.StartTime.Hour, s.StartTime.Minute, s.StartTime.Second);
          s.EndTime = new DateTime(rebaseTime.Value.Year, rebaseTime.Value.Month, rebaseTime.Value.Day, s.EndTime.Hour, s.EndTime.Minute, s.EndTime.Second);
        }
      }

      return s;
    }

    /// <summary>
    /// Checks if the decryptLimit for a card, regarding to a list of assigned schedules has been reached or not
    /// </summary>
    /// <param name="card">Card we wanna use</param>
    /// <param name="assignments">List of schedules assigned to cards</param>
    /// <returns></returns>
    protected bool IsCardDecryptLimitReached(ICard card, ISchedule schedule, IProgram program, IList<CardAssignment> assignments, IList<CardAssignment> assigmentsToIgnore = null)
    {
      int decrypts = 0;
      foreach (var assignment in assignments)
      {
        if (assigmentsToIgnore?.Contains(assignment) ?? false)
          continue;

        if (IsScheduleOverlap(schedule, program, assignment.Schedule, assignment.Program) && assignment.Tuning.IsEncrypted)
          decrypts++;

        if (card.DecryptLimit < decrypts)
          return false;
      }

      return true;
    }

    protected bool IsSameTransmitter(ITuningDetail tuningDetail, ITuningDetail otherTuningDetail)
    {
      if (tuningDetail != null && otherTuningDetail != null)
      {
        if (tuningDetail.ChannelType == otherTuningDetail.ChannelType)
        {
          if (tuningDetail.ChannelType == ChannelType.Analog)
          {
            if (tuningDetail.Frequency == otherTuningDetail.Frequency &&
                tuningDetail.CountryId == otherTuningDetail.CountryId &&
                tuningDetail.TuningSource == otherTuningDetail.TuningSource)
            {
              return true;
            }
          }
          else if (tuningDetail.ChannelType == ChannelType.Atsc)
          {
            if (tuningDetail.Frequency == otherTuningDetail.Frequency &&
                tuningDetail.Modulation == otherTuningDetail.Modulation)
            {
              return true;
            }
          }
          else if (tuningDetail.ChannelType == ChannelType.DvbC)
          {
            if (tuningDetail.Frequency == otherTuningDetail.Frequency &&
                tuningDetail.Modulation == otherTuningDetail.Modulation &&
                tuningDetail.Symbolrate == otherTuningDetail.Symbolrate)
            {
              return true;
            }
          }
          else if (tuningDetail.ChannelType == ChannelType.DvbS)
          {
            if (tuningDetail.Frequency == otherTuningDetail.Frequency &&
                tuningDetail.Modulation == otherTuningDetail.Modulation &&
                tuningDetail.Symbolrate == otherTuningDetail.Symbolrate &&
                tuningDetail.Polarisation == otherTuningDetail.Polarisation &&
                tuningDetail.InnerFecRate == otherTuningDetail.InnerFecRate &&
                tuningDetail.RollOff == otherTuningDetail.RollOff)
            {
              return true;
            }
          }
          else if (tuningDetail.ChannelType == ChannelType.DvbT)
          {
            if (tuningDetail.Frequency == otherTuningDetail.Frequency &&
                tuningDetail.Bandwidth == otherTuningDetail.Bandwidth)
            {
              return true;
            }
          }
          else if (tuningDetail.ChannelType == ChannelType.DvbIP)
          {
            if (string.Equals(tuningDetail.Url, otherTuningDetail.Url, StringComparison.InvariantCultureIgnoreCase))
            {
              return true;
            }
          }
        }
      }
      return false;
    }

    /// <summary>
    /// Checks if 2 scheduled recordings are overlapping
    /// </summary>
    /// <returns>true if scheduled recordings are overlapping</returns>
    protected bool IsScheduleOverlap(ISchedule schedule1, IProgram program1, ISchedule schedule2, IProgram program2)
    {
      DateTime start1 = schedule1.StartTime.Add(-schedule1.PreRecordInterval);
      DateTime start2 = schedule2.StartTime.Add(-schedule2.PreRecordInterval);
      DateTime end1 = schedule1.EndTime.Add(schedule1.PostRecordInterval);
      DateTime end2 = schedule2.EndTime.Add(schedule2.PostRecordInterval);
      if (_serverSettings.DetectMovedPrograms)
      {
        if (!IsScheduleCoveringProgram(schedule1, program1) && !IsManualTitle(schedule1.Name))
        {
          start1 = (program1?.StartTime ?? schedule1.StartTime).Add(-schedule1.PreRecordInterval);
          end1 = (program1?.EndTime ?? schedule1.EndTime).Add(schedule1.PostRecordInterval);
        }

        if (!IsScheduleCoveringProgram(schedule2, program2) && !IsManualTitle(schedule2.Name))
        {
          start2 = (program2?.StartTime ?? schedule2.StartTime).Add(-schedule2.PreRecordInterval);
          end2 = (program2?.EndTime ?? schedule2.EndTime).Add(schedule2.PostRecordInterval);
        }
      }

      // sch_1        s------------------------e
      // sch_2             s-----------------------------
      // sch_2    s--------------------------------e
      // sch_2  ------------------e
      if ((start2 >= start1 && start2 < end1) ||
          (start2 <= start1 && end2 >= end1) ||
          (end2 > start1 && end2 <= end1))
      {
        return true;
      }
      return false;
    }

    protected bool IsManualTitle(string title)
    {
      if (title == null)
        return false;

      return title.Equals(Consts.MANUAL_RECORDING_TITLE, StringComparison.InvariantCultureIgnoreCase) ||
             title.StartsWith(Consts.MANUAL_RECORDING_TITLE_PREFIX, StringComparison.InvariantCultureIgnoreCase);
    }

    protected bool IsScheduleCoveringProgram(ISchedule schedule, IProgram program)
    {
      if (program == null)
        return true;

      DateTime start = schedule.StartTime.Add(-schedule.PreRecordInterval);
      DateTime end = schedule.EndTime.Add(schedule.PostRecordInterval);
      return start <= program.StartTime && end >= program.EndTime;
    }

    protected bool IsValidRuleProgram(IScheduleRule schedule, IProgram program, CollectionCache cache)
    {
      if (schedule == null)
        return true;

      if (schedule.StartOnOrAfterDay.HasValue || schedule.StartOnOrBeforeDay.HasValue)
      {
        var validDays = GetValidDayOfWeeks(schedule.StartOnOrAfterDay, schedule.StartOnOrBeforeDay);
        if (!validDays.Contains(program.StartTime.DayOfWeek))
          return false;
      }

      if (schedule.StartFromTime.HasValue || schedule.StartToTime.HasValue)
      {
        var start = schedule.StartFromTime?.TimeOfDay ?? TimeSpan.FromDays(0);
        var end = schedule.StartToTime?.TimeOfDay ?? TimeSpan.FromDays(24);
        if (start > program.StartTime.TimeOfDay || end < program.StartTime.TimeOfDay)
          return false;
      }

      //All targets must match
      foreach (var target in schedule.Targets)
      {
        if (string.IsNullOrWhiteSpace(target.SearchText))
          return false;

        if (target.SearchTarget == RuleSearchTarget.Titel || target.SearchTarget == RuleSearchTarget.Description || target.SearchTarget == RuleSearchTarget.Genre)
        {
          string data = "";
          if (target.SearchTarget == RuleSearchTarget.Titel)
            data = program.Title;
          else if (target.SearchTarget == RuleSearchTarget.Description)
            data = program.Description;
          else if (target.SearchTarget == RuleSearchTarget.Genre)
            data = program.Genre;

          if (target.SearchMatch == RuleSearchMatch.Exact && !string.Equals(data, target.SearchText, StringComparison.InvariantCultureIgnoreCase))
            return false;
          if (target.SearchMatch == RuleSearchMatch.Include && data.IndexOf(target.SearchText, StringComparison.InvariantCultureIgnoreCase) < 0)
            return false;
          if (target.SearchMatch == RuleSearchMatch.Exclude && data.IndexOf(target.SearchText, StringComparison.InvariantCultureIgnoreCase) >= 0)
            return false;
          if (target.SearchMatch == RuleSearchMatch.Regex && !Regex.IsMatch(data, target.SearchText, RegexOptions.IgnoreCase))
            return false;
        }
        else if (target.SearchTarget == RuleSearchTarget.StarRating)
        {
          if (target.SearchMatch == RuleSearchMatch.Exact && int.TryParse(target.SearchText, out int exactRating) && program.StarRating != exactRating)
            return false;
          if (target.SearchMatch == RuleSearchMatch.Include && int.TryParse(target.SearchText, out int minRating) && program.StarRating < minRating)
            return false;
          if (target.SearchMatch == RuleSearchMatch.Exclude && int.TryParse(target.SearchText, out int maxRating) && program.StarRating > maxRating)
            return false;
          if (target.SearchMatch == RuleSearchMatch.Regex && !Regex.IsMatch(program.StarRating.ToString(), target.SearchText))
            return false;
        }

        if (schedule.IsSeries)
        {
          if (!(program is IProgramSeries series))
            return false;

          bool seriesMatch = string.IsNullOrWhiteSpace(schedule.SeasonNumber) && string.IsNullOrWhiteSpace(schedule.EpisodeNumber) && string.IsNullOrWhiteSpace(schedule.EpisodeTitle);

          //Episode number matching
          int? seasonNo = ParseSeasonNumber(schedule.SeasonNumber);
          int? episodeNo = ParseEpisodeNumber(schedule.EpisodeNumber);
          if (schedule.EpisodeInfoFallbackType == RuleEpisodeInfoFallback.TitleContainsSeasonEpisodeRegEx || schedule.EpisodeInfoFallbackType == RuleEpisodeInfoFallback.DescriptionContainsSeasonEpisodeRegex || 
              schedule.EpisodeInfoFallbackType == RuleEpisodeInfoFallback.EpisodeTitleContainsSeasonEpisodeRegEx)
          {
            string data = "";
            if (schedule.EpisodeInfoFallbackType == RuleEpisodeInfoFallback.TitleContainsSeasonEpisodeRegEx)
              data = series.Title;
            else if (schedule.EpisodeInfoFallbackType == RuleEpisodeInfoFallback.DescriptionContainsSeasonEpisodeRegex)
              data = series.Description;
            else if (schedule.EpisodeInfoFallbackType == RuleEpisodeInfoFallback.EpisodeTitleContainsSeasonEpisodeRegEx)
              data = series.EpisodeTitle;

            if (!string.IsNullOrWhiteSpace(schedule.EpisodeInfoFallback) && !string.IsNullOrWhiteSpace(data))
            {
              var regex = new Regex(schedule.EpisodeInfoFallback, RegexOptions.IgnoreCase);
              var match = regex.Match(data);
              if (match.Success)
              {
                foreach (string group in regex.GetGroupNames())
                {
                  // Save so we can use it later when checking assignments
                  if (string.Equals(group, "SeasonNo", StringComparison.InvariantCultureIgnoreCase) && int.TryParse(match.Groups[group].Value, out int prgSeasonNo))
                    series.SeasonNumber = prgSeasonNo.ToString();
                  if (string.Equals(group, "EpisodeNo", StringComparison.InvariantCultureIgnoreCase) && int.TryParse(match.Groups[group].Value, out int prgEpisodeNo))
                    series.EpisodeNumber = prgEpisodeNo.ToString();
                }
              }
            }
          }
          if (seasonNo.HasValue && episodeNo.HasValue)
          {
            var info = GetCachedEpisodeInfoFromProgram(series, cache);
            if (info != null)
            {
              if (info.SeasonNumber == seasonNo && info.EpisodeNumber == episodeNo)
                seriesMatch = true;
            }
          }
          else if (seasonNo.HasValue)
          {
            var info = GetCachedEpisodeInfoFromProgram(series, cache);
            if (info != null)
            {
              if (info.SeasonNumber == seasonNo)
                seriesMatch = true;
            }
          }
          else if (episodeNo.HasValue)
          {
            var info = GetCachedEpisodeInfoFromProgram(series, cache);
            if (info != null)
            {
              if (info.EpisodeNumber == episodeNo)
                seriesMatch = true;
            }
          }

          //Episode title matching
          if (schedule.EpisodeInfoFallbackType == RuleEpisodeInfoFallback.TitleIsEpisodeName)
          {
            // Save so we can use it later when checking assignments
            series.EpisodeTitle = series.Title?.Trim();
          }
          else if (schedule.EpisodeInfoFallbackType == RuleEpisodeInfoFallback.DescriptionIsEpisodeName)
          {
            // Save so we can use it later when checking assignments
            series.EpisodeTitle = series.Description?.Trim();
          }
          else if (schedule.EpisodeInfoFallbackType == RuleEpisodeInfoFallback.TitleContainsEpisodeTitleRegEx || schedule.EpisodeInfoFallbackType == RuleEpisodeInfoFallback.DescriptionContainsEpisodeTitleRegex || 
                   schedule.EpisodeInfoFallbackType == RuleEpisodeInfoFallback.EpisodeTitleContainsEpisodeTitleRegEx)
          {
            string data = "";
            if (schedule.EpisodeInfoFallbackType == RuleEpisodeInfoFallback.TitleContainsEpisodeTitleRegEx)
              data = series.Title;
            else if (schedule.EpisodeInfoFallbackType == RuleEpisodeInfoFallback.DescriptionContainsEpisodeTitleRegex)
              data = series.Description;
            else if (schedule.EpisodeInfoFallbackType == RuleEpisodeInfoFallback.EpisodeTitleContainsEpisodeTitleRegEx)
              data = series.EpisodeTitle;

            if (!string.IsNullOrWhiteSpace(schedule.EpisodeInfoFallback) && !string.IsNullOrWhiteSpace(data))
            {
              var regex = new Regex(schedule.EpisodeInfoFallback, RegexOptions.IgnoreCase);
              var match = regex.Match(data);
              if (match.Success)
              {
                if (match.Groups.Count >= 1)
                {
                  // Save so we can use it later when checking assignments
                  series.EpisodeTitle = match.Groups[regex.GroupNameFromNumber(0)].Value;
                }
              }
            }
          }
          if (!string.IsNullOrWhiteSpace(schedule.EpisodeTitle))
          {
            if (schedule.EpisodeTitle.Equals(series.EpisodeTitle, StringComparison.InvariantCultureIgnoreCase))
              seriesMatch = true;
          }

          if (!seriesMatch)
            return false;
        }
      }
      
      return true;
    }

    protected int? ParseSeasonNumber(string seriesNum)
    {
      if (int.TryParse(seriesNum, out int seasonNo) && seasonNo >= 0)
        return seasonNo;

      return null;
    }

    protected int? ParseEpisodeNumber(string episodeNum)
    {
      if (int.TryParse(episodeNum, out int episodeNo) && episodeNo > 0)
        return episodeNo;

      if (episodeNum?.Contains("/") == true && int.TryParse(episodeNum.Substring(0, episodeNum.IndexOf("/")), out int partEpisodeNo) && partEpisodeNo > 0)
        return partEpisodeNo;

      return null;
    }

    protected bool IsScheduleProgramValid(ISchedule schedule, IProgram program)
    {
      switch (schedule.RecordingType)
      {
        case ScheduleRecordingType.Once:
        case ScheduleRecordingType.EveryTimeOnThisChannel:
        case ScheduleRecordingType.WeeklyEveryTimeOnThisChannel:
          return string.Equals(program?.Title, schedule?.Name);
        case ScheduleRecordingType.Daily:
        case ScheduleRecordingType.Weekly:
        case ScheduleRecordingType.Weekends:
        case ScheduleRecordingType.WorkingDays:
          return true;
      }

      return false;
    }

    protected bool IsWeekend(DayOfWeek dayOfWeek)
    {
      return dayOfWeek == DayOfWeek.Saturday || dayOfWeek == DayOfWeek.Sunday;
    }

    protected IList<DayOfWeek> GetValidDayOfWeeks(DayOfWeek? firstDayOfWeek, DayOfWeek? lastDayOfWeek)
    {
      var first = firstDayOfWeek ?? DayOfWeek.Monday;
      var last = lastDayOfWeek ?? DayOfWeek.Sunday;

      List<DayOfWeek> days = new List<DayOfWeek>();
      days.Add(first);
      while (first != last)
      {
        first = (DayOfWeek)(((int)first + 1) % 7);
        days.Add(first);
      }
      return days;
    }

    protected string GetRecordingFolderFromTags(string defaultRecordingPath, string format, Dictionary<string, string> tags)
    {
      foreach (var tag in tags)
      {
        format = ReplaceTag(format, tag.Key, tag.Value, "unknown");
        if (!format.Contains("%"))
          break;
      }

      string subDirectory = Path.GetDirectoryName(format);
      if (!string.IsNullOrWhiteSpace(subDirectory))
      {
        subDirectory = RemoveTrailingSlash(subDirectory);

        //Replace any trailing dots in path name
        subDirectory = new Regex(@"\.*$").Replace(subDirectory, "");
        //Replace any trailing spaces in path name
        subDirectory = new Regex(@"\s+\\\s*|\\\s+").Replace(subDirectory, "\\");

        defaultRecordingPath = Path.Combine(defaultRecordingPath, subDirectory.Trim());
      }

      return defaultRecordingPath;
    }

    protected string RemoveTrailingSlash(string strLine)
    {
      if (strLine == null)
        return String.Empty;
      if (strLine.Length == 0)
        return String.Empty;
      string strPath = strLine;
      while (strPath.Length > 0)
      {
        if (strPath[strPath.Length - 1] == '\\' || strPath[strPath.Length - 1] == '/')
          strPath = strPath.Substring(0, strPath.Length - 1);
        else
          break;
      }
      return strPath;
    }

    protected string ReplaceTag(string line, string tag, string value, string empty)
    {
      if (line == null)
        return String.Empty;
      if (line.Length == 0)
        return String.Empty;
      if (tag == null)
        return line;
      if (tag.Length == 0)
        return line;

      Regex r = new Regex(String.Format(@"\[[^%]*{0}[^\]]*[\]]", tag));
      if (value == empty)
      {
        Match match = r.Match(line);
        if (match.Length > 0)
        {
          line = line.Remove(match.Index, match.Length);
        }
      }
      else
      {
        Match match = r.Match(line);
        if (match.Length > 0)
        {
          line = line.Remove(match.Index, match.Length);
          string m = match.Value.Substring(1, match.Value.Length - 2);
          line = line.Insert(match.Index, m);
        }
      }
      return line.Replace(tag, value);
    }

    #endregion

    protected static ILogger Logger => ServiceRegistration.Get<ILogger>();
  }
}
