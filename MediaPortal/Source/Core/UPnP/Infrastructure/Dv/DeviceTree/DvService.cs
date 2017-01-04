#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using UPnP.Infrastructure.Utils;

namespace UPnP.Infrastructure.Dv.DeviceTree
{
  /// <summary>
  /// Delegate type to be used for the server side state variable change event.
  /// </summary>
  /// <param name="stateVariable">State variable which was changed.</param>
  public delegate void StateVariableChangedDlgt(DvStateVariable stateVariable);

  /// <summary>
  /// Base UPnP service class to host all functionality of a UPnP service.
  /// To build special service configurations, either subclasses can be implemented doing the service initialization or
  /// instances of this class can be created and configured from outside.
  /// </summary>
  public class DvService : IDisposable
  {
    protected string _serviceType;
    protected int _serviceTypeVersion;
    protected string _serviceId;
    protected DvDevice _parentDevice = null;
    protected IDictionary<string, DvAction> _actions = new Dictionary<string, DvAction>();
    protected IDictionary<string, DvStateVariable> _stateVariables = new Dictionary<string, DvStateVariable>();

    /// <summary>
    /// Creates a new UPnP service instance at the server (device) side.
    /// </summary>
    /// <param name="serviceType">Type of the new service instance, in the format "schemas-upnp-org:service:[service-type]" or
    /// "vendor-domain:service:[service-type]". Note that in vendor-defined types, all dots in the vendors domain must
    /// be replaced by hyphens.</param>
    /// <param name="serviceTypeVersion">Version of the implemented service type.</param>
    /// <param name="serviceId">Service id in the format "urn:upnp-org:serviceId:[service-id]" (for standard services) or
    /// "urn:domain-name:serviceId:[service-id]" (for vendor-defined service types). The service id is defined by a
    /// UPnP Forum working committee (for standard services) or specified by UPnP vendors.</param>
    public DvService(string serviceType, int serviceTypeVersion, string serviceId)
    {
      _serviceType = serviceType;
      _serviceTypeVersion = serviceTypeVersion;
      _serviceId = serviceId;
    }

    public virtual void Dispose()
    {
      // Sub classes can override this method
    }

    /// <summary>
    /// Gets invoked when one of the state variables of this service has changed.
    /// </summary>
    public event StateVariableChangedDlgt StateVariableChanged;

    /// <summary>
    /// Gets or sets the device which contains this service.
    /// </summary>
    public DvDevice ParentDevice
    {
      get { return _parentDevice; }
      internal set { _parentDevice = value; }
    }

    /// <summary>
    /// Returns the service type, in the format "schemas-upnp-org:service:[service-type]" or
    /// "vendor-domain:service:[service-type]".
    /// </summary>
    public string ServiceType
    {
      get { return _serviceType; }
    }

    /// <summary>
    /// Returns the version of the type of this service.
    /// </summary>
    public int ServiceTypeVersion
    {
      get { return _serviceTypeVersion; }
    }

    /// <summary>
    /// Returns the service type URN with version, in the format "urn:schemas-upnp-org:service:[service-type]:[version]" or
    /// "urn:domain-name:service:[service-type]:[version]".
    /// </summary>
    public string ServiceTypeVersion_URN
    {
      get { return "urn:" + _serviceType + ":" + _serviceTypeVersion; }
    }

    /// <summary>
    /// Returns the service id, in the format "urn:upnp-org:serviceId:[service-id]" (for standard services) or
    /// "urn:domain-name:serviceId:[service-id]".
    /// The service id is specified by the UPnP Forum working committee or the UPnP vendor for the service type.
    /// </summary>
    public string ServiceId
    {
      get { return _serviceId; }
    }

    /// <summary>
    /// Returns a dictionary which maps action names to actions.
    /// </summary>
    public IDictionary<string, DvAction> Actions
    {
      get { return _actions; }
    }

    /// <summary>
    /// Returns a dictionary which maps state variable names to state variables.
    /// </summary>
    public IDictionary<string, DvStateVariable> StateVariables
    {
      get { return _stateVariables; }
    }

    /// <summary>
    /// Returns <c>true</c> if at least one state variable of this service is of an extended data type and doesn't support
    /// a string equivalent.
    /// </summary>
    public bool HasComplexStateVariables
    {
      get
      {
        return _stateVariables.Values.Any(
            stateVariable =>
              {
                DvExtendedDataType edt = stateVariable.DataType as DvExtendedDataType;
                return edt != null && !edt.SupportsStringEquivalent;
              });
      }
    }

    /// <summary>
    /// Returns the information if this service is compatible with the specified service <paramref name="type"/> and
    /// <paramref name="version"/>. A given <paramref name="type"/> and <paramref name="version"/> combination is compatible
    /// if the requested <paramref name="type"/> matches exactly the <see cref="ServiceType"/> and the requested
    /// <paramref name="version"/> is equal or lower than this service's <see cref="ServiceTypeVersion"/>.
    /// </summary>
    /// <param name="type">Type of the service to check.</param>
    /// <param name="version">Version of the service to check.</param>
    /// <returns><c>true</c>, if the specified <paramref name="type"/> is equal to our <see cref="ServiceType"/> and
    /// the specified <paramref name="version"/> is equal or lower than our <see cref="ServiceTypeVersion"/>, else
    /// <c>false</c>.</returns>
    public bool IsCompatible(string type, int version)
    {
      return _serviceType == type && _serviceTypeVersion >= version;
    }

    /// <summary>
    /// Adds the specified <paramref name="action"/> to this service.
    /// </summary>
    /// <remarks>
    /// The actions need to be added in a special order. If this service is a standard service, actions of the standard
    /// service type need to be added first. After that, additional actions might be added.
    /// </remarks>
    /// <param name="action">Action to be added.</param>
    public void AddAction(DvAction action)
    {
      _actions.Add(action.Name, action);
      action.ParentService = this;
    }

    /// <summary>
    /// Adds the specified state <paramref name="variable"/> to this service.
    /// </summary>
    /// <remarks>
    /// The state variables need to be added in a special order. If this service is a standard service, state variables
    /// of the standard service type need to be added first. After that, additional state variables might be added.
    /// </remarks>
    /// <param name="variable">UPnP state variable to add.</param>
    public void AddStateVariable(DvStateVariable variable)
    {
      _stateVariables.Add(variable.Name, variable);
      variable.ParentService = this;
    }

    internal void FireStateVariableChanged(DvStateVariable variable)
    {
      try
      {
        StateVariableChangedDlgt stateVariableChanged = StateVariableChanged;
        if (stateVariableChanged != null)
          stateVariableChanged(variable);
      }
      catch (Exception e)
      {
        UPnPConfiguration.LOGGER.Warn("DvService: Error invoking StateVariableChanged delegate", e);
      }
    }

    #region Description generation

    /// <summary>
    /// Creates the UPnP service description for this service.
    /// </summary>
    /// <param name="config">Endpoint configuration for that the SCPD document should be created.</param>
    /// <param name="serverData">Global server data structure.</param>
    /// <returns>UPnP service description document.</returns>
    public string BuildSCPDDocument(EndpointConfiguration config, ServerData serverData)
    {
      StringBuilder result = new StringBuilder(10000);
      using (StringWriterWithEncoding stringWriter = new StringWriterWithEncoding(result, UPnPConsts.UTF8_NO_BOM))
      using (XmlWriter writer = XmlWriter.Create(stringWriter, UPnPConfiguration.DEFAULT_XML_WRITER_SETTINGS))
      {
        writer.WriteStartDocument();
        writer.WriteStartElement(string.Empty, "scpd", UPnPConsts.NS_SERVICE_DESCRIPTION);
        // Datatype schema namespaces
        uint ct = 0;
        HashSet<string> schemaURIs = new HashSet<string>();
        foreach (DvStateVariable stateVariable in _stateVariables.Values)
        {
          DvDataType dataType = stateVariable.DataType;
          if (dataType is DvExtendedDataType)
          {
            string schemaURI = ((DvExtendedDataType) dataType).SchemaURI;
            if (schemaURIs.Contains(schemaURI))
              continue;
            schemaURIs.Add(schemaURI);
            writer.WriteAttributeString("xmlns", "dt" + ct++, null, schemaURI);
          }
        }
        writer.WriteAttributeString("configId", config.ConfigId.ToString());
        writer.WriteStartElement("specVersion");
        writer.WriteElementString("major", UPnPConsts.UPNP_VERSION_MAJOR.ToString());
        writer.WriteElementString("minor", UPnPConsts.UPNP_VERSION_MINOR.ToString());
        writer.WriteEndElement(); // specVersion

        ICollection<DvAction> actions = _actions.Values;
        if (actions.Count > 0)
        {
          writer.WriteStartElement("actionList");
          foreach (DvAction action in actions)
            action.AddSCPDDescriptionForAction(writer);
          writer.WriteEndElement(); // actionList
        }
        writer.WriteStartElement("serviceStateTable");
        foreach (DvStateVariable stateVariable in _stateVariables.Values)
          stateVariable.AddSCPDDescriptionForStateVariable(writer);
        writer.WriteEndElement(); // serviceStateTable
        writer.WriteEndElement(); // scpd
        writer.WriteEndDocument();
        writer.Close();
      }
      return result.ToString();
    }

    internal void AddDeviceDescriptionForService(XmlWriter writer, EndpointConfiguration config)
    {
      ServicePaths servicePaths = config.ServicePaths[this];

      writer.WriteStartElement("service");
      writer.WriteElementString("serviceType", ServiceTypeVersion_URN);
      writer.WriteElementString("serviceId", _serviceId);
      writer.WriteElementString("SCPDURL", servicePaths.SCPDPath);
      writer.WriteElementString("controlURL", servicePaths.ControlPath);
      writer.WriteElementString("eventSubURL", servicePaths.EventSubPath);
      writer.WriteEndElement(); // service
    }

    #endregion
  }
}
