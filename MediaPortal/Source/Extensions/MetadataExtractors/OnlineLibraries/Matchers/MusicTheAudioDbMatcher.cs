#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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
using MediaPortal.Common;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.PathManager;
using MediaPortal.Extensions.OnlineLibraries.Wrappers;
using System.Collections.Generic;

namespace MediaPortal.Extensions.OnlineLibraries.Matchers
{
  public class MusicTheAudioDbMatcher : MusicMatcher<string, string>
  {
    #region Static instance

    public static MusicTheAudioDbMatcher Instance
    {
      get { return ServiceRegistration.Get<MusicTheAudioDbMatcher>(); }
    }

    #endregion

    #region Constants

    public static string CACHE_PATH = ServiceRegistration.Get<IPathManager>().GetPath(@"<DATA>\TheAudioDB\");
    protected static TimeSpan MAX_MEMCACHE_DURATION = TimeSpan.FromMinutes(10);

    #endregion

    #region Init

    public MusicTheAudioDbMatcher() : 
      base(CACHE_PATH, MAX_MEMCACHE_DURATION, false)
    {
    }

    public override bool InitWrapper(bool useHttps)
    {
      try
      {
        TheAudioDbWrapper wrapper = new TheAudioDbWrapper();
        // Try to lookup online content in the configured language
        CultureInfo currentCulture = new CultureInfo(PreferredLanguageCulture);
        string lang = new RegionInfo(currentCulture.LCID).TwoLetterISORegionName;
        if(currentCulture.TwoLetterISOLanguageName.Equals("en", StringComparison.InvariantCultureIgnoreCase))
        {
          lang = "EN";
        }
        wrapper.SetPreferredLanguage(lang);
        if (wrapper.Init(CACHE_PATH))
        {
          _wrapper = wrapper;
          return true;
        }
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("MusicTheAudioDbMatcher: Error initializing wrapper", ex);
      }
      return false;
    }

    #endregion

    #region Overrides

    public override bool FindAndUpdateTrack(TrackInfo trackInfo, bool importOnly)
    {
      if (!Init())
        return false;

      //Try and find the album and match the track against the album tracks as the track name search can be a bit hit and miss.
      //This should also improve performance as this search will be cached for other tracks in the same album whereas a track name
      //search won't be
      //TODO: Handle this better in the wrapper.
      if (trackInfo.AudioDbId == 0 && string.IsNullOrEmpty(trackInfo.MusicBrainzId))
        FindTrackFromAlbum(trackInfo, importOnly);
      return base.FindAndUpdateTrack(trackInfo, importOnly);
    }

    protected void FindTrackFromAlbum(TrackInfo trackInfo, bool importOnly)
    {
      AlbumInfo album = trackInfo.CloneBasicInstance<AlbumInfo>();
      if (!UpdateAlbum(album, true, importOnly))
        return;
      List<TrackInfo> tracks = new List<TrackInfo>(album.Tracks);
      if (_wrapper.TestTrackMatch(trackInfo, ref tracks))
        trackInfo.AudioDbId = tracks[0].AudioDbId;
    }

    #endregion

    #region Translators

    protected override bool GetTrackAlbumId(AlbumInfo album, out string id)
    {
      id = null;
      if (album.AudioDbId > 0)
        id = album.AudioDbId.ToString();
      return id != null;
    }

    protected override bool SetTrackAlbumId(AlbumInfo album, string id)
    {
      if (!string.IsNullOrEmpty(id))
      {
        album.AudioDbId = Convert.ToInt64(id);
        return true;
      }
      return false;
    }

    protected override bool GetTrackId(TrackInfo track, out string id)
    {
      id = null;
      if (track.AudioDbId > 0)
        id = track.AudioDbId.ToString();
      return id != null;
    }

    protected override bool SetTrackId(TrackInfo track, string id)
    {
      if (!string.IsNullOrEmpty(id))
      {
        track.AudioDbId = Convert.ToInt64(id);
        return true;
      }
      return false;
    }

    protected override bool GetCompanyId(CompanyInfo company, out string id)
    {
      id = null;
      if (company.AudioDbId > 0)
        id = company.AudioDbId.ToString();
      return id != null;
    }

    protected override bool SetCompanyId(CompanyInfo company, string id)
    {
      if (!string.IsNullOrEmpty(id))
      {
        company.AudioDbId = Convert.ToInt64(id);
        return true;
      }
      return false;
    }

    protected override bool GetPersonId(PersonInfo person, out string id)
    {
      id = null;
      if (person.AudioDbId > 0)
        id = person.AudioDbId.ToString();
      return id != null;
    }

    protected override bool SetPersonId(PersonInfo person, string id)
    {
      if (!string.IsNullOrEmpty(id))
      {
        person.AudioDbId = Convert.ToInt64(id);
        return true;
      }
      return false;
    }

    #endregion
  }
}
