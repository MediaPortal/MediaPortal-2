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
using System.IO;
using System.Runtime.InteropServices;
using MediaPortal.Core.MediaManagement;
using Ui.Players.BassPlayer.Interfaces;
using Ui.Players.BassPlayer.Utils;
using Un4seen.Bass;

namespace Ui.Players.BassPlayer.InputSources
{
  /// <summary>
  /// Represents a file inputsource implemented by the Bass library.
  /// </summary>
  /// <remarks>
  /// This class encapsulates the access to local filesystem resources as well as resources which aren't don't provide
  /// the <see cref="ILocalFsResourceAccessor"/> interface.
  /// </remarks>
  internal class BassAudioFileInputSource : AbstractBassResourceInputSource, IInputSource
  {
    /// <summary>
    /// Encapsulates stream access methods for a <see cref="BASS_FILEPROCS"/> instance.
    /// </summary>
    protected class StreamInput
    {
      private readonly Stream _inputStream;
      private readonly BASS_FILEPROCS _fileProcs;

      public StreamInput(Stream inputStream)
      {
        _inputStream = inputStream;
        _fileProcs = new BASS_FILEPROCS(closeCallback, lengthCalback, readCallback, seekCallback);
      }

      private bool seekCallback(long offset, IntPtr user)
      {
        try
        {
          _inputStream.Seek(offset, SeekOrigin.Begin);
          return true;
        }
        catch
        {
          return false;
        }
      }

      private int readCallback(IntPtr buffer, int length, IntPtr user)
      {
        // Code taken from BASS help for BASS_FILEPROCS class
        try
        {
          // At first we need to create a byte[] with the size of the requested length
          byte[] data = new byte[length];
          // Read the file into data
          int bytesread = _inputStream.Read(data, 0, length);
          // And now we need to copy the data to the buffer we write as many bytes as we read via the file operation.
          Marshal.Copy(data, 0, buffer, bytesread);
          return bytesread;
        }
        catch { return 0; }
      }

      private long lengthCalback(IntPtr user)
      {
        return _inputStream.Length;
      }

      private void closeCallback(IntPtr user)
      {
        _inputStream.Close();
      }

      public BASS_FILEPROCS FileProcs
      {
        get { return _fileProcs; }
      }

    }

    #region Static members

    /// <summary>
    /// Creates and initializes an new instance.
    /// </summary>
    /// <param name="resourceAccessor">The resource accessor to the media item to be handled by the instance.</param>
    /// <returns>The new instance.</returns>
    public static BassAudioFileInputSource Create(IResourceAccessor resourceAccessor)
    {
      BassAudioFileInputSource inputSource = new BassAudioFileInputSource(resourceAccessor);
      inputSource.Initialize();
      return inputSource;
    }

    #endregion

    #region Fields

    protected BassStream _BassStream;
    protected StreamInput _streamInput = null;

    #endregion

    #region IInputSource Members

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

    public override void Dispose()
    {
      base.Dispose();
      if (_BassStream != null)
        _BassStream.Dispose();
    }

    #endregion

    #region Public members

    #endregion

    #region Private Members

    private BassAudioFileInputSource(IResourceAccessor resourceAccessor) : base(resourceAccessor) { }

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    private void Initialize()
    {
      Log.Debug("BassAudioFileInputSource.Initialize()");

      BASSFlag flags = BASSFlag.BASS_STREAM_DECODE | BASSFlag.BASS_SAMPLE_FLOAT;

      int handle;
      ILocalFsResourceAccessor lfra = _accessor as ILocalFsResourceAccessor;
      if (lfra == null)
      { // Build stream reading procs for the resource's input stream
        flags |= BASSFlag.BASS_STREAM_PRESCAN;
        _streamInput = new StreamInput(_accessor.OpenRead());
        handle = Bass.BASS_StreamCreateFileUser(
            BASSStreamSystem.STREAMFILE_NOBUFFER, flags, _streamInput.FileProcs, IntPtr.Zero);
      }
      else
        // Optimize access to local filesystem resource
        handle = Bass.BASS_StreamCreateFile(lfra.LocalFileSystemPath, 0, 0, flags);

      if (handle == BassConstants.BassInvalidHandle)
        throw new BassLibraryException("BASS_StreamCreateFile");

      _BassStream = BassStream.Create(handle);
    }

    #endregion
  }
}
