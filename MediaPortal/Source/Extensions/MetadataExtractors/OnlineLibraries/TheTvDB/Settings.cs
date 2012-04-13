using System;
using System.IO;
using System.Xml.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.TheTvDB
{
  internal static class Settings
  {
    public static TE Load<TE>(string fileName)
    {
      try
      {
        XmlSerializer serializer = new XmlSerializer(typeof(TE));
        using (FileStream fileStream = new FileStream(fileName, FileMode.Open))
          return (TE)serializer.Deserialize(fileStream);
      }
      catch (Exception)
      {
        return default(TE);
      }
    }
    public static void Save<TE>(string fileName, TE settingsObject)
    {
      try
      {
        XmlSerializer serializer = new XmlSerializer(typeof(TE));
        using (FileStream fileStream = new FileStream(fileName, FileMode.Create))
          serializer.Serialize(fileStream, settingsObject);
      }
      catch (Exception)
      {
      }
    }
  }
}
