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
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using UPnP.Infrastructure.Dv.DeviceTree;
using UPnP.Infrastructure.Utils;

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

    protected internal class AsyncRequestState
    {
      protected HttpWebRequest _httpWebRequest;
      protected string _sid;
      protected ICollection<string> _pendingCallbackURLs = new List<string>();
      protected uint _eventKey;
      protected byte[] _messageData;

      public AsyncRequestState(string sid, ICollection<string> pendingCallbackURLs, uint eventKey, byte[] messageData)
      {
        _pendingCallbackURLs = pendingCallbackURLs;
        _sid = sid;
        _eventKey = eventKey;
        _messageData = messageData;
      }

      public HttpWebRequest Request
      {
        get { return _httpWebRequest; }
        set { _httpWebRequest = value; }
      }

      public string SID
      {
        get { return _sid; }
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
    }

    public void Dispose()
    {
      lock (_serverData.SyncObj)
      {
        _disposed = true;
        foreach (AsyncRequestState state in _pendingRequests)
          state.Request.Abort();
      }
    }

    private void OnNotificationTimerElapsed(object state)
    {
      lock (_serverData.SyncObj)
      {
        if (_disposed)
          return;
        ICollection<DvStateVariable> variablesToEvent = _eventingState.GetDueEvents();
        if (variablesToEvent != null)
          SendEventNotification(variablesToEvent);
      }
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
        _pendingRequests.Remove(state);
        return;
      }
      string callbackURL = e.Current;
      state.PendingCallbackURLs.Remove(callbackURL);

      HttpWebRequest request = (HttpWebRequest) WebRequest.Create(callbackURL);
      request.Method = "NOTIFY";
      request.KeepAlive = false;
      request.ContentType = "text/xml; charset=\"utf-8\"";
      request.Headers.Add("NT", "upnp:event");
      request.Headers.Add("NTS", "upnp:propchange");
      request.Headers.Add("SID", _sid);
      request.Headers.Add("SEQ", _eventingState.EventKey.ToString());
      state.Request = request;

      // First get the request stream...
      IAsyncResult result = state.Request.BeginGetRequestStream(OnEventGetRequestStream, state);
      NetworkHelper.AddTimeout(request, result, PENDING_EVENT_NOTIFICATION_TIMEOUT * 1000);
    }

    private void OnEventGetRequestStream(IAsyncResult ar)
    {
      AsyncRequestState state = (AsyncRequestState) ar.AsyncState;
      try
      {
        Stream requestStream = state.Request.EndGetRequestStream(ar);
        // ... then write to it...
        requestStream.Write(state.MessageData, 0, state.MessageData.Length);
        requestStream.Close();
        // ... and get the response
        IAsyncResult result = state.Request.BeginGetResponse(OnEventResponseReceived, state);
        NetworkHelper.AddTimeout(state.Request, result, PENDING_EVENT_NOTIFICATION_TIMEOUT * 1000);
      }
      catch (IOException) { }
      catch (WebException) { }
      ContinueEventNotification(state);
    }

    private void OnEventResponseReceived(IAsyncResult ar)
    {
      AsyncRequestState state = (AsyncRequestState) ar.AsyncState;
      try
      {
        HttpWebResponse response = (HttpWebResponse) state.Request.EndGetResponse(ar);
        if (response.StatusCode == HttpStatusCode.OK)
          // When one callback URL succeeded, break event notification for this SID
          lock (_serverData.SyncObj)
          {
            _pendingRequests.Remove(state);
            return;
          }
      }
      catch (WebException e)
      {
        if (e.Response != null)
          e.Response.Close();
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
      lock (_serverData.SyncObj)
      {
        // Only send event if:
        // - Subscription is not expired
        // - Initial event was already sent
        if (DateTime.Now > Expiration)
        {
          Dispose();
          return;
        }
        if (_eventingState.EventKey == 0)
          // Avoid sending "normal" change events before the initial event was sent
          return;
        _eventingState.ModerateChangeEvent(variable);
        ScheduleEvents();
      }
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
      lock (_serverData.SyncObj)
      {
        foreach (DvStateVariable variable in variables)
          _eventingState.UpdateModerationData(variable);
        String body;
        lock (_serverData.SyncObj)
          body = GENAMessageBuilder.BuildEventNotificationMessage(variables, !_subscriberSupportsUPnP11);
        byte[] bodyData = Encoding.UTF8.GetBytes(body);
  
        AsyncRequestState state = new AsyncRequestState(_sid, _callbackURLs, _eventingState.EventKey, bodyData);
        _pendingRequests.Add(state);
        ContinueEventNotification(state);
        _eventingState.IncEventKey();
      }
    }
  }
}
