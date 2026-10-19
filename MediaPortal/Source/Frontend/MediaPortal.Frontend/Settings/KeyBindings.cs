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
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia.Input;

namespace MediaPortal.Frontend.Settings
{
  /// <summary>The keys a viewer can hand to a remote: the commands that reach the screens apart from the
  /// arrow ring, the fields' own typing, and Enter, which every screen spends on its own business.</summary>
  public enum KeyAction
  {
    /// <summary>Away with the newest thing on the screen; the shell pops the screen when nothing is open.</summary>
    Back,

    /// <summary>The remote's menu key: the list menu on a title screen, the on-screen keyboard on a field.</summary>
    Menu,

    /// <summary>What the selected title is, next to it instead of playing it.</summary>
    Details,

    /// <summary>The transport key of the remote; on a title screen it plays the selected row.</summary>
    PlayPause,

    /// <summary>Moving the focus to the search field of the screen that has one.</summary>
    Find,

    /// <summary>Round through the watched states.</summary>
    Watched,

    /// <summary>Rows, tiles, covers.</summary>
    View,

    /// <summary>The picture the tiles wear.</summary>
    Media
  }

  /// <summary>
  /// Which keys of which remotes mean which command - the reference's key assignments, which lived in its
  /// input server's XML, as a small JSON file instead of hard-coded key constants.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The reference's client translated remote buttons through <c>InputMappings.xml</c> before a screen ever
  /// saw a key; our screens have so far compared against hard-coded keys, which works until a viewer's
  /// remote reports its menu button as something other than F16. This file is that translation: an action
  /// answers to any key in its list, and a remote whose buttons report differently is fitted by editing
  /// JSON, not by recompiling the front end.
  /// </para>
  /// <para>
  /// The defaults are exactly the keys the screens compared against before this class existed - the file
  /// changes nothing until somebody edits it. What is deliberately NOT mapped: the arrow keys (the ring is
  /// layout, not preference), Enter and Escape-as-typing (each screen spends them on its own business), and
  /// the letters of the on-screen keyboard.
  /// </para>
  /// </remarks>
  public sealed class KeyBindings
  {
    private const string SETTINGS_FILE_NAME = "keybindings.json";

    /// <summary>
    /// Where the bindings are read from; overridable with <c>MP2_KEYBINDINGS_FILE</c> so that tests never
    /// read what a viewer on the machine edited.
    /// </summary>
    public static string DefaultFilePath
    {
      get
      {
        string? fromEnvironment = Environment.GetEnvironmentVariable("MP2_KEYBINDINGS_FILE");
        if (!string.IsNullOrWhiteSpace(fromEnvironment))
          return fromEnvironment;

        string serverFile = ServerSettings.DefaultFilePath;
        string? folder = Path.GetDirectoryName(serverFile);
        if (string.IsNullOrEmpty(folder))
          folder = Path.GetTempPath();
        return Path.Combine(folder, SETTINGS_FILE_NAME);
      }
    }

    /// <summary>Action name to the key names that mean it.</summary>
    [JsonPropertyName("bindings")]
    public Dictionary<string, List<string>> Bindings { get; set; } =
        new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

    /// <summary>The keys the shipped front end was written with - the answer when no file was edited.</summary>
    public static KeyBindings TheDefaults()
    {
      KeyBindings defaults = new KeyBindings();
      defaults.Bindings[KeyAction.Back.ToString()] = new List<string> { "Escape", "Back" };
      defaults.Bindings[KeyAction.Menu.ToString()] = new List<string> { "F16" };
      defaults.Bindings[KeyAction.Details.ToString()] = new List<string> { "I", "Apps" };
      defaults.Bindings[KeyAction.PlayPause.ToString()] = new List<string> { "MediaPlayPause" };
      defaults.Bindings[KeyAction.Find.ToString()] = new List<string> { "F" };
      defaults.Bindings[KeyAction.Watched.ToString()] = new List<string> { "W" };
      defaults.Bindings[KeyAction.View.ToString()] = new List<string> { "V" };
      defaults.Bindings[KeyAction.Media.ToString()] = new List<string> { "M" };
      return defaults;
    }

    /// <summary>
    /// The bindings the front end runs on, read once from the file - and a key nobody mapped still answers
    /// what it was written to answer, so half an edited file never silences a remote button: the file's list
    /// for an action replaces the default, and an action the file forgot keeps its default keys.
    /// </summary>
    public static KeyBindings Current
    {
      get
      {
        if (_current == null)
        {
          lock (_gate)
          {
            if (_current == null)
            {
              KeyBindings read = Load(null, out _);
              KeyBindings merged = TheDefaults();
              foreach (KeyValuePair<string, List<string>> entry in read.Bindings)
              {
                if (entry.Value != null)
                  merged.Bindings[entry.Key] = entry.Value;
              }
              _current = merged;
            }
          }
        }
        return _current;
      }
    }

    private static readonly object _gate = new object();
    private static KeyBindings? _current;

    /// <summary>For the tests and the day a settings screen can write the file: the bindings to run on.</summary>
    public static void Override(KeyBindings? bindings)
    {
      lock (_gate)
      {
        _current = bindings;
      }
    }

    /// <summary>Whether this key means this command - what every screen asks instead of naming keys itself.</summary>
    public bool Is(KeyAction action, Key key)
    {
      List<string>? names;
      if (!Bindings.TryGetValue(action.ToString(), out names) || names == null)
        return false;
      foreach (string name in names)
      {
        if (IsNamedKey(name, key))
          return true;
      }
      return false;
    }

    /// <summary>The keys that mean this command, for a screen that has to say them to the viewer.</summary>
    public IReadOnlyList<Key> KeysFor(KeyAction action)
    {
      List<Key> keys = new List<Key>();
      List<string>? names;
      if (Bindings.TryGetValue(action.ToString(), out names) && names != null)
      {
        foreach (string name in names)
        {
          Key parsed;
          if (TryParseKey(name, out parsed) && !keys.Contains(parsed))
            keys.Add(parsed);
        }
      }
      return keys;
    }

    private static bool IsNamedKey(string name, Key key)
    {
      Key parsed;
      return TryParseKey(name, out parsed) && parsed == key;
    }

    private static bool TryParseKey(string name, out Key key)
    {
      // The names are the keys' own words - "F16", "MediaPlayPause", "Escape" - the same spelling Avalonia
      // prints them with, so no second table of names to keep true.
      return Enum.TryParse(name.Trim(), true, out key);
    }

    /// <summary>
    /// Reads the file. A missing or broken file answers nothing of its own - the defaults stand behind it,
    /// because a remote must keep working on a machine whose JSON a viewer half-edited.
    /// </summary>
    public static KeyBindings Load(string? path, out string? problem)
    {
      problem = null;
      string file = path ?? DefaultFilePath;

      try
      {
        if (!File.Exists(file))
          return new KeyBindings();

        string text = File.ReadAllText(file);
        if (string.IsNullOrWhiteSpace(text))
          return new KeyBindings();

        KeyBindings? read = JsonSerializer.Deserialize<KeyBindings>(text, JsonFileOptions);
        if (read == null)
        {
          problem = string.Format("{0} did not contain any key bindings.", file);
          return new KeyBindings();
        }

        read.Bindings ??= new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        return read;
      }
      catch (Exception ex)
      {
        problem = string.Format("{0} could not be read ({1}: {2}); the keys answer as they were written.",
            file, ex.GetType().Name, ex.Message);
        return new KeyBindings();
      }
    }

    /// <summary>Writes the file, saying rather than throwing what went wrong, like every other memory.</summary>
    public string Save(string? path, out bool saved)
    {
      string file = path ?? DefaultFilePath;
      try
      {
        string? folder = Path.GetDirectoryName(file);
        if (!string.IsNullOrEmpty(folder))
          Directory.CreateDirectory(folder);

        string temporary = file + ".new";
        File.WriteAllText(temporary, JsonSerializer.Serialize(this, JsonFileOptions));
        File.Move(temporary, file, true);
        saved = true;
        return string.Format("Saved to {0}.", file);
      }
      catch (Exception ex)
      {
        saved = false;
        return string.Format("Could not be saved ({0}: {1}).", ex.GetType().Name, ex.Message);
      }
    }

    private static readonly JsonSerializerOptions JsonFileOptions = new JsonSerializerOptions
    {
      WriteIndented = true,
      ReadCommentHandling = JsonCommentHandling.Skip,
      AllowTrailingCommas = true
    };
  }
}
