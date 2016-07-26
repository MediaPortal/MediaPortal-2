/*
 *   TvdbLib: A library to retrieve information and media from http://thetvdb.com
 * 
 *   Copyright (C) 2008  Benjamin Gmeiner
 * 
 *   This program is free software: you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation, either version 3 of the License, or
 *   (at your option) any later version.
 *
 *   This program is distributed in the hope that it will be useful,
 *   but WITHOUT ANY WARRANTY; without even the implied warranty of
 *   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *   GNU General Public License for more details.
 *
 *   You should have received a copy of the GNU General Public License
 *   along with this program.  If not, see <http://www.gnu.org/licenses/>.
 * 
 */

using System;
using System.Collections.Generic;
using System.Text;
using MediaPortal.Extensions.OnlineLibraries.Libraries.TvdbLib.Data.Banner;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.TvdbLib.Data
{
  /// <summary>
  /// Class representing an episode with all the information that can be retrieved from http://thetvdb.com. <br/> 
  /// <br/>
  /// Those are: <br/>
  /// <![CDATA[
  ///      <id>332179</id> <br/>
  ///      <DVD_chapter></DVD_chapter> <br/>
  ///      <DVD_discid></DVD_discid> <br/>
  ///      <DVD_episodenumber></DVD_episodenumber> <br/>
  ///      <DVD_season></DVD_season> <br/>
  ///      <Director>|Joseph McGinty Nichol|</Director> <br/>
  ///      <EpisodeName>Chuck Versus the World</EpisodeName> <br/>
  ///      <EpisodeNumber>1</EpisodeNumber> <br/>
  ///      <FirstAired>2007-09-24</FirstAired> <br/>
  ///      <GuestStars>|Julia Ling|Vik Sahay|Mieko Hillman|</GuestStars> <br/>
  ///      <IMDB_ID></IMDB_ID> <br/>
  ///      <Language>English</Language> <br/>
  ///      <Overview>Chuck Bartowski is an average computer geek...</Overview> <br/>
  ///      <ProductionCode></ProductionCode> <br/>
  ///      <Rating>9.0</Rating> <br/>
  ///      <RatingCount>109</RatingCount> <br/>
  ///      <SeasonNumber>1</SeasonNumber> <br/>
  ///      <Writer>|Josh Schwartz|Chris Fedak|</Writer> <br/>
  ///      <absolute_number></absolute_number> <br/>
  ///      <airsafter_season></airsafter_season> <br/>
  ///      <airsbefore_episode></airsbefore_episode> <br/>
  ///      <airsbefore_season></airsbefore_season> <br/>
  ///      <filename>episodes/80348-332179.jpg</filename> <br/>
  ///      <lastupdated>1201292806</lastupdated> <br/>
  ///      <seasonid>27985</seasonid> <br/>
  ///      <seriesid>80348</seriesid> <br/>
  /// 
  /// ]]>
  /// Additionally the banner image is stored
  /// </summary>
  [Serializable]
  public class TvdbEpisode
  {

    #region private fields

    #endregion

    /// <summary>
    /// While one would think that the episode number would be a simple affair there are several different ways that someone might choose to number the episodes on this site episodes are numbered in the order they aired on TV. That being said the site does provide two alternative numbering methods. <br /> <br />
    /// 1. Absolute Episode Order <br />
    /// 2. DVD Release Order <br />
    /// <br />
    /// More information on the topic can be found at: http://thetvdb.com/wiki/index.php/Category:Episodes
    /// </summary>
    public enum EpisodeOrdering
    {
      /// <summary>
      /// Default order used by thetvdb
      /// </summary>
      DefaultOrder = 0,

      /// <summary>
      /// As everyone knows series can air on tv in an order completely different than the one intended by the series creator. Firefly being the most often discussed example on this site. Therefore we have provided a method for entering this "Correct" order. See also http://thetvdb.com/wiki/index.php/DVD_Order
      /// </summary>
      DvdOrder = 1,

      /// <summary>
      /// The standard for this site is the the primary episode number is representative of the shows aired order. But as any Anime fan will tell you Anime episodes are usually numbered without seasons and go from episode 1 to whatever the final episode is, often into the hundreds. Most western broadcasters however do break these shows into seasons, so in order to accommodate this alternate numbering scheme an additional field Absolute Number is available. While this system is primarily intended for Anime series that don't really have seasons, it will work for any program. To use this interface there is no alternate Season number only the absolute episode number. Numbering continues on from Season to Season. So if Season 1 ends with 25 then Season 2 begins with 26. So for example Bleach is currently in it's "sixth Season" but Season 5 episode 14 Shock! The Father's True Character is actually episode 111. 
      /// </summary>
      AbsoluteOrder = 2
    }

    /// <summary>
    /// Default constructor for the TvdbEpisode class
    /// </summary>
    public TvdbEpisode()
    {

    }

    /// <summary>
    /// Returns a short description of the episode (e.g. 1x20 Episodename)
    /// </summary>
    /// <returns>short description of the episode</returns>
    public override String ToString()
    {
      return SeasonNumber + "x" + EpisodeNumber + (EpisodeName != null ? " " + EpisodeName : "");
    }

    #region specials

    /// <summary>
    /// if the episode is a special episode -> Before which Season did
    /// it air
    /// </summary>
    public int AirsBeforeSeason { get; set; }

    /// <summary>
    /// if the episode is a special episode -> Before which episode did
    /// it air
    /// </summary>
    public int AirsBeforeEpisode { get; set; }

    /// <summary>
    /// if the episode is a special episode -> After which Season did
    /// it air
    /// </summary>
    public int AirsAfterSeason { get; set; }

    /// <summary>
    /// Is the episode a special episode
    /// 
    /// The fields airsafter_season, airsbefore_episode, and airsbefore_season will only be included when the episode is listed as a special. Specials are also listed as being in Season 0, so they're easy to identify and sort.
    /// </summary>
    public bool IsSpecial
    {
      get
      {
        return (SeasonNumber == 0);
      }
    }

    #endregion

    #region DVD

    /// <summary>
    /// Which DVD Season is this episode
    /// </summary>
    public int DvdSeason { get; set; }

    /// <summary>
    /// The Dvd Episode Number
    /// </summary>
    public double DvdEpisodeNumber { get; set; }

    /// <summary>
    /// The DVD Disc Id
    /// </summary>
    public int DvdDiscId { get; set; }

    /// <summary>
    /// The chapter of this episode on the dvd
    /// </summary>
    public int DvdChapter { get; set; }

    #endregion

    #region other tvdb information

    /// <summary>
    /// unique tvdb Id of this episode
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Id of series this episode belongs to
    /// </summary>
    public int SeriesId { get; set; }

    /// <summary>
    /// Id of Season this episode belong to
    /// </summary>
    public int SeasonId { get; set; }

    /// <summary>
    /// When was the episode last updated
    /// </summary>
    public DateTime LastUpdated { get; set; }

    /// <summary>
    /// Path to the banner image on http://thetvdb.com
    /// </summary>
    public string BannerPath { get; set; }


    /// <summary>
    /// The absolute number of the episode
    /// </summary>
    public int AbsoluteNumber { get; set; }

    /// <summary>
    /// List of writers for this episode
    /// </summary>
    public List<string> Writer { get; set; }

    /// <summary>
    /// Season number of this episode
    /// </summary>
    public int SeasonNumber { get; set; }

    /// <summary>
    /// Rating for this episode
    /// </summary>
    public double Rating { get; set; }

    /// <summary>
    /// Rating count for this episode
    /// </summary>
    public int RatingCount { get; set; }

    /// <summary>
    /// Production code for this episode
    /// </summary>
    public string ProductionCode { get; set; }

    /// <summary>
    /// Overview of this episode
    /// </summary>
    public string Overview { get; set; }

    /// <summary>
    /// Language of this episode
    /// </summary>
    public TvdbLanguage Language { get; set; }

    /// <summary>
    /// Imdb number of this episode
    /// </summary>
    public string ImdbId { get; set; }

    /// <summary>
    /// List of guest stars that appeared in this episode
    /// </summary>
    public List<string> GuestStars { get; set; }

    /// <summary>
    /// When did the episode air first
    /// </summary>
    public DateTime FirstAired { get; set; }

    /// <summary>
    /// Episode number
    /// </summary>
    public int EpisodeNumber { get; set; }

    /// <summary>
    /// Name of the episode
    /// </summary>
    public string EpisodeName { get; set; }

    /// <summary>
    /// List of directors for this episode
    /// </summary>
    public List<string> Directors { get; set; }

    /// <summary>
    /// n/a
    /// </summary>
    public double CombinedSeason { get; set; }

    /// <summary>
    /// n/a
    /// </summary>
    public double CombinedEpisodeNumber { get; set; }

    #endregion


    /// <summary>
    /// Formatted String of writers for this episode in the 
    /// format | writer1 | writer2 | writer3 |
    /// </summary>
    public String WriterString
    {
      get
      {
        if (Writer == null || Writer.Count == 0) return "";
        StringBuilder retString = new StringBuilder();
        retString.Append("|");
        foreach (String s in Writer)
        {
          retString.Append(s);
          retString.Append("|");
        }
        return retString.ToString();
      }
    }

    /// <summary>
    /// Formatted String of guest stars that appeared during this episode in the 
    /// format | gueststar1 | gueststar2 | gueststar3 |
    /// </summary>
    public String GuestStarsString
    {
      get
      {
        if (GuestStars == null || GuestStars.Count == 0) return "";
        StringBuilder retString = new StringBuilder();
        retString.Append("|");
        foreach (String s in GuestStars)
        {
          retString.Append(s);
          retString.Append("|");
        }
        return retString.ToString();
      }
    }



    /// <summary>
    /// Formatted String of directors of this episode in the 
    /// format | director1 | director2 | director3 |
    /// </summary>
    public String DirectorsString
    {
      get
      {
        if (Directors == null || Directors.Count == 0) return "";
        StringBuilder retString = new StringBuilder();
        retString.Append("|");
        foreach (String s in Directors)
        {
          retString.Append(s);
          retString.Append("|");
        }
        return retString.ToString();
      }
    }

    /// <summary>
    /// The episode image banner
    /// </summary>
    public TvdbEpisodeBanner Banner { get; set; }

    /// <summary>
    /// Updates all information of this episode from the given
    /// episode...
    /// </summary>
    /// <param name="episode">new episode</param>
    internal void UpdateEpisodeInfo(TvdbEpisode episode)
    {
      LastUpdated = episode.LastUpdated;
      BannerPath = episode.BannerPath;
      Banner = episode.Banner;
      AbsoluteNumber = episode.AbsoluteNumber;
      CombinedEpisodeNumber = episode.CombinedEpisodeNumber;
      CombinedSeason = episode.CombinedSeason;
      Directors = episode.Directors;
      DvdChapter = episode.DvdChapter;
      DvdDiscId = episode.DvdDiscId;
      DvdEpisodeNumber = episode.DvdEpisodeNumber;
      DvdSeason = episode.DvdSeason;
      EpisodeName = episode.EpisodeName;
      EpisodeNumber = episode.EpisodeNumber;
      FirstAired = episode.FirstAired;
      GuestStars = episode.GuestStars;
      ImdbId = episode.ImdbId;
      Language = episode.Language;
      Overview = episode.Overview;
      ProductionCode = episode.ProductionCode;
      Rating = episode.Rating;
      RatingCount = episode.RatingCount;
      SeasonId = episode.SeasonId;
      SeasonNumber = episode.SeasonNumber;
      Writer = episode.Writer;
    }
  }
}
