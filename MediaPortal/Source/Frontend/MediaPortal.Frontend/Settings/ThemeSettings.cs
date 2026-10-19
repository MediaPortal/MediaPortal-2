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
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MediaPortal.Frontend.Settings;

/// <summary>
/// The theme the viewer picked from the settings screen, and nothing else - a theme is one word.
/// </summary>
/// <remarks>
/// <para>
/// A separate file beside <c>frontend.json</c>, on WeatherPlacesSettings' reasoning that the server address
/// should stay small and boringly stable. The picker writes it, and the startup precedence chain reads it after
/// the command line and the environment, so that what a viewer chose on the screen outlives the run that chose
/// it without ever quietly overriding a <c>--theme</c> that somebody typed on purpose.
/// </para>
/// <para>
/// The file stores the name as it was picked; whether that name still means anything is <c>FrontendTheme</c>'s
/// question at apply time, not this file's. An empty name - a missing or blank file - means the viewer never
/// chose, which is not the same as choosing the default palette.
/// </para>
/// </remarks>
public sealed class ThemeSettings
{
  private const string SETTINGS_FILE_NAME = "theme.json";

  /// <summary>
  /// Where the chosen theme is saved, beside the server settings; overridable with <c>MP2_THEME_FILE</c> so
  /// that tests and gates never touch what a real viewer picked.
  /// </summary>
  public static string DefaultFilePath
  {
    get
    {
      string? fromEnvironment = Environment.GetEnvironmentVariable("MP2_THEME_FILE");
      if (!string.IsNullOrWhiteSpace(fromEnvironment))
        return fromEnvironment;

      string serverFile = ServerSettings.DefaultFilePath;
      string? folder = Path.GetDirectoryName(serverFile);
      if (string.IsNullOrEmpty(folder))
        folder = Path.GetTempPath();
      return Path.Combine(folder, SETTINGS_FILE_NAME);
    }
  }

  /// <summary>
  /// The picked theme's name, or empty when the viewer never picked one.
  /// </summary>
  [JsonPropertyName("theme")]
  public string Theme { get; set; } = "";

  /// <summary>
  /// Reads the picked name. A missing or broken file comes back empty with a sentence, the way every other
  /// local memory reads: nobody is stopped by their own settings being unreadable.
  /// </summary>
  public static ThemeSettings Load(string? path, out string? problem)
  {
    problem = null;
    string file = path ?? DefaultFilePath;

    try
    {
      if (!File.Exists(file))
        return new ThemeSettings();

      string text = File.ReadAllText(file);
      if (string.IsNullOrWhiteSpace(text))
        return new ThemeSettings();

      ThemeSettings? read = JsonSerializer.Deserialize<ThemeSettings>(text, JsonOptions);
      if (read is null)
      {
        problem = string.Format("{0} did not contain a theme.", file);
        return new ThemeSettings();
      }
      read.Theme ??= "";
      return read;
    }
    catch (Exception ex)
    {
      problem = string.Format("{0} could not be read ({1}: {2}); starting without a saved theme.", file,
          ex.GetType().Name, ex.Message);
      return new ThemeSettings();
    }
  }

  /// <summary>
  /// Writes the picked name. Returns the sentence for the status line instead of throwing, as everywhere a
  /// button is the caller.
  /// </summary>
  public string Save(string? path, out bool saved)
  {
    string file = path ?? DefaultFilePath;
    try
    {
      string? folder = Path.GetDirectoryName(file);
      if (!string.IsNullOrEmpty(folder))
        Directory.CreateDirectory(folder);

      string temporary = file + ".new";
      File.WriteAllText(temporary, JsonSerializer.Serialize(this, JsonOptions));
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

  private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
  {
    WriteIndented = true,
    ReadCommentHandling = JsonCommentHandling.Skip,
    AllowTrailingCommas = true
  };
}
