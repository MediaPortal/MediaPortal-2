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
  /// This is MP2's menu, entry for entry and in MP2's order. The old client did not have a main menu written down
  /// anywhere: <c>SkinBase</c>'s <c>MenuModel</c> (<c>Source/UI/UiComponents/SkinBase/Models/MenuModel.cs:41</c>)
  /// collected every workflow action whose source state was the home state - <c>7F702D9C-F2DD-42da-9ED8-0BA92F07787F</c>
  /// - sorted them by <c>DisplayCategory</c> and then <c>SortOrder</c> (<c>MenuModel.cs:121-153</c>) and handed the
  /// result to the skin. Every one of those contributions is still in this tree, so this table was written from them
  /// rather than invented: <c>Home-&gt;BrowseLocalMedia</c>, <c>Home-&gt;BrowseMedia</c>, <c>Home-&gt;Audio</c>,
  /// <c>Home-&gt;Videos</c>, <c>Home-&gt;Movies</c>, <c>Home-&gt;Series</c>, <c>Home-&gt;Images</c> and
  /// <c>Home-&gt;Latest Media</c> from <c>Media/Skin/default/workflow/media-actions.xml:5-56</c>;
  /// <c>Home-&gt;News</c>, <c>Home-&gt;Weather</c>, <c>Home-&gt;UtilitiesMain</c>, <c>Home-&gt;PartyMusicConfig</c>
  /// and <c>Home-&gt;ConfigurationMain</c> from the same file of their own plugin; and <c>Home-&gt;SlimTvClient</c>
  /// and <c>Home-&gt;SlimTvClientRadio</c> from <c>TV/SlimTvClient/Skin/default/workflow/SlimTvClient-actions.xml:5</c>
  /// and <c>:62</c>. The labels are those plugins' own English strings, read from each plugin's
  /// <c>Language/strings_en.xml</c> by the key named in <see cref="MainMenuEntry.LabelKey"/>.
  /// <c>Tests/Tests.Frontend/TestHomeMenuContributions.cs</c> repeats that reading on every test run, so a plugin
  /// added to or renamed in this tree shows up as a failing test instead of as a menu that quietly stopped matching.
  /// </para>
  /// <para>
  /// Two things are left out on purpose. <c>wmc-actions.xml</c> contributes six more home entries
  /// (<c>SlimTvClient-&gt;Guide</c>, <c>-&gt;Schedules</c>, <c>-&gt;Recordings</c>, <c>-&gt;ProgramSearch</c> and the
  /// two radio equivalents): those are the television plugin's insides, which the rescued WMC skin promoted onto its
  /// home screen, and they need the TV service this tree no longer has. And MP2 shows an entry only when the plugin
  /// that contributes it is installed; this front end has no plugin installation to ask, so an entry that nothing
  /// backs stays on screen and says so in its small print - a media centre that silently offers six tiles in
  /// whatever state the last read happened to leave them in cannot tell "no television here" apart from "the read
  /// went wrong".
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

      foreach ((string action_, string labelKey, string label, string[] aspects_, string detail, string? missing,
          string? screen, (string Sort, bool Descending)? sort) in ENTRIES)
      {
        MainMenuEntry entry = new MainMenuEntry(action_, labelKey, label, aspects_, detail, missing, screen,
            sort?.Sort, sort?.Descending ?? false);
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
              entry.Section = new HomeSection(label, match, entry.Sort, entry.SortDescending);
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
    /// The entries, in the order MP2 sorts its home contributions into - <c>DisplayCategory</c> first and
    /// <c>SortOrder</c> within it - and nothing else: every line of this table is a contribution the previous code
    /// really makes. The two fields before the label are the evidence for that claim, and
    /// <c>TestHomeMenuContributions</c> is what stops the claim from going stale.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The labels are MP2's words, not the ones this front end would have chosen, because a viewer who has used
    /// MediaPortal before should not have to learn that the tile they always pressed is now called something else:
    /// the media plugin calls music <c>Audio</c> and pictures <c>Images</c>
    /// (<c>Media/Language/strings_en.xml</c>), and the configuration plugin calls its screen <c>Settings</c>. Where
    /// MP2's word leaves room for doubt, the tile's second line says what pressing brings up.
    /// </para>
    /// <para>
    /// <c>Home-&gt;Movies</c> and <c>Home-&gt;Series</c> share sort order <c>e</c> in the media plugin's XML, so
    /// MP2's own order between those two is whatever its unstable list sort produced. Films come first here because
    /// more people press them. The sixth field is what the tile says when there is nothing behind the entry, so it
    /// has to be a line the viewer can read from three metres away - the whole reason, in each case, is written out
    /// here where a reader can have it:
    /// <c>TV</c> and <c>Radio</c> because live television and radio came from the TV service, which was removed from
    /// this tree (commit 029a262aa4) and nothing serves it since; <c>Settings</c> because the server keeps its
    /// configuration behind a .NET Remoting interface the new API has not grown an equivalent of yet; <c>News</c>,
    /// <c>Weather</c> and <c>Party music player</c> used to be on that list too, until the front end grew their
    /// screens - the first two reading the same feeds and forecast service the old plugins read, the last one
    /// shuffling the server's own music shelf and playing it through the application's player; and
    /// <c>Utilities</c> because what that plugin launched was the TV client's setup, the radio's setup and the
    /// playlist importer, every one of which served the television this tree no longer has.
    /// </para>
    /// <para>
    /// The seventh field names a screen of the front end rather than a part of the library. An entry with one is
    /// openable whatever the server holds, because there is nothing on the server for it to depend on; the key is
    /// handed to the function the application was given, which makes the screen or says it cannot.
    /// </para>
    /// <para>
    /// The eighth field is the order the opened shelf is read in: the API's own word and whether it runs
    /// newest-first, or null for the name order every shelf keeps by default. Only <c>Latest media</c> sets it.
    /// The server can sort titles by the date the library first saw the file - the media library has stamped that
    /// date on every title itself since before this port began - and it is the one fact that turns a shelf of
    /// videos into "the newest titles first" instead of into "Videos" wearing a different label. The direction
    /// travels with it because "latest" read oldest-first is the opposite list.
    /// </para>
    /// </remarks>
    private static readonly (string Mp2Action, string LabelKey, string Label, string[] Aspects, string Action,
        string? Missing, string? Screen, (string Sort, bool Descending)? Sort)[] ENTRIES =
    {
        // MP2's "local media" is the file view - the folders the library is built from, before the importer's
        // decisions have been applied to them. This front end has exactly that: the shares screen, which lists the
        // server's folders and what it made of each. "Folder by folder" came off this line to fit the tile.
        ("Home->BrowseLocalMedia", "[Media.LocalMediaMenuItem]", "Local media", Array.Empty<string>(),
            "the server's folders", null, "folders", null),
        ("Home->BrowseMedia", "[Media.BrowseMediaMenuItem]", "Browse media", Array.Empty<string>(),
            "grouped by genre, year, actor", null, "browse", null),
        // "Audio" is the plugin's own word for the music library, and it covers both shapes the importer can leave
        // the collection in; the second line is here so that the narrower word does not read as a mistake.
        ("Home->Audio", "[Media.AudioMenuItem]", "Audio", new[] { "AlbumItem", "AudioItem" },
            "each album, and its tracks on it", null, null, null),
        // Not "the files nobody filed as films": the server's VideoItem is the aspect every film and every episode
        // also carries, so this shelf holds the whole video library (measured against a real one: four films, eight
        // episodes and the two unfiled clips). Saying so is worth more than a tidier sentence.
        ("Home->Videos", "[Media.VideosMenuItem]", "Videos", new[] { "VideoItem" },
            "every video the server knows, films and episodes included", null, null, null),
        ("Home->Movies", "[Media.MoviesMenuItem]", "Movies", new[] { "MovieItem" },
            "the films on this server", null, null, null),
        ("Home->Series", "[Media.SeriesMenuItem]", "Series", new[] { "SeriesItem" },
            "each series, and its episodes inside it", null, null, null),
        // The folders, not the filed titles. A photograph rarely has anything for an importer's decision to attach
        // to - no title, no year, no episode number - so a pictures screen backed by ImageItem titles shows an
        // empty shelf over thousands of photographs, which is why MP2's own pictures screen walks the folders. If a
        // server does hold filed images, they are still on this screen: the leftover rule at the bottom of Build
        // gives them their own tile rather than dropping a part of somebody's library.
        ("Home->Images", "[Media.ImagesMenuItem]", "Images", Array.Empty<string>(),
            "the pictures, folder by folder", null, "pictures", null),
        // MP2's own "latest media" screen listed everything anybody sits down to watch - films, episodes and the
        // unfiled clips alike - in the order the library had learnt of them, which is exactly VideoItem read in the
        // date-the-library-first-saw-it order. The sort is the whole of the entry: without the eighth field this
        // tile would open the shelf "Videos" already names, alphabetically, and two labels would be one screen.
        ("Home->Latest Media", "[Media.LatestMediaMenuItem]", "Latest media", new[] { "VideoItem" },
            "the newest titles first", null, null, ("added", true)),
        ("Home->News", "[News.Title]", "News", Array.Empty<string>(),
            "the headlines", null, "news", null),
        ("Home->SlimTvClient", "[SlimTvClient.Tv]", "TV", Array.Empty<string>(),
            "live television and the programme guide", "no TV backend", null, null),
        ("Home->SlimTvClientRadio", "[SlimTvClient.Radio]", "Radio", Array.Empty<string>(),
            "radio stations and their guide", "no radio backend", null, null),
        ("Home->Weather", "[Weather.Title]", "Weather", Array.Empty<string>(),
            "the forecast", null, "weather", null),
        ("Home->PartyMusicConfig", "[PartyMusicPlayer.MenuItem]", "Party music player", Array.Empty<string>(),
            "the music shelf, shuffled", null, "party", null),
        // The one part of MP2's configuration that has an HTTP API, and therefore the one part that could be edited
        // from a television. Where the library comes from is already on this menu, as "Local media".
        ("Home->ConfigurationMain", "[Configuration.Name]", "Settings", Array.Empty<string>(),
            "the server's own configuration", "no settings API yet", null, null),
        ("Home->UtilitiesMain", "[Utilities.MenuItem]", "Utilities", Array.Empty<string>(),
            "the little tools", "its tools went with the TV", null, null)
    };
  }
}
