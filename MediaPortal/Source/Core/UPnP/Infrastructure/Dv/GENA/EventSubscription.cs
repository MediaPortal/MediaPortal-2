#region Copyright (C) 2007-2021 Team MediaPortal

/*
    Copyright (C) 2007-2021 Team MediaPortal
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

using MediaPortal.Utilities.Network;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using UPnP.Infrastructure.Dv.DeviceTree;

namespace UPnP.Infrastructure.Dv.GENA
{
  /// <summary>
  /// Instances of this class hold all data of a UPnP GENA event subscription.
  /// </summary>
  /// <remarks>
  /// An event subscription is made by a UPnP control point at a UPnP service. Every subscription is made on a
  /// by-service basis, i.e. when a subscription is made at a given service, the subscriber will receive ALL
  /// event messages for that service. No mechanism is provided to subscribe to event messages on
  /// a variable-by-variable basis (See DevArch, 4.1.1).
  /// </remarks>
  public class EventSubscription
  {
    /// <summary>
    /// Time in seconds to wait until an answer is received for a unicast event notification.
    /// </summary>
    public const int PENDING_EVENT_NOTIFICATION_TIMEOUT = 10;

    // Static as multiple instances of HttpClient should be avoided to limit socket usage
    protected static LocalEndPointHttpClient _httpClient;

    protected string _sid;
    protected DvService _service;
    protected ICollection<string> _callbackURLs;
    protected DateTime _expiration;
    protected string _subscriberHTTPVersion;
    protected bool _subscriberSupportsUPnP11;
    protected EndpointConfiguration _config;
    protected ServerData _serverData;
    protected Timer _notificationTimer;
    protected bool _disposed = false;
    protected EventingState _eventingState = new EventingState();
    protected ICollection<AsyncRequestState> _pendingRequests = new List<AsyncRequestState>();
    protected CancellationTokenSource _cancellationTokenSource;

    protected internal class AsyncRequestState
    {
      protected HttpRequestMessage _request;
      protected Task _requestTask;
      protected EndpointConfiguration _endpoint;
      protected string _sid;
      protected ICollection<string> _pendingCallbackURLs;
      protected uint _eventKey;
      protected byte[] _messageData;

      public AsyncRequestState(EndpointConfiguration endpoint, string sid, IEnumerable<string> pendingCallbackURLs, uint eventKey, byte[] messageData)
      {
        _endpoint = endpoint;
        _pendingCallbackURLs = new List<string>(pendingCallbackURLs);
        _sid = sid;
        _eventKey = eventKey;
        _messageData = messageData;
      }

      public HttpRequestMessage Request
      {
        get { return _request; }
        set { _request = value; }
      }

      public Task RequestTask
      {
        get { return _requestTask; }
        set { _requestTask = value; }
      }

      public string SID
      {
        get { return _sid; }
      }

      public EndpointConfiguration Endpoint
      {
        get { return _endpoint; }
      }

      public ICollection<string> PendingCallbackURLs
      {
        get { return _pendingCallbackURLs; }
      }

      public uint EventKey
      {
        get { return _eventKey; }
      }

      public byte[] MessageData
      {
        get { return _messageData; }
      }
    }

    static EventSubscription()
    {
      _httpClient = LocalEndPointHttpClient.Create();
      _httpClient.Timeout = TimeSpan.FromSeconds(PENDING_EVENT_NOTIFICATION_TIMEOUT);
    }

    public EventSubscription(string sid, DvService service, ICollection<string> callbackURLs, DateTime expiration,
        string httpVersion, bool subscriberSupportsUPnP11, EndpointConfiguration config, ServerData serverData)
    {
      _sid = sid;
      _service = service;
      _callbackURLs = callbackURLs;
      _expiration = expiration;
      _subscriberHTTPVersion = httpVersion;
      _subscriberSupportsUPnP11 = subscriberSupportsUPnP11;
      _config = config;
      _serverData = serverData;
      _notificationTimer = new Timer(OnNotificationTimerElapsed, null, Timeout.Infinite, Timeout.Infinite);
      _cancellationTokenSource = new CancellationTokenSource();
    }

    public void Dispose()
    {
      List<AsyncRequestState> pendingRequests;
      lock (_serverData.SyncObj)
      {
        _disposed = true;
        pendingRequests = new List<AsyncRequestState>(_pendingRequests);
      }
      _cancellationTokenSource.Cancel();
      foreach (AsyncRequestState state in new List<AsyncRequestState>(_pendingRequests))
        state.RequestTask?.Wait();
      _cancellationTokenSource.Dispose();
    }

    private void OnNotificationTimerElapsed(object state)
    {
      ICollection<DvStateVariable> variablesToEvent;
      lock (_serverData.SyncObj)
      {
        if (_disposed)
          return;
        variablesToEvent = _eventingState.GetDueEvents();
      }
      if (variablesToEvent != null)
        SendEventNotification(variablesToEvent);
    }

    /// <summary>
    /// Tries to send the event message specified by the given <paramref name="state"/> object to the next
    /// available event callback URL, if present.
    /// </summary>
    private void ContinueEventNotification(AsyncRequestState state)
    {
      IEnumerator<string> e = state.PendingCallbackURLs.GetEnumerator();
      if (!e.MoveNext())
      {
        lock (_serverData.SyncObj)
          _pendingRequests.Remove(state);
        return;
      }
      string callbackURL = e.Current;
      state.PendingCallbackURLs.Remove(callbackURL);

      HttpRequestMessage httpRequest = new HttpRequestMessage(new HttpMethod("NOTIFY"), callbackURL);
      LocalEndPointHttpClient.SetLocalEndpoint(httpRequest, _config.EndPointIPAddress);
      httpRequest.Headers.ConnectionClose = true;
      httpRequest.Headers.Add("NT", "upnp:event");
      httpRequest.Headers.Add("NTS", "upnp:propchange");
      httpRequest.Headers.Add("SID", _sid);
      httpRequest.Headers.Add("SEQ", _eventingState.EventKey.ToString());

      ByteArrayContent requestContent = new ByteArrayContent(state.MessageData);
      requestContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/xml; charset=\"utf-8\"");
      httpRequest.Content = requestContent;

      state.Request = httpRequest;
      state.RequestTask = _httpClient.SendAsync(httpRequest, _cancellationTokenSource.Token).ContinueWith(response => OnEventResponseReceived(response, state));
    }

    private void OnEventResponseReceived(Task<HttpResponseMessage> responseTask, AsyncRequestState state)
    {
      HttpResponseMessage response = null;
      try
      {
        response = responseTask.GetAwaiter().GetResult();
        if (response.StatusCode == HttpStatusCode.OK)
          // When one callback URL succeeded, break event notification for this SID
          lock (_serverData.SyncObj)
          {
            _pendingRequests.Remove(state);
            return;
          }
      }
      catch (TaskCanceledException)
      {
        // This instance is probably disposing, break event notification
        return;
      }
      catch (HttpRequestException)
      {
      }
      finally
      {
        if (response != null)
          response.Dispose();
      }
      // Try next callback URL
      ContinueEventNotification(state);
    }

    public bool IsDisposed
    {
      get { return _disposed; }
    }

    public string SID
    {
      get { return _sid; }
    }

    public DvService Service
    {
      get { return _service; }
    }

    public ICollection<string> CallbackURLs
    {
      get { return _callbackURLs; }
    }

    public DateTime Expiration
    {
      get { return _expiration; }
      internal set { _expiration = value; }
    }

    public string SubscriberHTTPVersion
    {
      get { return _subscriberHTTPVersion; }
    }

    public EndpointConfiguration EndPoint
    {
      get { return _config; }
    }

    public EventingState EventingState
    {
      get { return _eventingState; }
    }

    internal ICollection<AsyncRequestState> PendingRequests
    {
      get { return _pendingRequests; }
    }

    /// <summary>
    /// Called when a state variable has changed its value.
    /// </summary>
    /// <remarks>
    /// This method will check if the variable is evented. If not, nothing happens. If yes, and the variable
    /// is moderated in rate or minimum change, it will be scheduled to be evented later, or will
    /// be evented at once, depending on the moderation.
    /// </remarks>
    /// <param name="variable">The variable which changed its value.</param>
    public void StateVariableChanged(DvStateVariable variable)
    {
      if (!variable.SendEvents)
        return;
      bool expired;
      lock (_serverData.SyncObj)
      {
        // Only send event if:
        // - Subscription is not expired
        // - Initial event was already sent
        expired = DateTime.Now > Expiration;
        if (!expired)
        {
          if (_eventingState.EventKey == 0)
            // Avoid sending "normal" change events before the initial event was sent
            return;
          _eventingState.ModerateChangeEvent(variable);
        }
      }
      // Outside the lock
      if (expired)
        Dispose();
      else
        ScheduleEvents();
    }

    /// <summary>
    /// Schedules a change event for the specified variables to be sent in a timely manner.
    /// </summary>
    /// <param name="variables">The variables for whom the event notification will be sent.</param>
    public void ScheduleEventNotification(IEnumerable<DvStateVariable> variables)
    {
      ScheduleEventNotification(variables, DateTime.Now);
    }

    /// <summary>
    /// Schedules a change event for the specified variables at the given <paramref name="scheduleTime"/>.
    /// </summary>
    /// <param name="variables">The variables for whom the event notification will be sent.</param>
    /// <param name="scheduleTime">Time when the change event will be scheduled.</param>
    public void ScheduleEventNotification(IEnumerable<DvStateVariable> variables, DateTime scheduleTime)
    {
      lock (_serverData.SyncObj)
      {
        _eventingState.ScheduleEventNotification(variables, scheduleTime);
        ScheduleEvents();
      }
    }

    /// <summary>
    /// Configures the eventing timer to fire at the time of the next event in the pending events list.
    /// </summary>
    protected void ScheduleEvents()
    {
      lock (_serverData.SyncObj)
      {
        TimeSpan? nextScheduleTimeSpan = _eventingState.GetNextScheduleTimeSpan();
        if (nextScheduleTimeSpan.HasValue)
          _notificationTimer.Change(nextScheduleTimeSpan.Value, UPnPConsts.INFINITE_TIMESPAN);
      }
    }

    /// <summary>
    /// Sends an event notification to our subscriber for the specified <paramref name="variables"/>.
    /// </summary>
    protected void SendEventNotification(IEnumerable<DvStateVariable> variables)
    {
      AsyncRequestState state;
      lock (_serverData.SyncObj)
      {
        foreach (DvStateVariable variable in variables)
          _eventingState.UpdateModerationData(variable);
        String body;
        lock (_serverData.SyncObj)
          body = GENAMessageBuilder.BuildEventNotificationMessage(variables, !_subscriberSupportsUPnP11);
        byte[] bodyData = UPnPConsts.UTF8_NO_BOM.GetBytes(body);

        state = new AsyncRequestState(_config, _sid, _callbackURLs, _eventingState.EventKey, bodyData);
        _pendingRequests.Add(state);
        _eventingState.IncEventKey();
      }
      // Outside the lock
      ContinueEventNotification(state);
    }
  }
}
