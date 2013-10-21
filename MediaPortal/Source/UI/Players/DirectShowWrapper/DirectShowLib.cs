using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Security;
using DirectShow.Dvd;
using DirectShow.Helper;

namespace DirectShow
{
  public class DsROTEntry : IDisposable
  {
    [Flags]
    private enum ROTFlags
    {
      RegistrationKeepsAlive = 0x1,
      AllowAnyClient = 0x2
    }

    private int m_cookie = 0;

    #region APIs
    [DllImport("ole32.dll", ExactSpelling = true), SuppressUnmanagedCodeSecurity]
    private static extern int GetRunningObjectTable(int r, out IRunningObjectTable pprot);

    [DllImport("ole32.dll", CharSet = CharSet.Unicode, ExactSpelling = true), SuppressUnmanagedCodeSecurity]
    private static extern int CreateItemMoniker(string delim, string item, out IMoniker ppmk);
    #endregion

    public DsROTEntry(IFilterGraph graph)
    {
      int hr = 0;
      IRunningObjectTable rot = null;
      IMoniker mk = null;
      try
      {
        // First, get a pointer to the running object table
        hr = GetRunningObjectTable(0, out rot);
        new HRESULT(hr).Throw();

        // Build up the object to add to the table
        int id = System.Diagnostics.Process.GetCurrentProcess().Id;
        IntPtr iuPtr = Marshal.GetIUnknownForObject(graph);
        string s;
        try
        {
          s = iuPtr.ToString("x");
        }
        catch
        {
          s = "";
        }
        finally
        {
          Marshal.Release(iuPtr);
        }
        string item = string.Format("FilterGraph {0} pid {1}", s, id.ToString("x8"));
        hr = CreateItemMoniker("!", item, out mk);
        new HRESULT(hr).Throw();

        // Add the object to the table
        m_cookie = rot.Register((int)ROTFlags.RegistrationKeepsAlive, graph, mk);
      }
      finally
      {
        if (mk != null)
        {
          Marshal.ReleaseComObject(mk);
          mk = null;
        }
        if (rot != null)
        {
          Marshal.ReleaseComObject(rot);
          rot = null;
        }
      }
    }

    ~DsROTEntry()
    {
      Dispose();
    }

    public void Dispose()
    {
      if (m_cookie != 0)
      {
        GC.SuppressFinalize(this);
        IRunningObjectTable rot = null;

        // Get a pointer to the running object table
        int hr = GetRunningObjectTable(0, out rot);
        new HRESULT(hr).Throw();

        try
        {
          // Remove our entry
          rot.Revoke(m_cookie);
          m_cookie = 0;
        }
        finally
        {
          Marshal.ReleaseComObject(rot);
          rot = null;
        }
      }
    }
  }

  /// <summary>
  /// CLSID_DirectShowPluginControl
  /// </summary>
  [ComImport, Guid("8670C736-F614-427b-8ADA-BBADC587194B")]
  public class DirectShowPluginControl
  {
  }

  [ComImport, Guid("9B8C4620-2C1A-11d0-8493-00A02438AD48")]
  public class DVDNavigator
  {
  }

  [ComImport, System.Security.SuppressUnmanagedCodeSecurity,
    Guid("0e26a181-f40c-4635-8786-976284b52981"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
  public interface IAMPluginControl
  {
    [PreserveSig]
    int GetPreferredClsid(
        [In, MarshalAs(UnmanagedType.LPStruct)] Guid subType,
        out Guid clsid
        );

    [PreserveSig]
    int GetPreferredClsidByIndex(
        int index,
        out Guid subType,
        out Guid clsid
        );

    [PreserveSig]
    int SetPreferredClsid(
        [In, MarshalAs(UnmanagedType.LPStruct)] Guid subType,
        [In, MarshalAs(UnmanagedType.LPStruct)] DsGuid clsid
        );

    [PreserveSig]
    int IsDisabled(
        [In, MarshalAs(UnmanagedType.LPStruct)] Guid clsid
        );

    [PreserveSig]
    int GetDisabledByIndex(
        int index,
        out Guid clsid
        );

    [PreserveSig]
    int SetDisabled(
        [In, MarshalAs(UnmanagedType.LPStruct)] Guid clsid,
        bool disabled
        );

    [PreserveSig]
    int IsLegacyDisabled(
        [MarshalAs(UnmanagedType.LPWStr)] string dllName
        );
  }

  [ComImport, System.Security.SuppressUnmanagedCodeSecurity,
    Guid("6E8D4A21-310C-11d0-B79A-00AA003767A7"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
  public interface IAMLine21Decoder
  {
    [PreserveSig]
    int GetDecoderLevel([Out] out AMLine21CCLevel lpLevel);

    [PreserveSig]
    int GetCurrentService([Out] out AMLine21CCService lpService);

    [PreserveSig]
    int SetCurrentService([In] AMLine21CCService Service);

    [PreserveSig]
    int GetServiceState([Out] out AMLine21CCState lpState);

    [PreserveSig]
    int SetServiceState([In] AMLine21CCState State);

    [PreserveSig]
    int GetOutputFormat([Out] BitmapInfoHeader lpbmih);

    [PreserveSig]
    int SetOutputFormat([In] BitmapInfoHeader lpbmih);

    [PreserveSig]
    int GetBackgroundColor([Out] out int pdwPhysColor);

    [PreserveSig]
    int SetBackgroundColor([In] int dwPhysColor);

    [PreserveSig]
    int GetRedrawAlways([Out, MarshalAs(UnmanagedType.Bool)] out bool lpbOption);

    [PreserveSig]
    int SetRedrawAlways([In, MarshalAs(UnmanagedType.Bool)] bool bOption);

    [PreserveSig]
    int GetDrawBackgroundMode([Out] out AMLine21DrawBGMode lpMode);

    [PreserveSig]
    int SetDrawBackgroundMode([In] AMLine21DrawBGMode Mode);
  }

  /// <summary>
  /// From AM_LINE21_CCLEVEL
  /// </summary>
  public enum AMLine21CCLevel
  {
    TC2 = 0,
  }

  /// <summary>
  /// From AM_LINE21_CCSERVICE
  /// </summary>
  public enum AMLine21CCService
  {
    None = 0,
    Caption1,
    Caption2,
    Text1,
    Text2,
    XDS,
    DefChannel = 10,
    Invalid
  }

  /// <summary>
  /// From AM_LINE21_CCSTATE
  /// </summary>
  public enum AMLine21CCState
  {
    Off = 0,
    On
  }

  /// <summary>
  /// From AM_LINE21_DRAWBGMODE
  /// </summary>
  public enum AMLine21DrawBGMode
  {
    Opaque,
    Transparent
  }

  [Flags]
  public enum AMExtendedSeekingCapabilities
  {
    None = 0,
    CanSeek = 1,
    CanScan = 2,
    MarkerSeek = 4,
    ScanWithoutClock = 8,
    NoStandardRepaint = 16,
    Buffering = 32,
    SendsVideoFrameReady = 64
  }

  [ComImport, System.Security.SuppressUnmanagedCodeSecurity,
    Guid("FA2AA8F9-8B62-11D0-A520-000000000000"),
    InterfaceType(ComInterfaceType.InterfaceIsDual)]
  public interface IAMExtendedSeeking
  {
    [PreserveSig]
    int get_ExSeekCapabilities(out AMExtendedSeekingCapabilities pExCapabilities);

    [PreserveSig]
    int get_MarkerCount(out int pMarkerCount);

    [PreserveSig]
    int get_CurrentMarker(out int pCurrentMarker);

    [PreserveSig]
    int GetMarkerTime(int MarkerNum, out double pMarkerTime);

    [PreserveSig]
    int GetMarkerName(
        int MarkerNum,
        [MarshalAs(UnmanagedType.BStr)] out string pbstrMarkerName
        );

    [PreserveSig]
    int put_PlaybackSpeed(double Speed);

    [PreserveSig]
    int get_PlaybackSpeed(out double pSpeed);

  }

  public enum AspectRatioMode
  {
    Stretched,
    LetterBox,
    Crop,
    StretchedAsPrimary
  }

  [ComImport, Guid("FCC152B7-F372-11d0-8E00-00C04FD7C08B")]
  public class DvdGraphBuilder
  {
  }

  public static class DsUtils
  {
    /// <summary>
    ///  Free the nested structures and release any
    ///  COM objects within an AMMediaType struct.
    /// </summary>
    public static void FreeAMMediaType(AMMediaType mediaType)
    {
      if (mediaType != null)
      {
        if (mediaType.formatSize != 0)
        {
          Marshal.FreeCoTaskMem(mediaType.formatPtr);
          mediaType.formatSize = 0;
          mediaType.formatPtr = IntPtr.Zero;
        }
        if (mediaType.unkPtr != IntPtr.Zero)
        {
          Marshal.Release(mediaType.unkPtr);
          mediaType.unkPtr = IntPtr.Zero;
        }
      }
    }

    /// <summary>
    ///  Free the nested interfaces within a PinInfo struct.
    /// </summary>
    public static void FreePinInfo(PinInfo pinInfo)
    {
      if (pinInfo.filter != null)
      {
        Marshal.ReleaseComObject(pinInfo.filter);
        pinInfo.filter = null;
      }
    }
  }

    // This abstract class contains definitions for use in implementing a custom marshaler.
    //
    // MarshalManagedToNative() gets called before the COM method, and MarshalNativeToManaged() gets
    // called after.  This allows for allocating a correctly sized memory block for the COM call,
    // then to break up the memory block and build an object that c# can digest.

  abstract internal class DsMarshaler : ICustomMarshaler
  {
    #region Data Members
    // The cookie isn't currently being used.
    protected string m_cookie;

    // The managed object passed in to MarshalManagedToNative, and modified in MarshalNativeToManaged
    protected object m_obj;
    #endregion

    // The constructor.  This is called from GetInstance (below)
    public DsMarshaler(string cookie)
    {
      // If we get a cookie, save it.
      m_cookie = cookie;
    }

    // Called just before invoking the COM method.  The returned IntPtr is what goes on the stack
    // for the COM call.  The input arg is the parameter that was passed to the method.
    virtual public IntPtr MarshalManagedToNative(object managedObj)
    {
      // Save off the passed-in value.  Safe since we just checked the type.
      m_obj = managedObj;

      // Create an appropriately sized buffer, blank it, and send it to the marshaler to
      // make the COM call with.
      int iSize = GetNativeDataSize() + 3;
      IntPtr p = Marshal.AllocCoTaskMem(iSize);

      for (int x = 0; x < iSize / 4; x++)
      {
        Marshal.WriteInt32(p, x * 4, 0);
      }

      return p;
    }

    // Called just after invoking the COM method.  The IntPtr is the same one that just got returned
    // from MarshalManagedToNative.  The return value is unused.
    virtual public object MarshalNativeToManaged(IntPtr pNativeData)
    {
      return m_obj;
    }

    // Release the (now unused) buffer
    virtual public void CleanUpNativeData(IntPtr pNativeData)
    {
      if (pNativeData != IntPtr.Zero)
      {
        Marshal.FreeCoTaskMem(pNativeData);
      }
    }

    // Release the (now unused) managed object
    virtual public void CleanUpManagedData(object managedObj)
    {
      m_obj = null;
    }

    // This routine is (apparently) never called by the marshaler.  However it can be useful.
    abstract public int GetNativeDataSize();

    // GetInstance is called by the marshaler in preparation to doing custom marshaling.  The (optional)
    // cookie is the value specified in MarshalCookie="asdf", or "" is none is specified.

    // It is commented out in this abstract class, but MUST be implemented in derived classes
    //public static ICustomMarshaler GetInstance(string cookie)
  }

    // c# does not correctly create structures that contain ByValArrays of structures (or enums!).  Instead
    // of allocating enough room for the ByValArray of structures, it only reserves room for a ref,
    // even when decorated with ByValArray and SizeConst.  Needless to say, if DirectShow tries to
    // write to this too-short buffer, bad things will happen.
    //
    // To work around this for the DvdTitleAttributes structure, use this custom marshaler
    // by declaring the parameter DvdTitleAttributes as:
    //
    //    [In, Out, MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef=typeof(DTAMarshaler))]
    //    DvdTitleAttributes pTitle
    //
    // See DsMarshaler for more info on custom marshalers

  internal class DTAMarshaler : DsMarshaler
  {
    public DTAMarshaler(string cookie)
      : base(cookie)
    {
    }

    // Called just after invoking the COM method.  The IntPtr is the same one that just got returned
    // from MarshalManagedToNative.  The return value is unused.
    override public object MarshalNativeToManaged(IntPtr pNativeData)
    {
      DvdTitleAttributes dta = m_obj as DvdTitleAttributes;

      // Copy in the value, and advance the pointer
      dta.AppMode = (DvdTitleAppMode)Marshal.ReadInt32(pNativeData);
      pNativeData = (IntPtr)(pNativeData.ToInt64() + Marshal.SizeOf(typeof(int)));

      // Copy in the value, and advance the pointer
      dta.VideoAttributes = (DvdVideoAttributes)Marshal.PtrToStructure(pNativeData, typeof(DvdVideoAttributes));
      pNativeData = (IntPtr)(pNativeData.ToInt64() + Marshal.SizeOf(typeof(DvdVideoAttributes)));

      // Copy in the value, and advance the pointer
      dta.ulNumberOfAudioStreams = Marshal.ReadInt32(pNativeData);
      pNativeData = (IntPtr)(pNativeData.ToInt64() + Marshal.SizeOf(typeof(int)));

      // Allocate a large enough array to hold all the returned structs.
      dta.AudioAttributes = new DvdAudioAttributes[8];
      for (int x = 0; x < 8; x++)
      {
        // Copy in the value, and advance the pointer
        dta.AudioAttributes[x] = (DvdAudioAttributes)Marshal.PtrToStructure(pNativeData, typeof(DvdAudioAttributes));
        pNativeData = (IntPtr)(pNativeData.ToInt64() + Marshal.SizeOf(typeof(DvdAudioAttributes)));
      }

      // Allocate a large enough array to hold all the returned structs.
      dta.MultichannelAudioAttributes = new DvdMultichannelAudioAttributes[8];
      for (int x = 0; x < 8; x++)
      {
        // MultichannelAudioAttributes has nested ByValArrays.  They need to be individually copied.

        dta.MultichannelAudioAttributes[x].Info = new DvdMUAMixingInfo[8];

        for (int y = 0; y < 8; y++)
        {
          // Copy in the value, and advance the pointer
          dta.MultichannelAudioAttributes[x].Info[y] = (DvdMUAMixingInfo)Marshal.PtrToStructure(pNativeData, typeof(DvdMUAMixingInfo));
          pNativeData = (IntPtr)(pNativeData.ToInt64() + Marshal.SizeOf(typeof(DvdMUAMixingInfo)));
        }

        dta.MultichannelAudioAttributes[x].Coeff = new DvdMUACoeff[8];

        for (int y = 0; y < 8; y++)
        {
          // Copy in the value, and advance the pointer
          dta.MultichannelAudioAttributes[x].Coeff[y] = (DvdMUACoeff)Marshal.PtrToStructure(pNativeData, typeof(DvdMUACoeff));
          pNativeData = (IntPtr)(pNativeData.ToInt64() + Marshal.SizeOf(typeof(DvdMUACoeff)));
        }
      }

      // The DvdMultichannelAudioAttributes needs to be 16 byte aligned
      pNativeData = (IntPtr)(pNativeData.ToInt64() + 4);

      // Copy in the value, and advance the pointer
      dta.ulNumberOfSubpictureStreams = Marshal.ReadInt32(pNativeData);
      pNativeData = (IntPtr)(pNativeData.ToInt64() + Marshal.SizeOf(typeof(int)));

      // Allocate a large enough array to hold all the returned structs.
      dta.SubpictureAttributes = new DvdSubpictureAttributes[32];
      for (int x = 0; x < 32; x++)
      {
        // Copy in the value, and advance the pointer
        dta.SubpictureAttributes[x] = (DvdSubpictureAttributes)Marshal.PtrToStructure(pNativeData, typeof(DvdSubpictureAttributes));
        pNativeData = (IntPtr)(pNativeData.ToInt64() + Marshal.SizeOf(typeof(DvdSubpictureAttributes)));
      }

      // Note that 4 bytes (more alignment) are unused at the end

      return null;
    }

    // The number of bytes to marshal out
    override public int GetNativeDataSize()
    {
      // This is the actual size of a DvdTitleAttributes structure
      return 3208;
    }

    // This method is called by interop to create the custom marshaler.  The (optional)
    // cookie is the value specified in MarshalCookie="asdf", or "" is none is specified.
    public static ICustomMarshaler GetInstance(string cookie)
    {
      return new DTAMarshaler(cookie);
    }
  }

    // See DTAMarshaler for an explanation of the problem.  This class is for marshaling
    // a DvdKaraokeAttributes structure.
  internal class DKAMarshaler : DsMarshaler
  {
    // The constructor.  This is called from GetInstance (below)
    public DKAMarshaler(string cookie)
      : base(cookie)
    {
    }

    // Called just after invoking the COM method.  The IntPtr is the same one that just got returned
    // from MarshalManagedToNative.  The return value is unused.
    override public object MarshalNativeToManaged(IntPtr pNativeData)
    {
      DvdKaraokeAttributes dka = m_obj as DvdKaraokeAttributes;

      // Copy in the value, and advance the pointer
      dka.bVersion = (byte)Marshal.ReadByte(pNativeData);
      pNativeData = (IntPtr)(pNativeData.ToInt64() + Marshal.SizeOf(typeof(byte)));

      // DWORD Align
      pNativeData = (IntPtr)(pNativeData.ToInt64() + 3);

      // Copy in the value, and advance the pointer
      dka.fMasterOfCeremoniesInGuideVocal1 = Marshal.ReadInt32(pNativeData) != 0;
      pNativeData = (IntPtr)(pNativeData.ToInt64() + Marshal.SizeOf(typeof(bool)));

      // Copy in the value, and advance the pointer
      dka.fDuet = Marshal.ReadInt32(pNativeData) != 0;
      pNativeData = (IntPtr)(pNativeData.ToInt64() + Marshal.SizeOf(typeof(bool)));

      // Copy in the value, and advance the pointer
      dka.ChannelAssignment = (DvdKaraokeAssignment)Marshal.ReadInt32(pNativeData);
      pNativeData = (IntPtr)(pNativeData.ToInt64() + Marshal.SizeOf(DvdKaraokeAssignment.GetUnderlyingType(typeof(DvdKaraokeAssignment))));

      // Allocate a large enough array to hold all the returned structs.
      dka.wChannelContents = new DvdKaraokeContents[8];
      for (int x = 0; x < 8; x++)
      {
        // Copy in the value, and advance the pointer
        dka.wChannelContents[x] = (DvdKaraokeContents)Marshal.ReadInt16(pNativeData);
        pNativeData = (IntPtr)(pNativeData.ToInt64() + Marshal.SizeOf(DvdKaraokeContents.GetUnderlyingType(typeof(DvdKaraokeContents))));
      }

      return null;
    }

    // The number of bytes to marshal out
    override public int GetNativeDataSize()
    {
      // This is the actual size of a DvdKaraokeAttributes structure.
      return 32;
    }

    // This method is called by interop to create the custom marshaler.  The (optional)
    // cookie is the value specified in MarshalCookie="asdf", or "" is none is specified.
    public static ICustomMarshaler GetInstance(string cookie)
    {
      return new DKAMarshaler(cookie);
    }
  }
}
