#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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

using System.Collections.Generic;
using System.Linq;
using MediaPortal.Extensions.TranscodingService.Interfaces.Metadata.Streams;
using Newtonsoft.Json;

namespace MediaPortal.Extensions.TranscodingService.Interfaces.Metadata
{
  public class MetadataContainer
  {
    public const int DVD_EDITION_OFFSET = 10000;

    public Dictionary<int, MetadataStream> Metadata = new Dictionary<int, MetadataStream>();
    public Dictionary<int, ImageStream> Image = new Dictionary<int, ImageStream>();
    public Dictionary<int, VideoStream> Video = new Dictionary<int, VideoStream>();
    public Dictionary<int, List<AudioStream>> Audio = new Dictionary<int, List<AudioStream>>();
    public Dictionary<int, Dictionary<int, List<SubtitleStream>>> Subtitles = new Dictionary<int, Dictionary<int, List<SubtitleStream>>>();

    public void AddEdition(int edition)
    {
      if (!Metadata.ContainsKey(edition))
        Metadata.Add(edition, new MetadataStream());
      if (!Image.ContainsKey(edition))
        Image.Add(edition, new ImageStream());
      if (!Video.ContainsKey(edition))
        Video.Add(edition, new VideoStream());
      if (!Audio.ContainsKey(edition))
        Audio.Add(edition, new List<AudioStream>());
      if (!Subtitles.ContainsKey(edition))
        Subtitles.Add(edition, new Dictionary<int, List<SubtitleStream>>());
    }

    public bool ContainsDvdResource(int edition)
    {
      return Metadata[edition].FilePaths.Any(f => f.Key >= DVD_EDITION_OFFSET);
    }

    public static bool IsDvdResource(int resourceKey)
    {
      return resourceKey >= DVD_EDITION_OFFSET;
    }

    public static int GetDvdResource(int edition, int titleNumber, int fileNumber = 1)
    {
      return (edition + 1) * DVD_EDITION_OFFSET + titleNumber * 100 + fileNumber;
    }

    public bool HasEdition(int edition)
    {
      if (!Metadata.ContainsKey(edition))
        return false;
      if (!Image.ContainsKey(edition))
        return false;
      if (!Video.ContainsKey(edition))
        return false;
      if (!Audio.ContainsKey(edition))
        return false;
      if (!Subtitles.ContainsKey(edition))
        return false;
      return true;
    }

    public AudioStream GetFirstAudioStream(int edition)
    {
      if (!HasEdition(edition))
        return null;

      return Audio[edition].FirstOrDefault();
    }

    public SubtitleStream GetFirstSubtitleStream(int edition)
    {
      if (!HasEdition(edition))
        return null;

      return Subtitles[edition].FirstOrDefault().Value?.FirstOrDefault();
    }

    public bool IsImage(int edition)
    {
      if (!HasEdition(edition))
        return false;

      if (Audio[edition].Count > 0)
        return false;
      
      if (Metadata[edition].Mime != null && Metadata[edition].Mime.StartsWith("Image/", System.StringComparison.InvariantCultureIgnoreCase))
        return true;
      
      if (Metadata[edition].ImageContainerType != ImageContainer.Unknown)
        return true;
      
      return false;
    }

    public bool IsAudio(int edition)
    {
      if (!HasEdition(edition))
        return false;

      if (IsVideo(edition))
        return false;
      
      if (Audio[edition].Count > 0)
        return true;
      
      if (Metadata[edition].Mime != null && Metadata[edition].Mime.StartsWith("Audio/", System.StringComparison.InvariantCultureIgnoreCase))
        return true;
      
      if (Metadata[edition].AudioContainerType != AudioContainer.Unknown)
        return true;
      
      return false;
    }

    public bool IsVideo(int edition)
    {
      if (!HasEdition(edition))
        return false;

      if (Audio[edition].Count > 0 && (Video[edition].Codec != VideoCodec.Unknown || Metadata[edition].VideoContainerType != VideoContainer.Unknown))
        return true;
      
      if (Metadata[edition].Mime != null && Metadata[edition].Mime.StartsWith("Video/", System.StringComparison.InvariantCultureIgnoreCase))
        return true;
      
      if (Metadata[edition].VideoContainerType != VideoContainer.Unknown)
        return true;
      
      return false;
    }
  }
}
