using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Deusty.Net;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using Newtonsoft.Json.Linq;

namespace MediaPortal.Plugins.WifiRemote.MessageParser
{
  internal class ParserPosition
  {
    public static bool Parse(JObject message, SocketServer server, AsyncSocket sender)
    {
      int seekType = (int)message["SeekType"];

      Logger.Debug("ParserPosition: SeekType: {0}", seekType);

      if (seekType == 0)
      {
        int position = (int)message["Position"];
        Helper.SetPositionPercent(position, true);
      }
      if (seekType == 1)
      {
        int position = (int)message["Position"];
        Helper.SetPositionPercent(position, false);
      }
      if (seekType == 2)
      {
        int position = (int)message["Position"];
        Helper.SetPosition(position, true);
      }
      else if (seekType == 3)
      {
        int position = (int)message["Position"];
        Helper.SetPosition(position, false);
      }

      return true;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
