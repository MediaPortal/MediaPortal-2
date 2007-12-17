#region Copyright (C) 2007 Team MediaPortal

/*
    Copyright (C) 2007 Team MediaPortal
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

using MediaPortal.Core;
using MediaPortal.Core.Properties;
using MediaPortal.Core.WindowManager;
using SkinEngine.Controls;

namespace SkinEngine
{
  public class BooleanPropertyControl : IBooleanProperty
  {
    private IBooleanProperty _property;
    private string _controlName;
    private Control _control;

    public BooleanPropertyControl(IBooleanProperty property, string controlName)
    {
      _property = property;
      _controlName = controlName;
    }

    public bool Evaluate(IControl control, IControl container)
    {
      if (_control == null)
      {
        IWindowManager manager = (IWindowManager) ServiceScope.Get<IWindowManager>();
        IWindow window = manager.CurrentWindow;
        _control = ((Window) window).GetControlByName(_controlName);
      }
      if (_control == null)
      {
        return false;
      }
      return _property.Evaluate(_control, _control);
    }
  }
}