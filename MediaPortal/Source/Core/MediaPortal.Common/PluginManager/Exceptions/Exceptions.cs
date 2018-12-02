#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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

namespace MediaPortal.Common.PluginManager.Exceptions
{
  /// <summary>
  /// Base class for all exceptions in the plugin manager classes.
  /// </summary>
  public class PluginManagerException : ApplicationException
  {
    public PluginManagerException(string msg, params object[] args):
        base(string.Format(msg, args)) { }
    public PluginManagerException(string msg, Exception ex, params object[] args):
        base(string.Format(msg, args), ex) { }
  }

  public class PluginInvalidStateException : PluginManagerException
  {
    public PluginInvalidStateException(string msg, params object[] args):
        base(msg, args) { }
    public PluginInvalidStateException(string msg, Exception ex, params object[] args):
        base(msg, ex, args) { }
  }

  public class PluginMissingDependencyException : PluginManagerException
  {
    public PluginMissingDependencyException(string msg, params object[] args):
        base(msg, args) { }
    public PluginMissingDependencyException(string msg, Exception ex, params object[] args):
        base(msg, ex, args) { }
  }

  public class PluginInternalException : PluginManagerException
  {
    public PluginInternalException(string msg, params object[] args):
        base(msg, args) { }
    public PluginInternalException(string msg, Exception ex, params object[] args):
        base(msg, ex, args) { }
  }

  public class PluginItemBuildException : PluginManagerException
  {
    public PluginItemBuildException(string msg, params object[] args):
        base(msg, args) { }
    public PluginItemBuildException(string msg, Exception ex, params object[] args):
        base(msg, ex, args) { }
  }

  public class PluginLockException : PluginManagerException
  {
    public PluginLockException(string msg, params object[] args):
        base(msg, args) { }
    public PluginLockException(string msg, Exception ex, params object[] args):
        base(msg, ex, args) { }
  }

  public class PluginIncompatibleException : PluginManagerException
  {
    public PluginIncompatibleException(string msg, params object[] args) :
      base(msg, args) { }
    public PluginIncompatibleException(string msg, Exception ex, params object[] args) :
      base(msg, ex, args) { }
  }
}
