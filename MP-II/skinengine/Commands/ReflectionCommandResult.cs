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
using System.Reflection;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.Core.Commands;
using MediaPortal.Core.Properties;
using MediaPortal.Core.WindowManager;

namespace SkinEngine.Commands
{
  public class ReflectionCommandResult : ICommandResult
  {
    #region variables

    private string _command;
    private IWindow _window;
    private IControl _control;
    //private EventHandler _handler;
    //private bool _registeredForClose = false;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="ReflectionCommand"/> class.
    /// </summary>
    /// <param name="window">The window.</param>
    /// <param name="command">The command.</param>
    public ReflectionCommandResult(IWindow window, string command)
    {
      _window = window;
      _command = command;
      // _handler = new EventHandler(OnWindowClosed);
    }

    public ReflectionCommandResult(IControl control, IWindow window, string command)
    {
      _window = window;
      _command = command;
      _control = control;
      // _handler = new EventHandler(OnWindowClosed);
    }

    #region ICommand Members

    /// <summary>
    /// Executes the command .
    /// </summary>
    public void Execute()
    {
      string[] parts = _command.Split(new char[] {'='});
      if (parts.Length != 2)
      {
        return;
      }
      Property obj1 = GetProperty(parts[0]);
      Property obj2 = GetProperty(parts[1]);
      if (obj1 == null || obj2 == null)
      {
        return;
      }

      obj1.SetValue(obj2.GetValue());
    }

    private Property GetProperty(string command)
    {
      string[] parts = command.Split(new char[] {'.'});
      if (parts.Length < 2)
      {
        return null;
      }
      object control = ObjectFactory.GetObject(_control, _window, parts[0]);
      if (control == null)
      {
        ServiceScope.Get<ILogger>().Error("cannot get object for {0}", command);
        return null;
      }
      //Window w = control as Window;
      //if (w != null)
      //{
      //  if (_registeredForClose == false)
      //  {
      //    _registeredForClose = true;
      //    w.OnClose += _handler;
      //    return null;
      //  }
      //  else
      //  {
      //    _registeredForClose = false;
      //    w.OnClose -= _handler;
      //  }
      //}

      int partNr = 1;
      object obj = control;
      while (partNr < parts.Length)
      {
        object res = null;
        string propName = parts[partNr] + "Property";
        MemberInfo[] props =
          obj.GetType().FindMembers(MemberTypes.Property,
                                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static |
                                    BindingFlags.InvokeMethod | BindingFlags.ExactBinding, Type.FilterName, propName);
        if (props.Length == 0)
        {
          MethodInfo info =
            obj.GetType().GetProperty(parts[partNr],
                                      BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static |
                                      BindingFlags.InvokeMethod | BindingFlags.ExactBinding).GetGetMethod();
          if (info == null)
          {
            ServiceScope.Get<ILogger>().Error("cannot get object for {0}", command);
            return null;
          }
          res = info.Invoke(obj, null);
        }
        else
        {
          MethodInfo info =
            obj.GetType().GetProperty(parts[partNr] + "Property",
                                      BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static |
                                      BindingFlags.InvokeMethod | BindingFlags.ExactBinding).GetGetMethod();
          if (info == null)
          {
            ServiceScope.Get<ILogger>().Error("cannot get object for {0}", command);
            return null;
          }
          res = info.Invoke(obj, null);
        }
        if (obj == null)
        {
          break;
        }
        partNr++;
        obj = res;
      }
      return obj as Property;
    }

    private void OnWindowClosed(object sender, EventArgs e)
    {
      Execute();
    }

    #endregion
  }
}
