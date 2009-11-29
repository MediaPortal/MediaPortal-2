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
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.XPath;
using HttpServer;
using MediaPortal.Utilities.Exceptions;
using UPnP.Infrastructure.Common;
using UPnP.Infrastructure.CP.DeviceTree;
using UPnP.Infrastructure.CP.GENA;
using UPnP.Infrastructure.CP.SOAP;
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
  /// Delegate which is used to notify the disconnect event of a <see cref="DeviceConnection"/>.
  /// </summary>
  /// <param name="connection">Connection which was disconnected.</param>
  public delegate void DeviceDisconnectedDlgt(DeviceConnection connection);

  /// <summary>
  /// Contains the control point connection data of a device template to a network UPnP device.
  /// </summary>
  public class DeviceConnection : IDisposable
  {
    /// <summary>
    /// Default event expiration time to use.
    /// </summary>
    public static int EVENT_SUBSCRIPTION_TIME = 1800;

    /// <summary>
    /// Safety gap in seconds how near an event expiration can come until we'll automatically renew the event subscription.
    /// </summary>
    public static int EVENT_SUBSCRIPTION_RENEWAL_GAP = 30;

    /// <summary>
    /// Distance of the eventkey to 1 and to the eventkey wrap value of 4294967295 where we handle the event order check
    /// in another way.
    /// </summary>
    public static uint EVENTKEY_GAP_THRESHOLD = 100;

    /// <summary>
    /// Timeout for a pending action call in seconds.
    /// </summary>
    public const int PENDING_ACTION_CALL_TIMEOUT = 30;

    /// <summary>
    /// Timeout for a pending subscription call in seconds.
    /// </summary>
    public const int EVENT_SUBSCRIPTION_CALL_TIMEOUT = 30;

    /// <summary>
    /// Timeout for a pending unsubscription call in seconds.
    /// </summary>
    public const int EVENT_UNSUBSCRIPTION_CALL_TIMEOUT = 30;

    protected class AsyncRequestState
    {
      protected HttpWebRequest _httpWebRequest;

      public AsyncRequestState(HttpWebRequest request)
      {
        _httpWebRequest = request;
      }

      public HttpWebRequest Request
      {
        get { return _httpWebRequest; }
      }
    }

    protected class ActionCallState : AsyncRequestState
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
        StreamWriter sw = new StreamWriter(_httpWebRequest.GetRequestStream(), Encoding.UTF8);
        sw.Write(message);
        sw.Close();
      }
    }

    protected class ChangeEventSubscriptionState : AsyncRequestState
    {
      protected CpService _service;

      public ChangeEventSubscriptionState(CpService service, HttpWebRequest request) :
          base(request)
      {
        _service = service;
      }

      public CpService Service
      {
        get { return _service; }
      }
    }

    protected class EventSubscription
    {
      protected string _sid;
      protected CpService _service;
      protected DateTime _expiration;
      protected uint _eventKey = 0;

      public EventSubscription(string sid, CpService service, DateTime expiration)
      {
        _sid = sid;
        _service = service;
        _expiration = expiration;
      }

      public string Sid
      {
        get { return _sid; }
      }

      public CpService Service
      {
        get { return _service; }
      }

      public DateTime Expiration
      {
        get { return _expiration; }
        set { _expiration = value; }
      }

      public uint EventKey
      {
        get { return _eventKey; }
      }

      public bool SetNewEventKey(uint value)
      {
        ulong seq = value;
        ulong max_gap = _eventKey + EVENTKEY_GAP_THRESHOLD;
        if (seq < _eventKey)
          seq += 2 << 32;
        if (seq <= max_gap)
        {
          _eventKey = value;
          return true;
        }
        return false;
      }
    }

    protected CPData _cpData;
    protected UPnPControlPoint _controlPoint;
    protected RootDescriptor _rootDescriptor;
    protected string _deviceUUID;
    protected CpDevice _device;
    protected string _eventNotificationURL;
    protected Timer _subscriptionRenewalTimer;
    protected IDictionary<string, EventSubscription> _subscriptions = new Dictionary<string, EventSubscription>();
    protected ICollection<AsyncRequestState> _pendingCalls = new List<AsyncRequestState>();

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
      _eventNotificationURL = string.Format("http://{0}/{1}/", new IPEndPoint(_rootDescriptor.SSDPRootEntry.Endpoint.EndPointIPAddress,
          (int) cpData.HttpPort), Guid.NewGuid());
      BuildDevice(rootDescriptor, deviceUuid, dataTypeResolver);
      _subscriptionRenewalTimer = new Timer(OnSubscriptionRenewalTimerElapsed);
    }

    public void Dispose()
    {
      lock (_cpData.SyncObj)
      {
        DoDisconnect(false);
        _subscriptionRenewalTimer.Dispose();
        foreach (AsyncRequestState state in new List<AsyncRequestState>(_pendingCalls))
          state.Request.Abort();
        _pendingCalls.Clear();
      }
    }

    /// <summary>
    /// Establishes the actual device connection by building the control point's device tree correspondin to the
    /// device contained in the given <paramref name="rootDescriptor"/> specified by its <paramref name="deviceUUID"/>.
    /// </summary>
    /// <param name="rootDescriptor">Root descriptor which contains the device to build.</param>
    /// <param name="deviceUUID">UUID of the device to connect.</param>
    /// <param name="dataTypeResolver">Delegate method to resolve extended datatypes.</param>
    private void BuildDevice(RootDescriptor rootDescriptor, string deviceUUID, DataTypeResolverDlgt dataTypeResolver)
    {
      if (rootDescriptor.State == RootDescriptorState.Erroneous)
        throw new ArgumentException("Cannot connect to an erroneous root descriptor");
      XPathNavigator nav = rootDescriptor.DeviceDescription.CreateNavigator();
      nav.MoveToChild(XPathNodeType.Element);
      XmlNamespaceManager nsmgr = new XmlNamespaceManager(nav.NameTable);
      nsmgr.AddNamespace("d", UPnPConsts.NS_DEVICE_DESCRIPTION);
      XPathNodeIterator deviceIt = nav.Select("descendant::d:device[d:UDN/text()=concat(\"uuid:\",\"" + deviceUUID + "\")]", nsmgr);
      if (!deviceIt.MoveNext())
        throw new ArgumentException(string.Format("Device with the specified id '{0}' isn't present in the given root descriptor", _deviceUUID));
      _device = CpDevice.ConnectDevice(this, rootDescriptor, deviceIt.Current, nsmgr, dataTypeResolver);
    }

    /// <summary>
    /// Disconnects this device connection.
    /// </summary>
    /// <param name="unsubscribeEvents">If set to <c>true</c>, unsubscription messages are sent for all subscribed
    /// services.</param>
    internal void DoDisconnect(bool unsubscribeEvents)
    {
      lock (_cpData.SyncObj)
      {
        foreach (EventSubscription subscription in new List<EventSubscription>(_subscriptions.Values))
          if (unsubscribeEvents)
            UnsubscribeEvents(subscription);
        _subscriptions.Clear();
        if (_device.IsConnected)
          _device.Disconnect();
        InvokeDeviceDisconnected();
      }
    }

    internal void OnSubscribeEvents(CpService service)
    {
      if (!service.IsConnected)
        throw new IllegalCallException("Service '{0}' is not connected to a UPnP network service", service.FullQualifiedName);
      if (IsServiceSubscribedForEvents(service))
        throw new IllegalCallException("Service '{0}' is already subscribed to receive state variable change events", service.FullQualifiedName);

      SubscribeEvents(service);
    }

    internal void OnUnsubscribeEvents(CpService service)
    {
      if (!service.IsConnected)
        throw new IllegalCallException("Service '{0}' is not connected to a UPnP network service", service.FullQualifiedName);

      EventSubscription subscription = FindEventSubscriptionByService(service);
      if (subscription == null)
        throw new IllegalCallException("Service '{0}' is not subscribed to receive events", service.FullQualifiedName);
      UnsubscribeEvents(subscription);
    }

    internal void OnDeviceRebooted()
    {
      RenewAllEventSubscriptions();
    }

    internal void OnActionCalled(CpAction action, IList<object> inParams, object clientState)
    {
      if (!action.IsConnected)
        throw new IllegalCallException("Action '{0}' is not connected to a UPnP network action", action.FullQualifiedName);
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
      try
      {
        Stream body;
        Encoding contentEncoding;
        try
        {
          response = (HttpWebResponse) state.Request.EndGetResponse(ar);
          body = response.GetResponseStream();
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
            SOAPHandler.ActionFailed(state.Action, state.ClientState, string.Format("Network error when invoking action '{0}'", state.Action.Name));
          else if (response.StatusCode == HttpStatusCode.InternalServerError)
          {
            string mediaType;
            if (!EncodingUtils.TryParseContentTypeEncoding(response.ContentType, Encoding.UTF8, out mediaType, out contentEncoding) ||
                mediaType != "text/xml")
            {
              SOAPHandler.ActionFailed(state.Action, state.ClientState, "Invalid content type");
              return;
            }
            SOAPHandler.HandleErrorResult(new StreamReader(response.GetResponseStream(), contentEncoding), state.Action, state.ClientState);
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
        if (response != null)
          response.Close();
      }
    }

    protected void SubscribeEvents(CpService service)
    {
      lock (_cpData.SyncObj)
      {
        ServiceDescriptor serviceDescriptor = GetServiceDescriptor(service);
        HttpWebRequest request = CreateEventSubscribeRequest(serviceDescriptor);
        ChangeEventSubscriptionState state = new ChangeEventSubscriptionState(service, request);
        _pendingCalls.Add(state);
        IAsyncResult result = state.Request.BeginGetResponse(OnSubscribeOrRenewSubscriptionResponseReceived, state);
        NetworkHelper.AddTimeout(request, result, EVENT_SUBSCRIPTION_CALL_TIMEOUT * 1000);
      }
    }

    protected void RenewEventSubscription(EventSubscription subscription)
    {
      if (!subscription.Service.IsConnected)
        throw new IllegalCallException("Service '{0}' is not connected to a UPnP network service", subscription.Service.FullQualifiedName);

      lock (_cpData.SyncObj)
      {
        HttpWebRequest request = CreateRenewEventSubscribeRequest(subscription);
        ChangeEventSubscriptionState state = new ChangeEventSubscriptionState(subscription.Service, request);
        _pendingCalls.Add(state);
        IAsyncResult result = state.Request.BeginGetResponse(OnSubscribeOrRenewSubscriptionResponseReceived, state);
        NetworkHelper.AddTimeout(request, result, EVENT_SUBSCRIPTION_CALL_TIMEOUT * 1000);
      }
    }

    protected void RenewAllEventSubscriptions()
    {
      foreach (EventSubscription subscription in _subscriptions.Values)
        RenewEventSubscription(subscription);
    }

    private void OnSubscribeOrRenewSubscriptionResponseReceived(IAsyncResult ar)
    {
      ChangeEventSubscriptionState state = (ChangeEventSubscriptionState) ar.AsyncState;
      lock (_cpData.SyncObj)
        _pendingCalls.Remove(state);
      CpService service = state.Service;
      try
      {
        HttpWebResponse response = (HttpWebResponse) state.Request.EndGetResponse(ar);
        try
        {
          if (response.StatusCode != HttpStatusCode.OK)
          {
            service.InvokeEventSubscriptionFailed(new UPnPError((uint) response.StatusCode, response.StatusDescription));
            return;
          }
          string dateStr = response.Headers.Get("DATE");
          string sid = response.Headers.Get("SID");
          string timeoutStr = response.Headers.Get("TIMEOUT");
          DateTime date = DateTime.ParseExact(dateStr, "R", CultureInfo.InvariantCulture).ToLocalTime();
          int timeout;
          if (string.IsNullOrEmpty(timeoutStr) || (!timeoutStr.StartsWith("Second-") ||
              !int.TryParse(timeoutStr.Substring("Second-".Length).Trim(), out timeout)))
          {
            service.InvokeEventSubscriptionFailed(new UPnPError((int) HttpStatusCode.BadRequest, "Invalid answer from UPnP device"));
            return;
          }
          DateTime expiration = date.AddSeconds(timeout);
          EventSubscription subscription;
          lock (_cpData.SyncObj)
          {
            if (_subscriptions.TryGetValue(sid, out subscription))
              subscription.Expiration = expiration;
            else
              _subscriptions.Add(sid, new EventSubscription(sid, service, expiration));
            CheckSubscriptionRenewalTimer(_subscriptions.Values);
          }
        }
        finally
        {
          response.Close();
        }
      }
      catch (WebException e)
      {
        HttpWebResponse response = (HttpWebResponse) e.Response;
        if (response == null)
          service.InvokeEventSubscriptionFailed(new UPnPError(503, "Cannot complete event subscription"));
        else
          service.InvokeEventSubscriptionFailed(new UPnPError((uint) response.StatusCode, "Cannot complete event subscription"));
        if (response != null)
          response.Close();
        return;
      }
    }

    protected void UnsubscribeEvents(EventSubscription subscription)
    {
      lock (_cpData.SyncObj)
      {
        HttpWebRequest request = CreateEventUnsubscribeRequest(subscription);
        ChangeEventSubscriptionState state = new ChangeEventSubscriptionState(subscription.Service, request);
        _pendingCalls.Add(state);
        IAsyncResult result = state.Request.BeginGetResponse(OnUnsubscribeResponseReceived, state);
        NetworkHelper.AddTimeout(request, result, EVENT_UNSUBSCRIPTION_CALL_TIMEOUT * 1000);
      }
    }

    private void OnUnsubscribeResponseReceived(IAsyncResult ar)
    {
      ChangeEventSubscriptionState state = (ChangeEventSubscriptionState) ar.AsyncState;
      EventSubscription subscription = FindEventSubscriptionByService(state.Service);
      try
      {
        HttpWebResponse response = (HttpWebResponse) state.Request.EndGetResponse(ar);
        response.Close();
      }
      catch (WebException e)
      {
        if (e.Response != null)
          e.Response.Close();
      }
      lock (_cpData.SyncObj)
      {
        _pendingCalls.Remove(state);
        if (subscription != null)
        { // As we are asynchronous, the subscription might have gone already (maybe as a result of a duplicate unsubscribe event
          // or as a result of a disposal of this instance)
          _subscriptions.Remove(subscription.Sid);
          CheckSubscriptionRenewalTimer(_subscriptions.Values);
        }
      }
    }

    private void OnSubscriptionRenewalTimerElapsed(object state)
    {
      ICollection<EventSubscription> remainingEventSubscriptions = new List<EventSubscription>(_subscriptions.Count);
      lock (_cpData.SyncObj)
      {
        DateTime threshold = DateTime.Now.AddSeconds(EVENT_SUBSCRIPTION_RENEWAL_GAP);
        foreach (EventSubscription subscription in _subscriptions.Values)
          if (threshold > subscription.Expiration)
            RenewEventSubscription(subscription);
          else
            remainingEventSubscriptions.Add(subscription);
        CheckSubscriptionRenewalTimer(remainingEventSubscriptions);
      }
    }

    protected void CheckSubscriptionRenewalTimer(IEnumerable<EventSubscription> subscriptionsToCheck)
    {
      lock (_cpData.SyncObj)
      {
        DateTime? minExpiration = null;
        foreach (EventSubscription subscription in subscriptionsToCheck)
          if (!minExpiration.HasValue || subscription.Expiration < minExpiration.Value)
            minExpiration = subscription.Expiration;
        if (minExpiration.HasValue)
        {
          long numMilliseconds = (minExpiration.Value - DateTime.Now).Milliseconds - EVENT_SUBSCRIPTION_RENEWAL_GAP;
          if (numMilliseconds < 0)
            numMilliseconds = 0;
          try
          {
            _subscriptionRenewalTimer.Change(numMilliseconds, Timeout.Infinite);
          }
          catch (ObjectDisposedException) { }
        }
      }
    }

    protected void InvokeDeviceDisconnected()
    {
      DeviceDisconnectedDlgt dlgt = DeviceDisconnected;
      if (dlgt != null)
        dlgt(this);
    }

    protected EventSubscription FindEventSubscriptionByService(CpService service)
    {
      lock (_cpData.SyncObj)
        foreach (EventSubscription subscription in _subscriptions.Values)
          if (subscription.Service == service)
            return subscription;
      return null;
    }

    protected static HttpWebRequest CreateActionCallRequest(ServiceDescriptor sd, CpAction action)
    {
      HttpWebRequest request = (HttpWebRequest) WebRequest.Create(new Uri(
          new Uri(sd.RootDescriptor.SSDPRootEntry.DescriptionLocation), sd.ControlURL));
      request.Method = "POST";
      request.KeepAlive = true;
      request.AllowAutoRedirect = true;
      request.UserAgent = Configuration.UPnPMachineInfoHeader;
      request.ContentType = "text/xml; charset=\"utf-8\"";
      request.Headers.Add("SOAPACTION", action.Action_URN);
      return request;
    }

    protected HttpWebRequest CreateEventSubscribeRequest(ServiceDescriptor sd)
    {
      HttpWebRequest request = (HttpWebRequest) WebRequest.Create(new Uri(
          new Uri(sd.RootDescriptor.SSDPRootEntry.DescriptionLocation), sd.EventSubURL));
      request.Method = "SUBSCRIBE";
      request.UserAgent = Configuration.UPnPMachineInfoHeader;
      request.Headers.Add("CALLBACK", "<" + _eventNotificationURL + ">");
      request.Headers.Add("NT", "upnp:event");
      request.Headers.Add("TIMEOUT", "Second-" + EVENT_SUBSCRIPTION_TIME);
      return request;
    }

    protected HttpWebRequest CreateRenewEventSubscribeRequest(EventSubscription subscription)
    {
      ServiceDescriptor sd = GetServiceDescriptor(subscription.Service);
      HttpWebRequest request = (HttpWebRequest) WebRequest.Create(new Uri(
          new Uri(sd.RootDescriptor.SSDPRootEntry.DescriptionLocation), sd.EventSubURL));
      request.Method = "SUBSCRIBE";
      request.Headers.Add("SID", subscription.Sid);
      request.Headers.Add("TIMEOUT", "Second-" + EVENT_SUBSCRIPTION_TIME);
      return request;
    }

    protected HttpWebRequest CreateEventUnsubscribeRequest(EventSubscription subscription)
    {
      ServiceDescriptor sd = GetServiceDescriptor(subscription.Service);
      HttpWebRequest request = (HttpWebRequest) WebRequest.Create(new Uri(
          new Uri(sd.RootDescriptor.SSDPRootEntry.DescriptionLocation), sd.EventSubURL));
      request.Method = "UNSUBSCRIBE";
      request.Headers.Add("SID", subscription.Sid);
      return request;
    }

    internal HttpStatusCode HandleEventNotification(IHttpRequest request)
    {
      string nt = request.Headers.Get("NT");
      string nts = request.Headers.Get("NTS");
      string sid = request.Headers.Get("SID");
      string seqStr = request.Headers.Get("SEQ");
      string contentType = request.Headers.Get("CONTENT-TYPE");

      lock (_cpData.SyncObj)
      {
        EventSubscription subscription;
        if (nt != "upnp:event" || nts != "upnp:propchange" || string.IsNullOrEmpty(sid) ||
            !_subscriptions.TryGetValue(sid, out subscription))
          return HttpStatusCode.PreconditionFailed;
        uint seq;
        if (!uint.TryParse(seqStr, out seq))
          return HttpStatusCode.BadRequest;
        if (!subscription.SetNewEventKey(seq))
          // Skip notification messages which arrive in the wrong order
          return HttpStatusCode.OK;
        Encoding contentEncoding;
        string mediaType;
        if (!EncodingUtils.TryParseContentTypeEncoding(contentType, Encoding.UTF8, out mediaType, out contentEncoding) ||
            mediaType != "text/xml")
          return HttpStatusCode.BadRequest;
        Stream stream = request.Body;
        return GENAHandler.HandleEventNotification(stream, contentEncoding, subscription.Service,
            _rootDescriptor.SSDPRootEntry.UPnPVersion);
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
    /// Returns the unique URL which is used for event notifications from subscribed services.
    /// </summary>
    public string EventNotificationURL
    {
      get { return _eventNotificationURL; }
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
        return FindEventSubscriptionByService(service) != null;
    }

    /// <summary>
    /// Disconnects this device connection.
    /// </summary>
    public void Disconnect()
    {
      // The control point needs to trigger the disconnection to keep its datastructures updated; it will call us back
      // in method DoDisconnect
      _controlPoint.Disconnect(DeviceUUID);
    }
  }
}
