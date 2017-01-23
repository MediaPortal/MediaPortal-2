#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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
using MediaPortal.Extensions.BassLibraries;

namespace MediaPortal.Extensions.ResourceProviders.AudioCDResourceProvider
{
  public class AudioCDResourceAccessor : IResourceAccessor
  {
    protected AudioCDResourceProvider _provider;
    protected char _drive; // For example "D"
    protected byte _trackNo;

    public AudioCDResourceAccessor(AudioCDResourceProvider provider, char drive, byte trackNo)
    {
      _provider = provider;
      _drive = drive;
      _trackNo = trackNo;
    }

    /// <summary>
    /// Returns the drive of the audio CD where this audio CD track resource is located.
    /// </summary>
    public char Drive
    {
      get { return _drive; }
    }

    /// <summary>
    /// Returns the track number of this audio CD track resource. The first track has number 1.
    /// </summary>
    public byte TrackNo
    {
      get { return _trackNo; }
    }

    public void Dispose() { }

    #region IResourceAccessor implementation

    public IResourceProvider ParentProvider
    {
      get { return _provider; }
    }

    public ResourcePath CanonicalLocalResourcePath
    {
      get { return ResourcePath.BuildBaseProviderPath(AudioCDResourceProvider.AUDIO_CD_RESOURCE_PROVIDER_ID, AudioCDResourceProvider.BuildProviderPath(_drive, _trackNo)); }
    }

    public DateTime LastChanged
    {
      get { return DateTime.MinValue; }
    }

    public long Size
    {
      get { return 0; }
    }

    public void PrepareStreamAccess()
    {
      // Nothing to do
    }

    public Stream OpenRead()
    {
      return null; // No direct stream access to audio CD tracks supported. Track access should be done directly via an audio CD access API
    }

    public Stream OpenWrite()
    {
      return null;
    }

    public bool Exists
    {
      get { return BassUtils.GetNumAudioTracks(_drive + ":") > _trackNo; }
    }

    public bool IsFile
    {
      get { return false; }
    }

    public string Path
    {
      get { return AudioCDResourceProvider.BuildProviderPath(_drive, _trackNo); }
    }

    public string ResourceName
    {
      get { return string.Format("Track {0}", _trackNo); }
    }

    public string ResourcePathName
    {
      get { return string.Format("Track {0} on drive {1}:", _trackNo, _drive); }
    }

    public IResourceAccessor Clone()
    {
      return new AudioCDResourceAccessor(_provider, _drive, _trackNo);
    }

    #endregion

    #region Base overrides

    public override string ToString()
    {
      return ResourcePathName;
    }

    #endregion
  }
}
