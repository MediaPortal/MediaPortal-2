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
using System.Linq;
using System.Text;
using TvdbLib.Data;
using System.IO;
using System.Xml.Linq;
using TvdbLib.Data.Banner;
using System.Drawing;

namespace TvdbLib.Xml
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
    /// <param name="_languages">List of languages to store</param>
    /// <returns></returns>
    internal String CreateLanguageFile(List<TvdbLanguage> _languages)
    {
      XElement xml = new XElement("Languages");
      foreach (TvdbLanguage l in _languages)
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
    /// <param name="_languages">List of languages to store</param>
    /// <param name="_path">Path on disk</param>
    /// <returns>true if the file could be stored, false otherwise</returns>
    internal bool WriteLanguageFile(List<TvdbLanguage> _languages, String _path)
    {
      String fileContent = CreateLanguageFile(_languages);
      try
      {
        FileInfo info = new FileInfo(_path);
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
    /// <param name="_mirrors">List of mirrors to store</param>
    /// <returns>xml content</returns>
    [Obsolete("Not used any more, however if won't delete the class since it could be useful at some point")]
    internal String CreateMirrorList(List<TvdbMirror> _mirrors)
    {
      XElement xml = new XElement("Mirrors");
      foreach (TvdbMirror m in _mirrors)
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
    /// <param name="_mirrors">List of mirrors to store</param>
    /// <param name="_path">Path on disk</param>
    /// <returns>true if the file could be stored, false otherwise</returns>
    [Obsolete("Not used any more, however if won't delete the class since it could be useful at some point")]
    internal bool WriteMirrorFile(List<TvdbMirror> _mirrors, String _path)
    {
      String fileContent = CreateMirrorList(_mirrors);
      try
      {
        FileInfo info = new FileInfo(_path);
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
    /// <param name="_actors">List of actors to store</param>
    /// <returns>xml content</returns>
    internal String CreateActorList(List<TvdbActor> _actors)
    {
      XElement xml = new XElement("Actors");
      foreach (TvdbActor m in _actors)
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
    /// <param name="_actors">List of actors to store</param>
    /// <param name="_path">Path on disk</param>
    /// <returns>true if the file could be stored, false otherwise</returns>
    internal bool WriteActorFile(List<TvdbActor> _actors, String _path)
    {
      String fileContent = CreateActorList(_actors);
      try
      {
        FileInfo info = new FileInfo(_path);
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
    /// <param name="_series">Series to store</param>
    /// <returns>xml content</returns>
    internal String CreateSeriesContent(TvdbSeries _series)
    {
      XElement xml = new XElement("Data");

      xml.Add(new XElement("Series",
                  new XElement("id", _series.Id),
                  new XElement("Actors", _series.ActorsString),
                  new XElement("Airs_DayOfWeek", _series.AirsDayOfWeek),
                  new XElement("Airs_Time", _series.AirsTime),
                  new XElement("ContentRating", _series.ContentRating),
                  new XElement("FirstAired", _series.FirstAired),
                  new XElement("Genre", _series.GenreString),
                  new XElement("IMDB_ID", _series.ImdbId),
                  new XElement("Language", _series.Language.Abbriviation),
                  new XElement("Network", _series.Network),
                  new XElement("Overview", _series.Overview),
                  new XElement("Rating", _series.Rating),
                  new XElement("Runtime", _series.Runtime),
                  new XElement("SeriesID", _series.TvDotComId),
                  new XElement("SeriesName", _series.SeriesName),
                  new XElement("Status", _series.Status),
                  new XElement("banner", _series.BannerPath != null ? _series.BannerPath : ""),
                  new XElement("fanart", _series.FanartPath != null ? _series.FanartPath : ""),
                  new XElement("poster", _series.PosterPath != null ? _series.PosterPath : ""),
                  new XElement("lastupdated", Util.DotNetToUnix(_series.LastUpdated)),
                  new XElement("zap2it_id", _series.Zap2itId))
             );


      if (_series.Episodes != null && _series.EpisodesLoaded)
      {
        foreach (TvdbEpisode e in _series.Episodes)
        {
          xml.Add(new XElement("Episode",
                  new XElement("id", e.Id),
                  new XElement("Combined_episodenumber", e.CombinedEpisodeNumber),
                  new XElement("Combined_season", e.CombinedSeason),
                  new XElement("DVD_chapter", e.DvdChapter != -99 ? e.DvdChapter.ToString() : ""),
                  new XElement("DVD_discid", e.DvdDiscId != -99 ? e.DvdDiscId.ToString() : ""),
                  new XElement("DVD_episodenumber", e.DvdEpisodeNumber != -99 ? e.DvdEpisodeNumber.ToString() : ""),
                  new XElement("DVD_season", e.DvdSeason != -99 ? e.DvdSeason.ToString() : ""),
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
                  new XElement("SeasonNumber", e.SeasonNumber),
                  new XElement("Writer", e.WriterString),
                  new XElement("absolute_number", e.AbsoluteNumber),
                  new XElement("airsafter_season", e.AirsAfterSeason != -99 ? e.AirsAfterSeason.ToString() : ""),
                  new XElement("airsbefore_episode", e.AirsBeforeEpisode != -99 ? e.AirsBeforeEpisode.ToString() : ""),
                  new XElement("airsbefore_season", e.AirsBeforeSeason != -99 ? e.AirsBeforeSeason.ToString() : ""),
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
    /// <param name="_series">Series to store</param>
    /// <param name="_path">Path on disk</param>
    /// <returns>true if the file could be stored, false otherwise</returns>
    internal bool WriteSeriesContent(TvdbSeries _series, String _path)
    {
      String fileContent = CreateSeriesContent(_series);
      try
      {
        FileInfo info = new FileInfo(_path);
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
    /// <param name="_bannerList">List of banners to store</param>
    /// <returns>xml content</returns>
    internal String CreateSeriesBannerContent(List<TvdbBanner> _bannerList)
    {
      XElement xml = new XElement("Banners");

      foreach (TvdbBanner b in _bannerList)
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
          banner.Add(new XElement("BannerType", "poster"));
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
    /// <param name="_bannerList">Bannerlist to store</param>
    /// <param name="_path">Path on disk</param>
    /// <returns>true if the file could be stored, false otherwise</returns>
    internal bool WriteSeriesBannerContent(List<TvdbBanner> _bannerList, String _path)
    {
      String fileContent = CreateSeriesBannerContent(_bannerList);
      try
      {
        FileInfo info = new FileInfo(_path);
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
    /// <param name="_user">User to store</param>
    /// <returns>xml content</returns>
    internal String CreateUserData(TvdbUser _user)
    {
      XElement xml = new XElement("Data");

      StringBuilder favBuilder = new StringBuilder();
      if (_user.UserFavorites != null && _user.UserFavorites.Count > 0)
      {
        foreach (int f in _user.UserFavorites)
        {
          favBuilder.Append(f);
          favBuilder.Append(",");
        }
      }

      XElement preferred = new XElement("PreferredLanguage");
      if (_user.UserPreferredLanguage != null)
      {
        preferred.Add(new XAttribute("Id", _user.UserPreferredLanguage.Id));
        preferred.Add(new XAttribute("Abbriviation", _user.UserPreferredLanguage.Abbriviation));
        preferred.Add(new XAttribute("Name", _user.UserPreferredLanguage.Name));
      }

      xml.Add(new XElement("User",
                  new XElement("Name", _user.UserName),
                  new XElement("Identifier", _user.UserIdentifier),
                  new XElement("Favorites", favBuilder.ToString()),
                  preferred
             ));

      return xml.ToString();
    }

    /// <summary>
    /// Write the user data to file
    /// </summary>
    /// <param name="_user">User to store</param>
    /// <param name="_path">Path on disk</param>
    /// <returns>true if the file could be stored, false otherwise</returns>
    internal bool WriteUserData(TvdbUser _user, String _path)
    {
      String fileContent = CreateUserData(_user);
      try
      {
        FileInfo info = new FileInfo(_path);
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
