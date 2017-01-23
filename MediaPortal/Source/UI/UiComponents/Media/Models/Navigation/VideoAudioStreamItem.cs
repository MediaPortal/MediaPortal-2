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

using MediaPortal.UiComponents.Media.General;

namespace MediaPortal.UiComponents.Media.Models.Navigation
{
  public class VideoAudioStreamItem : NavigationItem
  {
    public VideoAudioStreamItem()
    {
    }

    public string Language
    {
      get { return this[Consts.KEY_LANGUAGE]; }
      set { SetLabel(Consts.KEY_LANGUAGE, value); }
    }

    public string AudioEncoding
    {
      get { return this[Consts.KEY_AUDIO_ENCODING]; }
      set { SetLabel(Consts.KEY_AUDIO_ENCODING, value); }
    }

    public string BitRate
    {
      get { return this[Consts.KEY_BITRATE]; }
      set { SetLabel(Consts.KEY_BITRATE, value); }
    }

    public string SampleRate
    {
      get { return this[Consts.KEY_SAMPLERATE]; }
      set { SetLabel(Consts.KEY_SAMPLERATE, value); }
    }

    public string Channels
    {
      get { return this[Consts.KEY_CHANNELS]; }
      set { SetLabel(Consts.KEY_CHANNELS, value); }
    }

    public int? Set
    {
      get { return (int?)AdditionalProperties[Consts.KEY_SET]; }
      set { AdditionalProperties[Consts.KEY_SET] = value; }
    }
  }
}
