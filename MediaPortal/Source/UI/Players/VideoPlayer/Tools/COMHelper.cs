#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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

//
// This class is based on the source of http://www.codeproject.com/Articles/13391/Using-IFilter-in-C. 
// Many thanks to the original author!
//

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using DirectShow;
#if DEBUG
using MediaPortal.Common;
using MediaPortal.Common.Logging;
#endif

namespace MediaPortal.UI.Players.Video.Tools
{
  [ComVisible(false)]
  [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("00000001-0000-0000-C000-000000000046")]
  public interface IClassFactory
  {
    void CreateInstance([MarshalAs(UnmanagedType.Interface)] object pUnkOuter, ref Guid refiid, [MarshalAs(UnmanagedType.Interface)] out object ppunk);
    void LockServer(bool fLock);
  }

  /// <summary>
  /// Utility class to load a DirectShow filter from file. The filter is not needed to be registered. The caller must <see cref="Dispose"/> the instance
  /// when finished to make sure the used library will be properly unloaded.
  /// </summary>
  public class FilterFileWrapper: IDisposable
  {
    private IntPtr _dllHandle;
    private readonly string _dllName;
    private Guid _classId;

    private IBaseFilter _filter;

    // DllGetClassObject fuction pointer signature
    private delegate int DllGetClassObject(ref Guid classId, ref Guid interfaceId, [Out, MarshalAs(UnmanagedType.Interface)] out object ppunk);

    /// <param name="dllName">The dll where the COM class is implemented.</param>
    /// <param name="classId">The requested Class ID.</param>
    public FilterFileWrapper(string dllName, Guid classId)
    {
      _dllName = dllName;
      _classId = classId;
    }

    /// <summary>
    /// Gets a class factory for a specific COM Class ID. 
    /// </summary>
    /// <returns>IClassFactory instance used to create instances of that class.</returns>
    public IBaseFilter GetFilter()
    {
      if (_filter != null)
        return _filter;

      // Load the class factory from the dll.
      // By specifying the flags we allow to search for dependencies in the same folder as the file to be loaded 
      // as well as default dirs like System32 and the Application folder.
      List<uint> loadFlagsToTry = new List<uint>
      {
        Utilities.SystemAPI.NativeMethods.LOAD_LIBRARY_SEARCH_DLL_LOAD_DIR | Utilities.SystemAPI.NativeMethods.LOAD_LIBRARY_SEARCH_DEFAULT_DIRS,
        0x00000010 /*LOAD_IGNORE_CODE_AUTHZ_LEVEL*/,
        0 /*NONE*/
      };
      _dllHandle = IntPtr.Zero;
      foreach (var flags in loadFlagsToTry)
      {
        _dllHandle = Utilities.SystemAPI.NativeMethods.LoadLibraryEx(_dllName, IntPtr.Zero, flags);
        if (_dllHandle != IntPtr.Zero)
          break;
#if DEBUG
        int error = Marshal.GetLastWin32Error();
        ServiceRegistration.Get<ILogger>().Warn("Failed to load library {0}, Flags: {1:X}, HR: {2:X}", _dllName, flags, error);
#endif
      }

      if (_dllHandle == IntPtr.Zero)
        return null;

      //Get a pointer to the DllGetClassObject function
      IntPtr dllGetClassObjectPtr = Utilities.SystemAPI.NativeMethods.GetProcAddress(_dllHandle, "DllGetClassObject");
      if (dllGetClassObjectPtr == IntPtr.Zero)
      {
#if DEBUG
        int error = Marshal.GetLastWin32Error();
        ServiceRegistration.Get<ILogger>().Warn("Failed to GetProcAddress of DllGetClassObject in library {0} HR: {1:X}", _dllName, error);
        return null;
#endif
      }

      // Convert the function pointer to a .net delegate.
      DllGetClassObject dllGetClassObject = (DllGetClassObject)Marshal.GetDelegateForFunctionPointer(dllGetClassObjectPtr, typeof(DllGetClassObject));

      // Call the DllGetClassObject to retreive a class factory for out Filter class.
      Guid classFactoryGuid = typeof(IClassFactory).GUID;
      object unk;
      if (dllGetClassObject(ref _classId, ref classFactoryGuid, out unk) != 0)
      {
#if DEBUG
        int error = Marshal.GetLastWin32Error();
        ServiceRegistration.Get<ILogger>().Warn("Failed to get class factory in library {0} HR: {1:X}", _dllName, error);
#endif
        return null;
      }

      var classFactory = (unk as IClassFactory);
      if (classFactory == null)
        return null;

      // And create an IFilter instance using that class factory
      Guid baseFilterGuid = typeof(IBaseFilter).GUID;
      object obj;
      classFactory.CreateInstance(null, ref baseFilterGuid, out obj);

      Marshal.ReleaseComObject(classFactory);

      _filter = obj as IBaseFilter;
      return _filter;
    }

    public void Dispose()
    {
      FilterGraphTools.TryRelease(ref _filter, true);
      Utilities.SystemAPI.NativeMethods.FreeLibrary(_dllHandle);
    }
  }
}
