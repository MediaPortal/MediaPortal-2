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
  /// <c>Home-&gt;Videos</c>, <c>Home-&gt;Movies</c>, <c>Home-&gt;Series</c> and <c>Home-&gt;Images</c> from
  /// <c>Media/Skin/default/workflow/media-actions.xml:5-49</c>;
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
  /// home screen, and this front end answers the two the viewer presses - the television's guide and the radio's -
  /// while the recording lists behind the other four are still on the way. And MP2 shows an entry only when the
  /// plugin that contributes it is installed; this front end has no plugin installation to ask, so an entry that
  /// nothing backs stays on screen and says so in its small print - a media centre that silently offers six tiles in
  /// whatever state the last read happened to leave them in cannot tell "no television here" apart from "the read
  /// went wrong". And <c>Home-&gt;Latest Media</c> is not missing from this menu but from the tree: the media
  /// plugin's own contribution was deleted - its workflow action, its contributor model, its screen model, its
  /// three screens and its language string - because what it opened was the video shelf MP2 already names
  /// <c>Home-&gt;Videos</c>, and only an arrival order told the two tiles apart. The order went with it, so a shelf
  /// opens by name everywhere; the media plugin's <c>Latest*</c> list providers stayed, since those feed the home
  /// wall's "Latest added" bands and have nothing to do with the deleted tile.
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
          string? screen, string home) in ENTRIES)
      {
        MainMenuEntry entry = new MainMenuEntry(action_, labelKey, label, aspects_, detail, missing, screen, home);
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
    /// here where a reader can have it: <c>Settings</c> used to be on that list too, until the front end grew a
    /// settings screen of its own - what the tile opens is the front end's look, since the server's own
    /// configuration is still behind a .NET Remoting interface the new API has not grown an equivalent
    /// of yet; <c>News</c>,
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
    /// The eighth field is the entry's home picture as the two rescued skins name it: the workflow action's own Id,
    /// lower-cased and without braces, because <c>images/HomeMenu</c> in both skins files its tiles under exactly
    /// that name - bare in Nereus, with the skin adding its own <c>Focus</c> or <c>NoFocus</c> in WMC. Every one of
    /// the fourteen was taken from the <c>Id</c> attribute of the same workflow element the first field names
    /// (Nereus' own <c>HomeMenuModel</c> keys its home content by these very Ids, which is the second witness);
    /// <c>TestHomeMenuContributions</c> re-reads those files and demands the field is still the element's Id, and
    /// <c>TestHomeMenuImages</c> demands the shipped skins still carry a picture of that name. WMC is the one
    /// skin's one exception - the reference WMC home screen never had a local-media tile, so that entry keeps its
    /// letter badge there and nowhere else.
    /// </para>
    /// </remarks>
    private static readonly (string Mp2Action, string LabelKey, string Label, string[] Aspects, string Action,
        string? Missing, string? Screen, string Home)[] ENTRIES =
    {
        // MP2's "local media" is the file view - the folders the library is built from, before the importer's
        // decisions have been applied to them. This front end has exactly that: the shares screen, which lists the
        // server's folders and what it made of each. "Folder by folder" came off this line to fit the tile.
        ("Home->BrowseLocalMedia", "[Media.LocalMediaMenuItem]", "Local media", Array.Empty<string>(),
            "the server's folders", null, "folders", "17d2390e-5b05-4fbd-89f6-24d60ceb427f"),
        ("Home->BrowseMedia", "[Media.BrowseMediaMenuItem]", "Browse media", Array.Empty<string>(),
            "grouped by genre, year, actor", null, "browse", "93442df7-186d-42e5-a0f5-cf1493e68f49"),
        // "Audio" is the plugin's own word for the music library, and it covers both shapes the importer can leave
        // the collection in; the second line is here so that the narrower word does not read as a mistake.
        ("Home->Audio", "[Media.AudioMenuItem]", "Audio", new[] { "AlbumItem", "AudioItem" },
            "each album, and its tracks on it", null, null, "30715d73-4205-417f-80aa-e82f0834171f"),
        // Not "the files nobody filed as films": the server's VideoItem is the aspect every film and every episode
        // also carries, so this shelf holds the whole video library (measured against a real one: four films, eight
        // episodes and the two unfiled clips). Saying so is worth more than a tidier sentence.
        ("Home->Videos", "[Media.VideosMenuItem]", "Videos", new[] { "VideoItem" },
            "every video the server knows, films and episodes included", null, null,
            "a4df2df6-8d66-479a-9930-d7106525eb07"),
        ("Home->Movies", "[Media.MoviesMenuItem]", "Movies", new[] { "MovieItem" },
            "the films on this server", null, null, "80d2e2cc-baaa-4750-807b-f37714153751"),
        ("Home->Series", "[Media.SeriesMenuItem]", "Series", new[] { "SeriesItem" },
            "each series, and its episodes inside it", null, null, "30f57cba-459c-4202-a587-09fff5098251"),
        // The folders, not the filed titles. A photograph rarely has anything for an importer's decision to attach
        // to - no title, no year, no episode number - so a pictures screen backed by ImageItem titles shows an
        // empty shelf over thousands of photographs, which is why MP2's own pictures screen walks the folders.
        // The row still claims ImageItem, and claims it alone: claiming is what keeps the leftover rule at the
        // bottom of Build from adding a second tile for the same photographs on a server that happens to have
        // filed some, and the tile's word is the word the shelf wears - MP2 has one word for this media, and so
        // has this menu. The filed titles lose nothing by it; the browse screen reads them under the same word.
        ("Home->Images", "[Media.ImagesMenuItem]", "Images", new[] { "ImageItem" },
            "the pictures, folder by folder", null, "pictures", "55556593-9fe9-436c-a3b6-a971e10c9d44"),
        ("Home->News", "[News.Title]", "News", Array.Empty<string>(),
            "the headlines", null, "news", "bb49a591-7705-408f-8177-45d633fdfad0"),
        // The two tiles the television plugin contributed, and the only two that had nothing behind them for as
        // long as this front end existed: what they launched was MP2's TV client, whose service was deleted from
        // this tree in 029a262aa4 because it was .NET-Framework-only. They have something behind them again - the
        // guide of a TvHeadend named on the television settings, which is where a multiplatform media centre's
        // television comes from now - so the small print is gone and both name a screen.
        ("Home->SlimTvClient", "[SlimTvClient.Tv]", "TV", Array.Empty<string>(),
            "the guide, hour by hour", null, "tv",
            "b4a9199f-6dd4-4bda-a077-de9c081f7703"),
        ("Home->SlimTvClientRadio", "[SlimTvClient.Radio]", "Radio", Array.Empty<string>(),
            "the stations' guide", null, "radio",
            "e3bbc989-99db-40e9-a15f-ccb50b17a4c8"),
        ("Home->Weather", "[Weather.Title]", "Weather", Array.Empty<string>(),
            "the forecast", null, "weather", "e34fdb62-1f3e-4aa9-8a61-d143e0af77b5"),
        ("Home->PartyMusicConfig", "[PartyMusicPlayer.MenuItem]", "Party music player", Array.Empty<string>(),
            "the music shelf, shuffled", null, "party", "e00b8442-8230-4d7b-b871-6ac77755a0d5"),
        // MP2's configuration tile, and from the viewer's seat it is still the door marked Settings. What sits
        // behind the door today is the front end's own settings - its look, chosen on the couch and worn from
        // then on - because that is the configuration this front end can actually change. The server's own
        // configuration is still behind the .NET Remoting interface the new API has not grown an equivalent of;
        // when it does, that belongs behind this same tile, one level in.
        ("Home->ConfigurationMain", "[Configuration.Name]", "Settings", Array.Empty<string>(),
            "its look, chosen right here", null, "themes", "f6255762-c52a-4231-9e67-14c28735216e"),
        ("Home->UtilitiesMain", "[Utilities.MenuItem]", "Utilities", Array.Empty<string>(),
            "the little tools", "its tools went with the TV", null,
            "02992bec-1e6d-4c29-8d79-f95f59e3f625")
    };
  }
}
