#region Copyright (C) 2007-2010 Team MediaPortal

/*
 *  Copyright (C) 2007-2010 Team MediaPortal
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

using System.Collections.Generic;
using MediaPortal.Core.Localization;

namespace MediaPortal.Core.Configuration
{
  /// <summary>
  /// Base class for all configuration setting classes holding a list of localizable string items.
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
