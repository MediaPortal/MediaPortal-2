using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using MediaPortal.Common.UPnP;
using UPnP.Infrastructure.Common;
using UPnP.Infrastructure.Utils;

namespace MediaPortal.Extensions.UserServices.FanArtService.UPnP
{
  internal class UPnPDtImageCollection : UPnPExtendedDataType
  {
    public const string DATATYPE_NAME = "DtImageCollection";

    internal UPnPDtImageCollection()
      : base(DataTypesConfiguration.DATATYPES_SCHEMA_URI, DATATYPE_NAME)
    {
    }

    public override bool SupportsStringEquivalent
    {
      get { return false; }
    }

    public override bool IsNullable
    {
      get { return false; }
    }

    public override bool IsAssignableFrom(Type type)
    {
      return typeof (IEnumerable).IsAssignableFrom(type);
    }

    protected override void DoSerializeValue(object value, bool forceSimpleValue, XmlWriter writer)
    {
      IEnumerable images = (IEnumerable) value;
      foreach (FanArtImage image in images)
        image.Serialize(writer);
    }

    protected override object DoDeserializeValue(XmlReader reader, bool isSimpleValue)
    {
      ICollection<FanArtImage> result = new List<FanArtImage>();
      if (SoapHelper.ReadEmptyStartElement(reader)) // Read start of enclosing element
        return result;
      while (reader.NodeType != XmlNodeType.EndElement)
        result.Add(FanArtImage.Deserialize(reader));
      reader.ReadEndElement(); // End of enclosing element
      return result;
    }    
  }
}
