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

namespace MediaPortal.Common.Async
{
  /// <summary>
  /// Wrapper class for common async method calls. It contains the <see cref="Success"/> return values and one additional <see cref="Result"/>.
  /// It is used to transform methods with out parameters more easy into async pattern where "out" and "ref" are not possible. 
  /// </summary>
  /// <typeparam name="T"></typeparam>
  public class AsyncResult<T>
  {
    public AsyncResult() { }

    public AsyncResult(bool success, T result)
    {
      Success = success;
      Result = result;
    }
    /// <summary>
    /// Returns <c>true</c> if successful.
    /// </summary>
    public bool Success { get; set; }
    /// <summary>
    /// Returns <c>true</c> if successful.
    /// </summary>
    public T Result { get; set; }
  }
}
