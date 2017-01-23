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

using System.Collections.Generic;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Configuration.ConfigurationClasses;
using System;
using DirectShow;
using MediaPortal.UI.Players.Video.Tools;

namespace MediaPortal.UI.Players.Video.Settings.Configuration
{
  public abstract class GenericCodecSelection : SingleSelectionList
  {
    private readonly Guid[] _inputMediaTypes;
    private readonly Guid[] _outputMediaTypes;
    protected DsGuid _currentSelection;
    protected string _currentSelectionName;
    protected bool _selectByName;
    protected List<CodecInfo> _codecList;

    protected GenericCodecSelection(Guid[] inputMediaTypes, Guid[] outputMediaTypes)
    {
      _inputMediaTypes = inputMediaTypes;
      _outputMediaTypes = outputMediaTypes;
    }

    protected virtual void GetAvailableFilters()
    {
      _codecList = CodecHandler.GetFilters(_inputMediaTypes, _outputMediaTypes);
    }

    public override void Load()
    {
      // Fill items
      GetAvailableFilters();
      _items = new List<IResourceString>(_codecList.Count);

      int idx = 0;
      foreach (CodecInfo codecInfo in _codecList)
      {
        _items.Add(LocalizationHelper.CreateStaticString(codecInfo.Name));

        // check if it is current the selected codec, use Guid because DsGuid's compare returns false.
        if (_selectByName && _currentSelectionName == codecInfo.Name || !_selectByName && _currentSelection != null && _currentSelection.ToGuid() == codecInfo.GetCLSID().ToGuid())
          Selected = idx;

        idx++;
      }
    }
  }
}
