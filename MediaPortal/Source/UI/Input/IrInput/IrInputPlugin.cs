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
using System.IO;
using System.Net;
using System.Threading;
using System.Xml.Serialization;

using MediaPortal.Common;
using MediaPortal.UI.Control.InputManager;
using MediaPortal.Common.Logging;
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.Settings;

using IrssComms;
using IrssUtils;

namespace MediaPortal.UiComponents.IrInput
{
  /// <summary>
  /// Input Service plugin. Communicates with an IR Server Suite server.
  /// </summary>
  public class IrInputPlugin : IPluginStateTracker
  {
    #region Protected fields

    protected Client _client = null;
    protected IRServerInfo _irServerInfo = null;

    protected IDictionary<string, Key> _mappedKeyCodes = null;

    #endregion

    #region Event handlers

    void Startup()
    {
      if (StartClient())
        ServiceRegistration.Get<ILogger>().Info("IrInputPlugin: Started");
      else
        ServiceRegistration.Get<ILogger>().Error("IrInputPlugin: Failed to start local comms, input service is unavailable for this session");
    }

    void CommsFailure(object obj)
    {
      Exception ex = obj as Exception;

      if (ex != null)
        ServiceRegistration.Get<ILogger>().Error("IrInputPlugin: Communications failure", ex);
      else
        ServiceRegistration.Get<ILogger>().Error("IrInputPlugin: Communications failure");

      StopClient();

      ServiceRegistration.Get<ILogger>().Warn("IrInputPlugin: Attempting communications restart ...");

      StartClient();
    }

    void Connected(object obj)
    {
      ServiceRegistration.Get<ILogger>().Info("IrInputPlugin: Connected to server");

      IrssMessage message = new IrssMessage(MessageType.RegisterClient, MessageFlags.Request);
      _client.Send(message);
    }

    void Disconnected(object obj)
    {
      ServiceRegistration.Get<ILogger>().Warn("IrInputPlugin: Communications with server has been lost");
    }

    void ReceivedMessage(IrssMessage received)
    {
      ServiceRegistration.Get<ILogger>().Debug("IrInputPlugin: Received Message '{0}' {1}", received.Type, received.GetDataAsString());
      try
      {
        switch (received.Type)
        {
          case MessageType.RemoteEvent:
            string keyCode = received.MessageData[IrssMessage.KEY_CODE] as string;
            RemoteHandler(keyCode);
            break;

          // TODO: What to do with this code?
          /*
          case MessageType.BlastIR:
            if ((received.Flags & MessageFlags.Success) == MessageFlags.Success)
              ServiceRegistration.Get<ILogger>().Info("IrInputPlugin: Blast successful");
            else if ((received.Flags & MessageFlags.Failure) == MessageFlags.Failure)
              ServiceRegistration.Get<ILogger>().Warn("IrInputPlugin: Failed to blast IR command");
            break;
          */
          case MessageType.RegisterClient:
            if ((received.Flags & MessageFlags.Success) == MessageFlags.Success)
            {
              _irServerInfo = IRServerInfo.FromBytes(received.GetDataAsBytes());

              ServiceRegistration.Get<ILogger>().Info("IrInputPlugin: Registered to Input Service '{0}'", _irServerInfo);
            }
            else if ((received.Flags & MessageFlags.Failure) == MessageFlags.Failure)
              ServiceRegistration.Get<ILogger>().Warn("IrInputPlugin: Input Service refused to register plugin");
            break;

          // TODO: What to do with this code?
          /*
          case MessageType.LearnIR:
            if ((received.Flags & MessageFlags.Success) == MessageFlags.Success)
            {
              ServiceRegistration.Get<ILogger>().Info("IrInputPlugin: Learned IR Successfully");

              byte[] dataBytes = received.GetDataAsBytes();

              using (FileStream file = File.Create(_learnIRFilename))
                file.Write(dataBytes, 0, dataBytes.Length);
            }
            else if ((received.Flags & MessageFlags.Failure) == MessageFlags.Failure)
              ServiceRegistration.Get<ILogger>().Error("IrInputPlugin: Failed to learn IR command");
            else if ((received.Flags & MessageFlags.Timeout) == MessageFlags.Timeout)
              ServiceRegistration.Get<ILogger>().Error("IrInputPlugin: Learn IR command timed-out");
            break;
          */
          case MessageType.ServerShutdown:
            ServiceRegistration.Get<ILogger>().Warn("IrInputPlugin: Input Service shutdown - IrInputPlugin is disabled until Input Service returns");
            break;

          case MessageType.Error:
            ServiceRegistration.Get<ILogger>().Error("IrInputPlugin: Received error '{0}'", received.GetDataAsString());
            break;
        }
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("Problem receiving IR message: {0}", ex);
      }
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Connects the IR client to the host configured in the settings.
    /// </summary>
    /// <returns><c>true</c>, if the client could successfully be started, else <c>false</c>.</returns>
    public bool StartClient()
    {
      ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>();
      IrInputSettings settings = settingsManager.Load<IrInputSettings>();
      IPAddress serverIP = Network.GetIPFromName(settings.ServerHost);
      IPEndPoint endPoint = new IPEndPoint(serverIP, Server.DefaultPort);

      return StartClient(endPoint);
    }

    /// <summary>
    /// Connects the IR client to the host specified by the parameter <paramref name="endPoint"/>.
    /// </summary>
    /// <returns><c>true</c>, if the client could successfully be started, else <c>false</c>.</returns>
    public bool StartClient(IPEndPoint endPoint)
    {
      ServiceRegistration.Get<ILogger>().Info("IrInputPlugin: Connect to service ({0}:{1})", endPoint.Address, endPoint.Port);
      if (_client != null)
        return false;

      ClientMessageSink sink = ReceivedMessage;

      _client = new Client(endPoint, sink)
        {
            CommsFailureCallback = CommsFailure,
            ConnectCallback = Connected,
            DisconnectCallback = Disconnected
        };

      if (_client.Start())
        return true;
      _client = null;
      return false;
    }

    /// <summary>
    /// Stops the IR client.
    /// </summary>
    public void StopClient()
    {
      if (_client == null)
        return;

      _client.Dispose();
      _client = null;
    }

    #endregion

    void RemoteHandler(string remoteButton)
    {
      IInputManager inputManager = ServiceRegistration.Get<IInputManager>();
      if (inputManager == null)
      {
        ServiceRegistration.Get<ILogger>().Error("IrInputPlugin: No Input Manager, can't map and act on '{0}'", remoteButton);
        return;
      }

      Key key;
      if (_mappedKeyCodes.TryGetValue(remoteButton, out key))
      {
        inputManager.KeyPress(key);
        ServiceRegistration.Get<ILogger>().Debug("IrInputPlugin: Mapped Key '{0}' to '{1}'", remoteButton, key);
      }
      else
        ServiceRegistration.Get<ILogger>().Warn("IrInputPlugin: No remote mapping found for remote button '{0}'", remoteButton);
    }

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

    #region IPluginStateTracker implementation

    public void Activated(PluginRuntime pluginRuntime)
    {
      ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>();
      // We initialize the key code map here instead of in the constructor because here, we have access to the plugin's
      // directory (via the pluginRuntime parameter).
      IrInputSettings settings = settingsManager.Load<IrInputSettings>();
      _mappedKeyCodes = new Dictionary<string, Key>();
      ICollection<MappedKeyCode> keyCodes = settings.RemoteMap ??
          LoadRemoteMap(pluginRuntime.Metadata.GetAbsolutePath("DefaultRemoteMap.xml"));
      foreach (MappedKeyCode mkc in keyCodes)
        _mappedKeyCodes.Add(mkc.Code, mkc.Key);

      Thread startupThread = new Thread(Startup)
        {
            IsBackground = true,
            Priority = ThreadPriority.BelowNormal,
            Name = "IrInput"
        };
      startupThread.Start();
    }

    public bool RequestEnd()
    {
      return true;
    }

    public void Stop()
    {
      StopClient();
    }

    public void Continue() { }

    public void Shutdown()
    {
      StopClient();
    }

    #endregion
  }
}
