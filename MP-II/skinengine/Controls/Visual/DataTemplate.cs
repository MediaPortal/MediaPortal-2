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
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using MediaPortal.Core.Properties;
using SkinEngine.Controls.Visuals.Styles;
using MediaPortal.Core.InputManager;

using SkinEngine;


namespace SkinEngine.Controls.Visuals
{
  public class DataTemplate : FrameworkElement
  {
    public DataTemplate()
    {
    }

    public DataTemplate(DataTemplate template)
    {
    }

    public override object Clone()
    {
      return new DataTemplate(this);
    }

    /// <summary>
    /// Gets or sets the type of the data. (not used in our xaml engine)
    /// </summary>
    /// <value>The type of the data.</value>
    public string DataType
    {
      get
      {
        return "";
      }
      set
      {
      }
    }
  }
}
