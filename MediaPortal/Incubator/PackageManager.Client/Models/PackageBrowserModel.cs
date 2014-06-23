#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using MediaPortal.Common.General;
using MediaPortal.Common.PluginManager.Packages.ApiEndpoints;
using MediaPortal.Common.PluginManager.Packages.DataContracts;
using MediaPortal.Common.PluginManager.Packages.DataContracts.Packages;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UiComponents.PackageManager.API;

namespace MediaPortal.UiComponents.PackageManager.Models
{
  public class PackageBrowserModel : IWorkflowModel
  {
    #region Consts

    public const string PACKAGE_BROWSER_MODEL_ID_STR = "0D8A053D-CF5A-4A9E-B2D4-A1B5D79CB993";
    public readonly static Guid PACKAGE_BROWSER_MODEL_ID = new Guid(PACKAGE_BROWSER_MODEL_ID_STR);

    public const string WF_BROWSE_PACKAGES_ID_STR = "4C0E204C-37DE-4E74-BC4D-A15069F3DE66";
    public readonly static Guid WF_BROWSE_PACKAGES_ID = new Guid(WF_BROWSE_PACKAGES_ID_STR);

    #endregion

    #region Protected fields

    protected readonly AbstractProperty _isUpdating = new WProperty(typeof(bool), false);
    protected readonly ItemsList _packages = new ItemsList();
    protected PackageItem _selectedPackage;

    #endregion

    #region Public properties - Bindable Data

    /// <summary>
    /// Exposes a list of packages to the skin.
    /// </summary>
    public ItemsList Packages
    {
      get { return _packages; }
    }

    /// <summary>
    /// Exposes the currently selected package to the skin.
    /// </summary>
    public PackageItem SelectedPackage
    {
      get { return _selectedPackage; }
    }

    /// <summary>
    /// Exposes info about a currently running background data refresh to the skin.
    /// </summary>
    public bool IsUpdating
    {
      get { return (bool)_isUpdating.GetValue(); }
      set { _isUpdating.SetValue(value); }
    }
    public AbstractProperty IsUpdatingProperty { get { return _isUpdating; } }

    #endregion

    #region Public methods - Commands

    public void Select(ListItem item)
    {
      if (item == null)
        return;

      var packageItem = item as PackageItem;
      if (packageItem != null)
      {
        _selectedPackage = packageItem;

        //ServiceRegistration.Get<IWorkflowManager>().NavigatePush(WORKFLOWSTATEID_NEWSITEMS, new NavigationContextConfig
        //{
        //  NavigationContextDisplayLabel = packageItem.PackageInfo.Name
        //});
      }
    }

    #endregion

    #region IWorkflowModel implementation

    public Guid ModelId
    {
      get { return PACKAGE_BROWSER_MODEL_ID; }
    }

    public bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
    {
      return true;
    }

    public void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      GetPackageList();
    }

    public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
    }

    public void ChangeModelContext(NavigationContext oldContext, NavigationContext newContext, bool push)
    {
      if (!push)
      {
        //if (oldContext.WorkflowState.StateId == WF_BROWSE_PACKAGES_ID)
        //{
        //  _selectedPackage = null;
        //}
      }
    }

    public void Deactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      // Nothing to do here
    }

    public void Reactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      // Nothing to do here
    }

    public void UpdateMenuActions(NavigationContext context, IDictionary<Guid, WorkflowAction> actions)
    {
      // Nothing to do here
    }

    public ScreenUpdateMode UpdateScreen(NavigationContext context, ref string screen)
    {
      return ScreenUpdateMode.AutoWorkflowManager;
    }

    #endregion

    #region Private members

    private void GetPackageList()
    {
      try
      {
        IsUpdating = true;
        using (RequestExecutionHelper proxy = new RequestExecutionHelper())
        {
          // TODO: GUI filters
          var model = new PackageListQuery
          {
            //PackageType = options.PackageType,
            //PartialAuthor = options.AuthorText,
            //PartialPackageName = options.PackageName,
            //SearchDescriptions = options.SearchDescriptions,
            //CategoryTags = options.CategoryTags,
            // CoreComponents = options.All ? null : localSystemCoreComponents
          };
          var response = proxy.ExecuteRequest(PackageServerApi.Packages.List, model);

          //if (!IsSuccess(response, null, HttpStatusCode.OK))
          //  return false;

          Packages.Clear();
          var packages = proxy.GetResponseContent<IList<PackageInfo>>(response);
          foreach (PackageInfo packageInfo in packages)
          {
            PackageItem packageItem = new PackageItem(packageInfo);
            Packages.Add(packageItem);
          }
          Packages.FireChange();
        }
      }
      catch (Exception)
      {
        throw;
      }
      finally
      {
        IsUpdating = false;
      }
    }

    #endregion
  }
}
