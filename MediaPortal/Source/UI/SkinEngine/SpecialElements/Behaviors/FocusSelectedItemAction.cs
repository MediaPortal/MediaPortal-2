#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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

using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.SkinEngine.Controls.Visuals;
using MediaPortal.UI.SkinEngine.ScreenManagement;
using MediaPortal.Utilities.DeepCopy;
using System.Collections.Generic;
using System.Linq;

namespace MediaPortal.UI.SkinEngine.SpecialElements.Behaviors
{
  /// <summary>
  /// Action that focuses the element bound to the currently selected <see cref="ListItem"/> in an <see cref="ItemsControl"/>. 
  /// </summary>
  public class FocusSelectedItemAction : IDeepCopyable
  {
    protected ItemsControl _targetObject;

    public void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      DetachFromObject();
      FocusSelectedItemAction f = (FocusSelectedItemAction)source;
      AttachToObject(copyManager.GetCopy(f._targetObject));
    }

    public void AttachToObject(ItemsControl targetObject)
    {
      _targetObject = targetObject;
      // If the element is running then the visual tree has
      // been created and we can set the focus now
      if (_targetObject.ElementState == ElementState.Running)
        SetFocusOnSelectedItem();
      // else set the focus after the prepare event is raised
      // and the visual tree has been created
      else
        _targetObject.EventOccured += OnEventOccured;
    }

    private void OnEventOccured(string eventName)
    {
      if (eventName == Screen.PREPARE_EVENT)
        SetFocusOnSelectedItem();
    }

    private void SetFocusOnSelectedItem()
    {
      ItemsControl itemsControl = _targetObject;
      if (itemsControl == null)
        return;
      // We work on the bound data items as these are always available, whereas the
      // selected ListViewItem might not be if using virtualization
      ListItem selected = (itemsControl.ItemsSource as IEnumerable<ListItem>)?.FirstOrDefault(i => i.Selected);
      if (selected != null)
        itemsControl.SetFocusOnItem(selected);
    }

    public void DetachFromObject()
    {
      if (_targetObject == null)
        return;
      _targetObject.EventOccured -= OnEventOccured;
      _targetObject = null;
    }
  }
}
