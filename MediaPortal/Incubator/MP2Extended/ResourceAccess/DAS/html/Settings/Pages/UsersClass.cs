using System;
using System.Collections.Generic;
using MediaPortal.Plugins.MP2Extended.Settings;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.DAS.html.Settings.Pages
{
  partial class UsersTemplate
  {
    private readonly string title = "Users";
    private readonly string headLine = "Users";
    private readonly string subHeadLine = "";
    private readonly List<string> _types = new List<string>();

    public UsersTemplate()
    {
      foreach (var type in Enum.GetValues(typeof(UserTypes)))
      {
        _types.Add(type.ToString());
      }
    }
  }
}
