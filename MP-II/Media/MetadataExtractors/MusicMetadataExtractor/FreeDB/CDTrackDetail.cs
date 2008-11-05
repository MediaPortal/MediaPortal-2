#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
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
	/// Contains Information about Tracks
	/// </summary>
	public class CDTrackDetail
	{
    private int m_trackNumber;
    private string m_artist;
    private string m_songTitle;
    private int m_duration;
    private int m_offset;
    private string m_extt;

		public CDTrackDetail()
		{
		}

    public CDTrackDetail(string songTitle, string artist, string extt, 
                         int trackNumber, int offset, int duration)
    {
      m_songTitle = songTitle;
      m_artist = artist;
      m_extt = extt;
      m_trackNumber = trackNumber;
      m_offset = offset;
      m_duration = duration;
    }

    public string Title
    {
      get
      {
        return m_songTitle;
      }
      set
      {
        m_songTitle = value;
      }
    }

    // can be null if the artist is the same as the main
    // album
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


    public int TrackNumber
    {
      get
      {
        return m_trackNumber;
      }
      set
      {
        m_trackNumber = value;
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

    public int Offset
    {
      get
      {
        return m_offset;
      }
      set
      {
        m_offset = value;
      }
    }

    public string EXTT
    {
      get
      {
        return m_extt;
      }
      set
      {
        m_extt = value;
      }
    }
	}
}
