#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;

namespace Media.Importers.MusicImporter.Freedb
{
	/// <summary>
	/// Contains Details about the CD
	/// </summary>
	public class CDInfoDetail
	{
    private string m_discId;
    private string m_artist;
    private string m_title;
    private string m_genre;
    private int m_year;
    private int m_duration;
    private CDTrackDetail[] m_tracks;
    private string m_extd;
    private int[] m_playorder;

    public CDInfoDetail()
		{
		}

    public CDInfoDetail(string discID, string artist, string title,
                        string genre, int year, int duration, CDTrackDetail[] tracks,
                        string extd, int[] playorder)
    {
      m_discId = discID;
      m_artist = artist;
      m_title = title;
      m_genre = genre;
      m_year = year;
      m_duration = duration;
      m_tracks = tracks;
      m_extd = extd;
      m_playorder = playorder;
    }

    public string DiscID
    {
      get
      {
        return m_discId;
      }
      set
      {
        m_discId = value;
      }
    }

    public string Artist
    {
      get
      {
        return m_artist;
      }
      set
      {
        m_artist = value;
      }
    }

    public string Title
    {
      get
      {
        return m_title;
      }
      set
      {
        m_title = value;
      }
    }

    public string Genre
    {
      get
      {
        return m_genre;
      }
      set
      {
        m_genre = value;
      }
    }

    public int Year
    {
      get
      {
        return m_year;
      }
      set
      {
        m_year = value;
      }
    }

    public int Duration
    {
      get
      {
        return m_duration;
      }
      set
      {
        m_duration = value;
      }
    }

    public CDTrackDetail getTrack(int index)
    {
      return m_tracks[index-1];
    }

    public CDTrackDetail[] Tracks
    {
      get
      {
        return m_tracks;
      }
      set
      {
        m_tracks = value;
      }
    }

    public string EXTD
    {
      get
      {
        return m_extd;
      }
      set
      {
        m_extd = value;
      }
    }

    public int[] PlayOrder
    {
      get
      {
        return m_playorder;
      }
      set
      {
        m_playorder = value;
      }
    }
	}
}
