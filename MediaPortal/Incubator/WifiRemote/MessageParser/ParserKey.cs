using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Deusty.Net;
using Newtonsoft.Json.Linq;

namespace MediaPortal.Plugins.WifiRemote.MessageParser
{
  internal class ParserKey
  {
    public static bool Parse(JObject message, SocketServer server, AsyncSocket sender)
    {
      string key = (string)message["Key"];
      string modifier = (message["Modifier"] != null) ? (string)message["Modifier"] : null;

      if (key == "{DONE}")
      {
        //TODO: simulate pressing "done" on the virtual keyboard -> needs MediaPortal patch
      }
      else
      {
        if (modifier != null)
        {
          SendKeys.SendWait(modifier + key);
        }
        else
        {
          //Sends a key to mediaportal
          SendKeys.SendWait(key);
        }
      }
      return true;
    }
  }
}
