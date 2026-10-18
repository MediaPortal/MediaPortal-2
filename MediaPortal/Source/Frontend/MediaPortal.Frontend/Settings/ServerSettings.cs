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
/// The one thing a viewer has to decide before browsing: which server to talk to.
/// </summary>
/// <remarks>
/// <para>
/// Kept in a file in the user's own application-data folder, which is a different place on every platform
/// (<c>%APPDATA%</c> on Windows, <c>~/.config</c> on a Linux desktop, the app's private directory on Android), and
/// the reason the whole thing is written with <see cref="Environment.GetFolderPath(SpecialFolder)"/> instead of a
/// registry key or a path with a drive letter in it: the old client kept its configuration in the registry, which is
/// one of the reasons it could only ever run on Windows.
/// </para>
/// <para>
/// The file is deliberately small, plain JSON, and written by replacing it rather than editing it in place: a media
/// centre is often switched off without being closed, and a half-written configuration file that stops the front end
/// from starting is a far worse outcome than losing the last saved address.
/// </para>
/// </remarks>
public sealed class ServerSettings
{
  /// <summary>
  /// Name of the folder the settings live in when nothing else is said.
  /// </summary>
  public const string SETTINGS_DIRECTORY_NAME = "MediaPortal";
  private const string SETTINGS_FILE_NAME = "frontend.json";

  /// <summary>
  /// Where the address of the server was saved, or is going to be saved when <see cref="Save"/> is called without a
  /// path. Overridable with the <c>MP2_SETTINGS_FILE</c> environment variable for the same reason the URL is
  /// overridable with <c>MP2_SERVER</c>: the gates and the tests must not read or write what a real user saved.
  /// </summary>
  public static string DefaultFilePath
  {
    get
    {
      string? fromEnvironment = Environment.GetEnvironmentVariable("MP2_SETTINGS_FILE");
      if (!string.IsNullOrWhiteSpace(fromEnvironment))
        return fromEnvironment;

      string folder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
      if (string.IsNullOrEmpty(folder))
        folder = Path.GetTempPath(); // A process without a user profile still has a temp folder. Settings are worth
                                     // keeping, but not worth refusing to start the front end over.
      return Path.Combine(folder, SETTINGS_DIRECTORY_NAME, SETTINGS_FILE_NAME);
    }
  }

  /// <summary>
  /// The server the front end last used, as an absolute URL with the server's path base on the end. Empty when
  /// nothing has been saved, which is what a fresh installation looks like.
  /// </summary>
  [JsonPropertyName("server_url")]
  public string ServerUrl { get; set; } = "";

  /// <summary>
  /// <c>true</c> if a URL is in these settings and could be used as the address of a server. Does not say the server
  /// is there - only that the address is well-formed enough to try.
  /// </summary>
  public bool HasUsableUrl => Uri.TryCreate(ServerUrl, UriKind.Absolute, out Uri? parsed)
      && (parsed.Scheme == Uri.UriSchemeHttp || parsed.Scheme == Uri.UriSchemeHttps);

  /// <summary>
  /// Reads the settings. A missing, empty, unreadable or malformed file is not an error: that is what a first run
  /// looks like, and a front end that refuses to open because of a stray byte in a settings file could not be
  /// repaired from the couch it is sitting on. The problem is reported through <paramref name="problem"/> so a screen
  /// can say it out loud, and usable defaults come back either way.
  /// </summary>
  public static ServerSettings Load(string? path, out string? problem)
  {
    problem = null;
    string file = path ?? DefaultFilePath;

    try
    {
      if (!File.Exists(file))
        return new ServerSettings();

      string text = File.ReadAllText(file);
      if (string.IsNullOrWhiteSpace(text))
        return new ServerSettings();

      ServerSettings? read = JsonSerializer.Deserialize<ServerSettings>(text, JsonOptions);
      if (read is null)
      {
        problem = string.Format("{0} did not contain any settings.", file);
        return new ServerSettings();
      }
      return read;
    }
    catch (Exception ex)
    {
      // JsonException for a file that is not JSON, IOException or UnauthorizedAccessException for a file on a share
      // that is away or owned by somebody else. All of them mean the same thing to the viewer: the saved address
      // could not be used, so it has to be entered again.
      problem = string.Format("{0} could not be read ({1}: {2}); using the defaults.", file,
          ex.GetType().Name, ex.Message);
      return new ServerSettings();
    }
  }

  /// <summary>
  /// Writes the settings, creating the folder if this is the first save. Returns the sentence to show the viewer
  /// instead of throwing: the only caller is a button on a screen, and "it was not saved, here is why" is more useful
  /// there than an exception dialog.
  /// </summary>
  public string Save(string? path, out bool saved)
  {
    string file = path ?? DefaultFilePath;
    try
    {
      string? folder = Path.GetDirectoryName(file);
      if (!string.IsNullOrEmpty(folder))
        Directory.CreateDirectory(folder);

      // Write to a sibling and move it over: being switched off halfway through leaves the old file intact instead
      // of a truncated one.
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
