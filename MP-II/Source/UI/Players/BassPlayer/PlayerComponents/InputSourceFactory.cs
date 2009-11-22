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
using MediaPortal.UI.Media.MediaManagement;
using Un4seen.Bass;

namespace Media.Players.BassPlayer
{
  public partial class BassPlayer
  {
    /// <summary>
    /// Creates inputsource objects.
    /// </summary>
    partial class InputSourceFactory : IDisposable
    {
      #region Fields

      BassPlayer _Player;

      #endregion

      #region IDisposable Members

      public void Dispose()
      {
      }

      #endregion

      #region Public members

      public InputSourceFactory(BassPlayer player)
      {
        _Player = player;
      }

      /// <summary>
      /// Creates an IInputSource object for a given mediaitem.
      /// </summary>
      /// <param name="mediaItem"></param>
      /// <returns></returns>
      public IInputSource CreateInputSource(IMediaItem mediaItem)
      {
        MediaItemType itemType = GetMediaItemType(mediaItem);
        Log.Info("Media item type: {0}", itemType);

        IInputSource inputSource;

        switch (itemType)
        {
          case MediaItemType.AudioFile:
            inputSource = BassAudioFileInputSource.Create(mediaItem);
            break;
          case MediaItemType.CDTrack:
            inputSource = BassCDTrackInputSource.Create(mediaItem);
            break;
          case MediaItemType.MODFile:
            inputSource = BassMODFileInputSource.Create(mediaItem);
            break;
          case MediaItemType.WebStream:
            inputSource = BassWebStreamInputSource.Create(mediaItem);
            break;
          default:
            throw new BassPlayerException(String.Format("Unknown constant MediaItemType.{0}", itemType));
        }
        return inputSource;
      }

      #endregion

      #region Private members

      /// <summary>
      /// Determines the mediaitem type for a given mediaitem.
      /// </summary>
      /// <param name="mediaItem">Mediaitem to analize.</param>
      /// <returns>One of the MediaItemType enumeration values.</returns>
      private MediaItemType GetMediaItemType(IMediaItem mediaItem)
      {
        Uri uri = mediaItem.ContentUri;
        MediaItemType fileType;

        if (uri.IsFile)
        {
          string filePath = uri.LocalPath;
          if (String.IsNullOrEmpty(filePath))
            fileType = MediaItemType.Unknown;
          else if (IsCDDA(filePath))
            fileType = MediaItemType.CDTrack;
          else if (IsASXFile(filePath))
            fileType = MediaItemType.WebStream;
          else if (IsMODFile(filePath))
            fileType = MediaItemType.MODFile;
          else
            fileType = MediaItemType.AudioFile;
        }
        else
          fileType = MediaItemType.WebStream;

        return fileType;
      }

      /// <summary>
      /// Determines if a given path represents a MOD music file.
      /// </summary>
      /// <param name="filePath"></param>
      /// <returns></returns>
      private bool IsMODFile(string path)
      {
        string ext = Path.GetExtension(path).ToLower();

        switch (ext)
        {
          case ".mod":
          case ".mo3":
          case ".it":
          case ".xm":
          case ".s3m":
          case ".mtm":
          case ".umx":
            return true;

          default:
            return false;
        }
      }

      /// <summary>
      /// Determines if a given path represents a audio CD track.
      /// </summary>
      /// <param name="filePath"></param>
      /// <returns></returns>
      private bool IsCDDA(string path)
      {
        path = path.ToLower();
        return
            (path.IndexOf("cdda:") >= 0 ||
            path.IndexOf(".cda") >= 0);
      }

      /// <summary>
      /// Determines if a given path represents a ASX file.
      /// </summary>
      /// <param name="filePath"></param>
      /// <returns></returns>
      private bool IsASXFile(string path)
      {
        return (Path.GetExtension(path).ToLower() == ".asx");
      }

      #endregion

    }
  }
}
