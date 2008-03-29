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
using MediaPortal.Core;
using MediaPortal.Core.WindowManager;

namespace SkinEngine
{
  /// <summary>
  /// Class to manage displaying wait cursors in the current <see cref="IWindow"/>.
  /// This class can be used to simplify displaying and resetting a wait cursor when
  /// used in a single method context, i.e. when the cursor should be shown and hidden again
  /// in the same method execution.
  /// To show a wait cursor, just create an instance of <see cref="ShowWaitCursor"/>, and dispose
  /// it afterwards:
  /// 
  /// <code>
  ///   ShowWaitCursor wc = new ShowWaitCursor();
  ///   try
  ///   {
  ///     [... Code which requires the wait cursor ...]
  ///   }
  ///   finally
  ///   {
  ///     wc.Dispose();
  ///   }
  /// </code>
  /// </summary>
  public class ShowWaitCursor : IDisposable
  {
    protected bool _oldWaitCursorState;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShowWaitCursor"/> class.
    /// </summary>
    public ShowWaitCursor()
    {
      WindowManager manager = (WindowManager) ServiceScope.Get<IWindowManager>();
      IWindow window = manager.CurrentWindow;
      _oldWaitCursorState = window.WaitCursorVisible;
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
      window.WaitCursorVisible = _oldWaitCursorState;
    }

    #endregion
  }
}
