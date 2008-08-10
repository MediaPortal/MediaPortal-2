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

using Presentation.SkinEngine.Controls.Visuals.Styles;

namespace Presentation.SkinEngine.Controls.Visuals
{
  /// <summary>
  /// Used within the template of an item control to specify the place in the control’s visual tree 
  /// where the ItemsPanel defined by the ItemsControl is to be added.
  /// http://msdn2.microsoft.com/en-us/library/system.windows.controls.itemspresenter.aspx
  /// </summary>
  public class ItemsPresenter : Control
  {
    public ItemsPresenter()
    { }

    public void ApplyTemplate(FrameworkTemplate template)
    {
      ControlTemplate ct = new ControlTemplate();
      ct.AddChild(template.LoadContent());
      this.Template = ct;
    }

    public void SetControlTemplate(ControlTemplate template)
    { }
  }
}
