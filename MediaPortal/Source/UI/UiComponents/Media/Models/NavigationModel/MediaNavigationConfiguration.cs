using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.UiComponents.Media.FilterTrees;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.UiComponents.Media.Models.NavigationModel
{
  /// <summary>
  /// Configuration to be used to override default media navigation initialization.
  /// </summary>
  public class MediaNavigationConfiguration
  {
    /// <summary>
    /// The screen to use as the navigation root. This screen will be used to
    /// load the screen hierarchy and will be removed from the list of available screens.
    /// </summary>
    public Type RootScreenType { get; set; }

    /// <summary>
    /// The default screen to show if there is no saved screen hierarchy. 
    /// </summary>
    public Type DefaultScreenType { get; set; }

    /// <summary>
    /// Media item id to use to apply a MediaItemIdFilter to the root media view.
    /// </summary>
    public Guid? LinkedId { get; set; }

    /// <summary>
    /// Filter to apply to the root media view.
    /// </summary>
    public IFilter Filter { get; set; }

    /// <summary>
    /// Relationship of the linked id/filter to the base media items
    /// </summary>
    public FilterTreePath FilterPath { get; set; }
  }
}
