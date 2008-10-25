#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System.Collections.Generic;
using MediaPortal.Core;

namespace MediaPortal.Media.MediaManager
{
	public interface IMediaManager : IStatus
  {
    IList<IProvider> Providers { get;}

    IList<IRootContainer> Views { get;}

    /// <summary>
    /// register a new provider
    /// </summary>
    /// <param name="provider">the provider</param>
    void Register(IProvider provider);

    /// <summary>
    /// Register a new container
    /// </summary>
    /// <param name="container"></param>
    void Register(IRootContainer container);

    /// <summary>
    /// unregister a  provider
    /// </summary>
    /// <param name="provider">the provider</param>
    void UnRegister(IProvider provider);

    /// <summary>
    /// Returns  a list of all root containers.
    /// </summary>
    /// <value>The root containers.</value>
    IList<IRootContainer> RootContainers { get; }

    IList<IAbstractMediaItem> GetView(string path);

    IList<IAbstractMediaItem> GetView(IRootContainer parentItem);
  }
}
