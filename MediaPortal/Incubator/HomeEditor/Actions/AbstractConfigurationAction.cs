using MediaPortal.UI.Presentation.Workflow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaPortal.Common.Localization;
using MediaPortal.Common;

namespace HomeEditor.Actions
{
  public abstract class AbstractConfigurationAction : IWorkflowContributor
  {
    public const string CONFIG_LOCATION_KEY = "ConfigurationModel: CONFIG_LOCATION";
    public static readonly Guid CONFIGURATION_STATE_ID = new Guid("E7422BB8-2779-49ab-BC99-E3F56138061B");

    public abstract IResourceString DisplayTitle
    {
      get;
    }

    protected abstract string ConfigLocation
    {
      get;
    }

    public event ContributorStateChangeDelegate StateChanged;

    public virtual void Execute()
    {
      var wf = ServiceRegistration.Get<IWorkflowManager>();
      wf.NavigatePush(CONFIGURATION_STATE_ID, new NavigationContextConfig()
      {
        NavigationContextDisplayLabel = DisplayTitle.Evaluate(),
        AdditionalContextVariables = new Dictionary<string, object> { { CONFIG_LOCATION_KEY, ConfigLocation } }
      });
    }

    public virtual void Initialize()
    {

    }

    public virtual bool IsActionEnabled(NavigationContext context)
    {
      return true;
    }

    public virtual bool IsActionVisible(NavigationContext context)
    {
      return true;
    }

    public virtual void Uninitialize()
    {

    }
  }
}