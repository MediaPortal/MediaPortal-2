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

using System;
using MediaPortal.Core;
using MediaPortal.Core.WindowManager;

namespace SkinEngine
{
  public class ShowWaitCursor : IDisposable
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="ShowWaitCursor"/> class.
    /// </summary>
    public ShowWaitCursor()
    {
      WindowManager manager = (WindowManager) ServiceScope.Get<IWindowManager>();
      IWindow window = manager.CurrentWindow;
      window.WaitCursorVisible = true;
    }

    #region IDisposable Members

    /// <summary>
    /// Disposes this instance.
    /// </summary>
    public void Dispose()
    {
      WindowManager manager = (WindowManager) ServiceScope.Get<IWindowManager>();
      IWindow window = manager.CurrentWindow;
      window.WaitCursorVisible = false;
    }

    #endregion
  }
}