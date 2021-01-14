#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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

using System.Windows.Forms;

namespace MediaPortal.ServiceMonitor.Utilities
{

  public delegate void WinProcHandler(ref Message m);

  /// <summary>
  /// Creates a dummy instance that provides an empty pointer rather than a real window handler.
  /// Used at design time.
  /// </summary>
  public class WindowMessageSink : NativeWindow
  {
    #region Events

    /// <summary>
    /// Public event for the processing window messages.
    /// </summary>
    public event WinProcHandler OnWinProc;

    #endregion

    public WindowMessageSink()
    {
      CreateHandle(new CreateParams());
    }

    protected override void WndProc(ref Message m)
    {
      var handler = OnWinProc;
      handler?.Invoke(ref m);

      base.WndProc(ref m);
    }
  }
}
