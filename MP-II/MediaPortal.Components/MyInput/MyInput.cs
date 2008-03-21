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
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Serialization;

using MediaPortal.Core;
using MediaPortal.Control.InputManager;
using MediaPortal.Core.Logging;
using MediaPortal.Core.PluginManager;
using MediaPortal.Core.Settings;

using IrssComms;
using IrssUtils;

namespace MediaPortal.MyInput
{

  /// <summary>
  /// MediaPortal Input Service plugin.
  /// </summary>
  public class MyInput : IPlugin, IAutoStart
  {

    #region IPlugin Members

    public void Initialize(string id)
    {
    }

    #endregion

    #region IDisposable Members

    public void Dispose()
    {
    }

    #endregion

    #region IAutoStart Members

    public void Startup()
    {
      Thread startupThread = new Thread(new ThreadStart(Start));
      startupThread.IsBackground = true;
      startupThread.Priority = ThreadPriority.BelowNormal;
      startupThread.Name = "My Input Startup";
      startupThread.Start();
    }

    #endregion

    #region Variables

    MyInputSettings _settings;
    Client _client;
    //string _learnIRFilename;
    //bool _registered;

    IRServerInfo _irServerInfo;

    #endregion Variables

    void Start()
    {
      System.Windows.Forms.Application.ApplicationExit += new EventHandler(Application_ApplicationExit);
      ILogger logger = ServiceScope.Get<ILogger>(false);
      logger.Info("MyInput: Startup");

      ISettingsManager settingsManager = ServiceScope.Get<ISettingsManager>(true);
      
      _settings = new MyInputSettings();
      settingsManager.Load(_settings);

      if (_settings.RemoteMap == null)
        _settings.RemoteMap = new List<MappedKeyCode>();

      logger.Info("MyInput: Connect to service");
      IPAddress serverIP = Client.GetIPFromName(_settings.ServerHost);
      IPEndPoint endPoint = new IPEndPoint(serverIP, IrssComms.Server.DefaultPort);

      if (!StartClient(endPoint))
      {
        if (logger != null)
          logger.Error("MyInput: Failed to start local comms, input service is unavailable for this session");
      }
      else
      {
        if (logger != null)
          logger.Info("MyInput: Started");
      }
    }

    void Application_ApplicationExit(object sender, EventArgs e)
    {
      ILogger logger = ServiceScope.Get<ILogger>(false);
      logger.Info("MyInput: Stopped");
      StopClient();
    }


    void CommsFailure(object obj)
    {
      Exception ex = obj as Exception;

      ILogger logger = ServiceScope.Get<ILogger>(false);
      if (logger != null)
      {
        if (ex != null)
          logger.Error("MyInput: Communications failure: {0}", ex.Message);
        else
          logger.Error("MyInput: Communications failure");
      }
      StopClient();

      if (logger != null)
      {
        logger.Warn("MyInput: Attempting communications restart ...");
      }
      IPAddress serverIP = Client.GetIPFromName(_settings.ServerHost);
      IPEndPoint endPoint = new IPEndPoint(serverIP, IrssComms.Server.DefaultPort);

      StartClient(endPoint);
    }

    void Connected(object obj)
    {
      ILogger logger = ServiceScope.Get<ILogger>();

      if (logger != null)
        logger.Info("MyInput: Connected to server");

      IrssMessage message = new IrssMessage(MessageType.RegisterClient, MessageFlags.Request);
      _client.Send(message);
    }
    void Disconnected(object obj)
    {
      ILogger logger = ServiceScope.Get<ILogger>();

      if (logger != null)
        logger.Warn("MyInput: Communications with server has been lost");

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
      ILogger logger = ServiceScope.Get<ILogger>();

      if (logger != null)
        logger.Debug("MyInput: Received Message \"{0}\" {1}", received.Type, received.GetDataAsString());

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
              ServiceScope.Get<ILogger>().Info("MyInput: Blast successful");
            else if ((received.Flags & MessageFlags.Failure) == MessageFlags.Failure)
              ServiceScope.Get<ILogger>().Warn("MyInput: Failed to blast IR command");
            break;
          */
          case MessageType.RegisterClient:
            if ((received.Flags & MessageFlags.Success) == MessageFlags.Success)
            {
              _irServerInfo = IRServerInfo.FromBytes(received.GetDataAsBytes());
              //_registered = true;

              if (_settings.FirstRun)
              {
                IrssMessage requestActiveReceivers = new IrssMessage(MessageType.ActiveReceivers, MessageFlags.Request);
                _client.Send(requestActiveReceivers);
              }

              if (logger != null)
                logger.Info("MyInput: Registered to Input Service");
            }
            else if ((received.Flags & MessageFlags.Failure) == MessageFlags.Failure)
            {
              //_registered = false;
              if (logger != null)
                logger.Warn("MyInput: Input Service refused to register plugin");
            }
            break;

          // When the plugin receives the list of active receivers it tries to load default
          case MessageType.ActiveReceivers:
            if ((received.Flags & MessageFlags.Response) == MessageFlags.Response)
            {
              if (!_settings.FirstRun)
                break;

              string[] activeReceivers = received.GetDataAsString().Split(new char[] { ',' });

              foreach (string receiver in activeReceivers)
              {
                // Load default mappings for active receivers...

                string fileName = String.Format("Plugins\\MyInput\\{0}.xml", receiver);
                if (File.Exists(fileName))
                {
                  List<MappedKeyCode> newMap = LoadRemoteMap(fileName);
                  _settings.RemoteMap.AddRange(newMap);
                }
              }

              _settings.FirstRun = false;

              ISettingsManager settingsManager = ServiceScope.Get<ISettingsManager>(true);

              if (settingsManager != null)
                settingsManager.Save(_settings);
            }
            break;
          /*
          case MessageType.LearnIR:
            if ((received.Flags & MessageFlags.Success) == MessageFlags.Success)
            {
              if (logger != null)
                logger.Info("MyInput: Learned IR Successfully");

              byte[] dataBytes = received.GetDataAsBytes();

              using (FileStream file = File.Create(_learnIRFilename))
                file.Write(dataBytes, 0, dataBytes.Length);
            }
            else if ((received.Flags & MessageFlags.Failure) == MessageFlags.Failure)
            {
              if (logger != null)
                logger.Error("MyInput: Failed to learn IR command");
            }
            else if ((received.Flags & MessageFlags.Timeout) == MessageFlags.Timeout)
            {
              if (logger != null)
                logger.Error("MyInput: Learn IR command timed-out");
            }

            _learnIRFilename = null;
            break;
          */
          case MessageType.ServerShutdown:
            if (logger != null)
              logger.Warn("MyInput: Input Service Shutdown - MyInput is disabled until Input Service returns");
            //_registered = false;
            break;

          case MessageType.Error:
            //_learnIRFilename = null;
            if (logger != null)
              logger.Error("MyInput: Received error: {0}", received.GetDataAsString());
            break;
        }

        //if (_handleMessage != null)
        //_handleMessage(received);
      }
      catch (Exception ex)
      {
        if (logger != null)
          logger.Error("MyInput - ReveivedMessage(): {0}", ex.Message);
        //else
          //throw ex;
      }
    }

    void RemoteHandler(string remoteButton)
    {
      ILogger logger = ServiceScope.Get<ILogger>();
      
      IInputMapper inputMapper = ServiceScope.Get<IInputMapper>();
      if (inputMapper == null)
      {
        if (logger != null)
          logger.Error("MyInput: No Input Mapper, can't map \"{0}\"", remoteButton);

        return;
      }

      IInputManager inputManager = ServiceScope.Get<IInputManager>();
      if (inputManager == null)
      {
        if (logger != null)
          logger.Error("MyInput: No Input Manager, can't map and act on \"{0}\"", remoteButton);

        return;
      }

      bool alt = false;
      Key key = Key.None;

      foreach (MappedKeyCode mapped in _settings.RemoteMap)
      {
        if (mapped.Code == remoteButton)
        {
          key = inputMapper.Map(mapped.Key, alt);

          inputManager.KeyPressed(key);

          if (logger != null)
            logger.Info("MyInput: Mapped \"{0}\" to \"{1}\"", remoteButton, mapped.Key);

          return;
        }
      }

      if (logger != null)
        logger.Warn("MyInput: No remote mapping found for \"{0}\"", remoteButton);
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

    void SaveRemoteMap(string remoteFile, List<MappedKeyCode> remoteMap)
    {
      XmlSerializer writer = new XmlSerializer(typeof(List<MappedKeyCode>));
      
      using (StreamWriter file = new StreamWriter(remoteFile))
        writer.Serialize(file, remoteMap);
    }

  }

}
