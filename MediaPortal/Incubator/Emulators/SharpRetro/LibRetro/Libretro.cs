using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SharpRetro.LibRetro
{
  public enum RETRO_DEVICE
  {
    NONE = 0,
    JOYPAD = 1,
    MOUSE = 2,
    KEYBOARD = 3,
    LIGHTGUN = 4,
    ANALOG = 5,
    POINTER = 6,
    SENSOR_ACCELEROMETER = 7
  };

  public enum RETRO_DEVICE_ID_JOYPAD
  {
    B = 0,
    Y = 1,
    SELECT = 2,
    START = 3,
    UP = 4,
    DOWN = 5,
    LEFT = 6,
    RIGHT = 7,
    A = 8,
    X = 9,
    L = 10,
    R = 11,
    L2 = 12,
    R2 = 13,
    L3 = 14,
    R3 = 15
  };

  public enum RETRO_LOG_LEVEL : int //exact type size is unclear
  {
    DEBUG = 0,
    INFO,
    WARN,
    ERROR,

    DUMMY = Int32.MaxValue
  };

  public enum RETRO_DEVICE_INDEX_ANALOG
  {
    LEFT = 0,
    RIGHT = 1
  };

  public enum RETRO_DEVICE_ID_ANALOG
  {
    X = 0,
    Y = 1
  };

  public enum RETRO_DEVICE_ID_MOUSE
  {
    X = 0,
    Y = 1,
    LEFT = 2,
    RIGHT = 3
  };

  public enum RETRO_DEVICE_ID_LIGHTGUN
  {
    X = 0,
    Y = 1,
    TRIGGER = 2,
    CURSOR = 3,
    TURBO = 4,
    PAUSE = 5,
    START = 6
  };

  public enum RETRO_DEVICE_ID_POINTER
  {
    X = 0,
    Y = 1,
    PRESSED = 2
  };

  public enum RETRO_DEVICE_ID_SENSOR_ACCELEROMETER
  {
    X = 0,
    Y = 1,
    Z = 2
  };

  public enum RETRO_REGION
  {
    NTSC = 0,
    PAL = 1
  };

  public enum RETRO_MEMORY
  {
    SAVE_RAM = 0,
    RTC = 1,
    SYSTEM_RAM = 2,
    VIDEO_RAM = 3,
  };

  public enum RETRO_KEY
  {
    UNKNOWN = 0,
    FIRST = 0,
    BACKSPACE = 8,
    TAB = 9,
    CLEAR = 12,
    RETURN = 13,
    PAUSE = 19,
    ESCAPE = 27,
    SPACE = 32,
    EXCLAIM = 33,
    QUOTEDBL = 34,
    HASH = 35,
    DOLLAR = 36,
    AMPERSAND = 38,
    QUOTE = 39,
    LEFTPAREN = 40,
    RIGHTPAREN = 41,
    ASTERISK = 42,
    PLUS = 43,
    COMMA = 44,
    MINUS = 45,
    PERIOD = 46,
    SLASH = 47,
    _0 = 48,
    _1 = 49,
    _2 = 50,
    _3 = 51,
    _4 = 52,
    _5 = 53,
    _6 = 54,
    _7 = 55,
    _8 = 56,
    _9 = 57,
    COLON = 58,
    SEMICOLON = 59,
    LESS = 60,
    EQUALS = 61,
    GREATER = 62,
    QUESTION = 63,
    AT = 64,
    LEFTBRACKET = 91,
    BACKSLASH = 92,
    RIGHTBRACKET = 93,
    CARET = 94,
    UNDERSCORE = 95,
    BACKQUOTE = 96,
    a = 97,
    b = 98,
    c = 99,
    d = 100,
    e = 101,
    f = 102,
    g = 103,
    h = 104,
    i = 105,
    j = 106,
    k = 107,
    l = 108,
    m = 109,
    n = 110,
    o = 111,
    p = 112,
    q = 113,
    r = 114,
    s = 115,
    t = 116,
    u = 117,
    v = 118,
    w = 119,
    x = 120,
    y = 121,
    z = 122,
    DELETE = 127,

    KP0 = 256,
    KP1 = 257,
    KP2 = 258,
    KP3 = 259,
    KP4 = 260,
    KP5 = 261,
    KP6 = 262,
    KP7 = 263,
    KP8 = 264,
    KP9 = 265,
    KP_PERIOD = 266,
    KP_DIVIDE = 267,
    KP_MULTIPLY = 268,
    KP_MINUS = 269,
    KP_PLUS = 270,
    KP_ENTER = 271,
    KP_EQUALS = 272,

    UP = 273,
    DOWN = 274,
    RIGHT = 275,
    LEFT = 276,
    INSERT = 277,
    HOME = 278,
    END = 279,
    PAGEUP = 280,
    PAGEDOWN = 281,

    F1 = 282,
    F2 = 283,
    F3 = 284,
    F4 = 285,
    F5 = 286,
    F6 = 287,
    F7 = 288,
    F8 = 289,
    F9 = 290,
    F10 = 291,
    F11 = 292,
    F12 = 293,
    F13 = 294,
    F14 = 295,
    F15 = 296,

    NUMLOCK = 300,
    CAPSLOCK = 301,
    SCROLLOCK = 302,
    RSHIFT = 303,
    LSHIFT = 304,
    RCTRL = 305,
    LCTRL = 306,
    RALT = 307,
    LALT = 308,
    RMETA = 309,
    LMETA = 310,
    LSUPER = 311,
    RSUPER = 312,
    MODE = 313,
    COMPOSE = 314,

    HELP = 315,
    PRINT = 316,
    SYSREQ = 317,
    BREAK = 318,
    MENU = 319,
    POWER = 320,
    EURO = 321,
    UNDO = 322,

    LAST
  };

  [Flags]
  public enum RETRO_MOD
  {
    NONE = 0,
    SHIFT = 1,
    CTRL = 2,
    ALT = 4,
    META = 8,
    NUMLOCK = 16,
    CAPSLOCK = 32,
    SCROLLLOCK = 64
  };

  [Flags]
  public enum RETRO_SIMD
  {
    SSE = (1 << 0),
    SSE2 = (1 << 1),
    VMX = (1 << 2),
    VMX128 = (1 << 3),
    AVX = (1 << 4),
    NEON = (1 << 5),
    SSE3 = (1 << 6),
    SSSE3 = (1 << 7),
    MMX = (1 << 8),
    MMXEXT = (1 << 9),
    SSE4 = (1 << 10),
    SSE42 = (1 << 11),
    AVX2 = (1 << 12),
    VFPU = (1 << 13),
    PS = (1 << 14),
    AES = (1 << 15),
    VFPV3 = (1 << 16),
    VFPV4 = (1 << 17),
  }

  public enum RETRO_ENVIRONMENT
  {
    SET_ROTATION = 1,
    GET_OVERSCAN = 2,
    GET_CAN_DUPE = 3,
    SET_MESSAGE = 6,
    SHUTDOWN = 7,
    SET_PERFORMANCE_LEVEL = 8,
    GET_SYSTEM_DIRECTORY = 9,
    SET_PIXEL_FORMAT = 10,
    SET_INPUT_DESCRIPTORS = 11,
    SET_KEYBOARD_CALLBACK = 12,
    SET_DISK_CONTROL_INTERFACE = 13,
    SET_HW_RENDER = 14,
    GET_VARIABLE = 15,
    SET_VARIABLES = 16,
    GET_VARIABLE_UPDATE = 17,
    SET_SUPPORT_NO_GAME = 18,
    GET_LIBRETRO_PATH = 19,
    SET_AUDIO_CALLBACK = 22,
    SET_FRAME_TIME_CALLBACK = 21,
    GET_RUMBLE_INTERFACE = 23,
    GET_INPUT_DEVICE_CAPABILITIES = 24,
    //25,26 are experimental
    GET_LOG_INTERFACE = 27,
    GET_PERF_INTERFACE = 28,
    GET_LOCATION_INTERFACE = 29,
    GET_CORE_ASSETS_DIRECTORY = 30,
    GET_SAVE_DIRECTORY = 31,
    SET_SYSTEM_AV_INFO = 32,
    SET_PROC_ADDRESS_CALLBACK = 33,
    SET_SUBSYSTEM_INFO = 34,
    SET_CONTROLLER_INFO = 35,
    SET_MEMORY_MAPS = 36 | EXPERIMENTAL,
    SET_GEOMETRY = 37,
    GET_USERNAME = 38,
    GET_LANGUAGE = 39,

    EXPERIMENTAL = 0x10000
  };

  public enum retro_hw_context_type
  {
    RETRO_HW_CONTEXT_NONE = 0,
    RETRO_HW_CONTEXT_OPENGL = 1,
    RETRO_HW_CONTEXT_OPENGLES2 = 2,
    RETRO_HW_CONTEXT_OPENGL_CORE = 3,
    RETRO_HW_CONTEXT_OPENGLES3 = 4,
    RETRO_HW_CONTEXT_OPENGLES_VERSION = 5,

    RETRO_HW_CONTEXT_DUMMY = Int32.MaxValue
  };
  
  [StructLayout(LayoutKind.Sequential)]
  public struct retro_hw_render_callback
  {
    public const int RETRO_HW_FRAME_BUFFER_VALID = -1;

    public uint context_type; //retro_hw_context_type
    public IntPtr context_reset; //retro_hw_context_reset_t
    public IntPtr get_current_framebuffer; //retro_hw_get_current_framebuffer_t
    public IntPtr get_proc_address; //retro_hw_get_proc_address_t
    [MarshalAs(UnmanagedType.U1)]
    public bool depth;
    [MarshalAs(UnmanagedType.U1)]
    public bool stencil;
    [MarshalAs(UnmanagedType.U1)]
    public bool bottom_left_origin;
    public uint version_major;
    public uint version_minor;
    [MarshalAs(UnmanagedType.U1)]
    public bool cache_context;
    public IntPtr context_destroy; //retro_hw_context_reset_t
    [MarshalAs(UnmanagedType.U1)]
    public bool debug_context;
  };

  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  public delegate void retro_hw_context_reset_t();

  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  public delegate uint retro_hw_get_current_framebuffer_t();

  //not used
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  public delegate void retro_proc_address_t();

  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  public delegate IntPtr retro_hw_get_proc_address_t(IntPtr sym);

  [StructLayout(LayoutKind.Sequential)]
  struct retro_memory_map
  {
    public IntPtr descriptors; //retro_memory_descriptor *
    public uint num_descriptors;
  };

  [StructLayout(LayoutKind.Sequential)]
  struct retro_memory_descriptor
  {
    ulong flags;
    IntPtr ptr;
    IntPtr offset; //size_t
    IntPtr start; //size_t
    IntPtr select; //size_t
    IntPtr disconnect; //size_t
    IntPtr len; //size_t
    IntPtr addrspace;
  };

  public enum RETRO_PIXEL_FORMAT
  {
    XRGB1555 = 0,
    XRGB8888 = 1,
    RGB565 = 2
  };

  [StructLayout(LayoutKind.Sequential)]
  public struct retro_message
  {
    public string msg;
    public uint frames;
  }

  [StructLayout(LayoutKind.Sequential)]
  public struct retro_input_descriptor
  {
    public uint port;
    public uint device;
    public uint index;
    public uint id;
  }

  [StructLayout(LayoutKind.Sequential)]
  public struct retro_system_info
  {
    public IntPtr library_name;
    public IntPtr library_version;
    public IntPtr valid_extensions;
    [MarshalAs(UnmanagedType.U1)]
    public bool need_fullpath;
    [MarshalAs(UnmanagedType.U1)]
    public bool block_extract;
  }

  [StructLayout(LayoutKind.Sequential)]
  public struct retro_game_geometry
  {
    public uint base_width;
    public uint base_height;
    public uint max_width;
    public uint max_height;
    public float aspect_ratio;
  }

  [StructLayout(LayoutKind.Sequential)]
  public struct retro_system_timing
  {
    public double fps;
    public double sample_rate;
  }

  [StructLayout(LayoutKind.Sequential)]
  public struct retro_system_av_info
  {
    public retro_game_geometry geometry;
    public retro_system_timing timing;
  }

  [StructLayout(LayoutKind.Sequential)]
  public struct retro_variable
  {
    public string key;
    public string value;
  }

  [StructLayout(LayoutKind.Sequential)]
  public struct retro_game_info
  {
    public string path;
    public IntPtr data;
    public uint size;
    public string meta;
  }

  //untested
  [StructLayout(LayoutKind.Sequential)]
  public struct retro_perf_counter
  {
    public string ident;
    public ulong start;
    public ulong total;
    public ulong call_cnt;

    [MarshalAs(UnmanagedType.U1)]
    public bool registered;
  };

  //perf callbacks
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  public delegate long retro_perf_get_time_usec_t();
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  public delegate long retro_perf_get_counter_t();
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  public delegate ulong retro_get_cpu_features_t();
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  public delegate void retro_perf_log_t();
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  public delegate void retro_perf_register_t(ref retro_perf_counter counter);
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  public delegate void retro_perf_start_t(ref retro_perf_counter counter);
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  public delegate void retro_perf_stop_t(ref retro_perf_counter counter);

  //for GET_PERF_INTERFACE
  [StructLayout(LayoutKind.Sequential)]
  public struct retro_perf_callback
  {
    public retro_perf_get_time_usec_t get_time_usec;
    public retro_get_cpu_features_t get_cpu_features;
    public retro_perf_get_counter_t get_perf_counter;
    public retro_perf_register_t perf_register;
    public retro_perf_start_t perf_start;
    public retro_perf_stop_t perf_stop;
    public retro_perf_log_t perf_log;
  }

  //Rumble interface
  public enum retro_rumble_effect
  {
    RETRO_RUMBLE_STRONG = 0,
    RETRO_RUMBLE_WEAK = 1,

    RETRO_RUMBLE_DUMMY = int.MaxValue
  };

  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  [return: MarshalAs(UnmanagedType.U1)]
  public delegate bool retro_set_rumble_state_t(uint port, retro_rumble_effect effect, ushort strength);

  [StructLayout(LayoutKind.Sequential)]
  public struct retro_rumble_interface
  {
    public retro_set_rumble_state_t set_rumble_state;
  }

  [StructLayout(LayoutKind.Sequential)]
  public struct retro_subsystem_memory_info
  {
    /* The extension associated with a memory type, e.g. "psram". */
    public string extension;

    /* The memory type for retro_get_memory(). This should be at 
     * least 0x100 to avoid conflict with standardized 
     * libretro memory types. */
    uint type;
  };

  [StructLayout(LayoutKind.Sequential)]
  public struct retro_subsystem_rom_info
  {
    /* Describes what the content is (SGB BIOS, GB ROM, etc). */
    public string desc; //const char *

    /* Same definition as retro_get_system_info(). */
    public string valid_extensions; //const char *

    /* Same definition as retro_get_system_info(). */
    [MarshalAs(UnmanagedType.U1)]
    public bool need_fullpath;

    /* Same definition as retro_get_system_info(). */
    [MarshalAs(UnmanagedType.U1)]
    public bool block_extract;

    /* This is set if the content is required to load a game. 
     * If this is set to false, a zeroed-out retro_game_info can be passed. */
    [MarshalAs(UnmanagedType.U1)]
    public bool required;

    /* Content can have multiple associated persistent 
     * memory types (retro_get_memory()). */
    public IntPtr memory; //retro_subsystem_memory_info *
    public uint num_memory;
  };

  [StructLayout(LayoutKind.Sequential)]
  public struct retro_subsystem_info
  {
    /* Human-readable string of the subsystem type, e.g. "Super GameBoy" */
    public string desc;

    /* A computer friendly short string identifier for the subsystem type.
     * This name must be [a-z].
     * E.g. if desc is "Super GameBoy", this can be "sgb".
     * This identifier can be used for command-line interfaces, etc.
     */
    public string ident;

    /* Infos for each content file. The first entry is assumed to be the 
     * "most significant" content for frontend purposes.
     * E.g. with Super GameBoy, the first content should be the GameBoy ROM, 
     * as it is the most "significant" content to a user.
     * If a frontend creates new file paths based on the content used 
     * (e.g. savestates), it should use the path for the first ROM to do so. */
    public IntPtr roms; //retro_subsystem_rom_info*

    /* Number of content files associated with a subsystem. */
    public uint num_roms;

    /* The type passed to retro_load_game_special(). */
    public uint id;
  };

  #region callback prototypes
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  public unsafe delegate void retro_log_printf_t(RETRO_LOG_LEVEL level, string fmt, IntPtr a0, IntPtr a1, IntPtr a2, IntPtr a3, IntPtr a4, IntPtr a5, IntPtr a6, IntPtr a7, IntPtr a8, IntPtr a9, IntPtr a10, IntPtr a11, IntPtr a12, IntPtr a13, IntPtr a14, IntPtr a15);
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  [return: MarshalAs(UnmanagedType.U1)]
  public delegate bool retro_environment_t(RETRO_ENVIRONMENT cmd, IntPtr data);
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  public delegate void retro_video_refresh_t(IntPtr data, uint width, uint height, uint pitch);
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  public delegate void retro_audio_sample_t(short left, short right);
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  public delegate uint retro_audio_sample_batch_t(IntPtr data, uint frames);
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  public delegate void retro_input_poll_t();
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  public delegate short retro_input_state_t(uint port, uint device, uint index, uint id);
  #endregion

  #region entry point prototypes
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  public delegate void epretro_set_environment(retro_environment_t cb);
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  public delegate void epretro_set_video_refresh(retro_video_refresh_t cb);
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  public delegate void epretro_set_audio_sample(retro_audio_sample_t cb);
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  public delegate void epretro_set_audio_sample_batch(retro_audio_sample_batch_t cb);
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  public delegate void epretro_set_input_poll(retro_input_poll_t cb);
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  public delegate void epretro_set_input_state(retro_input_state_t cb);
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  public delegate void epretro_init();
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  public delegate void epretro_deinit();
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  public delegate uint epretro_api_version();
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  public delegate void epretro_get_system_info(ref retro_system_info info);
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  public delegate void epretro_get_system_av_info(ref retro_system_av_info info);
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  public delegate void epretro_set_controller_port_device(uint port, uint device);
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  public delegate void epretro_reset();
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  public delegate void epretro_run();
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  public delegate uint epretro_serialize_size();
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  [return: MarshalAs(UnmanagedType.U1)]
  public delegate bool epretro_serialize(IntPtr data, uint size);
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  [return: MarshalAs(UnmanagedType.U1)]
  public delegate bool epretro_unserialize(IntPtr data, uint size);
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  public delegate void epretro_cheat_reset();
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  public delegate void epretro_cheat_set(uint index, [MarshalAs(UnmanagedType.U1)]bool enabled, string code);
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  [return: MarshalAs(UnmanagedType.U1)]
  public delegate bool epretro_load_game(ref retro_game_info game);
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  [return: MarshalAs(UnmanagedType.U1)]
  public delegate bool epretro_load_game_special(uint game_type, ref retro_game_info info, uint num_info);
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  public delegate void epretro_unload_game();
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  public delegate uint epretro_get_region();
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  public delegate IntPtr epretro_get_memory_data(RETRO_MEMORY id);
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  public delegate uint epretro_get_memory_size(RETRO_MEMORY id);
  #endregion
}
