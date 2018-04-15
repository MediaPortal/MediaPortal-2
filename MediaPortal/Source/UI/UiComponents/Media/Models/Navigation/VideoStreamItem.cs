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
  public class VideoStreamItem : NavigationItem
  {
    public VideoStreamItem()
    {
    }

    public string Format
    {
      get { return this[Consts.KEY_FORMAT]; }
      set { SetLabel(Consts.KEY_FORMAT, value); }
    }

    public string VideoEncoding
    {
      get { return this[Consts.KEY_VIDEO_ENCODING]; }
      set { SetLabel(Consts.KEY_VIDEO_ENCODING, value); }
    }

    public string Duration
    {
      get { return this[Consts.KEY_DURATION]; }
      set { SetLabel(Consts.KEY_DURATION, value); }
    }

    public string AspectRatio
    {
      get { return this[Consts.KEY_ASPECTRATIO]; }
      set { SetLabel(Consts.KEY_ASPECTRATIO, value); }
    }

    public string FPS
    {
      get { return this[Consts.KEY_FPS]; }
      set { SetLabel(Consts.KEY_FPS, value); }
    }

    public string BitRate
    {
      get { return this[Consts.KEY_BITRATE]; }
      set { SetLabel(Consts.KEY_BITRATE, value); }
    }

    public int? Set
    {
      get { return (int?)AdditionalProperties[Consts.KEY_SET]; }
      set { AdditionalProperties[Consts.KEY_SET] = value; }
    }

    public string SetName
    {
      get { return (string)AdditionalProperties[Consts.KEY_SET_NAME]; }
      set { AdditionalProperties[Consts.KEY_SET_NAME] = value; }
    }

    public int? Parts
    {
      get { return (int?)AdditionalProperties[Consts.KEY_PARTS]; }
      set { AdditionalProperties[Consts.KEY_PARTS] = value; }
    }

    public int? Height
    {
      get { return (int?)AdditionalProperties[Consts.KEY_HEIGHT]; }
      set { AdditionalProperties[Consts.KEY_HEIGHT] = value; }
    }

    public int? Width
    {
      get { return (int?)AdditionalProperties[Consts.KEY_WIDTH]; }
      set { AdditionalProperties[Consts.KEY_WIDTH] = value; }
    }
  }
}
