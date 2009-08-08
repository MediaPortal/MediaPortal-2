using System;
using System.Xml;
using MediaPortal.Utilities.Exceptions;
using UPnP.Infrastructure.Common;

namespace UPnP.Infrastructure.Utils
{
  public class ParserHelper
  {
    /// <summary>
    /// Given the <paramref name="userAgentStr"/> from an HTTP USER-AGENT header, this method extracts the UPnP version
    /// from the string.
    /// </summary>
    /// <param name="userAgentStr">USER-AGENT header entry of the form "OS/version UPnP/1.1 product/version".</param>
    /// <param name="minorVersion">Returns the minor version number in the specified <paramref name="userAgentStr"/>.</param>
    /// <returns><c>true</c>, if the user agent string could successfully be parsed and denotes a UPnP major version of 1.</returns>
    /// <exception cref="MediaPortal.Utilities.Exceptions.InvalidDataException">If the specified header value is malformed.</exception>
    public static bool ParseUserAgentUPnP1MinorVersion(string userAgentStr, out int minorVersion)
    {
      string[] tokens = userAgentStr.Split(new char[] {' '}, StringSplitOptions.RemoveEmptyEntries);
      if (tokens.Length != 3)
        throw new InvalidDataException("Invalid USER-AGENT header entry");
      string upnpToken = tokens[1];
      UPnPVersion ver;
      if (!UPnPVersion.TryParse(upnpToken, out ver))
        throw new UnsupportedRequestException(string.Format("Unsupported USER-AGENT header entry '{0}'", userAgentStr));
      minorVersion = 0;
      if (ver.VerMax != 1)
        return false;
      minorVersion = ver.VerMin;
      return true;
    }

    public static bool TryParseTypeVersion_URN(string typeVersionURN, out string type, out int version)
    {
      type = null;
      version = 0;
      int index = typeVersionURN.LastIndexOf(':');
      if (!typeVersionURN.StartsWith("urn:") || index == -1)
        return false;
      type = typeVersionURN.Substring("urn:".Length, index - "urn:".Length); // Type without "urn:" prefix and without version suffix
      string versionStr = typeVersionURN.Substring(index + 1); // Version suffix
      return int.TryParse(versionStr, out version); // We don't permit version numbers which aren't integers.
    }

    public static bool TryParseDataTypeReference(string typeStr, XmlElement dataTypeElement,
        out string schemaURI, out string dataTypeName)
    {
      schemaURI = null;
      dataTypeName = null;
      int index = typeStr.LastIndexOf(':');
      if (index == -1)
        return false;
      string prefix = typeStr.Substring(0, index);
      dataTypeName = typeStr.Substring(index + 1);
      if (prefix.StartsWith("urn:"))
        schemaURI = prefix;
      else
        schemaURI = dataTypeElement.GetNamespaceOfPrefix(prefix);
      return true;
    }

    /// <summary>
    /// Extracts the UUID from a string containing a UDN (of the form: "uuid:[uuid]").
    /// </summary>
    /// <param name="udn">UDN to break up.</param>
    /// <returns>UUID part of the given <paramref name="udn"/></returns>
    /// <exception cref="ArgumentException">If the given <paramref name="udn"/> doesn't start with "uuid:".</exception>
    public static string ExtractUUIDFromUDN(string udn)
    {
      if (!udn.StartsWith("uuid:"))
        throw new ArgumentException(string.Format("Invalid UDN '{0}'", udn));
      return udn.Substring("uuid:".Length);
    }

    /// <summary>
    /// Returns the text string result of the specified <paramref name="xPathExpr"/> referencing an
    /// XML text node.
    /// </summary>
    /// <param name="element">Element to apply the XPath expression to.</param>
    /// <param name="xPathExpr">XPath expression which references an XML text node (i.e. must end with "text()").</param>
    /// <returns>Contents of the referenced XML text node.</returns>
    /// <exception cref="ArgumentException">If the given <paramref name="xPathExpr"/> doesn't reference an
    /// XML text node.</exception>
    public static string SelectText(XmlElement element, string xPathExpr)
    {
      XmlText text = element.SelectSingleNode(xPathExpr) as XmlText;
      if (text == null)
        throw new ArgumentException(string.Format("Error evaluating XPath expression '{0}'", xPathExpr));
      return text.Data;
    }
  }
}
