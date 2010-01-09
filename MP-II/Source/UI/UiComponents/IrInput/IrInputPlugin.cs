#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Xml.Serialization;

using MediaPortal.Core;
using MediaPortal.UI.Control.InputManager;
using MediaPortal.Core.Logging;
using MediaPortal.Core.PluginManager;
using MediaPortal.Core.Settings;

using IrssComms;
using IrssUtils;

namespace UiComponents.IrInput
{
  /// <summary>
  /// MediaPortal Input Service plugin.
  /// </summary>
  // TODO: Tidy up code which was commented out. (Why was it commented out? Is it still needed?)
  public class IrInputPlugin : IPluginStateTracker
  {
    #region Variables

    protected ICollection<MappedKeyCode> _mappedKeyCodes;
    protected Client _client;

    protected IRServerInfo _irServerInfo;

    #endregion Variables

    void Start()
    {
      ServiceScope.Get<ILogger>().Info("IrInputPlugin: Startup");

      if (!StartClient())
        ServiceScope.Get<ILogger>().Error("IrInputPlugin: Failed to start local comms, input service is unavailable for this session");
      else
        ServiceScope.Get<ILogger>().Info("IrInputPlugin: Started");
    }

    void CommsFailure(object obj)
    {
      Exception ex = obj as Exception;

      if (ex != null)
        ServiceScope.Get<ILogger>().Error("IrInputPlugin: Communications failure: {0}", ex.Message);
      else
        ServiceScope.Get<ILogger>().Error("IrInputPlugin: Communications failure");

      StopClient();

      ServiceScope.Get<ILogger>().Warn("IrInputPlugin: Attempting communications restart ...");

      StartClient();
    }

    void Connected(object obj)
    {
      ServiceScope.Get<ILogger>().Info("IrInputPlugin: Connected to server");

      IrssMessage message = new IrssMessage(MessageType.RegisterClient, MessageFlags.Request);
      _client.Send(message);
    }

    void Disconnected(object obj)
    {
      ServiceScope.Get<ILogger>().Warn("IrInputPlugin: Communications with server has been lost");

      // FIXME Albert: Why Sleep(1000) here???
      Thread.Sleep(1000);
    }

    /// <summary>
    /// Connects the IR client to the host configured in the settings.
    /// </summary>
    /// <returns><c>true</c>, if the client could successfully be started, else <c>false</c>.</returns>
    public bool StartClient()
    {
      ISettingsManager settingsManager = ServiceScope.Get<ISettingsManager>();
      IrInputSettings settings = settingsManager.Load<IrInputSettings>();
      IPAddress serverIP = Client.GetIPFromName(settings.ServerHost);
      IPEndPoint endPoint = new IPEndPoint(serverIP, Server.DefaultPort);

      return StartClient(endPoint);
    }

    /// <summary>
    /// Connects the IR client to the host specified by the parameter <paramref name="endPoint"/>.
    /// </summary>
    /// <returns><c>true</c>, if the client could successfully be started, else <c>false</c>.</returns>
    public bool StartClient(IPEndPoint endPoint)
    {
      ServiceScope.Get<ILogger>().Info("IrInputPlugin: Connect to service ({0}:{1})", endPoint.Address, endPoint.Port);
      if (_client != null)
        return false;

      ClientMessageSink sink = ReceivedMessage;

      _client = new Client(endPoint, sink);
      _client.CommsFailureCallback = CommsFailure;
      _client.ConnectCallback = Connected;
      _client.DisconnectCallback = Disconnected;

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

    void ReceivedMessage(IrssMessage received)
    {
      ServiceScope.Get<ILogger>().Debug("IrInputPlugin: Received Message '{0}' {1}", received.Type, received.GetDataAsString());
      try
      {
        // FIXME Albert: See why some areas are commented out here
        // CHEFKOCH: IRserver is able to send (blast) ir commands
        switch (received.Type)
        {
          case MessageType.RemoteEvent:
            // RemoteHandler(received.GetDataAsString());
            // CHEFKOCH:
            // using the code from irss
            byte[] data = received.GetDataAsBytes();
            int deviceNameSize = BitConverter.ToInt32(data, 0);
            string deviceName = System.Text.Encoding.ASCII.GetString(data, 4, deviceNameSize);
            int keyCodeSize = BitConverter.ToInt32(data, 4 + deviceNameSize);
            string keyCode = System.Text.Encoding.ASCII.GetString(data, 8 + deviceNameSize, keyCodeSize);

            RemoteHandler(keyCode);
            break;
          /*
          case MessageType.BlastIR:
            if ((received.Flags & MessageFlags.Success) == MessageFlags.Success)
              ServiceScope.Get<ILogger>().Info("IrInputPlugin: Blast successful");
            else if ((received.Flags & MessageFlags.Failure) == MessageFlags.Failure)
              ServiceScope.Get<ILogger>().Warn("IrInputPlugin: Failed to blast IR command");
            break;
          */
          case MessageType.RegisterClient:
            if ((received.Flags & MessageFlags.Success) == MessageFlags.Success)
            {
              _irServerInfo = IRServerInfo.FromBytes(received.GetDataAsBytes());
              //_registered = true;

              ServiceScope.Get<ILogger>().Info("IrInputPlugin: Registered to Input Service '{0}'", _irServerInfo.ToString());
            }
            else if ((received.Flags & MessageFlags.Failure) == MessageFlags.Failure)
            {
              //_registered = false;
              ServiceScope.Get<ILogger>().Warn("IrInputPlugin: Input Service refused to register plugin");
            }
            break;

          /*
          case MessageType.LearnIR:
            if ((received.Flags & MessageFlags.Success) == MessageFlags.Success)
            {
              ServiceScope.Get<ILogger>().Info("IrInputPlugin: Learned IR Successfully");

              byte[] dataBytes = received.GetDataAsBytes();

              using (FileStream file = File.Create(_learnIRFilename))
                file.Write(dataBytes, 0, dataBytes.Length);
            }
            else if ((received.Flags & MessageFlags.Failure) == MessageFlags.Failure)
              ServiceScope.Get<ILogger>().Error("IrInputPlugin: Failed to learn IR command");
            else if ((received.Flags & MessageFlags.Timeout) == MessageFlags.Timeout)
              ServiceScope.Get<ILogger>().Error("IrInputPlugin: Learn IR command timed-out");

            _learnIRFilename = null;
            break;
          */
          case MessageType.ServerShutdown:
            ServiceScope.Get<ILogger>().Warn("IrInputPlugin: Input Service Shutdown - IrInputPlugin is disabled until Input Service returns");
            //_registered = false;
            break;

          case MessageType.Error:
            //_learnIRFilename = null;
            ServiceScope.Get<ILogger>().Error("IrInputPlugin: Received error: {0}", received.GetDataAsString());
            break;
        }

        //if (_handleMessage != null)
        //_handleMessage(received);
      }
      catch (Exception ex)
      {
        ServiceScope.Get<ILogger>().Error("IrInputPlugin - ReveivedMessage(): {0}", ex.Message);
      }
    }

    void RemoteHandler(string remoteButton)
    {
      IInputManager inputManager = ServiceScope.Get<IInputManager>();
      if (inputManager == null)
      {
        ServiceScope.Get<ILogger>().Error("IrInputPlugin: No Input Manager, can't map and act on '{0}'", remoteButton);
        return;
      }


      foreach (MappedKeyCode mapped in _mappedKeyCodes)
      {
        ServiceScope.Get<ILogger>().Debug("MappedKeyCode: Code '{0}' Key '{1}' Key_Name '{2}'", mapped.Code, mapped.Key, mapped.Key_Name);

        if (mapped.Code == remoteButton)
        {
          inputManager.KeyPress(mapped.Key);
          ServiceScope.Get<ILogger>().Debug("IrInputPlugin: Mapped Key '{0}' to '{1}'", remoteButton, mapped.Key);
          return;
        }
      }
      ServiceScope.Get<ILogger>().Warn("IrInputPlugin: No remote mapping found for key '{0}'", remoteButton);
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
      ISettingsManager settingsManager = ServiceScope.Get<ISettingsManager>();
      IrInputSettings settings = settingsManager.Load<IrInputSettings>();
      _mappedKeyCodes = settings.RemoteMap ??
          LoadRemoteMap(pluginRuntime.Metadata.GetAbsolutePath("DefaultRemoteMap.xml"));

      Thread startupThread = new Thread(Start);
      startupThread.IsBackground = true;
      startupThread.Priority = ThreadPriority.BelowNormal;
      startupThread.Name = "IrInputPlugin Startup";
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
