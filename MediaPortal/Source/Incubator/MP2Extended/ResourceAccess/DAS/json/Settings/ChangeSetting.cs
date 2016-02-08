using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using HttpServer;
using HttpServer.Sessions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Authentication;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.MP2Extended.Settings;
using MediaPortal.Plugins.MP2Extended.Utils;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.DAS.json.Settings
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, ReturnType = typeof(WebBoolResult), Summary = "Allowes to change a MP2Ext setting.")]
  [ApiFunctionParam(Name = "name", Type = typeof(string), Nullable = false)]
  [ApiFunctionParam(Name = "value", Type = typeof(string), Nullable = false)]
  internal class ChangeSetting
  {
    public WebBoolResult Process(string name, string value)
    {
      // Security
      // TODO: Add Security
      /*if (!CheckRights.AccessAllowed(session, UserTypes.Admin))
        return new WebBoolResult { Result = false };*/

      if (string.IsNullOrEmpty(name))
        throw new BadRequestException("ChangeSetting: name is null or empty");
      if (value == null)
        throw new BadRequestException("ChangeSetting: value is null");

      
      var properties = MP2Extended.Settings.GetType().GetProperties().ToList();
      int index = properties.FindIndex(x => x.Name == name);
      if (index == -1)
      {
        Logger.Warn("ChangeSetting: A setting with the name '{0}' wasn't found!", name);
        return new WebBoolResult { Result = false };
      }

      var property = properties[index];
      object convertedValue;


      bool result = value.TryParse(property.PropertyType, out convertedValue);

      if (result)
        property.SetValue(MP2Extended.Settings, convertedValue);

      return new WebBoolResult { Result = result };
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}