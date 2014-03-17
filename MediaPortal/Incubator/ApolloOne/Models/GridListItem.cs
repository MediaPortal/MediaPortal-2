using MediaPortal.Common.General;
using MediaPortal.UI.Presentation.DataObjects;

namespace MediaPortal.UiComponents.ApolloOne.Models
{
  /// <summary>
  /// Extends a generic <see cref="ListItem"/> with position and size information.
  /// </summary>
  public class GridListItem : ListItem
  {
    readonly AbstractProperty _gridRowProperty = new WProperty(typeof(int), 0);
    readonly AbstractProperty _gridRowSpanProperty = new WProperty(typeof(int), 1);
    readonly AbstractProperty _gridColumnProperty = new WProperty(typeof(int), 0);
    readonly AbstractProperty _gridColumnSpanProperty = new WProperty(typeof(int), 1);

    public int GridRow
    {
      get { return (int)_gridRowProperty.GetValue(); }
      set { _gridRowProperty.SetValue(value); }
    }

    public AbstractProperty GridRowProperty
    {
      get { return _gridRowProperty; }
    }

    public int GridRowSpan
    {
      get { return (int)_gridRowSpanProperty.GetValue(); }
      set { _gridRowSpanProperty.SetValue(value); }
    }
    public AbstractProperty GridRowSpanProperty
    {
      get { return _gridRowSpanProperty; }
    }

    public int GridColumn
    {
      get { return (int)_gridColumnProperty.GetValue(); }
      set { _gridColumnProperty.SetValue(value); }
    }

    public AbstractProperty GridColumnProperty
    {
      get { return _gridColumnProperty; }
    }

    public int GridColumnSpan
    {
      get { return (int)_gridColumnSpanProperty.GetValue(); }
      set { _gridColumnSpanProperty.SetValue(value); }
    }

    public AbstractProperty GridColumnSpanProperty
    {
      get { return _gridColumnSpanProperty; }
    }

    #region Constructor

    public GridListItem()
    {
    }

    public GridListItem(ListItem origItem)
    {
      AdditionalProperties = origItem.AdditionalProperties;
      Enabled = origItem.Enabled;
      Command = origItem.Command;
      IsVisible = origItem.IsVisible;
      Labels = origItem.Labels;
      Selected = origItem.Selected;
    }

    #endregion
  }
}
