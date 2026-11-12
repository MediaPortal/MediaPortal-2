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
/// What each media screen wears and how each one is read, remembered per screen: whether its titles are walked
/// as rows, tiles or covers, which picture a tile wears, and - the four the reference kept beside them - the
/// order the shelf is read in, which end it starts at, what it is grouped under, and which value of which
/// category it was cut to.
/// </summary>
/// <remarks>
/// <para>
/// The reference kept these choices in the worn skin's settings - a grid type per media kind, one flag for the
/// details view - and they were therefore only answerable while a skin with those settings was worn. This front
/// end asks them on the screen itself, on every screen whatever it wears, and a screen remembers the answer its
/// viewer gave it: the films may be walked as covers while the episodes stay rows, and both come back that way
/// the next evening.
/// </para>
/// <para>
/// <b>Two keys, because the reference had two.</b> The order, the grouping, the shape and the picture belong to
/// a <em>screen</em>, and a screen is one media kind seen through one filter class: the reference wrote them
/// into <c>ViewSettings.ScreenConfigs[screen class]</c>, which is why its "films by genres" and its "films by
/// years" could each be read in their own order. The other half - <em>which</em> filter the viewer last stood
/// on - belongs to the media kind alone, which is what the reference kept in <c>ViewSettings.ScreenHierarchy</c>
/// and what decides the screen a shelf opens on. This front end adds the value to that second half, because its
/// filters carry one: <c>genre</c> alone is a question, <c>genre</c> with <c>Action</c> is the shelf somebody
/// was looking at, and a category with no value is a shelf no server will answer.
/// </para>
/// <para>
/// A file beside <c>frontend.json</c> on ThemeSettings' reasoning that the server address stays small and
/// boringly stable, named after the signed-in person the way every skin file is - two viewers of one household
/// read their films in two orders. A missing or broken file is not a fault but an answer of "never asked",
/// which leaves every screen to open in the shape it is written in.
/// </para>
/// </remarks>
public sealed class ScreenViewSettings
{
  private const string SETTINGS_FILE_NAME = "screens.json";
  private const string SETTINGS_FILE_STEM = "screens";

  /// <summary>
  /// The filter class of a shelf that is read whole, which is the class of every screen a shelf opens on
  /// before a filter was ever said to it.
  /// </summary>
  public const string NO_FILTER = "";

  /// <summary>
  /// Where one screen's answers are kept. The key is the screen: the server's own aspect name and the class of
  /// the filter it is seen through, joined by a <c>#</c> - <c>MovieItem</c> for the whole shelf of films,
  /// <c>MovieItem#genre</c> for the films under one genre.
  /// </summary>
  [JsonPropertyName("screens")]
  public Dictionary<string, ScreenAnswer> Screens { get; set; } =
      new Dictionary<string, ScreenAnswer>(StringComparer.OrdinalIgnoreCase);

  /// <summary>
  /// Which filter a media kind's screen was last standing on, per aspect name. This is the reference's screen
  /// hierarchy: it does not describe the shelf, it says which shelf to open.
  /// </summary>
  [JsonPropertyName("landings")]
  public Dictionary<string, ScreenLanding> Landings { get; set; } =
      new Dictionary<string, ScreenLanding>(StringComparer.OrdinalIgnoreCase);

  /// <summary>The key one screen's answers stand under.</summary>
  public static string KeyFor(string? aspect, string? filterClass)
  {
    return (aspect ?? "").Trim() + "#" + (filterClass ?? NO_FILTER).Trim().ToLowerInvariant();
  }

  /// <summary>
  /// The answers one screen holds, or a new empty set put into the file for the caller to write on. Every row
  /// the menu offers writes through this, so a screen gains its entry the moment its viewer answers something on
  /// it rather than when the file is closed.
  /// </summary>
  public ScreenAnswer AnswerFor(string? aspect, string? filterClass)
  {
    string key = KeyFor(aspect, filterClass);
    if (!Screens.TryGetValue(key, out ScreenAnswer? answer) || answer == null)
    {
      answer = new ScreenAnswer();
      Screens[key] = answer;
    }
    return answer;
  }

  /// <summary>The answers one screen holds, or null when its viewer never answered anything there.</summary>
  public bool TryAnswer(string? aspect, string? filterClass, out ScreenAnswer answer)
  {
    bool known = Screens.TryGetValue(KeyFor(aspect, filterClass), out ScreenAnswer? found)
        && found != null;
    answer = found ?? new ScreenAnswer();
    return known;
  }

  /// <summary>The filter this media kind's screen was last standing on, or null when it was last whole.</summary>
  public ScreenLanding? LandingFor(string? aspect)
  {
    string name = (aspect ?? "").Trim();
    if (name.Length == 0 || !Landings.TryGetValue(name, out ScreenLanding? landing) || landing == null)
      return null;

    return string.IsNullOrWhiteSpace(landing.By) || string.IsNullOrWhiteSpace(landing.Value)
        ? null
        : landing;
  }

  /// <summary>
  /// Says which filter a media kind's screen opens on. Both halves together or nothing at all: a category
  /// whose value went missing is not a shelf anybody can read, and the server says so in as many words.
  /// </summary>
  public void RememberLanding(string? aspect, string? by, string? value)
  {
    string name = (aspect ?? "").Trim();
    if (name.Length == 0)
      return;

    if (string.IsNullOrWhiteSpace(by) || string.IsNullOrWhiteSpace(value))
      Landings.Remove(name);
    else
      Landings[name] = new ScreenLanding { By = by.Trim(), Value = value.Trim() };
  }

  /// <summary>
  /// The file the memories are read from for the person standing in front of the machine: their own file once
  /// they have one, the shared file while it is still the only answer this machine has - a viewer inherits what
  /// the household was told, and their first save leaves their own file behind - and their own missing file
  /// when nobody ever answered anything. Exactly how every skin file reads.
  /// </summary>
  public static string ReadFilePath(string? path)
  {
    string own = WriteFilePath(path);
    if (!string.IsNullOrWhiteSpace(path) || string.IsNullOrEmpty(SkinSettingsBase.FileOwner))
      return own;

    string shared = Path.Combine(SettingsFolder(), SETTINGS_FILE_NAME);
    return File.Exists(own) || !File.Exists(shared) ? own : shared;
  }

  /// <summary>
  /// The file the memories are written to: the named file, or the signed-in person's own, or the shared one
  /// while nobody is signed in.
  /// </summary>
  public static string WriteFilePath(string? path)
  {
    if (!string.IsNullOrWhiteSpace(path))
      return path;

    string? from_environment = Environment.GetEnvironmentVariable("MP2_SCREEN_VIEWS_FILE");
    if (!string.IsNullOrWhiteSpace(from_environment))
      return from_environment;

    string owner = (SkinSettingsBase.FileOwner ?? "").Trim();
    if (owner.Length == 0)
      return Path.Combine(SettingsFolder(), SETTINGS_FILE_NAME);

    foreach (char bad in Path.GetInvalidFileNameChars())
      owner = owner.Replace(bad, '-');

    return Path.Combine(SettingsFolder(),
        SETTINGS_FILE_STEM + "-" + owner.ToLowerInvariant() + ".json");
  }

  /// <summary>Where the files stand: the server settings' folder, or the temporary one when none was named.</summary>
  private static string SettingsFolder()
  {
    string server_file = ServerSettings.DefaultFilePath;
    string? folder = Path.GetDirectoryName(server_file);
    if (string.IsNullOrEmpty(folder))
      folder = Path.GetTempPath();
    return folder;
  }

  /// <summary>Where the memories of the person standing in front of the machine are written.</summary>
  public static string DefaultFilePath
  {
    get { return WriteFilePath(null); }
  }

  /// <summary>
  /// Reads the remembered screens. A missing or broken file comes back empty with a sentence, the way every
  /// other local memory reads: nobody is stopped by their own settings being unreadable. A file in the old
  /// shape - three dictionaries keyed by media kind alone, from the time before a screen was more than a media
  /// kind - is read into the new shape here, and the first save writes it back the new way.
  /// </summary>
  public static ScreenViewSettings Load(string? path, out string? problem)
  {
    problem = null;
    ScreenViewSettings settings = new ScreenViewSettings();
    string file = ReadFilePath(path);

    try
    {
      if (!File.Exists(file))
        return settings;

      string text = File.ReadAllText(file);
      if (string.IsNullOrWhiteSpace(text))
        return settings;

      RawFile? read = JsonSerializer.Deserialize<RawFile>(text, JsonOptions);
      if (read is null)
      {
        problem = string.Format("{0} did not contain any screens.", file);
        return settings;
      }

      if (read.Screens != null)
      {
        foreach (KeyValuePair<string, ScreenAnswer?> entry in read.Screens)
        {
          if (entry.Value != null)
            settings.Screens[entry.Key] = entry.Value;
        }
      }

      if (read.Landings != null)
      {
        foreach (KeyValuePair<string, ScreenLanding?> entry in read.Landings)
        {
          if (entry.Value != null && !string.IsNullOrWhiteSpace(entry.Value.By)
              && !string.IsNullOrWhiteSpace(entry.Value.Value))
            settings.Landings[entry.Key] = entry.Value;
        }
      }

      // The old three dictionaries, every one of them keyed by the media kind alone. The screen they described
      // is the screen with no filter in front of it, which is what a media kind on its own always was.
      if (read.Layouts != null)
      {
        foreach (KeyValuePair<string, string?> entry in read.Layouts)
        {
          if (!string.IsNullOrWhiteSpace(entry.Value))
            settings.AnswerFor(entry.Key, NO_FILTER).Layout = entry.Value;
        }
      }

      if (read.Media != null)
      {
        foreach (KeyValuePair<string, string?> entry in read.Media)
        {
          if (!string.IsNullOrWhiteSpace(entry.Value))
            settings.AnswerFor(entry.Key, NO_FILTER).Media = entry.Value;
        }
      }

      if (read.Details != null)
      {
        foreach (KeyValuePair<string, bool> entry in read.Details)
          settings.AnswerFor(entry.Key, NO_FILTER).Details = entry.Value;
      }

      return settings;
    }
    catch (Exception ex)
    {
      problem = string.Format("{0} could not be read ({1}: {2}); screens open in their written shapes.", file,
          ex.GetType().Name, ex.Message);
      return new ScreenViewSettings();
    }
  }

  /// <summary>
  /// Writes the memories. Returns the sentence for the log instead of throwing, as everywhere a button press
  /// is the caller - a screen that remembers its view on a stick where it cannot write still shows its view.
  /// </summary>
  public string Save(string? path, out bool saved)
  {
    string file = WriteFilePath(path);
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
    AllowTrailingCommas = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
  };

  /// <summary>
  /// What one screen remembers. Every word is the screen's own spelling - the layout's enum name, the order key
  /// the server answers, the category as the counting names it - so that a file read by hand says what the
  /// screen will do with it.
  /// </summary>
  public sealed class ScreenAnswer
  {
    /// <summary>"List", "Grid", "Cover", "Stacked" or "Carousel".</summary>
    [JsonPropertyName("layout")]
    public string? Layout { get; set; }

    /// <summary>
    /// Whether the details stand beside the shelf. Carried over from the old file; every screen wears its
    /// details today - see MediaBrowserViewModel.DetailsAlwaysShown - so nothing reads this back yet.
    /// </summary>
    [JsonPropertyName("details")]
    public bool? Details { get; set; }

    /// <summary>"Poster", "Banner" or "Thumbnail".</summary>
    [JsonPropertyName("media")]
    public string? Media { get; set; }

    /// <summary>The order's key: "title", "year", "added", "lastplayed", "season", "episode" or "track".</summary>
    [JsonPropertyName("sort")]
    public string? Sort { get; set; }

    /// <summary>Whether that order starts at its far end.</summary>
    [JsonPropertyName("descending")]
    public bool Descending { get; set; }

    /// <summary>The category the shelf stands under, or nothing while it is read as one shelf.</summary>
    [JsonPropertyName("grouping")]
    public string? Grouping { get; set; }

    /// <summary>
    /// Whether the names that stand for more than one artist ("Various Artists", the placeholder for no artist at
    /// all) are shown on a people wall.  Nothing stands here while the wall shows what it was written to show, which
    /// is the names with those two left out; <c>true</c> is the viewer having asked for every name.
    /// </summary>
    [JsonPropertyName("collections")]
    public bool? Collections { get; set; }

    /// <summary>Whether nothing was ever answered here, which is what keeps a half-empty file short.</summary>
    public bool IsEmpty
    {
      get
      {
        return string.IsNullOrWhiteSpace(Layout) && string.IsNullOrWhiteSpace(Media)
            && string.IsNullOrWhiteSpace(Sort) && string.IsNullOrWhiteSpace(Grouping)
            && Collections == null && !Descending && Details == null;
      }
    }
  }

  /// <summary>The filter a media kind's screen last stood on: the category and the value picked out of it.</summary>
  public sealed class ScreenLanding
  {
    /// <summary>"genre", "year", "age", "actors", "directors" or "collection".</summary>
    [JsonPropertyName("by")]
    public string By { get; set; } = "";

    /// <summary>The value as the counting printed it; a year's number for <c>year</c> and for <c>age</c>.</summary>
    [JsonPropertyName("value")]
    public string Value { get; set; } = "";
  }

  /// <summary>
  /// The file as it stands on disk, which has to answer to two shapes: the one this class writes, and the flat
  /// one it wrote until a screen became more than a media kind. Reading both is cheaper than telling a viewer
  /// that the choices of their evenings were thrown away by an upgrade.
  /// </summary>
  private sealed class RawFile
  {
    [JsonPropertyName("screens")]
    public Dictionary<string, ScreenAnswer?>? Screens { get; set; }

    [JsonPropertyName("landings")]
    public Dictionary<string, ScreenLanding?>? Landings { get; set; }

    [JsonPropertyName("layouts")]
    public Dictionary<string, string?>? Layouts { get; set; }

    [JsonPropertyName("media")]
    public Dictionary<string, string?>? Media { get; set; }

    [JsonPropertyName("details")]
    public Dictionary<string, bool>? Details { get; set; }
  }
}
