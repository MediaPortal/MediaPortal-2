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
using System.Drawing;
using MediaPortal.UI.SkinEngine.Rendering;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals
{
  public class VirtualKeyboardPresenter : FrameworkElement
  {
    protected FrameworkElement _currentKeyboardControl = null;

    public FrameworkElement CurrentKeyboardControl
    {
      get { return _currentKeyboardControl; }
    }

    public void SetKeyboardControl(VirtualKeyboardControl parent, FrameworkElement keyboardControl)
    {
      FrameworkElement oldKeyboardControl = _currentKeyboardControl;
      _currentKeyboardControl = null;
      if (oldKeyboardControl != null)
        oldKeyboardControl.VisualParent = null;
      if (keyboardControl == null)
        return;
      keyboardControl.Context = parent;
      keyboardControl.SetScreen(Screen);
      keyboardControl.VisualParent = this;
      _currentKeyboardControl = keyboardControl;
      InvalidateLayout();
    }

    public override void DoRender(RenderContext localRenderContext)
    {
      base.DoRender(localRenderContext);
      FrameworkElement keyboardControl = _currentKeyboardControl;
      if (keyboardControl == null)
        return;
      keyboardControl.Render(localRenderContext);
    }

    protected override SizeF CalculateDesiredSize(SizeF totalSize)
    {
      FrameworkElement keyboardControl = _currentKeyboardControl;
      if (keyboardControl == null)
        return new SizeF();
      keyboardControl.Measure(ref totalSize);
      return totalSize;
    }

    protected override void ArrangeOverride()
    {
      base.ArrangeOverride();
      FrameworkElement keyboardControl = _currentKeyboardControl;
      if (keyboardControl == null)
        return;
      keyboardControl.Arrange(_innerRect);
    }

    public override void AddChildren(ICollection<UIElement> childrenOut)
    {
      base.AddChildren(childrenOut);
      FrameworkElement keyboardControl = _currentKeyboardControl;
      if (keyboardControl != null)
        childrenOut.Add(keyboardControl);
    }

  }
}