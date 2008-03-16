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
using System.Drawing;
using System.IO;
using MediaPortal.Core;
using MediaPortal.Core.Logging;

namespace Media.Importers.MusicImporter.Tags
{
  public class MusicTag
  {
    #region Variables

    internal string m_strArtist = "";
    internal string m_strAlbum = "";
    internal string m_strGenre = "";
    internal string m_strTitle = "";
    internal string m_strComment = "";
    internal int m_iYear = 0;
    internal int m_iDuration = 0;
    internal int m_iTrack = 0;
    internal int m_iNumTrack = 0;
    internal int m_TimesPlayed = 0;
    internal int m_iRating = 0;
    internal byte[] m_CoverArtImageBytes = null;
    internal string m_AlbumArtist = string.Empty;
    internal string m_Composer = string.Empty;
    internal string m_FileType = string.Empty;
    internal int m_BitRate = 0;
    internal string m_FileName = string.Empty;
    internal string m_Lyrics = string.Empty;
    internal int m_iDiscId = 0;
    internal int m_iNumDisc = 0;

    #endregion

    #region ctor

    /// <summary>
    /// empty constructor
    /// </summary>
    public MusicTag() {}

    /// <summary>
    /// copy constructor
    /// </summary>
    /// <param name="tag"></param>
    public MusicTag(MusicTag tag)
    {
      if (tag == null)
      {
        return;
      }
      Artist = tag.Artist;
      Album = tag.Album;
      Genre = tag.Genre;
      Title = tag.Title;
      Comment = tag.Comment;
      Year = tag.Year;
      Duration = tag.Duration;
      Track = tag.Track;
      TimesPlayed = tag.m_TimesPlayed;
      Rating = tag.Rating;
      BitRate = tag.BitRate;
      Composer = tag.Composer;
      CoverArtImageBytes = tag.CoverArtImageBytes;
      AlbumArtist = tag.AlbumArtist;
      Lyrics = tag.Lyrics;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Method to clear the current item
    /// </summary>
    public void Clear()
    {
      m_strArtist = "";
      m_strAlbum = "";
      m_strGenre = "";
      m_strTitle = "";
      m_strComment = "";
      m_iYear = 0;
      m_iDuration = 0;
      m_iTrack = 0;
      m_iNumTrack = 0;
      m_TimesPlayed = 0;
      m_iRating = 0;
      m_BitRate = 0;
      m_Composer = "";
      m_AlbumArtist = "";
      m_Lyrics = "";
      m_iDiscId = 0;
      m_iNumDisc = 0;
    }

    public bool IsMissingData
    {
      get
      {
        return Artist.Length == 0
               || Album.Length == 0
               || Title.Length == 0
               || Artist.Length == 0
               || Genre.Length == 0
               || Track == 0
               || Duration == 0;
      }
    }

    #endregion

    #region Properties

    /// <summary>
    /// Property to get/set the comment field of the music file
    /// </summary>
    public string Comment
    {
      get { return m_strComment; }
      set
      {
        if (value == null)
        {
          return;
        }
        m_strComment = value.Trim();
      }
    }

    /// <summary>
    /// Property to get/set the Title field of the music file
    /// </summary>
    public string Title
    {
      get { return m_strTitle; }
      set
      {
        if (value == null)
        {
          return;
        }
        m_strTitle = value.Trim();
      }
    }

    /// <summary>
    /// Property to get/set the Artist field of the music file
    /// </summary>
    public string Artist
    {
      get { return m_strArtist; }
      set
      {
        if (value == null)
        {
          return;
        }
        m_strArtist = value.Trim();
      }
    }

    /// <summary>
    /// Property to get/set the comment Album name of the music file
    /// </summary>
    public string Album
    {
      get { return m_strAlbum; }
      set
      {
        if (value == null)
        {
          return;
        }
        m_strAlbum = value.Trim();
      }
    }

    /// <summary>
    /// Property to get/set the Genre field of the music file
    /// </summary>
    public string Genre
    {
      get { return m_strGenre; }
      set
      {
        if (value == null)
        {
          return;
        }
        m_strGenre = value.Trim();
      }
    }

    /// <summary>
    /// Property to get/set the Year field of the music file
    /// </summary>
    public int Year
    {
      get { return m_iYear; }
      set { m_iYear = value; }
    }

    /// <summary>
    /// Property to get/set the duration in seconds of the music file
    /// </summary>
    public int Duration
    {
      get { return m_iDuration; }
      set { m_iDuration = value; }
    }

    /// <summary>
    /// Property to get/set the Track number field of the music file
    /// </summary>
    public int Track
    {
      get { return m_iTrack; }
      set { m_iTrack = value; }
    }

    /// <summary>
    /// Property to get/set the Total Track number field of the music file
    /// </summary>
    public int TrackTotal
    {
      get { return m_iNumTrack; }
      set { m_iNumTrack = value; }
    }

    /// <summary>
    /// Property to get/set the Disc Id field of the music file
    /// </summary>
    public int DiscID
    {
      get { return m_iDiscId; }
      set { m_iDiscId = value; }
    }

    /// <summary>
    /// Property to get/set the Total Disc number field of the music file
    /// </summary>
    public int DiscTotal
    {
      get { return m_iNumDisc; }
      set { m_iNumDisc = value; }
    }

    /// <summary>
    /// Property to get/set the Track number field of the music file
    /// </summary>
    public int Rating
    {
      get { return m_iRating; }
      set { m_iRating = value; }
    }

    /// <summary>
    /// Property to get/set the number of times this file has been played
    /// </summary>
    public int TimesPlayed
    {
      get { return m_TimesPlayed; }
      set { m_TimesPlayed = value; }
    }

    public string FileType
    {
      get { return m_FileType; }
      set { m_FileType = value; }
    }

    public int BitRate
    {
      get { return m_BitRate; }
      set { m_BitRate = value; }
    }

    public string AlbumArtist
    {
      get { return m_AlbumArtist; }
      set { m_AlbumArtist = value; }
    }

    public string Composer
    {
      get { return m_Composer; }
      set { m_Composer = value; }
    }

    public string FileName
    {
      get { return m_FileName; }
      set { m_FileName = value; }
    }

    public string Lyrics
    {
      get { return m_Lyrics; }
      set { m_Lyrics = value; }
    }

    public byte[] CoverArtImageBytes
    {
      get { return m_CoverArtImageBytes; }
      set { m_CoverArtImageBytes = value; }
    }

    public Image CoverArtImage
    {
      get
      {
        if (m_CoverArtImageBytes == null || m_CoverArtImageBytes.Length == 0)
        {
          return null;
        }

        Image img = null;
        MemoryStream stream = null;

        try
        {
          stream = new MemoryStream(m_CoverArtImageBytes);
          img = Image.FromStream(stream, true, true);
        }
        catch (Exception ex)
        {
          ServiceScope.Get<ILogger>().Debug("Could not extract Image: {0}", ex.Message);
        }
        finally
        {
          if (stream != null)
          {
            stream.Close();
            stream = null;
          }
        }
        return img;
      }

      #endregion
    }
  }
}
