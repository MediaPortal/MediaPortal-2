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
using CommunityToolkit.Mvvm.ComponentModel;
using MediaPortal.Frontend.Notifications;

namespace MediaPortal.Frontend.ViewModels
{
  /// <summary>
  /// The dialog that reads the queue out loud: one thing at a time, with the picture that belongs to it, and the
  /// count of whatever is still waiting underneath.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This is the reference's <c>WatchSystemNotifications.xaml</c>
  /// (<c>Source/UI/Skins/Nereus/Skin/Nereus/screens/WatchSystemNotifications.xaml</c>) dialog for dialog. The
  /// reference showed the notification's picture, its title and its text, said how many more were waiting
  /// (<c>NMoreNotificationsText</c>), and gave its viewer one button whose label was "OK" unless the notification
  /// knew a screen that could deal with it - in which case a second button went there instead of merely
  /// acknowledging. It was reached from the notification bar's button, whose picture was the picture of the next
  /// notification and whose badge was the count of the rest (<c>SkinBase/Models/NotificationsModel.cs:114-135</c>).
  /// </para>
  /// <para>
  /// Both of its buttons end up taking the notification out of the queue - "OK" and Discard differed in what the
  /// household meant, not in what happened - so both are <see cref="ReadThat"/> here, and the second one is the same
  /// thing with a jump in front of it.
  /// </para>
  /// </remarks>
  public sealed partial class NotificationsViewModel : ObservableObject
  {
    private readonly NotificationQueue _queue;

    public NotificationsViewModel(NotificationQueue queue)
    {
      if (queue == null)
        throw new ArgumentNullException(nameof(queue));

      _queue = queue;
      ShowNext();
    }

    /// <summary>
    /// The one being shown now. Null once the queue has been read through, which is what closes the dialog - the
    /// reference's dialog closed itself the same way, on the notification that left nothing behind.
    /// </summary>
    [ObservableProperty]
    private UiNotification? _current;

    /// <summary>
    /// Said when the viewer presses the button that goes to the screen a notification is about, with the screen's
    /// name. The model has no idea what a screen is; the application does, and it is the one that can open one.
    /// </summary>
    public event Action<string>? HandleRequested;

    /// <summary>
    /// Said when the last notification has been read and the dialog has nothing left to show, so that whoever raised
    /// it can take it away rather than leave an empty box standing over the screen behind.
    /// </summary>
    public event Action? Finished;

    /// <summary>Whether there is anything to show at all.</summary>
    public bool HasNotification
    {
      get { return Current != null; }
    }

    /// <summary>The picture that belongs to the notification being read.</summary>
    public string Icon
    {
      get { return Current == null ? "" : Current.Icon; }
    }

    /// <summary>The first line, in the reference's words.</summary>
    public string Title
    {
      get { return Current == null ? "" : Current.Title; }
    }

    /// <summary>What happened, said properly.</summary>
    public string Text
    {
      get { return Current == null ? "" : Current.Text; }
    }

    /// <summary>
    /// Whether a button that goes and deals with it makes sense, which it only does when somebody knows which screen
    /// can. The reference's second button was there exactly as often.
    /// </summary>
    public bool CanHandle
    {
      get { return Current != null && Current.HasHandler; }
    }

    /// <summary>
    /// How many are waiting behind this one, said the way the reference said it: nothing when none, "one more" when
    /// one, and the number when more.
    /// </summary>
    public string MoreWords
    {
      get
      {
        // The one being read is still in the queue - the dialog reads it and only then takes it out - so "behind this
        // one" is the count without it.
        int more = _queue.Count;
        if (Current != null && more > 0)
          more--;

        if (more <= 0)
          return "";
        return more == 1 ? "one more waiting" : more + " more waiting";
      }
    }

    /// <summary>Whether there is anything to say about what is still waiting.</summary>
    public bool HasMore
    {
      get { return MoreWords.Length > 0; }
    }

    /// <summary>
    /// Reads this notification and puts the next one in its place, or says that there is nothing left. One call for
    /// both buttons, because both of them mean "this one has been read".
    /// </summary>
    public void ReadThat()
    {
      _queue.Dequeue();
      ShowNext();
    }

    /// <summary>
    /// Goes to the screen this notification is about, and takes the notification with it: a thing the viewer went and
    /// dealt with has been dealt with, and the household should not be told again.
    /// </summary>
    public void Handle()
    {
      UiNotification? current = Current;
      if (current == null || !current.HasHandler)
        return;

      ReadThat();
      HandleRequested?.Invoke(current.Screen!);
    }

    /// <summary>
    /// Empties the queue without reading the rest of it, for the viewer who has understood the arrangement and wants
    /// the box gone. The reference let its viewer do this by walking away; a dialog that cannot be dismissed would be
    /// a worse one than the reference's.
    /// </summary>
    public void ReadNoMore()
    {
      _queue.Clear();
      ShowNext();
    }

    /// <summary>Puts the next notification on the dialog, or says that the queue is empty.</summary>
    private void ShowNext()
    {
      UiNotification? was = Current;

      Current = _queue.Peek();
      OnPropertyChanged(nameof(HasNotification));
      OnPropertyChanged(nameof(Icon));
      OnPropertyChanged(nameof(Title));
      OnPropertyChanged(nameof(Text));
      OnPropertyChanged(nameof(CanHandle));
      OnPropertyChanged(nameof(MoreWords));
      OnPropertyChanged(nameof(HasMore));

      // Said on the way out and not afterwards on every press: whoever raised the dialog takes it away on this, and
      // hearing it twice would be a second pop - which is somebody else's screen walking out of the way.
      if (Current == null && was != null)
        Finished?.Invoke();
    }
  }
}
