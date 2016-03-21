using System;
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

namespace MediaPortal.Plugins.Transcoding.Interfaces.Profiles.Setup.Settings
{
  public class VideoSettings
  {
    public int MaxHeight = 1080;
    public QualityMode Quality = QualityMode.Default;
    public Coder CoderType = Coder.Default;
    public int QualityFactor = 3;

    public LevelCheck H264LevelCheckMethod = LevelCheck.Any;
    public int H264QualityFactor = 25;
    public EncodingPreset H264TargetPreset = EncodingPreset.Default;
    public EncodingProfile H264TargetProfile = EncodingProfile.Baseline;
    public float H264Level = 3.0F;

    public int H265QualityFactor = 25;
    public EncodingPreset H265TargetPreset = EncodingPreset.Default;
    public EncodingProfile H265TargetProfile = EncodingProfile.Main;
    public float H265Level = 3.0F;

    public int H262QualityFactor = 3;
    public EncodingPreset H262TargetPreset = EncodingPreset.Default;
    public EncodingProfile H262TargetProfile = EncodingProfile.Main;
  }
}
