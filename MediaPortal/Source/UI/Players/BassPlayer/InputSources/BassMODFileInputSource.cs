#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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
using System.IO;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.UI.Players.BassPlayer.Interfaces;
using MediaPortal.UI.Players.BassPlayer.Utils;
using Un4seen.Bass;

namespace MediaPortal.UI.Players.BassPlayer.InputSources
{
  /// <summary>
  /// Represents a MOD file inputsource.
  /// </summary>
  internal class BassMODFileInputSource : AbstractBassResourceInputSource, IInputSource
  {
    #region Static members

    /// <summary>
    /// Creates and initializes an new instance.
    /// </summary>
    /// <param name="resourceAccessor">The resource accessor to the media item to be handled by the instance.</param>
    /// <returns>The new instance.</returns>
    public static BassMODFileInputSource Create(IFileSystemResourceAccessor resourceAccessor)
    {
      resourceAccessor.PrepareStreamAccess();
      BassMODFileInputSource inputSource = new BassMODFileInputSource(resourceAccessor);
      inputSource.Initialize();
      return inputSource;
    }

    #endregion

    #region Fields

    private BassStream _bassStream;

    #endregion

    #region IInputSource Members

    public MediaItemType MediaItemType
    {
      get { return MediaItemType.MODFile; }
    }

    public BassStream OutputStream
    {
      get { return _bassStream; }
    }

    public TimeSpan Length
    {
      get { return _bassStream.Length; }
    }

    #endregion

    #region IDisposable Members

    public override void Dispose()
    {
      base.Dispose();
      if (_bassStream != null)
        _bassStream.Dispose();
      // Bass.BASS_MusicFree is not necessary to be called here because of flag BASS_MUSIC_AUTOFREE
    }

    #endregion

    #region Public members

    #endregion

    #region Private members

    private BassMODFileInputSource(IFileSystemResourceAccessor resourceAccessor) : base(resourceAccessor) { }

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    private void Initialize()
    {
      Log.Debug("BassMODFileInputSource.Initialize()");

      const BASSFlag flags = BASSFlag.BASS_SAMPLE_SOFTWARE | BASSFlag.BASS_SAMPLE_FLOAT |
          BASSFlag.BASS_MUSIC_AUTOFREE | BASSFlag.BASS_MUSIC_PRESCAN;

      int handle;
      ILocalFsResourceAccessor lfra = _accessor as ILocalFsResourceAccessor;
      if (lfra == null)
      { // Build stream reading procs for the resource's input stream
        Stream inputStream = _accessor.OpenRead();
        int length = (int) inputStream.Length;
        byte[] audioData = new byte[length];
        inputStream.Read(audioData, 0, length);
        handle = Bass.BASS_MusicLoad(audioData, 0, length, flags, 0);
      }
      else
        // Optimize access to local filesystem resource
        handle = Bass.BASS_MusicLoad(lfra.LocalFileSystemPath, 0, 0, flags, 0);

      if (handle == BassConstants.BassInvalidHandle)
        throw new BassLibraryException("BASS_MusicLoad");

      _bassStream = BassStream.Create(handle);
    }

    #endregion
  }
}
