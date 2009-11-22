#region Copyright (C) 2007-2009 Team MediaPortal

/* 
 *  Copyright (C) 2007-2009 Team MediaPortal
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
using System.Xml;
using System.Xml.XPath;
using MediaPortal.Utilities.Exceptions;
using UPnP.Infrastructure.Common;

namespace UPnP.Infrastructure.CP.DeviceTree
{
  /// <summary>
  /// Delegate which gets called when an event subscription to a service wasn't successful.
  /// </summary>
  /// <param name="service">The service for that the event subscription didn't succeed.</param>
  /// <param name="error">A UPnP error code and error description.</param>
  public delegate void EventSubscriptionFailedDlgt(CpService service, UPnPError error);

  /// <summary>
  /// Delegate to be used for the client side state variable change event.
  /// </summary>
  /// <param name="stateVariable">State variable which was changed.</param>
  public delegate void StateVariableChangedDlgt(CpStateVariable stateVariable);

  /// <summary>
  /// UPnP service template which gets instantiated at the client (control point) side for each service
  /// the control point wants to connect to.
  /// </summary>
  /// <remarks>
  /// Parts of this class are intentionally parallel to the implementation in <see cref="UPnP.Infrastructure.Dv.DeviceTree.DvService"/>.
  /// </remarks>
  public class CpService
  {
    protected CpDevice _parentDevice;
    protected string _serviceType;
    protected int _serviceTypeVersion;
    protected string _serviceId;
    protected EventSubscriptionFailedDlgt _eventSubscriptionFailed;
    protected IDictionary<string, CpAction> _actions = new Dictionary<string, CpAction>();
    protected IDictionary<string, CpStateVariable> _stateVariables = new Dictionary<string, CpStateVariable>();
    protected bool _isOptional = true;
    protected DeviceConnection _connection = null;
    
    /// <summary>
    /// Creates a new UPnP service instance at the client (control point) side.
    /// </summary>
    /// <param name="connection">Device connection instance which attends the connection with the server side.</param>
    /// <param name="parentDevice">Instance of the device which contains the new service.</param>
    /// <param name="serviceType">Type of the service instance, in the format "schemas-upnp-org:service:[service-type]" or
    /// "vendor-domain:service:[service-type]". Note that in vendor-defined types, all dots in the vendors domain are
    /// replaced by hyphens.</param>
    /// <param name="serviceTypeVersion">Version of the implemented service type.</param>
    /// <param name="serviceId">Service id in the format "urn:upnp-org:serviceId:[service-id]" (for standard services) or
    /// "urn:domain-name:serviceId:[service-id]" (for vendor-defined service types).</param>
    public CpService(DeviceConnection connection, CpDevice parentDevice, string serviceType, int serviceTypeVersion, string serviceId)
    {
      _connection = connection;
      _parentDevice = parentDevice;
      _serviceType = serviceType;
      _serviceTypeVersion = serviceTypeVersion;
      _serviceId = serviceId;
    }

    /// <summary>
    /// Gets invoked when one of the state variables of this service has changed. Can be set for concrete service
    /// implementations.
    /// </summary>
    public event StateVariableChangedDlgt StateVariableChanged;

    public EventSubscriptionFailedDlgt EventSubscriptionFailed
    {
      get { return _eventSubscriptionFailed; }
      set { _eventSubscriptionFailed = value; }
    }

    /// <summary>
    /// Gets or sets a flag which controls the control point's matching behaviour.
    /// If <see cref="IsOptional"/> is set to <c>true</c>, the control point will also return devices from the network
    /// which don't implement this service. If this flag is set to <c>false</c>, devices without a service matching this
    /// service template won't be considered as matching devices.
    /// </summary>
    public bool IsOptional
    {
      get { return _isOptional; }
      set { _isOptional = value; }
    }

    /// <summary>
    /// Returns the information if this service template is connected to a matching UPnP service. Will be set by the UPnP system.
    /// </summary>
    public bool IsConnected
    {
      get { return _connection != null; }
    }

    /// <summary>
    /// Returns the device which contains this service.
    /// </summary>
    public CpDevice ParentDevice
    {
      get { return _parentDevice; }
    }

    /// <summary>
    /// Returns the full qualified name of this service in the form "[DeviceName].[ServiceType]:[ServiceVersion]".
    /// </summary>
    public string FullQualifiedName
    {
      get { return _parentDevice.FullQualifiedName + "." + _serviceType + ":" + _serviceTypeVersion; }
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
    /// Returns the service type URN with version, in the format "urn:schemas-upnp-org:service:[service-type]:[version]".
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
    public IDictionary<string, CpAction> Actions
    {
      get { return _actions; }
    }

    /// <summary>
    /// Returns a dictionary which maps state variable names to state variables.
    /// </summary>
    public IDictionary<string, CpStateVariable> StateVariables
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
        foreach (CpStateVariable stateVariable in _stateVariables.Values)
        {
          CpExtendedDataType edt = stateVariable.DataType as CpExtendedDataType;
          if (edt != null && !edt.SupportsStringEquivalent)
            return true;
        }
        return false;
      }
    }

    /// <summary>
    /// Returns the information whether the state variables are subscribed for change events.
    /// See <see cref="DeviceConnection.IsServiceSubscribedForEvents"/> for more hints.
    /// </summary>
    public bool IsStateVariablesSubscribed
    {
      get
      {
        DeviceConnection connection = _connection;
        return connection != null && connection.IsServiceSubscribedForEvents(this);
      }
    }

    /// <summary>
    /// Returns the information if this service is compatible with the specified service <paramref name="type"/> and
    /// <paramref name="version"/>. A given <paramref name="type"/> and <paramref name="version"/> combination is compatible
    /// if the given <paramref name="type"/> matches exactly the <see cref="ServiceType"/> and the given
    /// <paramref name="version"/> is equal or higher than this service's <see cref="ServiceTypeVersion"/>.
    /// </summary>
    /// <param name="type">Type of the service to check.</param>
    /// <param name="version">Version of the service to check.</param>
    /// <returns><c>true</c>, if the specified <paramref name="type"/> is equal to our <see cref="ServiceType"/> and
    /// the specified <paramref name="version"/> is equal or higher than our <see cref="ServiceTypeVersion"/>, else
    /// <c>false</c>.</returns>
    public bool IsCompatible(string type, int version)
    {
      return _serviceType == type && _serviceTypeVersion >= version;
    }

    /// <summary>
    /// Subscribes for change events for all state variables of this service.
    /// </summary>
    /// <remarks>
    /// If the event subscription fails, the delegate <see cref="EventSubscriptionFailed"/> will be called.
    /// </remarks>
    /// <exception cref="IllegalCallException">If the state variables are already subscribed
    /// (see <see cref="IsStateVariablesSubscribed"/>).</exception>
    public void SubscribeStateVariables()
    {
      DeviceConnection connection = _connection;
      if (connection == null)
        throw new IllegalCallException("UPnP service is not connected to a UPnP network service");
      if (connection.IsServiceSubscribedForEvents(this))
        throw new IllegalCallException("State variables are already subscribed");
      connection.OnSubscribeEvents(this);
    }

    /// <summary>
    /// Unsubscribes change events for all state variables of this service.
    /// </summary>
    /// <exception cref="IllegalCallException">If the state variables are not subscribed
    /// (see <see cref="IsStateVariablesSubscribed"/>).</exception>
    public void UnsubscribeStateVariables()
    {
      DeviceConnection connection = _connection;
      if (connection == null)
        throw new IllegalCallException("UPnP service is not connected to a UPnP network service");
      if (!connection.IsServiceSubscribedForEvents(this))
        throw new IllegalCallException("State variables are not subscribed");
      connection.OnUnsubscribeEvents(this);
    }

    internal void InvokeStateVariableChanged(CpStateVariable variable)
    {
      StateVariableChangedDlgt stateVariableChanged = StateVariableChanged;
      if (stateVariableChanged != null)
        stateVariableChanged(variable);
    }

    internal void InvokeEventSubscriptionFailed(UPnPError error)
    {
      if (_eventSubscriptionFailed != null)
        _eventSubscriptionFailed(this, error);
    }

    #region Connection

    /// <summary>
    /// Adds the specified <paramref name="action"/> instance to match to this service template.
    /// </summary>
    /// <param name="action">Action template to be added.</param>
    internal void AddAction(CpAction action)
    {
      _actions.Add(action.Name, action);
    }

    /// <summary>
    /// Adds the specified state <paramref name="variable"/> instance to match to this service template.
    /// </summary>
    /// <param name="variable">UPnP state variable to add.</param>
    internal void AddStateVariable(CpStateVariable variable)
    {
      _stateVariables.Add(variable.Name, variable);
    }

    internal static CpService ConnectService(DeviceConnection connection, CpDevice parentDevice,
        ServiceDescriptor serviceDescriptor, DataTypeResolverDlgt dataTypeResolver)
    {
      lock (connection.CPData.SyncObj)
      {
        CpService result = new CpService(connection, parentDevice, serviceDescriptor.ServiceType, serviceDescriptor.ServiceTypeVersion,
            serviceDescriptor.ServiceId);
        XPathNavigator serviceNav = serviceDescriptor.ServiceDescription.CreateNavigator();
        serviceNav.MoveToChild(XPathNodeType.Element);
        XmlNamespaceManager nsmgr = new XmlNamespaceManager(serviceNav.NameTable);
        nsmgr.AddNamespace("s", UPnPConsts.NS_SERVICE_DESCRIPTION);
        XPathNodeIterator svIt = serviceNav.Select("s:serviceStateTable/s:stateVariable", nsmgr);
        // State variables must be connected first because they are needed from the action's arguments
        while (svIt.MoveNext())
          result.AddStateVariable(CpStateVariable.ConnectStateVariable(connection, result, svIt.Current, nsmgr, dataTypeResolver));
        XPathNodeIterator acIt = serviceNav.Select("s:actionList/s:action", nsmgr);
        while (acIt.MoveNext())
          result.AddAction(CpAction.ConnectAction(connection, result, acIt.Current, nsmgr));
        return result;
      }
    }

    internal void Disconnect()
    {
      DeviceConnection connection = _connection;
      if (connection == null)
        return;
      lock (connection.CPData.SyncObj)
        _connection = null;
    }

    #endregion
  }
}
