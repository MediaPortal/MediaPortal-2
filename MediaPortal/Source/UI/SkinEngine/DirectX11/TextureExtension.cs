#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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

using System.IO;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.WIC;

namespace MediaPortal.UI.SkinEngine.DirectX11
{
  public static class Texture2DExtensions
  {
    public static void Save(this Texture2D texture, Stream stream)
    {
      DeviceContext context3D = GraphicsDevice11.Instance.Device3D1.ImmediateContext;

      var textureCopy = new Texture2D(GraphicsDevice11.Instance.Device3D1, new Texture2DDescription
      {
        Width = texture.Description.Width,
        Height = texture.Description.Height,
        MipLevels = 1,
        ArraySize = 1,
        Format = texture.Description.Format,
        Usage = ResourceUsage.Staging,
        SampleDescription = new SampleDescription(1, 0),
        BindFlags = BindFlags.None,
        CpuAccessFlags = CpuAccessFlags.Read,
        OptionFlags = ResourceOptionFlags.None
      });
      context3D.CopyResource(texture, textureCopy);

      DataStream dataStream;
      var dataBox = context3D.MapSubresource(
        textureCopy,
        0,
        0,
        MapMode.Read,
        SharpDX.Direct3D11.MapFlags.None,
        out dataStream);

      var dataRectangle = new DataRectangle
      {
        DataPointer = dataStream.DataPointer,
        Pitch = dataBox.RowPitch
      };

      var bitmap = new Bitmap(
        GraphicsDevice11.Instance.FactoryWIC,
        textureCopy.Description.Width,
        textureCopy.Description.Height,
        PixelFormat.Format32bppBGRA,
        dataRectangle);

      using (var s = stream)
      {
        s.Position = 0;
        using (var bitmapEncoder = new PngBitmapEncoder(GraphicsDevice11.Instance.FactoryWIC, s))
        {
          using (var bitmapFrameEncode = new BitmapFrameEncode(bitmapEncoder))
          {
            bitmapFrameEncode.Initialize();
            bitmapFrameEncode.SetSize(bitmap.Size.Width, bitmap.Size.Height);
            var pixelFormat = PixelFormat.FormatDontCare;
            bitmapFrameEncode.SetPixelFormat(ref pixelFormat);
            bitmapFrameEncode.WriteSource(bitmap);
            bitmapFrameEncode.Commit();
            bitmapEncoder.Commit();
          }
        }
      }

      context3D.UnmapSubresource(textureCopy, 0);
      textureCopy.Dispose();
      bitmap.Dispose();
    }
  }
}
