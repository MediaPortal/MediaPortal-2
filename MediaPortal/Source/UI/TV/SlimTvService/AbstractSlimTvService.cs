#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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
using MediaPortal.Utilities.FileSystem;
using IChannel = MediaPortal.Plugins.SlimTv.Interfaces.Items.IChannel;
using ILogger = MediaPortal.Common.Logging.ILogger;
using IPathManager = MediaPortal.Common.PathManager.IPathManager;
using ScheduleRecordingType = MediaPortal.Plugins.SlimTv.Interfaces.ScheduleRecordingType;
using MediaPortal.Common.Runtime;
using MediaPortal.Common.Messaging;

namespace MediaPortal.Plugins.SlimTv.Service
{
  public abstract class AbstractSlimTvService : ITvProvider, ITimeshiftControlEx, IProgramInfo, IChannelAndGroupInfo, IScheduleControl, IMessageReceiver
  {
    public static readonly MediaCategory Series = new MediaCategory("Series", null);
    public static readonly MediaCategory Movie = new MediaCategory("Movie", null);

    protected const int MAX_WAIT_MS = 10000;
    public const string LOCAL_USERNAME = "Local";
    public const string TVDB_NAME = "MP2TVE";
    protected DbProviderFactory _dbProviderFactory;
    protected string _cloneConnection;
    protected string _providerName;
    protected string _serviceName;
    private bool _abortInit = false;

    public string Name
    {
      get { return _providerName; }
    }

    public bool Init()
    {
      ServiceRegistration.Get<IMessageBroker>().RegisterMessageReceiver(SystemMessaging.CHANNEL, this);
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
    }

    #region Database and program data initialization

    private void InitAsync()
    {
      ServiceRegistration.Get<ILogger>().Info("SlimTvService: Initialising");

      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>(false);
      if (database == null)
      {
        ServiceRegistration.Get<ILogger>().Error("SlimTvService: Database not available.");
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

      // Run the actual TV core thread(s)
      InitTvCore();
      if (_abortInit)
      {
        DeInit();
        return;
      }

      // Prepare the MP2 integration
      PrepareMediaSources();

      ServiceRegistration.Get<ILogger>().Info("SlimTvService: Initialised");
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
      return !string.IsNullOrEmpty(timeshiftFile);
    }

    public abstract bool StopTimeshift(string userName, int slotIndex);

    public abstract MediaItem CreateMediaItem(int slotIndex, string streamUrl, IChannel channel);

    protected virtual MediaItem CreateMediaItem(int slotIndex, string streamUrl, IChannel channel, bool isTv, IChannel fullChannel)
    {
      LiveTvMediaItem tvStream = isTv
        ? SlimTvMediaItemBuilder.CreateMediaItem(slotIndex, streamUrl, fullChannel)
        : SlimTvMediaItemBuilder.CreateRadioMediaItem(slotIndex, streamUrl, fullChannel);

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

    public abstract bool GetNowNextProgram(IChannel channel, out IProgram programNow, out IProgram programNext);

    public virtual bool GetNowAndNextForChannelGroup(IChannelGroup channelGroup, out IDictionary<int, IProgram[]> nowNextPrograms)
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

    public abstract bool GetPrograms(IChannel channel, DateTime from, DateTime to, out IList<IProgram> programs);

    public abstract bool GetPrograms(string title, DateTime from, DateTime to, out IList<IProgram> programs);

    public abstract bool GetProgramsGroup(IChannelGroup channelGroup, DateTime from, DateTime to, out IList<IProgram> programs);

    public abstract bool GetProgramsForSchedule(ISchedule schedule, out IList<IProgram> programs);

    public virtual bool GetScheduledPrograms(IChannel channel, out IList<IProgram> programs)
    {
      throw new NotImplementedException();
    }

    public abstract bool GetChannel(IProgram program, out IChannel channel);

    public abstract bool GetProgram(int programId, out IProgram program);

    public abstract bool GetChannelGroups(out IList<IChannelGroup> groups);

    public abstract bool GetChannel(int channelId, out IChannel channel);

    public abstract bool GetChannels(IChannelGroup group, out IList<IChannel> channels);

    // This property applies only to client side management and is not used in server!
    public int SelectedChannelId { get; set; }

    // This property applies only to client side management and is not used in server!
    public int SelectedChannelGroupId { get; set; }

    public abstract bool GetSchedules(out IList<ISchedule> schedules);

    public abstract bool CreateSchedule(IProgram program, ScheduleRecordingType recordingType, out ISchedule schedule);

    public abstract bool CreateScheduleByTime(IChannel channel, DateTime from, DateTime to, out ISchedule schedule);

    public abstract bool RemoveScheduleForProgram(IProgram program, ScheduleRecordingType recordingType);

    public abstract bool RemoveSchedule(ISchedule schedule);

    public abstract bool GetRecordingStatus(IProgram program, out RecordingStatus recordingStatus);

    public abstract bool GetRecordingFileOrStream(IProgram program, out string fileOrStream);

    protected abstract string SwitchTVServerToChannel(string userName, int channelId);

    protected static string GetUserName(string clientName, int slotIndex)
    {
      return string.Format("{0}-{1}", clientName, slotIndex);
    }

    #endregion
  }
}
