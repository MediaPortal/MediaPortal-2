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

using System.Threading;
using System.Threading.Tasks;
using MediaPortal.Common.Async;
using NUnit.Framework;

namespace Tests.Client.Common
{
  class TestAsync
  {

    private async Task<bool> TestDelay()
    {
      await Task.Delay(1000);
      return true;
    }

    [Test]
    [TestCase(500, true)]
    [TestCase(1500, false)]
    public async Task TestTimeOut(int delayMs, bool expectedTimeout)
    {
      bool timedOut = false;
      try
      {
        using (var cts = new CancellationTokenSource(delayMs))
        {
          var finished = await TestDelay().WaitAsync(cts.Token);
          Assert.IsTrue(finished);
        }
      }
      catch (TaskCanceledException tce)
      {
        timedOut = true;
      }
      Assert.IsTrue(timedOut == expectedTimeout);
    }
  }
}
