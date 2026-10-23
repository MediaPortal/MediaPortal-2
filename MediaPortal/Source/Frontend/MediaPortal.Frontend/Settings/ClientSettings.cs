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
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MediaPortal.Frontend.Settings;

/// <summary>
/// What this machine itself was told: its name on the network's tongue, whether it fills the television or
/// sits in a window, which screen it opens on when it wakes, and which language its dates speak.
/// </summary>
/// <remarks>
/// <para>
/// The reference kept these as client settings of their own - not the server's business, not the skin's -
/// and this class is their equivalent: one JSON file beside <c>theme.json</c> and the skin settings, in the
/// same folder, overridable with <c>MP2_CLIENT_FILE</c> for exactly the reason the others are overridable:
/// the gates and the tests must not read or write what a real viewer set.
/// </para>
/// <para>
/// The groups the reference's client settings carried beyond these (screensaver, autostart, sound scheme)
/// arrive with the machinery they belong to - a screensaver needs something to show, autostart is the
/// operating system's own business, and sounds belong with the player that will play them. What is here is
/// what has a screen and an effect today, which is the rule every settings class in this front end keeps.
/// </para>
/// </remarks>
public sealed class ClientSettings
{
  private const string SETTINGS_FILE_NAME = "client.json";

  /// <summary>The wake-up answer that never changes: the library, at the bottom of every way through.</summary>
  public const string START_HOME = "Home";

  /// <summary>The wake-up answer that follows the viewer: whatever shelf was last opened, if it still exists.</summary>
  public const string START_LAST_SHELF = "Last shelf";

  /// <summary>
  /// Raised after any client setting was written, so the window can follow its own answers - full screen is
  /// a thing a viewer wants to see happen when they switch it, not after the next start.
  /// </summary>
  public static event EventHandler? Changed;

  /// <summary>
  /// Where the client's own answers are saved. Overridable with <c>MP2_CLIENT_FILE</c>; otherwise the
  /// neighbour of the server settings file, which itself follows <c>MP2_SETTINGS_FILE</c> - so one variable
  /// sent at the gate or the test host isolates every file this front end writes.
  /// </summary>
  public static string DefaultFilePath
  {
    get
    {
      string? fromEnvironment = Environment.GetEnvironmentVariable("MP2_CLIENT_FILE");
      if (!string.IsNullOrWhiteSpace(fromEnvironment))
        return fromEnvironment;

      string? folder = Path.GetDirectoryName(ServerSettings.DefaultFilePath);
      if (string.IsNullOrEmpty(folder))
        folder = Path.GetTempPath();
      return Path.Combine(folder, SETTINGS_FILE_NAME);
    }
  }

  /// <summary>
  /// The name this machine answers to. The reference showed the client name in the system status and let
  /// the viewer name the box their television stands on; a machine nobody named calls itself what the
  /// operating system calls it, which is at least a name that is already on the label.
  /// </summary>
  [JsonPropertyName("clientName")]
  public string ClientName { get; set; } = Environment.MachineName;

  /// <summary>Whether the front end fills the whole television or sits in a window on the desktop.</summary>
  [JsonPropertyName("fullScreen")]
  public bool FullScreen { get; set; } = true;

  /// <summary>
  /// Whether the worn skin's own sounds - the step of a walk through a list, the click of a press - are
  /// heard at all. The reference's televisions clicked and stepped; a quiet household turns this off.
  /// </summary>
  [JsonPropertyName("skinSounds")]
  public bool SkinSounds { get; set; } = true;

  /// <summary>Which screen the front end opens on: <see cref="START_HOME"/> or <see cref="START_LAST_SHELF"/>.</summary>
  [JsonPropertyName("startScreen")]
  public string StartScreen { get; set; } = START_HOME;

  /// <summary>
  /// The label of the shelf the viewer last opened a whole of, remembered so that
  /// <see cref="START_LAST_SHELF"/> has something to reopen. A label rather than a stored address because
  /// shelves come and go with the library; a remembered label that no longer matches opens the home screen
  /// and says so, which is the honest version of a promise the library broke.
  /// </summary>
  [JsonPropertyName("lastShelf")]
  public string LastShelf { get; set; } = "";

  /// <summary>
  /// The language the viewer asked the calendar to speak - empty means the one the operating system was
  /// installed with. Dates and numbers follow this; the words on the screen follow the server, because the
  /// menu labels are the server's data.
  /// </summary>
  [JsonPropertyName("culture")]
  public string Culture { get; set; } = "";

  /// <summary>
  /// How many minutes of nobody touching anything it takes for the screen to go black; <c>0</c>, the shipped
  /// answer, means the screen never does. The reference asked the operating system how long the keyboard had
  /// been silent - a Windows call this front end cannot make - so it counts the presses and moves that come
  /// through its own window instead, which is the same question asked portably. It defaults to off because a
  /// front end that misjudges idleness puts a black screen over a room that is only listening.
  /// </summary>
  [JsonPropertyName("screensaverMinutes")]
  public int ScreensaverMinutes { get; set; }

  /// <summary>
  /// Whether the front end asks WHO is watching whenever the screen wakes - the reference's login screen,
  /// which was an option a household turned on, not a turnstile. On, every start that has no remembered
  /// answer asks for a name; off - the shipped answer - nobody is asked, because the reference's living
  /// rooms didn't either.
  /// </summary>
  [JsonPropertyName("askForViewer")]
  public bool AskForViewer { get; set; }

  /// <summary>
  /// The name the watching was answered with, remembered so the next start doesn't ask again - the
  /// reference's auto-login user. Empty means ask; forgetting it is the answer to "ask again". Whoever
  /// is remembered owns the skin's answers from then on: the skin settings file carries their name,
  /// the way the reference's per-user settings did.
  /// </summary>
  [JsonPropertyName("rememberedViewer")]
  public string RememberedViewer { get; set; } = "";

  /// <summary>
  /// Whether the remembered person's door carries four numbers. The wake cannot ask an unreachable server
  /// about this - and a PIN the server cannot check is a door welded shut, while walking a numbered person
  /// in unchecked would be no door at all. So the machine remembers what it last knew: with this set, an
  /// unreachable server keeps the question up and says why nothing can be answered; without it, the PIN-less
  /// person's fast way in stays. Written whenever the name is written and the server was there to be asked.
  /// </summary>
  [JsonPropertyName("rememberedViewerHasPin")]
  public bool RememberedViewerHasPin { get; set; }

  private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
  {
    WriteIndented = true,
    ReadCommentHandling = JsonCommentHandling.Skip,
    AllowTrailingCommas = true
  };

  /// <summary>
  /// The three answers the language row walks, in the order the reference walked them: the machine's own
  /// first, then the two languages MP2 itself shipped translations for.
  /// </summary>
  public static readonly (string Code, string Label)[] LANGUAGES =
  {
    ("", "System default"),
    ("en-GB", "English"),
    ("de-DE", "Deutsch")
  };

  /// <summary>
  /// The culture dates and numbers speak, as this file answered: the named one when it exists on this
  /// machine, the operating system's own while nobody named one, and an unpronounceable name answered with
  /// the invariant calendar rather than an exception on the way to the television.
  /// </summary>
  public static CultureInfo UiCulture(string? cultureName)
  {
    if (string.IsNullOrWhiteSpace(cultureName))
      return CultureInfo.CurrentUICulture;

    try
    {
      return CultureInfo.GetCultureInfo(cultureName.Trim());
    }
    catch (CultureNotFoundException)
    {
      return CultureInfo.InvariantCulture;
    }
  }

  /// <summary>The answer of the file on disk right now - the one the screen and the window read.</summary>
  public static CultureInfo UiCulture()
  {
    return UiCulture(Load(null, out _).Culture);
  }

  /// <summary>
  /// Reads the client's answers. A missing or broken file comes back as the shipped defaults and a
  /// sentence, exactly like every other settings file here: a machine with an unreadable client.json is a
  /// machine that was never configured, not a machine that does not start.
  /// </summary>
  public static ClientSettings Load(string? path, out string? problem)
  {
    problem = null;
    string file = path ?? DefaultFilePath;

    try
    {
      if (!File.Exists(file))
        return new ClientSettings();

      string text = File.ReadAllText(file);
      if (string.IsNullOrWhiteSpace(text))
        return new ClientSettings();

      ClientSettings? read = JsonSerializer.Deserialize<ClientSettings>(text, JsonOptions);
      if (read is null)
      {
        problem = string.Format("{0} did not contain any settings.", file);
        return new ClientSettings();
      }

      read.ClientName ??= Environment.MachineName;
      read.StartScreen ??= START_HOME;
      read.LastShelf ??= "";
      read.Culture ??= "";
      // A negative timeout would trip the screen the moment it is armed, and a number past a day is not an
      // answer anybody meant; both are answered with "off", which is what nobody-configured means here.
      read.ScreensaverMinutes = read.ScreensaverMinutes is > 0 and <= 24 * 60 ? read.ScreensaverMinutes : 0;
      return read;
    }
    catch (Exception ex)
    {
      problem = string.Format("{0} could not be read ({1}: {2}); starting with the shipped answers.", file,
          ex.GetType().Name, ex.Message);
      return new ClientSettings();
    }
  }

  /// <summary>
  /// Writes the answers to the file they were read from. The sentence is for the status line: a settings
  /// screen whose saves are silent is a settings screen whose saves are doubted.
  /// </summary>
  public string Save(string? path, out bool saved)
  {
    string file = path ?? DefaultFilePath;
    try
    {
      string? folder = Path.GetDirectoryName(file);
      if (!string.IsNullOrEmpty(folder))
        Directory.CreateDirectory(folder);

      File.WriteAllText(file, JsonSerializer.Serialize(this, JsonOptions));
      saved = true;
      // The screens that listen hear about it, but a listener that stumbles over its own business -
      // a window mid-teardown, a reader with nothing to read - must never turn a save that happened
      // into a save that "could not write".
      foreach (EventHandler handler in Changed?.GetInvocationList() ?? Array.Empty<Delegate>())
      {
        try
        {
          ((EventHandler)handler)(this, EventArgs.Empty);
        }
        catch (Exception listenerProblem)
        {
          Console.WriteLine("client settings listener: " + listenerProblem.Message);
        }
      }
      return string.Format("Saved in {0}.", file);
    }
    catch (Exception ex)
    {
      saved = false;
      return string.Format("Could not write {0} ({1}: {2}).", file, ex.GetType().Name, ex.Message);
    }
  }

  /// <summary>
  /// Remembers which shelf the viewer last opened a whole of, for <see cref="START_LAST_SHELF"/>. Writing
  /// happens where shelves are actually opened - the application's own door to them - so every road to a
  /// shelf counts, from the tile, from the browse column or from the wake-up itself. An unchanged label
  /// writes nothing: opening the same shelf over and over is not a change to remember.
  /// </summary>
  public static void RecordShelf(string label)
  {
    if (string.IsNullOrWhiteSpace(label))
      return;

    ClientSettings settings = Load(null, out _);
    if (string.Equals(settings.LastShelf, label, StringComparison.OrdinalIgnoreCase))
      return;

    settings.LastShelf = label;
    settings.Save(null, out _);
  }
}
