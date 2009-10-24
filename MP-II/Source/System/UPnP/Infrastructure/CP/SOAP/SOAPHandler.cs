#region Copyright (C) 2005-2008 Team MediaPortal

/* 
 *  Copyright (C) 2005-2008 Team MediaPortal
 *  http://www.team-mediaportal.com
 *
 *  This Program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation; either version 2, or (at your option)
 *  any later version.
 *   
 *  This Program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU General Public License for more details.
 *   
 *  You should have received a copy of the GNU General Public License
 *  along with GNU Make; see the file COPYING.  If not, write to
 *  the Free Software Foundation, 675 Mass Ave, Cambridge, MA 02139, USA. 
 *  http://www.gnu.org/copyleft/gpl.html
 *
 */

#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using UPnP.Infrastructure.Common;
using UPnP.Infrastructure.CP.DeviceTree;
using UPnP.Infrastructure.Utils;

namespace UPnP.Infrastructure.CP.SOAP
{
  /// <summary>
  /// Soap message encoding and decoding class for the UPnP control point.
  /// </summary>
  public class SOAPHandler
  {
    /// <summary>
    /// Encodes a call of the specified <paramref name="action"/> with the given <paramref name="inParamValues"/> and
    /// returns the resulting SOAP XML string.
    /// </summary>
    /// <param name="action">Action to be called.</param>
    /// <param name="inParamValues">List of parameter values which must match the action's signature.</param>
    /// <param name="upnpVersion">UPnP version to use for the encoding.</param>
    /// <returns>XML string which contains the SOAP document.</returns>
    public static string EncodeCall(CpAction action, IList<object> inParamValues, UPnPVersion upnpVersion)
    {
      bool targetSupportsUPnP11 = upnpVersion.VerMin >= 1;
      StringBuilder result = new StringBuilder(5000);
      XmlWriter writer = XmlWriter.Create(new StringWriterWithEncoding(result, Encoding.UTF8));
      SoapHelper.WriteSoapEnvelopeStart(writer, true);
      writer.WriteStartElement("u", action.Name, action.ParentService.ServiceTypeVersion_URN);

      // Check input parameters
      IList<CpArgument> formalArguments = action.InArguments;
      if (inParamValues.Count != formalArguments.Count)
        throw new ArgumentException("Invalid argument count");
      for (int i = 0; i < formalArguments.Count; i++)
      {
        CpArgument argument = formalArguments[i];
        object value = inParamValues[i];
        writer.WriteStartElement(argument.Name);
        argument.SoapSerializeArgument(value, !targetSupportsUPnP11, writer);
        writer.WriteEndElement(); // argument.Name
      }
      SoapHelper.WriteSoapEnvelopeEndAndClose(writer);
      return result.ToString();
    }

    /// <summary>
    /// Takes the XML document provided by the given <paramref name="textReader"/> instance, parses it and provides
    /// the action result to the appropriate receiver.
    /// </summary>
    /// <param name="textReader">Text reader which contains the SOAP XML action result message.</param>
    /// <param name="action">Action which was called before.</param>
    /// <param name="clientState">State object which was given in the action call and which will be returned to the client.</param>
    /// <param name="upnpVersion">UPnP version of the UPnP server.</param>
    public static void HandleResult(TextReader textReader, CpAction action, object clientState, UPnPVersion upnpVersion)
    {
      bool sourceSupportsUPnP11 = upnpVersion.VerMin >= 1;
      IList<object> outParameterValues;
      try
      {
        outParameterValues = ParseResult(textReader, action, sourceSupportsUPnP11);
      }
      catch (Exception)
      {
        // TODO Albert: In the current state of the (DevArch) document, the UPnP action error codes 613-699
        // are TBD. I guess we should use one of them instead of 501 (Action failed) here.
        action.ActionErrorResultPresent(new UPnPError(501, "Invalid action result"), clientState);
        return;
      }
      try
      {
        // Invoke action result
        action.ActionResultPresent(outParameterValues, clientState);
      }
      catch (Exception e)
      {
        Configuration.LOGGER.Warn("UPnP subsystem: Error invoking action '{0}'", e, action.FullQualifiedName);
      }
    }

    protected static IList<object> ParseResult(TextReader textReader, CpAction action, bool sourceSupportsUPnP11)
    {
      IList<object> outParameterValues = new List<object>();
      using(XmlReader reader = XmlReader.Create(textReader))
      {
        reader.MoveToContent();
        // Parse SOAP envelope
        reader.ReadStartElement("Envelope", UPnPConsts.NS_SOAP_ENVELOPE);
        reader.ReadStartElement("Body", UPnPConsts.NS_SOAP_ENVELOPE);
        // Reader is positioned at the action element
        string serviceTypeVersion_URN = reader.NamespaceURI;
        string type;
        int version;
        // Parse service and action
        if (!ParserHelper.TryParseTypeVersion_URN(serviceTypeVersion_URN, out type, out version))
          throw new ArgumentException("Invalid service type or version");
        string actionName = reader.LocalName;
        if (!actionName.EndsWith("Response") ||
            actionName.Substring(0, actionName.Length - "Response".Length) != action.Name)
          throw new ArgumentException("Invalid action name in result message");
        reader.ReadStartElement();

        // Parse and check output parameters
        IEnumerator<CpArgument> formalArgumentEnumer = action.OutArguments.GetEnumerator();
        while (reader.NodeType != XmlNodeType.EndElement)
        {
          string argumentName = reader.Name; // Arguments don't have a namespace, so take full name
          if (!formalArgumentEnumer.MoveNext())
            throw new ArgumentException("Invalid out argument count");
          if (formalArgumentEnumer.Current.Name != argumentName) // Too many arguments
            throw new ArgumentException("Invalid argument name");
          object value;
          if (SoapHelper.ReadNull(reader))
            value = null;
          else
            formalArgumentEnumer.Current.SoapParseArgument(reader, !sourceSupportsUPnP11, out value);
          outParameterValues.Add(value);
        }
      }
      return outParameterValues;
    }
  }
}
