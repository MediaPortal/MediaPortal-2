/*
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

using System;
using System.Collections.Generic;

namespace MediaPortal.Frontend.Notifications
{
  /// <summary>
  /// The things the household has been told and has not read yet: a queue nobody has to stop watching television
  /// for, with a hint in the head line that says how many are waiting.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This is the reference's <c>INotificationService</c>
  /// (<c>Source/Core/MediaPortal.UI/Presentation/UiNotifications/INotificationService.cs:69-160</c>) and its
  /// <c>NotificationService</c> in the shape this front end can use without a service registry. What it kept: the
  /// four types, the title-and-text shape, the <c>urgent</c> flag that puts a notification in front of the ones
  /// already waiting, reading the next one without removing it (<c>Peek</c>) and removing it (<c>Dequeue</c>) - which
  /// is what makes the dialog's OK button and its Discard button the same call - and the timeouts that make a
  /// notification go away when the reason for saying it has.
  /// </para>
  /// <para>
  /// What it left out is the reference's list order, which counted from the back because the skin's list did; here
  /// the next notification to be read is the first in <see cref="Waiting"/>, and "urgent" puts one in front of that
  /// same end. Two names for one arrangement would only have been confusing.
  /// </para>
  /// <para>
  /// The events fire on whichever thread handed the notification in, which is nowhere near the thread a window lives
  /// on; whoever listens for them is the one that knows which thread the screen wants, so <c>App</c> crosses over for
  /// the head line and nobody else has to think about it.
  /// </para>
  /// </remarks>
  public sealed class NotificationQueue
  {
    private readonly List<UiNotification> _waiting = new List<UiNotification>();
    private readonly object _gate = new object();

    /// <summary>
    /// Said after the queue is different: a notification went in, came out, or went stale. The head line's hint
    /// listens to this one, because its only job is to be there while the queue is not empty.
    /// </summary>
    public event Action? Changed;

    /// <summary>
    /// Said after a notification went in, with the notification. Distinct from <see cref="Changed"/> because "the
    /// queue has something" is not the same news as "this arrived", and a household that wants to be interrupted by
    /// an urgent one should be able to listen to one without hearing the other.
    /// </summary>
    public event Action<UiNotification>? Arrived;

    /// <summary>
    /// Tells the household something, at the back of what is already waiting - or at the front, when it is urgent.
    /// Returns the notification, so the caller can give it an expiry date afterwards.
    /// </summary>
    public UiNotification Enqueue(NotificationType type, string title, string text, bool urgent = false)
    {
      return Enqueue(new UiNotification(type, title, text), urgent);
    }

    /// <summary>The same, for a notification that already knows which screen can deal with it.</summary>
    public UiNotification Enqueue(NotificationType type, string title, string text, string screen, bool urgent = false)
    {
      return Enqueue(new UiNotification(type, title, text, screen), urgent);
    }

    /// <summary>
    /// Puts a prepared notification into the queue. Asking with one that is already in is answered by moving it to
    /// the end it is being asked for rather than by saying it twice, because two callers that both noticed the same
    /// broken thing should not make the household read it twice.
    /// </summary>
    public UiNotification Enqueue(UiNotification notification, bool urgent = false)
    {
      if (notification == null)
        throw new ArgumentNullException(nameof(notification));

      lock (_gate)
      {
        int known = _waiting.IndexOf(notification);
        if (known >= 0)
          _waiting.RemoveAt(known);

        if (urgent)
          _waiting.Insert(0, notification);
        else
          _waiting.Add(notification);
      }

      Changed?.Invoke();
      Arrived?.Invoke(notification);
      return notification;
    }

    /// <summary>
    /// Takes a notification out because it stopped being true - the share came back, the server answered again. False
    /// when it was not in the queue any more, which is the usual answer and not a failure.
    /// </summary>
    public bool Remove(UiNotification notification)
    {
      bool removed;
      lock (_gate)
      {
        removed = _waiting.Remove(notification);
      }

      if (removed)
        Changed?.Invoke();
      return removed;
    }

    /// <summary>The next notification to be read, without taking it. Null when there is nothing to read.</summary>
    public UiNotification? Peek()
    {
      lock (_gate)
      {
        DropStale();
        return _waiting.Count == 0 ? null : _waiting[0];
      }
    }

    /// <summary>
    /// Reads the next notification and takes it. The reference's dialog called exactly this both for its OK and for
    /// its Discard (<c>Skin/Nereus/screens/WatchSystemNotifications.xaml:31-45</c>), because the two differ in what
    /// the household meant, not in what happens to the queue.
    /// </summary>
    public UiNotification? Dequeue()
    {
      UiNotification? taken;
      bool changed;
      lock (_gate)
      {
        DropStale();
        if (_waiting.Count == 0)
        {
          taken = null;
          changed = false;
        }
        else
        {
          taken = _waiting[0];
          _waiting.RemoveAt(0);
          changed = true;
        }
      }

      if (changed)
        Changed?.Invoke();
      return taken;
    }

    /// <summary>How many are waiting, the stale ones already forgotten.</summary>
    public int Count
    {
      get
      {
        lock (_gate)
        {
          DropStale();
          return _waiting.Count;
        }
      }
    }

    /// <summary>Everything waiting, the next to be read first.</summary>
    public IReadOnlyList<UiNotification> Waiting()
    {
      lock (_gate)
      {
        DropStale();
        return new List<UiNotification>(_waiting);
      }
    }

    /// <summary>
    /// The type the head line's hint may stand for while several are waiting, or null when none is. The reference
    /// drew one icon - the one belonging to the notification the dialog would show next
    /// (<c>SkinBase/Models/NotificationsModel.cs:114-135</c>) - and said how many more there were in words; this
    /// keeps the counting and picks the loudest glyph, because a hint that quietly shows an information "i" while
    /// an error waits underneath it is a hint that lies.
    /// </summary>
    public NotificationType? LoudestType
    {
      get
      {
        lock (_gate)
        {
          DropStale();
          NotificationType? worst = null;
          foreach (UiNotification waiting in _waiting)
            worst = worst.HasValue ? UiNotification.WorstOf(worst.Value, waiting.Type) : waiting.Type;
          return worst;
        }
      }
    }

    /// <summary>
    /// Forgets the notifications whose expiry date has passed and answers how many went. The reference kept a timer
    /// for this (<c>INotificationService.CheckForTimeouts</c>); here it is also done by every read, because a hint
    /// that still counts a message nobody will ever need again is the same lie as the wrong glyph.
    /// </summary>
    public int CheckTimeouts()
    {
      int dropped;
      lock (_gate)
      {
        dropped = DropStale();
      }

      if (dropped > 0)
        Changed?.Invoke();
      return dropped;
    }

    /// <summary>Empties the queue, which is what "read them all" means and what a test wants before it starts.</summary>
    public void Clear()
    {
      int dropped;
      lock (_gate)
      {
        dropped = _waiting.Count;
        _waiting.Clear();
      }

      if (dropped > 0)
        Changed?.Invoke();
    }

    /// <summary>Throws out the stale ones. The caller holds the gate.</summary>
    private int DropStale()
    {
      DateTime now = DateTime.Now;
      int dropped = 0;
      for (int i = _waiting.Count - 1; i >= 0; i--)
      {
        if (_waiting[i].IsLive(now))
          continue;

        _waiting.RemoveAt(i);
        dropped++;
      }
      return dropped;
    }
  }
}
