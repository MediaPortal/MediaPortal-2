#region Copyright (C) 2007-2026 Team MediaPortal

/*
    Copyright (C) 2007-2026 Team MediaPortal
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

using System;
using System.Collections.Generic;
using System.Reflection;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common.Certifications;
using MediaPortal.Common.FanArt;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Services.Logging;

namespace MediaPortal.Server.Api
{
  /// <summary>
  /// The settings sections a household may read and change from a television, and what each of their fields
  /// is called, means and accepts.
  /// </summary>
  /// <remarks>
  /// <para>
  /// <b>What this replaces.</b> The reference configured a plugin through a window on the server's own desktop:
  /// every plugin that wanted to be configured shipped a GUI editor, the configuration tool listed the
  /// <c>plugin.xml</c> sections, and the editor read and wrote the plugin's settings class through
  /// <see cref="MediaPortal.Common.Settings.ISettingsManager"/>. None of that survives onto a headless server -
  /// there is no desktop, and the editors were WinForms. What is worth keeping is the shape of it: a settings
  /// class that already knows its own fields, and one screen that shows whatever a section says it holds.
  /// That shape needs one thing the reference never had, because its editors were hand-drawn screens: the words
  /// a screen shows. This table is those words.
  /// </para>
  /// <para>
  /// <b>Why a table here rather than an attribute on the settings classes.</b> The settings classes belong to
  /// the core and to the plugins; making every editable field carry display text and ranges would mean touching
  /// a dozen files that are otherwise quiet, for text that only a screen ever reads - and the one thing a
  /// television should not get to edit is every field there is. A section therefore stands open only while it
  /// is written here, field by field, which is also where the reason a field is open or left out gets said. The
  /// same argument the main menu table makes about <c>plugin.xml</c>: the copy names the original rather than
  /// replacing it, and a row is one line to add.
  /// </para>
  /// <para>
  /// <b>What is deliberately not here.</b> The database plugin's settings (which would let a television point
  /// the library at an empty file), the resource-access <c>ServerSettings</c> and
  /// <see cref="MediaPortal.Common.Services.MediaManagement.ImporterWorkerSettings"/> - both already have their
  /// own endpoint and their own screen, and a second door to the same value is how a household ends up with two
  /// switches that disagree - and anything whose change would leave the running server inconsistent, such as
  /// the plugin enable/disable list.
  /// </para>
  /// </remarks>
  public static class PluginSettingsCatalog
  {
    /// <summary>A field that is on or off.</summary>
    public const string KIND_TOGGLE = "toggle";

    /// <summary>A field that is a whole number, bounded by its own minimum and maximum.</summary>
    public const string KIND_NUMBER = "number";

    /// <summary>A field that is one of a fixed list of names.</summary>
    public const string KIND_CHOICE = "choice";

    /// <summary>
    /// A field that is a word typed in. Empty is an answer of its own ("nothing in particular"), which is why
    /// this exists next to <see cref="KIND_CHOICE"/>: a list long enough to need typing is too long to offer as
    /// buttons, and a list with an empty entry among its names cannot be offered as buttons at all.
    /// </summary>
    public const string KIND_TEXT = "text";

    /// <summary>
    /// One field of one settings class, as a screen needs it: which property it is, what to call it, what to
    /// say under it, and what it accepts.
    /// </summary>
    public sealed class Field
    {
      public Field(string key, string property, string label, string help, string kind,
          int min, int max, string[] options, Type enumType, bool restartNeeded)
      {
        Key = key;
        Property = property;
        Label = label;
        Help = help;
        Kind = kind;
        Min = min;
        Max = max;
        Options = options ?? Array.Empty<string>();
        EnumType = enumType;
        RestartNeeded = restartNeeded;
      }

      /// <summary>The name a screen sends this field back under. Lower case, with underscores.</summary>
      public string Key { get; }

      /// <summary>The property of the settings class behind it.</summary>
      public string Property { get; }

      /// <summary>The words a screen puts beside it.</summary>
      public string Label { get; }

      /// <summary>The words a screen puts under it. Empty when the label says everything.</summary>
      public string Help { get; }

      /// <summary>One of <see cref="KIND_TOGGLE"/>, <see cref="KIND_NUMBER"/>, <see cref="KIND_CHOICE"/>,
      /// <see cref="KIND_TEXT"/>.</summary>
      public string Kind { get; }

      /// <summary>Lowest accepted value. Only a number answers this question.</summary>
      public int Min { get; }

      /// <summary>Highest accepted value. Only a number answers this question.</summary>
      public int Max { get; }

      /// <summary>The names a choice offers, in the order a screen cycles them.</summary>
      public string[] Options { get; }

      /// <summary>
      /// The enum the property holds, for a choice; <c>null</c> for every other kind. The names in
      /// <see cref="Options"/> are written into it before the property is given the value.
      /// </summary>
      public Type EnumType { get; }

      /// <summary>
      /// Whether the running server has to come back before the stored value does anything. Saying "saved" for
      /// a value the running server ignores would be a lie, so the field that knows better says so here.
      /// </summary>
      public bool RestartNeeded { get; }
    }

    /// <summary>
    /// One settings class a household may reach, with the fields it may reach.
    /// </summary>
    public sealed class Section
    {
      private Type _resolved;

      /// <summary>A section whose settings class this assembly is allowed to name outright.</summary>
      public Section(string key, string title, string description, string owner, Type settingsType,
          Field[] fields)
          : this(key, title, description, owner, settingsType.FullName, fields)
      {
        SettingsType = settingsType;
      }

      /// <summary>
      /// A section whose settings class belongs to a plugin.
      /// </summary>
      /// <remarks>
      /// The API assembly names no plugin types, because the dependency would run from the core towards an
      /// extension - the direction the reference spent years getting the other way round - so such a class is
      /// written down by name and looked for among the assemblies the server has actually loaded. That is how
      /// the metadata endpoint reaches the extractors' settings, for the same reason.
      /// </remarks>
      public Section(string key, string title, string description, string owner, string settingsTypeName,
          Field[] fields)
      {
        Key = key;
        Title = title;
        Description = description;
        Owner = owner;
        SettingsTypeName = settingsTypeName;
        Fields = fields ?? Array.Empty<Field>();
      }

      /// <summary>The name in the URL. Lower case.</summary>
      public string Key { get; }

      /// <summary>What a screen calls the section.</summary>
      public string Title { get; }

      /// <summary>What the section is for, in the words a list shows under its title.</summary>
      public string Description { get; }

      /// <summary>Which part of the server the settings belong to, so a screen can say whose answer it is.</summary>
      public string Owner { get; }

      /// <summary>The settings class by its full name, whether or not this assembly may name it.</summary>
      public string SettingsTypeName { get; }

      /// <summary>The class this assembly was allowed to name outright; <c>null</c> for a plugin's own.</summary>
      private Type SettingsType { get; }

      public Field[] Fields { get; }

      /// <summary>
      /// The settings class <see cref="MediaPortal.Common.Settings.ISettingsManager"/> loads, or <c>null</c>
      /// when the part that owns it is not running here.
      /// </summary>
      /// <remarks>
      /// A name that cannot be found among the loaded assemblies is a plugin this server does not carry, not a
      /// typo to chase - the same reading the metadata endpoint gives it. Which is also why a section nobody
      /// runs is left out of the list rather than standing open onto a wall: a door the household cannot walk
      /// through is not information, it is a dead end with a handle.
      /// </remarks>
      public Type ResolveSettings()
      {
        if (SettingsType != null)
          return SettingsType;
        if (_resolved != null)
          return _resolved;

        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
          Type found = assembly.GetType(SettingsTypeName, false);
          if (found != null)
          {
            _resolved = found;
            return found;
          }
        }

        return null;
      }

      /// <summary>Whether the part that owns these settings is running in this process.</summary>
      public bool IsOnThisServer
      {
        get { return ResolveSettings() != null; }
      }

      /// <summary>The field a screen sends back under its own key, or <c>null</c> when there is no such field.</summary>
      public Field FieldFor(string key)
      {
        foreach (Field field in Fields)
        {
          if (string.Equals(field.Key, key, StringComparison.Ordinal))
            return field;
        }
        return null;
      }
    }

    /// <summary>
    /// Every section that stands open, in the order a list shows them.
    /// </summary>
    public static IReadOnlyList<Section> Sections { get; } = new List<Section>
    {
      new Section("fanart", "How many pictures of each kind one title keeps",
          "The library fetches pictures for a title and keeps the first few of every kind. This is that number.",
          "Media library", typeof(FanArtSettings),
          new Field[]
          {
            Number("max_posters", "MaxPosterFanArt", "Posters",
                "The poster is the picture a tile wears."),
            Number("max_banners", "MaxBannerFanArt", "Banners",
                "The wide strip with the title's name on it."),
            Number("max_backdrops", "MaxBackdropFanArt", "Backdrops",
                "The picture behind a title's own screen. The reference kept three."),
            Number("max_thumbs", "MaxThumbFanArt", "Thumbnails", ""),
            Number("max_clear_art", "MaxClearArt", "Clear art",
                "The title's logo without a background."),
            Number("max_disc_art", "MaxDiscArt", "Disc art",
                "The picture of the disc or cover art of the release."),
            Number("max_logos", "MaxLogoFanArt", "Logos", "")
          }),

      new Section("virtual_media", "Whether the titles there are not are shown too",
          "The library sometimes learns of a film, an episode or an album that stands on no share. This says "
          + "whether those belong on the shelves.",
          "Media library", typeof(VirtualMediaSettings),
          new Field[]
          {
            // The reference asked these per signed-in person, beside its browsing choices. Kept as Global here,
            // because a browse is answered before anybody is known; the wording on the screen says what the
            // switch does rather than promising a per-person answer it cannot give.
            Toggle("show_virtual_movies", "ShowVirtualMovieMedia", "Films and collections",
                "A collection shows every film that belongs to it, even the ones nobody has. Off is what this "
                + "runs with, because a cover that turns out to be a promise is a shelf one stops trusting."),
            Toggle("show_virtual_series", "ShowVirtualSeriesMedia", "Series, seasons and episodes",
                "A series shows the episodes it is missing too, so a show one is in the middle of collecting "
                + "stands complete. The ones without a file cannot be played."),
            Toggle("show_virtual_audio", "ShowVirtualAudioMedia", "Albums and recordings",
                "An album shows every recording that belongs to it, even the ones nobody has.")
          }),

      new Section("certifications", "In which country's words an age certificate is shown",
          "A title is imported with the mark of the country that rated it. This names the country whose mark a "
          + "screen should speak instead.",
          "Media library", typeof(MediaCertificationSettings),
          new Field[]
          {
            // Two letters typed in rather than a list of the thirty-one countries the table knows: the list is
            // too long for the row a choice is built out of, and its first entry - "as imported", which is what
            // everything runs with - is an empty string, which no button can be labelled with.
            Country("movie_country", "DisplayMovieCertificationCountry", "Films",
                "Two letters, like \"DE\" or \"US\". Empty - which is what this runs with - keeps the mark the "
                + "title was imported with, and so does a mark the server does not know."),
            Country("series_country", "DisplaySeriesCertificationCountry", "Series",
                "Two letters, like \"DE\" or \"US\". Empty keeps the imported mark.")
          }),

      new Section("logging", "How much the server writes down",
          "The one switch that makes a support question answerable: what the server leaves in its log.",
          "Server", typeof(LogSettings),
          new Field[]
          {
            // The log level is read when the log manager comes up, so the stored value does nothing to the
            // process that stored it. The field says so rather than letting a screen promise otherwise.
            Choice("log_level", "LogLevel", "Log level",
                "\"Debug\" writes a great deal; it is what to ask for before sending a log, and what to turn "
                + "off again afterwards.",
                typeof(LogLevel),
                new string[]
                {
                  nameof(LogLevel.None), nameof(LogLevel.Critical), nameof(LogLevel.Error),
                  nameof(LogLevel.Warning), nameof(LogLevel.Information), nameof(LogLevel.Debug),
                  nameof(LogLevel.All)
                }, true)
          }),

      // The reader of the .nfo files a media manager leaves beside a film. The reference never gave this
      // extractor a configuration window at all - its settings were edited as XML, if at all - so these three
      // sections go past the reference rather than catching up with it. They are here because the extractor is
      // the one extractor this server can actually run everywhere (it is the only one that is not
      // Windows-bound), and because the fields worth asking about are the same two questions the metadata
      // screen already asks of the online scrapers: which details, and how many of them.
      //
      // The two pictures switches at the bottom of each of them are the reference's own, from the window it
      // gives every extractor ("Cache FanArt on a local share", "Cache FanArt on a network share"). They are
      // worth having here for a reason that only shows up when the numbers above are already in place: they are
      // the switches that decide whether the pictures a media manager filed next to a title are looked at at
      // all. The four extractors that ran that walk in the reference are built for Windows, so a portable
      // server without these switches had no path from a film's <c>clearlogo.png</c> to its library.
      //
      // What is left out of them, and why: the file name lists, the separator and ignore strings (a set of
      // strings is not a question a remote can answer, and every one of them defaults to something that works),
      // the IMDb id pattern (a regular expression typed on a television is a support ticket), the preferred
      // certification country (it wants a list of countries this server does not own), and the switch for
      // searching online (this server has no online scraper, so it would be a question with one answer).

      new Section("nfo_movie", "What the .nfo reader takes for films",
          "Beside a film there is often a small .nfo file written by another program. This is what the server "
          + "reads out of it.",
          "Media library", NFO_MOVIE_SETTINGS,
          new Field[]
          {
            Toggle("include_actors", "IncludeActorDetails", "The actors",
                "The people who played in it, as the file names them."),
            Toggle("include_characters", "IncludeCharacterDetails", "The characters",
                "Which actor played whom."),
            Toggle("include_directors", "IncludeDirectorDetails", "The directors", ""),
            Count("max_actors", "MaximumActorCount", "Actors kept",
                "The rest are read and dropped."),
            Count("max_characters", "MaximumCharacterCount", "Characters kept", ""),
            CacheLocalPictures(),
            CacheNetworkPictures(),
            DebugLogging("films"),
            Toggle("write_nfo_into_log", "WriteRawNfoFileIntoDebugLog", "Write the file itself into the log",
                "Only useful while something is being worked out, and only alongside the line above.")
          }),

      new Section("nfo_series", "What the .nfo reader takes for series",
          "The same questions as for films, asked of the file beside a series.",
          "Media library", NFO_SERIES_SETTINGS,
          new Field[]
          {
            Toggle("include_actors", "IncludeActorDetails", "The actors", ""),
            Toggle("include_characters", "IncludeCharacterDetails", "The characters", ""),
            Count("max_actors", "MaximumActorCount", "Actors kept", ""),
            Count("max_characters", "MaximumCharacterCount", "Characters kept", ""),
            CacheLocalPictures(),
            CacheNetworkPictures(),
            DebugLogging("series"),
            Toggle("write_nfo_into_log", "WriteRawNfoFileIntoDebugLog", "Write the file itself into the log", "")
          }),

      new Section("nfo_audio", "What the .nfo reader takes for music",
          "What a file beside an album or an artist can tell the library about it. The pictures lying in a "
          + "music folder are the tag reader's work, so its own section asks about them.",
          "Media library", NFO_AUDIO_SETTINGS,
          new Field[]
          {
            Toggle("include_artists", "IncludeArtistDetails", "The artist", ""),
            Toggle("include_albums", "IncludeAlbumDetails", "The album", ""),
            DebugLogging("music"),
            Toggle("write_nfo_into_log", "WriteRawNfoFileIntoDebugLog", "Write the file itself into the log", "")
          }),

      // The reader that opens the tags of the music files. In the reference the two questions below belong to
      // its music extractor; here the music extractor it would name is built for Windows and cannot be loaded,
      // and the reader that walks an album's folder everywhere else is this one.
      new Section("audio_tags", "What the music reader keeps from the folders",
          "Beside the recording, an album's folder often holds its cover, the picture of the disc and the "
          + "title's logo. This says when those are copied into the server's own folder.",
          "Media library", AUDIO_TAGS_SETTINGS,
          new Field[]
          {
            CacheLocalPictures(),
            CacheNetworkPictures()
          }),
    };

    /// <summary>The reader of film .nfo files. Named, because this assembly may not reference the plugin.</summary>
    private const string NFO_MOVIE_SETTINGS =
        "MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.Settings.NfoMovieMetadataExtractorSettings";

    private const string NFO_SERIES_SETTINGS =
        "MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.Settings.NfoSeriesMetadataExtractorSettings";

    private const string NFO_AUDIO_SETTINGS =
        "MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.Settings.NfoAudioMetadataExtractorSettings";

    /// <summary>The reader of the tags of music files, which is also the one that walks a music folder.</summary>
    private const string AUDIO_TAGS_SETTINGS =
        "MediaPortal.Extensions.MetadataExtractors.AudioTagMetadataExtractor.Settings.AudioTagMetadataExtractorSettings";

    /// <summary>The open section named <paramref name="key"/>, or <c>null</c> when nothing stands open under it.</summary>
    public static Section Find(string key)
    {
      foreach (Section section in Sections)
      {
        if (string.Equals(section.Key, key, StringComparison.Ordinal))
          return section;
      }
      return null;
    }

    private static Field Number(string key, string property, string label, string help)
    {
      // One to twenty. One because a picture the library fetched and threw away at once is not a setting
      // anybody wants; twenty because a household that wants more backdrops than that is not sitting ten feet
      // away from a television, and this screen is for that household.
      return new Field(key, property, label, help, KIND_NUMBER, 1, 20, null, null, false);
    }

    /// <summary>
    /// A count of how many of something to keep. Zero to a hundred, which is the ground the metadata endpoint
    /// settled on for the same question: the reference hands the stored number to a LINQ <c>Take</c>, which
    /// throws on a negative and is happy with none, and the ceiling is this API's own because the reference
    /// states none.
    /// </summary>
    private static Field Count(string key, string property, string label, string help)
    {
      return new Field(key, property, label, help, KIND_NUMBER, 0, 100, null, null, false);
    }

    private static Field Toggle(string key, string property, string label, string help)
    {
      return new Field(key, property, label, help, KIND_TOGGLE, 0, 0, null, null, false);
    }

    /// <summary>
    /// A country named by its two letters. Offered as a word to type rather than a list to pick from, because
    /// the certificate table knows thirty-one countries - more than a row of buttons carries usefully - and
    /// because the first answer, "the country the title came from", is no name at all and so has nothing to
    /// label a button with.
    /// </summary>
    private static Field Country(string key, string property, string label, string help)
    {
      return new Field(key, property, label, help, KIND_TEXT, 0, 0, null, null, false);
    }

    /// <summary>
    /// The reader's own logging switch. It belongs to the class all three readers share, so the same line
    /// stands in every one of their sections - which is right rather than repetitive, because a household
    /// working out why one kind of media comes out wrong wants to turn it on for that kind only.
    /// </summary>
    private static Field DebugLogging(string subject)
    {
      return Toggle("debug_logging", "EnableDebugLogging", "Log what the reader does",
          "Off is what this runs with. For " + subject + " it writes what it reads into the server's log.");
    }

    /// <summary>
    /// The switch that asks whether the pictures lying next to a title on a disk in this machine are copied.
    /// </summary>
    /// <remarks>
    /// One builder for three sections, because the question is the same question and a household answers it
    /// differently for each kind of shelf: films that live on a disk in the box gain nothing from a second copy
    /// of every picture, while a shelf on a machine that is switched off at night loses its pictures without
    /// one. A change is seen by the next title the importer walks, not by the ones already walked.
    /// </remarks>
    private static Field CacheLocalPictures()
    {
      return Toggle("cache_local_fanart", "CacheLocalFanArt",
          "Copies of the pictures lying beside the files on a disk in this machine",
          "Off is what this runs with, because the pictures are read where they lie and a copy of all of them "
          + "only fills the disk the films are already on.");
    }

    /// <summary>
    /// The switch that asks whether the pictures lying next to a title on a network share are copied.
    /// </summary>
    private static Field CacheNetworkPictures()
    {
      return Toggle("cache_network_fanart", "CacheOfflineFanArt",
          "Copies of the pictures lying beside the files on a share that might be asleep",
          "On is what this runs with: a shelf on another machine keeps its pictures after that machine is "
          + "switched off, which is what the copy is for.");
    }

    private static Field Choice(string key, string property, string label, string help, Type enumType,
        string[] options, bool restartNeeded)
    {
      return new Field(key, property, label, help, KIND_CHOICE, 0, 0, options, enumType, restartNeeded);
    }
  }
}
