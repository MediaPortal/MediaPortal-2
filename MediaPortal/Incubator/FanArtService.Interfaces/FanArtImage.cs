#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace MediaPortal.Extensions.UserServices.FanArtService.Interfaces
{
  /// <summary>
  /// Data object for fan art images.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Note: This class is serialized/deserialized by the <see cref="XmlSerializer"/>.
  /// If changed, this has to be taken into consideration.
  /// </para>
  /// </remarks>
  public class FanArtImage
  {
    // We could use some cache for this instance, if we would have one...
    protected static XmlSerializer _xmlSerializer; // Lazy initialized
    private string _name;
    private byte[] _binaryData;

    public FanArtImage()
    { }

    public FanArtImage(string name, byte[] binaryData)
    {
      _name = name;
      _binaryData = binaryData;
    }

    /// <summary>
    /// Returns the name of this FanArtImage.
    /// </summary>
    [XmlIgnore]
    public string Name
    {
      get { return _name; }
    }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlAttribute("Name")]
    public string XML_Name
    {
      get { return _name; }
      set { _name = value; }
    }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlElement("BinaryData")]
    public byte[] XML_BinaryData
    {
      get { return _binaryData; }
      set { _binaryData = value; }
    }

    /// <summary>
    /// Serializes this user profile instance to XML.
    /// </summary>
    /// <returns>String containing an XML fragment with this instance's data.</returns>
    public string Serialize()
    {
      XmlSerializer xs = GetOrCreateXMLSerializer();
      StringBuilder sb = new StringBuilder(); // Will contain the data, formatted as XML
      using (XmlWriter writer = XmlWriter.Create(sb, new XmlWriterSettings { OmitXmlDeclaration = true }))
        xs.Serialize(writer, this);
      return sb.ToString();
    }

    /// <summary>
    /// Serializes this user profile instance to the given <paramref name="writer"/>.
    /// </summary>
    /// <param name="writer">Writer to write the XML serialization to.</param>
    public void Serialize(XmlWriter writer)
    {
      XmlSerializer xs = GetOrCreateXMLSerializer();
      xs.Serialize(writer, this);
    }

    /// <summary>
    /// Deserializes a user profile instance from a given XML fragment.
    /// </summary>
    /// <param name="str">XML fragment containing a serialized user profile instance.</param>
    /// <returns>Deserialized instance.</returns>
    public static FanArtImage Deserialize(string str)
    {
      XmlSerializer xs = GetOrCreateXMLSerializer();
      using (StringReader reader = new StringReader(str))
        return xs.Deserialize(reader) as FanArtImage;
    }

    /// <summary>
    /// Deserializes a user profile instance from a given <paramref name="reader"/>.
    /// </summary>
    /// <param name="reader">XML reader containing a serialized user profile instance.</param>
    /// <returns>Deserialized instance.</returns>
    public static FanArtImage Deserialize(XmlReader reader)
    {
      XmlSerializer xs = GetOrCreateXMLSerializer();
      return xs.Deserialize(reader) as FanArtImage;
    }

    protected static XmlSerializer GetOrCreateXMLSerializer()
    {
      return _xmlSerializer ?? (_xmlSerializer = new XmlSerializer(typeof(FanArtImage)));
    }

    /// <summary>
    /// Loads an image from filesystem an returns a new <see cref="FanArtImage"/>.
    /// </summary>
    /// <param name="fileName">File name to load</param>
    /// <returns>FanArtImage or <c>null</c>.</returns>
    public static FanArtImage FromFile(string fileName)
    {
      if (string.IsNullOrEmpty(fileName))
        return null;
      FileInfo fileInfo = new FileInfo(fileName);
      if (!fileInfo.Exists)
        return null;

      byte[] binary = new byte[fileInfo.Length];
      using (FileStream fileStream = new FileStream(fileName, FileMode.Open))
      using (BinaryReader binaryReader = new BinaryReader(fileStream))
        binaryReader.Read(binary, 0, binary.Length);

      return  new FanArtImage(fileInfo.Name, binary);
    }
  }
}
