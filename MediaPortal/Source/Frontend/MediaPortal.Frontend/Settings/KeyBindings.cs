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
  /// arrow ring and the fields' own typing. The remote's OK is here too (as <see cref="KeyAction.Select"/>),
  /// because the platforms disagree about its name.</summary>
  public enum KeyAction
  {
    /// <summary>Away with the newest thing on the screen; the shell pops the screen when nothing is open.</summary>
    Back,

    /// <summary>
    /// What the thing under the ring offers: open the tile, press the plate, accept the row. Every platform
    /// spells the remote's OK button differently - a television sends ENTER, the windowing system calls the
    /// same key Return, and Android's D-pad centre reports it as Space - so all three spellings belong to
    /// this one action instead of to a screen's own comparison.
    /// </summary>
    Select,

    /// <summary>The remote's menu key: the list menu on a title screen, the on-screen keyboard on a field.</summary>
    Menu,

    /// <summary>The screen's own menu, which the reference hung off the edge of every media screen.</summary>
    MediaMenu,

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
    Media,

    /// <summary>
    /// The transport's stop: the film ends and its screen leaves. The remote's stop button and the plate on the
    /// player's row say the same thing, which is why both ask this action rather than a key of their own.
    /// </summary>
    Stop,

    /// <summary>The room's sound, one step louder. The room's key, not the screen's - see ShellWindow.</summary>
    VolumeUp,

    /// <summary>The room's sound, one step quieter.</summary>
    VolumeDown,

    /// <summary>The room's sound, gone; and back again.</summary>
    Mute
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
  /// The defaults are the keys the screens compared against before this class existed, grown by the keys a
  /// television's own remote reports - see <see cref="TheDefaults"/> for where each of those comes from. What is
  /// deliberately NOT mapped: the arrow keys (the ring is layout, not preference) and the letters of the
  /// on-screen keyboard.
  /// </para>
  /// <para>
  /// A file a viewer wrote replaces its action's whole list, which is what lets a remote be refitted; the price
  /// is that an action a file names keeps only the file's keys, so a file written before an action learned a new
  /// key of a remote does not gain it by itself.
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

    /// <summary>
    /// The keys the shipped front end answers without anybody editing a file: the keyboard's letters and the
    /// reference's function keys, plus the media keys a television's remote reports its transport, volume and
    /// info buttons as. Those names are not guesses: the kernel's CEC keymap
    /// (<c>drivers/media/rc/keymaps/rc-cec.c</c>) decides what a CEC remote's button becomes on Linux, where it
    /// reaches this code as the Avalonia key of that name. What an Android TV remote reports is measured on the
    /// device rather than assumed - and a remote that answers differently is fitted here, in JSON, not in code.
    /// </summary>
    public static KeyBindings TheDefaults()
    {
      KeyBindings defaults = new KeyBindings();
      defaults.Bindings[KeyAction.Back.ToString()] = new List<string> { "Escape", "Back" };
      // The remote's OK button, in the spellings the platforms hand it over in: a television's ENTER through
      // the kernel's CEC keymap (drivers/media/rc/keymaps/rc-cec.c maps code 0x0d to KEY_ENTER), the
      // windowing systems' own enter key, and the D-pad centre of an Android remote, which Avalonia reports
      // as Space (src/Android/Avalonia.Android/Platform/Input/AndroidKeyboardDevice.cs). "Return" is the name
      // Avalonia prints for the logical key - it gives Enter and Return the same value and prints the second
      // name - while "Enter" is the name of the PHYSICAL key, which is what a remote that arrives unnamed is
      // recognised by; both names are listed because the two questions are different.
      defaults.Bindings[KeyAction.Select.ToString()] =
         new List<string> { "Return", "Enter", "Space", "Select" };
      defaults.Bindings[KeyAction.Menu.ToString()] = new List<string> { "F16" };
      // The reference's media screens bound the remote's Info key to opening the screen's own menu
      // (master_mediamenu.xaml:400), and that key reaches this code as Apps; the letter is the same question for a
      // keyboard. A television's remote asks the same question with its Info button, which the kernel's CEC keymap
      // hands over as MediaInfo, and with its contents-menu button, which comes over as MediaMenu. The details,
      // which this screen used to hang on the same key, are a second half of the view question and moved to the
      // letter of their own word - which is also where the reference kept them, behind the view mode dialog
      // rather than on a key of the shelf.
      defaults.Bindings[KeyAction.MediaMenu.ToString()] = new List<string> { "I", "Apps", "MediaInfo", "MediaMenu" };
      defaults.Bindings[KeyAction.Details.ToString()] = new List<string> { "D" };
      // The keys a television's own remote reports, which is what the front end has to answer to sit in front of
      // a set. A CEC remote reaches this code through the kernel's CEC keymap (drivers/media/rc/keymaps/
      // rc-cec.c), which turns the remote's codes into ordinary input keys: its arrows and its OK arrive as the
      // arrow keys and Enter, which the ring and the screens already answer, while its Pause-Play (0x61), Stop
      // (0x45/0x64), Info (0x35), volume (0x41/0x42) and mute (0x43/0x65) arrive as KEY_PLAYPAUSE, KEY_STOPCD,
      // KEY_INFO, KEY_VOLUMEUP/DOWN and KEY_MUTE. What a given windowing system makes of those - and on X11 it
      // makes nothing of the media and volume ones by itself, which is why a screen asks the event rather than
      // the key - is written down at Is(KeyAction, KeyEventArgs).
      //
      // The keyboard's stand-ins for a keyboard without a volume key ride along under the volume actions, as they
      // did while the shell compared against these keys in code.
      defaults.Bindings[KeyAction.PlayPause.ToString()] = new List<string> { "MediaPlayPause", "Play" };
      defaults.Bindings[KeyAction.Stop.ToString()] = new List<string> { "MediaStop" };
      defaults.Bindings[KeyAction.Find.ToString()] = new List<string> { "F" };
      defaults.Bindings[KeyAction.Watched.ToString()] = new List<string> { "W" };
      defaults.Bindings[KeyAction.View.ToString()] = new List<string> { "V" };
      defaults.Bindings[KeyAction.Media.ToString()] = new List<string> { "M" };
      defaults.Bindings[KeyAction.VolumeUp.ToString()] = new List<string> { "VolumeUp", "OemPlus" };
      defaults.Bindings[KeyAction.VolumeDown.ToString()] = new List<string> { "VolumeDown", "OemMinus" };
      defaults.Bindings[KeyAction.Mute.ToString()] = new List<string> { "VolumeMute" };
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
              _current = Merged(read, TheDefaults(), out string? repaired);
              if (repaired != null)
                Console.WriteLine("bindings: " + repaired);
            }
          }
        }
        return _current;
      }
    }

    /// <summary>
    /// The defaults with a viewer's file laid over them, plus the one repair a file written before the front end
    /// split an action out of another cannot do for itself.
    /// </summary>
    /// <param name="repaired">
    /// What was repaired, for whoever reads the console; null when the file was taken as it stands.
    /// </param>
    public static KeyBindings Merged(KeyBindings read, KeyBindings defaults, out string? repaired)
    {
      repaired = null;
      KeyBindings merged = new KeyBindings();
      foreach (KeyValuePair<string, List<string>> entry in defaults.Bindings)
        merged.Bindings[entry.Key] = new List<string>(entry.Value);

      foreach (KeyValuePair<string, List<string>> entry in read.Bindings)
      {
        if (entry.Value == null)
          continue;

        string? leftover = LeftoverOfASplit(entry.Key, entry.Value, defaults);
        if (leftover != null)
        {
          repaired = (repaired == null ? "" : repaired + "; ") + entry.Key + " was still bound to the keys of "
              + leftover + ", which it answered before the two were split apart; it answers "
              + string.Join("/", defaults.Bindings[entry.Key]) + " again";
          continue;
        }

        merged.Bindings[entry.Key] = new List<string>(entry.Value);
      }

      return merged;
    }

    /// <summary>
    /// The name of the action whose own default keys a file entry repeats, when it repeats one at all - and null
    /// when the entry is the viewer's own choice.
    /// </summary>
    /// <remarks>
    /// A file written while one key answered both questions - the screen's menu and the details having been the
    /// same <c>I</c> - binds the older name to the keys the younger action has taken for itself. Kept, that gives
    /// one key to two questions and takes away the letter the younger action moved to, so an entry that repeats
    /// another action's default while its OWN default is a different pair is read as what it is: the leftover of
    /// a split, not a choice. A viewer who really does want two actions on one key still gets it, because the
    /// key they chose is not the other action's whole default pair.
    /// </remarks>
    private static string? LeftoverOfASplit(string action, List<string> keys, KeyBindings defaults)
    {
      List<string>? own;
      if (!defaults.Bindings.TryGetValue(action, out own) || own == null)
        return null;
      if (SameKeys(own, keys))
        return null;

      foreach (KeyValuePair<string, List<string>> other in defaults.Bindings)
      {
        if (string.Equals(other.Key, action, StringComparison.OrdinalIgnoreCase))
          continue;
        if (SameKeys(other.Value, keys))
          return other.Key;
      }

      // The action the keys went to may have learned further keys since the split - the screen's menu answers a
      // television's Info button as well as the two keys it took over - in which case no default list equals the
      // file's list any more and the pair has to be remembered here.
      foreach (KeyValuePair<string, Split> split in _splits)
      {
        if (!string.Equals(split.Key, action, StringComparison.OrdinalIgnoreCase))
          continue;
        if (SameKeys(split.Value.Keys, keys))
          return split.Value.Took;
      }

      return null;
    }

    /// <summary>One of the times this front end moved a question to a key of its own: which action kept the
    /// keys, and the pair the two answered together until then.</summary>
    private readonly struct Split
    {
      public Split(string took, List<string> keys)
      {
        Took = took;
        Keys = keys;
      }

      public string Took { get; }

      public List<string> Keys { get; }
    }

    /// <summary>
    /// The one split this front end has made. Written down rather than read off the defaults, because the action
    /// that took the keys has since learned the further keys a television's remote reports, so the pair it answers
    /// today is no longer the pair the split left behind - and a file written back then has to be recognised by
    /// the pair it was written with.
    /// </summary>
    private static readonly KeyValuePair<string, Split>[] _splits =
    {
      new KeyValuePair<string, Split>("Details", new Split("MediaMenu", new List<string> { "I", "Apps" })),
    };

    private static bool SameKeys(List<string> first, List<string> second)
    {
      if (first == null || second == null || first.Count != second.Count)
        return false;

      foreach (string name in first)
      {
        bool found = false;
        foreach (string other in second)
        {
          if (string.Equals((name ?? "").Trim(), (other ?? "").Trim(), StringComparison.OrdinalIgnoreCase))
          {
            found = true;
            break;
          }
        }

        if (!found)
          return false;
      }

      return true;
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

    /// <summary>
    /// Whether this key means this command - what every screen asks instead of naming keys itself. Screens that
    /// have the event ask <see cref="Is(KeyAction, KeyEventArgs)"/> instead, which also reads the physical key.
    /// </summary>
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

    /// <summary>
    /// Whether the key a key event carries means this command. Asked of the event rather than of its
    /// <see cref="Key"/> alone because a remote's transport and volume buttons arrive on X11 with no logical key
    /// at all - Avalonia's own X11 table (src/Avalonia.X11/X11KeyTransform.cs) leaves
    /// <c>Key.VolumeUp</c>, <c>Key.MediaPlayPause</c>, <c>Key.MediaStop</c> and their brothers commented out -
    /// while the physical key under them IS filled in from the scan code, which for an evdev device is the
    /// kernel's key code plus eight. So where no key came over, the key that was physically pressed is asked,
    /// through Avalonia's own translation of it: one spelling in the binding file then answers a windowing
    /// system that reports the button and a windowing system that only reports the key under it.
    /// </summary>
    public bool Is(KeyAction action, KeyEventArgs args)
    {
      return args.Key != Key.None
          ? Is(action, args.Key)
          : Is(action, args.PhysicalKey.ToQwertyKey());
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
