#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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
using System.Threading.Tasks;

namespace MediaPortal.Plugins.AspNetServer
{
  /// <summary>
  /// Used to serialize starting and stopping WebApplications
  /// </summary>
  public class AspNetServerAction
  {
    #region Enums

    /// <summary>
    /// Type of the action to be performed
    /// </summary>
    public enum ActionType
    {
      Start,
      Stop
    }

    #endregion

    #region Public properties

    /// <summary>
    /// Indicates whether the WebApplication shall be started (<see cref="ActionType.Start"/>) or stopped (<see cref="ActionType.Stop"/>)
    /// </summary>
    public ActionType Action { get; }

    /// <summary>
    /// Necessary parameters to start (or stop) a WebApplication
    /// </summary>
    public WebApplicationParameter WebApplicationParameter { get; }

    /// <summary>
    /// Represents the start (or stop) proces of a WebApplication
    /// </summary>
    public TaskCompletionSource<bool> Tcs { get; }

    #endregion

    #region Constructor

    /// <summary>
    /// Creates a new instance of this class
    /// </summary>
    /// <param name="action"><see cref="ActionType"/> to be performed</param>
    /// <param name="webApplicationParameter">Parameters necessary to start (or stop) a WebApplication</param>
    /// <exception cref="ArgumentNullException"><paramref name="webApplicationParameter"/> was null</exception>
    /// <remarks>Ensures that none of the public properties is null</remarks>
    public AspNetServerAction(ActionType action, WebApplicationParameter webApplicationParameter)
    {
      if (webApplicationParameter == null)
        throw new ArgumentNullException(nameof(WebApplicationParameter));
      Action = action;
      WebApplicationParameter = webApplicationParameter;
      Tcs = new TaskCompletionSource<bool>();
    }

    #endregion
  }
}
