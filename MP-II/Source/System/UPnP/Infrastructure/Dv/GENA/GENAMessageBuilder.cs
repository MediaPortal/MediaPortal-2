using System.Collections.Generic;
using System.Text;
using UPnP.Infrastructure.Dv.DeviceTree;

namespace UPnP.Infrastructure.Dv.GENA
{
  public class GENAMessageBuilder
  {
    public static string BuildEventNotificationMessage(IEnumerable<DvStateVariable> variables, bool forceSimpleValue)
    {
      StringBuilder result = new StringBuilder(
          "<?xml version=\"1.0\"?>" +
          "<e:propertyset xmlns:e=\"urn:schemas-upnp-org:event-1-0\">", 1000);
      foreach (DvStateVariable variable in variables)
      {
        result.Append(
            "<e:property>" +
              "<");
        result.Append(variable.Name);
        result.Append(">");
        result.Append(variable.DataType.SoapSerializeValue(variable.Value, forceSimpleValue));
        result.Append("</");
        result.Append(variable.Name);
        result.Append(">" +
            "</e:property>");
      }
      result.Append(
          "</e:propertyset>");
      return result.ToString();
    }
  }
}
