#region Copyright (C) 2007-2009 Team MediaPortal

/*
 *  Copyright (C) 2007-2009 Team MediaPortal
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

using System.Drawing;
using System.Windows.Forms;

using MediaPortal.UI.Configuration.Settings;

namespace MediaPortal.Manager
{
  public class PreferenceListDetails
  {
    #region Variables

    private PreferenceList _preferenceList;
    private ListBox _list;
    private Button _up;
    private Button _down;

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the PreferenceList linked to the controls.
    /// </summary>
    public PreferenceList PreferenceList
    {
      get { return _preferenceList; }
      set { _preferenceList = value; }
    }

    /// <summary>
    /// Gets or sets the list containing items.
    /// </summary>
    public ListBox ListBox
    {
      get { return _list; }
      set { _list = value; }
    }

    /// <summary>
    /// Gets or sets the Button to move items upwards.
    /// </summary>
    public Button ButtonUp
    {
      get { return _up; }
      set { _up = value; }
    }

    /// <summary>
    /// Gets or sets the Button to move items downwards.
    /// </summary>
    public Button ButtonDown
    {
      get { return _down; }
      set { _down = value; }
    }

    #endregion

    #region Constructors

    public PreferenceListDetails(PreferenceList preferenceList, ListBox listBox, Button buttonUp, Button buttonDown)
    {
      _preferenceList = preferenceList;
      _list = listBox;
      _up = buttonUp;
      _down = buttonDown;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Returns the PreferenceList as a Panel, with the PreferenceListDetails as it's Tag.
    /// </summary>
    /// <param name="size"></param>
    /// <param name="location"></param>
    /// <returns></returns>
    public Panel GetAsPanel(Size size, Point location)
    {
      Panel panel = new Panel();
      panel.Size = size;
      panel.Location = location;
      panel.Controls.Add(_list);
      panel.Controls.Add(_up);
      panel.Controls.Add(_down);
      panel.Tag = this;
      return panel;
    }

    #endregion
  }

}
