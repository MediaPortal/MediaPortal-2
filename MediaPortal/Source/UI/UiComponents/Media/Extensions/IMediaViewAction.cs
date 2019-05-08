using MediaPortal.Common.Async;
using MediaPortal.Common.MediaManagement;
using MediaPortal.UiComponents.Media.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.UiComponents.Media.Extensions
{
  /// <summary>
  /// Extension interface to add actions for <see cref="View"/>s. Plugins can implement this interface and register the class in
  /// <c>plugin.xml</c> <see cref="MediaViewActionBuilder.MEDIA_EXTENSION_PATH"/> path.
  /// The action is added to the Info menu when the cursor is on a ViewItem in a media list.
  /// </summary>
  public interface IMediaViewAction
  {
    /// <summary>
    /// Checks if this action is available for the given <paramref name="MediaView"/>.
    /// </summary>
    /// <param name="view">View</param>
    /// <returns><c>true</c> if available</returns>
    Task<bool> IsAvailableAsync(View view);

    /// <summary>
    /// Executes the action for the given View (the one that would be switched to if the user pressed OK on the ViewItem).
    /// </summary>
    /// <param name="view">View</param>
    /// <returns>
    /// <see cref="AsyncResult{T}.Success"/> <c>true</c> if successful.
    /// <see cref="AsyncResult{T}.Result"/> returns what kind of changes was done on MediaView.
    /// </returns>
    Task<bool> ProcessAsync(View view);
  }

  /// <summary>
  /// Defined for actions that need a confirmation before.
  /// </summary>
  public interface IMediaViewActionConfirmation : IMediaViewAction
  {
    /// <summary>
    /// Gets the confirmation message.
    /// </summary>
    string ConfirmationMessage(View view);
  }

  /// <summary>
  /// Marker interface for actions that need a deferred execution in the former NavigationContext.
  /// </summary>
  public interface IDeferredMediaViewAction : IMediaViewAction
  {
  }

  /// <summary>
  /// Interface to be used in all ListItems which might have IMediaViewActions attached
  /// </summary>
  public interface IViewListItem
  {
    View View { get; }
  }
}
