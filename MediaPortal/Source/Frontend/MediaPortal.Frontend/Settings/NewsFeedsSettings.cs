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
using MediaPortal.Frontend.Api;

namespace MediaPortal.Frontend.Settings;

/// <summary>
/// One feed the viewer added themselves: what it is called on the screen's left column, and where to ask.
/// </summary>
public sealed class NewsFeedEntry
{
  [JsonPropertyName("name")]
  public string Name { get; set; } = "";

  [JsonPropertyName("url")]
  public string Url { get; set; } = "";
}

/// <summary>
/// The news the viewer asked for on top of MediaPortal's own feed list, and how often an open news screen
/// asks again. The reference's news plugin kept both in the client settings; here they keep their own file
/// beside the others for the same reason the weather places do: it is the news screen's own answer, not a
/// switch on a screen about the machine.
/// </summary>
public sealed class NewsFeedsSettings
{
  private const string SETTINGS_FILE_NAME = "news-feeds.json";

  /// <summary>The answers the interval row walks. Zero means what it says; the reference's fifteen minutes is where it starts.</summary>
  public static readonly int[] INTERVALS = { 0, 5, 15, 30, 60 };

  /// <summary>
  /// Where the file is: <c>MP2_NEWS_FEEDS_FILE</c> when said, otherwise the neighbour of the client
  /// settings - which itself follows <c>MP2_SETTINGS_FILE</c>, so the test host's single isolation
  /// variable isolates this file too.
  /// </summary>
  public static string DefaultFilePath
  {
    get
    {
      string? fromEnvironment = Environment.GetEnvironmentVariable("MP2_NEWS_FEEDS_FILE");
      if (!string.IsNullOrWhiteSpace(fromEnvironment))
        return fromEnvironment;

      string? folder = Path.GetDirectoryName(ServerSettings.DefaultFilePath);
      if (string.IsNullOrEmpty(folder))
        folder = Path.GetTempPath();
      return Path.Combine(folder, SETTINGS_FILE_NAME);
    }
  }

  /// <summary>Feeds the viewer added, in the order they added them; they read after MediaPortal's own.</summary>
  [JsonPropertyName("extraFeeds")]
  public List<NewsFeedEntry> ExtraFeeds { get; set; } = new List<NewsFeedEntry>();

  /// <summary>
  /// How often an open news screen asks again, in minutes; zero - the default here, unlike the
  /// reference's fifteen - reads only when the screen opens and when the viewer says so.
  /// </summary>
  [JsonPropertyName("intervalMinutes")]
  public int IntervalMinutes { get; set; }

  private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
  {
    WriteIndented = true,
    ReadCommentHandling = JsonCommentHandling.Skip,
    AllowTrailingCommas = true
  };

  /// <summary>
  /// Reads the file; a missing or broken one answers "no extra feeds, no clock" and a sentence, like every
  /// other settings file - the news screen reads MediaPortal's own list either way.
  /// </summary>
  public static NewsFeedsSettings Load(string? path, out string? problem)
  {
    problem = null;
    string file = path ?? DefaultFilePath;

    try
    {
      if (!File.Exists(file))
        return new NewsFeedsSettings();

      string text = File.ReadAllText(file);
      if (string.IsNullOrWhiteSpace(text))
        return new NewsFeedsSettings();

      NewsFeedsSettings? read = JsonSerializer.Deserialize<NewsFeedsSettings>(text, JsonOptions);
      if (read is null)
      {
        problem = string.Format("{0} did not contain any feed settings.", file);
        return new NewsFeedsSettings();
      }

      read.ExtraFeeds ??= new List<NewsFeedEntry>();
      return read;
    }
    catch (Exception ex)
    {
      problem = string.Format("{0} could not be read ({1}: {2}); reading MediaPortal's own feeds only.", file,
          ex.GetType().Name, ex.Message);
      return new NewsFeedsSettings();
    }
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

  /// <summary>
  /// The viewer's feeds as bookmarks, skipping the ones with no address - a named feed whose URL was never
  /// typed is a half an answer, and half an answer read as a refusing feed would be a lie about the network.
  /// </summary>
  public IReadOnlyList<NewsFeedBookmark> ExtraBookmarks()
  {
    List<NewsFeedBookmark> bookmarks = new List<NewsFeedBookmark>();
    foreach (NewsFeedEntry entry in ExtraFeeds)
    {
      if (string.IsNullOrWhiteSpace(entry.Url))
        continue;

      string name = string.IsNullOrWhiteSpace(entry.Name) ? entry.Url.Trim() : entry.Name.Trim();
      bookmarks.Add(new NewsFeedBookmark(name, entry.Url.Trim()));
    }

    return bookmarks;
  }

  /// <summary>MediaPortal's own list first, the viewer's on top of it - the order the reference read them.</summary>
  public static IReadOnlyList<NewsFeedBookmark> Combine(
      IReadOnlyList<NewsFeedBookmark> defaults, IReadOnlyList<NewsFeedBookmark> extra)
  {
    if (extra.Count == 0)
      return defaults;

    List<NewsFeedBookmark> all = new List<NewsFeedBookmark>(defaults);
    all.AddRange(extra);
    return all;
  }
}
