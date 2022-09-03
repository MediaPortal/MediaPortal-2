#region Copyright (C) 2007-2021 Team MediaPortal

/*
    Copyright (C) 2007-2021 Team MediaPortal
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

using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.Utilities;
using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace MediaPortal.UI.SkinEngine.MpfElements.Converters
{
  public abstract class AbstractConfigurationConverter : AbstractSingleDirectionConverter
  {
    public const string KEY_ITEM_ACTION = "MenuModel: Item-Action";
    private static Regex _reLocation = new Regex(@"Config.*->\/(.*)");

    public override bool Convert(object val, Type targetType, object parameter, CultureInfo culture, out object result)
    {
      result = null;
      if (!GetConfigLocation(val, out var location))
        return false;

      return ConvertConfigLocation(location, parameter, out result);
    }

    protected abstract bool ConvertConfigLocation(string configLocation, object parameter, out object result);
    protected static void ProcessFormatString(object parameter, ref object result)
    {
      var formatString = parameter as string;
      if (!string.IsNullOrEmpty(formatString))
        result = string.Format(formatString, result);
    }

    internal static bool GetConfigLocation(object val, out string location)
    {
      location = null;

      // Assume that if val is a string then it is already a config location
      if (val is string l)
      {
        location = StringUtils.RemovePrefixIfPresent(l, "/");
        return true;
      }

      // Else see if val is a list item containing a WF action that contains the location in it's name
      ListItem li = val as ListItem;
      if (li == null)
        return false;

      if (!li.AdditionalProperties.TryGetValue(KEY_ITEM_ACTION, out object oAction) || !(oAction is WorkflowAction action))
        return false;

      Match match = _reLocation.Match(action.Name);
      if (!match.Success)
        return false;

      location = match.Groups[1].Value;
      return true;
    }
  }

  public class ConfigurationRootConverter : AbstractConfigurationConverter
  {
    private static Regex _reRoot = new Regex(@"([^\/]*)");
    protected override bool ConvertConfigLocation(string configLocation, object parameter, out object result)
    {
      Match match = _reRoot.Match(configLocation);
      if (match.Success)
      {
        result = match.Groups[1].Value;
        ProcessFormatString(parameter, ref result);
        return true;
      }
      result = null;
      return false;
    }
  }

  public class ConfigurationPathConverter : AbstractConfigurationConverter
  {
    protected override bool ConvertConfigLocation(string configLocation, object parameter, out object result)
    {
      bool noReplace = parameter is bool b && b;
      if (!noReplace)
        configLocation = configLocation.Replace('/', '_');

      result = configLocation;
      ProcessFormatString(parameter, ref result);
      return true;
    }
  }

  public class ConfigurationLevelConverter : AbstractSingleDirectionConverter
  {
    public override bool Convert(object val, Type targetType, object parameter, CultureInfo culture, out object result)
    {
      result = null;
      ItemsList list = val as ItemsList;
      if (list == null)
        return false;

      ConfigurationPathConverter cpc = new ConfigurationPathConverter();
      foreach (ListItem listItem in list)
      {
        if (cpc.Convert(listItem, typeof(string), true /* no replace */, CultureInfo.CurrentCulture, out object itemResult))
        {
          string path = itemResult as string;
          if (path != null)
          {
            var pathParts = path.Split('/');
            result = pathParts.Length - 1;
            return true;
          }
        }
      }

      return false;
    }
  }
}
