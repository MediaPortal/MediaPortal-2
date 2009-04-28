#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Timers;
using MediaPortal.Control.InputManager;
using MediaPortal.Core;
using MediaPortal.Core.Messaging;
using MediaPortal.Presentation.DataObjects;
using MediaPortal.Presentation.Screens;
using Timer=System.Timers.Timer;

namespace UiComponents.SkinBase
{
  /// <summary>
  /// This model provides information about the screen saver and mouse controls state. It provides a copy of the
  /// <see cref="IScreenControl.IsScreenSaverActive"/> and <see cref="IScreenControl.IsMouseUsed"/> data, but as
  /// <see cref="Property"/> to enable the screen controls to bind to the data.
  /// </summary>
  public class ScreenSaverModel : IDisposable
  {
    public const string SCREENSAVER_MODEL_ID_STR = "D4B7FEDD-243F-4afc-A8BE-28BBBF17D799";

    protected Timer _timer = null;

    protected Property _isScreenSaverActiveProperty;
    protected Property _isMouseUsedProperty;

    public ScreenSaverModel()
    {
      _isScreenSaverActiveProperty = new Property(typeof(bool), false);
      _isMouseUsedProperty = new Property(typeof(bool), false);

      SubscribeToMessages();
    }

    public void Dispose()
    {
      StopListening();
      UnsubscribeFromMessages();
    }

    protected void SubscribeToMessages()
    {
      IMessageBroker broker = ServiceScope.Get<IMessageBroker>();

      ISystemStateService systemStateService = ServiceScope.Get<ISystemStateService>();
      if (systemStateService.CurrentState == SystemState.Started)
        StartListening();

      broker.GetOrCreate(SystemMessaging.QUEUE).MessageReceived += OnSystemMessageReceived;
    }

    protected void UnsubscribeFromMessages()
    {
      IMessageBroker broker = ServiceScope.Get<IMessageBroker>();
      broker.GetOrCreate(SystemMessaging.QUEUE).MessageReceived -= OnSystemMessageReceived;
    }

    protected void StartListening()
    {
      if (_timer != null)
        return;
      // Setup timer to update the properties
      _timer = new Timer(100);
      _timer.Elapsed += OnTimerElapsed;
      _timer.Enabled = true;
    }

    protected void StopListening()
    {
      if (_timer == null)
        return;
      _timer.Enabled = false;
      _timer.Elapsed -= OnTimerElapsed;
      _timer = null;
    }

    protected void OnSystemMessageReceived(QueueMessage message)
    {
      SystemMessaging.MessageType messageType =
          (SystemMessaging.MessageType) message.MessageData[SystemMessaging.MESSAGE_TYPE];
      if (messageType == SystemMessaging.MessageType.SystemStateChanged)
      {
        SystemState state = (SystemState) message.MessageData[SystemMessaging.PARAM];
        switch (state)
        {
          case SystemState.Started:
            StartListening();
            break;
          case SystemState.ShuttingDown:
            Dispose();
            break;
        }
      }
    }

    protected void OnTimerElapsed(object sender, ElapsedEventArgs e)
    {
      Update();
    }

    protected void Update()
    {
      IScreenControl screenControl = ServiceScope.Get<IScreenControl>();
      IInputManager inputManager = ServiceScope.Get<IInputManager>();
      IsScreenSaverActive = screenControl.IsScreenSaverActive;
      IsMouseUsed = inputManager.IsMouseUsed;
    }
  
    public Property IsScreenSaverActiveProperty
    {
      get { return _isScreenSaverActiveProperty; }
    }

    public bool IsScreenSaverActive
    {
      get { return (bool) _isScreenSaverActiveProperty.GetValue(); }
      internal set { _isScreenSaverActiveProperty.SetValue(value); }
    }

    public Property IsMouseUsedProperty
    {
      get { return _isMouseUsedProperty; }
    }

    public bool IsMouseUsed
    {
      get { return (bool) _isMouseUsedProperty.GetValue(); }
      internal set { _isMouseUsedProperty.SetValue(value); }
    }
  }
}
