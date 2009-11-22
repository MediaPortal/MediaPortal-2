#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
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

using System;

namespace MediaPortal.Utilities.Exceptions
{
  /// <summary>
  /// Fatal exception, will be thrown if something happened what must not happen. The reason
  /// of this exception is always a coding problem.
  /// </summary>
  public class FatalException : ApplicationException
  {
    public FatalException(string msg, params object[] args):
        base(string.Format(msg, args)) { }
    public FatalException(string msg, Exception ex, params object[] args):
        base(string.Format(msg, args), ex) { }
  }

  /// <summary>
  /// Thrown if a module or instance is in an invalid state for the current operation.
  /// </summary>
  public class IllegalCallException : ApplicationException
  {
    public IllegalCallException(string msg, params object[] args):
        base(string.Format(msg, args)) { }
    public IllegalCallException(string msg, Exception ex, params object[] args):
        base(string.Format(msg, args), ex) { }
  }

  /// <summary>
  /// Thrown if a method call is not valid with the given parameters.
  /// </summary>
  public class InvalidDataException : ApplicationException
  {
    public InvalidDataException(string msg, params object[] args) :
      base(string.Format(msg, args)) { }
    public InvalidDataException(string msg, Exception ex, params object[] args) :
      base(string.Format(msg, args), ex) { }
  }

  /// <summary>
  /// Thrown if a module or a variable is in an unexpected state.
  /// </summary>
  public class UnexpectedStateException : ApplicationException
  {
    public UnexpectedStateException(string msg, params object[] args) :
      base(string.Format(msg, args)) { }
    public UnexpectedStateException(string msg, Exception ex, params object[] args) :
      base(string.Format(msg, args), ex) { }
  }

  /// <summary>
  /// Thrown if the environment doesn't act like expected.
  /// </summary>
  public class EnvironmentException : ApplicationException
  {
    public EnvironmentException(string msg, params object[] args) :
      base(string.Format(msg, args)) { }
    public EnvironmentException(string msg, Exception ex, params object[] args) :
      base(string.Format(msg, args), ex) { }
  }

  /// <summary>
  /// Thrown if a circular reference is detected.
  /// </summary>
  public class CircularReferenceException : ApplicationException
  {
    public CircularReferenceException(string msg, params object[] args) :
      base(string.Format(msg, args)) { }
    public CircularReferenceException(string msg, Exception ex, params object[] args) :
      base(string.Format(msg, args), ex) { }
  }
}
