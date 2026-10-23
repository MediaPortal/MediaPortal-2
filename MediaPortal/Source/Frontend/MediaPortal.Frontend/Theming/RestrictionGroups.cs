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
using System.Linq;

namespace MediaPortal.Frontend.Theming
{
  /// <summary>
  /// The parts of this front end a user can be kept out of, under the names the reference registered them.
  /// </summary>
  /// <remarks>
  /// <para>
  /// In the reference a restriction group was never a fixed list: every plugin registered the groups its
  /// screens and actions hid behind (<c>WorkflowResourcesLoader.cs:209</c> read the attribute off the
  /// workflow XML, <c>ConfigBuilder.cs:252</c> off the settings entries, <c>ChannelContext.cs:206</c> per TV
  /// channel group), and the user editor listed exactly what had been registered at that moment
  /// (<c>UserConfigModel.cs:765</c>). This front end has no workflow loader, so the registry is spelled out
  /// here - and it holds only the groups something on this machine actually obeys, because a group nothing
  /// hides is a promise to a parent that nothing keeps.
  /// </para>
  /// <para>
  /// A user who restricts groups sees only what their group list covers; nobody signed in, or a grown-up, or
  /// a user with restrictions switched off reaches everything - the reference's own
  /// <c>UserProfile.CheckUserAccess</c> rule (<c>UserProfile.cs:244-254</c>), repeated in
  /// <see cref="Allows"/>.
  /// </para>
  /// </remarks>
  public static class RestrictionGroups
  {
    /// <summary>
    /// The whole "Settings" screen. The reference hung its entire configuration tree on this one group
    /// (<c>configuration-actions.xml:8</c>), which is what makes a child unable to walk into the machine room
    /// - and why a household with restrictions switched on must keep one grown-up who carries it.
    /// </summary>
    public const string Settings = "Settings";

    /// <summary>The "Users" section on its own. The reference knew it as Settings.UserProfile.</summary>
    public const string SettingsUserProfile = "Settings.UserProfile";

    /// <summary>The press that throws a recording away. The reference's name: SlimTvClient.DeleteRecording.</summary>
    public const string DeleteRecording = "SlimTvClient.DeleteRecording";

    /// <summary>
    /// The name a TV channel group answers to, in the reference's own shape - one group per channel group,
    /// so a household can keep the child's television to the channels it picked (<c>ChannelGroup.cs:91</c>:
    /// "TV: Group: &lt;name&gt;").
    /// </summary>
    public static string TvChannelGroup(string groupName)
    {
      return "TV: Group: " + groupName;
    }

    /// <summary>
    /// Everything the editor can offer: the fixed parts of this front end, plus the TV channel groups of the
    /// list the television is currently looking at, which is as dynamic here as the reference's registry was.
    /// </summary>
    /// <summary>
    /// The parts of the machine room a person can be kept out of. A fifth group existed in the reference -
    /// deleting library titles - but this front end offers no such press, and the registry keeps only the
    /// groups something on this machine actually obeys: a checkbox that hides nothing is a promise to a
    /// parent that nothing keeps.
    /// </summary>
    public static IList<RestrictionGroupInfo> Catalog(IEnumerable<string>? tvChannelGroups)
    {
      List<RestrictionGroupInfo> groups = new List<RestrictionGroupInfo>
      {
        new RestrictionGroupInfo(Settings, "The whole settings screen"),
        new RestrictionGroupInfo(SettingsUserProfile, "The \"Users\" section of settings"),
        new RestrictionGroupInfo(DeleteRecording, "Throwing a recording away")
      };
      if (tvChannelGroups != null)
        foreach (string name in tvChannelGroups.Where(n => !string.IsNullOrWhiteSpace(n)))
          groups.Add(new RestrictionGroupInfo(TvChannelGroup(name.Trim()),
              "The TV channel group \"" + name.Trim() + "\""));
      return groups;
    }

    /// <summary>
    /// The reference's rule for one element standing in front of one user
    /// (<c>UserProfile.cs:244-254</c>): an element that names no group is everybody's, a user whose
    /// restrictions are switched off reaches everything, and otherwise one of the element's groups has to
    /// stand in the user's list. Groups are compared without minding their case.
    /// </summary>
    /// <param name="enableRestrictions">Whether the user's restrictions are switched on at all.</param>
    /// <param name="userGroups">The groups the user was given, comma separated.</param>
    /// <param name="elementGroup">The group - or comma separated groups - the element hides behind.</param>
    public static bool Allows(bool enableRestrictions, string? userGroups, string? elementGroup)
    {
      if (!enableRestrictions || string.IsNullOrEmpty(elementGroup))
        return true;

      HashSet<string> owned = Split(userGroups);
      foreach (string wanted in Split(elementGroup))
        if (owned.Contains(wanted))
          return true;
      return false;
    }

    private static HashSet<string> Split(string? commaSeparated)
    {
      return new HashSet<string>(
          (commaSeparated ?? "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
              .Select(g => g.Trim()).Where(g => g.Length > 0),
          StringComparer.InvariantCultureIgnoreCase);
    }
  }

  /// <summary>One entry of the editor's list: what the group is called and what it keeps out, in words.</summary>
  public sealed class RestrictionGroupInfo
  {
    public RestrictionGroupInfo(string name, string says)
    {
      Name = name;
      Says = says;
    }

    public string Name { get; }

    public string Says { get; }
  }
}
