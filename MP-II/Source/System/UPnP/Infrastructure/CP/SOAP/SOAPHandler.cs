using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
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
    /// XML namespace to be used for the SOAP envelope.
    /// </summary>
    public const string NS_SOAP_ENVELOPE = "http://schemas.xmlsoap.org/soap/envelope/";

    protected class Parameter
    {
      protected CpArgument _argument;
      protected string _xmlValue;

      public Parameter(CpArgument argument, string xmlValue)
      {
        _argument = argument;
        _xmlValue = xmlValue;
      }

      public CpArgument Argument
      {
        get { return _argument; }
      }

      public string XMLValue
      {
        get { return _xmlValue; }
      }
    }

    /// <summary>
    /// Encodes a call of the specified <paramref name="action"/> with the given <paramref name="inParamValues"/> and
    /// returns the resulting SOAP XML string.
    /// </summary>
    /// <param name="action">Action to be called.</param>
    /// <param name="inParamValues">List of parameter values which must match the action's signature.</param>
    /// <param name="upnpVersion">UPnP version to use for the encoding.</param>
    /// <returns>XML string which contains the SOAP document.</returns>
    public static string EncodeCall(CpAction action, IList<object> inParamValues,
        UPnPVersion upnpVersion)
    {
      bool targetSupportsUPnP11 = upnpVersion.VerMin >= 1;
      // Check output parameters
      IList<CpArgument> formalArguments = action.InArguments;
      if (inParamValues.Count != formalArguments.Count)
        throw new ArgumentException("Invalid argument count");
      IList<Parameter> inParams = new List<Parameter>();
      for (int i = 0; i < formalArguments.Count; i++)
      {
        CpArgument argument = formalArguments[i];
        object value = inParamValues[i];
        string serializedValue;
        argument.SoapSerializeArgument(value, !targetSupportsUPnP11, out serializedValue);
        inParams.Add(new Parameter(argument, serializedValue));
      }
      return CreateCallDocument(action, inParams);
    }

    /// <summary>
    /// Takes the XML document provided by the given <paramref name="reader"/> instance, parses it and provides
    /// the action result to the appropriate receiver.
    /// </summary>
    /// <param name="reader">Text reader which contains the SOAP XML action result message.</param>
    /// <param name="action">Action which was called before.</param>
    /// <param name="clientState">State object which was given in the action call and which will be returned to the client.</param>
    /// <param name="upnpVersion">UPnP version of the UPnP server.</param>
    public static void HandleResult(TextReader reader, CpAction action, object clientState, UPnPVersion upnpVersion)
    {
      bool sourceSupportsUPnP11 = upnpVersion.VerMin >= 1;
      CpService service = action.ParentService;
      IList<object> outParameterValues = new List<object>();
      try
      {
        // Parse XML document
        XmlDocument doc = new XmlDocument();
        doc.Load(reader);
        XmlElement soapEnvelope = doc.DocumentElement;
        XmlElement body;
        // Parse SOAP envelope
        if (!UnwrapEnvelopeElement(soapEnvelope, out body))
          throw new ArgumentException("Invalid SOAP envelope");
        XmlElement xml_action = (XmlElement) body.SelectSingleNode("*");
        string serviceTypeVersion_URN = xml_action.NamespaceURI;
        string type;
        int version;
        // Parse service and action
        if (!ParserHelper.TryParseTypeVersion_URN(serviceTypeVersion_URN, out type, out version) ||
            service.ServiceType != type || version < service.ServiceTypeVersion)
          throw new ArgumentException("Invalid service type or version");
        if (!xml_action.Name.EndsWith("Response") ||
            xml_action.Name.Substring(0, xml_action.Name.Length - "Response".Length) != action.Name)
          throw new ArgumentException("Invalid action name in result message");
        // Parse and check output parameters
        IList<CpArgument> formalArguments = action.OutArguments;
        XmlNodeList xmlParameters = xml_action.SelectNodes("*");
        if (formalArguments.Count != xmlParameters.Count)
          throw new ArgumentException("Invalid out argument count");
        for (int i = 0; i < formalArguments.Count; i++)
        {
          CpArgument argument = formalArguments[i];
          XmlElement element = (XmlElement) xmlParameters[i++];
          if (element.Name != argument.Name)
            throw new ArgumentException("Invalid argument name");
          object value;
          argument.SoapParseArgument(element, !sourceSupportsUPnP11, out value);
          outParameterValues.Add(value);
        }
      }
      catch (Exception)
      {
        // TODO Albert: In the current state of the (DevArch) document, the UPnP action error codes 613-699
        // are TBD. I guess we should use one of them instead of 501 (Action failed) here.
        service.InvokeActionErrorResult(action, new UPnPError(501, "Invalid action result"), clientState);
      }
      try
      {
        // Invoke action result
        service.InvokeActionResult(action, outParameterValues, clientState);
      }
      catch (Exception e)
      {
        ServiceScope.Get<ILogger>().Warn("UPnP subsystem: Error invoking action '{0}'", e, action.FullQualifiedName);
      }
    }

    protected static string CreateCallDocument(CpAction action, IList<Parameter> inParameters)
    {
      StringBuilder sb = new StringBuilder(
          "<?xml version=\"1.0\"?>" +
              "<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/ " +
                  "s:encodingStyle=\"http://schemas.xmlsoap.org./soap/encoding/\">" +
                "<s:Body>" +
                  "<u:");
      sb.Append(action.Name);
      sb.Append(" xmlns:u=\"");
      sb.Append(action.ParentService.ServiceTypeVersion_URN);
      sb.Append("\">");
      foreach (Parameter parameter in inParameters)
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

    protected static bool UnwrapEnvelopeElement(XmlElement soapEnvelope, out XmlElement body)
    {
      body = null;
      if (soapEnvelope == null || soapEnvelope.Name != "Envelope" || soapEnvelope.NamespaceURI != NS_SOAP_ENVELOPE)
        return false;
      XmlElement ele = (XmlElement) soapEnvelope.SelectSingleNode("Body");
      if (ele.NamespaceURI != NS_SOAP_ENVELOPE)
        return false;
      body = ele;
      return true;
    }
  }
}
