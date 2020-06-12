using SharpRetro.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SharpRetro.LibRetro
{
  public class LibRetroBase : IDisposable
  {
    #region entry points
    // these are all hooked up by reflection on dll load
    protected epretro_set_environment retro_set_environment;
    protected epretro_set_video_refresh retro_set_video_refresh;
    protected epretro_set_audio_sample retro_set_audio_sample;
    protected epretro_set_audio_sample_batch retro_set_audio_sample_batch;
    protected epretro_set_input_poll retro_set_input_poll;
    protected epretro_set_input_state retro_set_input_state;
    protected epretro_init retro_init;
    protected epretro_deinit retro_deinit;
    protected epretro_api_version retro_api_version;
    protected epretro_get_system_info retro_get_system_info;
    protected epretro_get_system_av_info retro_get_system_av_info;
    protected epretro_set_controller_port_device retro_set_controller_port_device;
    protected epretro_reset retro_reset;
    protected epretro_run retro_run;
    protected epretro_serialize_size retro_serialize_size;
    protected epretro_serialize retro_serialize;
    protected epretro_unserialize retro_unserialize;
    protected epretro_cheat_reset retro_cheat_reset;
    protected epretro_cheat_set retro_cheat_set;
    protected epretro_load_game retro_load_game;
    protected epretro_load_game_special retro_load_game_special;
    protected epretro_unload_game retro_unload_game;
    protected epretro_get_region retro_get_region;
    protected epretro_get_memory_data retro_get_memory_data;
    protected epretro_get_memory_size retro_get_memory_size;
    #endregion

    #region LibRetro Callbacks
    retro_environment_t retro_environment_cb;
    retro_video_refresh_t retro_video_refresh_cb;
    retro_audio_sample_t retro_audio_sample_cb;
    retro_audio_sample_batch_t retro_audio_sample_batch_cb;
    retro_input_poll_t retro_input_poll_cb;
    retro_input_state_t retro_input_state_cb;
    #endregion

    #region Members
    public const int RETRO_API_VERSION = 1;
    protected InstanceDll _dll;
    protected string _corePath;
    #endregion

    #region Ctor
    public LibRetroBase(string corePath)
    {
      _corePath = corePath;
    }

    public virtual void Init()
    {
      InitEntryPoints();
      InitCallbacks();
    }
    #endregion

    #region Entry Point Connection
    void InitEntryPoints()
    {
      _dll = new InstanceDll(_corePath);
      if (!_dll.IsLoaded)
        throw new Exception("Unable to load LibRetro core. LoadLibrary failed.");

      foreach (var field in GetAllEntryPoints())
      {
        IntPtr entry = _dll.GetProcAddress(field.Name);
        if (entry != IntPtr.Zero)
          field.SetValue(this, Marshal.GetDelegateForFunctionPointer(entry, field.FieldType));
        else
          throw new Exception("Unable to load LibRetro core. ConnectAllEntryPoints failed.");
      }
    }

    static IEnumerable<FieldInfo> GetAllEntryPoints()
    {
      return typeof(LibRetroBase).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where((field) => field.FieldType.Name.StartsWith("epretro"));
    }
    #endregion

    #region Callbacks
    void InitCallbacks()
    {
      retro_environment_cb = new retro_environment_t(RetroEnvironment);
      retro_video_refresh_cb = new retro_video_refresh_t(RetroVideoRefresh);
      retro_audio_sample_cb = new retro_audio_sample_t(RetroAudioSample);
      retro_audio_sample_batch_cb = new retro_audio_sample_batch_t(RetroAudioSampleBatch);
      retro_input_poll_cb = new retro_input_poll_t(RetroInputPoll);
      retro_input_state_cb = new retro_input_state_t(RetroInputState);

      retro_set_environment(retro_environment_cb);
      retro_set_video_refresh(retro_video_refresh_cb);
      retro_set_audio_sample(retro_audio_sample_cb);
      retro_set_audio_sample_batch(retro_audio_sample_batch_cb);
      retro_set_input_poll(retro_input_poll_cb);
      retro_set_input_state(retro_input_state_cb);
    }
    #endregion

    #region Virtual Methods
    protected virtual bool RetroEnvironment(RETRO_ENVIRONMENT cmd, IntPtr data)
    {
      return false;
    }

    protected virtual void RetroVideoRefresh(IntPtr data, uint width, uint height, uint pitch)
    {
    }

    protected virtual void RetroAudioSample(short left, short right)
    {
    }

    protected virtual uint RetroAudioSampleBatch(IntPtr data, uint frames)
    {
      return 0;
    }

    protected virtual void RetroInputPoll()
    {
    }

    protected virtual short RetroInputState(uint port, uint device, uint index, uint id)
    {
      return 0;
    }
    #endregion

    #region IDisposable
    public virtual void Dispose()
    {
      if (_dll != null)
      {
        _dll.Dispose();
        _dll = null;
      }
    }
    #endregion
  }
}
