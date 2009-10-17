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

using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Xml.XPath;
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
    protected class Parameter
    {
      protected DvArgument _argument;
      protected string _xmlValue;

      public Parameter(DvArgument argument, string xmlValue)
      {
        _argument = argument;
        _xmlValue = xmlValue;
      }

      public DvArgument Argument
      {
        get { return _argument; }
      }

      public string XMLValue
      {
        get { return _xmlValue; }
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
        bool subscriberSupportsUPnP11, out string result)
    {
      result = null;
      // Parse XML request
      XPathDocument doc;
      try
      {
        doc = new XPathDocument(new StreamReader(messageStream, streamEncoding));
      }
      catch (XPathException)
      {
        return HttpStatusCode.BadRequest;
      }
      XPathNavigator soapEnvelopeNav = doc.CreateNavigator();
      soapEnvelopeNav.MoveToChild(XPathNodeType.Element);
      XPathNavigator body;
      // Parse SOAP envelope
      if (!ParserHelper.UnwrapSoapEnvelopeElement(soapEnvelopeNav, out body))
        return HttpStatusCode.BadRequest;
      XPathNavigator actionNav = body.Clone();
      if (!actionNav.MoveToChild(XPathNodeType.Element))
        return HttpStatusCode.BadRequest;
      string serviceTypeVersion_URN = actionNav.NamespaceURI;
      string type;
      int version;
      // Parse service and action
      if (!ParserHelper.TryParseTypeVersion_URN(serviceTypeVersion_URN, out type, out version))
        return HttpStatusCode.BadRequest;
      string actionName = actionNav.LocalName;
      DvAction action;
      if (!service.Actions.TryGetValue(actionName, out action))
      {
        result = CreateFaultDocument(401, "Invalid Action");
        return HttpStatusCode.InternalServerError;
      }
      // Parse and check input parameters
      IList<DvArgument> formalArguments = action.InArguments;
      IList<object> inParameterValues = new List<object>();
      XPathNodeIterator parameterIt = actionNav.SelectChildren(XPathNodeType.Element);
      if (formalArguments.Count != parameterIt.Count)
      {
        result = CreateFaultDocument(402, "Invalid Args");
        return HttpStatusCode.InternalServerError;
      }
      UPnPError res;
      for (int i = 0; parameterIt.MoveNext(); i++)
      {
        DvArgument argument = formalArguments[i];
        XPathNavigator parameterNav = parameterIt.Current;
        if (parameterNav.LocalName != argument.Name)
        {
          result = CreateFaultDocument(402, "Invalid Args");
          return HttpStatusCode.InternalServerError;
        }
        object value;
        res = argument.SoapParseArgument(parameterNav, !subscriberSupportsUPnP11, out value);
        if (res != null)
        {
          result = CreateFaultDocument(res.ErrorCode, res.ErrorDescription);
          return HttpStatusCode.InternalServerError;
        }
        inParameterValues.Add(value);
      }
      IList<object> outParameterValues;
      // Invoke action
      res = action.InvokeAction(inParameterValues, out outParameterValues, false);
      if (res != null)
      {
        result = CreateFaultDocument(res.ErrorCode, res.ErrorDescription);
        return HttpStatusCode.InternalServerError;
      }
      // Check output parameters
      formalArguments = action.OutArguments;
      if (outParameterValues.Count != formalArguments.Count)
      {
        result = CreateFaultDocument(501, "Action Failed");
        return HttpStatusCode.InternalServerError;
      }
      IList<Parameter> outParams = new List<Parameter>();
      for (int i = 0; i < formalArguments.Count; i++)
      {
        DvArgument argument = formalArguments[i];
        object value = outParameterValues[i];
        string serializedValue;
        res = argument.SoapSerializeArgument(value, !subscriberSupportsUPnP11, out serializedValue);
        if (res != null)
        {
          result = CreateFaultDocument(501, "Action Failed");
          return HttpStatusCode.InternalServerError;
        }
        outParams.Add(new Parameter(argument, serializedValue));
      }
      result = CreateResultDocument(action, outParams);
      return HttpStatusCode.OK;
    }

    protected static string CreateResultDocument(DvAction action, IList<Parameter> outParameters)
    {
      StringBuilder sb = new StringBuilder(
          "<?xml version=\"1.0\"?>" +
              "<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/ " +
                  "s:encodingStyle=\"http://schemas.xmlsoap.org./soap/encoding/\">" +
                "<s:Body>" +
                  "<u:");
      sb.Append(action.Name);
      sb.Append("Response xmlns:u=\"");
      sb.Append(action.ParentService.ServiceTypeVersion_URN);
      sb.Append("\">");
      foreach (Parameter parameter in outParameters)
      {
        sb.Append("<");
        sb.Append(parameter.Argument.Name);
        sb.Append(">");
        sb.Append(parameter.XMLValue);
        sb.Append("</");
        sb.Append(parameter.Argument.Name);
        sb.Append(">");
      }
      sb.Append(
                  "</u:");
      sb.Append(action.Name);
      sb.Append("Response>" +
                "</s:Body" +
              "</s:Envelope>");
      return sb.ToString();
    }

    protected static string CreateFaultDocument(int errorCode, string errorDescription)
    {
      StringBuilder sb = new StringBuilder(
          "<?xml version=\"1.0\"?>" +
            "<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/ " +
                "s:encodingStyle=\"http://schemas.xmlsoap.org./soap/encoding/\">" +
              "<s:Body>" +
                "<s:Fault>" +
                  "<faultcode>x:Client</faultcode>" +
                  "<faultstring>UPnPError</faultstring>" +
                  "<detail>" +
                    "<UPnPError xmlns=\"urn:schemas-upnp-org:control-1-0\">" +
                      "<errorCode>", 10000);
      sb.Append(
                        errorCode);
      sb.Append(
                      "</errorCode>" +
                      "<errorDescription>");
      sb.Append(
                        errorDescription);
      sb.Append(
                      "</errorDescription>" +
                    "</UPnPError>" +
                  "</detail>" +
                "</s:Fault>" +
              "</s:Body>" +
            "</s:Envelope>");
      return sb.ToString();
    }
  }
}
