using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace HttpServer.Helpers
{
  /// <summary>
  /// Helpers to make XML handling easier
  /// </summary>
  public static class XmlHelper
  {
    /// <summary>
    /// Serializes object to XML.
    /// </summary>
    /// <param name="value">object to serialize.</param>
    /// <returns>XML</returns>
    /// <remarks>
    /// Removes name spaces and adds indentation
    /// </remarks>
    public static string Serialize(object value)
    {
      Check.Require(value, "value");

      //These to lines are nessacary to get rid of the default namespaces.
      XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
      ns.Add(string.Empty, string.Empty);

      // removing XML declaration, the default is false
      XmlWriterSettings xmlSettings = new XmlWriterSettings();
      xmlSettings.Indent = true;
      xmlSettings.IndentChars = "\t";
      xmlSettings.OmitXmlDeclaration = true;

      StringBuilder sb = new StringBuilder();
      using (XmlWriter writer = XmlWriter.Create(sb, xmlSettings))
      {
        XmlSerializer serializer = new XmlSerializer(value.GetType());
        serializer.Serialize(writer, value, ns);
        return sb.ToString();
      }
    }

    /// <summary>
    /// Create an object from a XML string
    /// </summary>
    /// <typeparam name="T">Type of object</typeparam>
    /// <param name="xml">XML string</param>
    /// <returns>object</returns>
    public static T Deserialize<T>(string xml)
    {
      Check.NotEmpty(xml, "xml");

      XmlSerializer serializer = new XmlSerializer(typeof(T));
      using (Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(xml)))
      {
        return (T) serializer.Deserialize(stream);
      }
    }
  }
}