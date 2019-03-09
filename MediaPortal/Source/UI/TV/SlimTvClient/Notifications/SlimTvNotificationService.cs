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

using MediaPortal.Common;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.Settings;
using MediaPortal.Plugins.SlimTv.Client.Models;
using MediaPortal.Plugins.SlimTv.Client.Settings;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.UI.ServerCommunication;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaPortal.Plugins.SlimTv.Client.Notifications
{
  /// <summary>
  /// Implementation of <see cref="ISlimTvNotificationService"/>.
  /// </summary>
  public class SlimTvNotificationService : ISlimTvNotificationService, IDisposable
  {
    #region Protected members

    protected TvServerState _currentTvServerState;
    protected AsynchronousMessageQueue _messageQueue;

    #endregion

    #region Ctor/Dispose

    public SlimTvNotificationService()
    {
      InitServerState();
      SubscribeToMessages();
    }

    public void Dispose()
    {
      _messageQueue.Shutdown();
    }

    #endregion

    #region MessageHandling

    void SubscribeToMessages()
    {
      _messageQueue = new AsynchronousMessageQueue(this, new string[] { ServerStateMessaging.CHANNEL });
      _messageQueue.MessageReceived += OnMessageReceived;
      _messageQueue.Start();
    }

    void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == ServerStateMessaging.CHANNEL)
      {
        //Check if Tv Server state has changed and update if necessary
        ServerStateMessaging.MessageType messageType = (ServerStateMessaging.MessageType)message.MessageType;
        if (messageType == ServerStateMessaging.MessageType.StatesChanged)
        {
          var states = message.MessageData[ServerStateMessaging.STATES] as IDictionary<Guid, object>;
          if (states != null && states.ContainsKey(TvServerState.STATE_ID))
            OnTvServerStateChanged(states[TvServerState.STATE_ID] as TvServerState);
        }
      }
    }

    #endregion

    #region ISlimTvNotificationService implementation

    public TvServerState CurrentTvServerState
    {
      get { return _currentTvServerState; }
    }

    public void ShowNotification(ISlimTvNotification notification, TimeSpan duration)
    {
      SlimTvNotificationModel.Instance.ShowNotification(notification, duration);
    }

    #endregion

    #region Protected methods

    protected void InitServerState()
    {
      //Get an initial state to avoid showing notifications for events that happened before the client was started.
      var ssm = ServiceRegistration.Get<IServerStateManager>();
      if (!ssm.TryGetState(TvServerState.STATE_ID, out _currentTvServerState))
        _currentTvServerState = null;
    }

    protected void OnTvServerStateChanged(TvServerState newState)
    {
      TvServerState oldState = _currentTvServerState;
      _currentTvServerState = newState;
      ShowScheduleChangeNotifications(oldState, newState);
    }

    protected void ShowScheduleChangeNotifications(TvServerState oldState, TvServerState newState)
    {
      var sm = ServiceRegistration.Get<ISettingsManager>();
      var settings = sm.Load<SlimTvClientSettings>();
      if (settings.RecordingNotificationDuration < 1 ||
        (!settings.ShowRecordingStartedNotifications && !settings.ShowRecordingEndedNotifications))
        return;
            
      IEnumerable<ISchedule> startedSchedules;
      IEnumerable<ISchedule> endedSchedules;
      GetChangedSchedules(oldState, newState, out startedSchedules, out endedSchedules);

      TimeSpan duration = TimeSpan.FromSeconds(settings.RecordingNotificationDuration);
      if (settings.ShowRecordingStartedNotifications)
        foreach (ISchedule schedule in startedSchedules)
          ShowRecordingStartedNotification(schedule, duration);

      if (settings.ShowRecordingEndedNotifications)
        foreach (ISchedule schedule in endedSchedules)
          ShowRecordingEndedNotification(schedule, duration);
    }

    protected void ShowRecordingStartedNotification(ISchedule schedule, TimeSpan duration)
    {
      ShowNotification(new SlimTvRecordingStartedNotification(schedule, GetChannel(schedule.ChannelId)), duration);
    }

    protected void ShowRecordingEndedNotification(ISchedule schedule, TimeSpan duration)
    {
      ShowNotification(new SlimTvRecordingEndedNotification(schedule, GetChannel(schedule.ChannelId)), duration);
    }

    protected void GetChangedSchedules(TvServerState oldState, TvServerState newState, out IEnumerable<ISchedule> started, out IEnumerable<ISchedule> ended)
    {
      IList<ISchedule> oldSchedules = oldState != null && oldState.CurrentlyRecordingSchedules != null ?
        oldState.CurrentlyRecordingSchedules : new List<ISchedule>();
      IList<ISchedule> newSchedules = newState != null && newState.CurrentlyRecordingSchedules != null ?
        newState.CurrentlyRecordingSchedules : new List<ISchedule>();

      started = newSchedules.Where(s => !oldSchedules.Any(os => os.ScheduleId == s.ScheduleId));
      ended = oldSchedules.Where(s => !newSchedules.Any(ns => ns.ScheduleId == s.ScheduleId));
    }

    protected IChannel GetChannel(int channelId)
    {
      ITvHandler tvHandler = ServiceRegistration.Get<ITvHandler>();
      tvHandler.Initialize();
      if (tvHandler.ChannelAndGroupInfo == null)
        return null;
      var result = tvHandler.ChannelAndGroupInfo.GetChannelAsync(channelId).Result;
      return result.Success ? result.Result : null;
    }

    #endregion
  }
}
