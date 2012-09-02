//#region Copyright (C) 2007-2012 Team MediaPortal

///*
//    Copyright (C) 2007-2012 Team MediaPortal
//    http://www.team-mediaportal.com

//    This file is part of MediaPortal 2

//    MediaPortal 2 is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.

//    MediaPortal 2 is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//    GNU General Public License for more details.

//    You should have received a copy of the GNU General Public License
//    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
//*/

//#endregion

//using System;
//using System.Collections.Generic;
//using MediaPortal.Common;
//using MediaPortal.Common.General;
//using MediaPortal.Common.Localization;
//using MediaPortal.Common.Logging;
//using MediaPortal.Common.MediaManagement;
//using MediaPortal.Common.Settings;
//using MediaPortal.Plugins.SlimTv.Interfaces;
//using MediaPortal.Plugins.SlimTv.Interfaces.Items;
//using MediaPortal.Plugins.SlimTv.Interfaces.LiveTvMediaItem;
//using MediaPortal.Plugins.SlimTv.Interfaces.ResourceProvider;
//using MediaPortal.Plugins.SlimTv.Providers.Items;
//using MediaPortal.Plugins.SlimTv.Providers.Settings;
//using MediaPortal.UI.Presentation.UiNotifications;
//using IChannel = MediaPortal.Plugins.SlimTv.Interfaces.Items.IChannel;

//namespace MediaPortal.Plugins.SlimTv.Providers
//{
//  public class SlimTvNativeProvider : ITvProvider, ITimeshiftControl, IProgramInfo, IChannelAndGroupInfo, IScheduleControl
//  {
//    #region Internal class

//    internal class ServerContext
//    {
//      public string ServerName;
//      public string Username;
//      public string Password;
//      public ITVAccessService TvServer;
//      public bool ConnectionOk;

//      public static bool IsLocal(string host)
//      {
//        if (string.IsNullOrEmpty(host))
//          return true;

//        string lowerHost = host.ToLowerInvariant();
//        return lowerHost == "localhost" || lowerHost == LocalSystem.ToLowerInvariant() || host == "127.0.0.1" || host == "::1";
//      }

//      public void CreateChannel()
//      {
//        Binding binding;
//        EndpointAddress endpointAddress;
//        bool useAuth = !string.IsNullOrEmpty(Username) && !string.IsNullOrEmpty(Password);
//        if (IsLocal(ServerName))
//        {
//          endpointAddress = new EndpointAddress("net.pipe://localhost/MPExtended/TVAccessService");
//          binding = new NetNamedPipeBinding { MaxReceivedMessageSize = 10000000 };
//        }
//        else
//        {
//          endpointAddress = new EndpointAddress(string.Format("http://{0}:4322/MPExtended/TVAccessService", ServerName));
//          BasicHttpBinding basicBinding = new BasicHttpBinding { MaxReceivedMessageSize = 10000000 };
//          if (useAuth)
//          {
//            basicBinding.Security.Mode = BasicHttpSecurityMode.TransportCredentialOnly;
//            basicBinding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Basic;
//          }
//          binding = basicBinding;
//        }
//        binding.OpenTimeout = TimeSpan.FromSeconds(5);
//        ChannelFactory<ITVAccessService> factory = new ChannelFactory<ITVAccessService>(binding);
//        if (factory.Credentials != null && useAuth)
//        {
//          factory.Credentials.UserName.UserName = Username;
//          factory.Credentials.UserName.Password = Password;
//        }
//        TvServer = factory.CreateChannel(endpointAddress);
//      }
//    }

//    #endregion

//    #region Constants

//    private const string RES_TV_CONNECTION_ERROR_TITLE = "[Settings.Plugins.TV.ConnectionErrorTitle]";
//    private const string RES_TV_CONNECTION_ERROR_TEXT = "[Settings.Plugins.TV.ConnectionErrorText]";
//    private const int MAX_RECONNECT_ATTEMPTS = 2;

//    #endregion

//    #region Fields

//    private static readonly string LocalSystem = SystemName.LocalHostName;
//    private readonly IChannel[] _channels = new IChannel[2];
//    private ServerContext[] _tvServers;
//    private int _reconnectCounter = 0;

//    #endregion

//    #region ITvProvider Member

//    public IChannel GetChannel(int slotIndex)
//    {
//      return _channels[slotIndex];
//    }

//    public string Name
//    {
//      get { return "TV4Home Provider"; }
//    }

//    #endregion

//    #region ITimeshiftControl Member

//    public bool Init()
//    {
//      CreateAllTvServerConnections();
//      return true;
//    }

//    public bool DeInit()
//    {
//      if (_tvServers == null)
//        return false;

//      try
//      {
//        foreach (ServerContext tvServer in _tvServers)
//        {
//          if (tvServer.TvServer != null)
//          {
//            tvServer.TvServer.CancelCurrentTimeShifting(GetTimeshiftUserName(0));
//            tvServer.TvServer.CancelCurrentTimeShifting(GetTimeshiftUserName(1));
//          }
//        }
//      }
//      catch (Exception) { }

//      _tvServers = null;
//      return true;
//    }

//    public String GetTimeshiftUserName(int slotIndex)
//    {
//      return String.Format("STC_{0}_{1}", LocalSystem, slotIndex);
//    }

//    public bool StartTimeshift(int slotIndex, IChannel channel, out MediaItem timeshiftMediaItem)
//    {
//      timeshiftMediaItem = null;
//      Channel indexChannel = channel as Channel;
//      if (indexChannel == null)
//        return false;

//      if (!CheckConnection(indexChannel.ServerIndex))
//        return false;
//      try
//      {
//        String streamUrl = TvServer(indexChannel.ServerIndex).SwitchTVServerToChannelAndGetStreamingUrl(GetTimeshiftUserName(slotIndex), channel.ChannelId);
//        if (String.IsNullOrEmpty(streamUrl))
//          return false;

//        _channels[slotIndex] = channel;

//        // assign a MediaItem, can be null if streamUrl is the same.
//        timeshiftMediaItem = CreateMediaItem(slotIndex, streamUrl, channel);
//        return true;
//      }
//      catch (Exception ex)
//      {
//        NotifyException(ex, indexChannel.ServerIndex);
//        return false;
//      }
//    }

//    public bool StopTimeshift(int slotIndex)
//    {
//      Channel slotChannel = _channels[slotIndex] as Channel;
//      if (slotChannel == null)
//        return false;

//      if (!CheckConnection(slotChannel.ServerIndex))
//        return false;

//      try
//      {
//        TvServer(slotChannel.ServerIndex).CancelCurrentTimeShifting(GetTimeshiftUserName(slotIndex));
//        _channels[slotIndex] = null;
//        return true;
//      }
//      catch (Exception ex)
//      {
//        NotifyException(ex, slotChannel.ServerIndex);
//        return false;
//      }
//    }

//    private ITVAccessService TvServer(int serverIndex)
//    {
//      return _tvServers[serverIndex].TvServer;
//    }

//    private bool CheckConnection(int serverIndex)
//    {
//      bool reconnect = false;
//      ServerContext tvServer = _tvServers[serverIndex];
//      try
//      {
//        if (tvServer.TvServer != null)
//          tvServer.ConnectionOk = tvServer.TvServer.TestConnectionToTVService();

//        _reconnectCounter = 0;
//      }
//      catch (CommunicationObjectFaultedException)
//      {
//        reconnect = true;
//      }
//      catch (ProtocolException)
//      {
//        reconnect = true;
//      }
//      catch (Exception ex)
//      {
//        NotifyException(ex, serverIndex);
//        return false;
//      }
//      if (reconnect)
//      {
//        // Try to reconnect
//        tvServer.CreateChannel();
//        if (_reconnectCounter++ < MAX_RECONNECT_ATTEMPTS)
//          return CheckConnection(serverIndex);

//        return false;
//      }
//      return tvServer.ConnectionOk;
//    }

//    private void NotifyException(Exception ex, int serverIndex)
//    {
//      NotifyException(ex, null, serverIndex);
//    }

//    private void NotifyException(Exception ex, string localizationMessage, int serverIndex)
//    {
//      string serverName = _tvServers[serverIndex].ServerName;
//      string notification = string.IsNullOrEmpty(localizationMessage)
//                              ? string.Format("{0}:", serverName)
//                              : ServiceRegistration.Get<ILocalization>().ToString(localizationMessage, serverName);
//      notification += " " + ex.Message;

//      ServiceRegistration.Get<INotificationService>().EnqueueNotification(NotificationType.Error, RES_TV_CONNECTION_ERROR_TITLE,
//          notification, true);
//      ServiceRegistration.Get<ILogger>().Error(notification);
//    }

//    private void CreateAllTvServerConnections()
//    {
//      MPExtendedProviderSettings setting = ServiceRegistration.Get<ISettingsManager>().Load<MPExtendedProviderSettings>();
//      if (setting.TvServerHost == null)
//        return;

//      string[] serverNames = setting.TvServerHost.Split(';');
//      _tvServers = new ServerContext[serverNames.Length];

//      for (int serverIndex = 0; serverIndex < serverNames.Length; serverIndex++)
//      {
//        try
//        {
//          string serverName = serverNames[serverIndex];
//          ServerContext tvServer = new ServerContext
//                                     {
//                                       ServerName = serverName,
//                                       ConnectionOk = false,
//                                       Username = setting.Username,
//                                       Password = setting.Password
//                                     };
//          _tvServers[serverIndex] = tvServer;
//          tvServer.CreateChannel();
//        }
//        catch (Exception ex)
//        {
//          NotifyException(ex, RES_TV_CONNECTION_ERROR_TEXT, serverIndex);
//        }
//      }
//    }

//    #endregion

//    #region IChannelAndGroupInfo members

//    public int SelectedChannelId { get; set; }

//    public int SelectedChannelGroupId
//    {
//      get
//      {
//        MPExtendedProviderSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<MPExtendedProviderSettings>();
//        return settings.LastChannelGroupId;
//      }
//      set
//      {
//        MPExtendedProviderSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<MPExtendedProviderSettings>();
//        settings.LastChannelGroupId = value;
//        ServiceRegistration.Get<ISettingsManager>().Save(settings);
//      }
//    }

//    /// <summary>
//    /// Creates a group prefix if more than 1 tvserver is used.
//    /// </summary>
//    /// <param name="serverIndex">Server Index</param>
//    /// <returns>Formatted prefix or String.Empty</returns>
//    protected string GetServerPrefix(int serverIndex)
//    {
//      if (_tvServers.Length == 1 || serverIndex >= _tvServers.Length)
//        return string.Empty;

//      return string.Format("{0}: ", _tvServers[serverIndex].ServerName);
//    }

//    public bool GetChannelGroups(out IList<IChannelGroup> groups)
//    {
//      groups = new List<IChannelGroup>();
//      try
//      {
//        int idx = 0;
//        foreach (ServerContext tvServer in _tvServers)
//        {
//          if (!CheckConnection(idx))
//            continue;
//          IList<WebChannelGroup> tvGroups = tvServer.TvServer.GetGroups();
//          foreach (WebChannelGroup webChannelGroup in tvGroups)
//          {
//            groups.Add(new ChannelGroup { ChannelGroupId = webChannelGroup.Id, Name = String.Format("{0}{1}", GetServerPrefix(idx), webChannelGroup.GroupName), ServerIndex = idx });
//          }
//          idx++;
//        }
//        return true;
//      }
//      catch (Exception ex)
//      {
//        ServiceRegistration.Get<ILogger>().Error(ex.Message);
//        return false;
//      }
//    }

//    public bool GetChannels(IChannelGroup group, out IList<IChannel> channels)
//    {
//      channels = new List<IChannel>();
//      ChannelGroup indexGroup = group as ChannelGroup;
//      if (indexGroup == null)
//        return false;
//      if (!CheckConnection(indexGroup.ServerIndex))
//        return false;
//      try
//      {
//        IList<WebChannelBasic> tvChannels = TvServer(indexGroup.ServerIndex).GetChannelsBasic(group.ChannelGroupId);
//        foreach (WebChannelBasic webChannel in tvChannels)
//        {
//          channels.Add(new Channel { ChannelId = webChannel.Id, Name = webChannel.DisplayName, ServerIndex = indexGroup.ServerIndex });
//        }
//        return true;
//      }
//      catch (Exception ex)
//      {
//        ServiceRegistration.Get<ILogger>().Error(ex.Message);
//        return false;
//      }
//    }

//    public bool GetChannel(IProgram program, out IChannel channel)
//    {
//      channel = null;
//      Program indexProgram = program as Program;
//      if (indexProgram == null)
//        return false;

//      if (!CheckConnection(indexProgram.ServerIndex))
//        return false;

//      try
//      {
//        WebChannelBasic tvChannel = TvServer(indexProgram.ServerIndex).GetChannelBasicById(indexProgram.ChannelId);
//        if (tvChannel != null)
//        {
//          channel = new Channel { ChannelId = tvChannel.Id, Name = tvChannel.DisplayName, ServerIndex = indexProgram.ServerIndex };
//          return true;
//        }
//      }
//      catch (Exception ex)
//      {
//        ServiceRegistration.Get<ILogger>().Error(ex.Message);
//      }
//      return false;
//    }

//    #endregion

//    public MediaItem CreateMediaItem(int slotIndex, string streamUrl, IChannel channel)
//    {
//      LiveTvMediaItem tvStream = SlimTvMediaItemBuilder.CreateMediaItem(slotIndex, streamUrl, channel);
//      if (tvStream != null)
//      {
//        // Add program infos to the LiveTvMediaItem
//        IProgram currentProgram;
//        if (GetCurrentProgram(channel, out currentProgram))
//          tvStream.AdditionalProperties[LiveTvMediaItem.CURRENT_PROGRAM] = currentProgram;

//        IProgram nextProgram;
//        if (GetNextProgram(channel, out nextProgram))
//          tvStream.AdditionalProperties[LiveTvMediaItem.NEXT_PROGRAM] = nextProgram;

//        return tvStream;
//      }
//      return null;
//    }

//    #region IProgramInfo Member

//    public bool GetCurrentProgram(IChannel channel, out IProgram program)
//    {
//      program = null;

//      Channel indexChannel = channel as Channel;
//      if (indexChannel == null)
//        return false;

//      if (!CheckConnection(indexChannel.ServerIndex))
//        return false;

//      try
//      {
//        WebProgramDetailed tvProgram = TvServer(indexChannel.ServerIndex).GetCurrentProgramOnChannel(channel.ChannelId);
//        if (tvProgram != null)
//        {
//          program = new Program(tvProgram, indexChannel.ServerIndex);
//          return true;
//        }
//      }
//      catch (Exception ex)
//      {
//        ServiceRegistration.Get<ILogger>().Error(ex.Message);
//      }
//      return false;
//    }

//    public bool GetNextProgram(IChannel channel, out IProgram program)
//    {
//      program = null;

//      Channel indexChannel = channel as Channel;
//      if (indexChannel == null)
//        return false;

//      if (!CheckConnection(indexChannel.ServerIndex))
//        return false;

//      IProgram currentProgram;
//      try
//      {
//        if (GetCurrentProgram(channel, out currentProgram))
//        {
//          IList<WebProgramDetailed> nextPrograms = TvServer(indexChannel.ServerIndex).GetProgramsDetailedForChannel(channel.ChannelId,
//                                                                                          currentProgram.EndTime.AddMinutes(1),
//                                                                                          currentProgram.EndTime.AddMinutes(1));
//          if (nextPrograms != null && nextPrograms.Count > 0)
//          {
//            program = new Program(nextPrograms[0], indexChannel.ServerIndex);
//            return true;
//          }
//        }
//      }
//      catch (Exception ex)
//      {
//        ServiceRegistration.Get<ILogger>().Error(ex.Message);
//      }
//      return false;
//    }

//    public bool GetPrograms(IChannel channel, DateTime from, DateTime to, out IList<IProgram> programs)
//    {
//      programs = null;
//      Channel indexChannel = channel as Channel;
//      if (indexChannel == null)
//        return false;

//      if (!CheckConnection(indexChannel.ServerIndex))
//        return false;

//      programs = new List<IProgram>();
//      try
//      {
//        IList<WebProgramDetailed> tvPrograms = TvServer(indexChannel.ServerIndex).GetProgramsDetailedForChannel(channel.ChannelId, from, to);
//        foreach (WebProgramDetailed webProgram in tvPrograms)
//          programs.Add(new Program(webProgram, indexChannel.ServerIndex));
//      }
//      catch (Exception ex)
//      {
//        ServiceRegistration.Get<ILogger>().Error(ex.Message);
//        return false;
//      }
//      return programs.Count > 0;
//    }

//    public bool GetProgramsForSchedule(ISchedule schedule, out IList<IProgram> programs)
//    {
//      throw new NotImplementedException();
//    }

//    public bool GetScheduledPrograms(IChannel channel, out IList<IProgram> programs)
//    {
//      throw new NotImplementedException();
//    }

//    #endregion

//    #region IScheduleControl Member

//    public bool CreateSchedule(IProgram program)
//    {
//      Program indexProgram = program as Program;
//      if (indexProgram == null)
//        return false;

//      if (!CheckConnection(indexProgram.ServerIndex))
//        return false;

//      WebResult result;
//      try
//      {
//        result = TvServer(indexProgram.ServerIndex).AddSchedule(program.ChannelId, program.Title, program.StartTime,
//                                                       program.EndTime, WebScheduleType.Once);
//      }
//      catch
//      {
//        return false;
//      }
//      return result.Result;
//    }

//    public bool RemoveSchedule(IProgram program)
//    {
//      Program indexProgram = program as Program;
//      if (indexProgram == null)
//        return false;

//      if (!CheckConnection(indexProgram.ServerIndex))
//        return false;

//      WebResult result;
//      try
//      {
//        result = TvServer(indexProgram.ServerIndex).CancelSchedule(program.ProgramId);
//      }
//      catch
//      {
//        return false;
//      }
//      return result.Result;
//    }
    
//    public bool GetRecordingStatus(IProgram program, out RecordingStatus recordingStatus)
//    {
//      recordingStatus = RecordingStatus.None;
      
//      Program indexProgram = program as Program;
//      if (indexProgram == null)
//        return false;

//      if (!CheckConnection(indexProgram.ServerIndex))
//        return false;

//      try
//      {
//        WebProgramDetailed programDetailed = TvServer(indexProgram.ServerIndex).GetProgramDetailedById(program.ProgramId);
//        recordingStatus = Program.GetRecordingStatus(programDetailed);
//      }
//      catch
//      {
//        return false;
//      }
//      return true;
//    }

//    #endregion
//  }
//}
