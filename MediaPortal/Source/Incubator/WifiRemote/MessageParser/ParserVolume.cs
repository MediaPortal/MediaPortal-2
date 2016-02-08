using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Deusty.Net;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.UI.Presentation.Players;
using Newtonsoft.Json.Linq;

namespace MediaPortal.Plugins.WifiRemote.MessageParser
{
  internal class ParserVolume
  {
    public static bool Parse(JObject message, SocketServer server, AsyncSocket sender)
    {

      int volume = (int)message["Volume"];
      if (message["Relative"] != null && (bool)message["Relative"])
      {
        volume += ServiceRegistration.Get<IPlayerManager>().Volume;
      }

      if (volume >= 0 && volume <= 100)
      {
        ServiceRegistration.Get<IPlayerManager>().Volume = volume;
      }
      else
      {
        ServiceRegistration.Get<ILogger>().Warn("Trying to set wrong Volume level: {0}", volume);
      }
      return true;
    }
  }
}
