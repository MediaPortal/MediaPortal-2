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

using MediaPortal.UI.SkinEngine.Controls.Visuals;
using MediaPortal.UI.SkinEngine.MpfElements.Input;
using System.Windows.Forms;

namespace MediaPortal.UiComponents.Nereus.Controls
{
  public class HomeTilesScrollContentPresenter : AnimatedScrollContentPresenter
  {
    protected const int TILE_SCROLL_PIXEL = 100;

    public HomeTilesScrollContentPresenter()
    {
      PreviewMouseWheel += OnMouseWheel;      
    }

    public void OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
      int scrollByLines = SystemInformation.MouseWheelScrollLines; // Use the system setting as default.

      if (NumberOfVisibleLines != 0) // If ScrollControl can shown less items, use this as limit.
        scrollByLines = NumberOfVisibleLines;

      int numLines = e.NumDetents * scrollByLines;

      if (numLines < 0)
        ScrollRight(-1 * numLines);
      else if (numLines > 0)
        ScrollLeft(numLines);

      e.Handled = true;
    }

    public bool ScrollLeft(int numLines)
    {
      if (base.IsViewPortAtLeft)
        return false;
      SetScrollOffset(_scrollOffsetX + numLines * TILE_SCROLL_PIXEL, _scrollOffsetY);
      return true;
    }

    public bool ScrollRight(int numLines)
    {
      if (base.IsViewPortAtRight)
        return false;
      SetScrollOffset(_scrollOffsetX - numLines * TILE_SCROLL_PIXEL, _scrollOffsetY);
      return true;
    }

    public override bool IsViewPortAtLeft => true;
    public override bool IsViewPortAtRight => true;
    public override bool IsViewPortAtTop => true;
    public override bool IsViewPortAtBottom => true;
  }
}
