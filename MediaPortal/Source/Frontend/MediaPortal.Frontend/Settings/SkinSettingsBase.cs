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
/// What kind of picture a media item shows in a grid: the cover the skin was designed around, the banner
/// the series tiles used, or whatever still had a picture - in the reference's words, exactly one of the
/// three <c>GridViewType</c> values both skin settings classes carry.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GridViewType
{
  Poster,
  Banner,
  Thumbnail
}

/// <summary>
/// The settings one skin keeps, as the reference kept them: options of the skin, not of the application,
/// stored per viewer. Both shipped skins fill this with their own properties; a screen reads the worn
/// skin's answers through the base and asks the concrete class for anything only that skin has.
/// </summary>
/// <remarks>
/// <para>
/// The reference stored every one of these as <c>[Setting(SettingScope.User, …)]</c>, so two viewers of one
/// machine could wear different colours of the same skin. This front end has one memory per Windows user
/// exactly that way: the file sits beside <c>theme.json</c> in the user's own application data, where
/// <c>ServerSettings</c> keeps its address book. A later user management (plan C7) hangs another layer
/// above this one; the per-skin file is the ground it stands on.
/// </para>
/// <para>
/// One file per skin (<c>skin-nereus.json</c>, <c>skin-wmc.json</c>) because the reference kept the two
/// skins' settings in separate sections too, and because a screen must never read an option the worn skin
/// does not have. Where that file lives is overridable with <c>MP2_SKIN_SETTINGS_DIR</c> for tests and
/// gates; without it they follow <c>theme.json</c>'s folder, so a run that isolates one isolates the others.
/// </para>
/// <para>
/// The settings screen for these - the reference's own "/Appearance/SkinSettings" section - is plan phase
/// C2. Until it exists, every property reads its shipped default, which is the reference default taken
/// from <c>NereusSkinSettings.cs</c> and <c>WMCSkinSettings.cs</c> line by line.
/// </para>
/// </remarks>
public abstract class SkinSettingsBase
{
  /// <summary>The name the skin is worn by ('nereus', 'wmc'), which is also what names its file.</summary>
  public abstract string SkinName { get; }

  /// <summary>
  /// Who the settings files currently name: the signed-in user, or null while nobody has signed in. This is
  /// the layer the plan promised above the per-skin file - the reference stored its skin settings per
  /// MediaPortal user, so two viewers of one household could wear different colours of one skin. While
  /// nobody signs in (the ordinary living room, and every server that asks nobody), the shared file stands
  /// exactly where it always stood.
  /// </summary>
  public static string? FileOwner { get; set; }

  /// <summary>
  /// The folder the skin files share: <c>MP2_SKIN_SETTINGS_DIR</c> when said, <c>theme.json</c>'s folder
  /// otherwise - which itself follows the user's application data or <c>MP2_THEME_FILE</c>.
  /// </summary>
  private static string SettingsFolder()
  {
    string? folder = Environment.GetEnvironmentVariable("MP2_SKIN_SETTINGS_DIR");
    if (!string.IsNullOrWhiteSpace(folder))
      return folder;

    folder = Path.GetDirectoryName(ServerSettings.DefaultFilePath);
    if (string.IsNullOrEmpty(folder))
      folder = Path.GetTempPath();
    return folder;
  }

  /// <summary>The shared file of one skin - the answer, and the file, from before anybody signed in.</summary>
  public static string FileFor(string skinName)
  {
    return Path.Combine(SettingsFolder(), "skin-" + skinName.Trim().ToLowerInvariant() + ".json");
  }

  /// <summary>
  /// The file one skin's answers live in right now: the signed-in user's own when there is one, the shared
  /// file while there is not. A name is tidied for the filesystem the same way every other stored name here
  /// is - what a person is called and what a file may be called are two questions.
  /// </summary>
  public static string FileForUser(string skinName)
  {
    string owner = FileOwner ?? "";
    if (owner.Length == 0)
      return FileFor(skinName);

    foreach (char bad in Path.GetInvalidFileNameChars())
      owner = owner.Replace(bad, '-');

    return Path.Combine(SettingsFolder(),
        "skin-" + skinName.Trim().ToLowerInvariant() + "-" + owner.Trim().ToLowerInvariant() + ".json");
  }

  /// <summary>
  /// The settings of the skin named: the shipped catalogue read from that skin's own file. A skin that
  /// ships no settings class answers null with the sentence, because inventing options for a skin nobody
  /// wrote one for is exactly the mistake this class exists to keep out.
  /// </summary>
  public static SkinSettingsBase? LoadFor(string? skinName, string? path, out string? problem)
  {
    string name = (skinName ?? "").Trim();

    if (name.Equals("nereus", StringComparison.OrdinalIgnoreCase))
      return NereusSkinSettings.Load(path, out problem);
    if (name.Equals("wmc", StringComparison.OrdinalIgnoreCase))
      return WMCSkinSettings.Load(path, out problem);

    problem = string.Format("'{0}' ships no settings of its own, so the screens draw with none.", skinName);
    return null;
  }

  // What both skins speak, in the reference's order. Screens read these through the base without knowing
  // whose skin they are wearing; the names are the reference's names, unchanged, because every future
  // screen, dialog and translation will quote them.

  [JsonPropertyName("enableFanart")]
  public bool EnableFanart { get; set; } = true;

  /// <summary>Not shared after all: Nereus shipped 0.85 and WMC 1.0, so the constructors say it.</summary>
  [JsonPropertyName("fanartOverlayOpacity")]
  public double FanartOverlayOpacity { get; set; }

  [JsonPropertyName("enableListWatchedFlags")]
  public bool EnableListWatchedFlags { get; set; } = true;

  [JsonPropertyName("enableGridWatchedFlags")]
  public bool EnableGridWatchedFlags { get; set; } = true;

  [JsonPropertyName("enableCoverWatchedFlags")]
  public bool EnableCoverWatchedFlags { get; set; } = true;

  [JsonPropertyName("movieGridViewType")]
  public GridViewType MovieGridViewType { get; set; } = GridViewType.Poster;

  [JsonPropertyName("seriesGridViewType")]
  public GridViewType SeriesGridViewType { get; set; } = GridViewType.Poster;

  [JsonPropertyName("seasonGridViewType")]
  public GridViewType SeasonGridViewType { get; set; } = GridViewType.Poster;

  /// <summary>
  /// Whether a screen without a picked answer wears its details. The reference kept this off by default and
  /// showed details only instead of playing; this front end shows details beside the shelf and its viewers
  /// asked to see them, so the answer it gives until somebody says otherwise on the screen itself is yes.
  /// </summary>
  [JsonPropertyName("enableMediaItemDetailsView")]
  public bool EnableMediaItemDetailsView { get; set; } = true;

  [JsonPropertyName("enableAgeCertificationLogos")]
  public bool EnableAgeCertificationLogos { get; set; } = true;

  /// <summary>
  /// Which file a read consults for the current owner: their own file once they have one; the shared file
  /// if it is still the only answer this machine has (a signed-in viewer inherits what the machine was
  /// told, and their first save leaves their own file behind); and their own missing file - the shipped
  /// defaults - when nobody ever answered anything.
  /// </summary>
  private static string ReadFileFor(string skinName)
  {
    string own = FileForUser(skinName);
    if (string.IsNullOrEmpty(FileOwner) || File.Exists(own))
      return own;

    string shared = FileFor(skinName);
    return File.Exists(shared) ? shared : own;
  }

  /// <summary>
  /// Reads one skin's file. A missing file is nobody having touched the settings - the shipped defaults
  /// answer without a complaint; a broken one says its sentence and still answers, because the front end
  /// draws rather than refusing to start over an unreadable file (the <see cref="ThemeSettings"/> bargain).
  /// </summary>
  protected static T Read<T>(string? path, out string? problem) where T : SkinSettingsBase, new()
  {
    problem = null;
    T settings = new T();
    string file = path ?? ReadFileFor(settings.SkinName);

    try
    {
      if (!File.Exists(file))
        return settings;

      string text = File.ReadAllText(file);
      if (string.IsNullOrWhiteSpace(text))
        return settings;

      T? read = JsonSerializer.Deserialize<T>(text, JsonOptions);
      if (read is null)
      {
        problem = string.Format("{0} did not contain skin settings.", file);
        return settings;
      }

      return read;
    }
    catch (Exception ex)
    {
      problem = string.Format("{0} could not be read ({1}: {2}); wearing the skin's shipped settings.", file,
          ex.GetType().Name, ex.Message);
      return settings;
    }
  }

  /// <summary>
  /// Fires whenever one skin's answers have been written to disk. Screens that opened under the old
  /// answers reread themselves on it, so a switch the viewer flips takes its effect on the television
  /// at once rather than "next time the screen opens" - the skin-settings screen is a remote control,
  /// not an application form.
  /// </summary>
  public static event EventHandler? Saved;

  /// <summary>
  /// Writes this skin's file. Best-effort with a sentence instead of an exception, like every save a
  /// screen's button performs; the runtime type is serialized, so a save through the base class still
  /// carries the concrete skin's own properties.
  /// </summary>
  public string Save(string? path, out bool saved)
  {
    string file = path ?? FileForUser(SkinName);
    string says;
    try
    {
      string? folder = Path.GetDirectoryName(file);
      if (!string.IsNullOrEmpty(folder))
        Directory.CreateDirectory(folder);

      string temporary = file + ".new";
      File.WriteAllText(temporary, JsonSerializer.Serialize(this, GetType(), JsonOptions));
      File.Move(temporary, file, true);
      saved = true;
      says = string.Format("Saved to {0}.", file);
    }
    catch (Exception ex)
    {
      saved = false;
      says = string.Format("Could not be saved ({0}: {1}).", ex.GetType().Name, ex.Message);
    }

    if (saved)
    {
      // The screens that reread themselves on this event belong to the television, not to the file: one
      // that throws on its refresh must never make a written file be reported as unwritten.
      try
      {
        Saved?.Invoke(this, EventArgs.Empty);
      }
      catch (Exception)
      {
      }
    }

    return says;
  }

  protected static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
  {
    WriteIndented = true,
    ReadCommentHandling = JsonCommentHandling.Skip,
    AllowTrailingCommas = true
  };
}
