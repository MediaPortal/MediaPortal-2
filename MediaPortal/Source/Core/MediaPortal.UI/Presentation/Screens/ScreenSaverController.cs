#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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

using System;
using MediaPortal.Common.General;

namespace MediaPortal.UI.Presentation.Screens
{
  /// <summary>
  /// Controller instance for the screen saver. This instance provides a means to temporary control the MP2 system's screen saver.
  /// If this instance is disposed, it doesn't override the original screen saver handling any more.
  /// </summary>
  public class ScreenSaverController : IDisposable
  {
    protected ParameterlessMethod _disposeDlgt;
    protected bool _screenSaverDisabled = false;
    protected bool _screenSaverActive = false;

    public ScreenSaverController(ParameterlessMethod disposeDlgt)
    {
      _disposeDlgt = disposeDlgt;
    }

    ~ScreenSaverController()
    {
      Dispose();
    }

    public void Dispose()
    {
      if (_disposeDlgt != null)
        _disposeDlgt();
      _disposeDlgt = null;
    }

    /// <summary>
    /// Returns the information if the MediaPortal 2 internal screen saver's active state is overridden by this screen saver controller.
    /// </summary>
    public bool? IsScreenSaverActiveOverride
    {
      get
      {
        bool? result = null;
        if (_screenSaverActive)
          result = true;
        if (_screenSaverDisabled)
          result = false;
        return result;
      }
    }

    /// <summary>
    /// Gets or sets if the MediaPortal 2 internal screen saver state is disabled by this screen saver controller.
    /// </summary>
    public bool IsScreenSaverDisabled
    {
      get { return _screenSaverDisabled; }
      set { _screenSaverDisabled = value; }
    }

    /// <summary>
    /// Gets or sets if the MediaPortal 2 internal screen saver state is set to active by this screen saver controller.
    /// This property is overruled by property <see cref="IsScreenSaverDisabled"/>.
    /// </summary>
    public bool IsScreenSaverActive
    {
      get { return _screenSaverActive; }
      set { _screenSaverActive = value; }
    }
  }
}
