using System.Runtime.Serialization;
using System.Xml;

namespace MediaPortal.Plugins.SlimTv.Interfaces.LiveTvMediaItem
{
  public static class SerializeHelper
  {
    public static void SerializeXml<TE>(this TE objectToSerialize, XmlWriter writer)
    {
      DataContractSerializer dsc = new DataContractSerializer(typeof(TE));
      dsc.WriteObject(writer, objectToSerialize);
    }
    public static TE DeserializeXml<TE>(this XmlReader reader)
    {
      DataContractSerializer dsc = new DataContractSerializer(typeof(TE));
      return (TE) dsc.ReadObject(reader);
    }
  }
}
