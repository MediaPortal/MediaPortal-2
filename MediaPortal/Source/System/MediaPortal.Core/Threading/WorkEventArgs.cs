#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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

namespace MediaPortal.Core.Threading
{
  public class WorkEventArgs
  {
    public readonly IWork Work;
    private Type resultType;
    private object result;

    public WorkEventArgs(IWork work)
    {
      Work = work;
    }

    public WorkState State
    {
      get { return Work.State; }
    }

    public Exception Exception
    {
      get { return Work.Exception; }
    }

    public void SetResult<T>(T result)
    {
      resultType = typeof(T);
      this.result = result;
    }

    public T GetResult<T>()
    {
      Type t = typeof(T);
      if (t == resultType)
      {
        return (T)result;
      }
      return default(T);
    }
  }

}
