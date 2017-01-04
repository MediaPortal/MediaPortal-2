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
using MediaPortal.Extensions.MetadataExtractors.Aspects;
using MediaPortal.Plugins.SlimTv.Client.TvHandler;
using MediaPortal.UiComponents.Media.Models.Navigation;

namespace MediaPortal.Plugins.SlimTv.Client.Models.Navigation
{
  public class RecordingItem : VideoItem
  {
    public RecordingItem(MediaItem mediaItem)
      : base(mediaItem)
    {
    }

    public override void Update(MediaItem mediaItem)
    {
      base.Update(mediaItem);
      SingleMediaItemAspect recordingAspect;
      if (MediaItemAspect.TryGetAspect(mediaItem.Aspects, RecordingAspect.Metadata, out recordingAspect))
      {
        SimpleTitle = Title;
        Channel = (string)recordingAspect[RecordingAspect.ATTR_CHANNEL];
        StartTime = (DateTime?)recordingAspect[RecordingAspect.ATTR_STARTTIME];
        EndTime = (DateTime?)recordingAspect[RecordingAspect.ATTR_ENDTIME];
      }
      FireChange();
    }

    public string Channel
    {
      get { return this[SlimTvConsts.KEY_CHANNEL]; }
      set { SetLabel(SlimTvConsts.KEY_CHANNEL, value); }
    }

    public DateTime? StartTime
    {
      get { return (DateTime?)AdditionalProperties[SlimTvConsts.KEY_STARTTIME]; }
      set { AdditionalProperties[SlimTvConsts.KEY_STARTTIME] = value; }
    }

    public DateTime? EndTime
    {
      get { return (DateTime?)AdditionalProperties[SlimTvConsts.KEY_ENDTIME]; }
      set { AdditionalProperties[SlimTvConsts.KEY_ENDTIME] = value; }
    }
  }
}
