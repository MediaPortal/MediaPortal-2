﻿#region Copyright (C) 2007-2015 Team MediaPortal

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

using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using Deusty.Net;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Plugins.WifiRemote.Messages;
using MediaPortal.Plugins.WifiRemote.SendMessages;
using MediaPortal.Plugins.WifiRemote.Utils;
using Newtonsoft.Json.Linq;

namespace MediaPortal.Plugins.WifiRemote.MessageParser
{
  internal class ParserImage
  {
    public static async Task<bool> ParseAsync(JObject message, SocketServer server, AsyncSocket sender)
    {
      string action = (string)message["Action"];

      if (!string.IsNullOrEmpty(action))
      {
        string imagePath = (string)message["ImagePath"];
        string userTag = (string)message["UserTag"];
        int maxWidth = (int)message["MaximumWidth"];
        int maxHeight = (int)message["MaximumHeight"];
        string id = (string)message["ImageId"];

        ServiceRegistration.Get<ILogger>().Debug("WifiRemote Play Image: UserTag: {0}, ImageId: {1}, ImagePath: {2}, MaximumWidth: {3}, MaximumHeight: {4}", userTag, id, imagePath, maxWidth, maxHeight);

        Guid mediaItemGuid;
        if (Guid.TryParse(imagePath, out mediaItemGuid))
          id = mediaItemGuid.ToString();

        if (!Guid.TryParse(id, out mediaItemGuid))
        {
          ServiceRegistration.Get<ILogger>().Error("WifiRemote Play Image: Couldn't convert ImageId {0} to Guid", id);
          return false;
        }

        MessageImage msg = new MessageImage();
        var mediaItem = await Helper.GetMediaItemByIdAsync(mediaItemGuid);
        Image image = null;
        IResourceLocator locator = mediaItem.GetResourceLocator();
        using (IResourceAccessor ra = locator.CreateAccessor())
        {
          IFileSystemResourceAccessor fsra = ra as IFileSystemResourceAccessor;
          if (fsra == null)
          {
            ServiceRegistration.Get<ILogger>().Error("WifiRemote Play Image: Couldn't read image {0}", id);
            return false;
          }
          using (Stream stream = fsra.OpenRead())
          {
            image = Image.FromStream(stream);
          }
        }

        if ((maxWidth > 0 && image.Width > maxWidth) || (maxHeight > 0 && image.Height > maxHeight))
        {
          int height = image.Height;
          int width = image.Width;
          if (maxHeight > 0 && height > maxHeight)
          {
            float ratio = (float)height / (float)maxHeight;
            width = Convert.ToInt32((float)width / ratio);
          }
          if (maxWidth > 0 && width > maxWidth)
          {
            width = maxWidth;
          }
          var newImage = ImageHelper.ResizedImage(image, width);
          image.Dispose();
          image = newImage;
        }

        byte[] data = ImageHelper.ImageToByteArray(image, System.Drawing.Imaging.ImageFormat.Jpeg);
        image?.Dispose();
        msg.ImagePath = mediaItem.ToString();
        msg.UserTag = userTag;
        msg.Image = Convert.ToBase64String(data);
        SendMessageToClient.Send(msg, sender);

        //await Helper.PlayMediaItemAsync(mediaItemGuid, 0);
      }

      return true;
    }
  }
}
