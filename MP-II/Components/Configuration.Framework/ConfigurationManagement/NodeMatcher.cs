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
using System.Globalization;

using MediaPortal.Core;
using MediaPortal.Presentation.Localisation;

namespace MediaPortal.Configuration
{

  /// <summary>
  /// Matches a ConfigBase with a specified value.
  /// </summary>
  internal class NodeMatcher
  {

    #region Variables

    private ConfigBase _setting;
    private CultureInfo _culture;
    private IList<string> _keys;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of NodeMatcher.
    /// </summary>
    /// <param name="setting">Setting to match values with.</param>
    public NodeMatcher(ConfigBase setting)
    {
      _setting = setting;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Matches the specified value with the current instance of ConfigBase.
    /// </summary>
    /// <param name="value">Value to match.</param>
    /// <returns>
    /// Value between 0 and 1,
    /// with 1 representing an exact match.
    /// And 0 representing no match.
    /// </returns>
    public float Match(string value)
    {
      if (_keys == null || _culture != ServiceScope.Get<ILocalisation>().CurrentCulture)
        UpdateSearchKeys();
      lock (_keys)
      {
        value = value.ToLower(_culture);
        if (value == _setting.Text.ToString().ToLower(_culture))
          return 1;
        double result = 0;
        foreach (string key in _keys)
        {
          if (key == value)
            result++;
          else if (key.Contains(value))
            result += 0.5;
        }
        return (float)(result / _keys.Count);
      }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Updates the search keys and sets the current culture.
    /// </summary>
    private void UpdateSearchKeys()
    {
      _culture = ServiceScope.Get<ILocalisation>().CurrentCulture;
      _keys = new List<string>();
      lock (_keys)
      {
        // Add setting information to the keys
        if (_setting.Text.Label != "[system.invalid]")
          _keys.Add(_setting.Text.ToString().ToLower(_culture));
        if (_setting is ConfigItem
          && ((ConfigItem)_setting).Help.Label != "[system.invalid]")
          _keys.Add(((ConfigItem)_setting).Help.ToString().ToLower(_culture));
        // If the setting is a list, add its items
        //if (_setting is ItemList)
        //{
        //  foreach (object o in ((ItemList)_setting).Items)
        //  {
        //    if (o != null)
        //    {
        //      string value = o.ToString();
        //      if (value != "" && value != "[system.invalid]")
        //        _keys.Add(value.ToLower(_culture));
        //    }
        //  }
        //}
      }
    }

    #endregion

  }
}
