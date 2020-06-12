using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Timers;
using System.Collections;
using System.IO;
using System.Runtime.ConstrainedExecution;
using System.Security;
using Microsoft.Win32.SafeHandles;
using System.Windows.Forms;

namespace Emulators.Input
{
  /// <summary>StephenToub's Simple implementation of a keyboard hook for key down events on the GUI thread of an application.</summary>
  public sealed class KeyboardHook : IDisposable
  {
    public const int WM_NULL = 0x000;
    public const int WM_CREATE = 0x001;
    public const int WM_DESTROY = 0x002;
    public const int WM_MOVE = 0x003;
    public const int WM_SIZE = 0x005;
    public const int WM_ACTIVATE = 0x006;
    public const int WM_SETFOCUS = 0x007;
    public const int WM_KILLFOCUS = 0x008;
    public const int WM_ENABLE = 0x00A;
    public const int WM_SETREDRAW = 0x00B;
    public const int WM_SETTEXT = 0x00C;
    public const int WM_GETTEXT = 0x00D;
    public const int WM_GETTEXTLENGTH = 0x00E;
    public const int WM_PAINT = 0x00F;
    public const int WM_CLOSE = 0x010;
    public const int WM_QUERYENDSESSION = 0x011;
    public const int WM_QUIT = 0x012;
    public const int WM_QUERYOPEN = 0x013;
    public const int WM_ERASEBKGND = 0x014;
    public const int WM_SYSCOLORCHANGE = 0x015;
    public const int WM_ENDSESSION = 0x016;
    public const int WM_SHOWWINDOW = 0x018;
    public const int WM_WININICHANGE = 0x01A;
    public const int WM_DEVMODECHANGE = 0x01B;
    public const int WM_ACTIVATEAPP = 0x01C;
    public const int WM_FONTCHANGE = 0x01D;
    public const int WM_TIMECHANGE = 0x01E;
    public const int WM_CANCELMODE = 0x01F;
    public const int WM_SETCURSOR = 0x020;
    public const int WM_MOUSEACTIVATE = 0x021;
    public const int WM_CHILDACTIVATE = 0x022;
    public const int WM_QUEUESYNC = 0x023;
    public const int WM_GETMINMAXINFO = 0x024;
    public const int WM_PAINTICON = 0x026;
    public const int WM_ICONERASEBKGND = 0x027;
    public const int WM_NEXTDLGCTL = 0x028;
    public const int WM_SPOOLERSTATUS = 0x02A;
    public const int WM_DRAWITEM = 0x02B;
    public const int WM_MEASUREITEM = 0x02C;
    public const int WM_DELETEITEM = 0x02D;
    public const int WM_VKEYTOITEM = 0x02E;
    public const int WM_CHARTOITEM = 0x02F;
    public const int WM_SETFONT = 0x030;
    public const int WM_GETFONT = 0x031;
    public const int WM_SETHOTKEY = 0x032;
    public const int WM_GETHOTKEY = 0x033;
    public const int WM_QUERYDRAGICON = 0x037;
    public const int WM_COMPAREITEM = 0x039;
    public const int WM_COMPACTING = 0x041;
    public const int WM_COMMNOTIFY = 0x044; /* no longer suported */
    public const int WM_WINDOWPOSCHANGING = 0x046;
    public const int WM_WINDOWPOSCHANGED = 0x047;
    public const int WM_POWER = 0x048;
    public const int WM_COPYDATA = 0x04A;
    public const int WM_CANCELJOURNAL = 0x04B;
    public const int WM_USER = 0x400;
    public const int WM_NOTIFY = 0x04E;
    public const int WM_INPUTLANGCHANGEREQUEST = 0x050;
    public const int WM_INPUTLANGCHANGE = 0x051;
    public const int WM_TCARD = 0x052;
    public const int WM_HELP = 0x053;
    public const int WM_USERCHANGED = 0x054;
    public const int WM_NOTIFYFORMAT = 0x055;
    public const int WM_CONTEXTMENU = 0x07B;
    public const int WM_STYLECHANGING = 0x07C;
    public const int WM_STYLECHANGED = 0x07D;
    public const int WM_DISPLAYCHANGE = 0x07E;
    public const int WM_GETICON = 0x07F;
    public const int WM_SETICON = 0x080;
    public const int WM_NCCREATE = 0x081;
    public const int WM_NCDESTROY = 0x082;
    public const int WM_NCCALCSIZE = 0x083;
    public const int WM_NCHITTEST = 0x084;
    public const int WM_NCPAINT = 0x085;
    public const int WM_NCACTIVATE = 0x086;
    public const int WM_GETDLGCODE = 0x087;
    public const int WM_SYNCPAINT = 0x088;
    public const int WM_NCMOUSEMOVE = 0x0A0;
    public const int WM_NCLBUTTONDOWN = 0x0A1;
    public const int WM_NCLBUTTONUP = 0x0A2;
    public const int WM_NCLBUTTONDBLCLK = 0x0A3;
    public const int WM_NCRBUTTONDOWN = 0x0A4;
    public const int WM_NCRBUTTONUP = 0x0A5;
    public const int WM_NCRBUTTONDBLCLK = 0x0A6;
    public const int WM_NCMBUTTONDOWN = 0x0A7;
    public const int WM_NCMBUTTONUP = 0x0A8;
    public const int WM_NCMBUTTONDBLCLK = 0x0A9;
    public const int WM_NCXBUTTONDOWN = 0x0AB;
    public const int WM_NCXBUTTONUP = 0x0AC;
    public const int WM_NCXBUTTONDBLCLK = 0x0AD;
    public const int WM_INPUT = 0x0FF;
    public const int WM_KEYFIRST = 0x100;
    public const int WM_KEYDOWN = 0x100;
    public const int WM_KEYUP = 0x101;
    public const int WM_CHAR = 0x102;
    public const int WM_DEADCHAR = 0x103;
    public const int WM_SYSKEYDOWN = 0x104;
    public const int WM_SYSKEYUP = 0x105;
    public const int WM_SYSCHAR = 0x106;
    public const int WM_SYSDEADCHAR = 0x107;
    public const int WM_UNICHAR = 0x109;
    public const int WM_KEYLAST = 0x109;
    public const int WM_IME_STARTCOMPOSITION = 0x10D;
    public const int WM_IME_ENDCOMPOSITION = 0x10E;
    public const int WM_IME_COMPOSITION = 0x10F;
    public const int WM_IME_KEYLAST = 0x10F;
    public const int WM_INITDIALOG = 0x110;
    public const int WM_COMMAND = 0x111;
    public const int WM_SYSCOMMAND = 0x112;
    public const int WM_TIMER = 0x113;
    public const int WM_HSCROLL = 0x114;
    public const int WM_VSCROLL = 0x115;
    public const int WM_INITMENU = 0x116;
    public const int WM_INITMENUPOPUP = 0x117;
    public const int WM_MENUSELECT = 0x11F;
    public const int WM_MENUCHAR = 0x120;
    public const int WM_ENTERIDLE = 0x121;
    public const int WM_MENURBUTTONUP = 0x122;
    public const int WM_MENUDRAG = 0x123;
    public const int WM_MENUGETOBJECT = 0x124;
    public const int WM_UNINITMENUPOPUP = 0x125;
    public const int WM_MENUCOMMAND = 0x126;
    public const int WM_CHANGEUISTATE = 0x127;
    public const int WM_UPDATEUISTATE = 0x128;
    public const int WM_QUERYUISTATE = 0x129;
    public const int WM_CTLCOLORMSGBOX = 0x132;
    public const int WM_CTLCOLOREDIT = 0x133;
    public const int WM_CTLCOLORLISTBOX = 0x134;
    public const int WM_CTLCOLORBTN = 0x135;
    public const int WM_CTLCOLORDLG = 0x136;
    public const int WM_CTLCOLORSCROLLBAR = 0x137;
    public const int WM_CTLCOLORSTATIC = 0x138;
    public const int MN_GETHMENU = 0x1E1;
    public const int WM_MOUSEFIRST = 0x200;
    public const int WM_MOUSEMOVE = 0x200;
    public const int WM_LBUTTONDOWN = 0x201;
    public const int WM_LBUTTONUP = 0x202;
    public const int WM_LBUTTONDBLCLK = 0x203;
    public const int WM_RBUTTONDOWN = 0x204;
    public const int WM_RBUTTONUP = 0x205;
    public const int WM_RBUTTONDBLCLK = 0x206;
    public const int WM_MBUTTONDOWN = 0x207;
    public const int WM_MBUTTONUP = 0x208;
    public const int WM_MBUTTONDBLCLK = 0x209;
    public const int WM_MOUSEWHEEL = 0x20A;
    public const int WM_XBUTTONDOWN = 0x20B;
    public const int WM_XBUTTONUP = 0x20C;
    public const int WM_XBUTTONDBLCLK = 0x20D;
    public const int WM_MOUSELAST = 0x20A;
    public const int WM_PARENTNOTIFY = 0x210;
    public const int WM_ENTERMENULOOP = 0x211;
    public const int WM_EXITMENULOOP = 0x212;
    public const int WM_NEXTMENU = 0x213;
    public const int WM_SIZING = 0x214;
    public const int WM_CAPTURECHANGED = 0x215;
    public const int WM_MOVING = 0x216;
    public const int WM_POWERBROADCAST = 0x218;
    public const int WM_DEVICECHANGE = 0x219;
    public const int WM_MDICREATE = 0x220;
    public const int WM_MDIDESTROY = 0x221;
    public const int WM_MDIACTIVATE = 0x222;
    public const int WM_MDIRESTORE = 0x223;
    public const int WM_MDINEXT = 0x224;
    public const int WM_MDIMAXIMIZE = 0x225;
    public const int WM_MDITILE = 0x226;
    public const int WM_MDICASCADE = 0x227;
    public const int WM_MDIICONARRANGE = 0x228;
    public const int WM_MDIGETACTIVE = 0x229;
    public const int WM_MDISETMENU = 0x230;
    public const int WM_ENTERSIZEMOVE = 0x231;
    public const int WM_EXITSIZEMOVE = 0x232;
    public const int WM_DROPFILES = 0x233;
    public const int WM_MDIREFRESHMENU = 0x234;
    public const int WM_IME_SETCONTEXT = 0x281;
    public const int WM_IME_NOTIFY = 0x282;
    public const int WM_IME_CONTROL = 0x283;
    public const int WM_IME_COMPOSITIONFULL = 0x284;
    public const int WM_IME_SELECT = 0x285;
    public const int WM_IME_CHAR = 0x286;
    public const int WM_IME_REQUEST = 0x288;
    public const int WM_IME_KEYDOWN = 0x290;
    public const int WM_IME_KEYUP = 0x291;
    public const int WM_MOUSEHOVER = 0x2A1;
    public const int WM_MOUSELEAVE = 0x2A3;
    public const int WM_NCMOUSEHOVER = 0x2A0;
    public const int WM_NCMOUSELEAVE = 0x2A2;
    public const int WM_WTSSESSION_CHANGE = 0x2B1;
    public const int WM_TABLET_FIRST = 0x2c0;
    public const int WM_TABLET_LAST = 0x2df;
    public const int WM_CUT = 0x300;
    public const int WM_COPY = 0x301;
    public const int WM_PASTE = 0x302;
    public const int WM_CLEAR = 0x303;
    public const int WM_UNDO = 0x304;
    public const int WM_RENDERFORMAT = 0x305;
    public const int WM_RENDERALLFORMATS = 0x306;
    public const int WM_DESTROYCLIPBOARD = 0x307;
    public const int WM_DRAWCLIPBOARD = 0x308;
    public const int WM_PAINTCLIPBOARD = 0x309;
    public const int WM_VSCROLLCLIPBOARD = 0x30A;
    public const int WM_SIZECLIPBOARD = 0x30B;
    public const int WM_ASKCBFORMATNAME = 0x30C;
    public const int WM_CHANGECBCHAIN = 0x30D;
    public const int WM_HSCROLLCLIPBOARD = 0x30E;
    public const int WM_QUERYNEWPALETTE = 0x30F;
    public const int WM_PALETTEISCHANGING = 0x310;
    public const int WM_PALETTECHANGED = 0x311;
    public const int WM_HOTKEY = 0x312;
    public const int WM_PRINT = 0x317;
    public const int WM_PRINTCLIENT = 0x318;
    public const int WM_APPCOMMAND = 0x319;
    public const int WM_THEMECHANGED = 0x31A;
    public const int WM_HANDHELDFIRST = 0x358;
    public const int WM_HANDHELDLAST = 0x35F;
    public const int WM_AFXFIRST = 0x360;
    public const int WM_AFXLAST = 0x37F;
    public const int WM_PENWINFIRST = 0x380;
    public const int WM_PENWINLAST = 0x38F;

    public const int MK_LBUTTON = 0x0001;
    public const int MK_RBUTTON = 0x0002;
    public const int MK_SHIFT = 0x0004;
    public const int MK_CONTROL = 0x0008;
    public const int MK_MBUTTON = 0x0010;

    public const uint VK_LBUTTON = 0x01;   //Left mouse button
    public const uint VK_RBUTTON = 0x02;   //Right mouse button
    public const uint VK_CANCEL = 0x03;   //Control-break processing
    public const uint VK_MBUTTON = 0x04; //Middle mouse button (three-button mouse)
    public const uint VK_BACK = 0x08;   //BACKSPACE key
    public const uint VK_TAB = 0x09;   //TAB key
    public const uint VK_CLEAR = 0x0C;     //CLEAR key
    public const uint VK_RETURN = 0x0D;   //ENTER key
    public const uint VK_SHIFT = 0x10;   //SHIFT key
    public const uint VK_CONTROL = 0x11;  //CTRL key
    public const uint VK_MENU = 0x12;    //ALT key
    public const uint VK_PAUSE = 0x13;  //PAUSE key
    public const uint VK_CAPITAL = 0x14;   //CAPS LOCK key
    public const uint VK_ESCAPE = 0x1B;   //ESC key
    public const uint VK_SPACE = 0x20;   //SPACEBAR
    public const uint VK_PRIOR = 0x21;   //PAGE UP key
    public const uint VK_NEXT = 0x22;   //PAGE DOWN key
    public const uint VK_END = 0x23;   //END key
    public const uint VK_HOME = 0x24;   //HOME key
    public const uint VK_LEFT = 0x25;  //LEFT ARROW key
    public const uint VK_UP = 0x26;   //UP ARROW key
    public const uint VK_RIGHT = 0x27;   //RIGHT ARROW key
    public const uint VK_DOWN = 0x28;   //DOWN ARROW key
    public const uint VK_SELECT = 0x29;   //SELECT key
    public const uint VK_PRINT = 0x2A;   //PRINT key
    public const uint VK_EXECUTE = 0x2B;   //EXECUTE key
    public const uint VK_SNAPSHOT = 0x2C;   //PRINT SCREEN key
    public const uint VK_INSERT = 0x2D;   //INS key
    public const uint VK_DELETE = 0x2E;   //DEL key
    public const uint VK_HELP = 0x2F;   //HELP key
    public const uint VK_0 = 0x30;   //0 key
    public const uint VK_1 = 0x31;   //1 key
    public const uint VK_2 = 0x32;   //2 key
    public const uint VK_3 = 0x33;   //3 key
    public const uint VK_4 = 0x34;   //4 key
    public const uint VK_5 = 0x35;   //5 key
    public const uint VK_6 = 0x36;   //6 key
    public const uint VK_7 = 0x37;   //7 key
    public const uint VK_8 = 0x38;   //8 key
    public const uint VK_9 = 0x39;   //9 key
    public const uint VK_A = 0x41;   //A key
    public const uint VK_B = 0x42;   //B key
    public const uint VK_C = 0x43;   //C key
    public const uint VK_D = 0x44;   //D key
    public const uint VK_E = 0x45;   //E key
    public const uint VK_F = 0x46;   //F key
    public const uint VK_G = 0x47;   //G key
    public const uint VK_H = 0x48;   //H key
    public const uint VK_I = 0x49;   //I key
    public const uint VK_J = 0x4A;   //J key
    public const uint VK_K = 0x4B;   //K key
    public const uint VK_L = 0x4C;   //L key
    public const uint VK_M = 0x4D;   //M key
    public const uint VK_N = 0x4E;   //N key
    public const uint VK_O = 0x4F;   //O key
    public const uint VK_P = 0x50;   //P key
    public const uint VK_Q = 0x51;   //Q key
    public const uint VK_R = 0x52;   //R key
    public const uint VK_S = 0x53;   //S key
    public const uint VK_T = 0x54;   //T key
    public const uint VK_U = 0x55;   //U key
    public const uint VK_V = 0x56;   //V key
    public const uint VK_W = 0x57;   //W key
    public const uint VK_X = 0x58;   //X key
    public const uint VK_Y = 0x59;   //Y key
    public const uint VK_Z = 0x5A;   //Z key
    public const uint VK_NUMPAD0 = 0x60;   //Numeric keypad 0 key
    public const uint VK_NUMPAD1 = 0x61;   //Numeric keypad 1 key
    public const uint VK_NUMPAD2 = 0x62;   //Numeric keypad 2 key
    public const uint VK_NUMPAD3 = 0x63;   //Numeric keypad 3 key
    public const uint VK_NUMPAD4 = 0x64;   //Numeric keypad 4 key
    public const uint VK_NUMPAD5 = 0x65;   //Numeric keypad 5 key
    public const uint VK_NUMPAD6 = 0x66;   //Numeric keypad 6 key
    public const uint VK_NUMPAD7 = 0x67;   //Numeric keypad 7 key
    public const uint VK_NUMPAD8 = 0x68;   //Numeric keypad 8 key
    public const uint VK_NUMPAD9 = 0x69;   //Numeric keypad 9 key
    public const uint VK_SEPARATOR = 0x6C;   //Separator key
    public const uint VK_SUBTRACT = 0x6D;   //Subtract key
    public const uint VK_DECIMAL = 0x6E;   //Decimal key
    public const uint VK_DIVIDE = 0x6F;   //Divide key
    public const uint VK_F1 = 0x70;   //F1 key
    public const uint VK_F2 = 0x71;   //F2 key
    public const uint VK_F3 = 0x72;   //F3 key
    public const uint VK_F4 = 0x73;   //F4 key
    public const uint VK_F5 = 0x74;   //F5 key
    public const uint VK_F6 = 0x75;   //F6 key
    public const uint VK_F7 = 0x76;   //F7 key
    public const uint VK_F8 = 0x77;   //F8 key
    public const uint VK_F9 = 0x78;   //F9 key
    public const uint VK_F10 = 0x79;  //F10 key
    public const uint VK_F11 = 0x7A;  //F11 key
    public const uint VK_F12 = 0x7B;  //F12 key
    public const uint VK_SCROLL = 0x91;   //SCROLL LOCK key
    public const uint VK_LSHIFT = 0xA0;   //Left SHIFT key
    public const uint VK_RSHIFT = 0xA1;   //Right SHIFT key
    public const uint VK_LCONTROL = 0xA2; //Left CONTROL key
    public const uint VK_RCONTROL = 0xA3; //Right CONTROL key
    public const uint VK_LMENU = 0xA4;    //Left MENU key
    public const uint VK_RMENU = 0xA5;    //Right MENU key
    public const uint VK_PLAY = 0xFA;    //Play key
    public const uint VK_ZOOM = 0xFB;   //Zoom key
    public const int SW_HIDE = 0;
    public const int SW_SHOWNORMAL = 1;
    public const int SW_SHOWMINIMIZED = 2;
    public const int SW_SHOWMAXIMIZED = 3;
    public const int SW_SHOWNOACTIVATE = 4;
    public const int SW_SHOW = 5;
    public const int SW_MINIMIZE = 6;
    public const int SW_SHOWMINNOACTIVE = 7;
    public const int SW_SHOWNA = 8;
    public const int SW_RESTORE = 9;
    public const int SW_SHOWDEFAULT = 10;
    public const int SW_FORCEMINIMIZE = 11;

    public const int APPCOMMAND_MEDIA_PLAY = 46;
    public const int APPCOMMAND_MEDIA_PAUSE = 47;
    public const int APPCOMMAND_MEDIA_RECORD = 48;
    public const int APPCOMMAND_MEDIA_FAST_FORWARD = 49;
    public const int APPCOMMAND_MEDIA_REWIND = 50;
    public const int APPCOMMAND_MEDIA_CHANNEL_UP = 51;
    public const int APPCOMMAND_MEDIA_CHANNEL_DOWN = 52;


    /// <summary>Low-level keyboard hook id.</summary>
    private const int WH_KEYBOARD_LL = 13;
    private const int WH_SHELL = 10;
    /// <summary>OS handle for the registered keyboard hook.</summary>
    private SafeWindowsHookHandle _hookHandle = null;
    /// <summary>Hook callback when a WH_KEYBOARD event occurs.</summary>
    private LowLevelKeyboardProc _hookProc;
    /// <summary>Delegate called when a key is pressed down.</summary>
    private KeyEventHandler _keyDown;
    /// <summary>Target process ID.</summary>
    private int _pid;

    /// <summary>Initializes the keyboard hook.</summary>
    /// <param name="keyDown">The delegate to be called when a key down event occurs.</param>
    public KeyboardHook(int targetProcessID, KeyEventHandler keyDown)
    {
      if (keyDown == null) throw new ArgumentNullException("keyDown");

      // Store the user's KeyDown delegate
      _keyDown = keyDown;
      _pid = targetProcessID;

      // Create the callback and pin it, since it'll be called 
      // from unmanaged code
      _hookProc = new LowLevelKeyboardProc(HookCallback);

      // Set the hook for just the GUI thread
      using (Process curProcess = Process.GetCurrentProcess())
      using (ProcessModule curModule = curProcess.MainModule)
      {
        _hookHandle = SafeWindowsHookHandle.SetWindowsHookEx(
            WH_KEYBOARD_LL, _hookProc,
            GetModuleHandle(curModule.ModuleName), 0);
      }
      if (_hookHandle.IsInvalid)
      {
        Exception exc = new Win32Exception();
        Dispose();
        throw exc;
      }
    }

    /// <summary>Dispose the KeyboardHook.</summary>
    public void Dispose()
    {
      if (_hookHandle != null)
      {
        _hookHandle.Dispose();
        _hookHandle = null;
      }
    }

    /// <summary>Event raised when an exception is encountered from a callback.</summary>
    public ErrorEventHandler Error = delegate { };

    /// <summary>HookCallback data.</summary>
    private struct KBDLLHOOKSTRUCT
    {
#pragma warning disable 0649
      public uint vkCode;
      public uint scanCode;
      public uint flags;
      public uint time;
      public IntPtr dwExtraInfo;
#pragma warning restore 0649
    }

    /// <summary>Callback from the installed hook.</summary> 
    ///</returns>
    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
      bool handled = false;
      try
      {
        if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
        {

          uint pid;
          GetWindowThreadProcessId(GetForegroundWindow(), out pid);
          if (pid == _pid)
          {
            KBDLLHOOKSTRUCT hookParam =
                (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam,
                typeof(KBDLLHOOKSTRUCT));
            Keys key = (Keys)hookParam.vkCode;
            if (key == Keys.Packet) key = (Keys)hookParam.scanCode;

            KeyEventArgs e = new KeyEventArgs(key | ModifierKeys);
            _keyDown(this, e);
            handled = e.Handled | e.SuppressKeyPress;
          }
        }
        // won't work because WH_KEYBOARD_LL doesn't fire for app commands
        // can't change it to WH_SHELL to get the app commands to fire because 
        // only WH_KEYBOARD_LL and WH_MOUSE_LL are supported for global hooks like this one
        else if (nCode >= 0 && wParam == (IntPtr)WM_APPCOMMAND)
        {
          //   LogToFile("app command detected");
          uint pid;
          GetWindowThreadProcessId(GetForegroundWindow(), out pid);
          if (pid == _pid)
          {
            int cmd = (int)((uint)lParam >> 16 & ~0xf000);
            //   LogToFile("app command is " + cmd);
            Keys key = Keys.F24;

            if (cmd == APPCOMMAND_MEDIA_PLAY)
            {
              key = Keys.Play;
            }
            KeyEventArgs e = new KeyEventArgs(key | ModifierKeys);

            _keyDown(this, e);
            handled = e.Handled | e.SuppressKeyPress;
          }
        }

        return handled ?
            new IntPtr(1) :
            SafeWindowsHookHandle.CallNextHookEx(
                _hookHandle, nCode, wParam, lParam);
      }
      catch (Exception exc) { Error(this, new ErrorEventArgs(exc)); }
      return new IntPtr(1);
    }

    /// <summary>Gets the modifier keys currently in use.</summary>
    private static Keys ModifierKeys
    {
      get
      {
        Keys modifiers = Keys.None;
        if (GetKeyState(VK_SHIFT) < 0) modifiers |= Keys.Shift;
        if (GetKeyState(VK_CONTROL) < 0) modifiers |= Keys.Control;
        if (GetKeyState(VK_MENU) < 0) modifiers |= Keys.Alt;
        return modifiers;
      }
    }

    /// <summary>
    /// The GetWindowThreadProcessId function retrieves the identifier of the thread that created the specified window 
    /// and, optionally, the identifier of the process that created the window.
    /// </summary>
    /// <param name="hWnd">Handle to the window.</param>
    /// <param name="lpdwProcessId">Pointer to a variable that receives the process identifier.</param>
    /// <returns>The return value is the identifier of the thread that created the window.</returns>
    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    /// <summary>Returns a handle to the foreground window (the window with which the user is currently working).</summary>
    /// <returns>A handle to the foreground window.</returns>
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetForegroundWindow();


    /// <summary>The GetKeyState function retrieves the status of the specified virtual key.</summary>
    /// <param name="nVirtKey">Specifies a virtual key.</param>
    /// <returns>The return value specifies the status of the specified virtual key.</returns>
    [DllImport("user32.dll", SetLastError = true)]
    private static extern short GetKeyState(uint nVirtKey);

    /// <summary>Retrieves a module handle for the specified module. The module must have been loaded by the calling process</summary>
    /// <param name="lpModuleName">The name of the loaded module (either a .dll or .exe file).</param>
    /// <returns>
    /// If the function succeeds, the return value is a handle to the specified module.
    /// If the function fails, the return value is NULL.
    /// </returns>
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("user32.dll")]
    public static extern bool PostMessage(IntPtr hWnd, int Msg, uint wParam, int lParam);
  }

  /// <summary>LowLevelKeyboardProc for callbacks from the OS.</summary>
  internal delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

  /// <summary>A SafeHandle for windows hook handles.</summary>
  [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
  internal sealed class SafeWindowsHookHandle : SafeHandleZeroOrMinusOneIsInvalid
  {
    /// <summary>Initializes the SafeWindowsHookHandle.</summary>
    private SafeWindowsHookHandle() : base(true) { }

    /// <summary>Releases the handle.</summary>
    /// <returns>true on success; false, otherwise.</returns>
    protected override bool ReleaseHandle()
    {
      return UnhookWindowsHookEx(handle);
    }

    /// <summary>The UnhookWindowsHookEx function removes a hook procedure installed in a hook chain by the SetWindowsHookEx function.</summary>
    /// <param name="hhk">Handle to the hook procedure</param>
    /// <returns>true on success; otherwise, false.</returns>
    [SuppressUnmanagedCodeSecurity]
    [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    /// <summary>The SetWindowsHookEx function installs an application-defined hook procedure into a hook chain.</summary>
    /// <param name="idHook">Specifies the type of hook procedure to be installed.</param>
    /// <param name="lpfn">Pointer to the hook procedure.</param>
    /// <param name="hMod">Handle to the DLL containing the hook procedure pointed to by the lpfn parameter.</param>
    /// <param name="dwThreadId">Specifies the identifier of the thread with which the hook procedure is to be associated.</param>
    /// <returns>If the function succeeds, the return value is the handle to the hook procedure; otherwise, 0.</returns>
    [DllImport("user32.dll", SetLastError = true)]
    public static extern SafeWindowsHookHandle SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    /// <summary>The CallNextHookEx function passes the hook information to the next hook procedure in the current hook chain.</summary>
    /// <param name="hhk">Handle to the current hook.</param>
    /// <param name="nCode">Specifies the hook code passed to the current hook procedure.</param>
    /// <param name="wParam">Specifies the wParam value passed to the current hook procedure.</param>
    /// <param name="lParam">Specifies the lParam value passed to the current hook procedure.</param>
    /// <returns>This value is returned by the next hook procedure in the chain.</returns>
    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr CallNextHookEx(SafeWindowsHookHandle hhk, int nCode, IntPtr wParam, IntPtr lParam);
  }
}