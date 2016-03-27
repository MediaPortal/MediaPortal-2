using MediaPortal.Common.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeEditor.Groups
{
  public class HomeMenuAction
  {
    public HomeMenuAction()
    { }

    public HomeMenuAction(string displayName, Guid actionId)
    {
      ActionId = actionId;
      DisplayName = LocalizationHelper.CreateResourceString(displayName).Evaluate();
    }

    public string DisplayName { get; set; }
    public Guid ActionId { get; set; }
  }
}
