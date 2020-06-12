using MediaPortal.UI.Presentation.Workflow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaPortal.Common.Localization;
using Emulators.Models;

namespace Emulators.Actions
{
  public class AddEmulatorAction : IWorkflowContributor
  {
    public static readonly Guid MODEL_ID = new Guid("3ECB5E18-5B24-411D-9F06-533249BA6F00");

    public IResourceString DisplayTitle
    {
      get { return LocalizationHelper.CreateResourceString("[Emulators.Config.AddEmulatorConfiguration.Title]"); }
    }

    public event ContributorStateChangeDelegate StateChanged;

    public void Execute()
    {
      EmulatorConfigurationModel.Instance().AddNewEmulatorConfiguration();
    }

    public void Initialize()
    {
      
    }

    public bool IsActionEnabled(NavigationContext context)
    {
      return true;
    }

    public bool IsActionVisible(NavigationContext context)
    {
      return context.WorkflowState.StateId == EmulatorConfigurationModel.STATE_OVERVIEW;
    }

    public void Uninitialize()
    {
      
    }
  }
}
