using System;
using System.Collections.Generic;
using WifiRemote;

namespace MediaPortal.Plugins.WifiRemote.Messages
{
  internal class MessageWelcome : IMessage
  {
    private string type = "welcome";
    private int server_version = 17;
    private AuthMethod authMethod = AuthMethod.UserPassword;

    /// <summary>
    /// Type of this method
    /// </summary>
    public string Type
    {
      get { return type; }
    }

    /// <summary>
    /// API version of this WifiRemote instance. 
    /// Should be increased on breaking changes.
    /// </summary>
    public int Server_Version
    {
      get { return server_version; }
    }

    /// <summary>
    /// Authentication method required of the client.
    /// </summary>
    public AuthMethod AuthMethod
    {
      get { return authMethod; }
      set { authMethod = value; }
    }

    public Dictionary<string, bool> MPExtendedServicesInstalled
    {
      get
      {
        return new Dictionary<string, bool>()
        {
          { "MAS", false },
          { "TAS", false },
          { "WSS", false }
        };
      }
    }

    public Boolean TvPluginInstalled
    {
      get { return false; }
    }

    /// <summary>
    /// Contructor.
    /// </summary>
    public MessageWelcome()
    {
    }
  }
}