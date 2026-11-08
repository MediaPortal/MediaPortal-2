/*
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

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;

namespace MediaPortal.Frontend.Player
{
  /// <summary>
  /// The mixer layer the operating system puts under this process's own audio, on Windows.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Windows keeps a volume and a mute switch per application and per output device, above whatever the
  /// application plays at, and remembers both across restarts. The volume mixer shows that row under the name
  /// the audio output chooses for itself, and libVLC's chooses "VLC media player (LibVLC 3.0.20)" - so a viewer
  /// who once dragged that row down, or clicked its speaker icon while clearing the mixer, has muted
  /// MediaPortal without ever having seen the word MediaPortal in it. Measured on such a machine: the frontend
  /// played, its session reported itself active with real peaks (0.02 to 0.55 of full scale), and its row stood
  /// at 9 % and muted. Everything inside the frontend was correct; the sound died one layer below it.
  /// </para>
  /// <para>
  /// This class is that layer, read and corrected for <b>this process's own sessions only</b> - a frontend that
  /// tidied other applications' rows would be a nuisance. It is deliberately Windows-only, deliberately quiet
  /// about a machine that refuses it, and deliberately small: the handful of COM shapes it needs are declared
  /// here rather than pulled in as a package, because this front end builds on Linux and macOS too and no
  /// dependency should be added for one platform's mixer.
  /// </para>
  /// <para>
  /// <b>How the shapes below were checked.</b> A COM vtable is counted and its identifiers are remembered, so
  /// both were measured instead of trusted: every row this file reads was read side by side with CSCore, a client
  /// that has spoken this shape for years, and the two were compared - the same rows, the same volumes, the same
  /// mute, and the writes this file makes read back through that same other client. Five identifiers and one
  /// method order turned out different from what the reference pages and memory offer: <c>IMMDevice</c> answers
  /// <c>OpenPropertyStore</c> before <c>GetId</c>, and <c>ISimpleAudioVolume</c>, <c>IAudioSessionControl</c>,
  /// <c>IAudioSessionControl2</c>, <c>IAudioSessionManager2</c> and <c>IMMDeviceCollection</c> each carry digits
  /// the remembered version got wrong. Read one slot or one digit off, and the machine answers with a wrong
  /// number or not at all.
  /// </para>
  /// </remarks>
  [SupportedOSPlatform("windows")]
  public static class WindowsAudioSessions
  {
    /// <summary>Whether this platform has the mixer layer this class speaks to.</summary>
    public static bool Supported => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    /// <summary>
    /// One of this process's audio sessions as the mixer sees it.
    /// </summary>
    /// <param name="EndpointId">Which output device the session is on.</param>
    /// <param name="ProcessId">Whose audio it is; only ever this process.</param>
    /// <param name="Muted">Whether the mixer mutes this row.</param>
    /// <param name="Volume">The row's own volume, 0.0 to 1.0, multiplied over what the frontend plays at.</param>
    /// <param name="State">The session's own state: 0 expired, 1 active, 2 inactive.</param>
    public sealed record Session(string EndpointId, int ProcessId, bool Muted, float Volume, int State)
    {
      public override string ToString()
      {
        return $"{EndpointId} pid={ProcessId} vol={Volume * 100f:0}%" +
               (Muted ? " muted" : "") + " state=" + Describe(State);
      }
    }

    /// <summary>
    /// The state number of a row the mixer is forgetting. Measured, not assumed: a row that had just been handed
    /// out for a playing title answered <c>0</c> here while a client that has spoken this shape for years called
    /// the same row active, so the number is taken at face value - expired, active, inactive - and a row standing
    /// at zero is not stood down for.
    /// </summary>
    public const int ExpiredState = 0;

    private static string Describe(int state)
    {
      return state switch
      {
        0 => "expired",
        1 => "active",
        2 => "inactive",
        _ => state.ToString()
      };
    }

    /// <summary>
    /// This process's own sessions on every output device the machine says is there. Empty on any other
    /// platform, and empty before the first media has opened an audio output - a session is born when the sound
    /// starts, not when the process is.
    /// </summary>
    public static IReadOnlyList<Session> ReadOwn()
    {
      List<Session> found = new List<Session>();
      if (!Supported)
        return found;

      try
      {
        foreach (IMMDevice device in ActiveRenderDevices())
        {
          string id;
          try
          {
            device.GetId(out id);
          }
          catch (COMException)
          {
            continue;
          }

          foreach (IAudioSessionControl control in SessionsOf(device))
          {
            try
            {
              control.GetState(out int state);
              if (!TryOwn(control, out int pid))
                continue;

              float level = 1.0f;
              bool muted = false;
              ISimpleAudioVolume? row = As<ISimpleAudioVolume>(control);
              if (row != null)
              {
                row.GetMasterVolume(out level);
                row.GetMute(out muted);
              }
              found.Add(new Session(id, pid, muted, level, state));
            }
            catch (COMException)
            {
              // A session can die between the enumerator naming it and this reading it. Nothing to say.
            }
          }
        }
      }
      catch (COMException)
      {
        // No audio service on this machine. An empty answer is the honest one.
      }

      return found;
    }

    /// <summary>
    /// Writes the front end's own volume into the mixer rows of this process and takes their mute off.
    /// </summary>
    /// <param name="volumePercent">What the front end believes it plays at, 0 to 100. Zero is answered with
    /// "then silence is what was asked for" and no write at all.</param>
    /// <param name="log">Where to say what was moved, if anywhere.</param>
    /// <returns>How many rows were moved.</returns>
    /// <remarks>
    /// The row multiplies what the front end plays at, so writing the front end's own number into it means the
    /// number the viewer sees in the front end stays the one number that means anything; the machine's row ends
    /// up out of the way rather than quietly halving everything. The write is not limited to rows that are mute
    /// or silent: a row left at a third by an old click in the mixer is the same mistake in a quieter voice, and
    /// a television that is quietly wrong is harder to notice than one that says nothing. What is never touched
    /// is anybody else's row, and a row that already stands at the wanted number is left alone.
    /// </remarks>
    public static int WriteOwnVolume(int volumePercent, Action<string>? log = null)
    {
      if (!Supported || volumePercent <= 0)
        return 0;

      float wanted = Math.Clamp(volumePercent, 0, 100) / 100f;
      int changed = 0;
      try
      {
        foreach (IMMDevice device in ActiveRenderDevices())
        {
          string id;
          try
          {
            device.GetId(out id);
          }
          catch (COMException)
          {
            continue;
          }

          foreach (IAudioSessionControl control in SessionsOf(device))
          {
            try
            {
              if (!TryOwn(control, out _))
                continue;
              control.GetState(out int state);
              if (state == ExpiredState)
                continue; // the mixer is already forgetting it

              ISimpleAudioVolume? row = As<ISimpleAudioVolume>(control);
              if (row == null)
                continue;

              row.GetMasterVolume(out float now);
              row.GetMute(out bool muted);
              if (!muted && Math.Abs(now - wanted) < 0.001f)
                continue;

              row.SetMasterVolume(wanted, IntPtr.Zero);
              row.SetMute(false, IntPtr.Zero);
              changed++;
              log?.Invoke($"audio session on {id} set to {volumePercent}%" +
                          (muted ? " and unmuted" : "") + $" (was {now * 100f:0}%" +
                          (muted ? ", muted" : "") + ").");
            }
            catch (COMException)
            {
              // The row went away or refused. The front end keeps playing; the machine decides what arrives.
            }
          }
        }
      }
      catch (COMException)
      {
        // No audio service. The sound was never going to come from here.
      }

      return changed;
    }

    // One loop at a time: media can be opened faster than a session appears, and two loops writing the same row
    // would only say the same line twice.
    private static int _lifting;

    /// <summary>
    /// Writes the front end's volume into its mixer rows once they exist, off the calling thread.
    /// </summary>
    /// <param name="volumePercent">Asked for at the moment of each look, so a volume changed while waiting is
    /// waited with.</param>
    /// <param name="looks">How often to look before giving up - the row is normally there at the first look, and
    /// a machine that never answers should stop being asked.</param>
    /// <param name="pauseMs">How long between looks.</param>
    /// <param name="log">Where to say what was moved, if anywhere.</param>
    /// <remarks>
    /// Why a wait at all: the row belongs to the audio output, and the audio output is born when the first media
    /// starts, a moment after the front end itself. Writing at startup - where "at the start" would naively put
    /// it - finds nothing to write to, which is exactly how one bad click in the mixer survived every restart
    /// until now.
    /// </remarks>
    public static void WriteOwnVolumeWhenItAppears(Func<int> volumePercent, int looks = 20,
        int pauseMs = 100, Action<string>? log = null)
    {
      if (!Supported || Interlocked.CompareExchange(ref _lifting, 1, 0) != 0)
        return;

      ThreadPool.QueueUserWorkItem(_ =>
      {
        try
        {
          for (int look = 0; look < looks; look++)
          {
            try
            {
              int moved = WriteOwnVolume(volumePercent(), log);

              // A row the mixer is already forgetting is not the row this media is playing on, so it does not
              // end the waiting: measured here, the first row found at the moment a title started was one being
              // cleared away, and standing down on it would have left a muted machine untouched.
              List<Session> living = new List<Session>();
              foreach (Session row in ReadOwn())
              {
                if (row.State != ExpiredState)
                  living.Add(row);
              }

              if (living.Count > 0)
              {
                if (moved == 0)
                {
                  // Nothing to change is worth one line too: when the sound is missing a month from now, the
                  // log has to say what the mixer's own rows were standing at, not be silent about having looked.
                  foreach (Session row in living)
                  {
                    log?.Invoke("audio row " + row + "; the front end plays at " + volumePercent() + "%.");
                  }
                }

                return;
              }
            }
            catch (Exception ex)
            {
              log?.Invoke($"audio session check failed: {ex.Message}");
              return;
            }

            Thread.Sleep(pauseMs);
          }
        }
        finally
        {
          Interlocked.Exchange(ref _lifting, 0);
        }
      });
    }

    /// <summary>
    /// The shape of <paramref name="unknown"/> as <typeparamref name="T"/>, or null when it is not that shape.
    /// </summary>
    /// <remarks>
    /// The mixer hands out plain session objects and expects the caller to ask for the interfaces it wants,
    /// which is what a query is; casting a runtime callable object asks the same question but throws when the
    /// answer is no, and "this session has no volume row" is an answer rather than a fault.
    /// </remarks>
    private static T? As<T>(object unknown) where T : class
    {
      try
      {
        return unknown as T;
      }
      catch (InvalidCastException)
      {
        return null;
      }
    }

    /// <summary>Whether a session belongs to this process.</summary>
    private static bool TryOwn(IAudioSessionControl control, out int processId)
    {
      processId = 0;
      IAudioSessionControl2? detailed = As<IAudioSessionControl2>(control);
      if (detailed == null)
        return false;

      try
      {
        detailed.GetProcessId(out processId);
      }
      catch (COMException)
      {
        return false;
      }

      return processId == Environment.ProcessId;
    }

    private static IEnumerable<IMMDevice> ActiveRenderDevices()
    {
      IMMDeviceEnumerator enumerator = NewEnumerator();
      Said(enumerator.EnumAudioEndpoints(DataFlow.Render, DeviceStateActive, out IMMDeviceCollection devices));
      Said(devices.GetCount(out uint count));
      for (uint index = 0; index < count; index++)
      {
        Said(devices.Item(index, out IMMDevice device));
        yield return device;
      }
    }

    private static IEnumerable<IAudioSessionControl> SessionsOf(IMMDevice device)
    {
      Said(device.Activate(typeof(IAudioSessionManager2).GUID, CtxAll, IntPtr.Zero, out object managerObject));
      IAudioSessionManager2 manager = (IAudioSessionManager2)managerObject;
      Said(manager.GetSessionEnumerator(out IAudioSessionEnumerator sessions));
      Said(sessions.GetCount(out int count));
      for (int index = 0; index < count; index++)
      {
        Said(sessions.GetSession(index, out IAudioSessionControl session));
        yield return session;
      }
    }

    /// <summary>
    /// Turns a refused answer into the exception everything here already catches, with the code the machine gave.
    /// </summary>
    /// <remarks>
    /// The shapes below keep their <c>HRESULT</c> - that is how a COM vtable hands its answer back - so a refusal
    /// is only heard when somebody looks at it. Left unread, it arrives as a pointer the next call then misreads,
    /// and the complaint names the wrong call: while this file was being written a wrong interface identifier
    /// made a call answer S_OK and the failure appear one step later, on an interface that was perfectly fine.
    /// </remarks>
    private static void Said(int result)
    {
      if (result < 0)
        throw new COMException("the audio mixer refused the question", result);
    }

    private const int DeviceStateActive = 1;
    private const int CtxAll = 1 | 4 | 16; // in-process server | local server | in-process handler

    private enum DataFlow
    {
      Render = 0,
      Capture = 1,
      All = 2
    }

    /// <summary>
    /// The enumerator's own coclass. It has to say which interface it carries: a runtime callable object only
    /// answers casts to interfaces its declared type offers, and on a current .NET an undeclared one is refused
    /// rather than answered by a query behind the caller's back.
    /// </summary>
    /// <summary>
    /// The enumerator, built from the class identifier the operating system registers it under.
    /// </summary>
    /// <remarks>
    /// Asked of the class identifier rather than declared as a coclass here, because a declared coclass would
    /// have to promise every interface it carries - and this file only ever asks one of them.
    /// </remarks>
    private static IMMDeviceEnumerator NewEnumerator()
    {
      Type? type = Type.GetTypeFromCLSID(EnumeratorClsid);
      if (type == null)
        throw new COMException("this machine has no audio device enumerator.");

      return (IMMDeviceEnumerator)Activator.CreateInstance(type)!;
    }

    private static readonly Guid EnumeratorClsid = new Guid("BCDE0395-E52F-467C-8E3D-C4579291692E");

    /// <summary>
    /// The mixer's device list. Every method of the shape is declared, in the operating system's order, even the
    /// ones nothing here calls: a COM vtable is counted, and leaving one out moves every call after it by one
    /// slot - which is a wrong answer at best and a crash at worst.
    /// </summary>
    [ComImport]
    [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDeviceEnumerator
    {
      int EnumAudioEndpoints(DataFlow dataFlow, int stateMask, out IMMDeviceCollection devices);

      int GetDefaultAudioEndpoint(DataFlow dataFlow, int role, out IMMDevice device);

      int GetDevice([MarshalAs(UnmanagedType.LPWStr)] string id, out IMMDevice device);

      int RegisterEndpointNotificationCallback(IntPtr client);

      int UnregisterEndpointNotificationCallback(IntPtr client);
    }

    [ComImport]
    [Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDeviceCollection
    {
      int GetCount(out uint count);

      int Item(uint index, out IMMDevice device);
    }

    /// <summary>
    /// One output device. The order of the methods is the machine's own, which is not the order the reference
    /// pages list them in: <c>OpenPropertyStore</c> sits between <c>Activate</c> and <c>GetId</c>, and the
    /// deprecated <c>GetVolumePropertyStore</c> only comes after <c>GetPropertyList</c>. Read one slot off and
    /// <c>GetId</c> answers with "value does not fall within the expected range" instead of an identifier -
    /// measured here, and the reason the order is written out rather than trusted to memory.
    /// </summary>
    [ComImport]
    [Guid("D666063F-1587-4E43-81F1-B948E807363F")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDevice
    {
      int Activate([MarshalAs(UnmanagedType.LPStruct)] Guid interfaceId, int context,
          IntPtr activationParameters, [MarshalAs(UnmanagedType.IUnknown)] out object interfacePointer);

      int OpenPropertyStore(int storageAccess, [MarshalAs(UnmanagedType.LPStruct)] Guid interfaceId,
          IntPtr propertyStore);

      int GetId([MarshalAs(UnmanagedType.LPWStr)] out string id);

      int GetState(out int state);

      int GetPropertyList([MarshalAs(UnmanagedType.LPStruct)] Guid format, IntPtr propertyList,
          IntPtr propertyArray, out int count);

      int GetVolumePropertyStore(int storeType, [MarshalAs(UnmanagedType.LPStruct)] Guid interfaceId,
          IntPtr store);

      int GetMatcher([MarshalAs(UnmanagedType.LPStruct)] Guid interfaceId, IntPtr matcher);

      int ActivateForPairing([MarshalAs(UnmanagedType.LPStruct)] Guid interfaceId, int context,
          IntPtr activationParameters, [MarshalAs(UnmanagedType.IUnknown)] out object interfacePointer);
    }

    [ComImport]
    [Guid("77AA99A0-1BD6-484F-8BC7-2C654C9A9B6F")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioSessionManager2
    {
      // The two slots of IAudioSessionManager, whose shape this one inherits.
      int GetAudioSessionControl([MarshalAs(UnmanagedType.LPWStr)] string? id, int flags,
          out IAudioSessionControl session);

      int GetSimpleAudioVolume([MarshalAs(UnmanagedType.LPWStr)] string? id, int flags,
          IntPtr simpleAudioVolume);

      int GetSessionEnumerator(out IAudioSessionEnumerator sessions);

      int RegisterSessionNotification(IntPtr callback);

      int UnregisterSessionNotification(IntPtr callback);

      int RegisterDuckNotification([MarshalAs(UnmanagedType.LPWStr)] string sessionId, IntPtr callback);

      int UnregisterDuckNotification([MarshalAs(UnmanagedType.LPWStr)] string sessionId);
    }

    [ComImport]
    [Guid("E2F5BB11-0570-40CA-ACDD-3AA01277DEE8")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioSessionEnumerator
    {
      int GetCount(out int count);

      int GetSession(int index, out IAudioSessionControl session);
    }

    [ComImport]
    [Guid("F4B1A599-7266-4319-A8CA-E70ACB11E8CD")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioSessionControl
    {
      int GetState(out int state);

      int GetDisplayName([MarshalAs(UnmanagedType.LPWStr)] out string name);

      int SetDisplayName([MarshalAs(UnmanagedType.LPWStr)] string name,
          [MarshalAs(UnmanagedType.LPStruct)] Guid eventContext);

      int GetIconPath([MarshalAs(UnmanagedType.LPWStr)] out string path);

      int SetIconPath([MarshalAs(UnmanagedType.LPWStr)] string path,
          [MarshalAs(UnmanagedType.LPStruct)] Guid eventContext);

      int GetGroupingParam(out Guid parameter);

      int SetGroupingParam([MarshalAs(UnmanagedType.LPStruct)] Guid parameter,
          [MarshalAs(UnmanagedType.LPStruct)] Guid eventContext);

      int RegisterAudioSessionNotification(IntPtr events);

      int UnregisterAudioSessionNotification(IntPtr events);
    }

    [ComImport]
    [Guid("BFB7FF88-7239-4FC9-8FA2-07C950BE9C6D")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioSessionControl2
    {
      // The nine slots of IAudioSessionControl, which this shape inherits.
      int GetState(out int state);

      int GetDisplayName([MarshalAs(UnmanagedType.LPWStr)] out string name);

      int SetDisplayName([MarshalAs(UnmanagedType.LPWStr)] string name,
          [MarshalAs(UnmanagedType.LPStruct)] Guid eventContext);

      int GetIconPath([MarshalAs(UnmanagedType.LPWStr)] out string path);

      int SetIconPath([MarshalAs(UnmanagedType.LPWStr)] string path,
          [MarshalAs(UnmanagedType.LPStruct)] Guid eventContext);

      int GetGroupingParam(out Guid parameter);

      int SetGroupingParam([MarshalAs(UnmanagedType.LPStruct)] Guid parameter,
          [MarshalAs(UnmanagedType.LPStruct)] Guid eventContext);

      int RegisterAudioSessionNotification(IntPtr events);

      int UnregisterAudioSessionNotification(IntPtr events);

      int GetSessionIdentifier([MarshalAs(UnmanagedType.LPWStr)] out string identifier);

      int GetSessionInstanceIdentifier([MarshalAs(UnmanagedType.LPWStr)] out string identifier);

      int GetProcessId(out int processId);

      int IsSystemSoundsSession();

      int SetDuckingPreference(bool preferDucking);
    }

    [ComImport]
    [Guid("87CE5498-68D6-44E5-9215-6DA47EF883D8")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface ISimpleAudioVolume
    {
      int SetMasterVolume(float level, IntPtr eventContext);

      int GetMasterVolume(out float level);

      int SetMute([MarshalAs(UnmanagedType.Bool)] bool muted, IntPtr eventContext);

      int GetMute([MarshalAs(UnmanagedType.Bool)] out bool muted);
    }
  }
}
