using MediaPortal.Common.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeEditor.Groups
{
  public class HomeMenuGroup
  {
    public HomeMenuGroup()
    {
      Actions = new List<HomeMenuAction>();
    }

    public HomeMenuGroup(string displayName, Guid id)
      : this()
    {
      Id = id;
      DisplayName = LocalizationHelper.CreateResourceString(displayName).Evaluate();
    }

    public string DisplayName { get; set; }
    public Guid Id { get; set; }
    public List<HomeMenuAction> Actions { get; set; }
  }
}
