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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Serialization;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.Settings;
using MediaPortal.Plugins.MceRemoteReceiver.Hardware;
using MediaPortal.UI.Control.InputManager;
using MediaPortal.UI.General;

namespace MediaPortal.Plugins.MceRemoteReceiver
{
  public class MceRemoteReceiver : IPluginStateTracker
  {
    #region Constants

    private const string LOG_PREFIX = "MceRemote:";

    #endregion

    #region Variables

    protected AsynchronousMessageQueue _messageQueue = null;
    protected IDictionary<int, Key> _mappedKeyCodes = null;
    protected ICollection<int> _unmappedKeyCodes = new HashSet<int>();

    #endregion

    private void StartReceiver()
    {
      try
      {
        // Register Device
        Remote.Click = null;
        Remote.Click += OnRemoteClick;
        Remote.DeviceRemoval += OnDeviceRemoval;
      }
      catch (Exception ex)
      {
        LogInfo("{0} - support disabled until MP restart", ex.InnerException.Message);
        return;
      }

      // Kill ehtray.exe since that program catches the MCE remote keys and would start MCE 2005
      Process[] myProcesses = Process.GetProcesses();
      foreach (Process myProcess in myProcesses.Where(p => p.ProcessName.ToLower().Equals("ehtray")))
      {
        try
        {
          LogInfo("Stopping Microsoft ehtray");
          myProcess.Kill();
        }
        catch (Exception)
        {
          LogInfo("Cannot stop Microsoft ehtray");
          Stop();
        }
      }
    }

    private void StopReceiver()
    {
      LogInfo("Stopping MCE remote");
      Remote.Click -= OnRemoteClick;
      Remote.DeviceRemoval -= OnDeviceRemoval;
      Remote.DeviceArrival -= OnDeviceArrival;
    }

    #region Message handling

    protected void SubscribeToMessages()
    {
      _messageQueue = new AsynchronousMessageQueue(this, new[] { WindowsMessaging.CHANNEL });
      _messageQueue.MessageReceived += OnMessageReceived;
      _messageQueue.Start();
    }

    protected virtual void UnsubscribeFromMessages()
    {
      if (_messageQueue == null)
        return;
      _messageQueue.Shutdown();
      _messageQueue = null;
    }

    void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == WindowsMessaging.CHANNEL)
      {
        WindowsMessaging.MessageType messageType = (WindowsMessaging.MessageType) message.MessageType;
        switch (messageType)
        {
          case WindowsMessaging.MessageType.WindowsBroadcast:
            Message msg = (Message) message.MessageData[WindowsMessaging.MESSAGE];
            HandleWindowsMessage(msg);
            break;
        }
      }
    }

    #endregion

    #region Remote event handling

    private void OnDeviceRemoval(object sender, EventArgs e)
    {
      Remote.DeviceRemoval -= OnDeviceRemoval;
      Remote.DeviceArrival += OnDeviceArrival;
      LogInfo("MCE receiver has been unplugged");
    }

    private void OnDeviceArrival(object sender, EventArgs e)
    {
      Remote.DeviceArrival -= OnDeviceArrival;
      Remote.Click -= OnRemoteClick;
      LogInfo("MCE receiver detected");
      StartReceiver();
    }

    /// <summary>
    /// Let everybody know that this HID message may not be handled by anyone else
    /// </summary>
    /// <param name="msg">System.Windows.Forms.Message</param>
    /// <returns>Command handled</returns>
    private bool HandleWindowsMessage(Message msg)
    {
      // Check if Message is a HID input message
      if (msg.Msg != 0x0319)
        return false;

      int command = (msg.LParam.ToInt32() >> 16) & ~0xF000;
      InputDevices.LastHidRequest = (AppCommands) command;

      RemoteButton remoteButton = RemoteButton.None;

      if ((AppCommands) command == AppCommands.VolumeUp)
        remoteButton = RemoteButton.VolumeUp;

      if ((AppCommands) command == AppCommands.VolumeDown)
        remoteButton = RemoteButton.VolumeDown;

      if (remoteButton != RemoteButton.None)
        RemoteHandler((int) remoteButton);

      return true;
    }

    /// <summary>
    /// Evaluate button press from remote
    /// </summary>
    /// <param name="sender">Sender</param>
    /// <param name="e">Arguments</param>
    private void OnRemoteClick(object sender, RemoteEventArgs e)
    {
      RemoteButton remoteButton = e.Button;

      // Set LastHidRequest, otherwise the HID handler (if enabled) would react on some remote buttons (double execution of command)
      switch (remoteButton)
      {
        case RemoteButton.Record:
          InputDevices.LastHidRequest = AppCommands.MediaRecord;
          break;
        case RemoteButton.Stop:
          InputDevices.LastHidRequest = AppCommands.MediaStop;
          break;
        case RemoteButton.Pause:
          InputDevices.LastHidRequest = AppCommands.MediaPause;
          break;
        case RemoteButton.Rewind:
          InputDevices.LastHidRequest = AppCommands.MediaRewind;
          break;
        case RemoteButton.Play:
          InputDevices.LastHidRequest = AppCommands.MediaPlay;
          break;
        case RemoteButton.Forward:
          InputDevices.LastHidRequest = AppCommands.MediaFastForward;
          break;
        case RemoteButton.Replay:
          InputDevices.LastHidRequest = AppCommands.MediaPreviousTrack;
          break;
        case RemoteButton.Skip:
          InputDevices.LastHidRequest = AppCommands.MediaNextTrack;
          break;
        case RemoteButton.Back:
          InputDevices.LastHidRequest = AppCommands.BrowserBackward;
          break;
        case RemoteButton.ChannelUp:
          InputDevices.LastHidRequest = AppCommands.MediaChannelUp;
          break;
        case RemoteButton.ChannelDown:
          InputDevices.LastHidRequest = AppCommands.MediaChannelDown;
          break;
        case RemoteButton.Mute:
          InputDevices.LastHidRequest = AppCommands.VolumeMute;
          break;
        case RemoteButton.VolumeUp:
          return; // Don't handle this command, benefit from OS' repeat handling instead
        case RemoteButton.VolumeDown:
          return; // Don't handle this command, benefit from OS' repeat handling instead
      }

      // Get & execute Mapping
      RemoteHandler((int) remoteButton);
    }

    private void RemoteHandler(int remoteButton)
    {
      IInputManager inputManager = ServiceRegistration.Get<IInputManager>();
      if (inputManager == null)
      {
        LogError("No Input Manager, can't map and act on '{0}'", remoteButton);
        return;
      }

      Key key;
      if (_mappedKeyCodes.TryGetValue(remoteButton, out key))
      {
        inputManager.KeyPress(key);
        LogDebug("Mapped Key '{0}' to '{1}'", remoteButton, key);
      }
      else
        LogUnmappedButtonOnce(remoteButton);
    }

    /// <summary>
    /// Writes log message for every distinct <paramref name="remoteButton"/> which is not mapped.
    /// This avoids writing many single lines for same button.
    /// </summary>
    /// <param name="remoteButton">remoteButton</param>
    private void LogUnmappedButtonOnce(int remoteButton)
    {
      if (_unmappedKeyCodes.Contains(remoteButton))
        return;
      _unmappedKeyCodes.Add(remoteButton);
      LogInfo("No remote mapping found for remote button '{0}'", remoteButton);
    }

    #endregion

    protected static ICollection<MappedKeyCode> LoadRemoteMap(string remoteFile)
    {
      XmlSerializer reader = new XmlSerializer(typeof(List<MappedKeyCode>));
      using (StreamReader file = new StreamReader(remoteFile))
        return (ICollection<MappedKeyCode>) reader.Deserialize(file);
    }

    protected static void SaveRemoteMap(string remoteFile, ICollection<MappedKeyCode> remoteMap)
    {
      XmlSerializer writer = new XmlSerializer(typeof(List<MappedKeyCode>));
      using (StreamWriter file = new StreamWriter(remoteFile))
        writer.Serialize(file, remoteMap);
    }

    protected static ICollection<eHomeTransceiver> LoadTransceivers(string remoteFile)
    {
      XmlSerializer reader = new XmlSerializer(typeof(List<eHomeTransceiver>));
      using (StreamReader file = new StreamReader(remoteFile))
        return (ICollection<eHomeTransceiver>) reader.Deserialize(file);
    }

    protected static void SaveTransceivers(string remoteFile, ICollection<eHomeTransceiver> remoteMap)
    {
      XmlSerializer writer = new XmlSerializer(typeof(List<eHomeTransceiver>));
      using (StreamWriter file = new StreamWriter(remoteFile))
        writer.Serialize(file, remoteMap);
    }

    #region Logging

    private static string FormatPrefix(string format)
    {
      return string.Format("{0} {1}", LOG_PREFIX, format);
    }

    public static void LogInfo(string format, params object[] args)
    {
      ServiceRegistration.Get<ILogger>().Info(FormatPrefix(format), args);
    }

    public static void LogWarn(string format, params object[] args)
    {
      ServiceRegistration.Get<ILogger>().Warn(FormatPrefix(format), args);
    }

    public static void LogDebug(string format, params object[] args)
    {
      ServiceRegistration.Get<ILogger>().Debug(FormatPrefix(format), args);
    }

    public static void LogError(string format, params object[] args)
    {
      ServiceRegistration.Get<ILogger>().Error(FormatPrefix(format), args);
    }

    #endregion

    #region Implementation of IPluginStateTracker

    /// <summary>
    /// Will be called when the plugin is started. This will happen as a result of a plugin auto-start
    /// or an item access which makes the plugin active.
    /// This method is called after the plugin's state was set to <see cref="PluginState.Active"/>.
    /// </summary>
    public void Activated(PluginRuntime pluginRuntime)
    {
      ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>();
      MceRemoteSettings settings = settingsManager.Load<MceRemoteSettings>();
      // We initialize the key code map here instead of in the constructor because here, we have access to the plugin's
      // directory (via the pluginRuntime parameter).
      _mappedKeyCodes = new Dictionary<int, Key>();
      ICollection<MappedKeyCode> keyCodes = settings.RemoteMap ?? LoadRemoteMap(pluginRuntime.Metadata.GetAbsolutePath("DefaultRemoteMap.xml"));
      foreach (MappedKeyCode mkc in keyCodes)
        _mappedKeyCodes.Add(mkc.Code, mkc.Key);

      //_eHomeTransceivers.Add(new eHomeTransceiver() { DeviceID = "testKey", Name = "testValue" });
      //SaveTransceivers(pluginRuntime.Metadata.GetAbsolutePath("eHomeTransceiverList.xml"), _eHomeTransceivers);
      ICollection<eHomeTransceiver> transceivers = settings.Transceivers ?? LoadTransceivers(pluginRuntime.Metadata.GetAbsolutePath("eHomeTransceiverList.xml"));
      if (transceivers.Count > 0)
        Remote.Transceivers.AddRange(transceivers);

      StartReceiver();
    }

    /// <summary>
    /// Schedules the stopping of this plugin. This method returns the information
    /// if this plugin can be stopped. Before this method is called, the plugin's state
    /// will be changed to <see cref="PluginState.EndRequest"/>.
    /// </summary>
    /// <remarks>
    /// This method is part of the first phase in the two-phase stop procedure.
    /// After this method returns <c>true</c> and all item's clients also return <c>true</c>
    /// as a result of their stop request, the plugin's state will change to
    /// <see cref="PluginState.Stopping"/>, then all uses of items by clients will be canceled,
    /// then this plugin will be stopped by a call to method <see cref="IPluginStateTracker.Stop"/>.
    /// If either this method returns <c>false</c> or one of the items clients prevent
    /// the stopping, the plugin will continue to be active and the method <see cref="IPluginStateTracker.Continue"/>
    /// will be called.
    /// </remarks>
    /// <returns><c>true</c>, if this plugin can be stopped at this time, else <c>false</c>.
    /// </returns>
    public bool RequestEnd()
    {
      return true;
    }

    /// <summary>
    /// Second step of the two-phase stopping procedure. This method stops this plugin,
    /// i.e. removes the integration of this plugin into the system, which was triggered
    /// by the <see cref="IPluginStateTracker.Activated"/> method.
    /// </summary>
    public void Stop()
    {
      StopReceiver();
    }

    /// <summary>
    /// Revokes the end request which was triggered by a former call to the
    /// <see cref="IPluginStateTracker.RequestEnd"/> method and restores the active state. After this call, the plugin remains active as
    /// it was before the call of <see cref="IPluginStateTracker.RequestEnd"/> method.
    /// </summary>
    public void Continue()
    {
    }

    /// <summary>
    /// Will be called before the plugin manager shuts down. The plugin can perform finalization
    /// tasks here. This method will called independently from the plugin state, i.e. it will also be called when the plugin
    /// was disabled or not started at all.
    /// </summary>
    public void Shutdown()
    {
      StopReceiver();
    }

    #endregion
  }
}
