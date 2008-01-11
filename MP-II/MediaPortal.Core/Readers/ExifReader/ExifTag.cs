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
using System.Collections.Generic;
using System.Text;

namespace MediaPortal.Core.ExifReader
{
  public class ExifTag
  {
    #region Variables

    internal string m_CameraModel = "";
    internal string m_EquipmentMake = "";
    internal string m_ExposureCompensation = "";
    internal string m_ExposureTime = "";
    internal string m_ImgTitle = "";
    internal string m_MeteringMod = "";
    internal string m_Flash = "";
    internal string m_Fstop = "";
    internal string m_ImgDimensions = "";
    internal string m_ShutterSpeed = "";
    internal string m_Resolutions = "";
    internal string m_ViewComment = "";
    internal string m_Date = "";

    //internal string m_strArtist = "";
    //internal string m_strAlbum = "";
    //internal string m_strGenre = "";
    //internal string m_strTitle = "";
    //internal string m_strComment = "";
    //internal int m_iYear = 0;
    //internal int m_iDuration = 0;
    //internal int m_iTrack = 0;
    //internal int m_iNumTrack = 0;
    //internal int m_TimesPlayed = 0;
    //internal int m_iRating = 0;
    //internal byte[] m_CoverArtImageBytes = null;
    //internal string m_AlbumArtist = string.Empty;
    //internal string m_Composer = string.Empty;
    //internal string m_FileType = string.Empty;
    //internal int m_BitRate = 0;
    //internal string m_FileName = string.Empty;
    //internal string m_Lyrics = string.Empty;
    //internal int m_iDiscId = 0;
    //internal int m_iNumDisc = 0;

    #endregion

    #region ctor

    /// <summary>
    /// empty constructor
    /// </summary>
    public ExifTag() {}

    /// <summary>
    /// copy constructor
    /// </summary>
    /// <param name="tag"></param>
    public ExifTag(ExifTag tag)
    {
      if (tag == null)
      {
        return;
      }
      /*Artist = tag.Artist;
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
      Lyrics = tag.Lyrics;*/
    }

    #endregion

    #region Methods

    /// <summary>
    /// Method to clear the current item
    /// </summary>
    public void Clear()
    {
      /*m_strArtist = "";
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
      m_iNumDisc = 0;*/
    }

    public bool IsMissingData
    {
      get
      {
        return false;
        /*Artist.Length == 0
               || Album.Length == 0
               || Title.Length == 0
               || Artist.Length == 0
               || Genre.Length == 0
               || Track == 0
                || Duration == 0; */
      }
    }

    #endregion

    #region Properties


    /*
    internal string m_CameraModel = "";
    internal string m_EquipmentMake = "";
    internal string m_ExposureCompensation = "";
    internal string m_ExposureTime = "";
    internal string m_ImgTitle = "";
    internal string m_MeteringMod = "";
    internal string m_Flash = "";
    internal string m_Fstop = "";
    internal string m_ImgDimensions = "";
    internal string m_ShutterSpeed = "";
    internal string m_Resolutions = "";
    internal string m_ViewComment = "";
    internal string m_Date = ""; */

    /// <summary>
    /// Property to get/set the CameraModel field of the picture file
    /// </summary>
    public string CameraModel
    {
      get { return m_CameraModel; }
      set
      {
        if (value == null)
        {
          return;
        }
        m_CameraModel = value.Replace('\0',' ');
      }
    }

    /// <summary>
    /// Property to get/set the EquipmentMake field of the picture file
    /// </summary>
    public string EquipmentMake
    {
      get { return m_EquipmentMake; }
      set
      {
        if (value == null)
        {
          return;
        }
        m_EquipmentMake = value.Replace('\0', ' ');
      }
    }

    /// <summary>
    /// Property to get/set the ExposureCompensation field of the picture file
    /// </summary>
    public string ExposureCompensation
    {
      get { return m_ExposureCompensation; }
      set
      {
        if (value == null)
        {
          return;
        }
        m_ExposureCompensation = value.Replace('\0', ' ');
      }
    }

    /// <summary>
    /// Property to get/set the ExposureCompensation field of the picture file
    /// </summary>
    public string ExposureTime
    {
      get { return m_ExposureTime; }
      set
      {
        if (value == null)
        {
          return;
        }
        m_ExposureTime = value.Replace('\0', ' ');
      }
    }

    /// <summary>
    /// Property to get/set the  ImgTitle field of the picture file
    /// </summary>
    public string ImgTitle
    {
      get { return m_ImgTitle; }
      set
      {
        if (value == null)
        {
          return;
        }
        m_ImgTitle = value.Replace('\0', ' ');
      }
    }

    /// <summary>
    /// Property to get/set the  MeteringMod field of the picture file
    /// </summary>
    public string  MeteringMod
    {
      get { return m_MeteringMod; }
      set
      {
        if (value == null)
        {
          return;
        }
        m_MeteringMod = value.Replace('\0', ' ');
      }
    }

    /// <summary>
    /// Property to get/set the  Flash field of the picture file
    /// </summary>
    public string  Flash
    {
      get { return m_Flash; }
      set
      {
        if (value == null)
        {
          return;
        }
        m_Flash = value.Replace('\0', ' ');
      }
    }

    /// <summary>
    /// Property to get/set the  m_Fstop field of the picture file
    /// </summary>
    public string Fstop
    {
      get { return m_Fstop; }
      set
      {
        if (value == null)
        {
          return;
        }
        m_Fstop = value.Replace('\0', ' ');
      }
    }

    /// <summary>
    /// Property to get/set the   ImgDimensions field of the picture file
    /// </summary>
    public string ImgDimensions
    {
      get { return m_ImgDimensions; }
      set
      {
        if (value == null)
        {
          return;
        }
        m_ImgDimensions = value.Replace('\0', ' ');
      }
    }

    /// <summary>
    /// Property to get/set the   ShutterSpeed field of the picture file
    /// </summary>
    public string  ShutterSpeed
    {
      get { return m_ShutterSpeed; }
      set
      {
        if (value == null)
        {
          return;
        }
        m_ShutterSpeed = value.Replace('\0', ' ');
      }
    }

    /// <summary>
    /// Property to get/set the  Resolutions field of the picture file
    /// </summary>
    public string  Resolutions
    {
      get { return m_Resolutions; }
      set
      {
        if (value == null)
        {
          return;
        }
        m_Resolutions = value.Replace('\0', ' ');
      }
    }

    /// <summary>
    /// Property to get/set the  m_ViewComment field of the picture file
    /// </summary>
    public string  ViewComment
    {
      get { return m_ViewComment; }
      set
      {
        if (value == null)
        {
          return;
        }
        m_ViewComment = value.Replace('\0', ' ');
      }
    }

    /// <summary>
    /// Property to get/set the   Date field of the picture file
    /// </summary>
    public string  Date
    {
      get { return m_Date; }
      set
      {
        if (value == null)
        {
          return;
        }
        m_Date = value.Replace('\0', ' ');
      }
    }


    

      #endregion
  }
}
