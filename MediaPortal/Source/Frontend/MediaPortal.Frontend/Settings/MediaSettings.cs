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
/// What this machine does with the pictures of the library: whether it keeps them, how much room it spends on
/// them, how many it holds where they cost nothing, and how many it asks for at one moment.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why these four answers and no others.</b> Every picture on every screen is read over the network from the
/// server, and the server hands out the pictures that lie next to the media - a backdrop is around 650 KB and up
/// to two megabytes, so a wall of tiles is tens of megabytes before a single frame is drawn. On a television on
/// a cable that is invisible and on one behind wifi it is a wall of empty frames for a few seconds. What a viewer
/// can be asked about that is how much of it this machine is allowed to remember: on disk, in memory, and at once.
/// Nothing here pretends to control the server, and nothing here is written until a screen offers it.
/// </para>
/// <para>
/// The answers belong to the machine, not to the library: one viewer's television has a solid-state disk and
/// another's has none, and both point at the same server. So this is a file beside <c>theme.json</c>, in this
/// viewer's own folder, and the server never hears about it.
/// </para>
/// </remarks>
public sealed class MediaSettings
{
  private const string SETTINGS_FILE_NAME = "media.json";

  /// <summary>
  /// The sizes the cache may be told to hold, in megabytes. The steps are the ones a person picks between:
  /// a box that can spare a fraction of a disk and a box that has a large one.
  /// </summary>
  public static readonly int[] CACHE_SIZES_MB = { 64, 128, 256, 512, 1024, 2048, 4096 };

  /// <summary>
  /// How many pictures of one kind a screen may hold in memory, oldest dropped first. The steps are shelves:
  /// a remembered picture costs the bytes it came with, so the answer is really "how much walking around before
  /// the first shelf has to be fetched again".
  /// </summary>
  public static readonly int[] REMEMBERED_PICTURES = { 50, 100, 200, 400, 800 };

  /// <summary>
  /// How many pictures one screen may have on the wire at one moment. Four is the balance this front end chose
  /// for a wired television; a slow link wants one or two so that the tiles in sight win against the ones off
  /// screen, and a fast one can afford more.
  /// </summary>
  public static readonly int[] PICTURES_AT_ONCE = { 1, 2, 4, 8, 12 };

  /// <summary>
  /// Where the pictures are kept when the viewer named no folder of their own: beside the settings of this
  /// machine, which is the one place that is certain to exist, to be writable, and to belong to this viewer.
  /// </summary>
  public static string DefaultCacheFolder
  {
    get
    {
      string? fromEnvironment = Environment.GetEnvironmentVariable("MP2_PICTURE_CACHE");
      if (!string.IsNullOrWhiteSpace(fromEnvironment))
        return fromEnvironment;

      string? folder = Path.GetDirectoryName(DefaultFilePath);
      if (string.IsNullOrEmpty(folder))
        folder = Path.GetTempPath();
      return Path.Combine(folder, "picture-cache");
    }
  }

  /// <summary>
  /// Where these answers are saved, beside the server settings; overridable with <c>MP2_MEDIA_FILE</c> so that
  /// tests and gates never touch what a real viewer answered.
  /// </summary>
  public static string DefaultFilePath
  {
    get
    {
      string? fromEnvironment = Environment.GetEnvironmentVariable("MP2_MEDIA_FILE");
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
  /// Whether the pictures this machine has once fetched are written to its disk. Off means every shelf is paid
  /// for again at every start, which is what a box with no room to spare wants.
  /// </summary>
  [JsonPropertyName("cachePictures")]
  public bool CachePictures { get; set; } = true;

  /// <summary>
  /// How much room the kept pictures may take, in megabytes. Past it the oldest go, the way the memory cache
  /// drops the title nobody looked at longest.
  /// </summary>
  [JsonPropertyName("cacheSizeMb")]
  public int CacheSizeMb { get; set; } = 256;

  /// <summary>
  /// Where the pictures are kept, or empty for this machine's own folder. A viewer with a small system disk and
  /// a big data disk names the second one here.
  /// </summary>
  [JsonPropertyName("cacheFolder")]
  public string CacheFolder { get; set; } = "";

  /// <summary>
  /// How many pictures of a kind one screen remembers without asking again, per kind and per screen.
  /// </summary>
  [JsonPropertyName("rememberedPictures")]
  public int RememberedPictures { get; set; } = 200;

  /// <summary>
  /// How many pictures one screen may have on the wire at one moment.
  /// </summary>
  [JsonPropertyName("picturesAtOnce")]
  public int PicturesAtOnce { get; set; } = 4;

  /// <summary>
  /// The folder these answers point at, whether that is the machine's own or the one the viewer named.
  /// Derived from <see cref="CacheFolder"/>, so it is not written to the file.
  /// </summary>
  [JsonIgnore]
  public string CacheFolderPath
  {
    get
    {
      if (string.IsNullOrWhiteSpace(CacheFolder))
        return DefaultCacheFolder;

      try
      {
        return Environment.ExpandEnvironmentVariables(CacheFolder).Trim();
      }
      catch (Exception)
      {
        // A path the operating system refuses to expand is still a path the viewer typed; the cache will fail
        // on it politely, and the screen will say what it could not do.
        return CacheFolder.Trim();
      }
    }
  }

  /// <summary>
  /// The room the cache was given, in bytes. Derived from <see cref="CacheSizeMb"/>, so it is not written
  /// to the file either.
  /// </summary>
  [JsonIgnore]
  public long CacheSizeBytes
  {
    get { return (long)NotMoreThan(CACHE_SIZES_MB, CacheSizeMb) * 1024L * 1024L; }
  }

  /// <summary>
  /// Reads the answers. A missing file is not a problem but a first run, and a broken one says so and starts
  /// from the answers this front end chose, the way every other local memory reads.
  /// </summary>
  public static MediaSettings Load(string? path, out string? problem)
  {
    problem = null;
    string file = path ?? DefaultFilePath;

    MediaSettings settings = new MediaSettings();
    try
    {
      if (!File.Exists(file))
        return settings;

      string text = File.ReadAllText(file);
      if (string.IsNullOrWhiteSpace(text))
        return settings;

      MediaSettings? read = JsonSerializer.Deserialize<MediaSettings>(text, JsonOptions);
      if (read is null)
      {
        problem = string.Format("{0} did not contain any media answers.", file);
        return settings;
      }

      // Answers from a file are not trusted further than they can be used: what a hand typed into the file
      // that is not one of the answers on the screen is moved to the nearest answer that is, rather than being
      // honoured with a cache that eats the disk or a screen that asks for sixty pictures at once.
      settings = read;
      settings.CacheFolder = read.CacheFolder ?? "";
      settings.CacheSizeMb = NotMoreThan(CACHE_SIZES_MB, read.CacheSizeMb);
      settings.RememberedPictures = NotMoreThan(REMEMBERED_PICTURES, read.RememberedPictures);
      settings.PicturesAtOnce = NotMoreThan(PICTURES_AT_ONCE, read.PicturesAtOnce);
      return settings;
    }
    catch (Exception ex)
    {
      problem = string.Format("{0} could not be read ({1}: {2}); starting with the media answers this front end "
          + "chose.", file, ex.GetType().Name, ex.Message);
      return new MediaSettings();
    }
  }

  /// <summary>
  /// Writes the answers. Returns the sentence for the status line instead of throwing, as everywhere a row is
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

  /// <summary>
  /// The answer among a screen's answers that is nearest to the one that was read without going over it. A
  /// hand-written file with <c>300</c> remembered pictures means two hundred, not four hundred: a viewer who
  /// writes a number into a file has said how much they are willing to spend, and a screen that quietly
  /// doubles that promise is worse than a screen that admits it did not understand.
  /// </summary>
  private static int NotMoreThan(int[] answers, int asked)
  {
    if (answers.Length == 0)
      return asked;

    int chosen = answers[0];
    foreach (int answer in answers)
    {
      if (answer > asked)
        break;

      chosen = answer;
    }

    return chosen;
  }

  private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
  {
    WriteIndented = true,
    ReadCommentHandling = JsonCommentHandling.Skip,
    AllowTrailingCommas = true
  };
}
