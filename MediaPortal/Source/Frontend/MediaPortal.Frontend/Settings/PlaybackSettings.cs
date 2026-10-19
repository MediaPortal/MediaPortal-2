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
/// What the viewer told the player before it played: how far a skip reaches, how big a volume step is,
/// where the sound starts, which language to reach for when a media offers a choice, whether subtitles may
/// come on at all, and how long a picture stands before the slideshow moves on.
/// </summary>
/// <remarks>
/// <para>
/// The reference kept these under its playback settings; this class carries the ones this front end can
/// actually honour - the player behind it is libVLC, so the reference's DirectShow-only answers (audio
/// renderer device, hardware MPEG2, DVD paths) are not repeated here as promises nobody can keep, and the
/// reference's subtitle appearance (font, size, colour) waits until the renderer shows subtitles through
/// this front end rather than the engine's overlay.
/// </para>
/// <para>
/// One JSON file beside <c>client.json</c>, overridable with <c>MP2_PLAYBACK_FILE</c> for the same reason
/// as every other settings file here: gates and tests must not read or write what a real viewer set.
/// </para>
/// </remarks>
public sealed class PlaybackSettings
{
  private const string SETTINGS_FILE_NAME = "playback.json";

  /// <summary>Raised after any answer was written, so a watching screen can follow its own values.</summary>
  public static event EventHandler? Changed;

  /// <summary>
  /// Where the player's answers are saved. Overridable with <c>MP2_PLAYBACK_FILE</c>; otherwise the
  /// neighbour of the other settings files.
  /// </summary>
  public static string DefaultFilePath
  {
    get
    {
      string? fromEnvironment = Environment.GetEnvironmentVariable("MP2_PLAYBACK_FILE");
      if (!string.IsNullOrWhiteSpace(fromEnvironment))
        return fromEnvironment;

      string? folder = Path.GetDirectoryName(ServerSettings.DefaultFilePath);
      if (string.IsNullOrEmpty(folder))
        folder = Path.GetTempPath();
      return Path.Combine(folder, SETTINGS_FILE_NAME);
    }
  }

  /// <summary>How far one press of forward or back reaches, in seconds.</summary>
  [JsonPropertyName("skipStepSeconds")]
  public int SkipStepSeconds { get; set; } = 30;

  /// <summary>How much one press of the volume key adds or takes away.</summary>
  [JsonPropertyName("volumeStep")]
  public int VolumeStep { get; set; } = 5;

  ///
  /// The volume a media starts at, when this front end starts to play. 0-100.
  /// </summary>
  /// <remarks>
  /// Applied on the way INTO a media, not during one: a viewer who turned the sound down mid-film turned
  /// it down on purpose, and a setting that overruled them at the next file would be the setting that
  /// learned nothing. It is applied once per media, and only by the front end's own start.
  /// </remarks>
  [JsonPropertyName("initialVolume")]
  public int InitialVolume { get; set; } = 100;

  /// <summary>Whether subtitles may come on by themselves at all.</summary>
  [JsonPropertyName("subtitlesEnabled")]
  public bool SubtitlesEnabled { get; set; } = true;

  /// <summary>
  /// The language to reach for among several audio tracks, as a two-or-three-letter code; empty means the
  /// media's own order. It is a preference, honoured only when a track says that language - a file with
  /// nothing labelled says nothing, and the media's default stands.
  /// </summary>
  [JsonPropertyName("audioLanguage")]
  public string AudioLanguage { get; set; } = "";

  /// <summary>The language to reach for among several subtitle tracks; empty means the media's own order.</summary>
  [JsonPropertyName("subtitleLanguage")]
  public string SubtitleLanguage { get; set; } = "";

  /// <summary>How long one picture stands during a slideshow, in seconds.</summary>
  [JsonPropertyName("slideshowSeconds")]
  public int SlideshowSeconds { get; set; } = 7;

  /// <summary>The answers the skip row walks, in the reference's own spread of the same choice.</summary>
  public static readonly int[] SKIP_STEPS = { 5, 10, 30, 60, 120, 300 };

  /// <summary>The answers the volume-step row walks.</summary>
  public static readonly int[] VOLUME_STEPS = { 1, 2, 5, 10, 20 };

  /// <summary>The answers the slideshow row walks.</summary>
  public static readonly int[] SLIDESHOW_SECONDS = { 3, 5, 7, 10, 15, 30, 60 };

  /// <summary>
  /// The languages the track rows walk. The codes are the ones containers write (ISO 639-2/B), because
  /// that is what <see cref="Player.PlaybackTrack.Language"/> carries when a file says anything; the empty
  /// first answer hands the choice back to the media.
  /// </summary>
  public static readonly (string Code, string Label)[] TRACK_LANGUAGES =
  {
    ("", "The media decides"),
    ("eng", "English"),
    ("deu", "Deutsch"),
    ("nld", "Nederlands"),
    ("fra", "Français"),
    ("spa", "Español"),
    ("ita", "Italiano"),
    ("pol", "Polski")
  };

  private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
  {
    WriteIndented = true,
    ReadCommentHandling = JsonCommentHandling.Skip,
    AllowTrailingCommas = true
  };

  /// <summary>
  /// Reads the player's answers. A missing or broken file comes back as the shipped defaults and a
  /// sentence, exactly like every other settings file here. Values a broken or hand-edited file left out
  /// of range are clamped on the way in, because a skip of a thousand hours is not an answer anybody
  /// meant, and the player must never be handed nonsense.
  /// </summary>
  public static PlaybackSettings Load(string? path, out string? problem)
  {
    problem = null;
    string file = path ?? DefaultFilePath;

    try
    {
      if (!File.Exists(file))
        return new PlaybackSettings();

      string text = File.ReadAllText(file);
      if (string.IsNullOrWhiteSpace(text))
        return new PlaybackSettings();

      PlaybackSettings? read = JsonSerializer.Deserialize<PlaybackSettings>(text, JsonOptions);
      if (read is null)
      {
        problem = string.Format("{0} did not contain any settings.", file);
        return new PlaybackSettings();
      }

      read.SkipStepSeconds = Clamp(read.SkipStepSeconds, 1, 3600, 30);
      read.VolumeStep = Clamp(read.VolumeStep, 1, 50, 5);
      read.InitialVolume = Clamp(read.InitialVolume, 0, 100, 100);
      read.SlideshowSeconds = Clamp(read.SlideshowSeconds, 1, 600, 7);
      read.AudioLanguage ??= "";
      read.SubtitleLanguage ??= "";
      return read;
    }
    catch (Exception ex)
    {
      problem = string.Format("{0} could not be read ({1}: {2}); starting with the shipped answers.", file,
          ex.GetType().Name, ex.Message);
      return new PlaybackSettings();
    }
  }

  /// <summary>
  /// Writes the answers to the file they were read from. The sentence is for the status line, like
  /// everywhere else: silent saves are doubted saves.
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
      Changed?.Invoke(this, EventArgs.Empty);
      return string.Format("Saved in {0}.", file);
    }
    catch (Exception ex)
    {
      saved = false;
      return string.Format("Could not write {0} ({1}: {2}).", file, ex.GetType().Name, ex.Message);
    }
  }

  private static int Clamp(int value, int low, int high, int whenNonsense)
  {
    if (value < low || value > high)
      return whenNonsense;
    return value;
  }
}
