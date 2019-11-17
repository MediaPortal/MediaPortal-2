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

using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Extensions.OnlineLibraries.Wrappers;
using System;
using System.Threading.Tasks;

namespace MediaPortal.Extensions.OnlineLibraries.Matchers
{
  public class SubsMaxMatcher : SubtitleMatcher<string>
  {
    public const string NAME = "SubsMax.com";

    #region Init

    public SubsMaxMatcher() : base(NAME, nameof(SubsMaxMatcher))
    {
      //Will be overridden if the user enables it in settings
      Enabled = true;
    }

    public override Task<bool> InitWrapperAsync(bool useHttps)
    {
      try
      {
        SubsMaxWrapper wrapper = new SubsMaxWrapper(NAME);
        if (wrapper.Init())
        {
          _wrapper = wrapper;
          return Task.FromResult(true);
        }
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("SubsMaxMatcher: Error initializing wrapper", ex);
      }
      return Task.FromResult(false);
    }

    #endregion
  }
}
