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

namespace MediaPortal.Frontend.Notifications
{
  /// <summary>
  /// What kind of thing a notification is, which decides the glyph it stands on and nothing else.
  /// </summary>
  /// <remarks>
  /// The reference's four, kept as four: its <c>NotificationType</c>
  /// (<c>Source/Core/MediaPortal.UI/Presentation/UiNotifications/INotificationService.cs:28-36</c>) said that the
  /// type "only determines how the notification is shown to the user", and it is used for exactly that - the glyph
  /// the notification carries and, through the order below, which of several waiting notifications the hint in the
  /// head line is allowed to stand for.
  /// </remarks>
  public enum NotificationType
  {
    /// <summary>Something happened that is worth reading but asks for nothing.</summary>
    Info,

    /// <summary>Something went wrong that the household may want to look at.</summary>
    Warning,

    /// <summary>Something failed.</summary>
    Error,

    /// <summary>Something will stay wrong until somebody does something about it.</summary>
    UserInteractionRequired
  }

  /// <summary>
  /// One thing the machines have to say to whoever is standing in front of the television.
  /// </summary>
  /// <remarks>
  /// The reference's <c>INotification</c> (same file, <c>:41-67</c>) carried a type, a title, a text, a timeout and
  /// the workflow state a "handle this" button would navigate to, and that is what is here too. Two differences are
  /// worth naming rather than hiding: the workflow state is the <em>name of a screen</em>, because this front end
  /// navigates by screen key and not by a registered workflow id, and the glyph is asked for by name when the skin is
  /// asked for it, so a notification never carries a picture of its own.
  /// </remarks>
  public sealed class UiNotification
  {
    public UiNotification(NotificationType type, string title, string text, string? screen = null)
    {
      Type = type;
      Title = title ?? "";
      Text = text ?? "";
      Screen = string.IsNullOrWhiteSpace(screen) ? null : screen;
    }

    /// <summary>Which of the four this is, and therefore which glyph it wears.</summary>
    public NotificationType Type { get; }

    /// <summary>The short line, the one the reference put next to the icon.</summary>
    public string Title { get; }

    /// <summary>The detailed line under it, which is where the what-to-do-about-it belongs.</summary>
    public string Text { get; }

    /// <summary>
    /// The name of the screen that can deal with this, or null when there is nothing to deal with. The reference
    /// called this a handler workflow state and drew a second button for it - "Handle" beside "Discard",
    /// <c>Skin/Nereus/screens/WatchSystemNotifications.xaml:36-48</c>; a notification without one gets a single OK,
    /// exactly as the reference's did.
    /// </summary>
    public string? Screen { get; }

    /// <summary>
    /// The moment this stops being worth saying, or null to say it until somebody reads it. Whoever enqueues a
    /// notification whose reason for existing has an expiry date sets this, and the queue forgets such a notification
    /// by itself once the moment has passed.
    /// </summary>
    public System.DateTime? Timeout { get; set; }

    /// <summary>Whether the notification has a screen that can be walked to from it.</summary>
    public bool HasHandler
    {
      get { return !string.IsNullOrEmpty(Screen); }
    }

    /// <summary>Whether this one is still worth saying at this moment.</summary>
    public bool IsLive(System.DateTime now)
    {
      return !Timeout.HasValue || Timeout.Value > now;
    }

    /// <summary>
    /// The worn skin's glyph for this type.
    /// </summary>
    /// <remarks>
    /// The reference mapped its four onto <c>info-icon.png</c>, <c>warning-icon.png</c>, <c>error-icon.png</c> and
    /// <c>user-interaction-required-icon.png</c> (<c>SkinBase/Models/NotificationsModel.cs:116-135</c>). Three of
    /// those four are answered here by a shape of the same name; the fourth exists in the copied skin only as a
    /// bitmap, and a bitmap cannot be painted by a <c>Path</c>, which is the rule this skin's glyphs live by, so
    /// "somebody has to act" is said with the information glyph and the words carry the rest. That is the one place
    /// where the reference's icon set is not answered one for one.
    /// </remarks>
    public string Icon
    {
      get { return IconFor(Type); }
    }

    /// <summary>
    /// The glyph for a type on its own, for whoever only knows the type - the head line's hint, which stands for the
    /// loudest of several rather than for one notification.
    /// </summary>
    public static string IconFor(NotificationType type)
    {
      switch (type)
      {
        case NotificationType.Error:
          return "Notification/error";
        case NotificationType.Warning:
          return "Notification/warning";
        default:
          return "Notification/info";
      }
    }

    /// <summary>
    /// Which of two notifications the head line's hint may stand for: the one that would be rudest to ignore. The
    /// order puts an error above a thing that needs doing, that above a warning and that above plain information.
    /// </summary>
    public static NotificationType WorstOf(NotificationType a, NotificationType b)
    {
      return Rank(a) >= Rank(b) ? a : b;
    }

    private static int Rank(NotificationType type)
    {
      switch (type)
      {
        case NotificationType.Info:
          return 0;
        case NotificationType.Warning:
          return 1;
        case NotificationType.UserInteractionRequired:
          return 2;
        default:
          return 3;
      }
    }
  }
}
