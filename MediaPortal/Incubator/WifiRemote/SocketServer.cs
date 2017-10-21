using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Deusty.Net;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.WifiRemote.MessageParser;
using MediaPortal.Plugins.WifiRemote.Messages;
using MediaPortal.Plugins.WifiRemote.SendMessages;
using MediaPortal.UI.Control.InputManager;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WifiRemote;

namespace MediaPortal.Plugins.WifiRemote
{
  internal class SocketServer
  {
    // SocketServer
    private readonly UInt16 _port;

    private bool isStarted = false;

    public List<AsyncSocket> connectedSockets;

    private AsyncSocket listenSocket;
    private AuthMethod allowedAuth;
    private List<AutoLoginToken> loginTokens;
    private Dictionary<AsyncSocket, int> socketsWaitingForScreenshot;

    protected static SocketServer _instance = null;
    public static SocketServer Instance { get { return _instance; } }
    

    // This function specifies all the different Message Types and Maps the processing function to it.
    private readonly Dictionary<string, Func<JObject, SocketServer, AsyncSocket, bool>> MessageType = new Dictionary<string, Func<JObject, SocketServer, AsyncSocket, bool>>()
    {
      { "command", ParserCommand.Parse },
      { "key", ParserKey.Parse },
      { "commandstartrepeat", ParserCommand.ParseCommandStartRepeat },
      { "commandstoprepeat", ParserCommand.ParseCommandStopRepeat },
      /*{ "window", new Func<int, int, int>(Func1) },
      { "activatewindow", new Func<int, int, int>(Func1) },
      { "dialog", new Func<int, int, int>(Func1) },
      { "facade", new Func<int, int, int>(Func1) },*/
      { "powermode", ParserPowermode.Parse },
      { "volume", ParserVolume.Parse },
      { "position", ParserPosition.Parse },
      { "playfile", ParserPlayFile.Parse },
      { "playchannel", ParserPlaychannel.Parse },
      { "playradiochannel", ParserPlaychannel.Parse },  // should be the same as playchannel in MP2
      /*{ "playrecording", new Func<int, int, int>(Func1) },*/
      { "mpext", ParserMPExt.Parse },
      //{ "plugins", new Func<int, int, int>(Func1) },
      { "properties", ParserProperties.Parse },
      /*{ "image", new Func<int, int, int>(Func1) },*/
      { "screenshot", ParserScreenshot.Parse },
      { "playlist", ParserPlaylist.Parse },
      { "requeststatus", ParserRequeststatus.Parse },
      { "requestnowplaying", ParserRequestnowplaying.Parse },
      { "movingpictures", ParserMovingpictures.Parse },
      /*{ "tvseries", new Func<int, int, int>(Func1) },
      { "message", new Func<int, int, int>(Func1) },
      { "showdialog", new Func<int, int, int>(Func1) }*/

    };   

    private readonly MessageWelcome _welcomeMessage;

    /// <summary>
    /// Username  for client authentification
    /// </summary>
    internal String UserName { get; set; }

    /// <summary>
    /// Password for client authentification
    /// </summary>
    internal String Password { get; set; }

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
        return allowedAuth;
      }

      set
      {
        allowedAuth = value;
        _welcomeMessage.AuthMethod = allowedAuth;
      }
    }

    /// <summary>
    /// Display Notifications when clients connect/disconnect (needs MpNotifications plugin)
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

      initSocket();
    }

    /// <summary>
    /// Initialise the socket
    /// </summary>
    private void initSocket()
    {
      listenSocket = new AsyncSocket { AllowMultithreadedCallbacks = true };

      // Tell AsyncSocket to allow multi-threaded delegate methods

      // Register for client connect event
      listenSocket.DidAccept += listenSocket_DidAccept;

      // Initialize list to hold connected sockets
      connectedSockets = new List<AsyncSocket>();
    }

    /// <summary>
    /// Start listening for incoming connections.
    /// </summary>
    public void Start()
    {
      // Abort if already started
      if (isStarted)
      {
        Logger.Debug("ListenSocket already accepting connections, aborting start ...");
        return;
      }

      if (listenSocket == null)
      {
        initSocket();
      }

      Exception error;
      if (!listenSocket.Accept(_port, out error))
      {
        Logger.Error("Error starting server: " + error.Message);
        return;
      }

      isStarted = true;
      loginTokens = new List<AutoLoginToken>();
      Logger.Info("Now accepting connections.");
    }

    /// <summary>
    /// Stop the server and disconnect all clients.
    /// </summary>
    public void Stop()
    {
      if (!isStarted)
      {
        Logger.Debug("ListenSocket already stopped, ignoring stop command");
        return;
      }

      // Stop accepting connections
      listenSocket.Close();

      // Stop any client connections
      lock (connectedSockets)
      {
        foreach (AsyncSocket socket in connectedSockets)
        {
          //socket.CloseAfterReading();
          socket.Close();
        }
      }

      isStarted = false;
      listenSocket = null;

      Logger.Info("SocketServer stopped.");
    }

    /// <summary>
    /// A client connected.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="newSocket"></param>
    void listenSocket_DidAccept(AsyncSocket sender, AsyncSocket newSocket)
    {
      // Subsribe to worker socket events
      newSocket.DidRead += newSocket_DidRead;
      newSocket.DidWrite += newSocket_DidWrite;
      newSocket.WillClose += newSocket_WillClose;
      newSocket.DidClose += newSocket_DidClose;

      newSocket.SetRemoteClient(new RemoteClient());

      // Store worker socket in client list
      lock (connectedSockets)
      {
        connectedSockets.Add(newSocket);
      }

      // Send welcome message to client
      Logger.Debug("Client connected, sending welcome msg.");
      SendMessageToClient.Send(_welcomeMessage, newSocket, true);
    }

    /// <summary>
    /// A client closed the connection.
    /// </summary>
    /// <param name="sender"></param>
    void newSocket_DidClose(AsyncSocket sender)
    {
      // Remove the client from the client list.
      lock (connectedSockets)
      {
        Logger.Info("removing client " + sender.GetRemoteClient().ClientName + " from connected sockets");
        connectedSockets.Remove(sender);
      }
    }

    /// <summary>
    /// A client will disconnect.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    void newSocket_WillClose(AsyncSocket sender, Exception e)
    {
      Logger.Debug("A client is about to disconnect.");
    }

    /// <summary>
    /// The client sent a message
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="tag"></param>
    void newSocket_DidWrite(AsyncSocket sender, long tag)
    {
      sender.Read(AsyncSocket.CRLFData, -1, 0);
    }

    /// <summary>
    /// Read a message from the client.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="data"></param>
    /// <param name="tag"></param>
    void newSocket_DidRead(AsyncSocket sender, byte[] data, long tag)
    {
      string msg = null;

      try
      {
        msg = Encoding.UTF8.GetString(data);

        //comment this out to log all received commands
        //WifiRemote.LogMessage(msg, WifiRemote.LogType.Debug);

        // Get json object
        JObject message = JObject.Parse(msg);
        string type = (string)message["Type"];
        RemoteClient client = sender.GetRemoteClient();

        // Autologin handling
        //
        // Has to be activated in WifiRemote configuration.
        string clientKey = (string)message["AutologinKey"];

        // Key is set: try to authenticate by AutoLoginKey
        if (clientKey != null && !client.IsAuthenticated)
        {
          if (AutologinTimeout > 0)
          {
            AutoLoginToken token = new AutoLoginToken(clientKey, client);
            // the client token is in the list
            foreach (AutoLoginToken aToken in loginTokens)
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
              Logger.Debug("AutoLoginToken timed out. Client needs to reauthenticate.");
              TellClientToReAuthenticate(sender);
              return;
            }
          }
          else
          {
            Logger.Debug("AutoLogin is disabled but client tried to auto-authenticate.");
            TellClientToReAuthenticate(sender);
            return;
          }
        }

        // The client is already authentificated or we don't need authentification
        if (type != null && client.IsAuthenticated && type != "identify")
        {
          Func<JObject, SocketServer, AsyncSocket, bool> function;
          if (MessageType.TryGetValue(type, out function))
          {
            Logger.Info("WifiRemote: MessageType: {0} got called", type);
            function.Invoke(message, this, sender);
          }
          else
          {
            Logger.Info("WifiRemote: Couldn't get MessageType: {0}", type);
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


        //WifiRemote.LogMessage("Received: " + msg, WifiRemote.LogType.Info);
      }
      catch (Exception e)
      {
        Logger.Warn("WifiRemote Communication Error: " + e.Message);
        //WifiRemote.LogMessage("Error converting received data into UTF-8 String: " + e.Message, WifiRemote.LogType.Error);
        //MediaPortal.Dialogs.GUIDialogNotify dialog = (MediaPortal.Dialogs.GUIDialogNotify)MediaPortal.GUI.Library.GUIWindowManager.GetWindow((int)MediaPortal.GUI.Library.GUIWindow.Window.WINDOW_DIALOG_NOTIFY);
        //dialog.Reset();
        //dialog.SetHeading("WifiRemote Communication Error");
        //dialog.SetText(e.Message);
        //dialog.DoModal(MediaPortal.GUI.Library.GUIWindowManager.ActiveWindow);
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
        Logger.Warn("Client sent empty authentication String");
        return false;
      }

      AuthMethod auth = AllowedAuth;

      if (auth == AuthMethod.None)
      {
        // Every auth request is valid for AuthMethod.None
        return true;
      }

      JObject message = (JObject)msg;
      // For AuthMethod.Both we have to check which method was choosen.
      if (AllowedAuth == AuthMethod.Both)
      {
        if (message["AuthMethod"] == null)
        {
          Logger.Info("User {0} authentification failed, no authMethod submitted", client);
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
              Logger.Info("User " + client.ToString() + " authentification failed, invalid authMethod '" + authString + "'");
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
          if (user.Equals(this.UserName) && pass.Equals(this.Password))
          {
            client.AuthenticatedBy = auth;
            client.User = user;
            client.Password = pass;
            client.IsAuthenticated = true;
            Logger.Debug("User " + client.ToString() + " successfully authentificated by username and password");
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
            client.IsAuthenticated = true;
            Logger.Debug("User " + client.ToString() + " successfully authentificated by passcode");
            return true;
          }
        }
      }

      Logger.Info("User " + client.ToString() + " authentification failed");
      return false;
    }

    #endregion

    #region send Messages

    


    

    private void SendAuthenticationResponse(AsyncSocket socket, bool _success)
    {
      MessageAuthenticationResponse authResponse = new MessageAuthenticationResponse(_success);
      if (!_success)
      {
        authResponse.ErrorMessage = "Login failed";
      }
      else
      {
        Logger.Debug("Client identified: " + socket.GetRemoteClient().ToString());
        string key = getRandomMD5();
        authResponse.AutologinKey = key;
        loginTokens.Add(new AutoLoginToken(key, socket.GetRemoteClient()));
      }

      SendMessageToClient.Send(authResponse, socket, true);
    }

    #endregion

    #region MD5

    /// <summary>
    /// Get a random md5 hash
    /// </summary>
    /// <returns></returns>
    private String getRandomMD5()
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
