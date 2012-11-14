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

using System.Collections.Generic;
using System.Drawing;
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.UI.SkinEngine.Rendering;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals
{
  public class VirtualKeyboardPresenter : FrameworkElement
  {
    protected FrameworkElement _keyboardLayoutControl = null;

    public override void Dispose()
    {
      MPF.TryCleanupAndDispose(_keyboardLayoutControl);
      base.Dispose();
    }

    public FrameworkElement KeyboardLayoutControl
    {
      get { return _keyboardLayoutControl; }
    }

    public void SetKeyboardLayoutControl(VirtualKeyboardControl parent, FrameworkElement keyboardLayoutControl)
    {
      FrameworkElement oldKeyboardControl = _keyboardLayoutControl;
      _keyboardLayoutControl = null;
      if (oldKeyboardControl != null)
        oldKeyboardControl.CleanupAndDispose();
      if (keyboardLayoutControl == null)
        return;
      keyboardLayoutControl.Context = parent;
      keyboardLayoutControl.LogicalParent = this;
      keyboardLayoutControl.VisualParent = this;
      keyboardLayoutControl.SetScreen(Screen);
      keyboardLayoutControl.SetElementState(_elementState);
      if (IsAllocated)
        keyboardLayoutControl.Allocate();
      _keyboardLayoutControl = keyboardLayoutControl;
      InvalidateLayout(true, true);
    }

    public override void RenderOverride(RenderContext localRenderContext)
    {
      base.RenderOverride(localRenderContext);
      FrameworkElement keyboardControl = _keyboardLayoutControl;
      if (keyboardControl == null)
        return;
      keyboardControl.Render(localRenderContext);
    }

    protected override SizeF CalculateInnerDesiredSize(SizeF totalSize)
    {
      FrameworkElement keyboardControl = _keyboardLayoutControl;
      if (keyboardControl == null)
        return SizeF.Empty;
      keyboardControl.Measure(ref totalSize);
      return totalSize;
    }

    protected override void ArrangeOverride()
    {
      base.ArrangeOverride();
      FrameworkElement keyboardControl = _keyboardLayoutControl;
      if (keyboardControl == null)
        return;
      keyboardControl.Arrange(_innerRect);
    }

    public override void AddChildren(ICollection<UIElement> childrenOut)
    {
      base.AddChildren(childrenOut);
      FrameworkElement keyboardControl = _keyboardLayoutControl;
      if (keyboardControl != null)
        childrenOut.Add(keyboardControl);
    }

    // Allocate/Deallocate of _keyboardLayoutControl not necessary because UIElement handles all direct children
  }
}