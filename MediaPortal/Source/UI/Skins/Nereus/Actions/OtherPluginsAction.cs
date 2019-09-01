using MediaPortal.Common.Localization;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UiComponents.Nereus.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.UiComponents.Nereus.Actions
{
  public class OtherPluginsAction : WorkflowAction
  {
    public static readonly Guid ACTION_ID = new Guid("1DC6BE8F-2321-4274-9789-0FF60E1BB19C");

    public OtherPluginsAction()
      : base(ACTION_ID, "Other Plugins", new[] { HomeMenuModel.HOME_STATE_ID },
         LocalizationHelper.CreateResourceString("[Nereus.Home.OtherPlugins]"),
         LocalizationHelper.CreateResourceString("[Nereus.Home.OtherPlugins.Help]"))
    { }

    public override void Execute()
    {
    }

    public override bool IsEnabled(NavigationContext context)
    {
      return true;
    }

    public override bool IsVisible(NavigationContext context)
    {
      return true;
    }
  }
}
