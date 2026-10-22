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
using System.Collections.Generic;
using System.Globalization;

namespace MediaPortal.Frontend.Api
{
  /// <summary>
  /// Whether a channel is watched with pictures or listened to.
  /// </summary>
  /// <remarks>
  /// The reference had the same two answers under the same names (<c>SlimTv.Interfaces.Items.MediaType</c>:
  /// <c>TV</c> and <c>Radio</c>), and had the same problem: nothing on the wire says it, so each provider
  /// worked it out for itself from what the backend calls the service.
  /// </remarks>
  public enum TvMediaType
  {
    Television,
    Radio
  }

  /// <summary>
  /// One named bundle of channels, which is what a backend calls a bouquet and the reference called a channel
  /// group (<c>IChannelGroup</c>: an id, a name, a media type and a sort order).
  /// </summary>
  /// <remarks>
  /// The media type is not stored here on purpose. A TVHeadend tag is a list of channels and nothing else, so a
  /// bouquet is only television or radio through the channels inside it - which is also why a bouquet holding
  /// both is drawn as it comes rather than being quietly filed under one of them.
  /// </remarks>
  public sealed record TvChannelGroup(string Id, string Name, int ChannelCount)
  {
    /// <summary>Whether anything is in this bundle. An empty tag is a backend's leftover, not a choice.</summary>
    public bool IsEmpty
    {
      get { return ChannelCount == 0; }
    }
  }

  /// <summary>
  /// One channel, as it is listed by the backend: its number, its name, where to point the player at it, and
  /// which bundles it belongs to.
  /// </summary>
  /// <remarks>
  /// <para>
  /// <see cref="Number"/> is the channel's number as the backend sorts it - the number a viewer says and the
  /// number a remote's digits mean - and is not the same thing as <see cref="Id"/>, which is the string the
  /// backend's programme data is filed under. The reference kept both for the same reason
  /// (<c>IChannel.ChannelNumber</c> beside <c>IChannel.ChannelId</c>).
  /// </para>
  /// <para>
  /// <see cref="StreamUrl"/> is the address the player is pointed at. A channel without one is a channel this
  /// backend cannot show - the reference called the same difference a channel you can only look up in the
  /// guide.
  /// </para>
  /// </remarks>
  public sealed record TvChannel(int Number, string Name, string Id, Uri? StreamUrl,
      IReadOnlyList<string> GroupIds, TvMediaType MediaType)
  {
    /// <summary>Whether this channel can be watched or listened to right now.</summary>
    public bool IsWatchable
    {
      get { return StreamUrl != null; }
    }

    /// <summary>Whether this is one to listen to.</summary>
    public bool IsRadio
    {
      get { return MediaType == TvMediaType.Radio; }
    }

    /// <summary>The number and the name together, which is how a channel is spoken about on a television.</summary>
    public string Describe()
    {
      return Number > 0
          ? string.Format(CultureInfo.InvariantCulture, "{0} {1}", Number, Name)
          : Name;
    }
  }

  /// <summary>
  /// Which episode a programme is, when the backend was told. Every field is nullable because most of the time
  /// it was not told, and a guide that invented "S01E01" for a weather report would be worse than one that says
  /// nothing.
  /// </summary>
  public sealed record TvEpisode(int? Season, int? Episode, int? Part, string? OnScreen)
  {
    /// <summary>Whether any part of the episode was said.</summary>
    public bool IsKnown
    {
      get { return Season.HasValue || Episode.HasValue || Part.HasValue || !string.IsNullOrWhiteSpace(OnScreen); }
    }

    /// <summary>
    /// The episode in the short form a guide column has room for. What the backend wrote on the screen itself
    /// wins if it wrote anything, because that is what the broadcaster meant the viewer to read.
    /// </summary>
    public string Describe()
    {
      if (!string.IsNullOrWhiteSpace(OnScreen))
        return OnScreen.Trim();

      if (!Season.HasValue && !Episode.HasValue && !Part.HasValue)
        return "";

      // The reference wrote season and episode out in words ("Season 3, Episode 4"); on a guide bar there is
      // room for the abbreviation, and the detail screen writes the whole thing again from these same fields.
      System.Text.StringBuilder said = new System.Text.StringBuilder();
      if (Season.HasValue)
        said.Append("S").Append(Season.Value.ToString("00", CultureInfo.InvariantCulture));
      if (Episode.HasValue)
        said.Append(said.Length > 0 ? "E" : "Episode ").Append(Episode.Value.ToString("00", CultureInfo.InvariantCulture));
      if (Part.HasValue)
        said.Append(said.Length > 0 ? " part " : "Part ").Append(Part.Value.ToString(CultureInfo.InvariantCulture));
      return said.ToString();
    }
  }

  /// <summary>
  /// One programme in the guide, which is what the reference called a <c>IProgram</c> and TVHeadend an event.
  /// </summary>
  /// <remarks>
  /// <para>
  /// <see cref="Start"/> and <see cref="Stop"/> are instants, not wall-clock readings: the backend counts in
  /// seconds since 1970 in UTC and the client turns those into offsets the moment they arrive, so a guide laid
  /// out by subtracting them is right whether the household is in Lisbon or Helsinki. A screen that wants the
  /// clock the viewer's watch shows asks <see cref="StartLocal"/>.
  /// </para>
  /// <para>
  /// <see cref="ScheduleId"/> and <see cref="ScheduleState"/> are the backend telling, unasked, that this
  /// programme is tied to a recording - which is how a guide marks a programme somebody already asked to be
  /// saved without asking a second question about every programme in the window.
  /// </para>
  /// </remarks>
  public sealed record TvProgram(long Id, string? ChannelId, string? ChannelName, DateTimeOffset Start,
      DateTimeOffset Stop, string Title, string? Subtitle, string? Summary, string? Description,
      int ContentType, IReadOnlyList<string> Genres, TvEpisode? Episode, int? AgeRating, int? StarRating,
      string? ImageUrl, long? NextId, string? SeriesLink, string? ScheduleId, string? ScheduleState)
  {
    /// <summary>How long the programme was announced for.</summary>
    public TimeSpan Duration
    {
      get { return Stop - Start; }
    }

    /// <summary>The hour and minute the programme begins, on the viewer's own clock.</summary>
    public string StartLocal
    {
      get { return Start.LocalDateTime.ToString("t", CultureInfo.CurrentCulture); }
    }

    /// <summary>The hour and minute it ends, on the viewer's own clock.</summary>
    public string StopLocal
    {
      get { return Stop.LocalDateTime.ToString("t", CultureInfo.CurrentCulture); }
    }

    /// <summary>The day it begins on, in the short form a guide header uses.</summary>
    public string DayLocal
    {
      get { return Start.LocalDateTime.ToString("ddd", CultureInfo.CurrentCulture); }
    }

    /// <summary>Whether this is running at <paramref name="moment"/>.</summary>
    public bool IsOnAt(DateTimeOffset moment)
    {
      return Start <= moment && Stop > moment;
    }

    /// <summary>Whether it is over. A programme that has not started is not over.</summary>
    public bool IsOver(DateTimeOffset moment)
    {
      return Stop <= moment;
    }

    /// <summary>
    /// How far through it is, from 0 to 1, for the strip that runs under a programme that is on. Outside a
    /// programme the answer is 0 or 1 rather than a negative or an over-full number, so a bar cannot grow past
    /// its own box.
    /// </summary>
    public double ProgressAt(DateTimeOffset moment)
    {
      TimeSpan total = Stop - Start;
      if (total <= TimeSpan.Zero)
        return 0;
      if (moment <= Start)
        return 0;
      if (moment >= Stop)
        return 1;
      return (moment - Start).Ticks / (double) total.Ticks;
    }

    /// <summary>
    /// Whether a recording of this was asked for. The backend says so by attaching a schedule to the programme;
    /// a state of "scheduled", "recording", "compl" or "error" all mean somebody asked.
    /// </summary>
    public bool IsRecorded
    {
      get { return !string.IsNullOrWhiteSpace(ScheduleId); }
    }

    /// <summary>
    /// Whether the recording of this is happening at the moment, which is worth a different mark on the screen
    /// than a recording still to come.
    /// </summary>
    public bool IsBeingRecorded
    {
      get
      {
        return IsRecorded && string.Equals(ScheduleState, "recording", StringComparison.OrdinalIgnoreCase);
      }
    }

    /// <summary>The first genre the backend named, or empty. A guide colours by this one.</summary>
    public string PrimaryGenre
    {
      get { return Genres.Count > 0 ? Genres[0] : ""; }
    }
  }

  /// <summary>
  /// What is running on one channel now and what comes after it, which is what an on-screen strip and every
  /// channel list beside a picture is made of. Either half may be missing: at three in the morning a small
  /// sender's schedule has holes, and a guide that invented a programme to fill one would be lying.
  /// </summary>
  public sealed record TvNowAndNext(TvProgram? Now, TvProgram? Next)
  {
    /// <summary>Whether there is anything at all to say about this channel.</summary>
    public bool HasAnything
    {
      get { return Now != null || Next != null; }
    }

    /// <summary>The running programme, or the sentence a screen shows instead of it.</summary>
    public string NowTitle
    {
      get { return Now?.Title ?? "nothing on this channel right now"; }
    }

    /// <summary>The one after it, or nothing to draw beside it.</summary>
    public string NextTitle
    {
      get { return Next?.Title ?? ""; }
    }
  }

  /// <summary>
  /// The colour a guide wears per genre, in the eight colours the reference chose.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The reference kept eight colours in its settings (<c>SlimTvGenreColorSettings</c>: film, series,
  /// documentary, music, children, news, sport, and everything else) and looked a programme's colour up by its
  /// EPG genre id. A TVHeadend does not send a genre id a screen can switch on - it sends a content type out of
  /// its own table and, on the richer queries, the genre's name in the viewer's language - so the lookup is by
  /// the words instead, which is also what a viewer sees and can therefore check.
  /// </para>
  /// <para>
  /// Matching on words has to be forgiving in both directions: the same programme is "Film" in German and
  /// "movie" in English, and a backend in between languages writes "Movie/Drama" for what is one of the
  /// reference's two first colours. Hence the substring search, and hence the order: the more specific words
  /// are tried before the general ones, so a "Children's programme" is blue and not the grey of everything else.
  /// </para>
  /// </remarks>
  public static class TvGenreColors
  {
    /// <summary>Film. The reference's own #7532a8.</summary>
    public const string MOVIE = "#7532a8";

    /// <summary>Series and soaps. #ed7d31.</summary>
    public const string SERIES = "#ed7d31";

    /// <summary>Documentary. #4f7a32.</summary>
    public const string DOCUMENTARY = "#4f7a32";

    /// <summary>Music. #c89800.</summary>
    public const string MUSIC = "#c89800";

    /// <summary>Children. #4e93d2.</summary>
    public const string CHILDREN = "#4e93d2";

    /// <summary>News and current affairs. #c03636.</summary>
    public const string NEWS = "#c03636";

    /// <summary>Sport. #00817e.</summary>

    public const string SPORT = "#00817e";

    /// <summary>
    /// Everything the eight do not name, which is what the reference's eighth colour was for. It had no default
    /// of its own - an empty string, which the skin then drew as its own background - so this is a grey of the
    /// same quietness rather than a colour nobody chose.
    /// </summary>
    public const string OTHER = "#4a4a52";

    /// <summary>
    /// The colour for one genre's name, in the language the backend happened to answer in.
    /// </summary>
    public static string ForGenre(string? genreName)
    {
      string said = (genreName ?? "").Trim().ToLowerInvariant();
      if (said.Length == 0)
        return OTHER;

      // Ordered most specific first. "Children" is checked before "drama" because a children's drama carries
      // both words and the reference painted it blue.
      if (Contains(said, "child", "kids", "kinder", "junior", "young"))
        return CHILDREN;
      if (Contains(said, "documentary", "dokumentation", "doku", "fact", "science", "nature", "wissen"))
        return DOCUMENTARY;
      if (Contains(said, "movie", "film", "cinema", "spielfilm"))
        return MOVIE;
      if (Contains(said, "sport", "sportreport"))
        return SPORT;
      if (Contains(said, "news", "nachricht", "current affairs", "aktuelle", "magazin", "magazine"))
        return NEWS;
      if (Contains(said, "music", "musik", "concert", "konzert", "dance"))
        return MUSIC;
      if (Contains(said, "series", "serie", "soap", "drama", "show", "sitcom", "telenovela"))
        return SERIES;

      return OTHER;
    }

    private static bool Contains(string said, params string[] words)
    {
      foreach (string word in words)
      {
        if (said.Contains(word, StringComparison.Ordinal))
          return true;
      }
      return false;
    }
  }
}
