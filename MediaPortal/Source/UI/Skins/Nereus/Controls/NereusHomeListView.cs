using MediaPortal.Common.General;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UI.SkinEngine.Controls.Visuals;
using MediaPortal.UiComponents.SkinBase.General;
using MediaPortal.Utilities;
using SharpDX;
using System.Collections.Generic;

namespace MediaPortal.UiComponents.Nereus.Controls
{
  /// <summary>
  /// Custom ListView that tries to set the focus on the last data item bound to the <see cref="RestoreFocusItem"/> property
  /// when the focus [re]enters the list. Used for the home menu to ensure that after returning from another screen the correct
  /// item is focused when navigating to the main list from the home content.
  /// </summary>
  public class NereusHomeListView : ListView
  {
    protected AbstractProperty _restoreFocusItemProperty = new SProperty(typeof(object), null);
    
    public AbstractProperty RestoreFocusItemProperty
    {
      get { return _restoreFocusItemProperty; }
    }

    public object RestoreFocusItem
    {
      get { return _restoreFocusItemProperty.GetValue(); }
      set { _restoreFocusItemProperty.SetValue(value); }
    }

    public override void AddPotentialFocusableElements(RectangleF? startingRect, ICollection<FrameworkElement> elements)
    {
      ICollection<FrameworkElement> potentialElements = new List<FrameworkElement>();
      base.AddPotentialFocusableElements(startingRect, potentialElements);

      object restoreFocusItem = RestoreFocusItem;

      // Try and find the element that has the restoreFocusItem as Context
      if (restoreFocusItem != null && _itemsHostPanel != null)
        foreach (FrameworkElement potentialElement in potentialElements)
        {
          Visual element = potentialElement;
          while (element != null && element.VisualParent != _itemsHostPanel)
            element = element.VisualParent;
          if (element != null && (element.Context == restoreFocusItem || IsSameWorkflowAction(restoreFocusItem as ListItem, element.Context as ListItem)))
          {
            // Just add this element to ensure that
            // it gets the focus
            elements.Add(potentialElement);
            return;
          }
        }

      // No matching element found, use the default focus selection
      CollectionUtils.AddAll(elements, potentialElements);
    }

    protected bool IsSameWorkflowAction(ListItem restoreItem, ListItem contextItem)
    {
      WorkflowAction restoreAction = restoreItem?.AdditionalProperties[Consts.KEY_ITEM_ACTION] as WorkflowAction;
      WorkflowAction contextAction = contextItem?.AdditionalProperties[Consts.KEY_ITEM_ACTION] as WorkflowAction;
      return restoreAction != null && contextAction != null && restoreAction.ActionId == contextAction.ActionId;
    }
  }
}
