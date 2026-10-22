#region Copyright (C) 2007-2026 Team MediaPortal

/*
    Copyright (C) 2007-2026 Team MediaPortal
    http://www.team-mediaportal.com

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

#endregion

using System;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MediaPortal.Frontend.Settings
{
  /// <summary>
  /// Where the television comes from: the address of the TV backend this front end asks for channels, for the
  /// programme guide and for recordings, and the few answers about how the guide is to be drawn.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The reference kept these two answers in two places, and both are followed here. The address of the machine
  /// that serves television belonged to its provider plugin
  /// (<c>UI/TV/SlimTVMPExtendedProvider/Settings/MPExtendedProviderSettings.cs</c>: host, user, password),
  /// because which product serves television is the provider's business and not the television plugin's. How much
  /// guide to show belonged to the plugin (<c>SlimTvClientSettings.SingleChannelGuideDays</c>, fourteen days).
  /// </para>
  /// <para>
  /// A file beside <c>frontend.json</c> rather than fields inside it, for the reason spelled out on
  /// <see cref="ServerSettings"/> and again on <see cref="WeatherPlacesSettings"/>: the server address is the one
  /// decision the front end cannot run without and should stay boring, while a TV backend is added, moved and
  /// re-typed by people.
  /// </para>
  /// <para>
  /// The password is written to this file in the open. That is deliberate rather than overlooked: every question
  /// about television has to be asked of the backend with these credentials, so unlike a share's credential -
  /// which belongs to the server and never travels to this machine - this one has to be here to be of any use.
  /// It is the same trade the reference made when it stored its provider's password in the settings store.
  /// Nobody but the household reads a file in their own user folder, and the address of a TV backend is not a
  /// secret worth an encryption scheme nobody can recover the key from.
  /// </para>
  /// <para>
  /// An empty host is not a broken answer; it is a box that has not been told where its television comes from,
  /// and every TV screen says exactly that instead of failing.
  /// </para>
  /// </remarks>
  public sealed class TvBackendSettings
  {
    private const string SETTINGS_FILE_NAME = "tv.json";

    /// <summary>The port TVHeadend listens on when it was installed without anyone changing it.</summary>
    public const int DEFAULT_PORT = 9981;

    /// <summary>
    /// How far ahead the single-channel guide reaches, which is the reference's own answer and the horizon most
    /// backends actually hold programme data for.
    /// </summary>
    public const int DEFAULT_GUIDE_DAYS = 14;

    /// <summary>
    /// Where the answers are saved. Overridable with <c>MP2_TV_FILE</c> for the same reason the weather file is
    /// overridable with <c>MP2_WEATHER_FILE</c>: the gates and the tests must not read or write what a real
    /// viewer saved.
    /// </summary>
    public static string DefaultFilePath
    {
      get
      {
        string? fromEnvironment = Environment.GetEnvironmentVariable("MP2_TV_FILE");
        if (!string.IsNullOrWhiteSpace(fromEnvironment))
          return fromEnvironment;

        // The server settings' folder is the folder's definition, so a test that moves one moves the other with it.
        string serverFile = ServerSettings.DefaultFilePath;
        string? folder = Path.GetDirectoryName(serverFile);
        if (string.IsNullOrEmpty(folder))
          folder = Path.GetTempPath();
        return Path.Combine(folder, SETTINGS_FILE_NAME);
      }
    }

    /// <summary>
    /// The machine that serves television, as somebody wrote it: a name from the local network or an address.
    /// Empty until the viewer says.
    /// </summary>
    [JsonPropertyName("host")]
    public string Host { get; set; } = "";

    /// <summary>The port it answers on.</summary>
    [JsonPropertyName("port")]
    public int Port { get; set; } = DEFAULT_PORT;

    /// <summary>The name the backend knows this box as. Empty when the backend lets anybody look.</summary>
    [JsonPropertyName("username")]
    public string Username { get; set; } = "";

    /// <summary>The password for that name. Empty when the backend lets anybody look.</summary>
    [JsonPropertyName("password")]
    public string Password { get; set; } = "";

    /// <summary>
    /// How many days ahead the guide of one channel reaches. Long horizons cost a longer answer from the backend,
    /// and most backends stop having data after a fortnight anyway.
    /// </summary>
    [JsonPropertyName("guide_days")]
    public int GuideDays { get; set; } = DEFAULT_GUIDE_DAYS;

    /// <summary>
    /// Whether anybody has said where the television comes from. Every TV screen asks this before it asks
    /// anything of the backend, so that a box without a backend says so rather than complaining about a timeout.
    /// </summary>
    public bool IsConfigured
    {
      get { return !string.IsNullOrWhiteSpace(Host); }
    }

    /// <summary>
    /// The address every question is asked at, or <c>null</c> when no host was named. Never throws, because the
    /// screens would rather ask <see cref="IsConfigured"/> and explain than catch.
    /// </summary>
    public Uri? BaseUrl
    {
      get
      {
        string host = (Host ?? "").Trim();
        if (host.Length == 0)
          return null;

        // A named port has to be a number somebody meant. A box holding "998a" is a typing mistake, and the
        // screen that offered the box is the one that has to hear about it - so no address comes back.
        int port = Port;
        if (port < 1 || port > 65535)
          return null;

        // Spaces are stripped out of a host that was pasted in with them; anything the URI builder refuses is
        // answered with no address rather than with an exception travelling through a settings screen.
        try
        {
          return new Uri(string.Format(CultureInfo.InvariantCulture, "http://{0}:{1}/",
              host.Replace(" ", ""), port));
        }
        catch (UriFormatException)
        {
          return null;
        }
      }
    }

    /// <summary>
    /// What to call this backend in a sentence the viewer reads. Says the host and the port, and never the
    /// password - the same rule the connection screen plays by.
    /// </summary>
    public string Describe()
    {
      string host = (Host ?? "").Trim();
      if (host.Length == 0)
        return "no TV backend named";
      return host + ":" + Port.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Reads the backend's address. A missing or broken file comes back as nothing configured and a sentence,
    /// exactly like every other file beside <c>frontend.json</c>: the television screens open either way and
    /// say what is missing.
    /// </summary>
    public static TvBackendSettings Load(string? path, out string? problem)
    {
      problem = null;
      string file = path ?? DefaultFilePath;

      try
      {
        if (!File.Exists(file))
          return new TvBackendSettings();

        string text = File.ReadAllText(file);
        if (string.IsNullOrWhiteSpace(text))
          return new TvBackendSettings();

        TvBackendSettings? read = JsonSerializer.Deserialize<TvBackendSettings>(text, JsonOptions);
        if (read is null)
        {
          problem = string.Format("{0} did not contain any settings.", file);
          return new TvBackendSettings();
        }

        // A file written by hand - which is how a headless box is set up from ssh - may leave the port out, and
        // zero is not a port. The defaults are the answer to a missing field, not an empty screen.
        if (read.Port < 1 || read.Port > 65535)
          read.Port = DEFAULT_PORT;
        if (read.GuideDays < 1)
          read.GuideDays = DEFAULT_GUIDE_DAYS;
        read.Host ??= "";
        read.Username ??= "";
        read.Password ??= "";
        return read;
      }
      catch (Exception ex)
      {
        problem = string.Format("{0} could not be read ({1}: {2}); starting without a TV backend.", file,
            ex.GetType().Name, ex.Message);
        return new TvBackendSettings();
      }
    }

    /// <summary>
    /// Writes the address. Returns the sentence for the status line instead of throwing, as everywhere a button
    /// is the caller.
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
}
