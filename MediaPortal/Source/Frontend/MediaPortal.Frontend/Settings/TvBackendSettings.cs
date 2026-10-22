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
  /// <para>
  /// The reference kept thirteen switches and numbers of its own in <c>SlimTvClientSettings</c>. Eleven are here:
  /// the guide's window and its number of rows, which of a channel's two names are written at the head of a row,
  /// whether a programme is coloured by its genre and carries its episode line, whether the "All Channels"
  /// bouquet is hidden, and the three zapping answers plus the automatic start. The days of guide were ported
  /// earlier and are <see cref="GuideDays"/>. Three are deliberately not here: the recording notifications and
  /// their duration, because this front end has nothing that could announce a recording to a viewer who is doing
  /// something else, and a switch that announces nothing would be a lie; and the channel logo, because this client
  /// has never read a picture for a channel from a backend, so there is nothing for a switch to turn on. The
  /// per-genre colours the reference lets a viewer pick are the skin's business here and are in
  /// <c>TvGenreColors</c>, not in a file a viewer edits over ssh.
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
    /// How much of the day the programme guide shows at once. The reference's own default was two hours
    /// (<c>SlimTvClientSettings.EpgVisibleHours</c>) across seventy-five columns; this front end draws ninety
    /// columns and started at three, which is what a 16:9 screen has room for without the hour labels crowding
    /// each other. The switch is the reference's and can be turned back to two.
    /// </summary>
    public const double DEFAULT_VISIBLE_HOURS = 3;

    /// <summary>How many channels the guide keeps on the screen at once, the reference's <c>EpgNumberOfRows</c>.</summary>
    public const int DEFAULT_VISIBLE_ROWS = 9;

    /// <summary>
    /// How long a viewer is left alone after typing a digit before the number is taken as a channel, which is the
    /// reference's <c>ZapTimeout</c> and its two seconds.
    /// </summary>
    public const double DEFAULT_ZAP_TIMEOUT_SECONDS = 2;

    /// <summary>The window the guide will not draw past, in hours. Beyond it the hour labels overlap.</summary>
    public const double MAX_VISIBLE_HOURS = 12;

    /// <summary>The narrowest window worth drawing: half an hour, which is one long programme.</summary>
    public const double MIN_VISIBLE_HOURS = 0.5;

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
    /// How many hours of the day the programme guide shows at once. The reference read the same number straight
    /// into its grid's arithmetic - seventy-five columns over that many hours, so the width of a column is what is
    /// left after dividing (<c>Controls/EpgGrid.cs:96-100</c>) - and this front end does the same with ninety.
    /// </summary>
    [JsonPropertyName("visible_hours")]
    public double VisibleHours { get; set; } = DEFAULT_VISIBLE_HOURS;

    /// <summary>
    /// How many channels the guide keeps on the screen at once. The reference let its grid have exactly this many
    /// rows and scrolled the rest; here the rows share the height the screen has left, so the number says how many
    /// are visible together and how big each one has to be.
    /// </summary>
    [JsonPropertyName("visible_rows")]
    public int VisibleRows { get; set; } = DEFAULT_VISIBLE_ROWS;

    /// <summary>
    /// Whether a channel's name stands at the head of its row. The reference asked this together with the number
    /// and the logo in one "visible channel information" item (<c>EpgVisibleChannelInfoSetting</c>) and computed
    /// the header's width from what was left on.
    /// </summary>
    [JsonPropertyName("show_channel_names")]
    public bool ShowChannelNames { get; set; } = true;

    /// <summary>Whether a channel's number stands at the head of its row, before its name.</summary>
    [JsonPropertyName("show_channel_numbers")]
    public bool ShowChannelNumbers { get; set; } = true;

    /// <summary>
    /// Whether a programme is coloured by what it is. The reference's own default was off, because its skin drew a
    /// coloured bar of its own beside every block and the fill was a plain blue; here the fill is the only place a
    /// genre is written, so it is on unless a viewer says otherwise.
    /// </summary>
    [JsonPropertyName("show_genre_colors")]
    public bool ShowGenreColors { get; set; } = true;

    /// <summary>
    /// Whether the "S07 E03" line is written into a programme. The reference hid it from the details of a
    /// programme and from its list items alike when this was off
    /// (<c>Helpers/ProgramListItem.cs:112</c>).
    /// </summary>
    [JsonPropertyName("show_series_info")]
    public bool ShowSeriesInfo { get; set; } = true;

    /// <summary>
    /// Whether the bouquet the reference's TV server called "All Channels" is left out of the picker. TVHeadend has
    /// no inbuilt bouquet of that name, so this only ever hides one a viewer named that way - which is exactly what
    /// the reference's own filter did, comparing names and nothing else
    /// (<c>Helpers/ChannelContext.cs:218-231</c>).
    /// </summary>
    [JsonPropertyName("hide_all_channels_group")]
    public bool HideAllChannelsGroup { get; set; }

    /// <summary>
    /// Whether this viewer wants that bouquet left out of what they are offered.
    /// </summary>
    public bool IsHiddenGroup(string? name)
    {
      return HideAllChannelsGroup && IsAllChannelsGroup(name);
    }

    /// <summary>
    /// Whether a bouquet is named the one the reference's <c>HideAllChannelsGroup</c> switch threw away. The
    /// reference compared the name against "All Channels" and nothing else (<c>Helpers/ChannelContext.cs:218-231</c>);
    /// a TVHeadend bouquet is named by whoever made it, so this also takes the German spelling and does not care
    /// how it was capitalised, which is the only place it is more generous than the reference was.
    /// </summary>
    public static bool IsAllChannelsGroup(string? name)
    {
      string said = (name ?? "").Trim();
      return said.Equals("All Channels", StringComparison.OrdinalIgnoreCase)
          || said.Equals("Alle Kanale", StringComparison.OrdinalIgnoreCase)
          || said.Equals("Alle Kanäle", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Whether standing on a running programme and pressing sends the viewer to it. The reference's
    /// <c>ZapFromGuide</c>, on by default and worded there as "If enabled, OK on currently running programs tunes
    /// the channel" - which is what the guide's second press does.
    /// </summary>
    [JsonPropertyName("zap_from_guide")]
    public bool ZapFromGuide { get; set; } = true;

    /// <summary>
    /// How long a viewer is left alone after the last digit before the number they typed is taken as a channel.
    /// The reference's <c>ZapTimeout</c>: "Configures the delay until tuning starts".
    /// </summary>
    [JsonPropertyName("zap_timeout_seconds")]
    public double ZapTimeoutSeconds { get; set; } = DEFAULT_ZAP_TIMEOUT_SECONDS;

    /// <summary>
    /// Whether a typed number is a position in the bouquet or the number printed on a channel. The reference's
    /// <c>ZapByChannelIndex</c>, which it implemented by renumbering the bouquet's channels one to n before the
    /// lookup (<c>Helpers/ChannelContext.cs:190-194</c>) - so on this screen it decides which of the two the
    /// digits are read as.
    /// </summary>
    [JsonPropertyName("zap_by_channel_index")]
    public bool ZapByChannelIndex { get; set; } = true;

    /// <summary>
    /// Whether the watching screen starts a picture as it opens. The reference's <c>AutoStartTV</c>, which tuned the
    /// channel its navigation stood on the moment its television home opened with nothing else playing
    /// (<c>Models/SlimTvClientModel.cs:553-565</c>) and which it defaulted to <b>off</b>. That default is kept: the
    /// reference's home state was a place a viewer fell into by walking sideways and its own manual said so, and a
    /// box that starts making noise before anybody has chosen anything is a box nobody wants in a bedroom. The
    /// screen this applies to is the one the tile called "Live TV" opens, and only while nothing has been chosen -
    /// somebody who came out of a guide with a channel in hand is already being shown that channel.
    /// </summary>
    [JsonPropertyName("auto_start_tv")]
    public bool AutoStartTV { get; set; }

    /// <summary>
    /// How many hours the guide shows, with the nonsense taken out: a window has to be at least half an hour wide
    /// and no wider than a working day, because past that the hour labels overlap and a guide stops being a guide.
    /// </summary>
    public double VisibleHoursOr
    {
      get
      {
        double hours = VisibleHours;
        if (double.IsNaN(hours) || double.IsInfinity(hours))
          return DEFAULT_VISIBLE_HOURS;
        if (hours < MIN_VISIBLE_HOURS || hours > MAX_VISIBLE_HOURS)
          return DEFAULT_VISIBLE_HOURS;
        return hours;
      }
    }

    /// <summary>How many channels fit on the screen at once, with the nonsense taken out.</summary>
    public int VisibleRowsOr
    {
      get
      {
        int rows = VisibleRows;
        return rows < 1 || rows > 40 ? DEFAULT_VISIBLE_ROWS : rows;
      }
    }

    /// <summary>How long a viewer gets to finish typing a channel number, with the nonsense taken out.</summary>
    public double ZapTimeoutSecondsOr
    {
      get
      {
        double seconds = ZapTimeoutSeconds;
        if (double.IsNaN(seconds) || double.IsInfinity(seconds))
          return DEFAULT_ZAP_TIMEOUT_SECONDS;
        if (seconds < 0.5 || seconds > 30)
          return DEFAULT_ZAP_TIMEOUT_SECONDS;
        return seconds;
      }
    }

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

        // The same rule for the shape of the screens: a hand-written file with forty hours in it is a mistake, and
        // the defaults are the answer to a field that cannot mean anything. Normalised here rather than at every
        // place that draws a grid, so that a screen asking for a window and a screen asking for a screenshot of the
        // same window cannot disagree.
        read.VisibleHours = read.VisibleHoursOr;
        read.VisibleRows = read.VisibleRowsOr;
        read.ZapTimeoutSeconds = read.ZapTimeoutSecondsOr;
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
