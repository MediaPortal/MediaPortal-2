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
using MediaPortal.Media.MediaManager;
using Un4seen.Bass;

namespace Media.Players.BassPlayer
{
  public partial class BassPlayer
  {
    partial class InputSourceFactory
    {
      /// <summary>
      /// Represents a file inputsource implemented by the Bass library.
      /// </summary>
      class BassAudioFileInputSource : IInputSource
      {
        #region Static members

        /// <summary>
        /// Creates and initializes an new instance.
        /// </summary>
        /// <param name="mediaItem">The mediaItem to be handled by the instance.</param>
        /// <returns>The new instance.</returns>
        public static BassAudioFileInputSource Create(IMediaItem mediaItem)
        {
          BassAudioFileInputSource inputSource = new BassAudioFileInputSource(mediaItem);
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
          get { return MediaItemType.AudioFile; }
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

        #region Private Members

        private BassAudioFileInputSource(IMediaItem mediaItem)
        {
          _MediaItem = mediaItem;
        }

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        private void Initialize()
        {
          Log.Debug("Initializing inputsource \"BassAudioFileInputSource\"");

          BASSFlag flags =
              BASSFlag.BASS_STREAM_DECODE |
              BASSFlag.BASS_SAMPLE_FLOAT;

          int handle = Bass.BASS_StreamCreateFile(_MediaItem.ContentUri.LocalPath, 0, 0, flags);

          if (handle == Constants.BassInvalidHandle)
            throw new BassLibraryException("BASS_StreamCreateFile");

          _BassStream = BassStream.Create(handle);
        }

        #endregion
      }
    }
  }
}