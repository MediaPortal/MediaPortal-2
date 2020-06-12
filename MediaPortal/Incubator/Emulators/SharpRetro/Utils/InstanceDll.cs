using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace SharpRetro.Utils
{
  public class InstanceDll : IDisposable
  {
    [Flags]
    enum LoadLibraryFlags : uint
    {
      DONT_RESOLVE_DLL_REFERENCES = 0x00000001,
      LOAD_IGNORE_CODE_AUTHZ_LEVEL = 0x00000010,
      LOAD_LIBRARY_AS_DATAFILE = 0x00000002,
      LOAD_LIBRARY_AS_DATAFILE_EXCLUSIVE = 0x00000040,
      LOAD_LIBRARY_AS_IMAGE_RESOURCE = 0x00000020,
      LOAD_WITH_ALTERED_SEARCH_PATH = 0x00000008
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern IntPtr LoadLibrary(string dllToLoad);
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hReservedNull, LoadLibraryFlags dwFlags);
    [DllImport("kernel32.dll")]
    static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);
    [DllImport("kernel32.dll")]
    static extern bool FreeLibrary(IntPtr hModule);

    IntPtr _hModule;

    public InstanceDll(string dllPath)
    {
      //try to locate dlls in the current directory (for libretro cores)
      //this isnt foolproof but its a little better than nothing
      string path = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Process);
      try
      {
        string assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        string dllDirectory = Path.GetDirectoryName(dllPath);
        string alteredPath = string.Format("{0};{1};{2}", assemblyDirectory, dllDirectory, path);
        Environment.SetEnvironmentVariable("PATH", alteredPath, EnvironmentVariableTarget.Process);
        _hModule = LoadLibrary(dllPath);
      }
      finally
      {
        Environment.SetEnvironmentVariable("PATH", path, EnvironmentVariableTarget.Process);
      }
    }

    public bool IsLoaded
    {
      get { return _hModule != IntPtr.Zero; }
    }

    public IntPtr GetProcAddress(string procName)
    {
      return GetProcAddress(_hModule, procName);
    }

    public void Dispose()
    {
      if (_hModule != IntPtr.Zero)
      {
        FreeLibrary(_hModule);
        _hModule = IntPtr.Zero;
      }
    }
  }
}