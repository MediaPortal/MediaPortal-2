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

/****************************************************************************
While the underlying libraries are covered by LGPL, this sample is released 
as public domain.  It is distributed in the hope that it will be useful, but 
WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY 
or FITNESS FOR A PARTICULAR PURPOSE.  
*****************************************************************************/

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

using DirectShowLib;

using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace SkinEngine.Players.Vmr9
{

  [StructLayout(LayoutKind.Sequential)]
  public class Allocator
  {

  }
  [ComVisible(true)]
  [ClassInterface(ClassInterfaceType.None)]
  public class Allocator : IVMRSurfaceAllocator9, IVMRImagePresenter9, IVMRWindowlessControl9
  {
    #region constants
    private const int E_NOTIMPL = unchecked((int)0x80004001);
    private const int E_FAIL = unchecked((int)0x80004005);
    private const int D3DERR_INVALIDCALL = unchecked((int)0x8876086c);
    private const int DxMagicNumber = -759872593;
    #endregion

    #region variables
    private Device _device = null;
    private AdapterInformation _adapterInfo = null;
    private DeviceCreationParameters _creationParameters;

    IntPtr[] _surfaces;
    Texture _renderTexture;
    Surface _renderSurface;
    private Size _textureSize;
    private Size _videoSize;
    private Size _aspectRatio;
    private bool _deviceSet = false;
    private bool _isPresenting = false;
    private IVMRSurfaceAllocatorNotify9 vmrSurfaceAllocatorNotify = null;

    private bool disposed = false;
    #endregion

    #region ctor/dtor
    public Allocator(Device dev)
    {
      _adapterInfo = Manager.Adapters.Default;
      _device = dev;

      _creationParameters = _device.CreationParameters;
    }

    ~Allocator()
    {
      Dispose(false);
    }
    #endregion

    #region Members of IDisposable

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
      if (!disposed)
      {
        DeleteSurfaces();
        disposed = true;
      }
    }

    #endregion

    #region public properties
    public Size TextureSize
    {
      get
      {
        return _textureSize;
      }
    }

    public Size VideoSize
    {
      get
      {
        return _videoSize;
      }
    }
    public Size AspectRatio
    {
      get
      {
        return _aspectRatio;
      }
    }
    #endregion

    #region private members
    // Delete surfaces...
    private void DeleteSurfaces()
    {
      lock (this)
      {
        _isPresenting = false;
        if (_surfaces != null)
        {
          for (int i = 0; i < _surfaces.Length; ++i)
          {
            if (_surfaces[i] != IntPtr.Zero)
            {
              while (Marshal.Release(_surfaces[i]) > 0) ;
            }
          }
        }
        _surfaces = null;
        if (_renderSurface != null)
        {
          _renderSurface.Dispose();
          _renderSurface = null;
        }
        if (_renderTexture != null)
        {
          _renderTexture.Dispose();
          _renderTexture = null;
        }
      }
    }

    // Helper method to convert interger FourCC into a readable string
    private string FourCCToStr(int fcc)
    {
      byte[] chars = new byte[4];

      if (fcc < 100)
        return fcc.ToString();

      for (int i = 0; i < 4; i++)
      {
        chars[i] = (byte)(fcc & 0xff);
        fcc = fcc >> 8;
      }

      return System.Text.Encoding.ASCII.GetString(chars);
    }
    #endregion

    #region Members of IVMRSurfaceAllocator9

    public int InitializeDevice(IntPtr dwUserID, ref VMR9AllocationInfo lpAllocInfo, ref int lpNumBuffers)
    {
      int hr = 0;
      Trace.WriteLine(string.Format("vmr9:InitializeDevice WxH:{0}x{1} fmt:{2} [{3}] Pool:{4} flags:{5} buffers:{6}",
        lpAllocInfo.dwWidth, lpAllocInfo.dwHeight,
        FourCCToStr(lpAllocInfo.Format), (Format)lpAllocInfo.Format,
        (Pool)lpAllocInfo.Pool,
        lpAllocInfo.dwFlags, lpNumBuffers));

      // if format is YUV ? (note : 0x30303030 = "    ")
      if (lpAllocInfo.Format > 0x30303030)
      {
        // Check if the hardware support format conversion from this YUV format to the RGB desktop format
        if (!Manager.CheckDeviceFormatConversion(_creationParameters.AdapterOrdinal, _creationParameters.DeviceType, (Format)lpAllocInfo.Format, _adapterInfo.CurrentDisplayMode.Format))
        {
          // If not, refuse this format!
          // The VMR9 will propose other formats supported by the downstream filter output pin.
          Trace.WriteLine("vmr9:InitializeDevice:device does not support this format");
          return D3DERR_INVALIDCALL;
        }
      }

      try
      {
        IntPtr unmanagedDevice = _device.GetObjectByValue(DxMagicNumber);
        IntPtr hMonitor = Manager.GetAdapterMonitor(_adapterInfo.Adapter);

        //if (!_deviceSet)
        {
          _deviceSet = true;
          // Give our Direct3D device to the VMR9 filter
          hr = vmrSurfaceAllocatorNotify.SetD3DDevice(unmanagedDevice, hMonitor);
          DsError.ThrowExceptionForHR(hr);
        }

        _videoSize = new Size(lpAllocInfo.dwWidth, lpAllocInfo.dwHeight);
        _aspectRatio = lpAllocInfo.szAspectRatio;
        int width = 1;
        int height = 1;

        // If hardware require textures to power of two sized
        //if (_device.DeviceCaps.TextureCaps.SupportsPower2)
        {
          // Compute the ideal size
          while (width < lpAllocInfo.dwWidth)
            width = width << 1;
          while (height < lpAllocInfo.dwHeight)
            height = height << 1;

          // notify this change to the VMR9 filter
          lpAllocInfo.dwWidth = width;
          lpAllocInfo.dwHeight = height;
        }


        // Just in case
        DeleteSurfaces();
        _surfaces = new IntPtr[lpNumBuffers];
        int bufCount = lpNumBuffers;
        VMR9AllocationInfo info = lpAllocInfo;
        hr = vmrSurfaceAllocatorNotify.AllocateSurfaceHelper(ref lpAllocInfo, ref lpNumBuffers, _surfaces);
        if (hr != 0)
        {
          _surfaces = null;
          Trace.WriteLine("vmr9:InitializeDevice :fail");
          return E_FAIL;
        }
        _renderTexture = new Texture(_device, lpAllocInfo.dwWidth, lpAllocInfo.dwHeight, 1, Usage.RenderTarget, _adapterInfo.CurrentDisplayMode.Format, Pool.Default);
        _textureSize = new Size(lpAllocInfo.dwWidth, lpAllocInfo.dwHeight);
        _renderSurface = _renderTexture.GetSurfaceLevel(0);
      }
      catch (DirectXException e)
      {
        // A Direct3D error can occure : Notify it to the VMR9 filter
        Trace.WriteLine("vmr9:InitializeDevice :fail dx exception");
        return e.ErrorCode;
      }
      catch
      {
        // Or else, notify a more general error
        Trace.WriteLine("vmr9:InitializeDevice :fail exception");
        return E_FAIL;
      }

      // This allocation is a success
      _isPresenting = true;
      Trace.WriteLine("vmr9:InitializeDevice :success");
      return 0;
    }

    public int TerminateDevice(IntPtr dwID)
    {
      Trace.WriteLine("vmr9:TerminateDevice");
      DeleteSurfaces();
      return 0;
    }

    public int GetSurface(IntPtr dwUserID, int SurfaceIndex, int SurfaceFlags, out IntPtr lplpSurface)
    {
      lplpSurface = IntPtr.Zero;
      Trace.WriteLine("vmr9:GetSurface:");

      if (_surfaces == null)
      {
        Trace.WriteLine("vmr9:GetSurface:no surfaces");
        return E_FAIL;
      }

      // If the filter ask for an invalid buffer index, return an error.
      if (SurfaceIndex >= _surfaces.Length)
      {
        Trace.WriteLine("vmr9:GetSurface:invalid surface index");
        return E_FAIL;
      }
      lock (this)
      {
        // IVMRSurfaceAllocator9.GetSurface documentation state that the caller release the returned 
        // interface so we must increment its reference count.
        lplpSurface = _surfaces[SurfaceIndex];
        Marshal.AddRef(lplpSurface);
        return 0;
      }
    }

    public int AdviseNotify(IVMRSurfaceAllocatorNotify9 lpIVMRSurfAllocNotify)
    {
      lock (this)
      {
        Trace.WriteLine("vmr9:AdviseNotify");
        vmrSurfaceAllocatorNotify = lpIVMRSurfAllocNotify;

        // Give our Direct3D device to the VMR9 filter
        IntPtr unmanagedDevice = _device.GetObjectByValue(DxMagicNumber);
        IntPtr hMonitor = Manager.GetAdapterMonitor(Manager.Adapters.Default.Adapter);

        return vmrSurfaceAllocatorNotify.SetD3DDevice(unmanagedDevice, hMonitor);
      }
    }

    #endregion

    #region Membres de IVMRImagePresenter9

    public int StartPresenting(IntPtr dwUserID)
    {
      Trace.WriteLine("vmr9:StartPresenting");
      lock (this)
      {
        if (_device == null)
          return E_FAIL;
        return 0;
      }
    }

    public int StopPresenting(IntPtr dwUserID)
    {
      Trace.WriteLine("vmr9:StopPresenting");
      return 0;
    }
    public void Render()
    {
      if (!_isPresenting) return;
      if (_renderTexture == null) return;
      Trace.WriteLine("vmr9:render..");

      // Set the allocator texture as active texture
      GraphicsDevice.Device.SetTexture(0, _renderTexture);

      // Draw the Quad
      GraphicsDevice.Device.DrawPrimitives(PrimitiveType.TriangleFan, 0, 2);

    }

    public int PresentImage(IntPtr dwUserID, ref VMR9PresentationInfo lpPresInfo)
    {
      lock (this)
      {
        try
        {
          Trace.WriteLine("vmr9:PresentImage..");
          _aspectRatio = lpPresInfo.szAspectRatio;
          if (lpPresInfo.lpSurf == IntPtr.Zero) return 0;
          _isPresenting = true;
          int refcount = Marshal.AddRef(lpPresInfo.lpSurf);
          unsafe
          {
            Surface surf = new Surface(lpPresInfo.lpSurf);
            _device.StretchRectangle(
                  surf,
                  new Rectangle(Point.Empty, _videoSize),
                  _renderSurface,
                  new Rectangle(Point.Empty, _videoSize),
                  TextureFilter.None
                  );
            surf.Dispose();

            //refcount = Marshal.AddRef(lpPresInfo.lpSurf);
            //refcount = Marshal.Release(lpPresInfo.lpSurf);

            //int xxx = 1;
          }
        }
        catch (DirectXException e)
        {
          // A Direct3D error can occure : Notify it to the VMR9 filter
          Trace.WriteLine("vmr9:PresentImage:dx exception");
          return e.ErrorCode;
        }
        catch
        {
          // Or else, notify a more general error
          Trace.WriteLine("vmr9:PresentImage:exception");
          return E_FAIL;
        }

        // This presentation is a success
        return 0;
      }
    }

    #endregion

    #region IVMRWindowlessControl9 Members

    public int DisplayModeChanged()
    {
      return E_NOTIMPL;
    }

    public int GetAspectRatioMode(out VMR9AspectRatioMode lpAspectRatioMode)
    {
      lpAspectRatioMode = VMR9AspectRatioMode.None;
      return 0;
    }

    public int GetBorderColor(out int lpClr)
    {
      lpClr = 0;
      return E_NOTIMPL;
    }

    public int GetCurrentImage(out IntPtr lpDib)
    {
      lpDib = IntPtr.Zero;
      return E_NOTIMPL;
    }

    public int GetMaxIdealVideoSize(out int lpWidth, out int lpHeight)
    {
      lpWidth = _videoSize.Width;
      lpHeight = _videoSize.Height;
      return E_NOTIMPL;
    }

    public int GetMinIdealVideoSize(out int lpWidth, out int lpHeight)
    {
      lpWidth = _videoSize.Width;
      lpHeight = _videoSize.Height;
      return E_NOTIMPL;
    }

    public int GetNativeVideoSize(out int lpWidth, out int lpHeight, out int lpARWidth, out int lpARHeight)
    {
      lpWidth = _videoSize.Width;
      lpHeight = _videoSize.Height;
      lpARWidth = _aspectRatio.Width;
      lpARHeight = _aspectRatio.Height;
      return 0;
    }

    public int GetVideoPosition(DsRect lpSRCRect, DsRect lpDSTRect)
    {
      lpSRCRect.left = 0;
      lpSRCRect.right = 0;
      lpSRCRect.top = _videoSize.Width;
      lpSRCRect.bottom = _videoSize.Height;

      lpDSTRect.left = 0;
      lpDSTRect.right = 0;
      lpDSTRect.top = _videoSize.Width;
      lpDSTRect.bottom = _videoSize.Height;
      return 0;
    }

    public int RepaintVideo(IntPtr hwnd, IntPtr hdc)
    {
      return E_NOTIMPL;
    }

    public int SetAspectRatioMode(VMR9AspectRatioMode AspectRatioMode)
    {
      Trace.WriteLine("vmr9:SetAspectRatioMode:" + AspectRatioMode.ToString());
      return 0;
    }

    public int SetBorderColor(int Clr)
    {
      return E_NOTIMPL;
    }

    public int SetVideoClippingWindow(IntPtr hwnd)
    {
      return E_NOTIMPL;
    }

    public int SetVideoPosition(DsRect lpSRCRect, DsRect lpDSTRect)
    {
      return E_NOTIMPL;
    }

    #endregion
  }
}
