using SharpGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.LibRetro.GLContexts
{
  public class OpenGLEx : OpenGL
  {
    protected Dictionary<string, Delegate> _extensionFunctions = new Dictionary<string, Delegate>();

    /// <summary>
    /// Returns a delegate for an extension function. This delegate  can be called to execute the extension function.
    /// </summary>
    /// <typeparam name="T">The extension delegate type.</typeparam>
    /// <returns>The delegate that points to the extension function.</returns>
    protected T GetDelegateFor<T>() where T : class
    {
      //  Get the type of the extension function.
      Type delegateType = typeof(T);

      //  Get the name of the extension function.
      string name = delegateType.Name;

      // ftlPhysicsGuy - Better way
      Delegate del = null;
      if (_extensionFunctions.TryGetValue(name, out del) == false)
      {
        IntPtr proc = Win32.wglGetProcAddress(name);
        if (proc == IntPtr.Zero)
          throw new Exception("Extension function " + name + " not supported");

        //  Get the delegate for the function pointer.
        del = Marshal.GetDelegateForFunctionPointer(proc, delegateType);

        //  Add to the dictionary.
        _extensionFunctions.Add(name, del);
      }

      return del as T;
    }

    #region WGL_NV_DX_interop / WGL_NV_DX_interop2

    public bool DXSetResourceShareHandleNV(IntPtr dxObject, IntPtr shareHandle)
    {
      return GetDelegateFor<wglDXSetResourceShareHandleNV>()(dxObject, shareHandle);
    }

    public IntPtr DXOpenDeviceNV(IntPtr dxDevice)
    {
      return GetDelegateFor<wglDXOpenDeviceNV>()(dxDevice);
    }

    public bool DXCloseDeviceNV(IntPtr hDevice)
    {
      return GetDelegateFor<wglDXCloseDeviceNV>()(hDevice);
    }

    public IntPtr DXRegisterObjectNV(IntPtr hDevice, IntPtr dxObject, uint name, uint type, uint access)
    {
      return GetDelegateFor<wglDXRegisterObjectNV>()(hDevice, dxObject, name, type, access);
    }

    public bool DXUnregisterObjectNV(IntPtr hDevice, IntPtr hObject)
    {
      return GetDelegateFor<wglDXUnregisterObjectNV>()(hDevice, hObject);
    }

    public bool DXObjectAccessNV(IntPtr hObject, uint access)
    {
      return GetDelegateFor<wglDXObjectAccessNV>()(hObject, access);
    }

    public bool DXLockObjectsNV(IntPtr hDevice, IntPtr[] hObjects)
    {
      unsafe
      {
        void** objects = stackalloc void*[hObjects.Length];

        for (int i = 0; i < hObjects.Length; i++)
        {
          objects[i] = hObjects[i].ToPointer();
        }

        return GetDelegateFor<wglDXLockObjectsNV>()(hDevice, hObjects.Length, objects);
      }
    }

    public bool DXUnlockObjectsNV(IntPtr hDevice, IntPtr[] hObjects)
    {
      unsafe
      {
        void** objects = stackalloc void*[hObjects.Length];

        for (int i = 0; i < hObjects.Length; i++)
        {
          objects[i] = hObjects[i].ToPointer();
        }

        return GetDelegateFor<wglDXUnlockObjectsNV>()(hDevice, hObjects.Length, objects);
      }
    }

    private delegate bool wglDXSetResourceShareHandleNV(IntPtr dxObject, IntPtr shareHandle);
    private delegate IntPtr wglDXOpenDeviceNV(IntPtr dxDevice);
    private delegate bool wglDXCloseDeviceNV(IntPtr hDevice);
    private delegate IntPtr wglDXRegisterObjectNV(IntPtr hDevice, IntPtr dxObject, uint name, uint type, uint access);
    private delegate bool wglDXUnregisterObjectNV(IntPtr hDevice, IntPtr hObject);
    private delegate bool wglDXObjectAccessNV(IntPtr hObject, uint access);
    private unsafe delegate bool wglDXLockObjectsNV(IntPtr hDevice, int count, void** hObjects);
    private unsafe delegate bool wglDXUnlockObjectsNV(IntPtr hDevice, int count, void** hObjects);

    public const uint WGL_ACCESS_READ_ONLY_NV = 0x0000;
    public const uint WGL_ACCESS_READ_WRITE_NV = 0x0001;
    public const uint WGL_ACCESS_WRITE_DISCARD_NV = 0x0002;

    #endregion
  }
}
