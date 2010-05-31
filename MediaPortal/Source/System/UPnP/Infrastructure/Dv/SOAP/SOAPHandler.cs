#region Copyright (C) 2007-2010 Team MediaPortal

/* 
 *  Copyright (C) 2007-2010 Team MediaPortal
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
using System.Net;
using System.Text;
using System.Xml;
using UPnP.Infrastructure.Common;
using UPnP.Infrastructure.Dv.DeviceTree;
using UPnP.Infrastructure.Utils;

namespace UPnP.Infrastructure.Dv.SOAP
{
  /// <summary>
  /// Class for handling (i.e. parsing and processing) SOAP requests at the UPnP server.
  /// </summary>
  public class SOAPHandler
  {
    protected static IList<object> EMPTY_OBJECT_LIST;

    static SOAPHandler()
    {
      EMPTY_OBJECT_LIST = new List<object>();
    }

    protected class OutParameter
    {
      protected DvArgument _argument;
      protected object _value;

      public OutParameter(DvArgument argument, object value)
      {
        _argument = argument;
        _value = value;
      }

      public DvArgument Argument
      {
        get { return _argument; }
      }

      public object Value
      {
        get { return _value; }
      }
    }

    /// <summary>
    /// Handler method for SOAP control requests.
    /// </summary>
    /// <param name="service">The service whose action was called.</param>
    /// <param name="messageStream">The stream which contains the HTTP message body with the SOAP envelope.</param>
    /// <param name="streamEncoding">Encoding of the <paramref name="messageStream"/>.</param>
    /// <param name="subscriberSupportsUPnP11">Should be set if the requester sent a user agent header which denotes a UPnP
    /// version of 1.1. If set to <c>false</c>, in- and out-parameters with extended data type will be deserialized/serialized
    /// using the string-equivalent of the values.</param>
    /// <param name="context">Context object holding data for the current action call.</param>
    /// <param name="result">SOAP result - may be an action result, a SOAP fault or <c>null</c> if no body should
    /// be sent in the HTTP response.</param>
    /// <returns>HTTP status code to be sent. Should be
    /// <list>
    /// <item><see cref="HttpStatusCode.OK"/> If the action could be evaluated correctly and produced a SOAP result.</item>
    /// <item><see cref="HttpStatusCode.InternalServerError"/> If the result is a SOAP fault.</item>
    /// <item><see cref="HttpStatusCode.BadRequest"/> If the message stream was malformed.</item>
    /// </list>
    /// </returns>
    public static HttpStatusCode HandleRequest(DvService service, Stream messageStream, Encoding streamEncoding,
        bool subscriberSupportsUPnP11, CallContext context, out string result)
    {
      UPnPError res;
      try
      {
        IList<object> inParameterValues = null; // Default to null if there aren't parameters, will be lazily initialized later
        DvAction action;
        using (StreamReader streamReader = new StreamReader(messageStream, streamEncoding))
          using(XmlReader reader = XmlReader.Create(streamReader, Configuration.DEFAULT_XML_READER_SETTINGS))
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
              throw new MediaPortal.Utilities.Exceptions.InvalidDataException("Unable to parse service type and version URN '{0}'", serviceTypeVersion_URN);
            string actionName = reader.LocalName;
            if (!service.Actions.TryGetValue(actionName, out action))
            {
              result = CreateFaultDocument(401, "Invalid Action");
              return HttpStatusCode.InternalServerError;
            }
            IEnumerator<DvArgument> formalArgumentEnumer = action.InArguments.GetEnumerator();
            if (!SoapHelper.ReadEmptyStartElement(reader)) // Action name
              while (reader.NodeType != XmlNodeType.EndElement)
              {
                string argumentName = reader.Name; // Arguments don't have a namespace, so take full name
                if (!formalArgumentEnumer.MoveNext() || formalArgumentEnumer.Current.Name != argumentName)
                { // Too many arguments
                  result = CreateFaultDocument(402, "Invalid Args");
                  return HttpStatusCode.InternalServerError;
                }
                object value;
                if (SoapHelper.ReadNull(reader))
                  value = null;
                else
                {
                  res = formalArgumentEnumer.Current.SoapParseArgument(reader, !subscriberSupportsUPnP11, out value);
                  if (res != null)
                  {
                    result = CreateFaultDocument(res.ErrorCode, res.ErrorDescription);
                    return HttpStatusCode.InternalServerError;
                  }
                }
                if (inParameterValues == null)
                  inParameterValues = new List<object>();
                inParameterValues.Add(value);
              }
            if (formalArgumentEnumer.MoveNext())
            { // Too few arguments
              result = CreateFaultDocument(402, "Invalid Args");
              return HttpStatusCode.InternalServerError;
            }
          }
        IList<object> outParameterValues;
        // Invoke action
        try
        {
          res = action.InvokeAction(inParameterValues, out outParameterValues, false, context);
          // outParameterValues can be null if the action has no output parameters. Setting it to an empty list makes
          // it easier to check parameter count later.
          if (outParameterValues == null)
            outParameterValues = EMPTY_OBJECT_LIST;
        }
        catch (Exception e)
        {
          Configuration.LOGGER.Warn("SOAPHandler: Error invoking UPnP action '{0}'", e, action.Name);
          result = CreateFaultDocument(501, "Action Failed");
          return HttpStatusCode.InternalServerError;
        }
        if (res != null)
        {
          result = CreateFaultDocument(res.ErrorCode, res.ErrorDescription);
          return HttpStatusCode.InternalServerError;
        }
        // Check output parameters
        IList<DvArgument> formalArguments = action.OutArguments;
        if (outParameterValues.Count != formalArguments.Count)
        {
          result = CreateFaultDocument(501, "Action Failed");
          return HttpStatusCode.InternalServerError;
        }
        IList<OutParameter> outParams = new List<OutParameter>();
        for (int i = 0; i < formalArguments.Count; i++)
          outParams.Add(new OutParameter(formalArguments[i], outParameterValues[i]));
        result = CreateResultDocument(action, outParams, !subscriberSupportsUPnP11);
        return HttpStatusCode.OK;
      }
      catch (Exception e)
      {
        Configuration.LOGGER.Warn("Error handling SOAP request: " + e.Message); // Don't log the whole exception; it's only a communication error with a client
        result = null;
        return HttpStatusCode.BadRequest;
      }
    }

    protected static string CreateResultDocument(DvAction action, IList<OutParameter> outParameters, bool forceSimpleValues)
    {
      StringBuilder result = new StringBuilder(2000);
      using (XmlWriter writer = XmlWriter.Create(new StringWriterWithEncoding(result, Encoding.UTF8), Configuration.DEFAULT_XML_WRITER_SETTINGS))
      {
        SoapHelper.WriteSoapEnvelopeStart(writer, true);
        writer.WriteStartElement("u", action.Name + "Response", action.ParentService.ServiceTypeVersion_URN);
        foreach (OutParameter parameter in outParameters)
        {
          writer.WriteStartElement(parameter.Argument.Name);
          parameter.Argument.SoapSerializeArgument(parameter.Value, forceSimpleValues, writer);
          writer.WriteEndElement(); // parameter.Argument.Name
        }
        writer.WriteEndElement(); // u:[action.Name]Response
        SoapHelper.WriteSoapEnvelopeEndAndClose(writer);
      }
      return result.ToString();
    }

    public static string CreateFaultDocument(uint errorCode, string errorDescription)
    {
      StringBuilder result = new StringBuilder(2000);
      using (XmlWriter writer = XmlWriter.Create(new StringWriterWithEncoding(result, Encoding.UTF8), Configuration.DEFAULT_XML_WRITER_SETTINGS))
      {
        SoapHelper.WriteSoapEnvelopeStart(writer, false);
        writer.WriteStartElement("Fault", UPnPConsts.NS_SOAP_ENVELOPE);
        string soapNamespacePrefix = writer.LookupPrefix(UPnPConsts.NS_SOAP_ENVELOPE);
        writer.WriteElementString("faultcode", soapNamespacePrefix + ":Client");
        writer.WriteElementString("faultstring", "UPnPError");
        writer.WriteStartElement("detail");
        writer.WriteStartElement(string.Empty, "UPnPError", UPnPConsts.NS_UPNP_CONTROL);
        writer.WriteElementString("errorCode", errorCode.ToString());
        writer.WriteElementString("errorDescription", errorDescription);
        writer.WriteEndElement(); // UPnPError
        writer.WriteEndElement(); // detail
        writer.WriteEndElement(); // s:Fault
        SoapHelper.WriteSoapEnvelopeEndAndClose(writer);
      }
      return result.ToString();
    }
  }
}
