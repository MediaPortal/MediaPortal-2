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
using MediaPortal.Media.MediaManagement;
using Un4seen.Bass;

namespace Media.Players.BassPlayer
{
  public partial class BassPlayer
  {
    partial class InputSourceFactory
    {
      /// <summary>
      /// Represents a MOD file inputsource implemented by the Bass library.
      /// </summary>
      class BassMODFileInputSource : IInputSource
      {
        #region Static members

        /// <summary>
        /// Creates and initializes an new instance.
        /// </summary>
        /// <param name="mediaItem">The mediaItem to be handled by the instance.</param>
        /// <returns>The new instance.</returns>
        public static BassMODFileInputSource Create(IMediaItem mediaItem)
        {
          BassMODFileInputSource inputSource = new BassMODFileInputSource(mediaItem);
          inputSource.Initialize();
          return inputSource;
        }

        #endregion

        #region Fields

        private IMediaItem _MediaItem;
        private BassStream _BassStream;

        #endregion

        #region IInputSource Members

        public IMediaItem MediaItem
        {
          get { return _MediaItem; }
        }

        public MediaItemType MediaItemType
        {
          get { return MediaItemType.MODFile; }
        }

        public BassStream OutputStream
        {
          get { return _BassStream; }
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
          if (OutputStream != null)
            OutputStream.Dispose();
        }

        #endregion

        #region Public members

        #endregion

        #region Private members

        private BassMODFileInputSource(IMediaItem mediaItem)
        {
          _MediaItem = mediaItem;
        }

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        private void Initialize()
        {
          Log.Debug("BassMODFileInputSource.Initialize()");

          BASSFlag flags =
              BASSFlag.BASS_SAMPLE_SOFTWARE |
              BASSFlag.BASS_SAMPLE_FLOAT |
              BASSFlag.BASS_MUSIC_AUTOFREE |
              BASSFlag.BASS_MUSIC_PRESCAN;

          int handle = Bass.BASS_MusicLoad(_MediaItem.ContentUri.LocalPath, 0, 0, flags, 0);

          if (handle == BassConstants.BassInvalidHandle)
            throw new BassLibraryException("BASS_MusicLoad");

          _BassStream = BassStream.Create(handle);
        }

        #endregion
      }
    }
  }
}