using Emulators.Models;
using MediaPortal.Common.Localization;
using MediaPortal.UI.Presentation.Workflow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.Actions
{
  public class RemoveEmulatorAction : IWorkflowContributor
  {
    public static readonly Guid MODEL_ID = new Guid("495F94B5-9E13-4548-BAAA-6DB424B58267");

    public IResourceString DisplayTitle
    {
      get { return LocalizationHelper.CreateResourceString("[Emulators.Config.RemoveEmulatorConfiguration.Title]"); }
    }

    public event ContributorStateChangeDelegate StateChanged;

    public void Execute()
    {
      EmulatorConfigurationModel.Instance().RemoveEmulatorConfigurations();
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
