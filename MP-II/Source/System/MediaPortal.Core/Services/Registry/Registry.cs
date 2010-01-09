#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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
using MediaPortal.Core.Registry;

namespace MediaPortal.Core.Services.Registry
{
  /// <summary>
  /// Non-persistent application registry implementation.
  /// </summary>
  public class Registry: IRegistry, IStatus
  {
    #region Protected fields

    protected object _syncObj = new object();
    protected RegistryNode _rootNode;

    #endregion

    #region Ctor

    public Registry()
    {
      _rootNode = new RegistryNode(null, string.Empty, _syncObj);
    }

    #endregion

    #region IRegistry implementation

    public object SyncObj
    {
      get { return _syncObj; }
    }

    public IRegistryNode RootNode
    {
      get { return _rootNode; }
    }

    public IRegistryNode GetRegistryNode(string path, bool createOnNotExist)
    {
      RegistryHelper.CheckAbsolute(path);
      return _rootNode.GetSubNodeByPath(RegistryHelper.RemoveRootFromAbsolutePath(path), createOnNotExist);
    }

    public IRegistryNode GetRegistryNode(string path)
    {
      RegistryHelper.CheckAbsolute(path);
      return _rootNode.GetSubNodeByPath(path.Substring(1));
    }

    public bool RegistryNodeExists(string path)
    {
      RegistryHelper.CheckAbsolute(path);
      return _rootNode.SubNodeExists(path.Substring(1));
    }

    #endregion

    #region IStatus implementation

    public IList<string> GetStatus()
    {
      IList<string> result = new List<string> {"=== Registry"};
      foreach (string line in _rootNode.GetStatus())
        result.Add("  " + line);
      return result;
    }

    #endregion
  }
}
