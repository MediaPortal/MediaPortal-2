#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System.Collections.Generic;
using MediaPortal.Core.Localization;
using MediaPortal.Core.Configuration.ConfigurationClasses;
using System;
using DirectShowLib;
using MediaPortal.UI.Players.Video.Tools;

namespace MediaPortal.UI.Players.Video.Settings.Configuration
{
  public class GenericCodecSelection : SingleSelectionList
  {
    private readonly Guid[] _inputMediaTypes;
    private readonly Guid[] _outputMediaTypes;
    protected DsGuid _currentSelection;
    protected List<CodecInfo> _codecList;

    public GenericCodecSelection(Guid[] inputMediaTypes, Guid[] outputMediaTypes)
    {
      _inputMediaTypes = inputMediaTypes;
      _outputMediaTypes = outputMediaTypes;
    }

    public override void Load()
    {
      // Fill items
      _codecList = CodecHandler.GetFilters(_inputMediaTypes, _outputMediaTypes);
      _items = new List<IResourceString>(_codecList.Count);

      int idx = 0;
      foreach (CodecInfo codecInfo in _codecList)
      {
        _items.Add(LocalizationHelper.CreateStaticString(codecInfo.Name));
        
        // check if it is current the selected codec, use Guid because DsGuid's compare returns false.
        if (_currentSelection != null && _currentSelection.ToGuid() == codecInfo.GetCLSID().ToGuid())
          Selected = idx;

        idx++;
      }
    }
  }
}
