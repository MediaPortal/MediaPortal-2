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

using MediaPortal.Presentation.DataObjects;
using MediaPortal.Control.InputManager;
using MediaPortal.SkinEngine.Commands;
using MediaPortal.SkinEngine.InputManagement;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.SkinEngine.Controls.Visuals
{
  public class TreeView : HeaderedItemsControl
  {
    #region Protected fields

    protected Property _selectionChangedProperty;

    #endregion

    #region Ctor

    public TreeView()
    {
      Init();
    }

    void Init()
    {
      _selectionChangedProperty = new Property(typeof(ICommandStencil), null);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      TreeView tv = (TreeView) source;
      SelectionChanged = copyManager.GetCopy(tv.SelectionChanged);
    }

    #endregion

    #region Events

    public Property SelectionChangedProperty
    {
      get { return _selectionChangedProperty; }
    }

    public ICommandStencil SelectionChanged
    {
      get { return (ICommandStencil)_selectionChangedProperty.GetValue(); }
      set { _selectionChangedProperty.SetValue(value); }
    }

    #endregion

    #region Input handling

    public override void OnMouseMove(float x, float y)
    {
      base.OnMouseMove(x, y);
      UpdateCurrentItem();
    }

    public override void OnKeyPressed(ref Key key)
    {
      UpdateCurrentItem();
      base.OnKeyPressed(ref key);
    }

    void UpdateCurrentItem()
    {
      FrameworkElement element = FocusManager.FocusedElement;
      if (element == null)
        CurrentItem = null;
      else
      {
        // FIXME Albert78: This does not necessarily find the right TreeViewItem
        while (!(element is TreeViewItem) && element.VisualParent != null)
          element = element.VisualParent as FrameworkElement;
        CurrentItem = element.Context;
      }
      if (SelectionChanged != null)
        SelectionChanged.Execute(new object[] { CurrentItem });
    }

    #endregion
  }
}
