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
using System.IO;
using System.Net;
using System.Text;
using MediaPortal.Utilities.Exceptions;
using MediaPortal.Utilities.Network;
using UPnP.Infrastructure.Common;
using UPnP.Infrastructure.CP.Description;
using UPnP.Infrastructure.CP.DeviceTree;
using UPnP.Infrastructure.CP.GENA;
using UPnP.Infrastructure.CP.SOAP;
using UPnP.Infrastructure.CP.SSDP;
using UPnP.Infrastructure.Utils;

namespace UPnP.Infrastructure.CP
{
  /// <summary>
  /// Delegate which is used to retrieve unknown extended data type instances.
  /// </summary>
  /// <param name="dataTypeName">Name of the data type to retrieve, in the form
  /// "urn:domain-name:schema-name:datatype-name".</param>
  /// <param name="dataType">If the return value is <c>true</c>, this parameter will contain the resolved data type instance.
  /// Else, this paramter is undefined.</param>
  /// <returns><c>true</c>, if the data type could be resolved, else <c>false</c>.</returns>
  public delegate bool DataTypeResolverDlgt(string dataTypeName, out UPnPExtendedDataType dataType);

  /// <summary>
  /// Contains the control point connection data of a device template to a network UPnP device.
  /// </summary>
  public class DeviceConnection : IDisposable
  {
    /// <summary>
    /// Delegate which is used to notify the disconnect event of a <see cref="DeviceConnection"/>.
    /// </summary>
    /// <param name="connection">Connection which was disconnected.</param>
    public delegate void DeviceDisconnectedDlgt(DeviceConnection connection);

    /// <summary>
    /// Delegate which is used to notify the reboot of a connected device.
    /// </summary>
    /// <param name="connection">Connection whose device was rebooted.</param>
    public delegate void DeviceRebootedDlgt(DeviceConnection connection);

    /// <summary>
    /// Timeout for a pending action call in seconds.
    /// </summary>
    public const int PENDING_ACTION_CALL_TIMEOUT = 30;

    protected class ActionCallState : AsyncWebRequestState
    {
      protected object _clientState;
      protected CpAction _action;

      public ActionCallState(CpAction action, object clientState, HttpWebRequest request) :
          base(request)
      {
        _action = action;
        _clientState = clientState;
      }

      public CpAction Action
      {
        get { return _action; }
      }

      public object ClientState
      {
        get { return _clientState; }
      }

      public void SetRequestMessage(string message)
      {
        try
        {
          StreamWriter sw = new StreamWriter(_httpWebRequest.GetRequestStream(), UPnPConsts.UTF8_NO_BOM);
          sw.Write(message);
          sw.Close();
        }
        catch (Exception e)
        {
          throw new UPnPRemoteException(new UPnPError(501, "Error writing action call document: " + e.Message));
        }
      }
    }

    protected CPData _cpData;
    protected UPnPControlPoint _controlPoint;
    protected RootDescriptor _rootDescriptor;
    protected string _deviceUUID;
    protected CpDevice _device;
    protected GENAClientController _genaClientController;
    protected ICollection<AsyncWebRequestState> _pendingCalls = new List<AsyncWebRequestState>();

    /// <summary>
    /// Creates a new <see cref="DeviceConnection"/> to the UPnP device contained in the given
    /// <paramref name="rootDescriptor"/> with the given <paramref name="deviceUuid"/>.
    /// </summary>
    /// <param name="controlPoint">Control point hosting the new device connection instance.</param>
    /// <param name="rootDescriptor">Root descriptor containing the description of the UPnP device to connect.</param>
    /// <param name="deviceUuid">UUID of the UPnP device to connect.</param>
    /// <param name="cpData">Shared control point data structure.</param>
    /// <param name="dataTypeResolver">Delegate method to resolve extended datatypes.</param>
    public DeviceConnection(UPnPControlPoint controlPoint, RootDescriptor rootDescriptor, string deviceUuid,
        CPData cpData, DataTypeResolverDlgt dataTypeResolver)
    {
      _controlPoint = controlPoint;
      _cpData = cpData;
      _rootDescriptor = rootDescriptor;
      _deviceUUID = deviceUuid;
      _genaClientController = new GENAClientController(_cpData, this, rootDescriptor.SSDPRootEntry.PreferredLink.Endpoint, rootDescriptor.SSDPRootEntry.UPnPVersion);
      BuildDeviceProxy(rootDescriptor, deviceUuid, dataTypeResolver);
      _genaClientController.Start();
    }

    public void Dispose()
    {
      lock (_cpData.SyncObj)
      {
        DoDisconnect(false);
        foreach (AsyncWebRequestState state in new List<AsyncWebRequestState>(_pendingCalls))
          state.Request.Abort();
        _pendingCalls.Clear();
      }
    }

    public GENAClientController GENAClientController
    {
      get { return _genaClientController; }
    }

    /// <summary>
    /// Establishes the actual device connection by building the control point's proxy device tree corresponding to the
    /// device contained in the given <paramref name="rootDescriptor"/> specified by its <paramref name="deviceUUID"/>.
    /// </summary>
    /// <param name="rootDescriptor">Root descriptor which contains the device to build.</param>
    /// <param name="deviceUUID">UUID of the device to connect.</param>
    /// <param name="dataTypeResolver">Delegate method to resolve extended datatypes.</param>
    private void BuildDeviceProxy(RootDescriptor rootDescriptor, string deviceUUID, DataTypeResolverDlgt dataTypeResolver)
    {
      if (rootDescriptor.State != RootDescriptorState.Ready)
        throw new ArgumentException("Root descriptor is not ready - cannot connect");
      DeviceDescriptor rootDeviceDescriptor = DeviceDescriptor.CreateRootDeviceDescriptor(rootDescriptor);
      DeviceDescriptor deviceDescriptor = rootDeviceDescriptor.FindDevice(deviceUUID);
      _device = CpDevice.ConnectDevice(this, deviceDescriptor, dataTypeResolver);
    }

    /// <summary>
    /// Disconnects this device connection.
    /// </summary>
    /// <param name="unsubscribeEvents">If set to <c>true</c>, unsubscription messages are sent for all subscribed
    /// services.</param>
    internal void DoDisconnect(bool unsubscribeEvents)
    {
      _genaClientController.Close(unsubscribeEvents);
      if (_device.IsConnected)
        _device.Disconnect();
      InvokeDeviceDisconnected();
    }

    internal void OnDeviceRebooted()
    {
      _genaClientController.RenewAllEventSubscriptions();
      InvokeDeviceRebooted();
    }

    internal void OnActionCalled(CpAction action, IList<object> inParams, object clientState)
    {
      if (!action.IsConnected)
        throw new UPnPDisconnectedException("Action '{0}' is not connected to a UPnP network action", action.FullQualifiedName);
      CpService service = action.ParentService;
      ServiceDescriptor sd = GetServiceDescriptor(service);
      string message = SOAPHandler.EncodeCall(action, inParams, _rootDescriptor.SSDPRootEntry.UPnPVersion);

      HttpWebRequest request = CreateActionCallRequest(sd, action);
      ActionCallState state = new ActionCallState(action, clientState, request);
      state.SetRequestMessage(message);
      lock (_cpData.SyncObj)
        _pendingCalls.Add(state);
      IAsyncResult result = state.Request.BeginGetResponse(OnCallResponseReceived, state);
      NetworkHelper.AddTimeout(request, result, PENDING_ACTION_CALL_TIMEOUT * 1000);
    }

    private void OnCallResponseReceived(IAsyncResult ar)
    {
      ActionCallState state = (ActionCallState) ar.AsyncState;
      lock (_cpData.SyncObj)
        _pendingCalls.Remove(state);
      HttpWebResponse response = null;
      Stream body = null;
      try
      {
        Encoding contentEncoding;
        try
        {
          response = (HttpWebResponse) state.Request.EndGetResponse(ar);
          body = CompressionHelper.Decompress(response);
          string mediaType;
          if (!EncodingUtils.TryParseContentTypeEncoding(response.ContentType, Encoding.UTF8, out mediaType, out contentEncoding) ||
              mediaType != "text/xml")
          {
            SOAPHandler.ActionFailed(state.Action, state.ClientState, "Invalid content type");
            return;
          }
        }
        catch (WebException e)
        {
          response = (HttpWebResponse) e.Response;
          if (response == null)
            SOAPHandler.ActionFailed(state.Action, state.ClientState, string.Format("Network error when invoking action '{0}': {1}", state.Action.Name, e.Message));
          else if (response.StatusCode == HttpStatusCode.InternalServerError)
          {
            string mediaType;
            if (!EncodingUtils.TryParseContentTypeEncoding(response.ContentType, Encoding.UTF8, out mediaType, out contentEncoding) ||
                mediaType != "text/xml")
            {
              SOAPHandler.ActionFailed(state.Action, state.ClientState, "Invalid content type");
              return;
            }
            using (TextReader reader = new StreamReader(response.GetResponseStream(), contentEncoding))
              SOAPHandler.HandleErrorResult(reader, state.Action, state.ClientState);
          }
          else
            SOAPHandler.ActionFailed(state.Action, state.ClientState, string.Format("Network error {0} when invoking action '{1}'", response.StatusCode, state.Action.Name));
          return;
        }
        UPnPVersion uPnPVersion;
        lock (_cpData.SyncObj)
          uPnPVersion = _rootDescriptor.SSDPRootEntry.UPnPVersion;
        SOAPHandler.HandleResult(body, contentEncoding, state.Action, state.ClientState, uPnPVersion);
      }
      finally
      {
        if (body != null)
          body.Dispose();
        if (response != null)
          response.Close();
      }
    }

    internal void OnSubscribeEvents(CpService service)
    {
      if (!service.IsConnected)
        throw new IllegalCallException("Service '{0}' is not connected to a UPnP network service", service.FullQualifiedName);
      if (IsServiceSubscribedForEvents(service))
        throw new IllegalCallException("Service '{0}' is already subscribed to receive state variable change events", service.FullQualifiedName);

      ServiceDescriptor serviceDescriptor = GetServiceDescriptor(service);
      _genaClientController.SubscribeEvents(service, serviceDescriptor);
    }

    internal void OnUnsubscribeEvents(CpService service)
    {
      if (!service.IsConnected)
        throw new IllegalCallException("Service '{0}' is not connected to a UPnP network service", service.FullQualifiedName);

      EventSubscription subscription = _genaClientController.FindEventSubscriptionByService(service);
      if (subscription == null)
        throw new IllegalCallException("Service '{0}' is not subscribed to receive events", service.FullQualifiedName);
      _genaClientController.UnsubscribeEvents(subscription);
    }

    protected static HttpWebRequest CreateActionCallRequest(ServiceDescriptor sd, CpAction action)
    {
      LinkData preferredLink = sd.RootDescriptor.SSDPRootEntry.PreferredLink;
      HttpWebRequest request = (HttpWebRequest) WebRequest.Create(new Uri(
          new Uri(preferredLink.DescriptionLocation), sd.ControlURL));
      NetworkUtils.SetLocalEndpoint(request, preferredLink.Endpoint.EndPointIPAddress);
      request.Method = "POST";
      request.KeepAlive = true;
      request.AllowAutoRedirect = true;
      request.UserAgent = UPnPConfiguration.UPnPMachineInfoHeader;
      request.ContentType = "text/xml; charset=\"utf-8\"";
      request.Headers.Add("SOAPACTION", action.Action_URN);
      request.Headers.Add("Accept-Encoding", CompressionHelper.PREFERRED_COMPRESSION);
      return request;
    }

    protected void InvokeDeviceDisconnected()
    {
      try
      {
        DeviceDisconnectedDlgt dlgt = DeviceDisconnected;
        if (dlgt != null)
          dlgt(this);
      }
      catch (Exception e)
      {
        UPnPConfiguration.LOGGER.Warn("DeviceConnection: Error invoking DeviceDisconnected delegate", e);
      }
    }

    protected void InvokeDeviceRebooted()
    {
      try
      {
        DeviceRebootedDlgt dlgt = DeviceRebooted;
        if (dlgt != null)
          dlgt(this);
      }
      catch (Exception e)
      {
        UPnPConfiguration.LOGGER.Warn("DeviceConnection: Error invoking DeviceRebooted delegate", e);
      }
    }

    protected ServiceDescriptor GetServiceDescriptor(CpService service)
    {
      if (!service.IsConnected)
        throw new IllegalCallException("Service '{0}' is not connected to a UPnP network service", service.FullQualifiedName);
      IDictionary<string, ServiceDescriptor> serviceDescriptors;
      string deviceUUID = service.ParentDevice.UUID;
      if (!_rootDescriptor.ServiceDescriptors.TryGetValue(deviceUUID, out serviceDescriptors))
        throw new IllegalCallException("Device '{0}' is not connected to a UPnP network device", deviceUUID);
      ServiceDescriptor sd;
      if (!serviceDescriptors.TryGetValue(service.ServiceTypeVersion_URN, out sd))
        throw new IllegalCallException("Service '{0}' in device '{1}' is not connected to a UPnP network service", service.ServiceTypeVersion_URN, deviceUUID);
      return sd;
    }

    /// <summary>
    /// Gets raised when the device of this device connection was disconnected.
    /// </summary>
    public event DeviceDisconnectedDlgt DeviceDisconnected;

    public event DeviceRebootedDlgt DeviceRebooted;

    /// <summary>
    /// Returns the shared control point data structure.
    /// </summary>
    public CPData CPData
    {
      get { return _cpData; }
    }

    /// <summary>
    /// Returns the root descriptor of the connected UPnP device.
    /// </summary>
    /// <remarks>
    /// The root descriptor contains multiple devices which can be connected in several <see cref="DeviceConnection"/> instances,
    /// so a single <see cref="RootDescriptor"/> might belong to multiple connections.
    /// </remarks>
    public RootDescriptor RootDescriptor
    {
      get { return _rootDescriptor; }
    }

    /// <summary>
    /// Returns the UUID of the device which is the base of the connected device tree.
    /// </summary>
    public string DeviceUUID
    {
      get { return _deviceUUID; }
    }

    /// <summary>
    /// Returns the device instance which is the base of the connected device tree.
    /// </summary>
    public CpDevice Device
    {
      get { return _device; }
    }

    /// <summary>
    /// Disconnects this device connection.
    /// </summary>
    public void Disconnect()
    {
      // The control point needs to trigger the disconnection to keep its datastructures updated; it will call us back
      // to method DoDisconnect
      _controlPoint.Disconnect(DeviceUUID);
    }

    /// <summary>
    /// Returns the information whether the specified <paramref name="service"/> is registered for event notifications.
    /// </summary>
    /// <remarks>
    /// When subscribing for state variable changes of a given service s, this method doesn't return <c>true</c> when invoked
    /// for that service s immediately. The subscription must first be confirmed by the UPnP network service.
    /// </remarks>
    /// <param name="service">The service instance to check.</param>
    /// <returns><c>true</c>, if the <paramref name="service"/> is subscribed for receiving event notifications, else
    /// <c>false</c>.</returns>
    public bool IsServiceSubscribedForEvents(CpService service)
    {
      lock (_cpData.SyncObj)
        return _genaClientController.FindEventSubscriptionByService(service) != null;
    }
  }
}
