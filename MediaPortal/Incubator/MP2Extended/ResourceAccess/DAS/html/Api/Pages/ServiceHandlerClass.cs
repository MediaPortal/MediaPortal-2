using System;
using System.Collections.Generic;
using MediaPortal.Plugins.MP2Extended.Attributes;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.DAS.html.Api.Pages
{
  partial class ServiceHandlerTemplate
  {
    private readonly Dictionary<string, ApiHandlerDescription> _serviceHandler = new Dictionary<string, ApiHandlerDescription>();
    private readonly string title = "Api overview - Service Handlers";
    private readonly string headLine = "API";
    private readonly string subHeadLine = "Service Handlers.";

    public ServiceHandlerTemplate()
    {
      foreach (var handler in MainRequestHandler.REQUEST_MODULE_HANDLERS)
      {
        Attribute[] attrs = Attribute.GetCustomAttributes(handler.Value.GetType());
        foreach (Attribute attr in attrs)
        {
          var description = attr as ApiHandlerDescription;
          if (description != null)
          {
            ApiHandlerDescription apiHandlerDescription = description;
            _serviceHandler.Add(handler.Key, apiHandlerDescription);
          }
        }
      }
    }
  }
}
