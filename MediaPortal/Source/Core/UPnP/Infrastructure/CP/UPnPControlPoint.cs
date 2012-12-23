#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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
using System.Net;
using System.Net.Sockets;
using HttpServer;
using MediaPortal.Utilities.Exceptions;
using UPnP.Infrastructure.CP.DeviceTree;
using UPnP.Infrastructure.Utils;
using HttpListener=HttpServer.HttpListener;

namespace UPnP.Infrastructure.CP
{
  /// <summary>
  /// UPnP control point managing connected <see cref="CpDevice"/>s.
  /// </summary>
  /// <remarks>
  /// To create a UPnP control point, the following sequence should be executed:
  /// <example>
  /// <code>
  /// CPData cpData = new CPData();
  /// UPnPNetworkTracker networkTracker = new UPnPNetworkTracker(cpData);
  /// UPnPControlPoint controlPoint = new UPnPControlPoint(networkTracker);
  /// networkTracker.RootDeviceAdded += OnUPnPRootDeviceAdded;
  /// controlPoint.Start(); // Start the control point before the network tracker to catch all device appearances
  /// networkTracker.Start();
  /// [... run the application ...]
  /// networkTracker.Close();
  /// controlPoint.Close();
  /// </code>
  /// </example>
  /// To react to an upcoming availability of a new UPnP device in the network, in the example method
  /// <c>OnUPnPRootDeviceAdded</c> is attached to the <see cref="UPnPNetworkTracker.RootDeviceAdded"/> event of the
  /// <see cref="NetworkTracker"/> property.
  /// Here is an example of an event handler for the event <see cref="UPnPNetworkTracker.RootDeviceAdded"/>:
  /// <example>
  /// <code>
  /// void OnUPnPRootDeviceAdded(RootDescriptor rootDescriptor)
  /// {
  ///   // TODO: Check if the given root descriptor contains a device which should be connected by this
  ///   // control point. If not, ignore the event by simply returning from this method. Else determine
  ///   // the device UUID and connect to that device:
  ///   DeviceConnection connection = controlPoint.Connect(rootDescriptor, deviceUuid, OnResolveDataType);
  ///   // TODO: Now configure the connected device which is available via connection.Device.
  ///   // Set the ActionResult and ActionErrorResult delegates for each service which contains actions to be
  ///   // called. Add event handlers for StateVariableChanged in services where necessary.
  /// }
  /// </code>
  /// </example>
  /// </remarks>
  public class UPnPControlPoint : IDisposable
  {
    /// <summary>
    /// Size of the queue which holds open HTTP requests before they are evaluated.
    /// </summary>
    public static int DEFAULT_HTTP_REQUEST_QUEUE_SIZE = 5;

    protected HttpListener _httpListenerV4 = null;
    protected HttpListener _httpListenerV6 = null;
    protected bool _isActive = false;
    protected IDictionary<string, DeviceConnection> _connectedDevices = new Dictionary<string, DeviceConnection>();
    protected CPData _cpData;
    protected UPnPNetworkTracker _networkTracker;

    /// <summary>
    /// Creates a new instance of <see cref="UPnPControlPoint"/>.
    /// </summary>
    /// <remarks>
    /// The specified <paramref name="networkTracker"/> must be controlled independently from this control point, i.e.
    /// it must be explicitly started and stopped. See the docs of this class for an example code sequence.
    /// </remarks>
    /// <param name="networkTracker">Network tracker instance used to collect UPnP network device descriptions.</param>
    public UPnPControlPoint(UPnPNetworkTracker networkTracker)
    {
      _cpData = networkTracker.SharedControlPointData;
      _networkTracker = networkTracker;
    }

    /// <summary>
    /// Disposes this <see cref="UPnPControlPoint"/> and revokes all event registrations at all connected servers.
    /// </summary>
    public void Dispose()
    {
      Close();
    }

    #region Event handlers

    private void OnHttpListenerRequestReceived(object sender, RequestEventArgs e)
    {
      IHttpClientContext context = (IHttpClientContext) sender;
      lock (_cpData.SyncObj)
      {
        if (!_isActive)
          return;
        HandleHTTPRequest(context, e.Request);
      }
    }

    private void OnDeviceRebooted(RootDescriptor rootdescriptor)
    {
      foreach (DeviceConnection connection in _connectedDevices.Values)
        if (connection.RootDescriptor == rootdescriptor)
          connection.OnDeviceRebooted();
    }

    private void OnRootDeviceRemoved(RootDescriptor rootdescriptor)
    {
      DoDisconnect(rootdescriptor.SSDPRootEntry.RootDeviceUUID, false);
    }

    #endregion

    /// <summary>
    /// Data which is shared among all components of the control point system.
    /// </summary>
    public CPData SharedControlPointData
    {
      get { return _cpData; }
    }

    /// <summary>
    /// Returns the network tracker used for this control point.
    /// </summary>
    public UPnPNetworkTracker NetworkTracker
    {
      get { return _networkTracker; }
    }

    /// <summary>
    /// Stores a collection of connected devices.
    /// The returned dictionary maps the device UUIDs to device instances.
    /// </summary>
    public IDictionary<string, DeviceConnection> ConnectedDevices
    {
      get { return _connectedDevices; }
    }

    /// <summary>
    /// Returns the information whether this UPnP control point is active, i.e. it can be connected and it listens for
    /// HTTP messages with event notifications and for disconnection of the conntected device.
    /// </summary>
    public bool IsActive
    {
      get { return _isActive; }
    }

    /// <summary>
    /// Starts this UPnP control point. All device templates should be configured at the time this method gets called.
    /// The network tracker must be started after this method is called, else we might miss some connect events.
    /// </summary>
    public void Start()
    {
      lock (_cpData.SyncObj)
      {
        if (_isActive)
          throw new IllegalCallException("UPnP control point mustn't be started multiple times");

        if (UPnPConfiguration.USE_IPV4)
        {
          _httpListenerV4 = HttpListener.Create(IPAddress.Any, 0);
          _httpListenerV4.RequestReceived += OnHttpListenerRequestReceived;
          try
          {
            _httpListenerV4.Start(DEFAULT_HTTP_REQUEST_QUEUE_SIZE); // Might fail if IPv4 isn't installed
            _cpData.HttpPortV4 = _httpListenerV4.LocalEndpoint.Port;
          }
          catch (SocketException e)
          {
            _httpListenerV4 = null;
            _cpData.HttpPortV4 = 0;
            UPnPConfiguration.LOGGER.Warn("UPnPControlPoint: Error starting HTTP server (IPv4)", e);
          }
        }
        else
        {
          _httpListenerV4 = null;
          _cpData.HttpPortV4 = 0;
        }
        if (UPnPConfiguration.USE_IPV6)
        {
          _httpListenerV6 = HttpListener.Create(IPAddress.IPv6Any, 0);
          _httpListenerV6.RequestReceived += OnHttpListenerRequestReceived;
          try
          {
            _httpListenerV6.Start(DEFAULT_HTTP_REQUEST_QUEUE_SIZE); // Might fail if IPv6 isn't installed
            _cpData.HttpPortV6 = _httpListenerV6.LocalEndpoint.Port;
          }
          catch (SocketException e)
          {
            _httpListenerV6 = null;
            _cpData.HttpPortV6 = 0;
            UPnPConfiguration.LOGGER.Warn("UPnPControlPoint: Error starting HTTP server (IPv6)", e);
          }
        }
        else
        {
          _httpListenerV6 = null;
          _cpData.HttpPortV6 = 0;
        }
        _networkTracker.RootDeviceRemoved += OnRootDeviceRemoved;
        _networkTracker.DeviceRebooted += OnDeviceRebooted;

        _isActive = true;
      }
    }

    /// <summary>
    /// Closes this control point. This will first disconnect all connected devices and then release all lower-level UPnP
    /// protocol layers.
    /// The network tracker must be closed before this method is called.
    /// </summary>
    public void Close()
    {
      ICollection<HttpListener> listenersToClose = new List<HttpListener>();
      lock (_cpData.SyncObj)
      {
        if (!_isActive)
          return;
        _isActive = false;
        if (_httpListenerV4 != null)
        {
          listenersToClose.Add(_httpListenerV4);
          _httpListenerV4 = null;
        }
        _httpListenerV4 = null;
        if (_httpListenerV6 != null)
        {
          listenersToClose.Add(_httpListenerV6);
          _httpListenerV6 = null;
        }
        _networkTracker.RootDeviceRemoved -= OnRootDeviceRemoved;
        _networkTracker.DeviceRebooted -= OnDeviceRebooted;
      }
      // Outside the lock
      DisconnectAll();
      foreach (HttpListener httpListener in listenersToClose)
        httpListener.Stop();
    }

    /// <summary>
    /// Connects to the device of the given <paramref name="deviceUuid"/> specified in the <paramref name="rootDescriptor"/>.
    /// </summary>
    /// <param name="rootDescriptor">UPnP root descriptor to connect.</param>
    /// <param name="deviceUuid">UUID of the device in the root descriptor which is the node to connect.</param>
    /// <param name="dataTypeResolver">Delegate method to resolve extended datatypes.</param>
    /// <exception cref="ArgumentException">
    /// <list type="bullet">
    /// <item>If the device with the specified <paramref name="deviceUuid"/> isn't present
    /// in the given <paramref name="rootDescriptor"/></item>
    /// <item>If the given <paramref name="rootDescriptor"/> has is in an erroneous state
    /// (<c><see cref="RootDescriptor.State"/> == <see cref="RootDescriptorState.Erroneous"/></c>)</item>
    /// <item>If the given <paramref name="rootDescriptor"/> contains erroneous data, e.g. erroneous device or
    /// service descriptions</item>
    /// </list>
    /// </exception>
    public DeviceConnection Connect(RootDescriptor rootDescriptor, string deviceUuid, DataTypeResolverDlgt dataTypeResolver)
    {
      return DoConnect(rootDescriptor, deviceUuid, dataTypeResolver);
    }

    /// <summary>
    /// Disconnects the connected device of the given <paramref name="deviceUUID"/>.
    /// </summary>
    /// <param name="deviceUUID">UUID of the (connected) device.</param>
    public void Disconnect(string deviceUUID)
    {
      DoDisconnect(deviceUUID, true);
    }

    /// <summary>
    /// Disconnects the connected device specified by the given <paramref name="connection"/> instance.
    /// </summary>
    /// <param name="connection">Connection instance to disconnect. Must be maintained by this instance, i.e. must have been
    /// returned by method <see cref="Connect"/> of this instance.</param>
    public void Disconnect(DeviceConnection connection)
    {
      DoDisconnect(connection, true);
    }

    /// <summary>
    /// Disconnects all connected devices.
    /// </summary>
    public void DisconnectAll()
    {
      ICollection<string> connectedUUIDs;
      lock (_cpData.SyncObj)
        connectedUUIDs = new List<string>(_connectedDevices.Keys);
      foreach (string deviceUUID in connectedUUIDs)
        DoDisconnect(deviceUUID, true);
    }

    #region Private/protected methods

    protected DeviceConnection DoConnect(RootDescriptor descriptor, string deviceUuid, DataTypeResolverDlgt dataTypeResolver)
    {
      lock (_cpData.SyncObj)
      {
        DeviceConnection connection = new DeviceConnection(this, descriptor, deviceUuid, _cpData, dataTypeResolver);
        _connectedDevices.Add(deviceUuid, connection);
        return connection;
      }
    }

    protected void DoDisconnect(string deviceUUID, bool unsubscribeEvents)
    {
      DeviceConnection connection;
      lock (_cpData.SyncObj)
      {
        if (!_connectedDevices.TryGetValue(deviceUUID, out connection))
          return;
        _connectedDevices.Remove(deviceUUID);
      }
      connection.DoDisconnect(unsubscribeEvents);
      connection.Dispose();
    }

    protected void DoDisconnect(DeviceConnection connection, bool unsubscribeEvents)
    {
      lock (_cpData.SyncObj)
      {
        string deviceUUID = connection.DeviceUUID;
        if (!_connectedDevices.ContainsKey(deviceUUID))
          throw new ArgumentException(string.Format("This control point instance doesn't manage the given device connection for device '{0}'",
              connection.DeviceUUID));
        DoDisconnect(deviceUUID, unsubscribeEvents);
      }
    }

    protected void HandleHTTPRequest(IHttpClientContext context, IHttpRequest request)
    {
      Uri uri = request.Uri;
      string hostName = uri.Host;
      string pathAndQuery = uri.PathAndQuery;
      try
      {
        // Handle different HTTP methods here
        if (request.Method == "NOTIFY")
        {
          foreach (DeviceConnection connection in _connectedDevices.Values)
          {
            if (!NetworkHelper.HostNamesEqual(hostName,
                NetworkHelper.IPAddrToHostName(connection.GENAClientController.EventNotificationEndpoint.Address)))
              continue;
            if (pathAndQuery == connection.GENAClientController.EventNotificationPath)
            {
              IHttpResponse response = request.CreateResponse(context);
              response.Status = connection.GENAClientController.HandleUnicastEventNotification(request);
              response.Send();
              return;
            }
          }
        }
        else
        {
          context.Respond(HttpHelper.HTTP11, HttpStatusCode.MethodNotAllowed, null);
          return;
        }
        // Url didn't match
        context.Respond(HttpHelper.HTTP11, HttpStatusCode.NotFound, null);
      }
      catch (Exception) // Don't log the exception here - we don't care about not being able to send the return value to the client
      {
        IHttpResponse response = request.CreateResponse(context);
        response.Status = HttpStatusCode.InternalServerError;
        response.Send();
      }
    }

    #endregion
  }
}
