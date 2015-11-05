using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Deusty.Net;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.WifiRemote.Messages;
using MediaPortal.Plugins.WifiRemote.SendMessages;
using Newtonsoft.Json.Linq;
using WifiRemote;

namespace MediaPortal.Plugins.WifiRemote.MessageParser
{
  // TODO what is this?!
  internal class ParserProperties
  {
    public static bool Parse(JObject message, SocketServer server, AsyncSocket sender)
    {
      List<string> output = new List<String>();
      JArray array = (JArray)message["Properties"];
      if (array != null)
      {
        foreach (JValue v in array)
        {
          String propString = (string)v.Value;
          ServiceRegistration.Get<ILogger>().Info("ParserProperties: propertiy: {0}", propString);
          output.Add(propString);
        }
        MessageProperties propertiesMessage = new MessageProperties();

        List<Property> properties = new List<Property>();
        foreach (String s in output)
        {
          properties.Add(new Property(s, "??"));
        }

        propertiesMessage.Tags = properties;
        SendMessageToClient.Send(propertiesMessage, sender);
      }

      

      return true;
    }
  }
}
