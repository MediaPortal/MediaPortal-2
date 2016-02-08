using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Deusty.Net;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.WifiRemote.Messages;
using MediaPortal.Plugins.WifiRemote.SendMessages;
using MediaPortal.Plugins.WifiRemote.Utils;
using MediaPortal.UI.Presentation;
using MediaPortal.UI.Presentation.Screens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MediaPortal.Plugins.WifiRemote.MessageParser
{
  internal class ParserScreenshot
  {
    private static ImageHelper _imageHelper;
    private static ConcurrentDictionary<AsyncSocket, int> _socketsWaitingForScreenshot;
    
    public static bool Parse(JObject message, SocketServer server, AsyncSocket sender)
    {
      if (_socketsWaitingForScreenshot == null)
      {
        _socketsWaitingForScreenshot = new ConcurrentDictionary<AsyncSocket, int>();
      }

      // Width to resize the image to, 0 to keep original width
      int imageWidth = (message["Width"] != null) ? (int)message["Width"] : 0;

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

      return true;
    }

    /// <summary>
    /// A requested screenshot is ready, send it to all interested clients
    /// </summary>
    static void ImageHelperScreenshotReady()
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
    static void ImageHelperScreenshotFailed(ImageHelperError error)
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
    static void SendScreenshotToClient(AsyncSocket sender, int width, ImageHelperError error)
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
