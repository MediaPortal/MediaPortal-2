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
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess.StreamedResourceToLocalFsAccessBridge;
using MediaPortal.Plugins.Transcoding.Aspects;
using MediaPortal.Plugins.Transcoding.Service;

namespace MediaPortal.Plugins.MP2Extended.Metadata
{
  public class DlnaAudioMetadata
  {
    public static MetadataContainer ParseMediaItem(MediaItem item)
    {
      MetadataContainer info = new MetadataContainer();
      IResourceAccessor mediaItemAccessor = item.GetResourceLocator().CreateAccessor();
      if (mediaItemAccessor is IFileSystemResourceAccessor)
      {
        using (var fsra = (IFileSystemResourceAccessor)mediaItemAccessor.Clone())
        {
          if (!fsra.IsFile)
            return null;
          using (var lfsra = StreamedResourceToLocalFsAccessBridge.GetLocalFsResourceAccessor(fsra))
          {
            info.Metadata.Source = lfsra;
            info.Metadata.Size = lfsra.Size;
          }
        }
      }
      else if (mediaItemAccessor is INetworkResourceAccessor)
      {
        using (var nra = (INetworkResourceAccessor)mediaItemAccessor.Clone())
        {
          info.Metadata.Source = nra;
        }
        info.Metadata.Size = 0;
      }
      if (item.Aspects.ContainsKey(TranscodeItemAudioAspect.ASPECT_ID) == true)
      {
        object oValue = null;
        oValue = item.Aspects[TranscodeItemAudioAspect.ASPECT_ID].GetAttributeValue(TranscodeItemAudioAspect.ATTR_CONTAINER);
        if (oValue != null && string.IsNullOrEmpty(oValue.ToString()) == false)
        {
          info.Metadata.AudioContainerType = (AudioContainer)Enum.Parse(typeof(AudioContainer), oValue.ToString());
        }
        AudioStream audio = new AudioStream();
        oValue = item.Aspects[TranscodeItemAudioAspect.ASPECT_ID].GetAttributeValue(TranscodeItemAudioAspect.ATTR_STREAM);
        if (oValue != null)
        {
          audio.StreamIndex = Convert.ToInt32(oValue);
          oValue = (string)item.Aspects[TranscodeItemAudioAspect.ASPECT_ID].GetAttributeValue(TranscodeItemAudioAspect.ATTR_CODEC);
          if (oValue != null && string.IsNullOrEmpty(oValue.ToString()) == false)
          {
            audio.Codec = (AudioCodec)Enum.Parse(typeof(AudioCodec), oValue.ToString());
          }
          oValue = item.Aspects[TranscodeItemAudioAspect.ASPECT_ID].GetAttributeValue(TranscodeItemAudioAspect.ATTR_CHANNELS);
          if (oValue != null)
          {
            audio.Channels = Convert.ToInt32(oValue);
          }
          oValue = item.Aspects[TranscodeItemAudioAspect.ASPECT_ID].GetAttributeValue(TranscodeItemAudioAspect.ATTR_FREQUENCY);
          if (oValue != null)
          {
            audio.Frequency = Convert.ToInt64(oValue);
          }
          if (item.Aspects.ContainsKey(AudioAspect.ASPECT_ID) == true)
          {
            oValue = item.Aspects[AudioAspect.ASPECT_ID].GetAttributeValue(AudioAspect.ATTR_BITRATE);
            if (oValue != null)
            {
              audio.Bitrate = Convert.ToInt64(oValue);
            }
            oValue = item.Aspects[AudioAspect.ASPECT_ID].GetAttributeValue(AudioAspect.ATTR_DURATION);
            if (oValue != null)
            {
              info.Metadata.Duration = Convert.ToDouble(oValue);
            }
          }
          if (item.Aspects.ContainsKey(MediaAspect.ASPECT_ID) == true)
          {
            oValue = item.Aspects[MediaAspect.ASPECT_ID].GetAttributeValue(MediaAspect.ATTR_MIME_TYPE);
            if (oValue != null && string.IsNullOrEmpty(oValue.ToString()) == false)
            {
              info.Metadata.Mime = oValue.ToString();
            }
          }
        }
        info.Audio.Add(audio);
        if (info.Audio.Count > 0 && info.Audio[0].Bitrate > 0)
        {
          info.Metadata.Bitrate = info.Audio[0].Bitrate;
        }
      }

      return info;
    }
  }
}
