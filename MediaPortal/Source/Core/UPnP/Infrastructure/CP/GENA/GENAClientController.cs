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
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Xml;
using HttpServer;
using MediaPortal.Utilities.Exceptions;
using MediaPortal.Utilities.Network;
using UPnP.Infrastructure.Common;
using UPnP.Infrastructure.CP.Description;
using UPnP.Infrastructure.CP.DeviceTree;
using UPnP.Infrastructure.CP.SSDP;
using UPnP.Infrastructure.Utils;
using UPnP.Infrastructure.Utils.HTTP;

namespace UPnP.Infrastructure.CP.GENA
{
  public class GENAClientController
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
    /// Timeout for a pending subscription call in seconds.
    /// </summary>
    public const int EVENT_SUBSCRIPTION_CALL_TIMEOUT = 30;

    /// <summary>
    /// Timeout for a pending unsubscription call in seconds.
    /// </summary>
    public const int EVENT_UNSUBSCRIPTION_CALL_TIMEOUT = 30;

    protected class ChangeEventSubscriptionState : AsyncWebRequestState
    {
      protected ServiceDescriptor _serviceDescriptor;
      protected CpService _service;

      public ChangeEventSubscriptionState(ServiceDescriptor serviceDescriptor, CpService service, HttpWebRequest request) :
          base(request)
      {
        _serviceDescriptor = serviceDescriptor;
        _service = service;
      }

      public CpService Service
      {
        get { return _service; }
      }

      public ServiceDescriptor ServiceDescriptor
      {
        get { return _serviceDescriptor; }
      }
    }

    protected CPData _cpData;
    protected bool _isActive = true;
    protected DeviceConnection _connection;
    protected UPnPVersion _upnpVersion;
    protected EndpointConfiguration _endpoint;
    protected IPEndPoint _eventNotificationEndpoint;
    protected string _eventNotificationPath;
    protected Timer _subscriptionRenewalTimer;
    protected IDictionary<string, EventSubscription> _subscriptions = new Dictionary<string, EventSubscription>();
    protected ICollection<AsyncWebRequestState> _pendingCalls = new List<AsyncWebRequestState>();

    public GENAClientController(CPData cpData, DeviceConnection connection, EndpointConfiguration endpoint, UPnPVersion upnpVersion)
    {
      _cpData = cpData;
      _connection = connection;
      _endpoint = endpoint;
      _upnpVersion = upnpVersion;
      _eventNotificationPath = "/" + Guid.NewGuid();
      IPAddress address = endpoint.EndPointIPAddress;
      _eventNotificationEndpoint = new IPEndPoint(address, address.AddressFamily == AddressFamily.InterNetwork ?
          cpData.HttpPortV4 : cpData.HttpPortV6);
      _subscriptionRenewalTimer = new Timer(OnSubscriptionRenewalTimerElapsed);
    }

    public void Close(bool unsubscribeEvents)
    {
      WaitHandle notifyObject = new ManualResetEvent(false);
      lock (_cpData.SyncObj)
      {
        if (!_isActive)
          return;
        _isActive = false;
        _subscriptionRenewalTimer.Dispose(notifyObject);
      }
      notifyObject.WaitOne();
      notifyObject.Close();
      lock (_cpData.SyncObj)
      {
        foreach (EventSubscription subscription in new List<EventSubscription>(_subscriptions.Values))
          if (unsubscribeEvents)
            UnsubscribeEvents(subscription);
        _subscriptions.Clear();
        Socket socket = _endpoint.GENA_UDP_MulticastReceiveSocket;
        if (socket != null)
        {
          UPnPConfiguration.LOGGER.Info("UPnPServerController: GENA disabled for IP endpoint '{0}'",
              NetworkHelper.IPAddrToString(_endpoint.EndPointIPAddress));
          _endpoint.GENA_UDP_MulticastReceiveSocket = null;
          NetworkHelper.DisposeGENAMulticastSocket(socket);
        }
      }
    }

    /// <summary>
    /// Returns the HTTP web server path which is used for event notifications from subscribed services.
    /// </summary>
    public string EventNotificationPath
    {
      get { return _eventNotificationPath; }
    }

    /// <summary>
    /// Returns the IP endpoint which is used as server for the publication of the event subscription callback path.
    /// </summary>
    public IPEndPoint EventNotificationEndpoint
    {
      get { return _eventNotificationEndpoint; }
    }

    public void Start()
    {
      IPAddress address = _endpoint.EndPointIPAddress;
      AddressFamily family = address.AddressFamily;

      // Multicast socket - used for receiving multicast GENA event messages
      Socket socket = new Socket(family, SocketType.Dgram, ProtocolType.Udp);
      _endpoint.GENA_UDP_MulticastReceiveSocket = socket;
      try
      {
        socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
        NetworkHelper.BindAndConfigureGENAMulticastSocket(socket, address);
        StartReceive(new UDPAsyncReceiveState<EndpointConfiguration>(_endpoint, UPnPConsts.UDP_GENA_RECEIVE_BUFFER_SIZE, socket));
      }
      catch (Exception e) // SocketException, SecurityException
      {
        UPnPConfiguration.LOGGER.Info("GENAClientController: Unable to bind to multicast address(es) for endpoint '{0}'", e,
            NetworkHelper.IPAddrToString(address));
      }
    }

    protected void StartReceive(UDPAsyncReceiveState<EndpointConfiguration> state)
    {
      EndPoint remoteEP = new IPEndPoint(state.Endpoint.AddressFamily == AddressFamily.InterNetwork ?
          IPAddress.Any : IPAddress.IPv6Any, 0);
      try
      {
        state.Socket.BeginReceiveFrom(state.Buffer, 0, state.Buffer.Length, SocketFlags.None,
            ref remoteEP, OnGENAReceive, state);
      }
      catch (Exception e) // SocketException and ObjectDisposedException
      {
        UPnPConfiguration.LOGGER.Info("GENAClientController: UPnP GENA subsystem unable to receive multicast events from IP address '{0}'", e,
            NetworkHelper.IPAddrToString(state.Endpoint.EndPointIPAddress));
      }
    }

    private void OnGENAReceive(IAsyncResult ar)
    {
      lock (_cpData.SyncObj)
        if (!_isActive)
          return;
      UDPAsyncReceiveState<EndpointConfiguration> state = (UDPAsyncReceiveState<EndpointConfiguration>) ar.AsyncState;
      EndpointConfiguration config = state.Endpoint;
      Socket socket = state.Socket;
      try
      {
        EndPoint remoteEP = new IPEndPoint(
            state.Endpoint.AddressFamily == AddressFamily.InterNetwork ?
            IPAddress.Any : IPAddress.IPv6Any, 0);
        // To retrieve the remote endpoint address, it is necessary that the SocketOptionName.PacketInformation is set to true on the socket
        Stream stream = new MemoryStream(state.Buffer, 0, socket.EndReceiveFrom(ar, ref remoteEP));
        try
        {
          SimpleHTTPRequest request;
          SimpleHTTPRequest.Parse(stream, out request);
          HandleMulticastEventNotification(request);
        }
        catch (Exception e)
        {
          UPnPConfiguration.LOGGER.Debug("GENAClientController: Problem parsing incoming multicast packet at IP endpoint '{0}'. Error message: '{1}'",
              NetworkHelper.IPAddrToString(config.EndPointIPAddress), e.Message);
        }
        StartReceive(state);
      }
      catch (Exception) // SocketException, ObjectDisposedException
      {
        // Socket was closed - ignore this exception
        UPnPConfiguration.LOGGER.Info("GENAClientController: Stopping listening for multicast messages at IP endpoint '{0}'",
            NetworkHelper.IPAddrToString(config.EndPointIPAddress));
      }
    }

    internal void SubscribeEvents(CpService service, ServiceDescriptor serviceDescriptor)
    {
      lock (_cpData.SyncObj)
      {
        HttpWebRequest request = CreateEventSubscribeRequest(serviceDescriptor);
        ChangeEventSubscriptionState state = new ChangeEventSubscriptionState(serviceDescriptor, service, request);
        _pendingCalls.Add(state);
        IAsyncResult result = state.Request.BeginGetResponse(OnSubscribeOrRenewSubscriptionResponseReceived, state);
        NetworkHelper.AddTimeout(request, result, EVENT_SUBSCRIPTION_CALL_TIMEOUT * 1000);
      }
    }

    internal void RenewAllEventSubscriptions()
    {
      foreach (EventSubscription subscription in _subscriptions.Values)
        RenewEventSubscription(subscription);
    }

    protected void RenewEventSubscription(EventSubscription subscription)
    {
      if (!subscription.Service.IsConnected)
        throw new IllegalCallException("Service '{0}' is not connected to a UPnP network service", subscription.Service.FullQualifiedName);

      lock (_cpData.SyncObj)
      {
        HttpWebRequest request = CreateRenewEventSubscribeRequest(subscription);
        ChangeEventSubscriptionState state = new ChangeEventSubscriptionState(subscription.ServiceDescriptor, subscription.Service, request);
        _pendingCalls.Add(state);
        IAsyncResult result = state.Request.BeginGetResponse(OnSubscribeOrRenewSubscriptionResponseReceived, state);
        NetworkHelper.AddTimeout(request, result, EVENT_SUBSCRIPTION_CALL_TIMEOUT * 1000);
      }
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
              _subscriptions.Add(sid, new EventSubscription(sid, state.ServiceDescriptor, service, expiration));
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
        service.InvokeEventSubscriptionFailed(new UPnPError(response == null ? 503 : (uint) response.StatusCode, "Cannot complete event subscription"));
        if (response != null)
          response.Close();
      }
    }

    internal void UnsubscribeEvents(EventSubscription subscription)
    {
      lock (_cpData.SyncObj)
      {
        HttpWebRequest request = CreateEventUnsubscribeRequest(subscription);
        ChangeEventSubscriptionState state = new ChangeEventSubscriptionState(subscription.ServiceDescriptor, subscription.Service, request);
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
      lock (_cpData.SyncObj)
      {
        ICollection<EventSubscription> remainingEventSubscriptions = new List<EventSubscription>(_subscriptions.Count);
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

    public EventSubscription FindEventSubscriptionByService(CpService service)
    {
      lock (_cpData.SyncObj)
        foreach (EventSubscription subscription in _subscriptions.Values)
          if (subscription.Service == service)
            return subscription;
      return null;
    }

    protected HttpWebRequest CreateEventSubscribeRequest(ServiceDescriptor sd)
    {
      LinkData preferredLink = sd.RootDescriptor.SSDPRootEntry.PreferredLink;
      HttpWebRequest request = (HttpWebRequest) WebRequest.Create(new Uri(
          new Uri(preferredLink.DescriptionLocation), sd.EventSubURL));
      NetworkUtils.SetLocalEndpoint(request, preferredLink.Endpoint.EndPointIPAddress);
      request.Method = "SUBSCRIBE";
      request.UserAgent = UPnPConfiguration.UPnPMachineInfoHeader;
      request.Headers.Add("CALLBACK", "<http://" + NetworkHelper.IPEndPointToString(_eventNotificationEndpoint) + _eventNotificationPath + ">");
      request.Headers.Add("NT", "upnp:event");
      request.Headers.Add("TIMEOUT", "Second-" + EVENT_SUBSCRIPTION_TIME);
      return request;
    }

    protected HttpWebRequest CreateRenewEventSubscribeRequest(EventSubscription subscription)
    {
      ServiceDescriptor sd = subscription.ServiceDescriptor;
      LinkData preferredLink = sd.RootDescriptor.SSDPRootEntry.PreferredLink;
      HttpWebRequest request = (HttpWebRequest) WebRequest.Create(new Uri(
          new Uri(preferredLink.DescriptionLocation), sd.EventSubURL));
      NetworkUtils.SetLocalEndpoint(request, preferredLink.Endpoint.EndPointIPAddress);
      request.Method = "SUBSCRIBE";
      request.Headers.Add("SID", subscription.Sid);
      request.Headers.Add("TIMEOUT", "Second-" + EVENT_SUBSCRIPTION_TIME);
      return request;
    }

    protected HttpWebRequest CreateEventUnsubscribeRequest(EventSubscription subscription)
    {
      ServiceDescriptor sd = subscription.ServiceDescriptor;
      LinkData preferredLink = sd.RootDescriptor.SSDPRootEntry.PreferredLink;
      HttpWebRequest request = (HttpWebRequest) WebRequest.Create(new Uri(
          new Uri(preferredLink.DescriptionLocation), sd.EventSubURL));
      NetworkUtils.SetLocalEndpoint(request, preferredLink.Endpoint.EndPointIPAddress);
      request.Method = "UNSUBSCRIBE";
      request.Headers.Add("SID", subscription.Sid);
      return request;
    }

    public HttpStatusCode HandleUnicastEventNotification(IHttpRequest request)
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
        return HandleEventNotification(stream, contentEncoding, subscription.Service,
            _upnpVersion);
      }
    }

    protected bool TryGetService(string usn, string svcid, out CpService service)
    {
      string deviceUUID;
      string serviceTypeVersion;
      service = null;
      if (!ParserHelper.TryParseUSN(usn, out deviceUUID, out serviceTypeVersion))
        return false;
      string type;
      int version;
      if (!ParserHelper.TryParseTypeVersion_URN(serviceTypeVersion, out type, out version))
        return false;
      foreach (CpService matchingService in _connection.Device.FindServicesByServiceTypeAndVersion(type, version, false))
      {
        if (matchingService.ServiceId == svcid)
        {
          service = matchingService;
          return true;
        }
      }
      return false;
    }

    public void HandleMulticastEventNotification(SimpleHTTPRequest request)
    {
      string nt = request["NT"];
      string nts = request["NTS"];
      string sid = request["SID"];
      string usn = request["USN"];
      string svcid = request["SVCID"];
      string contentType = request["CONTENT-TYPE"];

      lock (_cpData.SyncObj)
      {
        CpService service;
        if (nt != "upnp:event" || nts != "upnp:propchange" || string.IsNullOrEmpty(sid) ||
            !TryGetService(usn, svcid, out service))
          return;
        Encoding contentEncoding;
        string mediaType;
        if (!EncodingUtils.TryParseContentTypeEncoding(contentType, Encoding.UTF8, out mediaType, out contentEncoding) ||
            mediaType != "text/xml")
          return;
        Stream stream = new MemoryStream(request.MessageBody);
        HandleEventNotification(stream, contentEncoding, service, _upnpVersion);
      }
    }

    public static HttpStatusCode HandleEventNotification(Stream stream, Encoding streamEncoding, CpService service,
        UPnPVersion upnpVersion)
    {
      try
      {
        // Parse XML document
        using (StreamReader streamReader = new StreamReader(stream, streamEncoding))
          using (XmlReader reader = XmlReader.Create(streamReader, UPnPConfiguration.DEFAULT_XML_READER_SETTINGS))
          {
            reader.MoveToContent();
            reader.ReadStartElement("propertyset", UPnPConsts.NS_UPNP_EVENT);
            while (reader.LocalName == "property" && reader.NamespaceURI == UPnPConsts.NS_UPNP_EVENT)
            {
              reader.ReadStartElement("property", UPnPConsts.NS_UPNP_EVENT);
              HandleVariableChangeNotification(reader, service, upnpVersion);
              reader.ReadEndElement(); // property
            }
            reader.Close();
          }
        return HttpStatusCode.OK;
      }
      catch (Exception e)
      {
        UPnPConfiguration.LOGGER.Warn("GENAClientController: Error handling event notification", e);
        return HttpStatusCode.BadRequest;
      }
    }

    protected static void HandleVariableChangeNotification(XmlReader reader, CpService service, UPnPVersion upnpVersion)
    {
      string variableName = reader.LocalName;
      CpStateVariable stateVariable;
      if (!service.StateVariables.TryGetValue(variableName, out stateVariable))
        // We don't know that variable - this is an error case but we won't raise an exception here
        return;
      object value = stateVariable.DataType.SoapDeserializeValue(reader, upnpVersion.VerMin == 0);
      service.InvokeStateVariableChanged(stateVariable, value);
    }
  }
}
