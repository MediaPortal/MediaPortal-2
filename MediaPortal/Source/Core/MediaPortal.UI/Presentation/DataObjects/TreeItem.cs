#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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

using System.Collections.Generic;
using MediaPortal.Common.Localization;
using MediaPortal.Utilities;

namespace MediaPortal.UI.Presentation.DataObjects
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

    protected ItemsList _subItems = new ItemsList();

    #endregion

    /// <see cref="ListItem(string, string, bool)"/>
    public TreeItem(string name, string value, bool testLocalized) : base(name, value, testLocalized) { }

    /// <see cref="ListItem(string, string)"/>
    public TreeItem(string name, string value) : base(name, value) { }

    /// <see cref="ListItem(string, StringId)"/>
    public TreeItem(string name, StringId value) : base(name, value) { }

    /// <see cref="ListItem(string, IResourceString)"/>
    public TreeItem(string name, IResourceString value) : base(name, value) { }

    /// <summary>
    /// Initializes a new empty instance of the <see cref="ListItem"/> class.
    /// All attributes are set to default values.
    /// </summary>
    public TreeItem() { }

    /// <summary>
    /// Returns the collection of sub items of this item.
    /// </summary>
    public ItemsList SubItems
    {
      get { return _subItems; }
    }

    public override string ToString()
    {
      IList<string> l = new List<string>();
      foreach (KeyValuePair<string, IResourceString> kvp in _labels)
        l.Add(kvp.Key + "=" + kvp.Value.Evaluate());
      if (_subItems.Count > 0)
        l.Add(_subItems.Count + " sub items");
      return typeof(ListItem).Name + ": " + StringUtils.Join(", ", l);
    }
  }
}
