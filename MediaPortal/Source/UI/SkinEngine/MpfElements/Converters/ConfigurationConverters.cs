#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Workflow;

namespace MediaPortal.UI.SkinEngine.MpfElements.Converters
{
  public abstract class AbstractConfigurationConverter : AbstractSingleDirectionConverter
  {
    public const string KEY_ITEM_ACTION = "MenuModel: Item-Action";
    public override bool Convert(object val, Type targetType, object parameter, CultureInfo culture, out object result)
    {
      result = null;
      if (!GetAction(val, out var action))
        return false;

      return ConvertAction(action, parameter, out result);
    }

    protected abstract bool ConvertAction(WorkflowAction action, object parameter, out object result);
    protected static void ProcessFormatString(object parameter, ref object result)
    {
      var formatString = parameter as string;
      if (!string.IsNullOrEmpty(formatString))
        result = string.Format(formatString, result);
    }

    internal static bool GetAction(object val, out WorkflowAction action)
    {
      ListItem li = val as ListItem;
      action = null;
      if (li == null)
        return false;

      if (!li.AdditionalProperties.TryGetValue(KEY_ITEM_ACTION, out object oAction))
        return false;

      action = oAction as WorkflowAction;
      return action != null;
    }
  }

  public class ConfigurationRootConverter : AbstractConfigurationConverter
  {
    private static Regex _reRoot = new Regex(@"Config.*->\/([^\/]*)");
    protected override bool ConvertAction(WorkflowAction action, object parameter, out object result)
    {
      var name = action.Name;
      Match match = _reRoot.Match(name);
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
    private static Regex _rePath = new Regex(@"Config.*->\/(.*)");
    protected override bool ConvertAction(WorkflowAction action, object parameter, out object result)
    {
      bool noReplace = parameter is bool b && b;
      var name = action.Name;
      Match match = _rePath.Match(name);
      if (match.Success)
      {
        string value = match.Groups[1].Value;
        if (!noReplace)
          value = value.Replace('/', '_');

        result = value;
        ProcessFormatString(parameter, ref result);
        return true;
      }
      result = null;
      return false;
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
