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
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Web;
using MediaPortal.Utilities;
using Microsoft.Owin;
using UPnP.Infrastructure.Dv.DeviceTree;
using UPnP.Infrastructure.Utils.HTTP;
using UPnP.Infrastructure.Utils;

namespace UPnP.Infrastructure.Dv.GENA
{
  /// <summary>
  /// Active controller class which attends the GENA protocol in a UPnP server.
  /// </summary>
  public class GENAServerController : IDisposable
  {
    /// <summary>
    /// Subscription expiration check timer interval in milliseconds.
    /// </summary>
    public static long TIMER_INTERVAL = 1000;

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
      // If we cannot acquire our lock for some reason, avoid blocking an infinite number of timer threads here
      if (Monitor.TryEnter(_serverData.SyncObj, UPnPConsts.TIMEOUT_TIMER_LOCK_ACCESS))
      {
        List<EventSubscription> removeSubscriptions = new List<EventSubscription>();
        try
        {
          // Tidy up expired event subscriptions
          foreach (EndpointConfiguration config in _serverData.UPnPEndPoints)
          {
            DateTime now = DateTime.Now;
            // Don't remove subscriptions whose initial notification was not sent yet
            removeSubscriptions.AddRange(config.EventSubscriptions.Where(subscription => now > subscription.Expiration && subscription.EventingState.EventKey > 0));
            CollectionUtils.RemoveAll(config.EventSubscriptions, removeSubscriptions);
          }
        }
        finally
        {
          Monitor.Exit(_serverData.SyncObj);
        }
        // Outside the lock
        foreach (EventSubscription subscription in removeSubscriptions)
          subscription.Dispose();
      }
      else
        UPnPConfiguration.LOGGER.Error("GENAServerController.OnExpirationTimerElapsed: Cannot acquire synchronization lock. Maybe a deadlock happened.");
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
        if (variable.Multicast)
        {
          EventingState eventingState = _serverData.GetMulticastEventKey(variable.ParentService);
          if (eventingState.EventKey == 0)
            // Avoid sending "normal" change events before the initial event was sent
            return;
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
          if (!nextScheduleTimeSpan.HasValue || (ts < nextScheduleTimeSpan.Value))
            nextScheduleTimeSpan = ts;
        }
        if (nextScheduleTimeSpan.HasValue)
          _notificationTimer.Change(nextScheduleTimeSpan.Value, UPnPConsts.INFINITE_TIMESPAN);
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
        callbackURLs.Add(callbackURLsStr.Substring(i + 1, j - i - 1));
        i = j + 1;
        // Find beginning of next URL
        j = callbackURLsStr.IndexOf('<', i);
        if (j == -1)
          break;
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
        UnregisterChangeEventsRecursive(device.EmbeddedDevices);
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
        ICollection<DvStateVariable> multicastVariables = kvp.Key.StateVariables.Values.Where(variable => variable.Multicast).ToList();
        if (multicastVariables.Count > 0)
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
      Socket socket = new Socket(config.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
      config.GENA_UDP_Socket = socket;
      if (config.AddressFamily == AddressFamily.InterNetwork)
      {
        try
        {
          socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, UPnPConfiguration.GENA_UDP_TTL_V4);
        }
        catch (SocketException e)
        {
          UPnPConfiguration.LOGGER.Warn("GENAServerController: Could not set MulticastTimeToLive", e);
        }
      }
      else if (config.AddressFamily == AddressFamily.InterNetworkV6)
      {
        try
        {
          socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.HopLimit, UPnPConfiguration.GENA_UDP_HOP_LIMIT_V6);
        }
        catch (SocketException e)
        {
          UPnPConfiguration.LOGGER.Warn("GENAServerController: Could not set HopLimit", e);
        }
      }
      config.GENAMulticastAddress = NetworkHelper.GetGENAMulticastAddressForInterface(config.EndPointIPAddress);
    }

    /// <summary>
    /// Closes the GENA part of the given network endpoint.
    /// </summary>
    /// <param name="config">The endpoint configuration which should be closed.</param>
    public void CloseGENAEndpoint(EndpointConfiguration config)
    {
      config.GENA_UDP_Socket.Close();
    }

    /// <summary>
    /// Handles SUBSCRIBE and UNSUBSCRIBE HTTP requests.
    /// </summary>
    /// <param name="request">The HTTP request instance to handle</param>
    /// <param name="context">The HTTP client context of the specified <paramref name="request"/>.</param>
    /// <param name="config">The UPnP endpoint over that the HTTP request was received.</param>
    /// <returns><c>true</c> if the request could be handled and a HTTP response was sent, else <c>false</c>.</returns>
    public bool HandleHTTPRequest(IOwinRequest request, IOwinContext context, EndpointConfiguration config)
    {
      var response = context.Response;
      if (request.Method == "SUBSCRIBE")
      { // SUBSCRIBE events
        string pathAndQuery = HttpUtility.UrlDecode(request.Uri.PathAndQuery);
        DvService service;
        if (config.EventSubPathsToServices.TryGetValue(pathAndQuery, out service))
        {
          string httpVersion = request.Protocol;
          string userAgentStr = request.Headers.Get("USER-AGENT");
          string callbackURLsStr = request.Headers.Get("CALLBACK");
          string nt = request.Headers.Get("NT");
          string sid = request.Headers.Get("SID");
          string timeoutStr = request.Headers.Get("TIMEOUT");
          int timeout = UPnPConsts.GENA_DEFAULT_SUBSCRIPTION_TIMEOUT;
          ICollection<string> callbackURLs = null;
          if ((!string.IsNullOrEmpty(timeoutStr) && (!timeoutStr.StartsWith("Second-") ||
              !int.TryParse(timeoutStr.Substring("Second-".Length).Trim(), out timeout))) ||
              (!string.IsNullOrEmpty(callbackURLsStr) &&
              !TryParseCallbackURLs(callbackURLsStr, out callbackURLs)))
          {
            response.StatusCode = (int)HttpStatusCode.BadRequest;
            return true;
          }
          if (!string.IsNullOrEmpty(sid) && (callbackURLs != null || !string.IsNullOrEmpty(nt)))
          {
            response.StatusCode = (int)HttpStatusCode.BadRequest;
            response.ReasonPhrase = "Incompatible Header Fields";
            return true;
          }
          if (callbackURLs != null && !string.IsNullOrEmpty(nt))
          { // Subscription
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
                  response.StatusCode = (int)HttpStatusCode.BadRequest;
                  return true;
                }
                subscriberSupportsUPnP11 = minorVersion >= 1;
              }
            }
            catch (Exception e)
            {
              UPnPConfiguration.LOGGER.Warn("GENAServerController: Error in event subscription", e);
              response.StatusCode = (int)HttpStatusCode.BadRequest;
              return true;
            }
            if (service.HasComplexStateVariables && !subscriberSupportsUPnP11)
            {
              response.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
              return true;
            }
            bool validURLs = callbackURLs.All(url => url.StartsWith("http://"));
            if (nt != "upnp:event" || !validURLs)
            {
              response.StatusCode = (int)HttpStatusCode.PreconditionFailed;
              response.ReasonPhrase = "Precondition Failed";
              return true;
            }
            DateTime date;
            if (Subscribe(config, service, callbackURLs, httpVersion, subscriberSupportsUPnP11, ref timeout,
                out date, out sid))
            {
              response.StatusCode = (int)HttpStatusCode.OK;
              response.Headers["DATE"] = date.ToUniversalTime().ToString("R");
              response.Headers["SERVER"] = UPnPConfiguration.UPnPMachineInfoHeader;
              response.Headers["SID"] = sid;
              response.Headers["CONTENT-LENGTH"] = "0";
              response.Headers["TIMEOUT"] = "Second-" + timeout;
              SendInitialEventNotification(sid);
              return true;
            }
            response.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
            response.ReasonPhrase = "Unable to accept renewal"; // See (DevArch), table 4-4
            return true;
          }
          if (!string.IsNullOrEmpty(sid))
          { // Renewal
            DateTime date;
            if (RenewSubscription(config, sid, ref timeout, out date))
            {
              response.StatusCode = (int)HttpStatusCode.OK;
              response.Headers["DATE"] = date.ToUniversalTime().ToString("R");
              response.Headers["SERVER"] = UPnPConfiguration.UPnPMachineInfoHeader;
              response.Headers["SID"] = sid;
              response.Headers["CONTENT-LENGTH"] = "0";
              response.Headers["TIMEOUT"] = "Second-" + timeout;
              return true;
            }
            response.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
            response.ReasonPhrase = "Unable to accept renewal";
            return true;
          }
        }
      }
      else if (request.Method == "UNSUBSCRIBE")
      { // UNSUBSCRIBE events
        string pathAndQuery = HttpUtility.UrlDecode(request.Uri.PathAndQuery);
        DvService service;
        if (config.EventSubPathsToServices.TryGetValue(pathAndQuery, out service))
        {
          string sid = request.Headers.Get("SID");
          string callbackURL = request.Headers.Get("CALLBACK");
          string nt = request.Headers.Get("NT");
          if (string.IsNullOrEmpty(sid) || !string.IsNullOrEmpty(callbackURL) || !string.IsNullOrEmpty(nt))
          {
            response.StatusCode = (int)HttpStatusCode.BadRequest;
            response.ReasonPhrase = "Incompatible Header Fields";
            return true;
          }
          if (Unsubscribe(config, sid))
          {
            response.StatusCode = (int)HttpStatusCode.OK;
            return true;
          }
          response.StatusCode = (int)HttpStatusCode.PreconditionFailed;
          response.ReasonPhrase = "Precondition Failed";
          return true;
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
        ICollection<DvStateVariable> variables = subscription.Service.StateVariables.Values.Where(variable => variable.SendEvents).ToList();
        if (variables.Count > 0)
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
      EventingState eventingState = _serverData.GetMulticastEventKey(service);
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
        // Use a maximum cluster size of GENA_MAX_MULTICAST_EVENT_VAR_COUNT to keep UDP message small
        ICollection<IList<DvStateVariable>> variableClusters = CollectionUtils.Cluster(
            varByLevel.Value, UPnPConsts.GENA_MAX_MULTICAST_EVENT_VAR_COUNT);
        foreach (IList<DvStateVariable> cluster in variableClusters)
        {
          foreach (DvStateVariable variable in cluster)
            eventingState.UpdateModerationData(variable);
          eventingState.IncEventKey();
          byte[] bodyData = UPnPConsts.UTF8_NO_BOM.GetBytes(GENAMessageBuilder.BuildEventNotificationMessage(
            cluster, false)); // Albert TODO: Is it correct not to force the simple string equivalent for extended data types here?
          SimpleHTTPRequest request = new SimpleHTTPRequest("NOTIFY", "*");
          request.SetHeader("CONTENT-LENGTH", bodyData.Length.ToString());
          request.SetHeader("CONTENT-TYPE", "text/xml; charset=\"utf-8\"");
          request.SetHeader("USN", device.UDN + "::" + service.ServiceTypeVersion_URN);
          request.SetHeader("SVCID", service.ServiceId);
          request.SetHeader("NT", "upnp:event");
          request.SetHeader("NTS", "upnp:propchange");
          request.SetHeader("SEQ", eventingState.EventKey.ToString());
          request.SetHeader("LVL", varByLevel.Key);
          request.SetHeader("BOOTID.UPNP.ORG", _serverData.BootId.ToString());

          foreach (EndpointConfiguration config in _serverData.UPnPEndPoints)
          {
            IPEndPoint ep = new IPEndPoint(config.GENAMulticastAddress, UPnPConsts.GENA_MULTICAST_PORT);
            request.SetHeader("HOST", NetworkHelper.IPEndPointToString(ep));
            request.MessageBody = bodyData;
            byte[] bytes = request.Encode();
            NetworkHelper.SendData(config.GENA_UDP_Socket, ep, bytes, 1);
          }
        }
      }
    }
  }
}
