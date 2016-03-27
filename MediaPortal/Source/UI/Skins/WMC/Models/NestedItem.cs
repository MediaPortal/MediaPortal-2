using MediaPortal.UI.Presentation.DataObjects;

namespace MediaPortal.UiComponents.WMCSkin.Models
{
  public class NestedItem : ListItem
  {
    public NestedItem(string name, string value):
      base(name, value)
    {
      SubItems = new ItemsList();
    }
    public NestedItem()
    {
      SubItems = new ItemsList();
    }
    public ItemsList SubItems { get; private set; }
  }
}
