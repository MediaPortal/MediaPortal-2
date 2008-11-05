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
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Xml.Serialization;

using MediaPortal.Core;
using MediaPortal.Control.InputManager;
using MediaPortal.Core.Logging;
using MediaPortal.Core.PluginManager;
using MediaPortal.Core.Settings;

using IrssComms;
using IrssUtils;

namespace Components.Control.IrInput
{

  /// <summary>
  /// MediaPortal Input Service plugin.
  /// </summary>
  public class IrInputPlugin : IPluginStateTracker
  {

    #region IPlugin Members

    public void Initialise()
    {
    }

    #endregion

    #region IDisposable Members

    public void Dispose()
    {
    }

    #endregion

    #region IPluginStateTracker Members

    public void Activated()
    {
      Thread startupThread = new Thread(Start);
      startupThread.IsBackground = true;
      startupThread.Priority = ThreadPriority.BelowNormal;
      startupThread.Name = "IrInputPlugin Startup";
      startupThread.Start();
    }

    public bool RequestEnd()
    {
      return false; // FIXME: The IR plugin should be able to be disabled
    }

    public void Stop() { }

    public void Continue() { }

    public void Shutdown() { }

    #endregion

    #region Variables

    IrInputSettings _settings;
    Client _client;
    //string _learnIRFilename;
    //bool _registered;

    IRServerInfo _irServerInfo;

    #endregion Variables

    void Start()
    {
      System.Windows.Forms.Application.ApplicationExit += new EventHandler(Application_ApplicationExit);
      
      ServiceScope.Get<ILogger>().Info("IrInputPlugin: Startup");

      ISettingsManager settingsManager = ServiceScope.Get<ISettingsManager>(true);
      
      _settings = settingsManager.Load<IrInputSettings>();

      if (_settings.RemoteMap == null)
        _settings.RemoteMap = LoadRemoteMap("Plugins\\Control.IrInput\\DefaultRemoteMap.xml");

      ServiceScope.Get<ILogger>().Info("IrInputPlugin: Connect to service");

      IPAddress serverIP = Client.GetIPFromName(_settings.ServerHost);
      IPEndPoint endPoint = new IPEndPoint(serverIP, IrssComms.Server.DefaultPort);

      if (!StartClient(endPoint))
        ServiceScope.Get<ILogger>().Error("IrInputPlugin: Failed to start local comms, input service is unavailable for this session");
      else
        ServiceScope.Get<ILogger>().Info("IrInputPlugin: Started");
    }

    void Application_ApplicationExit(object sender, EventArgs e)
    {
      ServiceScope.Get<ILogger>().Info("IrInputPlugin: Stopped");

      StopClient();
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

      IPAddress serverIP = Client.GetIPFromName(_settings.ServerHost);
      IPEndPoint endPoint = new IPEndPoint(serverIP, IrssComms.Server.DefaultPort);

      StartClient(endPoint);
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

      Thread.Sleep(1000);
    }

    bool StartClient(IPEndPoint endPoint)
    {
      if (_client != null)
        return false;

      ClientMessageSink sink = new ClientMessageSink(ReceivedMessage);

      _client = new Client(endPoint, sink);
      _client.CommsFailureCallback = new WaitCallback(CommsFailure);
      _client.ConnectCallback = new WaitCallback(Connected);
      _client.DisconnectCallback = new WaitCallback(Disconnected);

      if (_client.Start())
      {
        return true;
      }
      else
      {
        _client = null;
        return false;
      }
    }

    void StopClient()
    {
      if (_client == null)
        return;

      _client.Dispose();
      _client = null;
    }

    void ReceivedMessage(IrssMessage received)
    {
      ServiceScope.Get<ILogger>().Debug("IrInputPlugin: Received Message \"{0}\" {1}", received.Type, received.GetDataAsString());

      try
      {
        switch (received.Type)
        {
          case MessageType.RemoteEvent:
            RemoteHandler(received.GetDataAsString());
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

              ServiceScope.Get<ILogger>().Info("IrInputPlugin: Registered to Input Service");
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
            {
              ServiceScope.Get<ILogger>().Error("IrInputPlugin: Failed to learn IR command");
            }
            else if ((received.Flags & MessageFlags.Timeout) == MessageFlags.Timeout)
            {
              ServiceScope.Get<ILogger>().Error("IrInputPlugin: Learn IR command timed-out");
            }

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
      IInputMapper inputMapper = ServiceScope.Get<IInputMapper>();
      if (inputMapper == null)
      {
        ServiceScope.Get<ILogger>().Error("IrInputPlugin: No Input Mapper, can't map \"{0}\"", remoteButton);
        return;
      }

      IInputManager inputManager = ServiceScope.Get<IInputManager>();
      if (inputManager == null)
      {
        ServiceScope.Get<ILogger>().Error("IrInputPlugin: No Input Manager, can't map and act on \"{0}\"", remoteButton);
        return;
      }

      bool alt = false;
      Key key = Key.None;

      foreach (MappedKeyCode mapped in _settings.RemoteMap)
      {
        if (mapped.Code == remoteButton)
        {
          key = inputMapper.MapSpecialKey(mapped.Key, alt);

          inputManager.KeyPress(key);

          ServiceScope.Get<ILogger>().Info("IrInputPlugin: Mapped \"{0}\" to \"{1}\"", remoteButton, mapped.Key);

          return;
        }
      }

      ServiceScope.Get<ILogger>().Warn("IrInputPlugin: No remote mapping found for \"{0}\"", remoteButton);
    }

    /// <summary>
    /// Loads a remote map.
    /// </summary>
    /// <param name="remoteFile">The remote file.</param>
    /// <returns>Remote map.</returns>
    List<MappedKeyCode> LoadRemoteMap(string remoteFile)
    {
      List<MappedKeyCode> remoteMap = new List<MappedKeyCode>();

      XmlSerializer reader = new XmlSerializer(typeof(List<MappedKeyCode>));
      using (StreamReader file = new StreamReader(remoteFile))
        remoteMap = (List<MappedKeyCode>) reader.Deserialize(file);

      return remoteMap;
    }

    /*
    void SaveRemoteMap(string remoteFile, List<MappedKeyCode> remoteMap)
    {
      XmlSerializer writer = new XmlSerializer(typeof(List<MappedKeyCode>));
      
      using (StreamWriter file = new StreamWriter(remoteFile))
        writer.Serialize(file, remoteMap);
    }
    */

  }

}
