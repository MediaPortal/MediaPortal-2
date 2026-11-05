/*
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by the Free Software
    Foundation, either version 3 of the License, or (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General
    Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.Services;

namespace MediaPortal.Frontend.Settings;

/// <summary>
/// One line of the shutdown menu: which way out it is, and whether the viewer wants to see it. The
/// reference kept exactly this pair in its <c>SystemStateItem</c> - a line the dialog shows and the
/// configuration dialog's checkbox hides.
/// </summary>
public sealed class ShutdownMenuItem
{
  [JsonPropertyName("action")]
  public ShutdownAction Action { get; set; }

  [JsonPropertyName("visible")]
  public bool Visible { get; set; } = true;
}

/// <summary>
/// The shape of the shutdown menu, as the reference kept it in <c>SystemStateDialogSettings</c>: its
/// <c>ShutdownItemList</c> - the ways out in the order the operator left them, each with the flag the
/// configuration dialog's checkbox wrote - and its <c>MaxSleepTimeout</c>, the ceiling the sleep timer's
/// dialog may not cross. The reference kept both in client settings under the user scope; this front end
/// keeps its own file beside the weather places and the news feeds, because the answers are the front
/// end's own menu, not a switch on the server.
/// </summary>
public sealed class ShutdownMenuSettings
{
  private const string SETTINGS_FILE_NAME = "shutdown-menu.json";

  /// <summary>The reference's ceiling for a wait (<c>[Setting(SettingScope.User, 300)]</c>).</summary>
  public const int DEFAULT_MAX_SLEEP_TIMEOUT = 300;

  /// <summary>
  /// Where the file is: <c>MP2_SHUTDOWN_MENU_FILE</c> when said, otherwise the neighbour of the client
  /// settings - which itself follows <c>MP2_SETTINGS_FILE</c>, so the test host's single isolation
  /// variable isolates this file too.
  /// </summary>
  public static string DefaultFilePath
  {
    get
    {
      string? fromEnvironment = Environment.GetEnvironmentVariable("MP2_SHUTDOWN_MENU_FILE");
      if (!string.IsNullOrWhiteSpace(fromEnvironment))
        return fromEnvironment;

      string? folder = Path.GetDirectoryName(ServerSettings.DefaultFilePath);
      if (string.IsNullOrEmpty(folder))
        folder = Path.GetTempPath();
      return Path.Combine(folder, SETTINGS_FILE_NAME);
    }
  }

  /// <summary>
  /// The ways out, in the order the menu shows them, each with the flag that hides it. The reference's
  /// <c>CreateDefaultShutdownMenu</c> order, all visible - the two lines it hid by default, minimize and
  /// log off, are ways this front end does not offer at all, so they are not even hidden here.
  /// </summary>
  [JsonPropertyName("items")]
  public List<ShutdownMenuItem> Items { get; set; } = CreateDefaultMenu();

  /// <summary>
  /// How long a sleep timer may wait at most, in minutes; the reference's <c>MaxSleepTimeout</c>, which
  /// its configuration dialog wrote and its sleep timer dialog obeyed.
  /// </summary>
  [JsonPropertyName("maxSleepTimeout")]
  public int MaxSleepTimeout { get; set; } = DEFAULT_MAX_SLEEP_TIMEOUT;

  /// <summary>The reference's default menu, in the reference's order.</summary>
  public static List<ShutdownMenuItem> CreateDefaultMenu()
  {
    return new List<ShutdownMenuItem>
    {
        new ShutdownMenuItem { Action = ShutdownAction.Suspend },
        new ShutdownMenuItem { Action = ShutdownAction.Hibernate },
        new ShutdownMenuItem { Action = ShutdownAction.Shutdown },
        new ShutdownMenuItem { Action = ShutdownAction.SleepTimer },
        new ShutdownMenuItem { Action = ShutdownAction.Restart },
        new ShutdownMenuItem { Action = ShutdownAction.CloseMP },
        new ShutdownMenuItem { Action = ShutdownAction.RestartMP }
    };
  }

  private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
  {
    WriteIndented = true,
    ReadCommentHandling = JsonCommentHandling.Skip,
    AllowTrailingCommas = true,
    // The ways out are written as their names, so a viewer with a text editor can read and fix the
    // file; numbers stay readable on the way in, the converter takes both.
    Converters = { new JsonStringEnumConverter() }
  };

  /// <summary>
  /// Reads the file; a missing or broken one answers with the reference's default menu and a sentence.
  /// A file that knows fewer lines than this build does is merged, not overwritten - the reference's
  /// <c>GetShutdownActionsFromSettings</c> hung every action it knew and had never seen on the end of a
  /// list its operator had left, so an update never loses a way out or moves one the operator placed.
  /// </summary>
  public static ShutdownMenuSettings Load(string? path, out string? problem)
  {
    problem = null;
    string file = path ?? DefaultFilePath;
    ShutdownMenuSettings settings = new ShutdownMenuSettings();

    try
    {
      if (!File.Exists(file))
        return settings;

      string text = File.ReadAllText(file);
      if (string.IsNullOrWhiteSpace(text))
        return settings;

      ShutdownMenuSettings? read = JsonSerializer.Deserialize<ShutdownMenuSettings>(text, JsonOptions);
      if (read is null)
      {
        problem = string.Format("{0} did not contain any menu settings.", file);
        return settings;
      }

      settings.MaxSleepTimeout = read.MaxSleepTimeout > 0
          ? read.MaxSleepTimeout
          : DEFAULT_MAX_SLEEP_TIMEOUT;
      settings.Items = MergeWithDefaults(read.Items);
      return settings;
    }
    catch (Exception ex)
    {
      problem = string.Format("{0} could not be read ({1}: {2}); showing the default shutdown menu.", file,
          ex.GetType().Name, ex.Message);
      return new ShutdownMenuSettings();
    }
  }

  /// <summary>
  /// Keeping the file's order and hiding, dropping lines this build does not have (an enum value nobody
  /// offers is a promise nobody can keep), and hanging every way out the file forgot - in the default
  /// order - behind the ones it kept.
  /// </summary>
  private static List<ShutdownMenuItem> MergeWithDefaults(List<ShutdownMenuItem>? read)
  {
    List<ShutdownMenuItem> merged = new List<ShutdownMenuItem>();
    HashSet<ShutdownAction> known = new HashSet<ShutdownAction>();

    if (read != null)
    {
      foreach (ShutdownMenuItem item in read)
      {
        if (!Enum.IsDefined(item.Action) || !known.Add(item.Action))
          continue;
        merged.Add(item);
      }
    }

    foreach (ShutdownMenuItem fallback in CreateDefaultMenu())
    {
      if (!known.Contains(fallback.Action))
        merged.Add(fallback);
    }

    return merged;
  }

  /// <summary>Writes the answers; the sentence is for the status line, as everywhere else.</summary>
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
      return string.Format("Saved in {0}.", file);
    }
    catch (Exception ex)
    {
      saved = false;
      return string.Format("Could not write {0} ({1}: {2}).", file, ex.GetType().Name, ex.Message);
    }
  }
}
