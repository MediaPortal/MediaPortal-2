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
using System.IO;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using TagLib;
using TagLib.Id3v2;
using File=TagLib.File;
using Tag=TagLib.Id3v2.Tag;

namespace Media.Importers.MusicImporter.Tags
{
  public class TagReader 
  {
    public TagReader() {}

    public MusicTag ReadTag(string strFile)
    {
      if (!IsAudio(strFile))
      {
        return null;
      }

      try
      {
        // Set the flag to use the standard System Encoding set by the user
        // Otherwise Latin1 is used as default, which causes characters in various languages being displayed wrong
        ByteVector.UseBrokenLatin1Behavior = true;
        File tag = File.Create(strFile);
        MusicTag musictag = new MusicTag();
        string[] artists = tag.Tag.Performers;
        if (artists.Length > 0)
        {
          musictag.Artist = artists[0];
        }
        musictag.Album = tag.Tag.Album;
        string[] albumartists = tag.Tag.AlbumArtists;
        if (albumartists.Length > 0)
        {
          musictag.AlbumArtist = albumartists[0];
        }
        musictag.BitRate = tag.Properties.AudioBitrate;
        musictag.Comment = tag.Tag.Comment;
        string[] composer = tag.Tag.Composers;
        if (composer.Length > 0)
        {
          musictag.Composer = composer[0];
        }
        IPicture[] pics = new IPicture[] {};
        pics = tag.Tag.Pictures;
        if (pics.Length > 0)
        {
          musictag.CoverArtImageBytes = pics[0].Data.Data;
        }
        musictag.Duration = (int) tag.Properties.Duration.TotalSeconds;
        musictag.FileName = strFile;
        musictag.FileType = tag.MimeType;
        string[] genre = tag.Tag.Genres;
        if (genre.Length > 0)
        {
          musictag.Genre = genre[0];
        }
        string lyrics = tag.Tag.Lyrics;
        if (lyrics == null)
        {
          musictag.Lyrics = "";
        }
        else
        {
          musictag.Lyrics = lyrics;
        }
        musictag.Title = tag.Tag.Title;
        musictag.Track = (int) tag.Tag.Track;
        musictag.Year = (int) tag.Tag.Year;

        if (tag.MimeType == "taglib/mp3")
        {
          // Handle the Rating, which comes from the POPM frame
          Tag id32_tag = tag.GetTag(TagTypes.Id3v2) as Tag;
          if (id32_tag != null)
          {
            PopularimeterFrame popm;
            foreach (Frame frame in id32_tag)
            {
              popm = frame as PopularimeterFrame;
              if (popm != null)
              {
                int rating = popm.Rating;
                int i = 0;
                if (rating > 205)
                {
                  i = 5;
                }
                else if (rating > 154)
                {
                  i = 4;
                }
                else if (rating > 104)
                {
                  i = 3;
                }
                else if (rating > 53)
                {
                  i = 2;
                }
                else if (rating > 0)
                {
                  i = 1;
                }
                musictag.Rating = i;
              }
            }
          }
        }

        // if we didn't get a title, use the Filename without extension to prevent the file to appear as "unknown"
        if (musictag.Title == "")
        {
          musictag.Title = Path.GetFileNameWithoutExtension(strFile);
        }

        return musictag;
      }
      catch (UnsupportedFormatException)
      {
        ServiceScope.Get<ILogger>().Warn("Tagreader: Unsupported File Format {0}", strFile);
      }
      catch (Exception ex)
      {
        ServiceScope.Get<ILogger>().Error("TagReader: Exception reading file {0}. {1}", strFile, ex.Message);
      }
      return null;
    }

    private bool IsAudio(string fileName)
    {
      string ext = Path.GetExtension(fileName).ToLower();

      switch (ext)
      {
        case ".ape":
        case ".flac":
        case ".mp3":
        case ".ogg":
        case ".wv":
        case ".wav":
        case ".wma":
        case ".mp4":
        case ".m4a":
        case ".m4p":
        case ".mpc":
        case ".mp+":
        case ".mpp":
          return true;
      }

      return false;
    }
  }
}
