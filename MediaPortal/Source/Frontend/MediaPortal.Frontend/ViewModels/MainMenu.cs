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
*/

using System;
using System.Collections.Generic;

namespace MediaPortal.Frontend.ViewModels
{
  /// <summary>
  /// The home screen's main menu: the parts a MediaPortal media centre is made of, whether or not this particular
  /// build and this particular server can fill them.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The list is MP2's own. The old client's home screen was assembled from the actions every plugin contributed
  /// with the home state as their source, and those contributions are still in this tree: the media plugin offers
  /// <c>Home->Movies</c>, <c>Home->Series</c>, <c>Home->Audio</c>, <c>Home->Images</c>, <c>Home->Videos</c> and
  /// <c>Home->Latest Media</c> (<c>Source/UI/UiComponents/Media/Skin/default/workflow/media-actions.xml:5-56</c>),
  /// the TV client offers <c>Home->SlimTvClient</c> and <c>Home->SlimTvClientRadio</c>
  /// (<c>Source/UI/TV/SlimTvClient/Skin/default/workflow/SlimTvClient-actions.xml:5</c> and <c>:62</c>) and the
  /// configuration plugin offers <c>Home->ConfigurationMain</c>
  /// (<c>Source/UI/UiComponents/Configuration/Skin/default/workflow/configuration-actions.xml:6</c>). The order here
  /// is the order MP2 sorts its media entries in - <c>a-Media</c> and the letters after it - with the entries no
  /// plugin in this build contributes at the end, saying so.
  /// </para>
  /// <para>
  /// The <c>Weather</c>, <c>News</c>, <c>Utilities</c>, <c>PartyMusicConfig</c> and home-editor entries are not
  /// repeated here, because each of those is a plugin this front end has no screen for at all: naming them one by
  /// one would be a list of other people's missing work rather than a menu.
  /// </para>
  /// </remarks>
  public static class MainMenu
  {
    /// <summary>
    /// The menu, in the order it is drawn.
    /// </summary>
    /// <param name="aspects">What the server said it holds, or nothing when no server has answered.</param>
    /// <param name="connected">Whether a server has answered at all.</param>
    public static List<MainMenuEntry> Build(IEnumerable<AspectViewModel> aspects, bool connected)
    {
      List<AspectViewModel> held = new List<AspectViewModel>(aspects);
      List<MainMenuEntry> menu = new List<MainMenuEntry>();
      HashSet<string> claimed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

      foreach ((string label, string[] aspects_, string action, string? missing, string? screen) in ENTRIES)
      {
        MainMenuEntry entry = new MainMenuEntry(label, aspects_, action, missing, screen);
        foreach (string aspectName in aspects_)
          claimed.Add(aspectName);

        // A screen is openable whatever the server holds - a screen with nothing on it is still the answer to
        // "where do I tell it where my films are?", which is asked most often of an empty media centre.
        if (missing == null && screen == null)
        {
          // The preference is the outer loop on purpose: a server with both albums and tracks must lead with the
          // albums, and searching the server's list first would hand back whichever of the two it happens to
          // report first.
          foreach (string wanted in aspects_)
          {
            AspectViewModel? match = held.Find(heldAspect =>
                string.Equals(heldAspect.Name, wanted, StringComparison.OrdinalIgnoreCase));
            if (match != null)
            {
              entry.Section = new HomeSection(label, match);
              break;
            }
          }

          if (entry.Section == null)
            entry.Unavailable = connected ? "not on this server" : "no server connected";
        }

        menu.Add(entry);
      }

      // Anything the server holds that no entry claimed stays on the screen, after the menu. A front end that
      // silently dropped a part of somebody's library would be doing the same thing MP2's tiles were criticised
      // for, and a server with a custom aspect in it is a server whose owner expects to see it.
      foreach (HomeSection section in HomeSection.For(held))
      {
        if (!claimed.Contains(section.Aspect.Name))
          menu.Add(MainMenuEntry.FromSection(section));
      }

      return menu;
    }

    /// <summary>
    /// The entries, in MP2's order: the six the media plugin contributes, then the ones this build cannot back yet.
    /// </summary>
    /// <remarks>
    /// The fourth field is what the tile says when there is nothing behind the entry, so it has to be a line the
    /// viewer can read from three metres away - the whole reason, in each case, is written out here where a reader
    /// can have it: <c>Latest media</c> because the API can only sort by title, year, last-played, track and
    /// episode, and the import date a "newest first" list would need is not one of them; <c>TV</c> and
    /// <c>Radio</c> because live television and radio came from the TV service, which was removed from this tree
    /// (commit 029a262aa4) and nothing serves it since; <c>Settings</c> because the server keeps its configuration
    /// behind a .NET Remoting interface the new API has not grown an equivalent of yet.
    /// The fifth field names a screen of the front end rather than a part of the library. An entry with one is
    /// openable whatever the server holds, because there is nothing on the server for it to depend on; the key is
    /// handed to the function the application was given, which makes the screen or says it cannot.
    /// </remarks>
    private static readonly (string Label, string[] Aspects, string Action, string? Missing, string? Screen)[] ENTRIES =
    {
        ("Movies", new[] { "MovieItem" }, "the films on this server", null, null),
        ("TV Series", new[] { "SeriesItem" }, "each series, and its episodes inside it", null, null),
        ("Music", new[] { "AlbumItem", "AudioItem" }, "each album, and its tracks on it", null, null),
        // Not "the files nobody filed as films": the server's VideoItem is the aspect every film and every episode
        // also carries, so this shelf holds the whole video library (measured against a real one: four films, eight
        // episodes and the two unfiled clips). Saying so is worth more than a tidier sentence.
        ("Videos", new[] { "VideoItem" }, "every video the server knows, films and episodes included", null, null),
        // The folders, not the filed titles. A photograph rarely has anything for an importer's decision to attach
        // to - no title, no year, no episode number - so a pictures screen backed by ImageItem titles shows an
        // empty shelf over thousands of photographs, which is why MP2's own pictures screen walks the folders. If a
        // server does hold filed images, they are still on this screen: the leftover rule at the bottom of Build
        // gives them their own tile rather than dropping a part of somebody's library.
        ("Pictures", Array.Empty<string>(), "the pictures, folder by folder", null, "pictures"),
        ("Latest media", Array.Empty<string>(), "the newest titles first", "no import-date sort", null),
        ("TV", Array.Empty<string>(), "live television and the programme guide", "no TV backend", null),
        ("Radio", Array.Empty<string>(), "radio stations and their guide", "no radio backend", null),
        // The one part of MP2's configuration that has an HTTP API, and therefore the one part that can be edited
        // from a television. Where the media lives is also the first thing an empty media centre needs asked.
        ("Folders", Array.Empty<string>(), "where the library comes from", null, "folders"),
        ("Settings", Array.Empty<string>(), "the server's own configuration", "no settings API yet", null)
    };
  }
}