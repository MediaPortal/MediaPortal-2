using MediaPortal.Common;
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.Services.PluginManager.Builders;
using MediaPortal.Common.UserManagement;
using MediaPortal.Common.UserProfileDataManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.UiComponents.Media.Extensions
{
  public class BaseActionBuilder<T> : IPluginItemBuilder
  {
    Func<Type, string, string, string, string, T> _constructor;

    protected BaseActionBuilder(Func<Type, string, string, string, string, T> constructor)
    {
      _constructor = constructor;
    }

    #region IPluginItemBuilder Member

    virtual public object BuildItem(PluginItemMetadata itemData, PluginRuntime plugin)
    {
      BuilderHelper.CheckParameter("ClassName", itemData);
      BuilderHelper.CheckParameter("Caption", itemData);
      BuilderHelper.CheckParameter("Sort", itemData);
      string restrictionGroup; // optional
      if (itemData.Attributes.TryGetValue("RestrictionGroup", out restrictionGroup))
      {
        ServiceRegistration.Get<IUserManagement>().RegisterRestrictionGroup(restrictionGroup);
      }
      return _constructor(plugin.GetPluginType(itemData.Attributes["ClassName"]), itemData.Attributes["Caption"], itemData.Attributes["Sort"], restrictionGroup, itemData.Id);
    }

    virtual public void RevokeItem(object item, PluginItemMetadata itemData, PluginRuntime plugin)
    {
      // Noting to do
    }

    virtual public bool NeedsPluginActive(PluginItemMetadata itemData, PluginRuntime plugin)
    {
      return true;
    }

    #endregion
  }
}
