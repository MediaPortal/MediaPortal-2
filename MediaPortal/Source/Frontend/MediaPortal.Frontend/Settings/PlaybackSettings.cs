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

  /// <summary>
  /// How far an instant skip reaches, as a percentage of the title's length - the reference's
  /// <c>InstantSkipPercent</c> and its shipped twenty. The reference's default way of jumping: percent of
  /// the whole film, so one press covers the same stretch of a short film and a long one.
  /// </summary>
  [JsonPropertyName("instantSkipPercent")]
  public int InstantSkipPercent { get; set; } = 20;

  /// <summary>
  /// The other way to jump: a fixed number of seconds, the reference's <c>InstantSkipSeconds</c> and its
  /// shipped five. Asked for when <see cref="PreferInstantSkipSeconds"/> says so, and used as the answer
  /// when a stream has no length to take a percentage of.
  /// </summary>
  [JsonPropertyName("instantSkipSeconds")]
  public int InstantSkipSeconds { get; set; } = 5;

  /// <summary>Whether the instant skip counts in seconds instead of per cent - the reference's
  /// <c>PreferInstantSkipSeconds</c>, shipped off because it prefers the percentage.</summary>
  [JsonPropertyName("preferInstantSkipSeconds")]
  public bool PreferInstantSkipSeconds { get; set; }

  /// <summary>
  /// The ladder the left and right keys climb when they are pressed one after the other: comma-separated
  /// seconds, exactly like the reference's <c>SkipStepList</c>, and the same answer it ships. A press held
  /// in the same direction climbs one rung of this list, so four quick presses reach four minutes rather
  /// than four times fifteen seconds. Editing this by hand is the intended way; the ladder ignores pieces
  /// that are not positive whole numbers.
  /// </summary>
  [JsonPropertyName("skipStepList")]
  public string SkipStepList { get; set; } = DEFAULT_SKIP_STEP_LIST;

  /// <summary>
  /// How long the player waits after a skip-step press before it commits the jump - the reference's
  /// <c>SkipStepTimeout</c> and the same 1.5 seconds it ships. Long enough to press again and climb,
  /// short enough that a single press feels like a single press.
  /// </summary>
  [JsonPropertyName("skipStepTimeoutSeconds")]
  public double SkipStepTimeoutSeconds { get; set; } = 1.5;

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

  /// <summary>
  /// How much of a title has to be played before it counts as watched - the reference's "Watched play
  /// percentage" (<c>PlayerManagerSettings.WatchedPlayPercentage</c>), and the reference's own answer of
  /// ninety is the shipped one. The rule it serves is the reference's: <i>watched is a decision, not the
  /// end of the file</i> - whoever reports a viewing turns it into the hundred the library door
  /// understands once this much has been seen (see <see cref="ViewModels.NowPlayingViewModel"/>).
  /// </summary>
  [JsonPropertyName("watchedMarkPercentage")]
  public int WatchedMarkPercentage { get; set; } = 90;

  /// <summary>
  /// Whether the playback screen closes itself when the film ends - the reference's "close players when
  /// finished", and the reference shipped it on (<c>MediaModelSettings.DEFAULT_CLOSE_PLAYER_WHEN_FINISHED</c>
  /// is true, and the player context closed when its queue ran out). Only the engine's own ending counts; a
  /// stopped or watched-mark-raised viewing keeps its screen.
  /// </summary>
  [JsonPropertyName("closePlayerWhenFinished")]
  public bool ClosePlayerWhenFinished { get; set; } = true;

  /// <summary>The answers the watched-mark row walks. The reference offered the whole stretch of nought
  /// to a hundred as a number; a remote walks a ladder, and the file still takes any whole per cent.</summary>
  public static readonly int[] WATCHED_MARKS = { 50, 60, 70, 80, 90, 100 };

  /// <summary>
  /// Whether a slideshow moves the pictures it shows - the slow tenth-of-a-step zoom into a corner or the
  /// drift between two points that the reference called the Ken Burns effect. The reference shipped it on
  /// (<c>ImagePlayerSettings.UseKenBurns</c>, default true), and so does this.
  /// </summary>
  [JsonPropertyName("kenBurnsEffect")]
  public bool KenBurnsEffect { get; set; } = true;

  /// <summary>Ladder for the instant-skip row, in per cent; the reference's own twenty is on it.</summary>
  public static readonly int[] INSTANT_SKIP_PERCENTS = { 5, 10, 20, 25, 50 };

  /// <summary>Ladder for the instant-skip row when it counts in seconds; five is the reference's answer.</summary>
  public static readonly int[] INSTANT_SKIP_SECONDS = { 5, 10, 15, 30, 60 };

  /// <summary>The reference's escalating skip ladder, verbatim.</summary>
  public const string DEFAULT_SKIP_STEP_LIST = "15,30,60,180,300,600,900,1800,3600,7200";

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

      read.InstantSkipPercent = Clamp(read.InstantSkipPercent, 1, 100, 20);
      read.InstantSkipSeconds = Clamp(read.InstantSkipSeconds, 1, 3600, 5);
      read.VolumeStep = Clamp(read.VolumeStep, 1, 50, 5);
      read.InitialVolume = Clamp(read.InitialVolume, 0, 100, 100);
      read.SlideshowSeconds = Clamp(read.SlideshowSeconds, 1, 600, 7);
      // The whole per cent stretch 0..100 is an answer somebody can mean - nought means "watched the
      // moment anything is reported", which the reference allowed too; only nonsense outside it falls
      // back to the reference's own ninety.
      read.WatchedMarkPercentage = Clamp(read.WatchedMarkPercentage, 0, 100, 90);
      // A wait under a fifth of a second would commit the jump before anybody could press again, and one
      // over ten seconds would feel like the film had stopped listening; the reference's 1.5 answers both.
      if (read.SkipStepTimeoutSeconds < 0.2 || read.SkipStepTimeoutSeconds > 10 ||
          double.IsNaN(read.SkipStepTimeoutSeconds))
      {
        read.SkipStepTimeoutSeconds = 1.5;
      }
      read.SkipStepList = string.IsNullOrWhiteSpace(read.SkipStepList) ? DEFAULT_SKIP_STEP_LIST : read.SkipStepList;
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
