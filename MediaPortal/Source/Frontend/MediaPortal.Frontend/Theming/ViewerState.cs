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
using MediaPortal.Frontend.Api;

namespace MediaPortal.Frontend.Theming
{
  /// <summary>
  /// Who is watching, and what the house allows them - the front end's one answer to both questions, kept
  /// where the chrome, the wake screen, the settings and the idle timer can all read it without asking the
  /// server again.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The reference kept this in a singleton service, <c>IUserManagement</c>: a <c>CurrentUser</c> you could
  /// ask, an event (<c>UserMessaging.UserChanged</c>) when it moved, and the restriction questions answered
  /// against it. This is the same idea in the front end's own shape - an observable, so the header's user
  /// chip changes the moment a person does, and a plain setter, because only the wake screen's state machine
  /// and the settings move it.
  /// </para>
  /// <para>
  /// The <see cref="Policy"/> travels with it because three screens need it and none of them should own it:
  /// the wake decides whether it asks anything, the header decides whether it shows a person at all, and the
  /// idle timer decides when to give the television back. A server that cannot be read leaves the last-known
  /// policy standing - the same bargain the wake already strikes with an unreachable server, said out loud
  /// when it walks somebody in.
  /// </para>
  /// </remarks>
  public partial class ViewerState : ObservableObject
  {
    /// <summary>The person signed in, or null while the television belongs to nobody. The chip reads this.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasViewer))]
    [NotifyPropertyChangedFor(nameof(ViewerName))]
    private UserInfo? _viewer;

    /// <summary>The house rules, as the server last told them. The defaults are a house that asks.</summary>
    [ObservableProperty]
    private LoginPolicyAnswer _policy = new LoginPolicyAnswer();

    /// <summary>
    /// Whom the last PIN was checked for - the person the typed digits belonged to while the wake is still
    /// deciding, before they become <see cref="Viewer"/>. Nobody else writes it; it exists so the wake's
    /// continuation cannot answer for a person the room switched away from mid-flight.
    /// </summary>
    public Guid? PendingProfileId { get; set; }

    /// <summary>Raised after <see cref="Viewer"/> moved, with the person arrived - or null for a sign-out.</summary>
    public event Action<UserInfo?>? ViewerChanged;

    /// <summary>Whether a person stands behind the television right now.</summary>
    public bool HasViewer => Viewer != null;

    /// <summary>The name the header chip shows; empty while nobody is signed in.</summary>
    public string ViewerName => Viewer?.Name ?? "";

    /// <summary>The age this viewer may watch up to, or null when no ceiling applies to them.</summary>
    public int? AllowedAge
    {
      get
      {
        UserInfo? who = Viewer;
        return who != null && who.RestrictAges && who.AllowedAge.HasValue ? who.AllowedAge : null;
      }
    }

    /// <summary>
    /// Move to another person, or to nobody. Setting the same person again is silent; a real move is said to
    /// whoever listens - the skin settings' file owner follows the person, the chrome says their name, and
    /// the screens that cached lists for the last viewer know to read them again.
    /// </summary>
    public void SetViewer(UserInfo? person)
    {
      if (Viewer?.ProfileId == person?.ProfileId && Viewer?.Name == person?.Name)
        return;
      Viewer = person;
      ViewerChanged?.Invoke(person);
    }

    /// <summary>
    /// Whether the signed-in viewer may reach the element standing behind <paramref name="elementGroup"/> -
    /// the reference's <c>CheckUserAccess</c> with the current user filled in. Nobody signed in reaches
    /// everything: the restrictions are a rule about a person, and without the person there is nothing to
    /// be strict about - which is also why the house wants its PIN door closed around such a television.
    /// </summary>
    public bool Allows(string elementGroup)
    {
      UserInfo? who = Viewer;
      return who == null
          || RestrictionGroups.Allows(who.EnableRestrictionGroups, who.RestrictionGroups, elementGroup);
    }
  }
}
