#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using MediaPortal.Core.Logging;
using MediaPortal.Core.PluginManager;

namespace MediaPortal.Core.Services.PluginManager.Builders
{
  /// <summary>
  /// Builds an item of type "Service". The "Service" item type provides an instance of a system service of a
  /// specified class which will be loaded from the plugin's assemblies.
  /// </summary>
  /// <remarks>
  /// The item registration has to provide the parameters "ServiceClassName", which contains the fully
  /// qualified name of the class in one of the plugin's assemblies, the "RegistrationClassName", which contains the
  /// fully qualified name of the registration type in ServiceScope and the "RegistrationClassAssembly", which
  /// contains the name of the assembly, where the registration type is defined:
  /// <example>
  /// &lt;Service Id="FooService" RegistrationClassName="IFoo" RegistrationClassAssembly="System.Foo" ServiceClassName="Foo"/&gt;
  /// </example>
  /// If the "RegistrationClassAssembly" parameter isn't present, the registration class will be searched in the plugin's
  /// assemblies. If the "RegistrationClassName" isn't present, the service will be registered with its own class.
  /// </remarks>
  public class ServiceBuilder : IPluginItemBuilder
  {
    public class ServiceItem
    {
      protected Type _registrationType;
      protected object _serviceInstance;

      public ServiceItem(Type registrationType, object serviceInstance)
      {
        _registrationType = registrationType;
        _serviceInstance = serviceInstance;
      }

      public Type RegistrationType
      {
        get { return _registrationType; }
      }

      public object ServiceInstance
      {
        get { return _serviceInstance; }
      }
    }

    #region IPluginItemBuilder implementation

    public object BuildItem(PluginItemMetadata itemData, PluginRuntime plugin)
    {
      BuilderHelper.CheckParameter("ServiceClassName", itemData);
      string serviceClassName = itemData.Attributes["ServiceClassName"];
      object serviceInstance = plugin.InstantiatePluginObject(serviceClassName);
      if (serviceInstance == null)
      {
        ServiceScope.Get<ILogger>().Warn("ServiceBuilder: Could not instantiate service class '{0}' in plugin '{1}' (id: '{2}')",
            serviceClassName, itemData.PluginRuntime.Metadata.Name, itemData.PluginRuntime.Metadata.PluginId);
        return null;
      }
      string registrationClassAssembly;
      if (!itemData.Attributes.TryGetValue("RegistrationClassAssembly", out registrationClassAssembly))
        registrationClassAssembly = null;
      string registrationClassName;
      if (!itemData.Attributes.TryGetValue("RegistrationClassName", out registrationClassName))
        registrationClassName = null;
      Type registrationType;
      if (string.IsNullOrEmpty(registrationClassName))
        registrationType = serviceInstance.GetType();
      else
        registrationType = string.IsNullOrEmpty(registrationClassAssembly) ? plugin.GetPluginType(registrationClassName) :
            Type.GetType(registrationClassName + ", " + registrationClassAssembly);
      if (registrationType == null)
      {
        ServiceScope.Get<ILogger>().Warn("ServiceBuilder: Could not instantiate service registration type '{0}' (Assembly: '{1}') in plugin '{2}' (id: '{3}')",
            registrationClassName, registrationClassAssembly, itemData.PluginRuntime.Metadata.Name, itemData.PluginRuntime.Metadata.PluginId);
        return null;
      }
      return new ServiceItem(registrationType, serviceInstance);
    }

    public void RevokeItem(object item, PluginItemMetadata itemData, PluginRuntime plugin)
    {
      if (item == null)
        return;
      ServiceItem si = (ServiceItem) item;
      plugin.RevokePluginObject(si.ServiceInstance.GetType().FullName);
    }

    public bool NeedsPluginActive(PluginItemMetadata itemData, PluginRuntime plugin)
    {
      return true;
    }

    #endregion
  }
}
