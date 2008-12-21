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
using MediaPortal.Presentation.DataObjects;


namespace MediaPortal.Configuration
{

  /// <summary>
  /// ConfigItemList has no actual functionality implemented,
  /// it's only used to define that the ConfigBase has a list of items.
  /// </summary>
  public abstract class ConfigItemList : ConfigSetting
  {
    #region Variables

    protected IList<IResourceString> _items = new List<IResourceString>();

    #endregion

    #region Public properties

    /// <summary>
    /// Gets all items in the list.
    /// </summary>
    public IList<IResourceString> Items
    {
      get { return _items; }
    }

    #endregion

    public override IEnumerable<string> GetSearchTexts()
    {
      List<string> result = new List<string>();
      result.AddRange(base.GetSearchTexts());
      foreach (IResourceString sb in _items)
        result.Add(sb.Evaluate());
      return result;
    }
  }
}
