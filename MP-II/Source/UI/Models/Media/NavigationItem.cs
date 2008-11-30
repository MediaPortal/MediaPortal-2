#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using MediaPortal.Core;
using MediaPortal.Media.ClientMediaManager;
using MediaPortal.Media.ClientMediaManager.Views;
using MediaPortal.Presentation.DataObjects;

namespace Models.Media
{
  /// <summary>
  /// Holds a GUI item which encapsulates a view to navigate to.
  /// </summary>
  /// <remarks>
  /// Instances of this class represent view items to be displayed in a GUI view's items list.
  /// View's items lists contain view items (<see cref="NavigationItem"/>s) as well as
  /// Playable items (<see cref="PlayableItem"/>).
  /// </remarks>
  public class NavigationItem : ListItem
  {
    #region Protected fields

    protected Guid _viewId;
    protected string _overrideName;

    #endregion

    public NavigationItem(Guid viewId, string overrideName)
    {
      _viewId = viewId;
      _overrideName = overrideName;
    }

    public void UpdateData()
    {
      MediaManager mediaManager = ServiceScope.Get<MediaManager>();
      ViewMetadata viewMetadata = mediaManager.GetViewMetadata(_viewId);
      if (string.IsNullOrEmpty(_overrideName))
        SetLabel("Name", viewMetadata.DisplayName);
      else
        SetLabel("Name", _overrideName);
      // TODO: Other properties
    }

    public Guid ViewId
    {
      get { return _viewId; }
    }
  }
}
