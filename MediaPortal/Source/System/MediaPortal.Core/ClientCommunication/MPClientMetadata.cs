using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using MediaPortal.Core.General;

namespace MediaPortal.Core.ClientCommunication
{
  /// <summary>
  /// Contains data about a MediaPortal client which is attached to a MediaPortal server.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Note: This class is serialized/deserialized by the <see cref="XmlSerializer"/>.
  /// If changed, this has to be taken into consideration.
  /// </para>
  /// </remarks>
  public class MPClientMetadata
  {
    #region Protected fields

    protected string _systemId;
    protected SystemName _lastSystem;
    protected string _lastClientName;

    #endregion

    // We could use some cache for this instance, if we would have one...
    protected static XmlSerializer _xmlSerializer = null; // Lazy initialized

    public MPClientMetadata(string systemId, SystemName lastHostName, string lastClientName)
    {
      _systemId = systemId;
      _lastSystem = lastHostName;
      _lastClientName = lastClientName;
    }

    /// <summary>
    /// UUID of the attached client.
    /// </summary>
    [XmlIgnore]
    public string SystemId
    {
      get { return _systemId; }
    }

    /// <summary>
    /// Last known host name of the client.
    /// </summary>
    [XmlIgnore]
    public SystemName LastSystem
    {
      get { return _lastSystem; }
    }

    /// <summary>
    /// Last known client name.
    /// </summary>
    [XmlIgnore]
    public string LastClientName
    {
      get { return _lastClientName; }
    }

    /// <summary>
    /// Serializes this client metadata instance to XML.
    /// </summary>
    /// <returns>String containing an XML fragment with this instance's data.</returns>
    public string Serialize()
    {
      XmlSerializer xs = GetOrCreateXMLSerializer();
      StringBuilder sb = new StringBuilder(); // Will contain the data, formatted as XML
      using (XmlWriter writer = XmlWriter.Create(sb, new XmlWriterSettings {OmitXmlDeclaration = true}))
        xs.Serialize(writer, this);
      return sb.ToString();
    }

    /// <summary>
    /// Serializes this client metadata instance to the given <paramref name="writer"/>.
    /// </summary>
    /// <param name="writer">Writer to write the XML serialization to.</param>
    public void Serialize(XmlWriter writer)
    {
      XmlSerializer xs = GetOrCreateXMLSerializer();
      xs.Serialize(writer, this);
    }

    /// <summary>
    /// Deserializes a client metadata instance from a given XML fragment.
    /// </summary>
    /// <param name="str">XML fragment containing a serialized instance of this class.</param>
    /// <returns>Deserialized instance.</returns>
    public static MPClientMetadata Deserialize(string str)
    {
      XmlSerializer xs = GetOrCreateXMLSerializer();
      using (StringReader reader = new StringReader(str))
        return xs.Deserialize(reader) as MPClientMetadata;
    }

    /// <summary>
    /// Deserializes a client metadata instance from a given <paramref name="reader"/>.
    /// </summary>
    /// <param name="reader">XML reader containing a serialized instance of this class.</param>
    /// <returns>Deserialized instance.</returns>
    public static MPClientMetadata Deserialize(XmlReader reader)
    {
      XmlSerializer xs = GetOrCreateXMLSerializer();
      return xs.Deserialize(reader) as MPClientMetadata;
    }

    #region Additional members for the XML serialization

    internal MPClientMetadata() { }

    protected static XmlSerializer GetOrCreateXMLSerializer()
    {
      return _xmlSerializer ?? (_xmlSerializer = new XmlSerializer(typeof(MPClientMetadata)));
    }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlElement("SystemId", IsNullable = false)]
    public string XML_SystemId
    {
      get { return _systemId; }
      set { _systemId = value; }
    }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlElement("LastSystem")]
    public string XML_LastSystem
    {
      get { return _lastSystem == null ? null : _lastSystem.HostName; }
      set { _lastSystem = value == null ? null : new SystemName(value); }
    }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlElement("LastClientName")]
    public string XML_LastClientName
    {
      get { return _lastClientName; }
      set { _lastClientName = value; }
    }

    #endregion
  }
}