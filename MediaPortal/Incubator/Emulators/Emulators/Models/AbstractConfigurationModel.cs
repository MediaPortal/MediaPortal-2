using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Workflow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.Models
{
  public abstract class AbstractConfigurationModel : IWorkflowModel
  {
    protected AbstractProperty _isConfigurationSelected = new WProperty(typeof(bool), false);
    protected ItemsList _configurationItems = new ItemsList();
    protected ItemsList _configurationItemsToRemove = new ItemsList();

    public ItemsList Configurations
    {
      get { return _configurationItems; }
    }

    public ItemsList ConfigurationsToRemove
    {
      get { return _configurationItemsToRemove; }
    }

    public AbstractProperty IsConfigurationSelectedProperty
    {
      get { return _isConfigurationSelected; }
    }

    public bool IsConfigurationSelected
    {
      get { return (bool)_isConfigurationSelected.GetValue(); }
      set { _isConfigurationSelected.SetValue(value); }
    }

    protected abstract List<ListItem> GetItems(bool removing);
    protected abstract void OnItemsRemoved(IEnumerable<ListItem> items);

    protected virtual void UpdateState(NavigationContext newContext, bool push)
    {
    }

    public virtual void NavigateBackToOverview()
    {
    }

    public void FinishRemoveConfigurations()
    {
      var itemsToRemove = ConfigurationsToRemove.Where(i => i.Selected);
      OnItemsRemoved(itemsToRemove);
      UpdateConfigurations();
      NavigateBackToOverview();
    }

    protected void UpdateConfigurations()
    {
      UpdateItems(false);
    }

    protected void UpdateConfigurationsToRemove()
    {
      IsConfigurationSelected = false;
      UpdateItems(true);
    }

    protected void UpdateItems(bool removing)
    {
      ItemsList itemsList = removing ? _configurationItemsToRemove : _configurationItems;
      itemsList.Clear();
      List<ListItem> items = GetItems(removing);
      if (items == null || items.Count == 0)
      {
        itemsList.FireChange();
        return;
      }
      foreach (ListItem item in items)
      {
        if (removing)
          item.SelectedProperty.Attach(OnConfigurationSelected);
        itemsList.Add(item);
      }
      itemsList.FireChange();
    }

    protected void OnConfigurationSelected(AbstractProperty property, object oldValue)
    {
      UpdateIsConfigurationSelected();
    }

    protected void UpdateIsConfigurationSelected()
    {
      IsConfigurationSelected = _configurationItemsToRemove.Any(l => l.Selected);
    }

    protected static void NavigatePush(Guid stateId)
    {
      ServiceRegistration.Get<IWorkflowManager>().NavigatePush(stateId);
    }

    #region IWorkflow
    public abstract Guid ModelId
    {
      get;
    }

    public virtual bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
    {
      return true;
    }

    public virtual void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      UpdateConfigurations();
      UpdateState(newContext, true);
    }

    public virtual void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
    {

    }

    public virtual void ChangeModelContext(NavigationContext oldContext, NavigationContext newContext, bool push)
    {
      UpdateState(newContext, push);
    }

    public virtual void Deactivate(NavigationContext oldContext, NavigationContext newContext)
    {

    }

    public virtual void Reactivate(NavigationContext oldContext, NavigationContext newContext)
    {

    }

    public virtual void UpdateMenuActions(NavigationContext context, IDictionary<Guid, WorkflowAction> actions)
    {

    }

    public virtual ScreenUpdateMode UpdateScreen(NavigationContext context, ref string screen)
    {
      return ScreenUpdateMode.AutoWorkflowManager;
    }
    #endregion
  }
}
