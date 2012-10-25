#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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

namespace MediaPortal.UI.Players.Video.Tools
{
  [ComVisible(false)]
  [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("00000001-0000-0000-C000-000000000046")]
  internal interface IClassFactory
  {
    void CreateInstance([MarshalAs(UnmanagedType.Interface)] object pUnkOuter, ref Guid refiid, [MarshalAs(UnmanagedType.Interface)] out object ppunk);
    void LockServer(bool fLock);
  }

  /// <summary>
  /// Utility class to get a Class Factory for a certain Class ID 
  /// by loading the dll that implements that class
  /// </summary>
  internal static class ComHelper
  {
    // DllGetClassObject fuction pointer signature
    private delegate int DllGetClassObject(ref Guid classId, ref Guid interfaceId, [Out, MarshalAs(UnmanagedType.Interface)] out object ppunk);

    /// <summary>
    /// Holds a list of dll handles and unloads the dlls 
    /// in the destructor
    /// </summary>
    private class DllList
    {
      private readonly List<IntPtr> _handleList = new List<IntPtr>();
      public void AddDllHandle(IntPtr dllHandle)
      {
        lock (_handleList)
        {
          _handleList.Add(dllHandle);
        }
      }

      ~DllList()
      {
        foreach (IntPtr dllHandle in _handleList)
        {
          try
          {
            Common.Utils.NativeMethods.FreeLibrary(dllHandle);
          }
          catch { }
        }
      }
    }

    static readonly DllList DLL_LIST = new DllList();

    /// <summary>
    /// Gets a class factory for a specific COM Class ID. 
    /// </summary>
    /// <param name="dllName">The dll where the COM class is implemented</param>
    /// <param name="filterPersistClass">The requested Class ID</param>
    /// <returns>IClassFactory instance used to create instances of that class</returns>
    internal static IClassFactory GetClassFactory(string dllName, Guid filterPersistClass)
    {
      // Load the class factory from the dll.
      IClassFactory classFactory = GetClassFactoryFromDll(dllName, filterPersistClass);
      return classFactory;
    }

    private static IClassFactory GetClassFactoryFromDll(string dllName, Guid filterPersistClass)
    {
      // Load the dll.
      IntPtr dllHandle = Common.Utils.NativeMethods.LoadLibrary(dllName);
      if (dllHandle == IntPtr.Zero)
        return null;

      // Keep a reference to the dll until the process\AppDomain dies.
      DLL_LIST.AddDllHandle(dllHandle);

      //Get a pointer to the DllGetClassObject function
      IntPtr dllGetClassObjectPtr = Common.Utils.NativeMethods.GetProcAddress(dllHandle, "DllGetClassObject");
      if (dllGetClassObjectPtr == IntPtr.Zero)
        return null;

      // Convert the function pointer to a .net delegate.
      DllGetClassObject dllGetClassObject = (DllGetClassObject) Marshal.GetDelegateForFunctionPointer(dllGetClassObjectPtr, typeof(DllGetClassObject));

      // Call the DllGetClassObject to retreive a class factory for out Filter class.
      Guid baseFilterGuid = filterPersistClass;
      Guid classFactoryGuid = typeof(IClassFactory).GUID;
      Object unk;
      if (dllGetClassObject(ref baseFilterGuid, ref classFactoryGuid, out unk) != 0)
        return null;

      return (unk as IClassFactory);
    }
  }
}
