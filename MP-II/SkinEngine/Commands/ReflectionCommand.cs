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
using MediaPortal.Presentation.Commands;
using MediaPortal.Core.Logging;
using MediaPortal.SkinEngine.Commands.Expression;

namespace MediaPortal.SkinEngine.Commands
{
  public class ReflectionCommand : ICommand
  {
    #region variables

    private readonly string _command;

    #endregion

    public ReflectionCommand(string command)
    {
      _command = command;
    }

    #region ICommand Members

    void eval_AdditionalVariableEventHandler(object sender, AdditionalVariableEventArgs e)
    {
      e.ReturnValue = GetParameter(e.Name);
    }
    void eval_AdditionalFunctionEventHandler(object sender, AdditionalFunctionEventArgs e)
    {
      e.ReturnValue = 0;
      object[] parameters = e.GetParameters();
      if (parameters != null && parameters.Length == 0)
        parameters = null;

      string[] parts = e.Name.Split(new char[] { '.' });
      if (parts.Length < 2)
      {
        ServiceScope.Get<ILogger>().Error("invalid command parameter format for {0}", e.Name);
        return;
      }
      object control = ObjectFactory.GetObject(parts[0]);
      if (control == null)
      {
        ServiceScope.Get<ILogger>().Error("cannot get object for {0}", e.Name);
        return;
      }

      Type classType;
      int partNr = 1;
      while (partNr < parts.Length - 1)
      {
        classType = control.GetType();
        MethodInfo info =
          classType.GetProperty(parts[partNr],
                                BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static |
                                BindingFlags.InvokeMethod | BindingFlags.ExactBinding).GetGetMethod();
        if (info == null)
        {
          ServiceScope.Get<ILogger>().Error("cannot get object for {0}", e.Name);
          return;
        }
        object obj = info.Invoke(control, null);
        partNr++;
        if (partNr < parts.Length)
        {
          control = obj;
          if (control == null)
          {
            break;
          }
        }
      }
      string memberName = parts[parts.Length - 1];

      if (control != null)
      {
        classType = control.GetType();
        if (parameters == null)
        {
          MethodInfo info =
            classType.GetMethod(memberName,
                                BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static |
                                BindingFlags.InvokeMethod | BindingFlags.ExactBinding);
          if (info == null)
          {
            ServiceScope.Get<ILogger>().Error("cannot get object for {0}", e.Name);
            return;
          }
          e.ReturnValue = info.Invoke(control, null);
        }
        else
        {
          MethodInfo info =
            classType.GetMethod(memberName,
                                BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static |
                                BindingFlags.InvokeMethod | BindingFlags.ExactBinding); if (info == null)
          {
            ServiceScope.Get<ILogger>().Error("cannot get object for {0}", e.Name);
            return;
          }
          e.ReturnValue = info.Invoke(control, parameters);
        }
      }
    }

    /// <summary>
    /// Executes the command with the parameter.
    /// </summary>
    /// <param name="parameter">The parameter.</param>
    public void Execute(ICommandParameter parameter)
    {
      try
      {
        try
        {
          ExpressionEval eval = new ExpressionEval();
          eval.AdditionalFunctionEventHandler += new AdditionalFunctionEventHandler(eval_AdditionalFunctionEventHandler);
          eval.AdditionalVariableEventHandler += new AdditionalVariableEventHandler(eval_AdditionalVariableEventHandler);
          eval.Expression = _command.Replace('\'', '"');
          
          eval.Evaluate();
          if (eval.Succeeded)
          {
            return;
          }
        }
        catch (Exception ex)
        {
          ServiceScope.Get<ILogger>().Error("Error occured while executing {0}", _command);
          ServiceScope.Get<ILogger>().Error(ex);
        }

        string[] parts = _command.Split(new char[] { '+' });
        if (parameter!=null && parameter.Value!=null && !String.IsNullOrEmpty(parameter.Value.ToString()))
        {
          string[] partsParams = parameter.Value.ToString().Split(new char[] { '+' });
          for (int i = 0; i < parts.Length; ++i)
          {
            string param;
            if (i < partsParams.Length)
            {
              param = partsParams[i];
              if (param.Length > 0)
              {
                ExecuteCommand(parts[i], param);
              }
              else
              {
                ExecuteCommand(parts[i], null);
              }
            }
          }
        }
        else
        {
          for (int i = 0; i < parts.Length; ++i)
          {
            ExecuteCommand(parts[i], null);
          }
        }
      }
      catch (Exception ex)
      {
        ServiceScope.Get<ILogger>().Error("error executing command:{0} parameter:{1}", _command, parameter);
        ServiceScope.Get<ILogger>().Error(ex);
      }
    }

    /// <summary>
    /// Executes the command.
    /// </summary>
    /// <param name="command">The command.</param>
    /// <param name="parameter">The parameter.</param>
    private void ExecuteCommand(string command, string parameter)
    {
      ServiceScope.Get<ILogger>().Info("ExecuteCommand {0}", command);

      string[] parts = command.Split(new char[] { '.' });
      if (parts.Length < 2)
      {
        return;
      }
      object control = ObjectFactory.GetObject(parts[0]);
      if (control == null)
      {
        return;
      }

      Type classType;
      int partNr = 1;
      while (partNr < parts.Length - 1)
      {
        classType = control.GetType();
        MethodInfo info =
          classType.GetProperty(parts[partNr],
                                BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static |
                                BindingFlags.InvokeMethod | BindingFlags.ExactBinding).GetGetMethod();
        if (info == null)
        {
          ServiceScope.Get<ILogger>().Error("cannot get object for {0}", command);
          return;
        }
        object obj = info.Invoke(control, null);
        partNr++;
        if (partNr < parts.Length)
        {
          control = obj;
          if (control == null)
          {
            break;
          }
        }
      }
      string memberName = parts[parts.Length - 1];

      if (control != null)
      {
        classType = control.GetType();
        if (parameter == null)
        {
          MethodInfo info =
            classType.GetMethod(memberName,
                                BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static |
                                BindingFlags.InvokeMethod | BindingFlags.ExactBinding);
          if (info == null)
          {
            ServiceScope.Get<ILogger>().Error("cannot get object for {0}", command);
            return;
          }
          info.Invoke(control, null);
        }
        else
        {
          MethodInfo info =
            classType.GetMethod(memberName,
                                BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static |
                                BindingFlags.InvokeMethod | BindingFlags.ExactBinding);
          if (info != null)
          {
            info.Invoke(control, new object[] { GetParameterValue(parameter) });
          }
          else
          {
            info = classType.GetProperty(memberName,
                                 BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static |
                                 BindingFlags.InvokeMethod | BindingFlags.ExactBinding).GetSetMethod();
            if (info == null)
            {
              ServiceScope.Get<ILogger>().Error("cannot get object for {0}", command);
              return;
            }
            info.Invoke(control, new object[] { GetParameterValue(parameter) });
          }
        }
      }
    }

    /// <summary>
    /// Gets the parameter value.
    /// </summary>
    /// <param name="param">The param.</param>
    /// <returns></returns>
    public object GetParameterValue(object param)
    {
      if (param.GetType() == typeof(string))
      {
        string text = (string)param;
        string[] parts = text.Split(new char[] { '.' });
        if (parts.Length < 2)
        {
          return param;
        }
        object obj = ObjectFactory.GetObject(parts[0]);
        if (obj == null)
        {
          return param;
        }
        /*
        if (parts[1].StartsWith("#script"))
        {
          string scriptName = parts[1].Substring("#script:".Length);
          if (ScriptManager.Instance.Contains(scriptName))
          {
            IProperty property = (IProperty)ScriptManager.Instance.GetScript(scriptName);
            return property.Evaluate(obj);
          }
        }
        else*/
        {
          object model = ObjectFactory.GetObject(parts[0]);
          if (model != null)
          {
            int partNr = 1;
            obj = null;
            while (partNr < parts.Length)
            {
              Type classType = model.GetType();
              MethodInfo info =
                classType.GetProperty(parts[partNr],
                                      BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static |
                                      BindingFlags.InvokeMethod | BindingFlags.ExactBinding).GetGetMethod();
              if (info == null)
              {
                ServiceScope.Get<ILogger>().Error("cannot get object for {0}", text);
                return "";
              }
              obj = info.Invoke(model, null);

               partNr++;
              if (partNr < parts.Length)
              {
                model = obj;
                if (model == null)
                {
                  return "";
                }
              }
            }
            if (obj == null)
            {
              return null;
            }
            return obj;
          }
          return param;
        }
      }
      return param;
    }

    public object GetParameter(object param)
    {
      if (param.GetType() == typeof(string))
      {
        string text = (string)param;
        string[] parts = text.Split(new char[] { '.' });
        if (parts.Length < 2)
        {
          return param;
        }
        object obj = ObjectFactory.GetObject(parts[0]);
        if (obj == null)
        {
          return param;
        }
        /*
        if (parts[1].StartsWith("#script"))
        {
          string scriptName = parts[1].Substring("#script:".Length);
          if (ScriptManager.Instance.Contains(scriptName))
          {
            IProperty property = (IProperty)ScriptManager.Instance.GetScript(scriptName);
            return property.Evaluate(obj);
          }
        }
        else*/
        {
          object model = ObjectFactory.GetObject(parts[0]);
          if (model != null)
          {
            int partNr = 1;
            obj = null;
            while (partNr < parts.Length)
            {
              Type classType = model.GetType();
              MethodInfo info =
                classType.GetProperty(parts[partNr],
                                      BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static |
                                      BindingFlags.InvokeMethod | BindingFlags.ExactBinding).GetGetMethod();
              if (info == null)
              {
                ServiceScope.Get<ILogger>().Error("cannot get object for {0}", text);
                return "";
              }
              obj = info.Invoke(model, null);

              partNr++;
              if (partNr < parts.Length)
              {
                model = obj;
                if (model == null)
                {
                  return "";
                }
              }
            }
            if (obj == null)
            {
              return null;
            }
            return obj;
          }
          return param;
        }
      }
      return param;
    }

    #endregion
  }
}
