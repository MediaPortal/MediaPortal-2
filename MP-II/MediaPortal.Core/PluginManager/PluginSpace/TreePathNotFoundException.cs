#region Copyright (C) 2007-2008 Team MediaPortal

/*
 *  Copyright (C) 2007-2008 Team MediaPortal
 *  http://www.team-mediaportal.com
 *
 *  This file is part of MediaPortal II
 *
 *  MediaPortal II is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  MediaPortal II is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
 * 
 *  Code modified from SharpDevelop AddIn code
 *  Thanks goes to: Mike Krüger
 */

#endregion

using System;
using System.Runtime.Serialization;
using MediaPortal.Core.PluginManager;

namespace MediaPortal.Core.PluginManager.PluginSpace
{
  /// <summary>
  /// Is thrown when the AddInTree could not find the requested path.
  /// </summary>
  [Serializable()]
  public class TreePathNotFoundException : PluginException
  {
    #region Constructors/Destructors
    /// <summary>
    /// Constructs a new <see cref="TreePathNotFoundException"/>
    /// </summary>
    public TreePathNotFoundException(string path) : base("Treepath not found: " + path)
    {
    }
		
    // Required for Serialization
    public TreePathNotFoundException() : base()
    {
    }
		
    public TreePathNotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }
		
    protected TreePathNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
    #endregion
  }
}