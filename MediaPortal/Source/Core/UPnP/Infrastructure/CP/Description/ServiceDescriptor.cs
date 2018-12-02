#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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

using System.Xml.XPath;

namespace UPnP.Infrastructure.CP.Description
{
  /// <summary>
  /// State enumeration for the <see cref="ServiceDescriptor"/>.
  /// </summary>
  public enum ServiceDescriptorState
  {
    /// <summary>
    /// The UPnP system is initializing the service descriptor.
    /// </summary>
    Initializing,

    /// <summary>
    /// The UPnP system is just requesting the service description document for the corresponding service.
    /// </summary>
    AwaitingDescription,

    /// <summary>
    /// The corresponding UPnP network service is present in the network ready to be used. The service
    /// might already be connected to some service templates.
    /// </summary>
    Ready,

    /// <summary>
    /// The corresponding UPnP network service is not present in the network any more.
    /// </summary>
    Invalid,

    /// <summary>
    /// There was an unrecoverable error when communicating with the UPnP network service, in any of the communication
    /// protocol layers (for example a description document is erroneous, etc.).
    /// </summary>
    Erroneous,
  }

  /// <summary>
  /// Descriptor which aggregates information about a UPnP service from both the device description document and the
  /// service description document. It also contains a <see cref="State"/>.
  /// </summary>
  public class ServiceDescriptor
  {
    protected RootDescriptor _rootDescriptor;
    protected string _serviceType;
    protected int _serviceTypeVersion;
    protected string _serviceId;
    protected string _descriptionURL;
    protected string _controlURL;
    protected string _eventSubURL;
    protected XPathDocument _serviceDescription = null;
    protected ServiceDescriptorState _state = ServiceDescriptorState.Initializing;

    internal ServiceDescriptor(RootDescriptor rootDescriptor, string serviceType, int serviceTypeVersion, string serviceId,
        string descriptionURL, string controlURL, string eventSubURL)
    {
      _rootDescriptor = rootDescriptor;
      _serviceType = serviceType;
      _serviceTypeVersion = serviceTypeVersion;
      _serviceId = serviceId;
      _descriptionURL = descriptionURL;
      _controlURL = controlURL;
      _eventSubURL = eventSubURL;
    }

    /// <summary>
    /// Gets or sets the state of this service descriptor. The state provides the information whether the setup of this
    /// descriptor is already finished, whether there were errors while communicating with the UPnP server and some
    /// others. See <see cref="ServiceDescriptorState"/> for a complete list of states.
    /// </summary>
    public ServiceDescriptorState State
    {
      get { return _state; }
      internal set { _state = value; }
    }

    /// <summary>
    /// Gets the root descriptor which contains this service descriptor.
    /// </summary>
    public RootDescriptor RootDescriptor
    {
      get { return _rootDescriptor; }
    }

    /// <summary>
    /// Gets the UPnP service type of the corresponding UPnP service.
    /// </summary>
    public string ServiceType
    {
      get { return _serviceType; }
    }

    /// <summary>
    /// Gets the UPnP service type version of the corresponding UPnP service.
    /// </summary>
    public int ServiceTypeVersion
    {
      get { return _serviceTypeVersion; }
    }

    /// <summary>
    /// Gets the UPnP service ID of the corresponding UPnP service.
    /// </summary>
    public string ServiceId
    {
      get { return _serviceId; }
    }

    /// <summary>
    /// Gets the URL where the service description can be found.
    /// </summary>
    public string DescriptionURL
    {
      get { return _descriptionURL; }
    }

    /// <summary>
    /// Gets the URL for the control protocol for the corresponding UPnP service which was provided by the device description.
    /// </summary>
    public string ControlURL
    {
      get { return _controlURL; }
    }

    /// <summary>
    /// Gets the URL for event subscriptions for the corresponding UPnP service which was provided by the device description.
    /// </summary>
    public string EventSubURL
    {
      get { return _eventSubURL; }
    }

    /// <summary>
    /// Gets or sets the service description document.
    /// </summary>
    /// <remarks>
    /// The description is present when the service descriptor is in the <see cref="State"/>
    /// <see cref="RootDescriptorState.Ready"/>.
    /// </remarks>
    public XPathDocument ServiceDescription
    {
      get { return _serviceDescription; }
      internal set { _serviceDescription = value; }
    }

    /// <summary>
    /// Returns the service type URN with version, in the format "urn:schemas-upnp-org:service:[service-type]:[version]".
    /// </summary>
    public string ServiceTypeVersion_URN
    {
      get { return "urn:" + _serviceType + ":" + _serviceTypeVersion; }
    }
  }
}