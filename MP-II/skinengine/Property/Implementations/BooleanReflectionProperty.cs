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

using System;
using System.Reflection;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.Core.Properties;
using SkinEngine.Commands;
using SkinEngine.Controls;
using SkinEngine.Scripts;

namespace SkinEngine
{
  public class BooleanReflectionProperty : IBooleanProperty
  {
    #region IBooleanProperty Members

    private readonly string _property;

    public BooleanReflectionProperty(string property)
    {
      _property = property;
    }

    public bool Evaluate(IControl control, IControl container)
    {
      try
      {
        string[] parts = _property.Split(new char[] {'.'});
        object model = ObjectFactory.GetObject(((Control) control).Window, parts[0]);
        if (model != null)
        {
          if (parts[1].StartsWith("#script"))
          {
            string scriptName = parts[1].Substring("#script:".Length);
            if (ScriptManager.Instance.Contains(scriptName))
            {
              IBooleanProperty property = (IBooleanProperty) ScriptManager.Instance.GetScript(scriptName);
              return property.Evaluate((IControl) model, (IControl) model);
            }
          }
          int partNr = 1;
          object obj = null;
          while (partNr < parts.Length)
          {
            Type classType = model.GetType();

            MethodInfo info =
              classType.GetProperty(parts[partNr],
                                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static |
                                    BindingFlags.InvokeMethod | BindingFlags.ExactBinding).GetGetMethod();
            obj = info.Invoke(model, null);
            //obj = classType.InvokeMember(parts[partNr], BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.GetProperty, System.Type.DefaultBinder, model, null);
            partNr++;
            if (partNr < parts.Length)
            {
              model = obj;
              if (model == null)
              {
                return false;
              }
            }
          }
          if (obj == null)
            return false;
          return (bool) obj;
        }
      }
      catch (Exception ex)
      {
        ServiceScope.Get<ILogger>().Error("error evaluating boolean property {0}", _property);
        ServiceScope.Get<ILogger>().Error(ex);
      }
      return false;
    }

    #endregion
  }
}