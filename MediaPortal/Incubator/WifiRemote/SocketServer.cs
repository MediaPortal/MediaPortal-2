﻿#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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
using System.Text;
using System.Threading.Tasks;
using Deusty.Net;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.SystemResolver;
using MediaPortal.Common.UserManagement;
using MediaPortal.Common.UserProfileDataManagement;
using MediaPortal.Plugins.WifiRemote.MessageParser;
using MediaPortal.Plugins.WifiRemote.Messages;
using MediaPortal.Plugins.WifiRemote.SendMessages;
using Newtonsoft.Json.Linq;

namespace MediaPortal.Plugins.WifiRemote
{
  internal class SocketServer
  {
    // SocketServer
    private readonly UInt16 _port;
    private bool _isStarted = false;
    private AsyncSocket _listenSocket;
    private AuthMethod _allowedAuth;
    private List<AutoLoginToken> _loginTokens;
    private Dictionary<AsyncSocket, int> _socketsWaitingForScreenshot;

    public List<AsyncSocket> connectedSockets;

    private static SocketServer _instance = null;
    public static SocketServer Instance { get { return _instance; } }
    
    // This function specifies all the different Message Types and Maps the processing function to it.
    private readonly Dictionary<string, Func<JObject, SocketServer, AsyncSocket, Task<bool>>> MessageType = new Dictionary<string, Func<JObject, SocketServer, AsyncSocket, Task<bool>>>()
    {
      { "command", ParserCommand.ParseAsync },
      { "key", ParserKey.ParseAsync },
      { "commandstartrepeat", ParserCommand.ParseCommandStartRepeatAsync },
      { "commandstoprepeat", ParserCommand.ParseCommandStopRepeatAsync },
      /*{ "window", new Func<int, int, int>(Func1) },
      { "activatewindow", new Func<int, int, int>(Func1) },
      { "dialog", new Func<int, int, int>(Func1) },
      { "facade", new Func<int, int, int>(Func1) },*/
      { "powermode", ParserPowermode.ParseAsync },
      { "volume", ParserVolume.ParseAsync },
      { "position", ParserPosition.ParseAsync },
      { "playfile", ParserPlayFile.ParseAsync },
      { "playchannel", ParserPlaychannel.ParseAsync },
      { "playradiochannel", ParserPlaychannel.ParseAsync },  // should be the same as playchannel in MP2
      { "plugins", ParserPlugins.ParseAsync },
      //{ "properties", ParserProperties.ParseAsync },
      { "image", ParserImage.ParseAsync },
      { "images", ParserImages.ParseAsync },
      { "music", ParserMusic.ParseAsync },
      { "screenshot", ParserScreenshot.ParseAsync },
      { "playlist", ParserPlaylist.ParseAsync },
      { "requeststatus", ParserRequeststatus.ParseAsync },
      { "requestnowplaying", ParserRequestnowplaying.ParseAsync },
      { "movies", ParserMovingpictures.ParseAsync },
      { "movingpictures", ParserMovingpictures.ParseAsync },
      { "series", ParserTVSeries.ParseAsync },
      { "tvseries", ParserTVSeries.ParseAsync  },
      { "videos", ParserVideos.ParseAsync },
      //{ "message", new Func<int, int, int>(Func1) },
      //{ "showdialog", new Func<int, int, int>(Func1) },
      { "recordings", ParserRecordings.ParseAsync },
      { "tv", (msg, svr, sender) => ParserChannels.ParseAsync(msg, svr, sender, true) },
      { "radio", (msg, svr, sender) => ParserChannels.ParseAsync(msg, svr, sender, false) },
      { "schedules", ParserSchedules.ParseAsync },
    };   

    private readonly MessageWelcome _welcomeMessage;

    /// <summary>
    /// Passcode for client authentification
    /// </summary>
    internal String PassCode { get; set; }

    /// <summary>
    /// Time in minutes that an authenticated client is
    /// able to send commands without authenticating again.
    /// 
    /// 0 to disable autologin.
    /// </summary>
    internal int AutologinTimeout { get; set; }

    /// <summary>
    /// Passcode for client authentification
    /// </summary>
    internal AuthMethod AllowedAuth
    {
      get
      {
        return _allowedAuth;
      }

      set
      {
        _allowedAuth = value;
        _welcomeMessage.AuthMethod = _allowedAuth;
      }
    }

    /// <summary>
    /// Display Notifications when clients connect/disconnect
    /// </summary>
    internal bool ShowNotifications { get; set; }
    
    /// <summary>
    /// Constructor.
    /// Initialise and setup the socket server.
    /// </summary>
    public SocketServer(UInt16 port)
    {
      _welcomeMessage = new MessageWelcome();
      _port = port;
      _instance = this;

      InitSocket();
    }

    /// <summary>
    /// Initialise the socket
    /// </summary>
    private void InitSocket()
    {
      _listenSocket = new AsyncSocket { AllowMultithreadedCallbacks = true };

      // Tell AsyncSocket to allow multi-threaded delegate methods

      // Register for client connect event
      _listenSocket.DidAccept += ListenSocket_DidAccept;

      // Initialize list to hold connected sockets
      connectedSockets = new List<AsyncSocket>();
    }

    /// <summary>
    /// Start listening for incoming connections.
    /// </summary>
    public void Start()
    {
      // Abort if already started
      if (_isStarted)
      {
        Logger.Debug("WifiRemote: ListenSocket already accepting connections, aborting start ...");
        return;
      }

      if (_listenSocket == null)
      {
        InitSocket();
      }

      Exception error;
      if (!_listenSocket.Accept(_port, out error))
      {
        Logger.Error("WifiRemote: Error starting server: " + error.Message);
        return;
      }

      _isStarted = true;
      _loginTokens = new List<AutoLoginToken>();
      Logger.Info("WifiRemote: Now accepting connections");
    }

    /// <summary>
    /// Stop the server and disconnect all clients.
    /// </summary>
    public void Stop()
    {
      if (!_isStarted)
      {
        Logger.Debug("WifiRemote: ListenSocket already stopped, ignoring stop command");
        return;
      }

      // Stop accepting connections
      _listenSocket.Close();

      // Stop any client connections
      lock (connectedSockets)
      {
        foreach (AsyncSocket socket in connectedSockets)
        {
          //socket.CloseAfterReading();
          socket.Close();
        }
      }

      _isStarted = false;
      _listenSocket = null;

      Logger.Info("WifiRemote: Server stopped");
    }

    /// <summary>
    /// A client connected.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="newSocket"></param>
    private void ListenSocket_DidAccept(AsyncSocket sender, AsyncSocket newSocket)
    {
      // Subsribe to worker socket events
      newSocket.DidRead += NewSocket_DidRead;
      newSocket.DidWrite += NewSocket_DidWrite;
      newSocket.WillClose += NewSocket_WillClose;
      newSocket.DidClose += NewSocket_DidClose;

      newSocket.SetRemoteClient(new RemoteClient());

      // Store worker socket in client list
      lock (connectedSockets)
      {
        connectedSockets.Add(newSocket);
      }

      // Send welcome message to client
      Logger.Debug("WifiRemote: Client connected, sending welcome msg.");
      SendMessageToClient.Send(_welcomeMessage, newSocket, true);
    }

    /// <summary>
    /// A client closed the connection.
    /// </summary>
    /// <param name="sender"></param>
    private void NewSocket_DidClose(AsyncSocket sender)
    {
      // Remove the client from the client list.
      lock (connectedSockets)
      {
        Logger.Info("WifiRemote: Removing client " + sender.GetRemoteClient().ClientName + " from connected sockets");
        connectedSockets.Remove(sender);
      }
    }

    /// <summary>
    /// A client will disconnect.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void NewSocket_WillClose(AsyncSocket sender, Exception e)
    {
      Logger.Debug("WifiRemote: A client is about to disconnect.");
    }

    /// <summary>
    /// The client sent a message
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="tag"></param>
    private void NewSocket_DidWrite(AsyncSocket sender, long tag)
    {
      sender.Read(AsyncSocket.CRLFData, -1, 0);
    }

    /// <summary>
    /// Read a message from the client.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="data"></param>
    /// <param name="tag"></param>
    private void NewSocket_DidRead(AsyncSocket sender, byte[] data, long tag)
    {
      string msg = null;

      try
      {
        msg = Encoding.UTF8.GetString(data);

        //comment this out to log all received commands
        //Logger.Debug("WifiRemote: " + msg);

        // Get json object
        JObject message = JObject.Parse(msg);
        string type = (string)message["Type"];
        RemoteClient client = sender.GetRemoteClient();

        // Autologin handling
        // Has to be activated in WifiRemote configuration.
        string clientKey = (string)message["AutologinKey"];

        // Key is set: try to authenticate by AutoLoginKey
        if (clientKey != null && !client.IsAuthenticated)
        {
          if (AutologinTimeout > 0)
          {
            AutoLoginToken token = new AutoLoginToken(clientKey, client);
            // the client token is in the list
            foreach (AutoLoginToken aToken in _loginTokens)
            {
              if (aToken.Key == token.Key)
              {
                // Check if the autologin key was issued within the timeout
                TimeSpan elapsed = DateTime.Now - aToken.Issued;
                client.IsAuthenticated = (elapsed.Minutes < AutologinTimeout);
                client = aToken.Client;

                // Renew the timeout
                aToken.Issued = DateTime.Now;
              }
            }

            // MediaPortal was rebooted (will wipe all AutoLoginKeys) or
            // autologin time out period is over (configurable in settings).
            //
            // Tell the client to reauthenticate.
            if (!client.IsAuthenticated)
            {
              Logger.Debug("WifiRemote: AutoLoginToken timed out. Client needs to reauthenticate.");
              TellClientToReAuthenticate(sender);
              return;
            }
          }
          else
          {
            Logger.Debug("WifiRemote: AutoLogin is disabled but client tried to auto-authenticate.");
            TellClientToReAuthenticate(sender);
            return;
          }
        }

        // The client is already authentificated or we don't need authentification
        if (type != null && client.IsAuthenticated && type != "identify")
        {
          Func<JObject, SocketServer, AsyncSocket, Task<bool>> function;
          if (MessageType.TryGetValue(type, out function))
          {
            Logger.Debug("WifiRemote: MessageType: {0} got called", type);
            function.Invoke(message, this, sender);
          }
          else
          {
            Logger.Warn("WifiRemote: Couldn't get MessageType: {0}", type);
          }

        }
        else
        {
          // user is not yet authenticated
          if (type == "identify")
          {
            // Save client name if supplied
            if (message["Name"] != null)
            {
              client.ClientName = (string)message["Name"];
            }

            // Save client description if supplied
            if (message["Description"] != null)
            {
              client.ClientDescription = (string)message["Description"];
            }

            // Save application name if supplied
            if (message["Application"] != null)
            {
              client.ApplicationName = (string)message["Application"];
            }

            // Save application version if supplied
            if (message["Version"] != null)
            {
              client.ApplicationVersion = (string)message["Version"];
            }

            // Authentication
            if (AllowedAuth == AuthMethod.None || CheckAuthenticationRequest(client, message["Authenticate"]))
            {
              // User successfully authenticated
              sender.GetRemoteClient().IsAuthenticated = true;
              SendAuthenticationResponse(sender, true);
              SendMessageOverviewInformation.Send(sender);
            }
            else
            {
              // Client sends a message other then authenticate when not yet
              // authenticated or authenticate failed
              SendAuthenticationResponse(sender, false);
            }
          }
          else
          {
            // Client needs to authenticate first
            TellClientToReAuthenticate(sender);
          }
        }
      }
      catch (Exception e)
      {
        Logger.Error("WifiRemote: Communication Error", e);
      }

      // Continue listening
      sender.Read(AsyncSocket.CRLFData, -1, 0);
    }

    /// <summary>
    /// Send a "You need to authenticate yourself." error followed by the
    /// welcome message.
    /// </summary>
    /// <param name="socket"></param>
    private void TellClientToReAuthenticate(AsyncSocket socket)
    {
      MessageAuthenticationResponse response = new MessageAuthenticationResponse(false);
      response.ErrorMessage = "You need to authenticate yourself.";
      SendMessageToClient.Send(response, socket, true);
      SendMessageToClient.Send(_welcomeMessage, socket, true);
    }

    #region Auth

    private bool CheckAuthenticationRequest(RemoteClient client, JToken msg)
    {
      if (msg == null || !msg.HasValues)
      {
        Logger.Warn("WifiRemote: Client sent empty authentication String");
        return false;
      }

      AuthMethod auth = AllowedAuth;
      if (auth == AuthMethod.None)
      {
        // Every auth request is valid for AuthMethod.None
        client.UserId = null; //Use current client or logged in user
        client.IsAuthenticated = true;
        return true;
      }

      JObject message = (JObject)msg;
      // For AuthMethod.Both we have to check which method was choosen.
      if (AllowedAuth == AuthMethod.Both)
      {
        if (message["AuthMethod"] == null)
        {
          Logger.Warn("WifiRemote: Client {0} authentification failed, no AuthMethod submitted", client);
          return false;
        }
        else
        {
          String authString = (string)message["AuthMethod"];
          if (authString != null)
          {
            if (authString.Equals("userpass"))
            {
              auth = AuthMethod.UserPassword;
            }
            else if (authString.Equals("passcode"))
            {
              auth = AuthMethod.Passcode;
            }
            else
            {
              Logger.Warn("WifiRemote: Client " + client.ToString() + " authentification failed, invalid authMethod '" + authString + "'");
              return false;
            }
          }
        }
      }

      // Check user credentials
      if (auth == AuthMethod.UserPassword)
      {
        if (message["User"] != null && message["Password"] != null)
        {
          String user = (string)message["User"];
          String pass = (string)message["Password"];
          var id = VerifyUser(user, pass);
          if (id != null)
          {
            client.AuthenticatedBy = auth;
            client.User = user;
            client.Password = pass;
            client.UserId = id.Value;
            client.IsAuthenticated = true;
            Logger.Debug("WifiRemote: Client " + client.ToString() + " successfully authentificated by username and password");
            return true;
          }
        }
      }
      else if (auth == AuthMethod.Passcode)
      {
        if (message["PassCode"] != null)
        {
          String pass = (string)message["PassCode"];
          if (pass.Equals(this.PassCode))
          {
            client.AuthenticatedBy = auth;
            client.PassCode = pass;
            client.UserId = null; //Use current client or logged in user
            client.IsAuthenticated = true;
            Logger.Debug("WifiRemote: Client " + client.ToString() + " successfully authentificated by passcode");
            return true;
          }
        }
      }

      Logger.Warn("WifiRemote: Client " + client.ToString() + " authentification failed");
      return false;
    }

    private Guid?  VerifyUser(string username, string password)
    {
      IUserManagement clientUserManagement = ServiceRegistration.Get<IUserManagement>();
      var userManagement = clientUserManagement.UserProfileDataManagement;
      if (userManagement != null)
      {
        var user = userManagement.GetProfileByNameAsync(username).Result;
        if (user.Success)
        {
          byte[] converted = Convert.FromBase64String(password);
          var pass = Encoding.UTF8.GetString(converted);
          if (UserProfile.VerifyPassword(pass, user.Result.Password))
          {
            userManagement.LoginProfileAsync(user.Result.ProfileId).Wait();
            return user.Result.ProfileId;
          }
        }
      }
      return null;
    }

    #endregion

    #region Send Messages

    private void SendAuthenticationResponse(AsyncSocket socket, bool _success)
    {
      MessageAuthenticationResponse authResponse = new MessageAuthenticationResponse(_success);
      if (!_success)
      {
        authResponse.ErrorMessage = "Login failed";
      }
      else
      {
        Logger.Debug("WifiRemote: Client identified: " + socket.GetRemoteClient().ToString());
        string key = GetRandomMD5();
        authResponse.AutologinKey = key;
        _loginTokens.Add(new AutoLoginToken(key, socket.GetRemoteClient()));
      }

      SendMessageToClient.Send(authResponse, socket, true);
    }

    #endregion

    #region MD5

    /// <summary>
    /// Get a random md5 hash
    /// </summary>
    /// <returns></returns>
    private String GetRandomMD5()
    {
      string randomString = System.IO.Path.GetRandomFileName();
      randomString = randomString.Replace(".", "");

      System.Security.Cryptography.MD5CryptoServiceProvider md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
      byte[] randomBytes = Encoding.UTF8.GetBytes(randomString);
      randomBytes = md5.ComputeHash(randomBytes);
      StringBuilder hash = new StringBuilder();
      foreach (byte b in randomBytes)
      {
        hash.Append(b.ToString("x2").ToLower());
      }

      return hash.ToString();
    }

    #endregion MD5

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
