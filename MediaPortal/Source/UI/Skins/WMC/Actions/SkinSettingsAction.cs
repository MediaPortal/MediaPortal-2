using MediaPortal.UI.Presentation.Workflow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaPortal.Common.Localization;
using MediaPortal.Common;
using HomeEditor.Actions;

namespace MediaPortal.UiComponents.WMCSkin.Actions
{
  public class SkinSettingsAction : AbstractConfigurationAction
  {
    public const string CONFIG_LOCATION = "/Appearance/Skin/SkinSettings";

    public override IResourceString DisplayTitle
    {
      get { return LocalizationHelper.CreateResourceString("[WMC.Configuration.SkinSettings]"); }
    }

    protected override string ConfigLocation
    {
      get { return CONFIG_LOCATION; }
    }
  }
}
