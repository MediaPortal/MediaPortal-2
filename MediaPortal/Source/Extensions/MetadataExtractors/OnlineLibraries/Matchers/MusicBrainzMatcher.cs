#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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

using MediaPortal.Common;
using MediaPortal.Common.FanArt;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.PathManager;
using MediaPortal.Extensions.OnlineLibraries.Libraries.MusicBrainzV2.Data;
using MediaPortal.Extensions.OnlineLibraries.Wrappers;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace MediaPortal.Extensions.OnlineLibraries.Matchers
{
  public class MusicBrainzMatcher : MusicMatcher<TrackImage, string>
  {
    #region Static instance

    public static MusicBrainzMatcher Instance
    {
      get { return ServiceRegistration.Get<MusicBrainzMatcher>(); }
    }

    #endregion

    #region Constants

    public static string CACHE_PATH = ServiceRegistration.Get<IPathManager>().GetPath(@"<DATA>\MusicBrainz\");
    protected static TimeSpan MAX_MEMCACHE_DURATION = TimeSpan.FromMinutes(10);

    #endregion

    #region Init

    public MusicBrainzMatcher() :
      base(CACHE_PATH, MAX_MEMCACHE_DURATION, false)
    {
      //TODO: Disabled for now. Very slow response times (up to 30 seconds, maybe more).
      Enabled = false;
    }

    public override Task<bool> InitWrapperAsync(bool useHttps)
    {
      try
      {
        MusicBrainzWrapper wrapper = new MusicBrainzWrapper();
        // Try to lookup online content in the configured language
        string lang = new RegionInfo(PreferredLanguageCulture).TwoLetterISORegionName;
        wrapper.SetPreferredLanguage(lang);
        if (wrapper.Init(CACHE_PATH, useHttps))
        {
          _wrapper = wrapper;
          return Task.FromResult(true);
        }
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("MusicBrainzMatcher: Error initializing wrapper", ex);
      }
      return Task.FromResult(false);
    }

    #endregion

    #region Translators

    protected override bool GetTrackAlbumId(AlbumInfo album, out string id)
    {
      id = null;
      if (!string.IsNullOrEmpty(album.MusicBrainzId))
        id = album.MusicBrainzId;
      return id != null;
    }

    protected override bool SetTrackAlbumId(AlbumInfo album, string id)
    {
      if (!string.IsNullOrEmpty(id))
      {
        album.MusicBrainzId = id;
        return true;
      }
      return false;
    }

    protected override bool GetTrackId(TrackInfo track, out string id)
    {
      id = null;
      if (!string.IsNullOrEmpty(track.MusicBrainzId))
        id = track.MusicBrainzId;
      return id != null;
    }

    protected override bool SetTrackId(TrackInfo track, string id)
    {
      if (!string.IsNullOrEmpty(id))
      {
        track.MusicBrainzId = id;
        return true;
      }
      return false;
    }

    protected override bool GetCompanyId(CompanyInfo company, out string id)
    {
      id = null;
      if (!string.IsNullOrEmpty(company.MusicBrainzId))
        id = company.MusicBrainzId;
      return id != null;
    }

    protected override bool SetCompanyId(CompanyInfo company, string id)
    {
      if (!string.IsNullOrEmpty(id))
      {
        company.MusicBrainzId = id;
        return true;
      }
      return false;
    }

    protected override bool GetPersonId(PersonInfo person, out string id)
    {
      id = null;
      if (!string.IsNullOrEmpty(person.MusicBrainzId))
        id = person.MusicBrainzId;
      return id != null;
    }

    protected override bool SetPersonId(PersonInfo person, string id)
    {
      if (!string.IsNullOrEmpty(id))
      {
        person.MusicBrainzId = id;
        return true;
      }
      return false;
    }

    #endregion

    #region FanArt

    protected override bool VerifyFanArtImage(TrackImage image, string language, string fanArtType)
    {
      string imgType;
      if (fanArtType == FanArtTypes.Cover)
        imgType = "Front";
      else if (fanArtType == FanArtTypes.DiscArt)
        imgType = "Medium";
      else
        return false;
      return image.Types.Contains(imgType, StringComparer.InvariantCultureIgnoreCase);
    }

    #endregion
  }
}
