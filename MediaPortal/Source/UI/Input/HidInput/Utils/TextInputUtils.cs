#region Copyright (C) 2007-2021 Team MediaPortal

/*
    Copyright (C) 2007-2021 Team MediaPortal
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

using MediaPortal.Common;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.SkinEngine.Controls.Visuals;
using MediaPortal.UI.SkinEngine.ScreenManagement;

namespace HidInput.Utils
{
  public static class TextInputUtils
  {
    /// <summary>
    /// Checks if the currently focused control requires text input.
    /// </summary>
    /// <returns><c>True</c> if the focused control requires text input.</returns>
    public static bool DoesCurrentFocusedControlNeedTextInput()
    {
      var sm = ServiceRegistration.Get<IScreenManager>(false) as ScreenManager;
      if (sm == null)
        return false;

      Visual focusedElement = sm.FocusedScreen?.FocusedElement;
      while (focusedElement != null)
      {
        // Currently only the TextControl requires text input but ideally this check would be extensible
        // as a plugin could potentially add a new control which wouldn't be handled here.
        if (focusedElement is TextControl)
          return true;
        focusedElement = focusedElement.VisualParent;
      }
      return false;
    }
  }
}
