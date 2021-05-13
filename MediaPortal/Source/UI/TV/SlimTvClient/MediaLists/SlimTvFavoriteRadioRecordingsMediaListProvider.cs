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

using MediaPortal.Common.Commands;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Plugins.SlimTv.Client.Models.Navigation;
using MediaPortal.Plugins.SlimTv.Client.TvHandler;
using MediaPortal.UiComponents.Media.MediaLists;
using MediaPortal.UiComponents.Media.Models;
using System;
using MediaPortal.Plugins.SlimTv.Interfaces.Aspects;

namespace MediaPortal.Plugins.SlimTv.Client.MediaLists
{
  public class SlimTvFavoriteRadioRecordingsMediaListProvider : BaseFavoriteMediaListProvider
  {
    public SlimTvFavoriteRadioRecordingsMediaListProvider()
    {
      _changeAspectIds = new[] { RecordingAspect.ASPECT_ID, AudioAspect.ASPECT_ID };
      _necessaryMias = SlimTvConsts.NECESSARY_RADIO_RECORDING_MIAS;
      _optionalMias = SlimTvConsts.OPTIONAL_RADIO_RECORDING_MIAS;
      _playableConverterAction = mi => new RecordingItem(mi) { Command = new MethodDelegateCommand(() => PlayItemsModel.CheckQueryPlayAction(mi)) };
    }
  }
}
