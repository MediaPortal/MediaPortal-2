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

namespace MediaPortal.Frontend.Settings;

/// <summary>
/// What shape each media screen wears, remembered per screen: whether its titles are walked as rows, tiles
/// or covers, and whether the selected title's details stand beside them.
/// </summary>
/// <remarks>
/// <para>
/// The reference kept these choices in the worn skin's settings - a grid type per media kind, one flag for
/// the details view - and they were therefore only answerable while a skin with those settings was worn.
/// This front end asks them on the screen itself, on every screen whatever it wears, and a screen remembers
/// the answer its viewer gave it: the films may be walked as covers while the episodes stay rows, and both
/// come back that way the next evening. The key is the server's own aspect name, which is what a screen is.
/// </para>
/// <para>
/// A file beside <c>frontend.json</c> on ThemeSettings' reasoning that the server address stays small and
/// boringly stable. A missing or broken file is not a fault but an answer of "never asked", which leaves
/// every screen to open in the shape it is written in.
/// </para>
/// </remarks>
public sealed class ScreenViewSettings
{
  private const string SETTINGS_FILE_NAME = "screens.json";

  /// <summary>
  /// Where the per-screen shapes are saved, beside the server settings; overridable with
  /// <c>MP2_SCREEN_VIEWS_FILE</c> so that tests and gates never touch what a real viewer picked.
  /// </summary>
  public static string DefaultFilePath
  {
    get
    {
      string? fromEnvironment = Environment.GetEnvironmentVariable("MP2_SCREEN_VIEWS_FILE");
      if (!string.IsNullOrWhiteSpace(fromEnvironment))
        return fromEnvironment;

      string serverFile = ServerSettings.DefaultFilePath;
      string? folder = Path.GetDirectoryName(serverFile);
      if (string.IsNullOrEmpty(folder))
        folder = Path.GetTempPath();
      return Path.Combine(folder, SETTINGS_FILE_NAME);
    }
  }

  /// <summary>Chosen layout per aspect name, spelled "Grid", "List" or "Cover".</summary>
  [JsonPropertyName("layouts")]
  public Dictionary<string, string> Layouts { get; set; } =
      new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

  /// <summary>Whether the details stand beside the shelf, per aspect name.</summary>
  [JsonPropertyName("details")]
  public Dictionary<string, bool> Details { get; set; } =
      new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

  /// <summary>Chosen tile picture per aspect name, spelled "Poster", "Banner" or "Thumbnail".</summary>
  [JsonPropertyName("media")]
  public Dictionary<string, string> Media { get; set; } =
      new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

  /// <summary>Whether this screen was ever answered, which is the question the defaults hide behind.</summary>
  public bool HasLayout(string aspect)
  {
    return aspect != null && Layouts.ContainsKey(aspect);
  }

  /// <summary>Whether the viewer ever picked the tile picture on this screen.</summary>
  public bool HasMedia(string aspect)
  {
    return aspect != null && Media.ContainsKey(aspect);
  }

  /// <summary>Whether the viewer ever answered the details question for this screen.</summary>
  public bool HasDetails(string aspect)
  {
    return aspect != null && Details.ContainsKey(aspect);
  }

  /// <summary>
  /// Reads the remembered shapes. A missing or broken file comes back empty with a sentence, the way every
  /// other local memory reads: nobody is stopped by their own settings being unreadable.
  /// </summary>
  public static ScreenViewSettings Load(string? path, out string? problem)
  {
    problem = null;
    string file = path ?? DefaultFilePath;

    try
    {
      if (!File.Exists(file))
        return new ScreenViewSettings();

      string text = File.ReadAllText(file);
      if (string.IsNullOrWhiteSpace(text))
        return new ScreenViewSettings();

      ScreenViewSettings? read = JsonSerializer.Deserialize<ScreenViewSettings>(text, JsonOptions);
      if (read is null)
      {
        problem = string.Format("{0} did not contain any screen shapes.", file);
        return new ScreenViewSettings();
      }

      read.Layouts ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
      read.Details ??= new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
      read.Media ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
      return read;
    }
    catch (Exception ex)
    {
      problem = string.Format("{0} could not be read ({1}: {2}); screens open in their written shapes.", file,
          ex.GetType().Name, ex.Message);
      return new ScreenViewSettings();
    }
  }

  /// <summary>
  /// Writes the shapes. Returns the sentence for the log instead of throwing, as everywhere a button press
  /// is the caller - a screen that remembers its view on a stick where it cannot write still shows its view.
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
