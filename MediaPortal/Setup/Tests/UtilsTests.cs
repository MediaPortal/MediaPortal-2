#region Copyright (C) 2007-2021 Team MediaPortal

/*
    Copyright (C) 2007-2021 Team MediaPortal
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

using MP2BootstrapperApp.Utils;
using Xunit;

namespace Tests
{
  public class UtilsTests
  {
    [Theory]
    [InlineData(@"c:")]
    [InlineData(@"c:\")]
    [InlineData(@"c:\test")]
    [InlineData(@"c:\test\")]
    [InlineData(@"c:\test\path")]
    [InlineData(@"\\Server\test")]
    [InlineData(@"\\Server\test\")]
    [InlineData(@"\\Server\test\path")]
    void Should_IsValidInstallDirectoryReturnTrue_If_ValidDirectoryPath(string path)
    {
      bool isValid = PathUtils.IsValidAbsoluteDirectoryPath(path);

      Assert.True(isValid);
    }

    [Theory]
    [InlineData(@"c")]
    [InlineData(@"test")]
    [InlineData(@"test\path")]
    [InlineData(@"c:\test|")]
    [InlineData(@"c:\test|\")]
    [InlineData(@"c:\test?")]
    [InlineData(@"c:\test?\")]
    void Should_IsValidInstallDirectoryReturnFalse_If_InvalidDirectoryPath(string path)
    {
      bool isValid = PathUtils.IsValidAbsoluteDirectoryPath(path);

      Assert.False(isValid);
    }
  }
}
