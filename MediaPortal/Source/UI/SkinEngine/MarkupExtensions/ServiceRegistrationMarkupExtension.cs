#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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
  public class ServiceRegistrationMarkupExtension: IEvaluableMarkupExtension
  {
    #region Protected fields

    protected static IDictionary<string, Type> TYPE_MAPPING = new Dictionary<string, Type>();
    static ServiceRegistrationMarkupExtension()
    {
      TYPE_MAPPING.Add("ScreenManager", typeof(IScreenManager));
      TYPE_MAPPING.Add("WorkflowManager", typeof(IWorkflowManager));
      TYPE_MAPPING.Add("InputManager", typeof(IInputManager));
      TYPE_MAPPING.Add("Players", typeof(IPlayerManager));
      TYPE_MAPPING.Add("DialogManager", typeof(IDialogManager));
      TYPE_MAPPING.Add("Window", typeof(IScreenControl));
    }

    protected string _interfaceName = null;

    #endregion

    public ServiceRegistrationMarkupExtension() { }

    public ServiceRegistrationMarkupExtension(string interfaceName)
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

    void IEvaluableMarkupExtension.Initialize(IParserContext context)
    {
    }

    bool IEvaluableMarkupExtension.Evaluate(out object value)
    {
      if (_interfaceName == null)
        throw new XamlBindingException("ServiceRegistrationMarkupExtension: Property InterfaceName has to be set");
      if (!TYPE_MAPPING.ContainsKey(_interfaceName))
        throw new XamlBindingException("ServiceRegistrationMarkupExtension: Type '{0}' is not known", _interfaceName);
      Type t = TYPE_MAPPING[_interfaceName];
      try
      {
        Type scType = typeof(ServiceRegistration);
        MethodInfo mi = scType.GetMethod("Get",
          BindingFlags.Public | BindingFlags.Static,
          null, new Type[] { typeof(bool) }, null);
        mi = mi.MakeGenericMethod(t);
        value = mi.Invoke(null, new object[] { false });
        return true;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("ServiceRegistrationMarkupExtension: Error getting service '{0}'", ex, t.Name);
      }
      value = null;
      return false;
    }

    #endregion

    #region Base overrides

    public override string ToString()
    {
      return string.Format("Service InterfaceName={0}", _interfaceName);
    }

    #endregion
  }
}
