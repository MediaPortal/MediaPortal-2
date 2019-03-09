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
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.Async;
using MediaPortal.Common.General;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.Runtime;
using MediaPortal.Common.Services.Settings;
using MediaPortal.Common.Settings;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.Plugins.SlimTv.Interfaces.LiveTvMediaItem;
using MediaPortal.Plugins.SlimTv.Interfaces.ResourceProvider;
using MediaPortal.Plugins.SlimTv.Providers.Items;
using MediaPortal.Plugins.SlimTv.Providers.Settings;
using MediaPortal.UI.Presentation.UiNotifications;
using MPExtended.Services.Common.Interfaces;
using MPExtended.Services.TVAccessService.Interfaces;
using IChannel = MediaPortal.Plugins.SlimTv.Interfaces.Items.IChannel;

namespace MediaPortal.Plugins.SlimTv.Providers
{
  public class SlimTVMPExtendedProvider : ITvProvider, ITimeshiftControlAsync, IProgramInfoAsync, IChannelAndGroupInfoAsync, IScheduleControlAsync
  {
    #region Internal class

    internal class ServerContext
    {
      public string ServerName;
      public string Username;
      public string Password;
      public ITVAccessService TvServer;
      public bool ConnectionOk;
      public bool IsLocalConnection;
      public DateTime LastCheckTime = DateTime.MinValue;
      public ChannelFactory<ITVAccessService> Factory;

      public static bool IsLocal(string host)
      {
        if (string.IsNullOrEmpty(host))
          return true;

        string lowerHost = host.ToLowerInvariant();
        return lowerHost == "localhost" || lowerHost == LOCAL_SYSTEM.ToLowerInvariant() || host == "127.0.0.1" || host == "::1";
      }

      public void CreateChannel()
      {
        Binding binding;
        EndpointAddress endpointAddress;
        bool useAuth = !string.IsNullOrEmpty(Username) && !string.IsNullOrEmpty(Password);
        IsLocalConnection = IsLocal(ServerName);
        if (IsLocalConnection)
        {
          endpointAddress = new EndpointAddress("net.pipe://localhost/MPExtended/TVAccessService");
          binding = new NetNamedPipeBinding { MaxReceivedMessageSize = 10000000 };
        }
        else
        {
          endpointAddress = new EndpointAddress(string.Format("http://{0}:4322/MPExtended/TVAccessService", ServerName));
          BasicHttpBinding basicBinding = new BasicHttpBinding { MaxReceivedMessageSize = 10000000 };
          if (useAuth)
          {
            basicBinding.Security.Mode = BasicHttpSecurityMode.TransportCredentialOnly;
            basicBinding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Basic;
          }
          basicBinding.ReaderQuotas.MaxStringContentLength = 5 * 1024 * 1024; // 5 MB
          binding = basicBinding;
        }
        binding.OpenTimeout = TimeSpan.FromSeconds(5);
        Factory = new ChannelFactory<ITVAccessService>(binding);
        if (Factory.Credentials != null && useAuth)
        {
          Factory.Credentials.UserName.UserName = Username;
          Factory.Credentials.UserName.Password = Password;
        }
        TvServer = Factory.CreateChannel(endpointAddress);
      }
    }

    #endregion

    #region Constants

    private const string RES_TV_CONNECTION_ERROR_TITLE = "[Settings.Plugins.TV.ConnectionErrorTitle]";
    private const string RES_TV_CONNECTION_ERROR_TEXT = "[Settings.Plugins.TV.ConnectionErrorText]";
    private const int MAX_RECONNECT_ATTEMPTS = 2;
    private const int CONNECTION_CHECK_INTERVAL_SEC = 5;
    private static readonly string LOCAL_SYSTEM = SystemName.LocalHostName;

    #endregion

    #region Fields

    private readonly IChannel[] _channels = new IChannel[2];
    private ServerContext[] _tvServers;
    private int _reconnectCounter = 0;
    private readonly Dictionary<int, IChannel> _channelCache = new Dictionary<int, IChannel>();

    // Handling of changed connection details.
    private readonly SettingsChangeWatcher<MPExtendedProviderSettings> _settings = new SettingsChangeWatcher<MPExtendedProviderSettings>();
    private string _serverNames = null;
    private readonly TimeSpan _checkDuration = TimeSpan.FromSeconds(CONNECTION_CHECK_INTERVAL_SEC);
    private AsynchronousMessageQueue _messageQueue;

    #endregion

    #region ITvProvider Member

    public IChannel GetChannel(int slotIndex)
    {
      return _channels[slotIndex];
    }

    public string Name
    {
      get { return "MPExtended Provider"; }
    }

    #endregion

    #region ITimeshiftControl Member

    public bool Init()
    {
      _settings.SettingsChanged += ReCreateConnections;
      _messageQueue = new AsynchronousMessageQueue(this, new[] { SystemMessaging.CHANNEL });
      _messageQueue.PreviewMessage += OnMessageReceived;

      CreateAllTvServerConnections();
      return true;
    }

    private void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == SystemMessaging.CHANNEL)
      {
        SystemMessaging.MessageType messageType = (SystemMessaging.MessageType)message.MessageType;
        switch (messageType)
        {
          case SystemMessaging.MessageType.SystemStateChanged:
            SystemState newState = (SystemState)message.MessageData[SystemMessaging.NEW_STATE];
            if (newState == SystemState.Suspending)
            {
              ServiceRegistration.Get<ILogger>().Info("SlimTVMPExtendedProvider: System suspending, stopping timeshift.");
              DeInit();
            }
            if (newState == SystemState.Resuming)
            {
              ServiceRegistration.Get<ILogger>().Info("SlimTVMPExtendedProvider: System resuming, init connections.");
              Init();
            }
            break;
        }
      }
    }
    private void ReCreateConnections(object sender, EventArgs e)
    {
      // Settings will be changed for various reasons, we only need to handle changed server name(s).
      if (_serverNames != _settings.Settings.TvServerHost)
      {
        DeInit();
        Init();
      }
    }

    public bool DeInit()
    {
      _messageQueue.Dispose();
      _messageQueue = null;

      _settings.SettingsChanged -= ReCreateConnections;

      if (_tvServers == null)
        return false;

      try
      {
        foreach (ServerContext tvServer in _tvServers)
        {
          if (tvServer.TvServer != null)
          {
            tvServer.TvServer.CancelCurrentTimeShifting(GetTimeshiftUserName(0));
            tvServer.TvServer.CancelCurrentTimeShifting(GetTimeshiftUserName(1));
          }
        }
      }
      catch (Exception) { }

      _tvServers = null;
      return true;
    }

    public String GetTimeshiftUserName(int slotIndex)
    {
      return String.Format("STC_{0}_{1}", LOCAL_SYSTEM, slotIndex);
    }

    public async Task<AsyncResult<MediaItem>> StartTimeshiftAsync(int slotIndex, IChannel channel)
    {
      Channel indexChannel = channel as Channel;
      if (indexChannel == null || !CheckConnection(indexChannel.ServerIndex))
        return new AsyncResult<MediaItem>(false, null);

      try
      {
        ITVAccessService tvServer = TvServer(indexChannel.ServerIndex);
        String streamUrl = _tvServers[indexChannel.ServerIndex].IsLocalConnection ?
          // Prefer local timeshift file over RTSP streaming
          tvServer.SwitchTVServerToChannelAndGetTimeshiftFilename(GetTimeshiftUserName(slotIndex), channel.ChannelId) :
          tvServer.SwitchTVServerToChannelAndGetStreamingUrl(GetTimeshiftUserName(slotIndex), channel.ChannelId);

        if (String.IsNullOrEmpty(streamUrl))
          return new AsyncResult<MediaItem>(false, null);

        _channels[slotIndex] = channel;

        // assign a MediaItem, can be null if streamUrl is the same.
        var timeshiftMediaItem = CreateMediaItem(slotIndex, streamUrl, channel);
        return new AsyncResult<MediaItem>(true, timeshiftMediaItem);
      }
      catch (Exception ex)
      {
        NotifyException(ex, indexChannel.ServerIndex);
        return new AsyncResult<MediaItem>(false, null);
      }
    }

    public async Task<bool> StopTimeshiftAsync(int slotIndex)
    {
      Channel slotChannel = _channels[slotIndex] as Channel;
      if (slotChannel == null)
        return false;

      if (!CheckConnection(slotChannel.ServerIndex))
        return false;

      try
      {
        TvServer(slotChannel.ServerIndex).CancelCurrentTimeShifting(GetTimeshiftUserName(slotIndex));
        _channels[slotIndex] = null;
        return true;
      }
      catch (Exception ex)
      {
        NotifyException(ex, slotChannel.ServerIndex);
        return false;
      }
    }

    private ITVAccessService TvServer(int serverIndex)
    {
      return _tvServers[serverIndex].TvServer;
    }

    private bool CheckConnection(int serverIndex)
    {
      bool reconnect = false;
      ServerContext tvServer = _tvServers[serverIndex];
      try
      {
        DateTime now = DateTime.Now;
        if (now - tvServer.LastCheckTime > _checkDuration && tvServer.TvServer != null)
        {
          tvServer.ConnectionOk = tvServer.TvServer.TestConnectionToTVService();
          tvServer.LastCheckTime = now;
        }

        _reconnectCounter = 0;
      }
      catch (CommunicationObjectFaultedException)
      {
        reconnect = true;
      }
      catch (ProtocolException)
      {
        reconnect = true;
      }
      catch (Exception ex)
      {
        NotifyException(ex, serverIndex);
        return false;
      }
      if (reconnect)
      {
        // Try to reconnect
        tvServer.CreateChannel();
        if (_reconnectCounter++ < MAX_RECONNECT_ATTEMPTS)
          return CheckConnection(serverIndex);

        return false;
      }
      return tvServer.ConnectionOk;
    }

    private void NotifyException(Exception ex, int serverIndex)
    {
      NotifyException(ex, null, serverIndex);
    }

    private void NotifyException(Exception ex, string localizationMessage, int serverIndex)
    {
      string serverName = _tvServers[serverIndex].ServerName;
      string notification = string.IsNullOrEmpty(localizationMessage)
                              ? string.Format("{0}:", serverName)
                              : ServiceRegistration.Get<ILocalization>().ToString(localizationMessage, serverName);
      notification += " " + ex.Message;

      ServiceRegistration.Get<INotificationService>().EnqueueNotification(NotificationType.Error, RES_TV_CONNECTION_ERROR_TITLE, notification, true);
      ServiceRegistration.Get<ILogger>().Error(notification, ex);
    }

    private void CreateAllTvServerConnections()
    {
      MPExtendedProviderSettings setting = _settings.Settings;
      if (string.IsNullOrWhiteSpace(setting.TvServerHost))
        return;

      // Needed for checking setting changes
      _serverNames = setting.TvServerHost;

      string[] serverNames = setting.TvServerHost.Split(';');
      _tvServers = new ServerContext[serverNames.Length];

      for (int serverIndex = 0; serverIndex < serverNames.Length; serverIndex++)
      {
        try
        {
          string serverName = serverNames[serverIndex].Trim();
          ServerContext tvServer = new ServerContext
          {
            ServerName = serverName,
            ConnectionOk = false,
            Username = setting.Username,
            Password = setting.Password
          };
          _tvServers[serverIndex] = tvServer;
          tvServer.CreateChannel();
        }
        catch (Exception ex)
        {
          NotifyException(ex, RES_TV_CONNECTION_ERROR_TEXT, serverIndex);
        }
      }
    }

    #endregion

    #region IChannelAndGroupInfo members

    public int SelectedChannelId { get; set; }

    public int SelectedChannelGroupId
    {
      get
      {
        MPExtendedProviderSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<MPExtendedProviderSettings>();
        return settings.LastChannelGroupId;
      }
      set
      {
        MPExtendedProviderSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<MPExtendedProviderSettings>();
        settings.LastChannelGroupId = value;
        ServiceRegistration.Get<ISettingsManager>().Save(settings);
      }
    }

    /// <summary>
    /// Creates a group prefix if more than 1 tvserver is used.
    /// </summary>
    /// <param name="serverIndex">Server Index</param>
    /// <returns>Formatted prefix or String.Empty</returns>
    protected string GetServerPrefix(int serverIndex)
    {
      if (_tvServers.Length == 1 || serverIndex >= _tvServers.Length)
        return string.Empty;

      return string.Format("{0}: ", _tvServers[serverIndex].ServerName);
    }

    public async Task<AsyncResult<IList<IChannelGroup>>> GetChannelGroupsAsync()
    {
      var groups = new List<IChannelGroup>();
      try
      {
        int idx = 0;
        foreach (ServerContext tvServer in _tvServers)
        {
          if (!CheckConnection(idx))
            continue;
          IList<WebChannelGroup> tvGroups = tvServer.TvServer.GetGroups();
          foreach (WebChannelGroup webChannelGroup in tvGroups)
          {
            groups.Add(new ChannelGroup { ChannelGroupId = webChannelGroup.Id, Name = String.Format("{0}{1}", GetServerPrefix(idx), webChannelGroup.GroupName), ServerIndex = idx });
          }
          idx++;
        }
        return new AsyncResult<IList<IChannelGroup>>(true, groups);
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error(ex.Message);
        return new AsyncResult<IList<IChannelGroup>>(false, null);
      }
    }

    public async Task<AsyncResult<IChannel>> GetChannelAsync(int channelId)
    {
      IChannel channel;
      if (_channelCache.TryGetValue(channelId, out channel))
        return new AsyncResult<IChannel>(true, channel);

      // TODO: lookup by ID cannot guess which server might be adressed, so we force the first one.
      int serverIndex = 0;
      if (!CheckConnection(serverIndex))
        return new AsyncResult<IChannel>(false, null);
      try
      {
        WebChannelBasic webChannel = TvServer(serverIndex).GetChannelBasicById(channelId);
        channel = new Channel { ChannelId = webChannel.Id, Name = webChannel.Title, ServerIndex = serverIndex };
        _channelCache[channelId] = channel;
        return new AsyncResult<IChannel>(true, channel);
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error(ex.Message);
        return new AsyncResult<IChannel>(false, null);
      }
    }

    public async Task<AsyncResult<IList<IChannel>>> GetChannelsAsync(IChannelGroup group)
    {
      var channels = new List<IChannel>();
      ChannelGroup indexGroup = group as ChannelGroup;
      if (indexGroup == null || !CheckConnection(indexGroup.ServerIndex))
        return new AsyncResult<IList<IChannel>>(false, null);
      try
      {
        IList<WebChannelBasic> tvChannels = TvServer(indexGroup.ServerIndex).GetChannelsBasic(group.ChannelGroupId);
        foreach (WebChannelBasic webChannel in tvChannels)
        {
          channels.Add(new Channel { ChannelId = webChannel.Id, Name = webChannel.Title, ServerIndex = indexGroup.ServerIndex });
        }
        return new AsyncResult<IList<IChannel>>(true, channels);
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error(ex.Message);
        return new AsyncResult<IList<IChannel>>(false, null);
      }
    }

    public async Task<AsyncResult<IChannel>> GetChannelAsync(IProgram program)
    {
      return await GetChannelAsync(program.ChannelId);
    }

    /// <summary>
    /// Gets a program by its <see cref="IProgram.ProgramId"/>.
    /// </summary>
    /// <param name="programId">Program ID.</param>
    /// <param name="program">Program.</param>
    /// <returns>True if succeeded.</returns>
    public bool GetProgram(int programId, out IProgram program)
    {
      throw new NotImplementedException();
    }

    #endregion

    public MediaItem CreateMediaItem(int slotIndex, string streamUrl, IChannel channel)
    {
      LiveTvMediaItem tvStream = SlimTvMediaItemBuilder.CreateMediaItem(slotIndex, streamUrl, channel);
      if (tvStream != null)
      {
        // Add program infos to the LiveTvMediaItem
        IProgram currentProgram;
        if (GetCurrentProgram(channel, out currentProgram))
          tvStream.AdditionalProperties[LiveTvMediaItem.CURRENT_PROGRAM] = currentProgram;

        IProgram nextProgram;
        if (GetNextProgram(channel, out nextProgram))
          tvStream.AdditionalProperties[LiveTvMediaItem.NEXT_PROGRAM] = nextProgram;

        return tvStream;
      }
      return null;
    }

    #region IProgramInfo Member

    public bool GetCurrentProgram(IChannel channel, out IProgram program)
    {
      program = null;

      Channel indexChannel = channel as Channel;
      if (indexChannel == null)
        return false;

      if (!CheckConnection(indexChannel.ServerIndex))
        return false;

      try
      {
        WebProgramDetailed tvProgram = TvServer(indexChannel.ServerIndex).GetCurrentProgramOnChannel(channel.ChannelId);
        if (tvProgram != null)
        {
          program = new Program(tvProgram, indexChannel.ServerIndex);
          return true;
        }
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error(ex.Message);
      }
      return false;
    }

    public bool GetNextProgram(IChannel channel, out IProgram program)
    {
      program = null;

      Channel indexChannel = channel as Channel;
      if (indexChannel == null)
        return false;

      if (!CheckConnection(indexChannel.ServerIndex))
        return false;

      IProgram currentProgram;
      try
      {
        if (GetCurrentProgram(channel, out currentProgram))
        {
          IList<WebProgramDetailed> nextPrograms = TvServer(indexChannel.ServerIndex).GetProgramsDetailedForChannel(channel.ChannelId,
                                                                                          currentProgram.EndTime.AddMinutes(1),
                                                                                          currentProgram.EndTime.AddMinutes(1));
          if (nextPrograms != null && nextPrograms.Count > 0)
          {
            program = new Program(nextPrograms[0], indexChannel.ServerIndex);
            return true;
          }
        }
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error(ex.Message);
      }
      return false;
    }

    /// <summary>
    /// Tries to get the current and next program for the given <paramref name="channel"/>.
    /// </summary>
    /// <param name="channel">Channel</param>
    /// <returns><c>true</c> if a program could be found</returns>
    public async Task<AsyncResult<IProgram[]>> GetNowNextProgramAsync(IChannel channel)
    {
      // TODO: caching from NativeProvider?
      IProgram[] programNowNext = new IProgram[2];
      Channel indexChannel = channel as Channel;
      if (indexChannel == null || !CheckConnection(indexChannel.ServerIndex))
        return new AsyncResult<IProgram[]>(false, null);

      try
      {
        IList<WebProgramDetailed> tvPrograms = TvServer(indexChannel.ServerIndex).GetNowNextWebProgramDetailedForChannel(channel.ChannelId);
        if (tvPrograms.Count > 0 && tvPrograms[0] != null)
          programNowNext[0] = new Program(tvPrograms[0], indexChannel.ServerIndex);
        if (tvPrograms.Count > 1 && tvPrograms[1] != null)
          programNowNext[1] = new Program(tvPrograms[1], indexChannel.ServerIndex);
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error(ex.Message);
        return new AsyncResult<IProgram[]>(false, null);
      }
      return new AsyncResult<IProgram[]>(programNowNext[0] != null, programNowNext);
    }

    public async Task<AsyncResult<IDictionary<int, IProgram[]>>> GetNowAndNextForChannelGroupAsync(IChannelGroup channelGroup)
    {
      ChannelGroup indexGroup = channelGroup as ChannelGroup;
      if (indexGroup == null || !CheckConnection(indexGroup.ServerIndex))
        return new AsyncResult<IDictionary<int, IProgram[]>>(false, null);

      var programs = new Dictionary<int, IProgram[]>();
      try
      {
        IList<IChannel> channels;
        var result = await GetChannelsAsync(indexGroup);
        if (!result.Success)
          return new AsyncResult<IDictionary<int, IProgram[]>>(false, null);

        foreach (IChannel channel in result.Result)
        {
          IProgram[] nowNext = new IProgram[2];
          IList<WebProgramDetailed> tvPrograms = TvServer(indexGroup.ServerIndex).GetNowNextWebProgramDetailedForChannel(channel.ChannelId);
          if (tvPrograms.Count == 0)
            continue;

          if (tvPrograms.Count > 0 && tvPrograms[0] != null)
            nowNext[0] = new Program(tvPrograms[0], indexGroup.ServerIndex);
          if (tvPrograms.Count > 1 && tvPrograms[1] != null)
            nowNext[1] = new Program(tvPrograms[1], indexGroup.ServerIndex);
          programs[channel.ChannelId] = nowNext;
        }
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error(ex.Message);
        return new AsyncResult<IDictionary<int, IProgram[]>>(false, null);
      }
      return new AsyncResult<IDictionary<int, IProgram[]>>(true, programs);
    }

    public async Task<AsyncResult<IList<IProgram>>> GetProgramsAsync(IChannel channel, DateTime from, DateTime to)
    {
      Channel indexChannel = channel as Channel;
      if (indexChannel == null || !CheckConnection(indexChannel.ServerIndex))
        return new AsyncResult<IList<IProgram>>(false, null);

      var programs = new List<IProgram>();
      try
      {
        IList<WebProgramDetailed> tvPrograms = TvServer(indexChannel.ServerIndex).GetProgramsDetailedForChannel(channel.ChannelId, from, to);
        foreach (WebProgramDetailed webProgram in tvPrograms)
          programs.Add(new Program(webProgram, indexChannel.ServerIndex));
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error(ex.Message);
        return new AsyncResult<IList<IProgram>>(false, null);
      }
      return new AsyncResult<IList<IProgram>>(programs.Count > 0, programs);
    }

    public async Task<AsyncResult<IList<IProgram>>> GetProgramsAsync(string title, DateTime from, DateTime to)
    {
      // TODO: lookup by ID cannot guess which server might be adressed, so we force the first one.
      int serverIndex = 0;
      if (!CheckConnection(serverIndex))
        return new AsyncResult<IList<IProgram>>(false, null);

      var programs = new List<IProgram>();
      try
      {
        IList<WebProgramDetailed> tvPrograms = TvServer(serverIndex).SearchProgramsDetailed(title).
          Where(p => p.StartTime >= from && p.StartTime <= to || p.EndTime >= from && p.EndTime <= to).ToList();
        foreach (WebProgramDetailed webProgram in tvPrograms)
          programs.Add(new Program(webProgram, serverIndex));
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error(ex.Message);
        return new AsyncResult<IList<IProgram>>(false, null);
      }
      return new AsyncResult<IList<IProgram>>(programs.Count > 0, programs);
    }

    /// <summary>
    /// Tries to get a list of programs for all channels of the given <paramref name="channelGroup"/> and time range.
    /// </summary>
    /// <param name="channelGroup">Channel group</param>
    /// <param name="from">Time from</param>
    /// <param name="to">Time to</param>
    /// <returns><c>true</c> if at least one program could be found</returns>
    public async Task<AsyncResult<IList<IProgram>>> GetProgramsGroupAsync(IChannelGroup channelGroup, DateTime from, DateTime to)
    {
      ChannelGroup indexGroup = channelGroup as ChannelGroup;
      if (indexGroup == null || !CheckConnection(indexGroup.ServerIndex))
        return new AsyncResult<IList<IProgram>>(false, null);

      var programs = new List<IProgram>();
      try
      {
        IList<WebChannelPrograms<WebProgramDetailed>> tvPrograms = TvServer(indexGroup.ServerIndex).GetProgramsDetailedForGroup(channelGroup.ChannelGroupId, from, to);
        foreach (WebProgramDetailed webProgramDetailed in tvPrograms.SelectMany(webPrograms => webPrograms.Programs))
          programs.Add(new Program(webProgramDetailed, indexGroup.ServerIndex));
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error(ex.Message);
        return new AsyncResult<IList<IProgram>>(false, null);
      }
      return new AsyncResult<IList<IProgram>>(programs.Count > 0, programs);
    }

    public async Task<AsyncResult<IList<IProgram>>> GetProgramsForScheduleAsync(ISchedule schedule)
    {
      //Schedule indexSchedule = (Schedule)schedule;
      //programs = null;
      //if (!CheckConnection(indexSchedule.ServerIndex))
      //  return false;

      ServiceRegistration.Get<ILogger>().Error("SlimTV MPExtendedProvider: GetProgramsForSchedule is not implemented!");
      return new AsyncResult<IList<IProgram>>(false, null);
    }

    public bool GetScheduledPrograms(IChannel channel, out IList<IProgram> programs)
    {
      throw new NotImplementedException();
    }

    #endregion

    #region IScheduleControl Member

    public async Task<AsyncResult<ISchedule>> CreateScheduleAsync(IProgram program, ScheduleRecordingType recordingType)
    {
      Program indexProgram = program as Program;
      if (indexProgram == null || !CheckConnection(indexProgram.ServerIndex))
        return new AsyncResult<ISchedule>(false, null);

      try
      {
        // Note: the enums WebScheduleType and ScheduleRecordingType are defined equally. If one of them gets extended, the other must be changed the same way.
        var result = TvServer(indexProgram.ServerIndex).AddSchedule(program.ChannelId, program.Title, program.StartTime, program.EndTime, (WebScheduleType)recordingType);
        return new AsyncResult<ISchedule>(true, null);
      }
      catch
      {
        return new AsyncResult<ISchedule>(false, null);
      }
    }

    public async Task<AsyncResult<ISchedule>> CreateScheduleByTimeAsync(IChannel channel, DateTime from, DateTime to, ScheduleRecordingType recordingType)
    {
      Channel indexChannel = channel as Channel;
      if (indexChannel == null || !CheckConnection(indexChannel.ServerIndex))
        return new AsyncResult<ISchedule>(false, null);

      try
      {
        var result = TvServer(indexChannel.ServerIndex).AddSchedule(channel.ChannelId, "Manual", from, to, (WebScheduleType)recordingType);
        return new AsyncResult<ISchedule>(true, null);
      }
      catch
      {
        return new AsyncResult<ISchedule>(false, null);
      }
    }

    public async Task<bool> RemoveScheduleForProgramAsync(IProgram program, ScheduleRecordingType recordingType)
    {
      Program indexProgram = program as Program;
      if (indexProgram == null)
        return false;

      if (!CheckConnection(indexProgram.ServerIndex))
        return false;

      try
      {
        ITVAccessService tvAccessService = TvServer(indexProgram.ServerIndex);
        if (recordingType == ScheduleRecordingType.Once)
        {
          return tvAccessService.CancelSchedule(program.ProgramId);
        }

        // TODO: find matching schedule? return tvAccessService.DeleteSchedule(indexProgram);
        return false;
      }
      catch
      {
        return false;
      }
    }

    public async Task<bool> RemoveScheduleAsync(ISchedule schedule)
    {
      Schedule indexSchedule = schedule as Schedule;
      if (indexSchedule == null)
        return false;

      if (!CheckConnection(indexSchedule.ServerIndex))
        return false;

      try
      {
        ITVAccessService tvAccessService = TvServer(indexSchedule.ServerIndex);
        return tvAccessService.DeleteSchedule(indexSchedule.ScheduleId);
      }
      catch
      {
        return false;
      }
    }

    public async Task<AsyncResult<IList<ISchedule>>> GetSchedulesAsync()
    {
      // TODO: lookup by ID cannot guess which server might be adressed, so we force the first one.
      int serverIndex = 0;
      if (!CheckConnection(serverIndex))
        return new AsyncResult<IList<ISchedule>>(false, null);

      try
      {
        ITVAccessService tvAccessService = TvServer(serverIndex);
        var webSchedules = tvAccessService.GetSchedules();
        var schedules = webSchedules.Select(s => new Schedule(s, serverIndex)).Cast<ISchedule>().ToList();
        return new AsyncResult<IList<ISchedule>>(true, schedules);
      }
      catch
      {
        return new AsyncResult<IList<ISchedule>>(false, null);
      }
    }

    public async Task<AsyncResult<ISchedule>> IsCurrentlyRecordingAsync(string fileName)
    {
      // TODO
      return new AsyncResult<ISchedule>(false, null);
    }

    public async Task<AsyncResult<RecordingStatus>> GetRecordingStatusAsync(IProgram program)
    {
      var recordingStatus = RecordingStatus.None;

      Program indexProgram = program as Program;
      if (indexProgram == null || !CheckConnection(indexProgram.ServerIndex))
        return new AsyncResult<RecordingStatus>(false, recordingStatus);

      try
      {
        WebProgramDetailed programDetailed = TvServer(indexProgram.ServerIndex).GetProgramDetailedById(program.ProgramId);
        recordingStatus = Program.GetRecordingStatus(programDetailed);
        return new AsyncResult<RecordingStatus>(true, recordingStatus);
      }
      catch
      {
        return new AsyncResult<RecordingStatus>(false, recordingStatus);
      }
    }

    public async Task<AsyncResult<string>> GetRecordingFileOrStreamAsync(IProgram program)
    {
      Program indexProgram = program as Program;
      if (indexProgram == null || !CheckConnection(indexProgram.ServerIndex))
        return new AsyncResult<string>(false, null);

      try
      {
        // TODO: GetRecordings will return all recordings from server and we filter the list on client side. This could be optimized with MPExtended 0.6, where a server filter argument was added.
        var recording = TvServer(indexProgram.ServerIndex).GetRecordings(WebSortField.StartTime, WebSortOrder.Desc).
          FirstOrDefault(r => r.IsRecording && r.ChannelId == program.ChannelId && r.Title == program.Title);
        return new AsyncResult<string>(recording != null, recording?.FileName);
      }
      catch
      {
        return new AsyncResult<string>(false, null);
      }
    }
    #endregion
  }
}
