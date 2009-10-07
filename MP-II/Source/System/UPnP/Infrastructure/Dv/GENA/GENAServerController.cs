#region Copyright (C) 2005-2008 Team MediaPortal

/* 
 *  Copyright (C) 2005-2008 Team MediaPortal
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
using System.Net.Sockets;
using System.Text;
using System.Threading;
using HttpServer;
using MediaPortal.Utilities;
using UPnP.Infrastructure.Dv.DeviceTree;
using UPnP.Infrastructure.Dv.HTTP;
using UPnP.Infrastructure.Utils;

namespace UPnP.Infrastructure.Dv.GENA
{
  /// <summary>
  /// Active controller class which attends the GENA protocol in a UPnP server.
  /// </summary>
  public class GENAServerController : IDisposable
  {
    /// <summary>
    /// Default timeout for GENA event subscriptions in seconds.
    /// </summary>
    public static int DEFAULT_SUBSCRIPTION_TIMEOUT = 600;

    /// <summary>
    /// Subscription expiration check timer interval in milliseconds.
    /// </summary>
    public static long TIMER_INTERVAL = 1000;

    /// <summary>
    /// Multicast address for GENA multicast sendings for IPv4.
    /// </summary>
    public static IPAddress GENA_MULTICAST_ADDRESS_V4 = new IPAddress(new byte[] {239, 255, 255, 246});

    /// <summary>
    /// Multicast address for GENA multicast sendings for IPv6.
    /// </summary>
    public static IPAddress GENA_MULTICAST_ADDRESS_V6 = IPAddress.Parse("FF02::130");

    /// <summary>
    /// Maximum number of variables which are evented in a single multicast (UDP) message.
    /// </summary>
    public const int MAX_MULTICAST_EVENT_VAR_COUNT = 5;

    protected Timer _expirationTimer;
    protected Timer _notificationTimer;
    protected ServerData _serverData;

    /// <summary>
    /// Creates a new <see cref="GENAServerController"/>.
    /// </summary>
    /// <param name="serverData">Global UPnP server data structure.</param>
    public GENAServerController(ServerData serverData)
    {
      _serverData = serverData;
      _expirationTimer = new Timer(OnExpirationTimerElapsed, null, TIMER_INTERVAL, TIMER_INTERVAL);
      _notificationTimer = new Timer(OnNotificationTimerElapsed, null, Timeout.Infinite, Timeout.Infinite);
    }

    public void Dispose()
    {
      Close();
    }

    /// <summary>
    /// Tidies up expired event subscriptions.
    /// </summary>
    private void OnExpirationTimerElapsed(object state)
    {
      lock (_serverData.SyncObj)
      {
        // Tidy up expired event subscriptions
        foreach (EndpointConfiguration config in _serverData.UPnPEndPoints)
        {
          ICollection<EventSubscription> removeSubscriptions = new List<EventSubscription>();
          DateTime now = DateTime.Now;
          foreach (EventSubscription subscription in config.EventSubscriptions)
            if (now > subscription.Expiration && subscription.EventingState.EventKey > 0) // Don't remove subscriptions whose initial notification was not sent yet
            {
              removeSubscriptions.Add(subscription);
              subscription.Dispose();
            }
          CollectionUtils.RemoveAll(config.EventSubscriptions, removeSubscriptions);
        }
      }
    }

    /// <summary>
    /// Timer callback method for sending asynchronous multicast events.
    /// </summary>
    private void OnNotificationTimerElapsed(object state)
    {
      lock (_serverData.SyncObj)
      {
        foreach (KeyValuePair<DvService, EventingState> kvp in _serverData.ServiceMulticastEventingState)
        {
          ICollection<DvStateVariable> variablesToEvent = kvp.Value.GetDueEvents();
          if (variablesToEvent != null)
            SendMulticastEventNotification(kvp.Key, variablesToEvent);
        }
      }
    }

    private void OnStateVariableChanged(DvStateVariable variable)
    {
      lock (_serverData.SyncObj)
      {
        // Unicast event notifications
        DvService service = variable.ParentService;
        foreach (EndpointConfiguration config in _serverData.UPnPEndPoints)
          foreach (EventSubscription subscription in config.EventSubscriptions)
            if (subscription.Service == service && !subscription.IsDisposed)
              subscription.StateVariableChanged(variable);

        // Multicast event notifications
        EventingState eventingState = _serverData.ServiceMulticastEventingState[variable.ParentService];
        if (variable.Multicast && eventingState.EventKey != 0)
        {
          eventingState.ModerateChangeEvent(variable);
          ScheduleMulticastEvents();
        }
      }
    }

    /// <summary>
    /// Configures the eventing timer to fire at the time of the next event in the pending events list.
    /// </summary>
    protected void ScheduleMulticastEvents()
    {
      lock (_serverData.SyncObj)
      {
        TimeSpan? nextScheduleTimeSpan = null;
        foreach (EventingState eventingState in _serverData.ServiceMulticastEventingState.Values)
        {
          TimeSpan? ts = eventingState.GetNextScheduleTimeSpan();
          if (!nextScheduleTimeSpan.HasValue || (ts.HasValue && ts.Value < nextScheduleTimeSpan.Value))
            nextScheduleTimeSpan = ts;
        }
        if (nextScheduleTimeSpan.HasValue)
          _notificationTimer.Change(nextScheduleTimeSpan.Value, Consts.INFINITE_TIMESPAN);
      }
    }

    protected EventSubscription FindEventSubscription(EndpointConfiguration config, string sid)
    {
      lock (_serverData.SyncObj)
        foreach (EventSubscription subscription in config.EventSubscriptions)
          if (subscription.SID == sid && !subscription.IsDisposed)
            return subscription;
      return null;
    }

    protected EventSubscription FindEventSubscription(string sid)
    {
      lock (_serverData.SyncObj)
      {
        foreach (EndpointConfiguration config in _serverData.UPnPEndPoints)
        {
          EventSubscription subscription = FindEventSubscription(config, sid);
          if (subscription != null)
            return subscription;
        }
      }
      return null;
    }

    protected static bool TryParseCallbackURLs(string callbackURLsStr, out ICollection<string> callbackURLs)
    {
      callbackURLs = null;
      callbackURLsStr = callbackURLsStr.Trim();
      int i = 0;
      while (i < callbackURLsStr.Length)
      {
        if (callbackURLsStr[i] != '<')
          return false;
        int j = callbackURLsStr.IndexOf('>', i);
        if (j == -1)
          return false;
        if (callbackURLs == null)
          callbackURLs = new List<string>();
        callbackURLs.Add(callbackURLsStr.Substring(i + 1, j));
        i = j + 1;
        // Find beginning of next URL
        j = callbackURLsStr.IndexOf('<', i);
        if (j > -1)
          i = j;
      }
      return true;
    }

    protected void RegisterChangeEventsRecursive(IEnumerable<DvDevice> devices)
    {
      foreach (DvDevice device in devices)
      {
        foreach (DvService service in device.Services)
          service.StateVariableChanged += OnStateVariableChanged;
        RegisterChangeEventsRecursive(device.EmbeddedDevices);
      }
    }

    protected void UnregisterChangeEventsRecursive(IEnumerable<DvDevice> devices)
    {
      foreach (DvDevice device in devices)
      {
        foreach (DvService service in device.Services)
          service.StateVariableChanged -= OnStateVariableChanged;
        RegisterChangeEventsRecursive(device.EmbeddedDevices);
      }
    }

    /// <summary>
    /// Starts the GENA subsystem. This will register this GENA controller at all UPnP services for change
    /// events of state variables.
    /// </summary>
    public void Start()
    {
      RegisterChangeEventsRecursive(_serverData.Server.RootDevices);
      DateTime now = DateTime.Now;
      foreach (KeyValuePair<DvService, EventingState> kvp in _serverData.ServiceMulticastEventingState)
      {
        ICollection<DvStateVariable> multicastVariables = new List<DvStateVariable>();
        foreach (DvStateVariable variable in kvp.Key.StateVariables.Values)
          if (variable.Multicast)
            multicastVariables.Add(variable);
        kvp.Value.ScheduleEventNotification(multicastVariables, now);
      }
      ScheduleMulticastEvents();
    }

    /// <summary>
    /// Stops the GENA subsystem. This will unregister this GENA controller at all UPnP services for change
    /// events of state variables.
    /// </summary>
    public void Close()
    {
      UnregisterChangeEventsRecursive(_serverData.Server.RootDevices);
    }

    /// <summary>
    /// Initializes the given network endpoint for the GENA subsystem.
    /// </summary>
    /// <param name="config">The endpoint configuration which should be initialized.</param>
    public void InitializeGENAEndpoint(EndpointConfiguration config)
    {
      config.GENA_UDPClient = new UdpClient(config.EndPointIPAddress.AddressFamily)
        {
          Ttl = Configuration.DEFAULT_GENA_UDP_TTL_V4
        };
      if (config.EndPointIPAddress.AddressFamily == AddressFamily.InterNetwork)
        config.GENAMulticastAddress = GENA_MULTICAST_ADDRESS_V4;
      else if (config.EndPointIPAddress.AddressFamily == AddressFamily.InterNetworkV6)
        config.GENAMulticastAddress = GENA_MULTICAST_ADDRESS_V6;
      else
        return;
      config.EndPointGENAPort = Consts.GENA_MULTICAST_PORT;
    }

    /// <summary>
    /// Closes the GENA part of the given network endpoint.
    /// </summary>
    /// <param name="config">The endpoint configuration which should be closed.</param>
    public void CloseGENAEndpoint(EndpointConfiguration config)
    {
      config.GENA_UDPClient.Close();
    }

    /// <summary>
    /// Handles SUBSCRIBE and UNSUBSCRIBE HTTP requests.
    /// </summary>
    /// <param name="request">The HTTP request instance to handle</param>
    /// <param name="context">The HTTP client context of the specified <paramref name="request"/>.</param>
    /// <param name="config">The UPnP endpoint over that the HTTP request was received.</param>
    /// <returns><c>true</c> if the request could be handled and a HTTP response was sent, else <c>false</c>.</returns>
    public bool HandleHTTPRequest(IHttpRequest request, IHttpClientContext context, EndpointConfiguration config)
    {
      if (request.Method == "SUBSCRIBE")
      { // SUBSCRIBE events
        string uri = request.Uri.AbsoluteUri;
        DvService service;
        if (config.EventSubURLsToServices.TryGetValue(uri, out service))
        {
          IHttpResponse response = request.CreateResponse(context);
          string httpVersion = request.HttpVersion;
          string userAgentStr = request.Headers.Get("USER-AGENT");
          string callbackURLsStr = request.Headers.Get("CALLBACK");
          string nt = request.Headers.Get("NT");
          string sid = request.Headers.Get("SID");
          string timeoutStr = request.Headers.Get("TIMEOUT");
          bool subscriberSupportsUPnP11;
          try
          {
            if (string.IsNullOrEmpty(userAgentStr))
              subscriberSupportsUPnP11 = false;
            else
            {
              int minorVersion;
              if (!ParserHelper.ParseUserAgentUPnP1MinorVersion(userAgentStr, out minorVersion))
              {
                response.Status = HttpStatusCode.BadRequest;
                response.Send();
                return true;
              }
              subscriberSupportsUPnP11 = minorVersion >= 1;
            }
          }
          catch (Exception)
          {
            response.Status = HttpStatusCode.BadRequest;
            response.Send();
            return true;
          }
          if (service.HasComplexStateVariables && !subscriberSupportsUPnP11)
          {
            response.Status = HttpStatusCode.ServiceUnavailable;
            response.Send();
            return true;
          }
          int timeout = DEFAULT_SUBSCRIPTION_TIMEOUT;
          ICollection<string> callbackURLs = null;
          if ((!string.IsNullOrEmpty(timeoutStr) && (!timeoutStr.StartsWith("Second-") ||
              !int.TryParse(timeoutStr.Substring("Second-".Length).Trim(), out timeout))) ||
              (!string.IsNullOrEmpty(callbackURLsStr) &&
              !TryParseCallbackURLs(callbackURLsStr, out callbackURLs)))
          {
            response.Status = HttpStatusCode.BadRequest;
            response.Send();
            return true;
          }
          if (!string.IsNullOrEmpty(sid) && (callbackURLs != null || !string.IsNullOrEmpty(nt)))
          {
            response.Status = HttpStatusCode.BadRequest;
            response.Reason = "Incompatible Header Fields";
            response.Send();
            return true;
          }
          if (callbackURLs != null && !string.IsNullOrEmpty(nt))
          { // Subscription
            bool validURLs = true;
            foreach (string url in callbackURLs)
              if (!url.StartsWith("http://"))
              {
                validURLs = false;
                break;
              }
            if (nt != "upnp:event" || !validURLs)
            {
              response.Status = HttpStatusCode.PreconditionFailed;
              response.Reason = "Precondition Failed";
              response.Send();
              return true;
            }
            DateTime date;
            if (Subscribe(config, service, callbackURLs, httpVersion, subscriberSupportsUPnP11, ref timeout,
                out date, out sid))
            {
              response.Status = HttpStatusCode.OK;
              response.AddHeader("DATE", date.ToString("R"));
              response.AddHeader("SERVER", Configuration.UPnPMachineInfoHeader);
              response.AddHeader("SID", sid);
              response.AddHeader("CONTENT-LENGTH", "0");
              response.AddHeader("TIMEOUT", "Second-"+timeout);
              response.Send();
              SendInitialEventNotification(sid);
              return true;
            }
            else
            {
              response.Status = HttpStatusCode.ServiceUnavailable;
              response.Reason = "Unable to accept renewal"; // See (DevArch), table 4-4
              response.Send();
              return true;
            }
          }
          else if (!string.IsNullOrEmpty(sid))
          { // Renewal
            DateTime date;
            if (RenewSubscription(config, sid, ref timeout, out date))
            {
              response.Status = HttpStatusCode.OK;
              response.AddHeader("DATE", date.ToString("R"));
              response.AddHeader("SERVER", Configuration.UPnPMachineInfoHeader);
              response.AddHeader("SID", sid);
              response.AddHeader("CONTENT-LENGTH", "0");
              response.AddHeader("TIMEOUT", "Second-"+timeout);
              response.Send();
              return true;
            }
            else
            {
              response.Status = HttpStatusCode.ServiceUnavailable;
              response.Reason = "Unable to accept renewal";
              response.Send();
              return true;
            }
          }
        }
      }
      else if (request.Method == "UNSUBSCRIBE")
      { // UNSUBSCRIBE events
        string uri = request.Uri.AbsoluteUri;
        DvService service;
        if (config.EventSubURLsToServices.TryGetValue(uri, out service))
        {
          IHttpResponse response = request.CreateResponse(context);
          string sid = request.Headers.Get("SID");
          string callbackURL = request.Headers.Get("CALLBACK");
          string nt = request.Headers.Get("NT");
          if (string.IsNullOrEmpty(sid) || !string.IsNullOrEmpty(callbackURL) || !string.IsNullOrEmpty(nt))
          {
            response.Status = HttpStatusCode.BadRequest;
            response.Reason = "Incompatible Header Fields";
            response.Send();
            return true;
          }
          if (Unsubscribe(config, sid))
          {
            response.Status = HttpStatusCode.OK;
            response.Send();
            return true;
          }
          else
          {
            response.Status = HttpStatusCode.PreconditionFailed;
            response.Reason = "Precondition Failed";
            response.Send();
            return true;
          }
        }
      }
      return false;
    }

    /// <summary>
    /// Adds an event subscriber to the collection of subscribers for the given <paramref name="service"/>.
    /// </summary>
    /// <remarks>
    /// After this method returned <c>true</c>, the caller must send the HTTP response to notify the subscriber
    /// that the event subscription was accepted and to give it the necessary <paramref name="sid"/> which was generated
    /// for the event subscription. After that response was sent, the method <see cref="SendInitialEventNotification"/>
    /// needs to be called with the generated <paramref name="sid"/> value, to complete the event subscription.
    /// </remarks>
    /// <param name="config">Network endpoint which received the event subscription.</param>
    /// <param name="service">UPnP service where the subscription is made.</param>
    /// <param name="callbackURLs">Collection of URLs where change events are sent to (over the given endpoint
    /// <paramref name="config"/>). The URLs will be tried in order until one succeeds.</param>
    /// <param name="httpVersion">HTTP version of the subscriber.</param>
    /// <param name="subscriberSupportsUPnP11">Should be set to <c>true</c> if the subscriber has a user agent header which
    /// says that it uses UPnP 1.1.</param>
    /// <param name="timeout">The input value contains the timeout in seconds which was requested by the subscriber.
    /// The returned value for this parameter will contain the actual timeout in seconds for the new event
    /// subscription.</param>
    /// <param name="date">Date and time when the event subscription was registered. The event subscription
    /// will expire at <paramref name="date"/> plus <paramref name="timeout"/>.</param>
    /// <param name="sid">Generated sid for the new event subscription.</param>
    /// <returns><c>true</c>, if the event registration was accepted, else <c>false</c>.</returns>
    public bool Subscribe(EndpointConfiguration config, DvService service, ICollection<string> callbackURLs,
        string httpVersion, bool subscriberSupportsUPnP11, ref int timeout, out DateTime date, out string sid)
    {
      lock (_serverData.SyncObj)
      {
        Guid id = Guid.NewGuid();
        sid = "uuid:" + id.ToString("D");
        date = DateTime.Now;
        DateTime expiration = date.AddSeconds(timeout);
        config.EventSubscriptions.Add(new EventSubscription(sid, service, callbackURLs, expiration, httpVersion, subscriberSupportsUPnP11, config, _serverData));
        return true;
      }
    }

    /// <summary>
    /// SHOULD be called as soon as possible after the subscriber was notified about the successful subscription. MUST be called
    /// after the subscriber got the subscription id for the subscription.
    /// </summary>
    /// <param name="sid">Id of the subscription for that the initial event notification should be send.</param>
    public void SendInitialEventNotification(string sid)
    {
      lock (_serverData.SyncObj)
      {
        EventSubscription subscription = FindEventSubscription(sid);
        if (subscription == null)
          return;
        ICollection<DvStateVariable> variables = new List<DvStateVariable>();
        foreach (DvStateVariable variable in subscription.Service.StateVariables.Values)
          if (variable.SendEvents)
            variables.Add(variable);
        subscription.ScheduleEventNotification(variables);
      }
    }

    /// <summary>
    /// Renews the subscription with the given <paramref name="sid"/>.
    /// </summary>
    /// <param name="config">UPnP endpoint where the unsubscribe request was received.</param>
    /// <param name="sid">Subscription sid to unsubscribe.</param>
    /// <param name="timeout">The input value contains the new timeout in seconds which was requested by the subscriber.
    /// The returned value for this parameter will contain the actual timeout in seconds for the new event
    /// subscription.</param>
    /// <param name="date">Date and time when the subscription renewal was executed. The event subscription
    /// will expire at <paramref name="date"/> plus <paramref name="timeout"/>.</param>
    /// <returns><c>true</c>, if the subscription was successfully unsubscribed, else <c>false</c>.</returns>
    public bool RenewSubscription(EndpointConfiguration config, string sid, ref int timeout, out DateTime date)
    {
      lock (_serverData.SyncObj)
      {
        EventSubscription subscription = FindEventSubscription(config, sid);
        if (subscription == null)
        {
          date = new DateTime();
          return false;
        }
        date = DateTime.Now;
        DateTime expiration = date.AddSeconds(timeout);
        subscription.Expiration = expiration;
        return true;
      }
    }

    /// <summary>
    /// Unsubscribes the subscription with the given <paramref name="sid"/>.
    /// </summary>
    /// <param name="config">UPnP endpoint where the unsubscribe request was received.</param>
    /// <param name="sid">Subscription sid to unsubscribe.</param>
    /// <returns><c>true</c>, if the subscription was successfully unsubscribed, else <c>false</c>.</returns>
    public bool Unsubscribe(EndpointConfiguration config, string sid)
    {
      lock (_serverData.SyncObj)
      {
        EventSubscription subscription = FindEventSubscription(config, sid);
        if (subscription == null)
          return false;
        if (subscription.EventingState.EventKey == 0)
          // Make initial notification be sent although the deregistration was done
          subscription.Expiration = DateTime.MinValue;
        else
        {
          config.EventSubscriptions.Remove(subscription);
          subscription.Dispose();
        }
        return true;
      }
    }

    protected void SendMulticastEventNotification(DvService service, IEnumerable<DvStateVariable> variables)
    {
      DvDevice device = service.ParentDevice;
      EventingState eventingState = _serverData.ServiceMulticastEventingState[service];
      // First cluster variables by multicast event level so we can put variables of the same event level into a single message
      IDictionary<string, ICollection<DvStateVariable>> variablesByLevel =
          new Dictionary<string, ICollection<DvStateVariable>>();
      foreach (DvStateVariable variable in variables)
      {
        ICollection<DvStateVariable> variablesCollection;
        if (!variablesByLevel.TryGetValue(variable.MulticastEventLevel, out variablesCollection))
          variablesByLevel[variable.MulticastEventLevel] = variablesCollection = new List<DvStateVariable>();
        variablesCollection.Add(variable);
      }
      foreach (KeyValuePair<string, ICollection<DvStateVariable>> varByLevel in variablesByLevel)
      {
        // Use a maximum cluster size of MAX_MULTICAST_EVENT_VAR_COUNT to keep UDP message small
        ICollection<IList<DvStateVariable>> variableClusters = CollectionUtils.Cluster(
            varByLevel.Value, MAX_MULTICAST_EVENT_VAR_COUNT);
        foreach (IList<DvStateVariable> cluster in variableClusters)
        {
          foreach (DvStateVariable variable in cluster)
            eventingState.UpdateModerationData(variable);
          byte[] bodyData = Encoding.UTF8.GetBytes(GENAMessageBuilder.BuildEventNotificationMessage(
            cluster, false)); // Albert TODO: Is it correct not to force the simple string equivalent for extended data types here?
          SimpleHTTPRequest request = new SimpleHTTPRequest("NOTIFY", "*");
          request.SetHeader("CONTENT-LENGTH", bodyData.Length.ToString());
          request.SetHeader("CONTENT-TYPE", "text/xml; charset=\"utf-8\"");
          request.SetHeader("USN", device.UDN + "::" + service.ServiceTypeVersion_URN);
          request.SetHeader("SVCID", service.ServiceId);
          request.SetHeader("NT", "upnp:event");
          request.SetHeader("NTS", "upnp:propchange");
          request.SetHeader("SEQ", _serverData.GetNextMulticastEventKey(service).ToString());
          request.SetHeader("LVL", varByLevel.Key);
          request.SetHeader("BOOTID.UPNP.ORG", _serverData.BootId.ToString());

          foreach (EndpointConfiguration config in _serverData.UPnPEndPoints)
          {
            IPEndPoint ep = new IPEndPoint(config.GENAMulticastAddress, (int) config.EndPointGENAPort);
            request.SetHeader("HOST", ep.ToString());
            request.MessageBody = bodyData;
            byte[] bytes = request.Encode();
            config.GENA_UDPClient.Send(bytes, bytes.Length, ep);
          }
        }
      }
    }
  }
}
