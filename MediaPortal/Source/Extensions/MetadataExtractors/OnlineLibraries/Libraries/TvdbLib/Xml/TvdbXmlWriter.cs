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
using MediaPortal.Extensions.OnlineLibraries.Libraries.Common;
using MediaPortal.Extensions.OnlineLibraries.Libraries.TvdbLib.Data;
using MediaPortal.Extensions.OnlineLibraries.Libraries.TvdbLib.Data.Banner;
using System.IO;
using System.Xml.Linq;
using System.Drawing;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.TvdbLib.Xml
{
  /// <summary>
  /// Writes tvdb data to xml files
  /// </summary>
  internal class TvdbXmlWriter
  {
    /// <summary>
    /// TvdbXmlWriter constructor
    /// </summary>
    internal TvdbXmlWriter()
    {

    }

    /// <summary>
    /// Create the file contents
    /// </summary>
    /// <param name="languages">List of languages to store</param>
    /// <returns></returns>
    internal String CreateLanguageFile(List<TvdbLanguage> languages)
    {
      XElement xml = new XElement("Languages");
      foreach (TvdbLanguage l in languages)
      {
        xml.Add(new XElement("Language",
                   new XElement("name", l.Name),
                   new XElement("abbreviation", l.Abbriviation),
                   new XElement("id", l.Id))
               );
      }
      return xml.ToString();
    }

    /// <summary>
    /// Write the list of languages to file
    /// </summary>
    /// <param name="languages">List of languages to store</param>
    /// <param name="path">Path on disk</param>
    /// <returns>true if the file could be stored, false otherwise</returns>
    internal bool WriteLanguageFile(List<TvdbLanguage> languages, String path)
    {
      String fileContent = CreateLanguageFile(languages);
      try
      {
        FileInfo info = new FileInfo(path);
        if (!info.Directory.Exists) info.Directory.Create();
        File.WriteAllText(info.FullName, fileContent);
        return true;
      }
      catch (Exception)
      {
        return false;
      }
    }

    /// <summary>
    /// Create the file content for a list of mirrors
    /// </summary>
    /// <param name="mirrors">List of mirrors to store</param>
    /// <returns>xml content</returns>
    [Obsolete("Not used any more, however if won't delete the class since it could be useful at some point")]
    internal String CreateMirrorList(List<TvdbMirror> mirrors)
    {
      XElement xml = new XElement("Mirrors");
      foreach (TvdbMirror m in mirrors)
      {
        xml.Add(new XElement("Mirror",
                   new XElement("id", m.Id),
                   new XElement("mirrorpath", m.MirrorPath),
                   new XElement("typemask", m.TypeMask))
               );
      }
      return xml.ToString();
    }

    /// <summary>
    /// Write the xml file for the mirrors to file
    /// </summary>
    /// <param name="mirrors">List of mirrors to store</param>
    /// <param name="path">Path on disk</param>
    /// <returns>true if the file could be stored, false otherwise</returns>
    [Obsolete("Not used any more, however if won't delete the class since it could be useful at some point")]
    internal bool WriteMirrorFile(List<TvdbMirror> mirrors, String path)
    {
      String fileContent = CreateMirrorList(mirrors);
      try
      {
        FileInfo info = new FileInfo(path);
        if (!info.Directory.Exists) info.Directory.Create();
        File.WriteAllText(info.FullName, fileContent);
        return true;
      }
      catch (Exception)
      {
        return false;
      }
    }

    /// <summary>
    /// Create the file content for a list of actors
    /// </summary>
    /// <param name="actors">List of actors to store</param>
    /// <returns>xml content</returns>
    internal String CreateActorList(List<TvdbActor> actors)
    {
      XElement xml = new XElement("Actors");
      foreach (TvdbActor m in actors)
      {
        xml.Add(new XElement("Actor",
                   new XElement("id", m.Id),
                   new XElement("Image", m.ActorImage.BannerPath),
                   new XElement("Role", m.Role),
                   new XElement("SortOrder", m.SortOrder),
                   new XElement("Name", m.Name))
               );
      }
      return xml.ToString();
    }

    /// <summary>
    /// Write the xml file for the actors to file
    /// </summary>
    /// <param name="actors">List of actors to store</param>
    /// <param name="path">Path on disk</param>
    /// <returns>true if the file could be stored, false otherwise</returns>
    internal bool WriteActorFile(List<TvdbActor> actors, String path)
    {
      String fileContent = CreateActorList(actors);
      try
      {
        FileInfo info = new FileInfo(path);
        if (!info.Directory.Exists) info.Directory.Create();
        File.WriteAllText(info.FullName, fileContent);
        return true;
      }
      catch (Exception)
      {
        return false;
      }
    }

    /// <summary>
    /// Create the series content
    /// </summary>
    /// <param name="series">Series to store</param>
    /// <returns>xml content</returns>
    internal String CreateSeriesContent(TvdbSeries series)
    {
      XElement xml = new XElement("Data");

      xml.Add(new XElement("Series",
                  new XElement("id", series.Id),
                  new XElement("Actors", series.ActorsString),
                  new XElement("Airs_DayOfWeek", series.AirsDayOfWeek),
                  new XElement("Airs_Time", series.AirsTime),
                  new XElement("ContentRating", series.ContentRating),
                  new XElement("FirstAired", series.FirstAired),
                  new XElement("Genre", series.GenreString),
                  new XElement("IMDB_ID", series.ImdbId),
                  new XElement("Language", series.Language.Abbriviation),
                  new XElement("NetworkID", series.NetworkID),
                  new XElement("Network", series.Network),
                  new XElement("Overview", series.Overview),
                  new XElement("Rating", series.Rating),
                  new XElement("RatingCount", series.RatingCount),
                  new XElement("Runtime", series.Runtime),
                  new XElement("SeriesID", series.TvDotComId),
                  new XElement("SeriesName", series.SeriesName),
                  new XElement("Status", series.Status),
                  new XElement("banner", series.BannerPath ?? ""),
                  new XElement("fanart", series.FanartPath ?? ""),
                  new XElement("Poster", series.PosterPath ?? ""),
                  new XElement("lastupdated", Util.DotNetToUnix(series.LastUpdated)),
                  new XElement("zap2it_id", series.Zap2itId))
             );


      if (series.Episodes != null && series.EpisodesLoaded)
      {
        foreach (TvdbEpisode e in series.Episodes)
        {
          xml.Add(new XElement("Episode",
                  new XElement("id", e.Id),
                  new XElement("Combined_episodenumber", e.CombinedEpisodeNumber),
                  new XElement("Combined_season", e.CombinedSeason),
                  new XElement("DVD_chapter", e.DvdChapter != Util.NO_VALUE ? e.DvdChapter.ToString() : ""),
                  new XElement("DVD_discid", e.DvdDiscId != Util.NO_VALUE ? e.DvdDiscId.ToString() : ""),
                  new XElement("DVD_episodenumber", e.DvdEpisodeNumber != Util.NO_VALUE ? e.DvdEpisodeNumber.ToString() : ""),
                  new XElement("DVD_season", e.DvdSeason != Util.NO_VALUE ? e.DvdSeason.ToString() : ""),
                  new XElement("Director", e.DirectorsString),
                  new XElement("EpisodeName", e.EpisodeName),
                  new XElement("EpisodeNumber", e.EpisodeNumber),
                  new XElement("FirstAired", e.FirstAired),
                  new XElement("GuestStars", e.GuestStarsString),
                  new XElement("IMDB_ID", e.ImdbId),
                  new XElement("Language", e.Language.Abbriviation),
                  new XElement("Overview", e.Overview),
                  new XElement("ProductionCode", e.ProductionCode),
                  new XElement("Rating", e.Rating.ToString()),
                  new XElement("RatingCount", e.RatingCount.ToString()),
                  new XElement("SeasonNumber", e.SeasonNumber),
                  new XElement("Writer", e.WriterString),
                  new XElement("absolute_number", e.AbsoluteNumber),
                  new XElement("airsafter_season", e.AirsAfterSeason != Util.NO_VALUE ? e.AirsAfterSeason.ToString() : ""),
                  new XElement("airsbefore_episode", e.AirsBeforeEpisode != Util.NO_VALUE ? e.AirsBeforeEpisode.ToString() : ""),
                  new XElement("airsbefore_season", e.AirsBeforeSeason != Util.NO_VALUE ? e.AirsBeforeSeason.ToString() : ""),
                  new XElement("filename", e.BannerPath),
                  new XElement("lastupdated", Util.DotNetToUnix(e.LastUpdated)),
                  new XElement("seasonid", e.SeasonId),
                  new XElement("seriesid", e.SeriesId))
                 );

        }
      }
      return xml.ToString();
    }

    /// <summary>
    /// Write the series content to file
    /// </summary>
    /// <param name="series">Series to store</param>
    /// <param name="path">Path on disk</param>
    /// <returns>true if the file could be stored, false otherwise</returns>
    internal bool WriteSeriesContent(TvdbSeries series, String path)
    {
      String fileContent = CreateSeriesContent(series);
      try
      {
        FileInfo info = new FileInfo(path);
        if (!info.Directory.Exists) info.Directory.Create();
        File.WriteAllText(info.FullName, fileContent);
        return true;
      }
      catch (Exception)
      {
        return false;
      }
    }

    /// <summary>
    /// Create the series banner content
    /// </summary>
    /// <param name="bannerList">List of banners to store</param>
    /// <returns>xml content</returns>
    internal String CreateSeriesBannerContent(List<TvdbBanner> bannerList)
    {
      XElement xml = new XElement("Banners");

      foreach (TvdbBanner b in bannerList)
      {
        XElement banner = new XElement("Banner");
        banner.Add(new XElement("id", b.Id));
        banner.Add(new XElement("BannerPath", b.BannerPath));
        banner.Add(new XElement("LastUpdated", Util.DotNetToUnix(b.LastUpdated)));
        if (b.GetType() == typeof(TvdbSeriesBanner))
        {
          TvdbSeriesBanner sb = (TvdbSeriesBanner)b;
          banner.Add(new XElement("BannerType", "series"));
          banner.Add(new XElement("BannerType2", sb.BannerType));
          banner.Add(new XElement("Language", (sb.Language != null ? sb.Language.Abbriviation : "")));
        }
        else if (b.GetType() == typeof(TvdbFanartBanner))
        {
          TvdbFanartBanner fb = (TvdbFanartBanner)b;
          banner.Add(new XElement("BannerType", "fanart"));
          banner.Add(new XElement("BannerType2", fb.Resolution.X + "x" + fb.Resolution.Y));
          if (fb.Colors != null && fb.Colors.Count == 0)
          {
            StringBuilder colorString = new StringBuilder();
            colorString.Append("|");
            foreach (Color c in fb.Colors)
            {
              colorString.Append(c.R);
              colorString.Append(",");
              colorString.Append(c.G);
              colorString.Append(",");
              colorString.Append(c.B);
              colorString.Append("|");
            }
            banner.Add(new XElement("Colors", colorString.ToString()));
          }
          else
          {
            banner.Add(new XElement("Colors", ""));
          }
          banner.Add(new XElement("VignettePath", fb.VignettePath));
          banner.Add(new XElement("ThumbnailPath", fb.ThumbPath));
          banner.Add(new XElement("Language", (fb.Language != null ? fb.Language.Abbriviation : "")));
          banner.Add(new XElement("SeriesName", fb.ContainsSeriesName.ToString()));
        }
        else if (b.GetType() == typeof(TvdbSeasonBanner))
        {
          TvdbSeasonBanner sb = (TvdbSeasonBanner)b;
          banner.Add(new XElement("BannerType", "season"));
          banner.Add(new XElement("BannerType2", sb.BannerType));
          banner.Add(new XElement("Language", (sb.Language != null ? sb.Language.Abbriviation : "")));
          banner.Add(new XElement("Season", sb.Season));
        }
        else if (b.GetType() == typeof(TvdbPosterBanner))
        {
          TvdbPosterBanner pb = (TvdbPosterBanner)b;
          banner.Add(new XElement("BannerType", "Poster"));
          banner.Add(new XElement("BannerType2", pb.Resolution.X + "x" + pb.Resolution.Y));
          banner.Add(new XElement("Language", (pb.Language != null ? pb.Language.Abbriviation : "")));
        }
        else
        {
          //this shouldn't happen, it's an invalid banner type (maybe new?) -> don't store it
          continue;
        }
        xml.Add(banner);
      }

      return xml.ToString();
    }

    /// <summary>
    /// Write the series banner contents to xml file
    /// </summary>
    /// <param name="bannerList">Bannerlist to store</param>
    /// <param name="path">Path on disk</param>
    /// <returns>true if the file could be stored, false otherwise</returns>
    internal bool WriteSeriesBannerContent(List<TvdbBanner> bannerList, String path)
    {
      String fileContent = CreateSeriesBannerContent(bannerList);
      try
      {
        FileInfo info = new FileInfo(path);
        if (!info.Directory.Exists) info.Directory.Create();
        File.WriteAllText(info.FullName, fileContent);
        return true;
      }
      catch (Exception)
      {
        return false;
      }
    }

    /// <summary>
    /// Create the xml content to save a TvdbUser to file
    /// </summary>
    /// <param name="user">User to store</param>
    /// <returns>xml content</returns>
    internal String CreateUserData(TvdbUser user)
    {
      XElement xml = new XElement("Data");

      StringBuilder favBuilder = new StringBuilder();
      if (user.UserFavorites != null && user.UserFavorites.Count > 0)
      {
        foreach (int f in user.UserFavorites)
        {
          favBuilder.Append(f);
          favBuilder.Append(",");
        }
      }

      XElement preferred = new XElement("PreferredLanguage");
      if (user.UserPreferredLanguage != null)
      {
        preferred.Add(new XAttribute("Id", user.UserPreferredLanguage.Id));
        preferred.Add(new XAttribute("Abbreviation", user.UserPreferredLanguage.Abbriviation));
        preferred.Add(new XAttribute("Name", user.UserPreferredLanguage.Name));
      }

      xml.Add(new XElement("User",
                  new XElement("Name", user.UserName),
                  new XElement("Identifier", user.UserIdentifier),
                  new XElement("Favorites", favBuilder.ToString()),
                  preferred
             ));

      return xml.ToString();
    }

    /// <summary>
    /// Write the user data to file
    /// </summary>
    /// <param name="user">User to store</param>
    /// <param name="path">Path on disk</param>
    /// <returns>true if the file could be stored, false otherwise</returns>
    internal bool WriteUserData(TvdbUser user, String path)
    {
      String fileContent = CreateUserData(user);
      try
      {
        FileInfo info = new FileInfo(path);
        if (!info.Directory.Exists) info.Directory.Create();
        File.WriteAllText(info.FullName, fileContent);
        return true;
      }
      catch (Exception)
      {
        return false;
      }
    }
  }
}
