using System;
using System.Collections.Generic;
using System.Linq;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Utils;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.DAS.html.Api.Pages
{
  partial class ServiceHandlerFunctionsTemplate
  {
    private readonly string title = "Api overview - Service Handler Functions";
    private readonly string headLine = "API";
    private readonly string subHeadLine;
    private readonly Dictionary<string, List<FunctionDescription>> _serviceHandlerFunctions = new Dictionary<string, List<FunctionDescription>>();

    public ServiceHandlerFunctionsTemplate(string serviceHandler)
    {
      IRequestModuleHandler module;
      if (MainRequestHandler.REQUEST_MODULE_HANDLERS.TryGetValue(serviceHandler, out module))
      {
        Attribute[] attrs = Attribute.GetCustomAttributes(module.GetType());
        foreach (Attribute attr in attrs)
        {
          var description = attr as ApiHandlerDescription;
          if (description != null)
          {
            ApiHandlerDescription apiHandlerDescription = description;

            subHeadLine = apiHandlerDescription.FriendlyName;
          }
        }

        var handlerFunctions = module.GetRequestMicroModuleHandlers();
        foreach (var handlerFunction in handlerFunctions)
        {
          var type = handlerFunction.Value.GetType();

          FunctionDescription functionDescription = new FunctionDescription
          {
            Name = handlerFunction.Key,
            FriendlyName = handlerFunction.Key,
            Category = type.Namespace.Split('.').Last(),
          };
          Attribute[] attrsFunc = Attribute.GetCustomAttributes(type);
          foreach (Attribute attr in attrsFunc)
          {
            var description = attr as ApiFunctionDescription;
            var param = attr as ApiFunctionParam;

            if (description != null)
            {
              ApiFunctionDescription apiFunctionDescription = description;

              functionDescription.Summary = apiFunctionDescription.Summary?? string.Empty;
              functionDescription.Type = apiFunctionDescription.Type.ToString().ToLower();
              functionDescription.ReturnType = (apiFunctionDescription.ReturnType == null) ? string.Empty : apiFunctionDescription.ReturnType.ToString();
              functionDescription.Url = GetBaseStreamUrl.GetBaseStreamURL() + MainRequestHandler.RESOURCE_ACCESS_PATH + "/" + serviceHandler + "/" + apiFunctionDescription.Type.ToString().ToLower() + "/" + handlerFunction.Key;
            }
            
            if (param != null)
            {
              functionDescription.Parameters.Add(param);
            }
          }
          if (!_serviceHandlerFunctions.ContainsKey(functionDescription.Category))
            _serviceHandlerFunctions.Add(functionDescription.Category, new List<FunctionDescription>());
          _serviceHandlerFunctions[functionDescription.Category].Add(functionDescription);
        }
      }
    }

    internal class FunctionDescription
    {
      internal string Name { set; get; }
      internal string FriendlyName { set; get; }
      internal string Category { set; get; }
      internal string ReturnType { set; get; }
      internal string Type { set; get; }
      internal string Url { set; get; }
      internal string Summary { set; get; }
      internal List<ApiFunctionParam> Parameters { set; get; }

      internal FunctionDescription()
      {
        // prevent "Value cannot be null. Parameter name: objectToConvert" exception
        Name = FriendlyName = Category = ReturnType = Type = Url = Summary = string.Empty;
        Parameters = new List<ApiFunctionParam>();
      }
    }
  }
}
