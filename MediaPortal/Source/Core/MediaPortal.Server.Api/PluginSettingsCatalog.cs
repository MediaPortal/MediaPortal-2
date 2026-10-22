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

      /// <summary>One of <see cref="KIND_TOGGLE"/>, <see cref="KIND_NUMBER"/>, <see cref="KIND_CHOICE"/>.</summary>
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
      public Section(string key, string title, string description, string owner, Type settingsType,
          Field[] fields)
      {
        Key = key;
        Title = title;
        Description = description;
        Owner = owner;
        SettingsType = settingsType;
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

      /// <summary>The settings class <see cref="MediaPortal.Common.Settings.ISettingsManager"/> loads.</summary>
      public Type SettingsType { get; }

      public Field[] Fields { get; }

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
    };

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

    private static Field Choice(string key, string property, string label, string help, Type enumType,
        string[] options, bool restartNeeded)
    {
      return new Field(key, property, label, help, KIND_CHOICE, 0, 0, options, enumType, restartNeeded);
    }
  }
}
