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
    /// <param name="networkTracker">Network tracker instance used to collect UPnP network device descriptions.</param>
    /// <param name="cpData">Shared control point data instance.</param>
    public UPnPControlPoint(UPnPNetworkTracker networkTracker, CPData cpData)
    {
      _cpData = cpData;
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
    public void Connect(RootDescriptor rootDescriptor, string deviceUuid, DataTypeResolverDlgt dataTypeResolver)
    {
      DoConnect(rootDescriptor, deviceUuid, dataTypeResolver);
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
    /// Disconnects all connected devices.
    /// </summary>
    public void DisconnectAll()
    {
      foreach (KeyValuePair<string, DeviceConnection> kvp in _connectedDevices)
        DoDisconnect(kvp.Key, true);
    }

    #region Private/protected methods

    protected void DoConnect(RootDescriptor descriptor, string deviceUuid, DataTypeResolverDlgt dataTypeResolver)
    {
      lock (_cpData.SyncObj)
      {
        DeviceConnection connection = new DeviceConnection(descriptor, deviceUuid, _cpData, dataTypeResolver);
        _connectedDevices.Add(deviceUuid, connection);
      }
    }

    protected void DoDisconnect(string deviceUUID, bool unsubscribeEvents)
    {
      lock (_cpData.SyncObj)
      {
        DeviceConnection connection;
        if (!_connectedDevices.TryGetValue(deviceUUID, out connection))
          return;
        connection.Disconnect(unsubscribeEvents);
        connection.Dispose();
        _connectedDevices.Remove(deviceUUID);
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
          { // GET of descriptions
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
      catch (Exception)
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
