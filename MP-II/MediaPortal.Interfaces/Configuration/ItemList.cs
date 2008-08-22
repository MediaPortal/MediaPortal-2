using System;
using System.Collections.Generic;

using MediaPortal.Presentation.Localisation;


namespace MediaPortal.Configuration
{

  /// <summary>
  /// OptionList has no actual functionality implemented,
  /// it's only used to define that the ConfigBase has a list of items.
  /// </summary>
  public class ItemList : ConfigBase
  {

    #region Variables

    protected IList<StringId> _items = new List<StringId>();

    #endregion

    #region Properties

    /// <summary>
    /// Gets all items in the list.
    /// </summary>
    public IList<StringId> Items
    {
      get { return _items; }
    }

    #endregion

  }
}
