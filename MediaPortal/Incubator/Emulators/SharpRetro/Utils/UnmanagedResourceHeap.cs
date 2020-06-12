using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace SharpRetro.Utils
{
	public class UnmanagedResourceHeap : IDisposable
  {
    protected List<IntPtr> hGlobals = new List<IntPtr>();
    protected Dictionary<string, IntPtr> cache = new Dictionary<string, IntPtr>();

    public IntPtr StringToHGlobalAnsi(string str)
		{
      IntPtr ptr = Marshal.StringToHGlobalAnsi(str);
			hGlobals.Add(ptr);
			return ptr;
		}

    public IntPtr StringToHGlobalAnsiCached(string str)
    {
      IntPtr ptr;
      if (cache.TryGetValue(str, out ptr))
        return ptr;
      ptr = StringToHGlobalAnsi(str);
      cache[str] = ptr;
      return ptr;
    }

    ~UnmanagedResourceHeap()
    {
      Dispose();
    }

    public void Dispose()
		{
			foreach (var h in hGlobals)
				Marshal.FreeHGlobal(h);
			hGlobals.Clear();
      cache.Clear();
		}
	}
}
