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

using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using SkinEngine.Controls;
using MediaPortal.Core;
using MediaPortal.Core.Collections;
using MediaPortal.Core.Properties;
using MediaPortal.Core.WindowManager;
using MediaPortal.Core.Logging;

namespace SkinEngine
{
  public class ControlReflectionProperty : IControlProperty
  {
    #region ILabelProperty Members
    string _property;
    public ControlReflectionProperty(string property)
    {
      _property = property;
    }

    public IControl Evaluate(IControl control, IControl container)
    {
      try
      {
        string[] parts = _property.Split(new char[] { '.' });
        IWindowManager manager = (IWindowManager)ServiceScope.Get<IWindowManager>();
        IWindow window = manager.CurrentWindow;
        if (container != null)
          window = ((Control)container).Window;
        object model = Commands.ObjectFactory.GetObject(control,window, parts[0]);
        if (model != null)
        {
          int partNr = 1;
          object obj = null;
          while (partNr < parts.Length)
          {
            Type classType = model.GetType();
            MethodInfo info = classType.GetProperty(parts[partNr], BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.GetProperty | BindingFlags.ExactBinding).GetGetMethod();
            obj=info.Invoke(model, null);
            //obj = classType.InvokeMember(parts[partNr], BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.GetProperty, System.Type.DefaultBinder, model, null);
            partNr++;
            if (partNr < parts.Length)
            {
              model = obj;
              if (model == null)
                break;
            }
          }
          return (IControl)obj;
        }
      }
      catch (Exception ex)
      {
        ServiceScope.Get<ILogger>().Error("error evaluating label property {0}", _property);
        ServiceScope.Get<ILogger>().Error(ex);
      }
      return null;
    }


    public IControl Evaluate(IControl control, IControl container, string name)
    {
      return Evaluate(control, container);
    }

    #endregion
  }
}
