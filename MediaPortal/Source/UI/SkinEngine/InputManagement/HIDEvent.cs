#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using SharpLib.Hid;

namespace MediaPortal.UI.SkinEngine.InputManagement
{
  [Flags]
  public enum RawMouseButtonFlags : ushort
  {
    None,
    LeftButtonDown = 0x0001,
    LeftButtonUp = 0x0002,
    RightButtonDown = 0x0004,
    RightButtonUp = 0x0008,
    MiddleButtonDown = 0x0010,
    MiddleButtonUp = 0x0020,
    Button4Down = 0x0040,
    Button4Up = 0x0080,
    Button5Down = 0x0100,
    Button5Up = 0x0200,
    MouseWheel = 0x0400,
    MouseHorizontalWheel = 0x0800,
  }

  public enum UsagePage : ushort
  {
    Undefined,
    GenericDesktopControls = 0x0001,
    SimulationControls = 0x0002,
    VirtualRealityControls = 0x0003,
    SportControls = 0x0004,
    GameControls = 0x0005,
    GenericDeviceControls = 0x0006,
    Keyboard = 0x0007,
    LightEmittingDiode = 0x0008,
    Button = 0x0009,
    Ordinal = 0x000A,
    Telephony = 0x000B,
    Consumer = 0x000C,
    Digitizer =  0x000D,
    PhysicalInterfaceDevice = 0x000F,
    Unicode = 0x0010,
    AlphaNumericDisplay = 0x0014,
    MedicalInstruments = 0x0040,
    MonitorPage0 = 0x0080,
    MonitorPage1 = 0x0081,
    MonitorPage2 = 0x0082,
    MonitorPage3 = 0x0083,
    PowerPage0 = 0x0084,
    PowerPage1 = 0x0085,
    PowerPage2 = 0x0086,
    PowerPage3 = 0x0087,
    BarCodeScanner = 0x008C,
    Scale = 0x008D,
    MagneticStripeReader = 0x008E,
    ReservedPointOfSale = 0x008F,
    CameraControl = 0x0090,
    Arcade = 0x0091,
    WindowsMediaCenterRemoteControl = 0xFFBC,
    TerraTecRemote = 0xFFCC,
  }

  public enum WindowsMediaCenterRemoteControl : ushort
  {
    Undefined,
    GreenStart = 0x000D,
    DvdMenu = 0x0024,
    LiveTv = 0x0025,
    Zoom = 0x0027,
    Eject = 0x0028,
    ClosedCaptioning = 0x002B,
    NetworkSelection = 0x002C,
    SubAudio = 0x002D,
    Ext0 = 0x0032,
    Ext1 = 0x0033,
    Ext2 = 0x0034,
    Ext3 = 0x0035,
    Ext4 = 0x0036,
    Ext5 = 0x0037,
    Ext6 = 0x0038,
    Ext7 = 0x0039,
    Ext8 = 0x003A,
    Extras = 0x003C,
    ExtrasApp = 0x003D,
    Channel10 = 0x003E,
    Channel11 = 0x003F,
    Channel12 = 0x0040,
    ChannelInfo = 0x0041,
    ChannelInput = 0x0042,
    DvdTopMenu = 0x0043,
    Tv = 0x0046,
    Music = 0x0047,
    RecordedTv = 0x0048,
    Pictures = 0x0049,
    Videos = 0x004A,
    DvdAngle = 0x004B,
    DvdAudio = 0x004C,
    DvdSubtitle = 0x004D,
    Display = 0x004F,
    FmRadio = 0x0050,
    PrestigoA = 0x0052,
    PrestigoB = 0x0053,
    PrestigoC = 0x0054,
    PrestigoD = 0x0055,
    Teletext = 0x005A,
    TeletextRed = 0x005B,
    TeletextGreen = 0x005C,
    TeletextYellow = 0x005D,
    TeletextBlue = 0x005E,
    VideoSelection = 0x0061,
    TvPower = 0x0065,
    Messenger = 0x0069,
    Kiosk = 0x006A,
    Ext11 = 0x006F,
    BlueRayTool = 0x0078,
    Ext9 = 0x0080,
    Ext10 = 0x0081,
  }

  public enum ConsumerControl : ushort
  {
    Undefined,
    ConsumerControl = 0x0001,
    NumericKeyPad = 0x0002,
    ProgrammableButtons = 0x0003,
    Microphone = 0x0004,
    Headphone = 0x0005,
    GraphicEqualizer = 0x0006,
    ThinkPadMicrophoneMute = 0x0010,
    ThinkPadVantage = 0x0011,
    ThinkPadSystemLock = 0x0012,
    ThinkPadPowerManagement = 0x0013,
    ThinkPadWirelessNetwork = 0x0014,
    ThinkPadCamera = 0x0015,
    ThinkPadDisplayScheme = 0x0016,
    ThinkPadMouseProperties = 0x0017,
    ThinkPadEject = 0x0018,
    ThinkPadSystemHibernate = 0x0019,
    ThinkPadBrightnessIncrement = 0x001A,
    ThinkPadBrightnessDecrement = 0x001B,
    ThinkPadFullscreenMagnifier = 0x001D,
    Plus10 = 0x0020,
    Plus100 = 0x0021,
    AmPm = 0x0022,
    Power = 0x0030,
    Reset = 0x0031,
    Sleep = 0x0032,
    SleepAfter = 0x0033,
    SleepMode = 0x0034,
    Illumination = 0x0035,
    FunctionButtons = 0x0036,
    Menu = 0x0040,
    MenuPick = 0x0041,
    MenuUp = 0x0042,
    MenuDown = 0x0043,
    MenuLeft = 0x0044,
    MenuRight = 0x0045,
    MenuEscape = 0x0046,
    MenuValueIncrease = 0x0047,
    MenuValueDecrease = 0x0048,
    DataOnScreen = 0x0060,
    ClosedCaption = 0x0061,
    ClosedCaptionSelect = 0x0062,
    VcrTv = 0x0063,
    BroadcastMode = 0x0064,
    Snapshot = 0x0065,
    Still = 0x0066,
    LenovoBrightnessIncrement = 0x006F,
    LenovoBrightnessDecrement = 0x0070,
    Selection = 0x0080,
    AssignSelection = 0x0081,
    ModeStep = 0x0082,
    RecallLast = 0x0083,
    EnterChannel = 0x0084,
    OrderMovie = 0x0085,
    Channel = 0x0086,
    MediaSelection = 0x0087,
    MediaSelectComputer = 0x0088,
    MediaSelectTv = 0x0089,
    MediaSelectWww = 0x008A,
    MediaSelectDvd = 0x008B,
    MediaSelectTelephone = 0x008C,
    MediaSelectProgramGuide = 0x008D,
    MediaSelectVideoPhone = 0x008E,
    MediaSelectGames = 0x008F,
    MediaSelectMessages = 0x0090,
    MediaSelectCd = 0x0091,
    MediaSelectVcr = 0x0092,
    MediaSelectTuner = 0x0093,
    Quit = 0x0094,
    Help = 0x0095,
    MediaSelectTape = 0x0096,
    MediaSelectCable = 0x0097,
    MediaSelectSatellite = 0x0098,
    MediaSelectSecurity = 0x0099,
    MediaSelectHome = 0x009A,
    MediaSelectCall = 0x009B,
    ChannelIncrement = 0x009C,
    ChannelDecrement = 0x009D,
    MediaSelectSap = 0x009E,
    VcrPlus = 0x00A0,
    Once = 0x00A1,
    Daily = 0x00A2,
    Weekly = 0x00A3,
    Monthly = 0x00A4,
    Play = 0x00B0,
    Pause = 0x00B1,
    Record = 0x00B2,
    FastForward = 0x00B3,
    Rewind = 0x00B4,
    ScanNextTrack = 0x00B5,
    ScanPreviousTrack = 0x00B6,
    Stop = 0x00B7,
    Eject = 0x00B8,
    RandomPlay = 0x00B9,
    SelectDisc = 0x00BA,
    EnterDisc = 0x00BB,
    Repeat = 0x00BC,
    Tracking = 0x00BD,
    TrackNormal = 0x00BE,
    SlowTracking = 0x00BF,
    FrameForward = 0x00C0,
    FrameBack = 0x00C1,
    Mark = 0x00C2,
    ClearMark = 0x00C3,
    RepeatFromMark = 0x00C4,
    ReturnToMark = 0x00C5,
    SearchMarkForward = 0x00C6,
    SearchMarkBackwards = 0x00C7,
    CounterReset = 0x00C8,
    ShowCounter = 0x00C9,
    TrackingIncrement = 0x00CA,
    TrackingDecrement = 0x00CB,
    StopEject = 0x00CC,
    PlayPause = 0x00CD,
    PlaySkip = 0x00CE,
    Volume = 0x00E0,
    Balance = 0x00E1,
    Mute = 0x00E2,
    Bass = 0x00E3,
    Treble = 0x00E4,
    BassBoost = 0x00E5,
    SurroundMode = 0x00E6,
    Loudness = 0x00E7,
    Mpx = 0x00E8,
    VolumeIncrement = 0x00E9,
    VolumeDecrement = 0x00EA,
    SpeedSelect = 0x00F0,
    PlaybackSpeed = 0x00F1,
    StandardPlay = 0x00F2,
    LongPlay = 0x00F3,
    ExtendedPlay = 0x00F4,
    Slow = 0x00F5,
    FanEnable = 0x0100,
    FanSpeed = 0x0101,
    LightEnable = 0x0102,
    LightIlluminationLevel = 0x0103,
    ClimateControlEnable = 0x0104,
    RoomTemperature = 0x0105,
    SecurityEnable = 0x0106,
    FireAlarm = 0x0107,
    PoliceAlarm = 0x0108,
    Proximity = 0x0109,
    Motion = 0x010A,
    DuressAlarm = 0x010B,
    HoldupAlarm = 0x010C,
    MedicalAlarm = 0x010D,
    BalanceRight = 0x0150,
    BalanceLeft = 0x0151,
    BassIncrement = 0x0152,
    BassDecrement = 0x0153,
    TrebleIncrement = 0x0154,
    TrebleDecrement = 0x0155,
    SpeakerSystem = 0x0160,
    ChannelLeft = 0x0161,
    ChannelRight = 0x0162,
    ChannelCenter = 0x0163,
    ChannelFront = 0x0164,
    ChannelCenterFront = 0x0165,
    ChannelSide = 0x0166,
    ChannelSurround = 0x0167,
    ChannelLowFrequencyEnhancement = 0x0168,
    ChannelTop = 0x0169,
    ChannelUnknown = 0x016A,
    SubChannel = 0x0170,
    SubChannelIncrement = 0x0171,
    SubChannelDecrement = 0x0172,
    AlternateAudioIncrement = 0x0173,
    AlternateAudioDecrement = 0x0174,
    ApplicationLaunchButtons = 0x0180,
    AppLaunchLaunchButtonConfigurationTool = 0x0181,
    AppLaunchProgrammableButtonConfiguration = 0x0182,
    AppLaunchConsumerControlConfiguration = 0x0183,
    AppLaunchWordProcessor = 0x0184,
    AppLaunchTextEditor = 0x0185,
    AppLaunchSpreadsheet = 0x0186,
    AppLaunchGraphicsEditor = 0x0187,
    AppLaunchPresentationApp = 0x0188,
    AppLaunchDatabaseApp = 0x0189,
    AppLaunchEmailReader = 0x018A,
    AppLaunchNewsreader = 0x018B,
    AppLaunchVoicemail = 0x018C,
    AppLaunchContactsAddressBook = 0x018D,
    AppLaunchCalendarSchedule = 0x018E,
    AppLaunchTaskProjectManager = 0x018F,
    AppLaunchLogJournalTimecard = 0x0190,
    AppLaunchCheckbookFinance = 0x0191,
    AppLaunchCalculator = 0x0192,
    AppLaunchAVCapturePlayback = 0x0193,
    AppLaunchLocalMachineBrowser = 0x0194,
    AppLaunchLanWanBrowser = 0x0195,
    AppLaunchInternetBrowser = 0x0196,
    AppLaunchRemoteNetworkingIspConnect = 0x0197,
    AppLaunchNetworkConference = 0x0198,
    AppLaunchNetworkChat = 0x0199,
    AppLaunchTelephonyDialer = 0x019A,
    AppLaunchLogon = 0x019B,
    AppLaunchLogoff = 0x019C,
    AppLaunchLogonLogoff = 0x019D,
    AppLaunchTerminalLockScreensaver = 0x019E,
    AppLaunchControlPanel = 0x019F,
    AppLaunchCommandLineProcessorRun = 0x01A0,
    AppLaunchProcessTaskManager = 0x01A1,
    AppLaunchSelectTaskApplication = 0x01A2,
    AppLaunchNextTaskApplication = 0x01A3,
    AppLaunchPreviousTaskApplication = 0x01A4,
    AppLaunchPreemptiveHaltTaskApplication = 0x01A5,
    AppLaunchIntegratedHelpCenter = 0x01A6,
    AppLaunchDocuments = 0x01A7,
    AppLaunchThesaurus = 0x01A8,
    AppLaunchDictionary = 0x01A9,
    AppLaunchDesktop = 0x01AA,
    AppLaunchSpellCheck = 0x01AB,
    AppLaunchGrammarCheck = 0x01AC,
    AppLaunchWirelessStatus = 0x01AD,
    AppLaunchKeyboardLayout = 0x01AE,
    AppLaunchVirusProtection = 0x01AF,
    AppLaunchEncryption = 0x01B0,
    AppLaunchScreenSaver = 0x01B1,
    AppLaunchAlarms = 0x01B2,
    AppLaunchClock = 0x01B3,
    AppLaunchFileBrowser = 0x01B4,
    AppLaunchPowerStatus = 0x01B5,
    AppLaunchImageBrowser = 0x01B6,
    AppLaunchAudioBrowser = 0x01B7,
    AppLaunchMovieBrowser = 0x01B8,
    AppLaunchDigitalRightsManager = 0x01B9,
    AppLaunchDigitalWallet = 0x01BA,
    AppLaunchInstantMessaging = 0x01BC,
    AppLaunchOemFeaturesTipsTutorialBrowser = 0x01BD,
    AppLaunchOemHelp = 0x01BE,
    AppLaunchOnlineCommunity = 0x01BF,
    AppLaunchEntertainmentContentBrowser = 0x01C0,
    AppLaunchOnlineShoppingBrowser = 0x01C1,
    AppLaunchSmartcardInformationHelp = 0x01C2,
    AppLaunchMarketMonitorFinanceBrowser = 0x01C3,
    AppLaunchCustomizedCorporateNewsBrowser = 0x01C4,
    AppLaunchOnlineActivityBrowser = 0x01C5,
    AppLaunchResearchSearchBrowser = 0x01C6,
    AppLaunchAudioPlayer = 0x01C7,
    GenericGuiApplicationControls = 0x0200,
    AppCtrlNew = 0x0201,
    AppCtrlOpen = 0x0202,
    AppCtrlClose = 0x0203,
    AppCtrlExit = 0x0204,
    AppCtrlMaximize = 0x0205,
    AppCtrlMinimize = 0x0206,
    AppCtrlSave = 0x0207,
    AppCtrlPrint = 0x0208,
    AppCtrlProperties = 0x0209,
    AppCtrlUndo = 0x021A,
    AppCtrlCopy = 0x021B,
    AppCtrlCut = 0x021C,
    AppCtrlPaste = 0x021D,
    AppCtrlSelectAll = 0x021E,
    AppCtrlFind = 0x021F,
    AppCtrlFindAndReplace = 0x0220,
    AppCtrlSearch = 0x0221,
    AppCtrlGoTo = 0x0222,
    AppCtrlHome = 0x0223,
    AppCtrlBack = 0x0224,
    AppCtrlForward = 0x0225,
    AppCtrlStop = 0x0226,
    AppCtrlRefresh = 0x0227,
    AppCtrlPreviousLink = 0x0228,
    AppCtrlNextLink = 0x0229,
    AppCtrlBookmarks = 0x022A,
    AppCtrlHistory = 0x022B,
    AppCtrlSubscriptions = 0x022C,
    AppCtrlZoomIn = 0x022D,
    AppCtrlZoomOut = 0x022E,
    AppCtrlZoom = 0x022F,
    AppCtrlFullScreenView = 0x0230,
    AppCtrlNormalView = 0x0231,
    AppCtrlViewToggle = 0x0232,
    AppCtrlScrollUp = 0x0233,
    AppCtrlScrollDown = 0x0234,
    AppCtrlScroll = 0x0235,
    AppCtrlPanLeft = 0x0236,
    AppCtrlPanRight = 0x0237,
    AppCtrlPan = 0x0238,
    AppCtrlNewWindow = 0x0239,
    AppCtrlTileHorizontally = 0x023A,
    AppCtrlTileVertically = 0x023B,
    AppCtrlFormat = 0x023C,
    AppCtrlEdit = 0x023D,
    AppCtrlBold = 0x023E,
    AppCtrlItalics = 0x023F,
    AppCtrlUnderline = 0x0240,
    AppCtrlStrikethrough = 0x0241,
    AppCtrlSubscript = 0x0242,
    AppCtrlSuperscript = 0x0243,
    AppCtrlAllCaps = 0x0244,
    AppCtrlRotate = 0x0245,
    AppCtrlResize = 0x0246,
    AppCtrlFlipHorizontal = 0x0247,
    AppCtrlFlipVertical = 0x0248,
    AppCtrlMirrorHorizontal = 0x0249,
    AppCtrlMirrorVertical = 0x024A,
    AppCtrlFontSelect = 0x024B,
    AppCtrlFontColor = 0x024C,
    AppCtrlFontSize = 0x024D,
    AppCtrlJustifyLeft = 0x024E,
    AppCtrlJustifyCenterH = 0x024F,
    AppCtrlJustifyRight = 0x0250,
    AppCtrlJustifyBlockH = 0x0251,
    AppCtrlJustifyTop = 0x0252,
    AppCtrlJustifyCenterV = 0x0253,
    AppCtrlJustifyBottom = 0x0254,
    AppCtrlJustifyBlockV = 0x0255,
    AppCtrlIndentDecrease = 0x0256,
    AppCtrlIndentIncrease = 0x0257,
    AppCtrlNumberedList = 0x0258,
    AppCtrlRestartNumbering = 0x0259,
    AppCtrlBulletedList = 0x025A,
    AppCtrlPromote = 0x025B,
    AppCtrlDemote = 0x025C,
    AppCtrlYes = 0x025D,
    AppCtrlNo = 0x025E,
    AppCtrlCancel = 0x025F,
    AppCtrlCatalog = 0x0260,
    AppCtrlBuyCheckout = 0x0261,
    AppCtrlAddToCart = 0x0262,
    AppCtrlExpand = 0x0263,
    AppCtrlExpandAll = 0x0264,
    AppCtrlCollapse = 0x0265,
    AppCtrlCollapseAll = 0x0266,
    AppCtrlPrintPreview = 0x0267,
    AppCtrlPasteSpecial = 0x0268,
    AppCtrlInsertMode = 0x0269,
    AppCtrlDelete = 0x026A,
    AppCtrlLock = 0x026B,
    AppCtrlUnlock = 0x026C,
    AppCtrlProtect = 0x026D,
    AppCtrlUnprotect = 0x026E,
    AppCtrlAttachComment = 0x026F,
    AppCtrlDeleteComment = 0x0270,
    AppCtrlViewComment = 0x0271,
    AppCtrlSelectWord = 0x0272,
    AppCtrlSelectSentence = 0x0273,
    AppCtrlSelectParagraph = 0x0274,
    AppCtrlSelectColumn = 0x0275,
    AppCtrlSelectRow = 0x0276,
    AppCtrlSelectTable = 0x0277,
    AppCtrlSelectObject = 0x0278,
    AppCtrlRedoRepeat = 0x0279,
    AppCtrlSort = 0x027A,
    AppCtrlSortAscending = 0x027B,
    AppCtrlSortDescending = 0x027C,
    AppCtrlFilter = 0x027D,
    AppCtrlSetClock = 0x027E,
    AppCtrlViewClock = 0x027F,
    AppCtrlSelectTimeZone = 0x0280,
    AppCtrlEditTimeZones = 0x0281,
    AppCtrlSetAlarm = 0x0282,
    AppCtrlClearAlarm = 0x0283,
    AppCtrlSnoozeAlarm = 0x0284,
    AppCtrlResetAlarm = 0x0285,
    AppCtrlSynchronize = 0x0286,
    AppCtrlSendReceive = 0x0287,
    AppCtrlSendTo = 0x0288,
    AppCtrlReply = 0x0289,
    AppCtrlReplyAll = 0x028A,
    AppCtrlForwardMsg = 0x028B,
    AppCtrlSend = 0x028C,
    AppCtrlAttachFile = 0x028D,
    AppCtrlUpload = 0x028E,
    AppCtrlDownloadSaveTargetAs = 0x028F,
    AppCtrlSetBorders = 0x0290,
    AppCtrlInsertRow = 0x0291,
    AppCtrlInsertColumn = 0x0292,
    AppCtrlInsertFile = 0x0293,
    AppCtrlInsertPicture = 0x0294,
    AppCtrlInsertObject = 0x0295,
    AppCtrlInsertSymbol = 0x0296,
    AppCtrlSaveAndClose = 0x0297,
    AppCtrlRename = 0x0298,
    AppCtrlMerge = 0x0299,
    AppCtrlSplit = 0x029A,
    AppCtrlDistributeHorizontally = 0x029B,
    AppCtrlDistributeVertically = 0x029C,
  }

  public enum HpWindowsMediaCenterRemoteControl : ushort
  {
    Undefined,
    Visualization = 0x0032,
    SlideShow = 0x0033,
    HpEject = 0x0034,
    InputSelection = 0x0035,
  }

  public enum GameControl : ushort
  {
    Undefined,
    GameController3D = 0x0001,
    PinballDevice = 0x0002,
    GunDevice = 0x0003,
    PointOfView = 0x0020,
    TurnRightLeft = 0x0021,
    PitchForwardBackward = 0x0022,
    RollRightLeft = 0x0023,
    MoveRightLeft = 0x0024,
    MoveForwardBackward = 0x0025,
    MoveUpDown = 0x0026,
    LeanRightLeft = 0x0027,
    LeanForwardBackward = 0x0028,
    HeightOfPov = 0x0029,
    Flipper = 0x002A,
    SecondaryFlipper = 0x002B,
    Bump = 0x002C,
    NewGame = 0x002D,
    ShootBall = 0x002E,
    Player = 0x002F,
    GunBolt = 0x0030,
    GunClip = 0x0031,
    GunSelector = 0x0032,
    GunSingleShot = 0x0033,
    GunBurst = 0x0034,
    GunAutomatic = 0x0035,
    GunSafety = 0x0036,
    GamepadFireJump = 0x0037,
    GamepadTrigger = 0x0039,
  }

  public enum SimulationControl : ushort
  {
    Undefined,
    FlightSimulationDevice = 0x0001,
    AutomobileSimulationDevice = 0x0002,
    TankSimulationDevice = 0x0003,
    SpaceshipSimulationDevice = 0x0004,
    SubmarineSimulationDevice = 0x0005,
    SailingSimulationDevice = 0x0006,
    MotorcycleSimulationDevice = 0x0007,
    SportsSimulationDevice = 0x0008,
    AirplaneSimulationDevice = 0x0009,
    HelicopterSimulationDevice = 0x000A,
    MagicCarpetSimulationDevice = 0x000B,
    BicycleSimulationDevice = 0x000C,
    FlightControlStick = 0x0020,
    FlightStick = 0x0021,
    CyclicControl = 0x0022,
    CyclicTrim = 0x0023,
    FlightYoke = 0x0024,
    TrackControl = 0x0025,
    Aileron = 0x00B0,
    AileronTrim = 0x00B1,
    AntiTorqueControl = 0x00B2,
    AutopilotEnable = 0x00B3,
    ChaffRelease = 0x00B4,
    CollectiveControl = 0x00B5,
    DiveBrake = 0x00B6,
    ElectronicCountermeasures = 0x00B7,
    Elevator = 0x00B8,
    ElevatorTrim = 0x00B9,
    Rudder = 0x00BA,
    Throttle = 0x00BB,
    FlightCommunications = 0x00BC,
    FlareRelease = 0x00BD,
    LandingGear = 0x00BE,
    ToeBrake = 0x00BF,
    Trigger = 0x00C0,
    WeaponsArm = 0x00C1,
    WeaponsSelect = 0x00C2,
    WingFlaps = 0x00C3,
    Accelerator = 0x00C4,
    Brake = 0x00C5,
    Clutch = 0x00C6,
    Shifter = 0x00C7,
    Steering = 0x00C8,
    TurretDirection = 0x00C9,
    BarrelElevation = 0x00CA,
    DivePlane = 0x00CB,
    Ballast = 0x00CC,
    BicycleCrank = 0x00CD,
    HandleBars = 0x00CE,
    FrontBrake = 0x00CF,
    RearBrake = 0x00D0,
  }

  public enum TelephonyDevice : ushort
  {
    Undefined,
    Phone = 0x0001,
    AnsweringMachine = 0x0002,
    MessageControls = 0x0003,
    Handset = 0x0004,
    Headset = 0x0005,
    TelephonyKeyPad = 0x0006,
    ProgrammableButton = 0x0007,
    HookSwitch = 32, // 0x0020,
    Flash = 33, // 0x0021,
    Feature = 34, // 0x0022,
    Hold = 35, // 0x0023,
    Redial = 36, // 0x0024,
    Transfer = 37, // 0x0025,
    Drop = 38, // 0x0026,
    Park = 39, // 0x0027,
    ForwardCalls = 40, // 0x0028,
    AlternateFunction = 41, // 0x0029,
    Line = 42, // 0x002A,
    SpeakerPhone = 43, // 0x002B,
    Conference = 44, // 0x002C,
    RingEnable = 0x002D,
    RingSelect = 0x002E,
    PhoneMute = 0x002F,
    CallerId = 0x0030,
    Send = 0x0031,
    SpeedDial = 0x0050,
    StoreNumber = 0x0051,
    RecallNumber = 0x0052,
    PhoneDirectory = 0x0053,
    VoiceMail = 0x0070,
    ScreenCalls = 0x0071,
    DoNotDisturb = 0x0072,
    Message = 0x0073,
    AnswerOnOff = 0x0074,
    InsideDialTone = 0x0090,
    OutsideDialTone = 0x0091,
    InsideRingTone = 0x0092,
    OutsideRingTone = 0x0093,
    PriorityRingTone = 0x0094,
    InsideRingback = 0x0095,
    PriorityRingback = 0x0096,
    LineBusyTone = 0x0097,
    ReorderTone = 0x0098,
    CallWaitingTone = 0x0099,
    ConfirmationTone1 = 0x009A,
    ConfirmationTone2 = 0x009B,
    TonesOff = 0x009C,
    OutsideRingback = 0x009D,
    Ringer = 0x009E,
    PhoneKey0 = 0x00B0,
    PhoneKey1 = 0x00B1,
    PhoneKey2 = 0x00B2,
    PhoneKey3 = 0x00B3,
    PhoneKey4 = 0x00B4,
    PhoneKey5 = 0x00B5,
    PhoneKey6 = 0x00B6,
    PhoneKey7 = 0x00B7,
    PhoneKey8 = 0x00B8,
    PhoneKey9 = 0x00B9,
    PhoneKeyStar = 0x00BA,
    PhoneKeyPound = 0x00BB,
    PhoneKeyA = 0x00BC,
    PhoneKeyB = 0x00BD,
    PhoneKeyC = 0x00BE,
    PhoneKeyD = 0x00BF
  }

  public enum GenericDesktop : ushort
  {
    Undefined,
    Pointer = 0x0001,
    Mouse = 0x0002,
    Joystick = 0x0004,
    GamePad = 0x0005,
    Keyboard = 0x0006,
    Keypad = 0x0007,
    MultiAxisController = 0x0008,
    TabletPcSystemControls = 0x0009,
    X = 0x0030,
    Y = 0x0031,
    Z = 0x0032,
    Rx = 0x0033,
    Ry = 0x0034,
    Rz = 0x0035,
    Slider = 0x0036,
    Dial = 0x0037,
    Wheel = 0x0038,
    HatSwitch = 0x0039,
    CountedBuffer = 0x003A,
    ByteCount = 0x003B,
    MotionWakeup = 0x003C,
    Start = 0x003D,
    Select = 0x003E,
    Vx = 0x0040,
    Vy = 0x0041,
    Vz = 0x0042,
    Vbrx = 0x0043,
    Vbry = 0x0044,
    Vbrz = 0x0045,
    Vno = 0x0046,
    SystemControl = 0x0080,
    SystemPowerDown = 0x0081,
    SystemSleep = 0x0082,
    SystemWakeUp = 0x0083,
    SystemContextMenu = 0x0084,
    SystemMainMenu = 0x0085,
    SystemAppMenu = 0x0086,
    SystemMenuHelp = 0x0087,
    SystemMenuExit = 0x0088,
    SystemMenuSelect = 0x0089,
    SystemMenuRight = 0x008A,
    SystemMenuLeft = 0x008B,
    SystemMenuUp = 0x008C,
    SystemMenuDown = 0x008D,
    SystemColdRestart = 0x008E,
    SystemWarmRestart = 0x008F,
    DPadUp = 0x0090,
    DPadDown = 0x0091,
    DPadRight = 0x0092,
    DPadLeft = 0x0093,
    SystemDock = 0x00A0,
    SystemUndock = 0x00A1,
    SystemSetup = 0x00A2,
    SystemBreak = 0x00A3,
    SystemDebuggerBreak = 0x00A4,
    ApplicationBreak = 0x00A5,
    ApplicationDebuggerBreak = 0x00A6,
    SystemSpeakerMute = 0x00A7,
    SystemHibernate = 0x00A8,
    SystemDisplayInvert = 0x00B0,
    SystemDisplayInternal = 0x00B1,
    SystemDisplayExternal = 0x00B2,
    SystemDisplayBoth = 0x00B3,
    SystemDisplayDual = 0x00B4,
    SystemDisplayToggleIntExt = 0x00B5,
    SystemDisplaySwapPrimarySecondary = 0x00B6,
    SystemDisplayLcdAutoscale = 0x00B7
  }

  public enum DirectionPadState
  {
    Rest = -1,
    Up = 0,
    UpRight = 1,
    Right = 2,
    DownRight = 3,
    Down = 4,
    DownLeft = 5,
    Left = 6,
    UpLeft = 7,
  }

  public class HidEvent
  {
    public HidEvent(Event hidEvent)
    {
      if (hidEvent == null)
        return;

      HasModifierAlt = hidEvent.HasModifierAlt;
      HasModifierControl = hidEvent.HasModifierControl;
      HasModifierShift = hidEvent.HasModifierShift;
      HasModifierWindows = hidEvent.HasModifierWindows;
      IsForeground = hidEvent.IsForeground;
      IsButtonDown = hidEvent.IsButtonDown;
      IsButtonUp = hidEvent.IsButtonUp;
      IsGeneric = hidEvent.IsGeneric;
      IsKeyboard = hidEvent.IsKeyboard;
      IsMouse = hidEvent.IsMouse;
      IsValid = hidEvent.IsValid;
      IsRepeat = hidEvent.IsRepeat;
      VirtualKey = hidEvent.VirtualKey;
      RepeatCount = hidEvent.RepeatCount;
      KeyId = hidEvent.KeyId;
      if (hidEvent.Device != null)
        Device = new HidDevice(hidEvent.Device);
      if (hidEvent.IsMouse)
        MouseButtonFlags = (RawMouseButtonFlags)hidEvent.RawInput.data.mouse.mouseData.buttonsStr.usButtonFlags;
      else
        MouseButtonFlags = RawMouseButtonFlags.None;
      UsagePage = hidEvent.UsagePage;
      UsagePageEnum = (UsagePage)hidEvent.UsagePageEnum;
      UsagePageName = hidEvent.UsagePageName();
      UsagePageNameAndValue = hidEvent.UsagePageNameAndValue();
      UsageCollection = hidEvent.UsageCollection;
      UsageCollectionName = hidEvent.UsageCollectionName();
      UsageCollectionNameAndValue = hidEvent.UsageCollectionNameAndValue();
      Usages = hidEvent.Usages;
      UsageNames = new List<string>();
      UsageNameAndValues = new List<string>();
      for (int i = 0; i < Usages.Count; i++)
      {
        UsageNames.Add(hidEvent.UsageName(i));
        UsageNameAndValues.Add(hidEvent.UsageNameAndValue(i));
      }
      UsageId = hidEvent.UsageId;
      InputReport = hidEvent.InputReport;
      Time = hidEvent.Time;
      OriginalTime = hidEvent.OriginalTime;
      try
      {
        DirectionPadState = (DirectionPadState)hidEvent.GetDirectionPadState();
      }
      catch
      {
        DirectionPadState = DirectionPadState.Rest;
      }
    }

    public bool HasModifierShift { get; }
    public bool HasModifierControl { get; }
    public bool HasModifierAlt { get; }
    public bool HasModifierWindows { get; }
    public bool IsValid { get; }
    public bool IsForeground { get; }
    public bool IsBackground => !IsForeground;
    public bool IsMouse { get; }
    public bool IsKeyboard { get; }
    public bool IsGeneric { get; }
    public Keys VirtualKey { get; }
    public bool IsModifierShift
    {
      get
      {
        if (!IsKeyboard)
          return false;
        if (VirtualKey != Keys.ShiftKey && VirtualKey != Keys.LShiftKey)
          return VirtualKey == Keys.RShiftKey;
        return true;
      }
    }
    public bool IsModifierControl
    {
      get
      {
        if (!IsKeyboard)
          return false;
        if (VirtualKey != Keys.ControlKey && VirtualKey != Keys.LControlKey)
          return VirtualKey == Keys.RControlKey;
        return true;
      }
    }
    public bool IsModifierAlt
    {
      get
      {
        if (!IsKeyboard)
          return false;
        if (VirtualKey != Keys.Menu && VirtualKey != Keys.LMenu)
          return VirtualKey == Keys.RMenu;
        return true;
      }
    }
    public bool IsModifierWindows
    {
      get
      {
        if (!IsKeyboard)
          return false;
        if (VirtualKey != Keys.LWin)
          return VirtualKey == Keys.RWin;
        return true;
      }
    }
    public bool IsModifier
    {
      get
      {
        if (!IsModifierShift && !IsModifierControl && !IsModifierAlt)
          return IsModifierWindows;
        return true;
      }
    }
    public bool IsButtonDown { get; }
    public bool IsButtonUp { get; }
    public bool IsRepeat { get; }
    public uint RepeatCount { get; }
    public ulong KeyId { get; }
    public HidDevice Device { get; }
    public RawMouseButtonFlags MouseButtonFlags { get; }
    public ushort UsagePage { get; }
    public UsagePage UsagePageEnum { get; }
    public string UsagePageName { get; }
    public string UsagePageNameAndValue { get; }
    public ushort UsageCollection { get; }
    public string UsageCollectionName { get; }
    public string UsageCollectionNameAndValue { get; }
    public IList<ushort> Usages { get; }
    public IList<string> UsageNames { get; }
    public IList<string> UsageNameAndValues { get; }
    public uint UsageId { get; }
    public byte[] InputReport { get; }
    public string InputReportString
    {
      get
      {
        if (this.InputReport == null)
          return "null";
        string str = "";
        foreach (byte num in InputReport)
          str += num.ToString("X2");
        return str;
      }
    }
    public DateTime Time { get; }
    public DateTime OriginalTime { get; }
    public DirectionPadState DirectionPadState { get; }

    public override string ToString()
    {
      string str1 = "";
      if (!IsValid)
        return str1 + "HID Event Invalid";
      string str2 = str1 + "HID Event";
      if (IsButtonDown)
        str2 += ", DOWN";
      if (IsButtonUp)
        str2 += ", UP";
      if (IsGeneric)
      {
        string str3 = str2 + ", Generic";
        for (int aIndex = 0; aIndex < Usages.Count; ++aIndex)
          str3 = str3 + ", Usage: " + UsageNameAndValues[aIndex];
        str2 = str3 + ", UsagePage: " + UsagePageNameAndValue + ", UsageCollection: " + UsageCollectionNameAndValue + ", Input Report: 0x" + InputReportString;
      }
      else if (IsKeyboard)
        str2 = str2 + ", Keyboard" + ", Virtual Key: " + VirtualKey;
      else if (IsMouse)
        str2 += ", Mouse";

      if (IsBackground)
        str2 += ", Background";
      if (IsRepeat)
        str2 = str2 + ", Repeat: " + RepeatCount;
      return str2;
    }
  }

  public class HidDevice
  {
    public HidDevice(Device device)
    {
      if (device == null)
        return;

      Name = device.Name;
      FriendlyName = device.FriendlyName;
      Manufacturer = device.Manufacturer;
      Product = device.Product;
      VendorId = device.VendorId;
      ProductId = device.ProductId;
      Version = device.Version;
      InputCapabilitiesDescription = device.InputCapabilitiesDescription;
      ButtonCount = device.ButtonCount;
      IsGamePad = device.IsGamePad;
      IsMouse = device.IsMouse;
      IsKeyboard = device.IsKeyboard;
      IsHid = device.IsHid;
      UsagePage = device.UsagePage;
      UsageCollection = device.UsageCollection;
      UsageId = device.UsageId;
    }

    public string Name { get; }
    public string FriendlyName { get; }
    public string Manufacturer { get; }
    public string Product { get; }
    public ushort VendorId { get; }
    public ushort ProductId { get; }
    public ushort Version { get; }
    public string InputCapabilitiesDescription { get; }
    public int ButtonCount { get; }
    public bool IsGamePad { get; }
    public bool IsMouse { get; }
    public bool IsKeyboard { get; }
    public bool IsHid { get; }
    public ushort UsagePage { get; }
    public ushort UsageCollection { get; }
    public uint UsageId { get; }

    public override string ToString()
    {
      return "HID Device: " + FriendlyName;
    }
  }
}
