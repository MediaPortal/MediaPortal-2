using HomeEditor.Models;
using MediaPortal.Common.Localization;
using MediaPortal.UI.Presentation.Workflow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeEditor.Actions
{
  public class AddActionAction : IWorkflowContributor
  {
    public IResourceString DisplayTitle
    {
      get { return null; }
    }

    public event ContributorStateChangeDelegate StateChanged;

    public void Execute()
    {
      HomeEditorModel.Instance().AddAction();
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
      return true;
    }

    public void Uninitialize()
    {
    }
  }
}