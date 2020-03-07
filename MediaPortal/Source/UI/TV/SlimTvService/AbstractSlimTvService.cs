#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using MediaPortal.Common.General;
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
using MediaPortal.Common.SystemCommunication;
using MediaPortal.Common.Services.Settings;
using MediaPortal.Plugins.SlimTv.Interfaces.Settings;
using System.Collections.Concurrent;
using MediaPortal.Common.Services.GenreConverter;

namespace MediaPortal.Plugins.SlimTv.Service
{
  public abstract class AbstractSlimTvService : ITvProvider, ITimeshiftControlEx, IProgramInfoAsync, IChannelAndGroupInfoAsync, IScheduleControlAsync, IMessageReceiver
  {
    public static readonly MediaCategory Series = new MediaCategory("Series", null);
    public static readonly MediaCategory Movie = new MediaCategory("Movie", null);

    protected const int MAX_WAIT_MS = 10000;
    protected const int MAX_INIT_MS = 20000;
    public const string LOCAL_USERNAME = "Local";
    public const string TVDB_NAME = "MP2TVE_4";
    protected DbProviderFactory _dbProviderFactory;
    protected string _cloneConnection;
    protected string _providerName;
    protected string _serviceName;
    private bool _abortInit = false;
    // Stores a list of connected MP2-Clients. If one disconnects, we can cleanup resources like stopping timeshifting for this client
    protected List<string> _connectedClients = new List<string>();
    protected SettingsChangeWatcher<SlimTvGenreColorSettings> _settingWatcher;
    protected SlimTvGenreColorSettings _epgColorSettings = null;
    protected readonly ConcurrentDictionary<EpgGenre, ICollection<string>> _tvGenres = new ConcurrentDictionary<EpgGenre, ICollection<string>>();
    protected bool _tvGenresInited = false;
    protected TaskCompletionSource<bool> _initComplete = new TaskCompletionSource<bool>();

    private void SettingsChanged(object sender, EventArgs e)
    {
      _epgColorSettings = _settingWatcher.Settings;
    }

    public string Name
    {
      get { return _providerName; }
    }

    public bool Init()
    {
      ServiceRegistration.Get<IMessageBroker>().RegisterMessageReceiver(SystemMessaging.CHANNEL, this);
      ServiceRegistration.Get<IMessageBroker>().RegisterMessageReceiver(ClientManagerMessaging.CHANNEL, this);

      _settingWatcher = new SettingsChangeWatcher<SlimTvGenreColorSettings>();
      _settingWatcher.SettingsChanged += SettingsChanged;
      _settingWatcher.Refresh();
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
          client = LOCAL_USERNAME;

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
      List<string> disconnectedClients;
      UpdateClientList(out disconnectedClients);
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
      ServiceRegistration.Get<ILogger>().Info("SlimTvService: Initialising");
      Task.Delay(MAX_INIT_MS).ContinueWith((t) =>
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

      ServiceRegistration.Get<ILogger>().Info("SlimTvService: Initialised");
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
    /// Executes custom intialization of DB connection.
    /// </summary>
    /// <param name="transaction"></param>
    protected abstract void PrepareConnection(ITransaction transaction);

    /// <summary>
    /// Prepares the required data folders for first run. The required tuningdetails and other files are extracted to [TVCORE] path.
    /// </summary>
    protected virtual void PrepareProgramData()
    {
      if (!NeedsExtract())
        return;

      ServiceRegistration.Get<ILogger>().Info("SlimTvService: Tuningdetails folder does not exist yet, extracting default items.");
      try
      {
        // Morpheus_xx, 2014-09-01: As soon as our extension installer is able to place files in different target folders, this code can be removed.
        var mp2DataPath = GetTvCorePath();
        ZipFile.ExtractToDirectory(FileUtils.BuildAssemblyRelativePath("ProgramData\\ProgramData.zip"), mp2DataPath);
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("SlimTvService: Failed to extract Tuningdetails!", ex);
      }
    }

    /// <summary>
    /// Initializes genre mapping defined in the server settings if any. Needs to be overridden by plug-ins for which the server setup 
    /// supports genre mapping.
    /// </summary>
    protected virtual void InitGenreMap()
    {
      if (_tvGenresInited)
        return;

      _tvGenresInited = true;
    }

    /// <summary>
    /// Returns a program with assigned EPG genre data if possible.
    /// </summary>
#if TVE3
    protected virtual IProgram GetProgram(TvDatabase.Program tvProgram, bool includeRecordingStatus = false)
#else
    protected virtual IProgram GetProgram(Mediaportal.TV.Server.TVDatabase.Entities.Program tvProgram, bool includeRecordingStatus = false)
#endif
    {
      InitGenreMap();

      //Convert to IProgram
      IProgram prog = tvProgram.ToProgram(includeRecordingStatus);
      if (prog == null)
        return null;

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
        (!string.IsNullOrWhiteSpace(tvProgram.SeriesNum) || !string.IsNullOrWhiteSpace(tvProgram.EpisodeNum) || !string.IsNullOrWhiteSpace(tvProgram.EpisodePart)))
      {
        prog.EpgGenreId = (int)EpgGenre.Series;
        prog.EpgGenreColor = _epgColorSettings.SeriesGenreColor;
      }
      return prog;
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
            ServiceRegistration.Get<ILogger>().Info("SlimTvService: Sucessfully copied database template file {0}", file);
          }
        }
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("SlimTvService: Failed to copy database template!", ex);
      }
    }

    public abstract bool DeInit();

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
        ServiceRegistration.Get<ILogger>().Warn("SlimTvService: Received notifaction of new recording but could not find a media source. Have you added recordings folder as media source? File: {0}", fileName);
        return;
      }

      Share usedShare = possibleShares.OrderByDescending(s => s.BaseResourcePath.LastPathSegment.Path.Length).First();
      IImporterWorker importerWorker = ServiceRegistration.Get<IImporterWorker>();
      importerWorker.ScheduleRefresh(usedShare.BaseResourcePath, usedShare.MediaCategories, true);
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

    #endregion

    #region ITvProvider implementation

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
      string timeshiftFile = SwitchTVServerToChannel(GetUserName(userName, slotIndex), channel.ChannelId);
      var timeshiftMediaItem = await CreateMediaItem(slotIndex, timeshiftFile, channel);
      var result = !string.IsNullOrEmpty(timeshiftFile);
      return new AsyncResult<MediaItem>(result, timeshiftMediaItem);
    }

    public abstract Task<bool> StopTimeshiftAsync(string userName, int slotIndex);

    public abstract Task<MediaItem> CreateMediaItem(int slotIndex, string streamUrl, IChannel channel);

    protected virtual async Task<MediaItem> CreateMediaItem(int slotIndex, string streamUrl, IChannel channel, bool isTv, IChannel fullChannel)
    {
      LiveTvMediaItem tvStream = isTv
        ? SlimTvMediaItemBuilder.CreateMediaItem(slotIndex, streamUrl, fullChannel)
        : SlimTvMediaItemBuilder.CreateRadioMediaItem(slotIndex, streamUrl, fullChannel);

      if (tvStream != null)
      {
        // Add program infos to the LiveTvMediaItem
        var result = await GetNowNextProgramAsync(channel);
        if (result.Success)
        {
          tvStream.AdditionalProperties[LiveTvMediaItem.CURRENT_PROGRAM] = result.Result[0];
          tvStream.AdditionalProperties[LiveTvMediaItem.NEXT_PROGRAM] = result.Result[1];
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

    public abstract Task<AsyncResult<IProgram[]>> GetNowNextProgramAsync(IChannel channel);

    public virtual async Task<AsyncResult<IDictionary<int, IProgram[]>>> GetNowAndNextForChannelGroupAsync(IChannelGroup channelGroup)
    {
      var nowNextPrograms = new Dictionary<int, IProgram[]>();

      var result = await GetChannelsAsync(channelGroup);
      if (!result.Success)
        return new AsyncResult<IDictionary<int, IProgram[]>>(false, null);

      IList<IChannel> channels = result.Result;
      foreach (IChannel channel in channels)
      {
        var progrResult = await GetNowNextProgramAsync(channel);
        if (progrResult.Success)
          nowNextPrograms[channel.ChannelId] = progrResult.Result;
      }
      return new AsyncResult<IDictionary<int, IProgram[]>>(true, nowNextPrograms);
    }

    public abstract Task<AsyncResult<IList<IProgram>>> GetProgramsAsync(IChannel channel, DateTime from, DateTime to);

    public abstract Task<AsyncResult<IList<IProgram>>> GetProgramsAsync(string title, DateTime from, DateTime to);

    public abstract Task<AsyncResult<IList<IProgram>>> GetProgramsGroupAsync(IChannelGroup channelGroup, DateTime from, DateTime to);

    public abstract Task<AsyncResult<IList<IProgram>>> GetProgramsForScheduleAsync(ISchedule schedule);

    public abstract Task<AsyncResult<IChannel>> GetChannelAsync(IProgram program);

    public abstract Task<AsyncResult<IChannel>> GetChannelAsync(int channelId);

    public abstract bool GetProgram(int programId, out IProgram program);

    public abstract Task<AsyncResult<IList<IChannelGroup>>> GetChannelGroupsAsync();

    public abstract Task<AsyncResult<IList<IChannel>>> GetChannelsAsync(IChannelGroup group);

    // This property applies only to client side management and is not used in server!
    public int SelectedChannelId { get; set; }

    // This property applies only to client side management and is not used in server!
    public int SelectedChannelGroupId { get; set; }

    public abstract Task<AsyncResult<IList<ISchedule>>> GetSchedulesAsync();

    public abstract Task<AsyncResult<ISchedule>> IsCurrentlyRecordingAsync(string fileName);

    public abstract Task<AsyncResult<ISchedule>> CreateScheduleAsync(IProgram program, ScheduleRecordingType recordingType);

    public abstract Task<AsyncResult<ISchedule>> CreateScheduleByTimeAsync(IChannel channel, DateTime from, DateTime to, ScheduleRecordingType recordingType);

    public abstract Task<AsyncResult<ISchedule>> CreateScheduleByTimeAsync(IChannel channel, string title, DateTime from, DateTime to, ScheduleRecordingType recordingType);

    public abstract Task<AsyncResult<ISchedule>> CreateScheduleDetailedAsync(IChannel channel, string title, DateTime from, DateTime to, ScheduleRecordingType recordingType, int preRecordInterval, int postRecordInterval, string directory, int priority);

    public abstract Task<bool> EditScheduleAsync(ISchedule schedule, IChannel channel = null, string title = null, DateTime? from = null, DateTime? to = null, ScheduleRecordingType? recordingType = null, int? preRecordInterval = null, int? postRecordInterval = null, string directory = null, int? priority = null);

    public abstract Task<bool> RemoveScheduleForProgramAsync(IProgram program, ScheduleRecordingType recordingType);

    public abstract Task<bool> RemoveScheduleAsync(ISchedule schedule);

	public abstract Task<bool> UnCancelScheduleAsync(IProgram program);

    public abstract Task<AsyncResult<RecordingStatus>> GetRecordingStatusAsync(IProgram program);

    public abstract Task<AsyncResult<string>> GetRecordingFileOrStreamAsync(IProgram program);

    // TODO: Async
    protected abstract string SwitchTVServerToChannel(string userName, int channelId);

    protected static string GetUserName(string clientName, int slotIndex)
    {
      return string.Format("{0}-{1}", clientName, slotIndex);
    }

	public abstract Task<AsyncResult<List<ICard>>> GetCardsAsync();

    public abstract Task<AsyncResult<List<IVirtualCard>>> GetActiveVirtualCardsAsync();

    #endregion
  }
}
