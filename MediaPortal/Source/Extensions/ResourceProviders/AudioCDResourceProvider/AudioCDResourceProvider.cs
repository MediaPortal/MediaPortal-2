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
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Extensions.BassLibraries;

namespace MediaPortal.Extensions.ResourceProviders.AudioCDResourceProvider
{
  /// <summary>
  /// Resource provider implementation providing resource accessor for audio CD tracks.
  /// </summary>
  public class AudioCDResourceProvider : IBaseResourceProvider
  {
    #region Public constants

    /// <summary>
    /// GUID string for the audio CD resource provider.
    /// </summary>
    protected const string AUDIO_CD_RESOURCE_PROVIDER_ID_STR = "{EB6EB821-25B9-492E-B9FD-1E8B724F8945}";

    /// <summary>
    /// Audio CD resource provider GUID.
    /// </summary>
    public static Guid AUDIO_CD_RESOURCE_PROVIDER_ID = new Guid(AUDIO_CD_RESOURCE_PROVIDER_ID_STR);

    protected const string RES_RESOURCE_PROVIDER_NAME = "[AudioCDResourceProvider.Name]";
    protected const string RES_RESOURCE_PROVIDER_DESCRIPTION = "[AudioCDResourceProvider.Description]";

    #endregion

    #region Protected fields

    protected ResourceProviderMetadata _metadata;

    #endregion

    #region Ctor

    public AudioCDResourceProvider()
    {
      _metadata = new ResourceProviderMetadata(AUDIO_CD_RESOURCE_PROVIDER_ID, RES_RESOURCE_PROVIDER_NAME, RES_RESOURCE_PROVIDER_DESCRIPTION, true, false);
    }

    public bool TryExtract(string path, out char drive, out byte trackNo)
    {
      trackNo = 0;
      drive = (char) 0;
      if (string.IsNullOrEmpty(path))
        return false;
      if (path.Length < 4 || path[0] != '/' || path[2] != '/')
        return false;
      path = path.ToUpper();
      drive = path[1];
      if (drive < 'C' || drive > 'Z')
        return false;
      return byte.TryParse(path.Substring(3), out trackNo);
    }

    public static string BuildProviderPath(char drive, byte trackNo)
    {
      return "/" + drive + "/" + trackNo;
    }

    public static ResourcePath ToResourcePath(char drive, byte trackNo)
    {
      return ResourcePath.BuildBaseProviderPath(AUDIO_CD_RESOURCE_PROVIDER_ID, BuildProviderPath(drive, trackNo));
    }

    #endregion

    #region IBaseResourceProvider implementation

    public ResourceProviderMetadata Metadata
    {
      get { return _metadata; }
    }

    public bool IsResource(string path)
    {
      char drive;
      byte trackNo;
      if (!TryExtract(path, out drive, out trackNo))
        return false;
      return BassUtils.GetNumAudioTracks(drive + ":") > trackNo;
    }

    public bool TryCreateResourceAccessor(string path, out IResourceAccessor result)
    {
      char drive;
      byte trackNo;
      if (TryExtract(path, out drive, out trackNo))
      {
        result = new AudioCDResourceAccessor(this, drive, trackNo);
        return true;
      }
      result = null;
      return false;
    }

    public ResourcePath ExpandResourcePathFromString(string pathStr)
    {
      if (string.IsNullOrEmpty(pathStr))
        return null;
      // The input string is given by the user. We can cope with three formats:
      // 1) A resource provider path of the form "/1"
      // 2) The track number
      // 3) A resource path in the resource path syntax (i.e. {[Provider-Id]}:///1)
      if (IsResource(pathStr))
        return new ResourcePath(new ProviderPathSegment[]
          {
              new ProviderPathSegment(AUDIO_CD_RESOURCE_PROVIDER_ID, pathStr, true), 
          });
      string modifiedPath = "/" + pathStr;
      if (IsResource(modifiedPath))
        return new ResourcePath(new ProviderPathSegment[]
          {
              new ProviderPathSegment(AUDIO_CD_RESOURCE_PROVIDER_ID, modifiedPath, true), 
          });
      try
      {
        return ResourcePath.Deserialize(pathStr);
      }
      catch (ArgumentException)
      {
        return null;
      }
    }

    #endregion
  }
}
