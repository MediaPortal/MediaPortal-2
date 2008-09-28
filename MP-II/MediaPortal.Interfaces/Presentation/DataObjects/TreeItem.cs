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
using MediaPortal.Presentation.Localisation;

namespace MediaPortal.Presentation.DataObjects
{
  /// <summary>
  /// Common class for wrapping single tree "items" to be displayed in the GUI.
  /// </summary>
  /// <remarks>
  /// Instances of the <see cref="TreeItem"/> class add the following properties to those of
  /// the <see cref="ListItem"/> class:
  /// <list type="bullet">
  /// <item>Sub items</item>
  /// </list>
  /// </remarks>
  public class TreeItem : ListItem
  {
    #region Protected fields

    protected ItemsCollection _subItems = new ItemsCollection();

    #endregion

    /// <see cref="ListItem(string, string)"/>
    public TreeItem(string name, string value) : base(name, value) { }

    /// <see cref="ListItem(string, StringId)"/>
    public TreeItem(string name, StringId value) : base(name, value) { }

    /// <summary>
    /// Initializes a new empty instance of the <see cref="ListItem"/> class.
    /// All attributes are set to default values.
    /// </summary>
    public TreeItem() { }

    /// <summary>
    /// Returns the collection of sub items of this item.
    /// </summary>
    public ItemsCollection SubItems
    {
      get { return _subItems; }
    }

    public override string ToString()
    {
      List<string> l = new List<string>();
      foreach (KeyValuePair<string, IStringBuilder> kvp in _labels)
        l.Add(kvp.Key + "=" + kvp.Value.Evaluate());
      if (_subItems.Count > 0)
        l.Add(_subItems.Count + " sub items");
      string[] sl = new string[l.Count];
      l.CopyTo(sl);
      return typeof(ListItem).Name + ": " + string.Join(", ", sl);
    }
  }
}
