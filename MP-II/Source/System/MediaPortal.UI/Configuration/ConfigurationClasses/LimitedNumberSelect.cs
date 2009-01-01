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
 */

#endregion

namespace MediaPortal.Configuration.ConfigurationClasses
{
  public abstract class LimitedNumberSelect : NumberSelect
  {
    #region Variables

    protected double _lowerLimit;
    protected double _upperLimit;

    #endregion

    #region Protected Properties

    /// <summary>
    /// Gets the lower limit.
    /// </summary>
    public double LowerLimit
    {
      get { return _lowerLimit; }
      protected set { _lowerLimit = value; }
    }

    /// <summary>
    /// Gets the lower limit.
    /// </summary>
    public double UpperLimit
    {
      get { return _upperLimit; }
      protected set { _upperLimit = value; }
    }

    #endregion
  }
}
