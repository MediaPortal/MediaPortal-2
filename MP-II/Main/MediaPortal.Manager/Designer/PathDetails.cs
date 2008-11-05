#region Copyright (C) 2007-2008 Team MediaPortal

/*
 *  Copyright (C) 2007-2008 Team MediaPortal
 *  http://www.team-mediaportal.com
 *
 *  This file is part of MediaPortal II
 *
 *  MediaPortal II is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  MediaPortal II is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
 * 
 */

#endregion

using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

using MediaPortal.Configuration.Settings;


namespace MediaPortal.Manager
{

  public class PathDetails
  {

    #region Variables

    private Path _path;
    private TextBox _txtValue;
    private Button _btnBrowse;

    #endregion

    #region Properties

    public Path Path
    {
      get { return _path; }
      set { _path = value; }
    }

    public TextBox TextBox
    {
      get { return _txtValue; }
      set { _txtValue = value; }
    }

    public Button ButtonBrowse
    {
      get { return _btnBrowse; }
      set { _btnBrowse = value; }
    }

    #endregion

    #region Constructors

    public PathDetails(Path path, TextBox textValue, Button buttonBrowse)
    {
      _path = path;
      _txtValue = textValue;
      _btnBrowse = buttonBrowse;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Returns the Path as a Panel, with the PathDetails as it's Tag.
    /// </summary>
    /// <param name="size"></param>
    /// <param name="location"></param>
    /// <returns></returns>
    public Panel GetAsPanel(Size size, Point location)
    {
      Panel panel = new Panel();
      panel.Size = size;
      panel.Location = location;
      panel.Controls.Add(_txtValue);
      panel.Controls.Add(_btnBrowse);
      panel.Tag = this;
      return panel;
    }

    #endregion

  }

}
