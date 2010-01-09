#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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
using System.Reflection;
using MediaPortal.UI.Control.InputManager;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UI.SkinEngine.Xaml.Exceptions;
using MediaPortal.UI.SkinEngine.Xaml.Interfaces;

namespace MediaPortal.UI.SkinEngine.MarkupExtensions
{
  public class ServiceScopeMarkupExtension: IEvaluableMarkupExtension
  {
    #region Protected fields

    protected static IDictionary<string, Type> TYPE_MAPPING = new Dictionary<string, Type>();
    static ServiceScopeMarkupExtension()
    {
      TYPE_MAPPING.Add("ScreenManager", typeof(IScreenManager));
      TYPE_MAPPING.Add("WorkflowManager", typeof(IWorkflowManager));
      TYPE_MAPPING.Add("InputManager", typeof(IInputManager));
      TYPE_MAPPING.Add("Players", typeof(IPlayerManager));
      TYPE_MAPPING.Add("DialogManager", typeof(IDialogManager));
    }

    protected string _interfaceName = null;

    #endregion

    public ServiceScopeMarkupExtension() { }

    public ServiceScopeMarkupExtension(string interfaceName)
    {
      _interfaceName = interfaceName;
    }

    #region Properties

    public string InterfaceName
    {
      get { return _interfaceName; }
      set { _interfaceName = value; }
    }

    #endregion

    #region IEvaluableMarkupExtension implementation

    object IEvaluableMarkupExtension.Evaluate(IParserContext context)
    {
      if (_interfaceName == null)
        throw new XamlBindingException("ServiceScopeMarkupExtension: Property InterfaceName has to be set");
      if (!TYPE_MAPPING.ContainsKey(_interfaceName))
        throw new XamlBindingException("ServiceScopeMarkupExtension: Type '{0}' is not known", _interfaceName);
      Type t = TYPE_MAPPING[_interfaceName];
      try
      {
        Type scType = typeof(ServiceScope);
        MethodInfo mi = scType.GetMethod("Get",
          BindingFlags.Public | BindingFlags.Static,
          null, new Type[] { typeof(bool) }, null);
        mi = mi.MakeGenericMethod(t);
        return mi.Invoke(null, new object[] { false });
      }
      catch (Exception ex)
      {
        ServiceScope.Get<ILogger>().Error("ServiceScopeMarkupExtension: Error getting service '{0}'", ex, t.Name);
      }
      return null;
    }

    #endregion
  }
}
