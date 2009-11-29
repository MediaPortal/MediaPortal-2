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

using System;
using System.Collections.Generic;
using System.Net;
using HttpServer;
using MediaPortal.Utilities.Exceptions;
using UPnP.Infrastructure.CP.DeviceTree;
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

    protected HttpListener _httpListener = null;
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
      DoDisconnect(rootdescriptor.SSDPRootEntry.RootDeviceID, false);
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
    /// </summary>
    public void Start()
    {
      lock (_cpData.SyncObj)
      {
        if (_isActive)
          throw new IllegalCallException("UPnP control point mustn't be started multiple times");

        _httpListener = HttpListener.Create(IPAddress.Any, 0);
        _httpListener.RequestReceived += OnHttpListenerRequestReceived;
        _httpListener.Start(DEFAULT_HTTP_REQUEST_QUEUE_SIZE);
        _cpData.HttpPort = (uint) _httpListener.LocalEndpoint.Port;
        _networkTracker.RootDeviceRemoved += OnRootDeviceRemoved;
        _networkTracker.DeviceRebooted += OnDeviceRebooted;

        _isActive = true;
      }
    }

    /// <summary>
    /// Closes this control point. This will first disconnect all connected devices and then release all lower-level UPnP
    /// protocol layers.
    /// </summary>
    public void Close()
    {
      lock (_cpData.SyncObj)
      {
        if (!_isActive)
          return;
        _isActive = false;

        DisconnectAll();
        _httpListener.Stop();
        _httpListener = null;
        _networkTracker.RootDeviceRemoved -= OnRootDeviceRemoved;
        _networkTracker.DeviceRebooted -= OnDeviceRebooted;
      }
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
      foreach (string deviceUUID in new List<string>(_connectedDevices.Keys))
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
      lock (_cpData.SyncObj)
      {
        DeviceConnection connection;
        if (!_connectedDevices.TryGetValue(deviceUUID, out connection))
          return;
        connection.DoDisconnect(unsubscribeEvents);
        connection.Dispose();
        _connectedDevices.Remove(deviceUUID);
      }
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
      try
      {
        string uri = request.Uri.AbsoluteUri;
        foreach (DeviceConnection connection in _connectedDevices.Values)
        {
          // Handle different HTTP methods here
          if (request.Method == "NOTIFY")
          {
            if (uri.StartsWith(connection.EventNotificationURL))
            {
              IHttpResponse response = request.CreateResponse(context);
              response.Status = connection.HandleEventNotification(request);
              response.Send();
              return;
            }
          }
          else
          {
            context.Respond(HttpHelper.HTTP11, HttpStatusCode.MethodNotAllowed, null);
            return;
          }
        }
        // Url didn't match
        context.Respond(HttpHelper.HTTP11, HttpStatusCode.NotFound, null);
        return;
      }
      catch (Exception) // Don't log the exception here - we don't care about not being able to send the return value to the client
      {
        IHttpResponse response = request.CreateResponse(context);
        response.Status = HttpStatusCode.InternalServerError;
        response.Send();
        return;
      }
    }

    #endregion
  }
}
