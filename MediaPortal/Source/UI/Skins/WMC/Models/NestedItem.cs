using MediaPortal.Common.General;
using MediaPortal.UI.Presentation.DataObjects;

namespace MediaPortal.UiComponents.WMCSkin.Models
{
  public class NestedItem : ListItem
  {
    protected AbstractProperty _afterSelectedProperty = new WProperty(typeof(bool), false);

    public NestedItem(string name, string value) :
      base(name, value)
    {
    }
    public NestedItem()
    {
    }

    public AbstractProperty AfterSelectedProperty
    {
      get { return _afterSelectedProperty; }
    }

    public bool AfterSelected
    {
      get { return (bool)_afterSelectedProperty.GetValue(); }
      set { _afterSelectedProperty.SetValue(value); }
    }
  }
}