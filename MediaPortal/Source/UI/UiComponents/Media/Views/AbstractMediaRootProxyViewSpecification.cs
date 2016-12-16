#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.ResourceAccess;

namespace MediaPortal.UiComponents.Media.Views
{
  /// <summary>
  /// View implementation which can be used for the root view of the local media view hierarchy.
  /// Depending on the information if the home server is located on the local machine and/or if both the home server and this client
  /// have shares configured, this view specification only shows the client's shares or the server's shares or both system's shares in different sub views.
  /// </summary>
  public abstract class AbstractMediaRootProxyViewSpecification : ViewSpecification
  {
    #region Ctor

    protected AbstractMediaRootProxyViewSpecification(string viewDisplayName,
        IEnumerable<Guid> necessaryMIATypeIds, IEnumerable<Guid> optionalMIATypeIds) :
        base(viewDisplayName, necessaryMIATypeIds, optionalMIATypeIds) { }

    #endregion

    #region Base overrides

    public override IViewChangeNotificator CreateChangeNotificator()
    {
      // Actually, we should also watch the client's and the server's set of shares. If they change, we should also generate a change event.
      return new ServerConnectionChangeNotificator();
    }

    public delegate void NavigateToViewDlgt(ViewSpecification vs);

    /// <summary>
    /// According to the actual class (local media/browse media), this method simulates a user navigation to the given <paramref name="localShare"/>.
    /// </summary>
    /// <param name="localShare">Client or server share which is located at the local system.</param>
    /// <param name="navigateToViewDlgt">Callback which will be called for each view specification to navigate to to do the actual navigation.</param>
    protected abstract void NavigateToLocalRootView(Share localShare, NavigateToViewDlgt navigateToViewDlgt);

    /// <summary>
    /// Creates a view specification for the given resource accessor in the context of a <see cref="Navigate"/> call.
    /// </summary>
    /// <param name="systemId">System id where the given <paramref name="viewRa"/> is located. This can be the local system id or the system id of
    /// the server.</param>
    /// <param name="viewRa">Resource accessor representing a sub view.</param>
    /// <returns>View specification which matches the views that are created in the navigation under this class.</returns>
    protected abstract ViewSpecification NavigateCreateViewSpecification(string systemId, IFileSystemResourceAccessor viewRa);

    /// <summary>
    /// Helper method which simulates a user navigation under this view specification to the given <paramref name="targetPath"/>
    /// under the given <paramref name="localShare"/>.
    /// </summary>
    /// <param name="localShare">Client or server share which is located at the local system.</param>
    /// <param name="targetPath">Resource path to navigate to. This path must be located under the given <paramref name="localShare"/>'s base path.</param>
    /// <param name="navigateToViewDlgt">Callback which will be called for each view specification to navigate to to do the actual navigation.</param>
    public void Navigate(Share localShare, ResourcePath targetPath, NavigateToViewDlgt navigateToViewDlgt)
    {
      NavigateToLocalRootView(localShare, navigateToViewDlgt);
      IResourceAccessor startRA;
      if (!localShare.BaseResourcePath.TryCreateLocalResourceAccessor(out startRA))
        return;
      IFileSystemResourceAccessor current = startRA as IFileSystemResourceAccessor;
      if (current == null)
      {
        // Wrong path resource, cannot navigate. Should not happen if the share is based on a filesystem resource,
        // but might happen if we have found a non-standard share.
        startRA.Dispose();
        return;
      }
      while (true)
      {
        ICollection<IFileSystemResourceAccessor> children = FileSystemResourceNavigator.GetChildDirectories(current, false);
        current.Dispose();
        current = null;
        if (children != null)
          foreach (IFileSystemResourceAccessor childDirectory in children)
            using (childDirectory)
            {
              if (childDirectory.CanonicalLocalResourcePath.IsSameOrParentOf(targetPath))
              {
                current = childDirectory;
                break;
              }
            }
        if (current == null)
          break;
        ViewSpecification newVS = NavigateCreateViewSpecification(localShare.SystemId, current);
        if (newVS == null)
        {
          current.Dispose();
          return;
        }
        navigateToViewDlgt(newVS);
      }
    }

    #endregion
  }
}
