#region Copyright (C) 2007-2009 Team MediaPortal

/*
 *  Copyright (C) 2007-2009 Team MediaPortal
 *  http://www.team-mediaportal.com
 *
 *  This file is part of MediaPortal 2
 *
 *  MediaPortal 2 is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  MediaPortal 2 is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
 * 
 */

#endregion

using MediaPortal.UI.Configuration;

using FormControl = System.Windows.Forms.Control;


namespace MediaPortal.Manager
{
  public class SectionDetails
  {
    #region Variables

    private ConfigSection _section;
    private FormControl _control;
    private bool _designed;
    private bool _rightToLeft;
    private int _width;

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the specifications for the section.
    /// </summary>
    public ConfigSection Section
    {
      get { return _section; }
      set { _section = value; }
    }

    /// <summary>
    /// Gets or sets all controls inside the section.
    /// </summary>
    public FormControl Control
    {
      get { return _control; }
      set { _control = value; }
    }

    /// <summary>
    /// Gets or sets if the current SectionDetails have been designed to the Control property.
    /// </summary>
    public bool Designed
    {
      get { return _designed; }
      set { _designed = value; }
    }

    /// <summary>
    /// Gets or sets if controls are built from right to left.
    /// </summary>
    public bool RightToLeft
    {
      get { return _rightToLeft; }
      set { _rightToLeft = value; }
    }

    /// <summary>
    /// Gets or sets the width the controls are built for.
    /// </summary>
    public int Width
    {
      get { return _width; }
      set { _width = value; }
    }

    #endregion

    public SectionDetails()
    {
      _section = new ConfigSection();
      _control = new FormControl();
      _designed = false;
      _rightToLeft = false;
      _width = -1;
    }
  }
}
