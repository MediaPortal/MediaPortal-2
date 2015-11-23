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
  internal class ParserProperties
  {
    public static bool Parse(JObject message, SocketServer server, AsyncSocket sender)
    {
      JArray array = (JArray)message["Properties"];
      if (array != null)
      {
        foreach (JValue v in array)
        {
          String propString = (string)v.Value;
          ServiceRegistration.Get<ILogger>().Info("ParserProperties: propertiy: {0}", propString);
          SocketServer.Instance.connectedSockets.Single(x => x == sender).GetRemoteClient().Properties.Add(propString);
        }
        MessageProperties propertiesMessage = new MessageProperties();

        List<Property> properties = new List<Property>();
        foreach (String s in SocketServer.Instance.connectedSockets.Single(x => x == sender).GetRemoteClient().Properties)
        {
          // TODO: MP2 doesn' have properties like '#TV.TuningDetails.ChannelName'
          //String value = GUIPropertyManager.GetProperty(s);

          //if (value != null && !value.Equals("") && CheckProperty(s))
          //{
            //properties.Add(new Property(s, value));
          //}
        }

        propertiesMessage.Tags = properties;
        SendMessageToClient.Send(propertiesMessage, sender);
      }

      

      return true;
    }
  }
}
