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
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.Core.Collections;
using MediaPortal.Core.InputManager;
using MediaPortal.Core.Properties;
using MediaPortal.Core.WindowManager;
using SkinEngine.Properties;
using MediaPortal.Core.Commands;
using SkinEngine.Commands;
using SkinEngine.Scripts;
using SkinEngine.Skin;
using SlimDX;
using SlimDX.Direct3D;
using SlimDX.Direct3D9;
using SkinEngine;

namespace SkinEngine.Controls
{
  public class TreeItem : Button
  {
    Property _expanded;
    TreeContainer _childContainer;
    bool _childContainerVisible;
    bool _reactOnFocusChanges;
    private TreeContainer _treeContainer;

    /// <summary>
    /// Initializes a new instance of the <see cref="TreeItem"/> class.
    /// </summary>
    /// <param name="parent">The parent.</param>
    public TreeItem(Control parent)
      : base(parent)
    {
      _expanded = new Property(false);
      _childContainer = new TreeContainer(this, "");
      _childContainerVisible = false;
      _childContainer.IsSubTree = true;
      _childContainer.Container = this;
      _childContainer.Name = "childTree";
      _childContainer.Items = new ItemsCollection();
      _reactOnFocusChanges = true;
    }

    public TreeContainer TreeContainer
    {
      get
      {
        if (_treeContainer == null)
        {
          IControl c = this.Container;
          while ((c as TreeContainer) == null)
          {
            c = c.Container;
          }
          _treeContainer = (TreeContainer)c;
        }
        return _treeContainer;
      }
      set
      {
        _treeContainer = value;
      }
    }

    public override Property PositionProperty
    {
      get
      {
        return base.PositionProperty;
      }
      set
      {
        base.PositionProperty = value;
        Property prop = new Property(new Vector3(20, 46, 0));
      }
    }

    public ItemsCollection Childs
    {
      get
      {
        if (ListItem == null) return null;
        return ListItem.SubItems;
      }
    }

    public bool IsExpanded
    {
      get
      {
        return (bool)_expanded.GetValue();
      }
      set
      {
        _expanded.SetValue(value);
        if (value == false)
        {
          _style.Controls.Remove(_childContainer);
          _childContainerVisible = false;
          _childContainer.Items = new ItemsCollection();
          TreeContainer.RootContainer.Invalidate();
        }
        else
        {
          TreeContainer cont = (TreeContainer)Container.Container;
          if (this.ListItem != null)
            _childContainer.Items = this.ListItem.SubItems;

          WindowManager win = (WindowManager)ServiceScope.Get<IWindowManager>();

          Property prop = new Property(new Vector3(20, 46, 0));
          _childContainer.PositionProperty = new PositionDependency(this.PositionProperty, prop);
          _childContainer.OriginalPosition = new Vector3(20, 46, 0);
          _childContainerVisible = true;
          _childContainer.Window = this.Window;
          _childContainer.RootContainer = this.TreeContainer.RootContainer;
          _childContainer.Styles = win.SkinLoader.LoadStyles(this.Window, _childContainer, "tree_sub_style", _childContainer);

          _style.Controls.Add(_childContainer);
          //DoInvalidate(this);
          TreeContainer.RootContainer.Invalidate();
        }
      }
    }

    public Property IsExpandedProperty
    {
      get
      {
        return _expanded;
      }
      set
      {
        _expanded = value;
      }
    }

    public void Expand()
    {
      IsExpanded = !IsExpanded;
    }

    public override void Invalidate()
    {
      //Trace.WriteLine("treeitem.invalidate");
      _style.Invalidate();
    }

    public override void Render(uint timePassed)
    {
      base.Render(timePassed);
    }



    public override bool HasFocus
    {
      get
      {
        return (bool)_hasFocus.GetValue();
      }
      set
      {
        if (value != (bool)_hasFocus.GetValue())
        {
          if (value)
          {
            Window.FocusedControl = this;
          }
          if ((bool)_hasFocus.GetValue() && Window.FocusedControl == this)
          {
            Window.FocusedControl = null;
          }
          _hasFocus.SetValue(value);
          base.OnChildFocusChanged(this);
        }
      }
    }

    public override void OnChildFocusChanged(Control childControl)
    {
      _reactOnFocusChanges = false;
      if (childControl.HasFocus)
      {
        if (_childContainerVisible && _childContainer.HasFocus)
          HasFocus = false;
        else
          HasFocus = true;
      }
      else
      {
        HasFocus = false;
      }
      _reactOnFocusChanges = true;
      base.OnChildFocusChanged(childControl);
    }

    public override bool HitTest(float x, float y)
    {
      bool focused = false;
      if (!IsVisible)
      {
        return false;
      }
      if (MouseOnly)
      {
        focused = _style.HitTest(x, y);
        if (focused && _childContainerVisible && _childContainer.HitTest(x, y))
          focused = false;
        HasMouseFocus = focused;
        return false;
      }
      if (!IsFocusable)
      {
        return false;
      }
      focused = _style.HitTest(x, y);
      if (focused && _childContainerVisible && _childContainer.HitTest(x, y))
        focused = false;
      return focused;
    }

    public override void OnKeyPressed(ref Key key)
    {
      _style.OnKeyPressed(ref key);
      base.OnKeyPressed(ref key);
    }

    public override Control PredictFocusUp(Control focusedControl, ref Key key, bool strict)
    {
      if (_childContainerVisible)
      {
        Control c = null;
        if (focusedControl == this)
          c = _childContainer.PredictFocusUp(focusedControl, ref key, strict);
        else
          c = _style.PredictFocusUp(focusedControl, ref key, strict);
        if (c != null) return c;
      }

      if (focusedControl == this) return null;
      Control c2 = _style.PredictFocusUp(focusedControl, ref key, strict);
      if (c2 != null)
      {
        //key = Key.None;
        return this;
      }
      return null;
    }

    public override Control PredictFocusDown(Control focusedControl, ref Key key, bool strict)
    {
      if (_childContainerVisible)
      {
        Control c = null;
        if (focusedControl == this)
          c = _childContainer.PredictFocusDown(focusedControl, ref key, strict);
        else
          c = _style.PredictFocusDown(focusedControl, ref key, strict);
        if (c != null) return c;
      }
      if (focusedControl == this) return null;
      Control c2 = _style.PredictFocusDown(focusedControl, ref key, strict);
      if (c2 != null)
      {
        // key = Key.None;
        return this;
      }
      return null;
    }

    public override Control PredictFocusLeft(Control focusedControl, ref Key key, bool strict)
    {
      if (_childContainerVisible && !HasFocus && _childContainer.HasFocus)
      {
        Control c = _childContainer.PredictFocusLeft(focusedControl, ref key, strict);
        if (c == null)
        {
          key = Key.None;
          return this;
        }
        return c;
      }

      if (_style.HasFocus)
      {
        Control c = _style.PredictFocusLeft(focusedControl, ref key, strict);
        if (c == null)
        {
          key = Key.None;
          return this;
        }
      }
      return _style.PredictFocusLeft(focusedControl, ref key, strict);
    }

    public override Control PredictFocusRight(Control focusedControl, ref Key key, bool strict)
    {
      if (_childContainerVisible && !HasFocus && _childContainer.HasFocus)
      {
        return _childContainer.PredictFocusRight(focusedControl, ref key, strict);
      }
      Control c = _style.PredictFocusRight(focusedControl, ref key, strict);
      if (!HasFocus && c != null)
      {
        key = Key.None;
        return this;
      }
      return c;
    }

    public override void Execute()
    {
      if (Command != null)
      {
        Command.Execute(CommandParameter);
        if (CommandResult != null)
        {
          CommandResult.Execute();
        }
        return;
      }
      if (ListItem != null)
      {
        if (ListItem.Command != null)
        {
          ListItem.Command.Execute(ListItem.CommandParameter);
          return;
        }
      }
      if (_container != null && ((_container as Button) == null))
      {
        _container.Execute();
      }

    }

    public override void Reset()
    {
      _style.Controls.Remove(_childContainer);
      _childContainerVisible = false;
      _childContainer.Items = new ItemsCollection();
    }
  }
}
