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

using System.Collections.Concurrent;
using System.Threading.Tasks;
using Deusty.Net;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.WifiRemote.Messages;
using MediaPortal.Plugins.WifiRemote.SendMessages;
using MediaPortal.Plugins.WifiRemote.Utils;
using Newtonsoft.Json.Linq;

namespace MediaPortal.Plugins.WifiRemote.MessageParser
{
  internal class ParserScreenshot : BaseParser
  {
    private static ImageHelper _imageHelper;
    private static ConcurrentDictionary<AsyncSocket, int> _socketsWaitingForScreenshot;
    
    public static Task<bool> ParseAsync(JObject message, SocketServer server, AsyncSocket sender)
    {
      if (_socketsWaitingForScreenshot == null)
      {
        _socketsWaitingForScreenshot = new ConcurrentDictionary<AsyncSocket, int>();
      }

      // Width to resize the image to, 0 to keep original width
      int imageWidth = GetMessageValue<int>(message, "Width");

      // Requests are added to a "waiting queue" because taking the screenshot happens 
      // async.
      _socketsWaitingForScreenshot.GetOrAdd(sender, imageWidth);

      if (_imageHelper == null)
      {
        _imageHelper = new ImageHelper();
        _imageHelper.ScreenshotReady += ImageHelperScreenshotReady;
        _imageHelper.ScreenshotFailed += ImageHelperScreenshotFailed;
      }

      _imageHelper.TakeScreenshot();

      return Task.FromResult(true);
    }

    /// <summary>
    /// A requested screenshot is ready, send it to all interested clients
    /// </summary>
    private static void ImageHelperScreenshotReady()
    {
      foreach (var pair in _socketsWaitingForScreenshot)
      {
        SendScreenshotToClient(pair.Key, pair.Value, null);
      }

      _socketsWaitingForScreenshot = null;
    }

    /// <summary>
    /// The screenshot could not be taken. Inform clients.
    /// </summary>
    /// <param name="error"></param>
    private static void ImageHelperScreenshotFailed(ImageHelperError error)
    {
      foreach (var pair in _socketsWaitingForScreenshot)
      {
        SendScreenshotToClient(pair.Key, pair.Value, error);
      }

      _socketsWaitingForScreenshot = null;
    }

    /// <summary>
    /// Send the current screenshot to the client as byte array
    /// </summary>
    private static void SendScreenshotToClient(AsyncSocket sender, int width, ImageHelperError error)
    {
      MessageScreenshot screenshot = new MessageScreenshot();

      if (error != null)
      {
        screenshot.Error = error;
      }
      else
      {
        screenshot.Screenshot = _imageHelper.resizedScreenshot(width);
      }
      SendMessageToClient.Send(screenshot, sender);
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
