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
/// One place the weather is watched: the name the viewer picked it under, what the geocoder said around it, and
/// the coordinates it is read by.
/// </summary>
public sealed class WeatherPlaceEntry
{
  [JsonPropertyName("name")]
  public string Name { get; set; } = "";

  /// <summary>The state and country that tell two cities of the same name apart. May be empty.</summary>
  [JsonPropertyName("detail")]
  public string Detail { get; set; } = "";

  [JsonPropertyName("lat")]
  public double Lat { get; set; }

  [JsonPropertyName("lon")]
  public double Lon { get; set; }
}

/// <summary>
/// The weather screen's two pieces of memory: the places the viewer is watching, and - when they registered one -
/// their own OpenWeatherMap key.
/// </summary>
/// <remarks>
/// <para>
/// A separate file next to <c>frontend.json</c> rather than fields inside it: <see cref="ServerSettings"/> is the
/// address of the server, the one decision the front end cannot run without, and it should stay small and
/// boringly stable. The weather places are a list that grows and shrinks on the couch, and a corruption story
/// about somebody's adopted cities should not be able to touch the server address. Same folder, same forgiving
/// read, same replace-the-file write, for the reasons spelled out on <see cref="ServerSettings"/>.
/// </para>
/// <para>
/// An empty list is not an error - it is a television that has never been told where it stands, and the weather
/// screen says so and offers its search box.
/// </para>
/// </remarks>
public sealed class WeatherPlacesSettings
{
  private const string SETTINGS_FILE_NAME = "weather.json";

  /// <summary>
  /// Where the places are saved. Overridable with <c>MP2_WEATHER_FILE</c> for the same reason the server settings
  /// are overridable with <c>MP2_SETTINGS_FILE</c>: the gates and the tests must not read or write what a real
  /// viewer saved.
  /// </summary>
  public static string DefaultFilePath
  {
    get
    {
      string? fromEnvironment = Environment.GetEnvironmentVariable("MP2_WEATHER_FILE");
      if (!string.IsNullOrWhiteSpace(fromEnvironment))
        return fromEnvironment;

      // The ServerSettings path is the folder's definition; the weather file is its neighbour. Deriving from it
      // keeps the environment override honest too: point MP2_SETTINGS_FILE at a temporary file and the weather
      // file lands beside it, which is what a test wants.
      string serverFile = ServerSettings.DefaultFilePath;
      string? folder = Path.GetDirectoryName(serverFile);
      if (string.IsNullOrEmpty(folder))
        folder = Path.GetTempPath();
      return Path.Combine(folder, SETTINGS_FILE_NAME);
    }
  }

  /// <summary>
  /// The watched places, in the order the viewer added them - which is the order the screen draws them,
  /// because the first one is "home" and the viewer chose which that was.
  /// </summary>
  [JsonPropertyName("places")]
  public List<WeatherPlaceEntry> Places { get; set; } = new List<WeatherPlaceEntry>();

  /// <summary>
  /// The viewer's own API key, tried before the product's shared demo keys. Empty by default; the escape hatch
  /// the old plugin offered to people who hit the shared keys' limits.
  /// </summary>
  [JsonPropertyName("api_key")]
  public string ApiKey { get; set; } = "";

  /// <summary>
  /// Reads the places. A missing or broken file comes back as an empty list and a sentence, exactly like the
  /// server settings: the weather screen opens either way.
  /// </summary>
  public static WeatherPlacesSettings Load(string? path, out string? problem)
  {
    problem = null;
    string file = path ?? DefaultFilePath;

    try
    {
      if (!File.Exists(file))
        return new WeatherPlacesSettings();

      string text = File.ReadAllText(file);
      if (string.IsNullOrWhiteSpace(text))
        return new WeatherPlacesSettings();

      WeatherPlacesSettings? read = JsonSerializer.Deserialize<WeatherPlacesSettings>(text, JsonOptions);
      if (read is null)
      {
        problem = string.Format("{0} did not contain any places.", file);
        return new WeatherPlacesSettings();
      }
      read.Places ??= new List<WeatherPlaceEntry>();
      return read;
    }
    catch (Exception ex)
    {
      problem = string.Format("{0} could not be read ({1}: {2}); starting without saved places.", file,
          ex.GetType().Name, ex.Message);
      return new WeatherPlacesSettings();
    }
  }

  /// <summary>
  /// Writes the places. Returns the sentence for the status line instead of throwing, as everywhere a button is
  /// the caller.
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
