# HDMI-CEC: driving the front end from the television's own remote

Two different questions hide behind "CEC support", and they have completely different costs:

- **Stufe A - answering the remote.** The television's remote already speaks to the machine when the machine asks
  the HDMI cable for it: the buttons arrive as ordinary keys. Answering them is a matter of key bindings, which
  is what this front end does since the change described in ["What answers today"](#what-answers-today) below.
  Nothing here needs a CEC library, and nothing here turns the television on or switches its input.
- **Stufe B - speaking CEC.** Setting the input source, powering the set up and down, routing the sound to a
  receiver over ARC, reading the TV's real name: that means driving the CEC bus itself, which needs a library,
  a driver and a platform that lets an application touch the bus at all. The design and the per-platform reality
  of that are the second half of this document. It is a plan, not a build.

Everything below was read out of a source, named at the point of use. Where a platform's behaviour could not be
measured from here it says so instead of guessing.

## Stufe A - answering the remote

### How a CEC button becomes a key on Linux

The kernel owns the CEC bus (`drivers/media/cec/`), and when it registers an adapter it also registers a remote
for it: `cec_register_adapter()` sets `adap->rc->map_name = RC_MAP_CEC` (`drivers/media/cec/core/cec-core.c`),
and `RC_MAP_CEC` is the table in `drivers/media/rc/keymaps/rc-cec.c`. That table is compiled into `rc-core`
whenever `CONFIG_MEDIA_CEC_RC` is set, so a CEC remote's button is not a special event type anywhere above the
input layer - it is an ordinary `EV_KEY`, exactly like a USB keyboard's, and any tool that reads evdev reads it.

The codes the front end cares about, straight out of `rc-cec.c`:

| CEC user control code | kernel key | what this front end makes of it |
| --- | --- | --- |
| `0x01`-`0x04` direction keys | `KEY_UP`/`DOWN`/`LEFT`/`RIGHT` | the d-pad ring and the walls, as always |
| `0x00`, `0x2b` select, enter | `KEY_OK`, `KEY_ENTER` | `KeyAction.Select` - the plate the viewer stands on is pressed |
| `0x09` root menu | `KEY_ROOT_MENU` (618) | nothing anywhere: the code is above what X11 can carry at all, and no key of its own is bound |
| `0x0b` contents menu | `KEY_MENU` (139) | the screen's own menu - on X11 the button comes over as the keyboard's Menu key, which Avalonia reports as `Apps`, and `Apps` is one of the keys the screen-menu action answers |
| `0x0d` exit | `KEY_EXIT` (174) | back where the system delivers it; X11 names neither the key nor the scan code, so there it needs a remap |
| `0x11` contents/context menu | `KEY_CONTEXT_MENU` (438) | the menu of the screen that has one, where the windowing system delivers it; X11 delivers neither name |
| `0x35` display information | `KEY_INFO` | the screen's own menu (`KeyAction.MediaMenu`) - where the windowing system delivers it at all, see below |
| `0x41`, `0x42` volume up, down | `KEY_VOLUMEUP`, `KEY_VOLUMEDOWN` | the room's sound (`KeyAction.VolumeUp`/`Down`) |
| `0x43`, `0x65` mute, mute toggle | `KEY_MUTE` | the room's silence (`KeyAction.Mute`) |
| `0x61` pause-play | `KEY_PLAYPAUSE` | pause and resume (`KeyAction.PlayPause`) |
| `0x45`, `0x64` stop, stop function | `KEY_STOPCD` | stop the film and leave the screen (`KeyAction.Stop`) |
| `0x6c`, `0x6d` power off, power on | `KEY_SLEEP`, `KEY_WAKEUP` | unanswered today; a whole-machine question, not a screen's |
| `0x71`-`0x74` blue, red, green, yellow | `KEY_BLUE`, `KEY_RED`, `KEY_GREEN`, `KEY_YELLOW` | unanswered today; the reference's teletext colours have no equivalent screen here |

### What answers today

Keys are no longer compared against constants in the screens; they go through the binding table in
`Source/Frontend/MediaPortal.Frontend/Settings/KeyBindings.cs`, whose defaults now carry the remote's own keys
beside the keyboard's letters: `MediaPlayPause`/`Play`, `MediaStop`, `MediaInfo`/`MediaMenu`/`Apps`/`I`, and
`VolumeUp`, `VolumeDown`, `VolumeMute` - and, since the pass that widened the remote from the player to the
whole navigation, `Select` under the names `Return`, `Enter`, `Space` and `Select`. A viewer whose remote reports
a button as something else edits `keybindings.json` rather than recompiling, which is the whole point of the
table.

Three places stopped naming keys themselves as part of this:

- `Views/ShellWindow.cs` answers the room's sound keys from the table (`IsRoomKey`), and gained the silence key,
  which it did not have: the key says "Muted" or the number the silence hid, through the superlayer the volume
  keys already use.
- `Views/NowPlayingView.axaml.cs` asks the table for its menu key (it compared `Key.I`) and for its stop key (it
  compared `Key.MediaStop`), and it answers the play/pause key, which no key handler had held at all while a
  film ran - the button on the bar had it, the remote's most used button did not.
- Every screen asks the table of the *event* (`Is(KeyAction, KeyEventArgs)`) instead of `args.Key`. That is not
  cosmetic, and the next section is why.

### The whole navigation, not only the player

A television's remote is asked for more than transport: whoever sits on the sofa walks the walls, opens a tile,
answers a dialog and leaves a screen again with the same handful of buttons. Those buttons are therefore the
same actions the keyboard already drives, and since this change every one of them is an action of the table
rather than a key a screen compares by hand:

Legend for every table in this section: **yes** the button arrives and the action answers · **only with a
remap** the windowing system does not name it as it stands, and a `keybindings.json`, an `hwdb` entry or a
driver is needed · **no** nothing arrives to answer. *(m)* measured on the machine this was written on, with
the running front end · *(s)* read out of a source named at the point of use.

| the button on the set | kernel key (`rc-cec.c`) | Windows (`KeyInterop.cs`) | Linux/X11 (`X11KeyTransform.cs`) | Android (`AndroidKeyboardDevice.cs`) | the action | what the viewer sees |
| --- | --- | --- | --- | --- | --- | --- |
| the four direction keys `0x01`-`0x04` | `KEY_UP`/`DOWN`/`LEFT`/`RIGHT` | yes, `VK_UP` … *(s)* | yes, `X11Key.Left` → `Key.Left` *(s)* | yes, `DpadUp` … → `Key.Up` … *(s)* | the ring itself, deliberately unmapped | the caret walks wall, rail, list |
| enter `0x2b` | `KEY_ENTER` | yes, `VK_RETURN` *(m: opened the library)* | yes, `X11Key.Return`/`KP_Enter` → `Key.Return` *(s)* | yes, as Space *(s)* | `KeyAction.Select` | the tile, plate or row stood on is pressed |
| OK `0x00` | `KEY_OK` (352) | only with a remap - most adapters send `VK_RETURN` instead | no, above the 255 keycode ceiling | yes, `DpadCenter` → **`Key.Space`** *(s)* | `KeyAction.Select` | the same press, one key earlier |
| back / exit `0x0d` | `KEY_EXIT` (174), `KEY_BACK` (158) | yes, `VK_ESCAPE` *(m: back to the wall)* | no - neither `XF86XK_Exit` nor `XF86XK_Back` is in Avalonia's table; the keyboard's Escape yes *(s)* | yes, `Back` → `Key.Escape` *(s)* | `KeyAction.Back` | an open menu goes away, then the screen, then the application |
| menu / root menu `0x09` | `KEY_ROOT_MENU` (618) | no | no | no | - | nothing yet, see the gaps below |
| contents / context menu `0x0b`, `0x11` | `KEY_MENU` (139), `KEY_CONTEXT_MENU` (438) | yes, `VK_APPS` → `Apps` *(s)* | yes, `X11Key.Menu` → `Apps` *(s)*; the 438 one no | no | `KeyAction.MediaMenu` | the screen's own menu |
| display information `0x35` | `KEY_INFO` (358) | no | no, above the ceiling | no | `KeyAction.MediaMenu` | only where a driver hands the button over as a named key |
| play `0x44`/`0x60` | `KEY_PLAYCD` (200) | only with a remap | no - in neither of Avalonia's tables | only with a remap | `KeyAction.PlayPause` | a remote that sends Play Function rather than Pause-Play is silent |
| pause-play `0x61` | `KEY_PLAYPAUSE` (164) | yes, `VK_MEDIA_PLAY_PAUSE` *(s)* | no logical key, so the physical key 0xAC answers *(s)* | yes, `MediaPlayPause`, and `MediaPlay` → `Play` *(s)* | `KeyAction.PlayPause` | the film pauses and resumes |
| stop `0x45`, `0x64` | `KEY_STOPCD` (166) | yes, `VK_MEDIA_STOP` *(s)* | no logical key, physical 0xAE answers *(s)* | yes, `MediaStop` *(s)* | `KeyAction.Stop` | the film ends and its screen leaves |
| volume up, down `0x41`, `0x42` | `KEY_VOLUMEUP` (115), `KEY_VOLUMEDOWN` (114) | yes *(m: the reading dimmed the wall from 104 to 52)* | no logical key, physical 0x7B/0x7A answer *(s)* | yes, `VolumeUp`/`VolumeDown` *(s)* | `KeyAction.VolumeUp`/`Down` | the room's sound, with the number read aloud over the picture |
| mute `0x43`, `0x65` | `KEY_MUTE` (113) | yes *(m: "Muted", and the room went silent)* | no logical key, physical 0x79 answers *(s)* | no - `Key.VolumeMute` is one of the commented-out entries | `KeyAction.Mute` | "Muted", or the number the silence hid |
| power off, power on `0x6c`, `0x6d` | `KEY_SLEEP` (142), `KEY_WAKEUP` (143) | - | - | `Sleep` arrives | none - deliberately | a whole-machine question, and a Stufe B one |
| blue, red, green, yellow `0x71`-`0x74` | `KEY_BLUE` … `KEY_YELLOW` (398-401) | no | no, above the ceiling | no | none yet | nothing, see the gaps below |

The letter keys are the other half of the navigation on a keyboard and the half a television remote cannot
speak, so they are worth writing down next to it:

| the action | its keys | Windows | Linux/X11 | Android |
| --- | --- | --- | --- | --- |
| `KeyAction.Select` | Return, Enter, Space, Select | yes | yes | yes - as Space |
| `KeyAction.Back` | Escape, Back | yes | yes | yes - as Escape |
| `KeyAction.Menu` | F16 | only with a driver that sends `VK_F16` | no - Avalonia's X11 table has no F16, the real menu key lands on `Apps` | no |
| `KeyAction.MediaMenu` | I, Apps, MediaInfo, MediaMenu | yes (`Apps`, `I`) | yes (`Apps` from the menu key, `I`) | only `I`, and only over a paired keyboard |
| `KeyAction.Details`, `Find`, `Watched`, `View`, `Media` | D, F, W, V, M | yes *(m: the shelf's layout and picture keys were watched changing on screen)* | yes | only over a paired keyboard |
| `KeyAction.VolumeUp`/`Down`, `Mute` | VolumeUp/Down/Mute, plus OemPlus/OemMinus | yes *(m: the number and "Muted" both appeared)* | yes, through the physical key | yes, except Mute |

One name in that table bites on X11: Avalonia maps `X11Key.BackSpace` to `Key.Back`, so there the backspace key
is the back action. That is deliberate on the start screen, which clears a typed PIN digit with it, and it is
worth knowing everywhere else - a viewer erasing a word in a field whose caret has left the field steps back a
screen.

`KeyAction.Select` is new with this pass, and the reason for it is the Android column: Android reports the centre
of its D-pad as the space bar, so a front end that compared `Enter` in a hundred places was unreadable from an
Android television, however correct each of those places was on its own. The screens that now ask the action are
the wall and its rails (`Views/HomeView.axaml.cs`), the shelf's command rail and its "play this row" press
(`Views/MediaBrowserView.axaml.cs`), the title's and the programme's dialogs
(`Views/MediaTitleActionsView.axaml.cs`, `Views/TvProgramActionsView.axaml.cs`), the notification question
(`Views/NotificationsView.axaml.cs`), the way-out menu (`Views/CloseView.axaml.cs`) and the settings wall
(`Views/SettingsView.axaml.cs`). `Views/NewsView.axaml.cs` and `Views/WeatherView.axaml.cs` ask the back action
where they had compared `Key.Escape` themselves.

Two places deliberately keep a bare `Enter` rather than the action, both because a hand is typing: the shelf's
search field answers Enter with "search for this" while the space bar is still part of the word being spelled,
and a field that opens the on-screen keyboard on Enter (`Views/VirtualKeyboardService.cs`) cannot treat the
space bar as a command either. `ADialogIsAnsweredByWhicheverKeyARemotesOkButtonReports`
holds the first of these boundaries by proving the dialog answers Return, Space and Select alike.

The remap was tried on the running front end rather than only in a test: with `MP2_KEYBINDINGS_FILE` naming a
file that says `{"Bindings": {"Select": ["F7"]}}`, the wall stopped opening for the enter key and for the space
bar and opened for F7 instead, which is the promise the table exists for - a television whose OK button reports
as something nobody thought of is fitted with a line of JSON.

What is honestly still open, measured rather than assumed:

- **The colour buttons have no action.** Nothing in this front end is a colour-key screen, and no platform can
  deliver the key to an Avalonia window today: the kernel's `KEY_RED` (0x18e, 398) to `KEY_BLUE` (0x191, 401)
  are above the code an X11 keycode can carry at all (plus eight they would be 406 to 409, over the 255 the
  protocol allows), Android's `KEYCODE_RED` … `KEYCODE_BLUE` are not in Avalonia's table, and Windows has no
  virtual key for them. Binding them would promise a button that answers nowhere.
- **The root-menu button arrives nowhere.** `KEY_ROOT_MENU` is 618, above the 255 keycode ceiling X11 allows,
  unmapped by Avalonia on Android, and without a virtual key on Windows. A viewer who wants that button should
  remap it into range (`ir-keytable`/`systemd-hwdb` on Linux, the desktop's shortcuts on X11) onto a key the
  table already answers.
- **Collection posters on the shelf answer Enter alone** (`Views/MediaBrowserView.axaml.cs`, the switch over
  `args.Key` in `StandsOnTrackCard`). An Android hand's OK - which is Space - leaves a collection card unopened;
  the row's own plate beside it still answers, so the film is reachable, one press further away.
- **Opening the on-screen keyboard from a field** is an Enter-or-menu question, and Android maps neither of the
  two menu spellings. Once the keyboard is up its plates answer the D-pad and its OK, because Avalonia's own
  buttons answer Space.

### What the windowing systems make of those keys

This is where "the OS maps CEC keys to keys" stops being the whole truth. Avalonia's own tables decide what a
key event carries, and they differ per platform:

- **Windows** maps the media and volume virtual keys: `KeyInterop.cs` under `src/Windows/Avalonia.Win32/Input/`
  pairs `Key.VolumeMute`/`Key.VolumeDown`/`Key.VolumeUp` with `VK_VOLUME_MUTE`/`_DOWN`/`_UP`, and the media keys
  likewise. A remote button that reaches Windows as one of those virtual keys arrives named. Measured rather
  than trusted, on the machine this was written on: sending `VK_VOLUME_UP` and `VK_VOLUME_MUTE` to the running
  front end dims the picture and puts the reading at the foot of the screen (the wall's mean brightness fell
  from 104 to 52 with the layer up, which is the scrim's own fifty percent), and the mute key silenced the room
  and said "Muted". What Avalonia reports as the *physical* key for those consumer keys is nonsense (`B` for
  volume up, `D` for mute), so the logical key is the one to ask.
- **Android** maps `Keycode.VolumeUp`/`VolumeDown` to `Key.VolumeUp`/`VolumeDown` and `Keycode.MediaPlayPause`,
  `MediaStop`, `MediaNext`, `MediaPrevious` to their `Key.Media*` equivalents
  (`src/Android/Avalonia.Android/Platform/Input/AndroidKeyboardDevice.cs`). `Key.VolumeMute` is one of the
  entries Avalonia leaves commented out there, so a TV remote's silence key is not answered on Android until it
  is remapped or Avalonia maps it.
- **X11** is the interesting one, because that is what a Linux media machine runs. Avalonia's
  `src/Avalonia.X11/X11KeyTransform.cs` leaves `Key.VolumeMute`, `Key.VolumeDown`, `Key.VolumeUp`,
  `Key.MediaNextTrack`, `Key.MediaPreviousTrack`, `Key.MediaStop` and `Key.MediaPlayPause` **commented out** of
  its keysym table - so a CEC remote's transport and volume buttons reach an Avalonia window with `Key.None`.
  The same file's *physical* table, which is indexed by the X keycode (the evdev key code plus eight), does know
  them: `0xAC PhysicalKey.MediaPlayPause`, `0xAE PhysicalKey.MediaStop`, `0x7B PhysicalKey.AudioVolumeUp`,
  `0x7A PhysicalKey.AudioVolumeDown`, `0x79 PhysicalKey.AudioVolumeMute`. Those are exactly `KEY_PLAYPAUSE`
  (164), `KEY_STOPCD` (166), `KEY_VOLUMEUP` (115), `KEY_VOLUMEDOWN` (114) and `KEY_MUTE` (113) plus eight.

So the buttons are there on X11, one level down. `KeyBindings.Is(KeyAction, KeyEventArgs)` therefore asks the
physical key through Avalonia's own `PhysicalKey.ToQwertyKey()` - which translates
`PhysicalKey.AudioVolumeUp` to `Key.VolumeUp` and the media keys to theirs - but **only when no logical key came
over at all**, so a layout where the letter under a finger means something else is never second-guessed. One
spelling in `keybindings.json` then answers a windowing system that names the button and one that does not, and
`TestNowPlayingWindow.ARemoteWhoseButtonsTheWindowingSystemDoesNotNameStillAnswers` holds that to the fire.

What is still lost on X11, honestly: the X keycode is the kernel's key code plus eight and X has no keycode
above 255, so `KEY_INFO` (358) cannot be delivered there at all - a set whose Info button sends `0x35` opens
nothing through X11, whatever this front end binds. `KEY_PLAYCD` (200) and `KEY_PAUSECD` (201) do get a keycode
(208 and 209) but appear in neither of Avalonia's tables, so a remote whose play button sends the *Play
Function* (`0x44`/`0x60` → `KEY_PLAYCD`) rather than *Pause-Play* (`0x61`) is silent. Both cases are remappable
with `setxkbmap`/`xmodmap` or the desktop's own shortcut settings onto a key this front end hears, and both are
answered properly by Stufe B, which does not go near the windowing system at all. `KEY_SLEEP` (142) and
`KEY_WAKEUP` (143) are deliberately unanswered as keys: powering the set is a Stufe B question.

### Testing it without a television

The bindings and the answers they reach are covered by `Tests/Tests.Frontend/TestKeyBindings.cs` (the defaults
answer the names the kernel produces, an edited file refits them, an unnamed button's physical key counts) and
by `Tests/Tests.Frontend/TestNowPlayingWindow.cs` (the same remote drives the buttons, the film and the sound,
named and unnamed). On a machine with a real CEC adapter, what to look for is:

```sh
dmesg | grep -i cec          # which adapter the kernel found
ir-keytable                  # which remotes it registered, "cec" among them
ir-keytable -s cec -t        # then press the remote: the kernel key names print here
```

Whatever `ir-keytable` prints is the name to put into `keybindings.json` if the front end does not answer it.
A USB dongle (Pulse-Eight and clones) appears as `/dev/ttyACM*` rather than as a remote until
`inputattach --cec /dev/ttyACM0` (v4l-utils; the CEC line discipline needs 1.6.0 or newer) turns it into
`/dev/input/eventN`, and that step is also what makes its buttons reach the front end.

## Stufe B - speaking CEC

### Where the boundary goes

A second assembly, the way the player already sits behind one: `MediaPortal.Frontend.Player` keeps libVLC out of
the front end's own assembly, and `MediaPortal.Frontend.Hardware` would keep libCEC - and the platform
decisions below - out of it the same way. The front end would ask one capability interface, in the present tense
of what a viewer can do:

```csharp
public interface ITvControl
{
  bool CanDriveTheSet { get; }        // false, with a reason, on every machine that cannot
  string Reason { get; }              // said out loud through the superlayer; never a crash
  bool WakeSet();
  bool SleepSet();
  bool SetActiveSource();
  bool RouteSoundToReceiver();
  string NameOfTheSet { get; }
}
```

Backends, chosen once at start and probed rather than assumed:

| backend | machine | what it can honestly do |
| --- | --- | --- |
| `LibCecBackend` | Linux with a kernel CEC adapter (`/dev/cecN`), Windows with a Pulse-Eight-style USB adapter, Raspberry Pi, Odroid C2, Exynos and NXP TDA995x boards | the full list: power, input, ARC, the TV's name |
| `AndroidTvBackend` | Android TV | reactive only for an ordinary application: audio-device plug events, keep-awake, following the set rather than directing it |
| `NullBackend` | macOS, a Windows machine without an adapter, any machine whose bus is busy | nothing, and one sentence saying which of those it is |
| `FakeBackend` | the tests | the whole interface, recorded |

Composition belongs where the front end already composes its readers - the `CoverReader`/`ClearArtReader`
delegates in `App.axaml.cs` - so that a screen asks a question of an object that is always there and answers
"no" with a reason when the hardware is not. `CA1416` staying an error is what keeps a Linux-only call from
being reached from a Windows code path, and the assembly boundary is what keeps that promise checkable.

### The facts that decide the shape

- **libCEC is the only library worth building on**, and it is GPL-2.0-or-later, dual-licensed with a commercial
  option from Pulse-Eight. MediaPortal 2 is GPL, so linking it into the client is not a problem for this
  repository - but it is a decision that has to be said out loud in the README, and it is the reason a
  separate assembly (and the option of talking to a `cec-client` process instead of linking the library) is
  worth the extra hour.
- **A NuGet binding exists**: `LibCecSharp` (net8.0 class library, published since the 8.1.6 series). It binds
  the *native* library, which has to be installed on the machine, so a missing `libcec` is a start-up
  condition to speak about, not an exception to look at.
- **Linux no longer needs the vendor library path**: libCEC builds against the mainline kernel API with
  `-DHAVE_LINUX_API=1`, which is what a distribution kernel with `CONFIG_CEC_CORE` gives. The kernel side is
  also what makes Stufe A work at all, so a machine that cannot show the remote's keys in `ir-keytable -s cec`
  is a machine whose kernel has no CEC adapter, and no library will change that.
- **One adapter, one owner.** libCEC claims the device, so a `cec-daemon` or an user's `cec-client` already
  running means the front end gets nothing; the plan has to either own the device and say so, or talk to a
  daemon that owns it. This is the most likely support request in the whole feature.
- **Android does not let an application drive the bus.** `HdmiControlService` gates every CEC operation behind
  `static final String PERMISSION = "android.permission.HDMI_CEC"` (AOSP, `services/core/java/com/android/server/hdmi/`),
  and that permission is signature/privileged: it is granted to the system image's own apps. `HdmiControlManager`
  is not part of the public SDK reference. A sideloaded MediaPortal on an Android TV can therefore follow the
  set (audio device plug/unplug broadcasts, keeping itself awake, reacting to the remote through Stufe A) but
  cannot wake it, put it to sleep or take the input. Getting those on Android means either a privileged build
  for a specific device, or an companion - a TV-side app or a CEC-capable HDMI switch on the network - which is
  a separate piece of software with its own plan.
- **Television behaviour varies more than the standard allows.** libCEC's own README carries warnings about
  specific brands' power and input-switching quirks. Any wording the UI offers has to be "asked the set", not
  "the set is on", and the failure has to be visible.

### Milestones, each with a way to see it is done

1. **B0 - the seam without the hardware.** `MediaPortal.Frontend.Hardware` with `ITvControl`, `NullBackend` and
   `FakeBackend`, wired in `App.axaml.cs`; a menu entry that reports what the machine can do and why not. Done
   when a machine with no CEC at all says so in a sentence and nothing else changes.
2. **B1 - Linux, read only.** `LibCecBackend` listing the logical addresses and the set's name. Done when a
   screen shows the television's real name where the front end currently has no such thing, and says
   "no CEC adapter the kernel knows" on a machine without one.
3. **B2 - Linux, the three questions that matter.** Wake, sleep, take the active source; plus the viewer's
   switch in settings (default: off, because taking a input over a viewer's console is not a default anyone
   should get). Done when a `cec-ctl --monitor` trace and the screen agree.
4. **B3 - Windows with an adapter.** The same three questions through the USB adapter, with the "another
   program holds the adapter" answer written out. Done when it is tested on such a machine - which needs one.
5. **B4 - Android.** Reactive only, as the platform allows: audio routing follow-up, keep-awake while a film
   runs, and the remote's keys answered through Stufe A on `Avalonia.Android`. Done when measured on a device,
   since nothing above that paragraph was measured from here.

### Risks worth naming before the work

| risk | what it looks like | what the plan does about it |
| --- | --- | --- |
| the adapter is busy | the front end says nothing while the TV answers another program | probe once at start, name the holder in the message, and consider a daemon as the owner |
| a set that ignores or misapplies commands | the film wakes a television that stays black | every claim worded as "asked", never as done; a viewer-visible switch with a short timeout |
| GPL linking question | a downstream build with a license that cannot take GPL | the assembly boundary, and a `cec-client`-talking variant as the alternative |
| Android expectation | "why can't it turn my TV on" | say what the platform grants, in the settings screen, before the viewer tries |
| Avalonia's key gaps | a button that works on Windows and not on X11 | the physical-key fallback above, the JSON escape hatch, and the two lost codes named in this document |

## Sources

- `drivers/media/cec/core/cec-core.c`, `drivers/media/rc/keymaps/rc-cec.c`, `include/uapi/linux/input-event-codes.h`
  (torvalds/linux, read 2026-11-06) - the CEC remote, its keymap, and the key codes.
- `src/Avalonia.X11/X11KeyTransform.cs`, `src/Avalonia.Base/Input/PhysicalKeyExtensions.cs`,
  `src/Windows/Avalonia.Win32/Input/KeyInterop.cs`,
  `src/Android/Avalonia.Android/Platform/Input/AndroidKeyboardDevice.cs` (AvaloniaUI/Avalonia, read 2026-11-06) -
  what a key event carries on each platform, and where the media keys are missing.
- `services/core/java/com/android/server/hdmi/HdmiControlService.java` (AOSP) - the `HDMI_CEC` permission gate.
- libCEC's repository and README, and the `LibCecSharp` package on nuget.org - hardware support, the kernel API
  switch, and the GPL/commercial dual licensing.
