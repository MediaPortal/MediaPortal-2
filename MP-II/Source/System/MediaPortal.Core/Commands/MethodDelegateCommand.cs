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

namespace MediaPortal.Core.Commands
{
  public delegate void ParameterlessMethod();

  public class MethodDelegateCommand : ICommand
  {
    #region Protected fields

    protected ParameterlessMethod _methodDelegate;

    #endregion

    public MethodDelegateCommand(ParameterlessMethod methodDelegate)
    {
      _methodDelegate = methodDelegate;
    }

    #region ICommand implementation

    public void Execute()
    {
      _methodDelegate();
    }

    #endregion
  }
}