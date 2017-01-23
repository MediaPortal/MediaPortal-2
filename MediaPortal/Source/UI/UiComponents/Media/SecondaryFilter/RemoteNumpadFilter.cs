#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using MediaPortal.Common;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Logging;
using MediaPortal.UiComponents.Media.Models.Navigation;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.Utilities;

namespace MediaPortal.UiComponents.Media.SecondaryFilter
{
  public class RemoteNumpadFilter : IItemsFilter
  {
    private enum FilterAction
    {
      Contains,
      StartsWith
    }

    private string _listFilterString; // List Filter String
    private string _text = string.Empty;
    private FilterAction _listFilterAction; // List Filter Action
    private static Dictionary<string, string> _keyMap;

    public RemoteNumpadFilter()
    {
      _listFilterString = string.Empty;
      _listFilterAction = FilterAction.Contains;
      CreateKeyMap();
    }

    protected void CreateKeyMap()
    {
      var loc = ServiceRegistration.Get<ILocalization>();
      _keyMap = new Dictionary<string, string>
      {
        { "0", loc.ToString("[Filter.RemoteNumericAlphabet0]") }, 
        { "1", loc.ToString("[Filter.RemoteNumericAlphabet1]") }, 
        { "2", loc.ToString("[Filter.RemoteNumericAlphabet2]") }, 
        { "3", loc.ToString("[Filter.RemoteNumericAlphabet3]") }, 
        { "4", loc.ToString("[Filter.RemoteNumericAlphabet4]") }, 
        { "5", loc.ToString("[Filter.RemoteNumericAlphabet5]") }, 
        { "6", loc.ToString("[Filter.RemoteNumericAlphabet6]") },
        { "7", loc.ToString("[Filter.RemoteNumericAlphabet7]") },
        { "8", loc.ToString("[Filter.RemoteNumericAlphabet8]") }, 
        { "9", loc.ToString("[Filter.RemoteNumericAlphabet9]") }
      };
    }

    public void Filter(ItemsList filterList, ItemsList originalList, string search)
    {
      // Handle external filter
      if (string.IsNullOrEmpty(search))
        return;

      // Handle given search as single key
      KeyPress(search[0]);

      // List will not be replaced, as the instance is already bound by screen, we only filter items.
      filterList.Clear();
      foreach (NavigationItem item in originalList.OfType<NavigationItem>())
      {
        var simpleTitle = item.SimpleTitle;

        // Filter
        if (_listFilterAction == FilterAction.StartsWith)
        {
          if (simpleTitle.ToLower().StartsWith(_listFilterString))
            filterList.Add(item);
        }
        else
        {
          if (NumPadEncode(simpleTitle).Contains(_listFilterString))
            filterList.Add(item);
        }
      }
      filterList.FireChange();
    }

    public bool IsFiltered
    {
      get { return !string.IsNullOrEmpty(_listFilterString); }
    }

    public string Text { get { return _text; } }

    protected bool KeyPress(Char key)
    {
      if ((key >= '0' && key <= '9') || key == '*' || key == '(' || key == '#' || key == '§')
      {
        // reset the list filter string
        if (key == '*')
        {
          _listFilterString = string.Empty;
          _listFilterAction = FilterAction.Contains;
        }

        // activate "StartsWith" function
        else if ((_listFilterString == string.Empty) && (key == '0'))
        {
          _listFilterAction = FilterAction.StartsWith;
        }

        // Use StartsWith Filter
        else if (_listFilterAction == FilterAction.StartsWith)
        {
          _listFilterString = NumPadNext(_listFilterString, key.ToString());
          _text = ServiceRegistration.Get<ILocalization>().ToString("[Filter.StartsWith]", _listFilterString.ToUpper());
        }
        // Add the numeric code to the list filter string
        else
        {
          // Default
          _listFilterAction = FilterAction.Contains;
          _listFilterString += key.ToString();
          _text = ServiceRegistration.Get<ILocalization>().ToString("[Filter.Filtered]", _listFilterString.ToUpper());
        }

        ServiceRegistration.Get<ILogger>().Debug("Active Filter: {0}", _listFilterString);
        return true;
      }
      return false;
    }

    protected static string NumPadNext(string current, string requested)
    {
      string newValue;

      if (_keyMap.ContainsKey(requested))
        newValue = GetNextFromRange(_keyMap[requested] + requested, current);
      else
        newValue = requested;

      return newValue;
    }

    protected static string GetNextFromRange(string range, string current)
    {
      if (current == string.Empty)
        return range[0].ToString();

      int index = range.IndexOf(current) + 1;
      if (index > 0 && range.Length > index)
        return range[index].ToString();
      return range[0].ToString();
    }

    protected static string NumPadEncode(string input)
    {
      var trim = input.Trim();
      if (string.IsNullOrEmpty(trim))
        return trim;
      string rtn = StringUtils.RemoveDiacritics(trim);
      foreach (string key in _keyMap.Keys)
      {
        if (_keyMap[key].Length > 0)
          rtn = Regex.Replace(rtn, @"[" + Regex.Escape(_keyMap[key]) + @"]", key, RegexOptions.IgnoreCase);
      }

      return Regex.Replace(rtn, @"\s", "0", RegexOptions.IgnoreCase);
    }
  }
}
