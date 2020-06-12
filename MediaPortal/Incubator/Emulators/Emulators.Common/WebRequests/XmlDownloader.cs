using Emulators.Common.WebRequests;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace Emulators.Common.WebRequests
{
  public class XmlDownloader : AbstractDownloader
  {
    protected override T Deserialize<T>(string response) 
    {
      XmlSerializer serializer = new XmlSerializer(typeof(T));
      using (XmlReader reader = XmlReader.Create(new StringReader(response)))
        return (T)serializer.Deserialize(reader);
    }
  }
}
