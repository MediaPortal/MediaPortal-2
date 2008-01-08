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
using System.Collections.Generic;
using System.Text;

namespace SkinEngine.Controls.Visuals
{
  public interface IScrollInfo
  {
    /// <summary>
    /// Scrolls down within content by one logical unit.
    /// </summary>
    /// 
    bool LineDown();

    /// <summary>
    /// Scrolls left within content by one logical unit.
    /// </summary>
    /// 
    bool LineLeft();

    /// <summary>
    /// Scrolls right within content by one logical unit.
    /// </summary>
    bool LineRight();

    /// <summary>
    /// Scrolls up within content by one logical unit.
    /// </summary>
    bool LineUp();

    /// <summary>
    /// Forces content to scroll until the coordinate space of a Visual object is visible.
    /// </summary>
    bool MakeVisible();

    /// <summary>
    /// Scrolls down within content by one page.
    /// </summary>
    bool PageDown();

    /// <summary>
    /// Scrolls left within content by one page.
    /// </summary>
    bool PageLeft();

    /// <summary>
    /// Scrolls right within content by one page.
    /// </summary>
    bool PageRight();

    /// <summary>
    /// Scrolls up within content by one page.
    /// </summary>
    bool PageUp();

    void Home();
    void End();

    double LineHeight {get;}

    double LineWidth {get;}

    void Reset();
  }
}
