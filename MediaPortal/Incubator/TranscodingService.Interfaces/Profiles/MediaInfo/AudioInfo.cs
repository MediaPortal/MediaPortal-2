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

using MediaPortal.Extensions.TranscodingService.Interfaces.Metadata;
using System.Linq;

namespace MediaPortal.Extensions.TranscodingService.Interfaces.Profiles.MediaInfo
{
  public class AudioInfo
  {
    public AudioContainer AudioContainerType = AudioContainer.Unknown;
    public long Bitrate = 0;
    public long Frequency = 0;
    public bool ForceStereo = false;
    public bool ForceInheritance = false;

    public bool Matches(MetadataContainer info, int edition, int audioStreamIndex)
    {
      bool bPass = true;
      bPass &= (AudioContainerType == AudioContainer.Unknown || AudioContainerType == info.Metadata[edition].AudioContainerType);
      bPass &= (Bitrate == 0 || Bitrate >= info.Audio[edition].First(s => s.StreamIndex == audioStreamIndex).Bitrate);
      bPass &= (Frequency == 0 || Frequency >= info.Audio[edition].First(s => s.StreamIndex == audioStreamIndex).Frequency);

      return bPass;
    }

    public bool Matches(AudioInfo audioItem)
    {
      return AudioContainerType == audioItem.AudioContainerType &&
        Bitrate == audioItem.Bitrate &&
        Frequency == audioItem.Frequency &&
        ForceStereo == audioItem.ForceStereo;
    }
  }
}
