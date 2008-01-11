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

using System.Collections.Generic;
using System.Xml;

namespace SkinEngine.Skin
{
  public class SkinLoaderContext
  {
    #region variables

    private List<string> _includes;
    private Dictionary<string, XmlDocument> _styles;
    private string _windowName;
    public int Index = 0;
    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="SkinLoaderContext"/> class.
    /// </summary>
    /// <param name="windowName">Name of the window.</param>
    public SkinLoaderContext(string windowName)
    {
      _windowName = windowName;
      _includes = new List<string>();
      _styles = new Dictionary<string, XmlDocument>();
    }

    /// <summary>
    /// returns a list of all styles (filenames) included by this window
    /// </summary>
    /// <value>The includes.</value>
    public List<string> Includes
    {
      get { return _includes; }
    }

    /// <summary>
    /// Gets the styles.
    /// </summary>
    /// <value>The styles.</value>
    public Dictionary<string, XmlDocument> Styles
    {
      get { return _styles; }
    }

    /// <summary>
    /// Gets the name of the window.
    /// </summary>
    /// <value>The name of the window.</value>
    public string WindowName
    {
      get { return _windowName; }
    }
  }
}
