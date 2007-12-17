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

using System.Collections.Generic;
using MediaPortal.Core.Properties;
using SkinEngine.Controls;

namespace SkinEngine.Skin
{
  public class StylesCollection : Control
  {
    #region variables

    private List<Style> _styles;
    private Property _selectedIndex;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="StylesCollection"/> class.
    /// </summary>
    /// <param name="parent">The parent.</param>
    public StylesCollection(Control parent)
      : base(parent)
    {
      _styles = new List<Style>();
      _selectedIndex = new Property(0);
    }

    /// <summary>
    /// Gets or sets the styles.
    /// </summary>
    /// <value>The styles.</value>
    public List<Style> Styles
    {
      get { return _styles; }
      set { _styles = value; }
    }

    /// <summary>
    /// Gets or sets the index of the selected style.
    /// </summary>
    /// <value>The index of the selected style.</value>
    public int SelectedStyleIndex
    {
      get { return (int) _selectedIndex.GetValue(); }
      set { _selectedIndex.SetValue(value); }
    }

    public Property SelectedStyleIndexProperty
    {
      get { return _selectedIndex; }
      set { _selectedIndex = value; }
    }

    /// <summary>
    /// Gets the selected style.
    /// </summary>
    /// <value>The selected style.</value>
    public Style SelectedStyle
    {
      get
      {
        if (SelectedStyleIndex < 0 || SelectedStyleIndex >= _styles.Count)
        {
          return null;
        }
        return _styles[SelectedStyleIndex];
      }
    }

    /// <summary>
    /// Checks if a control is positioned at coordinates (x,y) 
    /// </summary>
    /// <param name="x">The x coordinate.</param>
    /// <param name="y">The y coordinate.</param>
    /// <returns></returns>
    public override bool HitTest(float x, float y)
    {
      return SelectedStyle.HitTest(x, y);
    }

    /// <summary>
    /// Renders the current selected style
    /// </summary>
    /// <param name="timePassed">The time passed.</param>
    public override void Render(uint timePassed)
    {
      SelectedStyle.Render(timePassed);
    }

    /// <summary>
    /// Resets all styles.
    /// </summary>
    public override void Reset()
    {
      base.Reset();
      foreach (Style style in _styles)
      {
        style.Reset();
      }
    }
  }
}