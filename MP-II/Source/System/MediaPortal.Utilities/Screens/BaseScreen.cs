#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Windows.Forms;

namespace MediaPortal.Utilities.Screens
{
  public partial class BaseScreen : Form
  {
    public enum Image
    {
      info,
      bug,
      idea
    }

    public BaseScreen()
    {
      InitializeComponent();
    }

    public BaseScreen(string windowTitle, string title, string details, Image image) : this()
    {

      this.Text = windowTitle;
      this.tbTitle.Text = title;
      this.tbDetails.Text = details;
      switch (image)
      {
        case Image.info:
          this.pbIcon.Image = global::MediaPortal.Utilities.Properties.Resources.info;
          break;
        case Image.bug:
          this.pbIcon.Image = global::MediaPortal.Utilities.Properties.Resources.bug;
          break;
        case Image.idea:
          this.pbIcon.Image = global::MediaPortal.Utilities.Properties.Resources.ideas;
          break;
        default:
          break;
      }
    }

  }
}
