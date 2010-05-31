#**********************************************************************************************************#
#
# For the MediaPortal Installer to work you need:
# 1. Lastest NSIS version from http://nsis.sourceforge.net/Download
# 
# Editing is much more easier, if you install HM NSIS Edit from http://hmne.sourceforge.net
#
# ATTENTION: You need to adjust the path for TSReader.ax and DVBSub2.ax.
#            Will be included in future versions with MP2 
#
#**********************************************************************************************************#

!define APP_NAME "MediaPortal2 Infinity"

Name "${APP_NAME}"

SetCompressor lzma

;..................................................................................................
;Following two definitions required. Uninstall log will use these definitions.
;You may use these definitions also, when you want to set up the InstallDirRagKey,
;store the language selection, store Start Menu folder etc.
;Enter the windows uninstall reg sub key to add uninstall information to Add/Remove Programs also.

!define INSTDIR_REG_ROOT "HKLM"
!define INSTDIR_REG_KEY "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_NAME}"
;..................................................................................................

# Defines
!define VERSION 0.1
!define COMPANY "Team MediaPortal"
!define URL www.team-mediaportal.com


# General Definitions for the Interface
;..................................................................................................
!define MUI_ICON "images\install.ico"
!define MUI_HEADERIMAGE_BITMAP "images\header.bmp"
!define MUI_WELCOMEFINISHPAGE_BITMAP "images\wizard.bmp"
!define MUI_UNWELCOMEFINISHPAGE_BITMAP "images\wizard.bmp"
!define MUI_FINISHPAGE_NOAUTOCLOSE
!define MUI_STARTMENUPAGE_REGISTRY_ROOT HKLM
!define MUI_STARTMENUPAGE_NODISABLE
!define MUI_STARTMENUPAGE_REGISTRY_KEY $(^INSTDIR_REG_KEY)
!define MUI_STARTMENUPAGE_REGISTRY_VALUENAME StartMenuGroup
!define MUI_STARTMENUPAGE_DEFAULTFOLDER MediaPortal2
!define MUI_FINISHPAGE_RUN  
!define MUI_FINISHPAGE_RUN_FUNCTION RunMP2
!define MUI_FINISHPAGE_RUN_TEXT "Run MediaPortal2"
!define MUI_UNICON "${NSISDIR}\Contrib\Graphics\Icons\modern-uninstall.ico"
!define MUI_UNFINISHPAGE_NOAUTOCLOSE
;..................................................................................................

# Included files
;..................................................................................................
!include Sections.nsh
!include MUI2.nsh
!include LogicLib.nsh
!include InstallOptions.nsh
!include Library.nsh
!include WordFunc.nsh
!insertmacro VersionCompare
;..................................................................................................

# Variables used within the Script
;..................................................................................................
Var StartMenuGroup  ; Holds the Startup Group
Var WindowsVersion  ; The Windows Version
Var CommonAppData   ; The Common Application Folder
Var TSReader        ; Should we install TSReader Filter
Var FilterDir       ; The Directory, where the filters have been installed
Var LibInstall      ; Needed for Library Installation
Var UninstAll       ; Set, when the user decided to uninstall everything
;..................................................................................................

# Installer pages
; These instructions define the sequence of the pages shown by the installer
;..................................................................................................
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_LICENSE "..\MediaPortal.Base\Docs\MediaPortal License.rtf"
!insertmacro MUI_PAGE_COMPONENTS
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_STARTMENU Application $StartMenuGroup
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH


!insertmacro MUI_UNPAGE_CONFIRM
UnInstPage custom un.UninstallOpionsSelection
!insertmacro MUI_UNPAGE_INSTFILES
;..................................................................................................

# Installer languages
; We might include other languages
;..................................................................................................
!insertmacro MUI_LANGUAGE English
;..................................................................................................

# Installer attributes
; Set the output file name
;..................................................................................................
OutFile "Release\${APP_NAME}_setup.exe"
BrandingText "MediaPortal2 Installer by Team MediaPortal"
InstallDir "$PROGRAMFILES\Team MediaPortal\MediaPortal2"
CRCCheck on
XPStyle on
ShowInstDetails show
VIProductVersion 0.1.0.0
VIAddVersionKey /LANG=${LANG_ENGLISH} ProductName "${NAME}"
VIAddVersionKey /LANG=${LANG_ENGLISH} ProductVersion "${VERSION}"
VIAddVersionKey /LANG=${LANG_ENGLISH} CompanyName "${COMPANY}"
VIAddVersionKey /LANG=${LANG_ENGLISH} CompanyWebsite "${URL}"
VIAddVersionKey /LANG=${LANG_ENGLISH} FileVersion "${VERSION}"
VIAddVersionKey /LANG=${LANG_ENGLISH} FileDescription ""
VIAddVersionKey /LANG=${LANG_ENGLISH} LegalCopyright ""
InstallDirRegKey HKLM "${INSTDIR_REG_KEY}" Path
ShowUninstDetails show

# Installer sections
; 
; This is the Main section, which installs all MediaPortal Files
;
;..................................................................................................
Section -Main SEC0000
    SetOverwrite on   

    SetOutPath $INSTDIR

    ; Folder   
    File /nonfatal /r ..\MediaPortal\bin\x86\Release\Burner
    File /nonfatal /r ..\MediaPortal\bin\x86\Release\Databases
    File /nonfatal /r ..\MediaPortal\bin\x86\Release\Docs
    File /nonfatal /r ..\MediaPortal\bin\x86\Release\Language
    File /nonfatal /r ..\MediaPortal\bin\x86\Release\log
    File /nonfatal /r ..\MediaPortal\bin\x86\Release\Media
    File /nonfatal /r ..\MediaPortal\bin\x86\Release\Models
    File /nonfatal /r ..\MediaPortal\bin\x86\Release\MusicPlayer
    File /nonfatal /r ..\MediaPortal\bin\x86\Release\nl
    File /nonfatal /r ..\MediaPortal\bin\x86\Release\Plugins
    File /nonfatal /r ..\MediaPortal\bin\x86\Release\skin
    File /nonfatal /r ..\MediaPortal\bin\x86\Release\state
    File /nonfatal /r ..\MediaPortal\bin\x86\Release\thumbs
    File /nonfatal /r ..\MediaPortal\bin\x86\Release\ViewMapping
    File /nonfatal /r ..\MediaPortal\bin\x86\Release\Views
     
    ; Attention: Don't forget to add a Remove for every file to the UniNstall Section
        
    ;------------  Common Files and Folders for XP & Vista
    ; Files
    File ..\MediaPortal\bin\x86\Release\ICSharpCode.SharpZipLib.dll
    File ..\MediaPortal\bin\x86\Release\MediaPortal.exe
    File ..\MediaPortal\bin\x86\Release\MediaPortal.Core.dll
    File ..\MediaPortal\bin\x86\Release\MediaPortal.Services.dll
    File ..\MediaPortal\bin\x86\Release\MediaPortal.Utilities.dll
    File ..\MediaPortal\bin\x86\Release\UPnP.DLL
    File ..\MediaPortal\bin\x86\Release\UPNP_AV.dll
    File ..\MediaPortal\bin\x86\Release\UPNPAVCDSML.dll
    File ..\MediaPortal\bin\x86\Release\UPNPAVMSCP.dll
    File ..\MediaPortal\bin\x86\Release\UPNPAVMSDV.dll
    File ..\MediaPortal\bin\x86\Release\UPnPServer.dll
    File ..\MediaPortal\bin\x86\Release\vmr9Helper.dll
    ;------------  End of Common Files and Folders for XP & Vista
    
       
    ; Now we delete the .plugin file for Control.IrInput.  In the first version , we want to use the IR Server Suite
    Delete /REBOOTOK "$INSTDIR\Plugins\Control.IrInput\Control.IrInput.plugin"

    ; The Following Filters and Dll need to be copied to \windows\system32 for xp
    ; In Vista they stay in the Install Directory
    ${if} $WindowsVersion == "Vista" 
        SetOutPath $INSTDIR
        StrCpy $FilterDir $InstDir
    ${Else}
        SetOutPath $SYSDIR
        StrCpy $FilterDir $SysDir
    ${Endif}
  
    ; Install and Register only when not found previously
    WriteRegStr HKLM "${INSTDIR_REG_KEY}" TSReader 0 
    ${If} $TSReader == 1
        DetailPrint "Installing TsReader Filter"  
        WriteRegStr HKLM "${INSTDIR_REG_KEY}" TSReader 1

        # ATTENTION: You need to specify the Path to TSReader and DVBSub2
        !insertmacro InstallLib REGDLL $LibInstall REBOOT_NOTPROTECTED  D:\Source\TVEngine3\Filters\bin\TsReader.ax $FilterDir\TsReader.ax $FilterDir
        !insertmacro InstallLib REGDLL $LibInstall REBOOT_NOTPROTECTED  D:\Source\TVEngine3\Filters\bin\DVBSub2.ax $FilterDir\DVBSub2.ax $FilterDir
        
        ; Write Default Values for Filter into the registry
        WriteRegStr HKCR "Media Type\Extensions\.ts" "Source Filter" "{b9559486-e1bb-45d3-a2a2-9a7afe49b23f}"
    ${EndIf}
   
    WriteRegStr HKLM "${INSTDIR_REG_KEY}\Components" Main 1
SectionEnd

# This Section is executed after the Main secxtion has finished and writes Uninstall information into the registry
;..................................................................................................
Section -post SEC0001
    WriteRegStr HKLM "${INSTDIR_REG_KEY}" Path $INSTDIR
    WriteRegStr HKLM "${INSTDIR_REG_KEY}" PathFilter $FILTERDIR
    WriteRegStr HKLM "${INSTDIR_REG_KEY}" WindowsVersion $WindowsVersion
  
    ; Create the Statmenu and the Desktop shortcuts  
    
    ; The OutputPath specifies the Working Directory used for the Shortcuts
    SetOutPath $INSTDIR
    WriteUninstaller $INSTDIR\uninstall.exe
    !insertmacro MUI_STARTMENU_WRITE_BEGIN Application
    ; We need to create the StartMenu Dir. Otherwise the CreateShortCut fails 
    CreateDirectory $SMPROGRAMS\$StartMenuGroup 
    SetShellVarContext current
    CreateShortcut "$SMPROGRAMS\$StartMenuGroup\MediaPortal2.lnk" "$INSTDIR\MediaPortal.exe" "" "$INSTDIR\MediaPortal.exe" 0 "" "" "MediaPortal2" 
    CreateShortcut "$SMPROGRAMS\$StartMenuGroup\$(^UninstallLink).lnk" $INSTDIR\uninstall.exe
    
    CreateShortcut "$DESKTOP\MediaPortal2.lnk" "$INSTDIR\MediaPortal.exe" "" "$INSTDIR\MediaPortal.exe" 0 "" "" "MediaPortal2" 
    !insertmacro MUI_STARTMENU_WRITE_END
    
    WriteRegStr HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\$(^Name)" DisplayName "$(^Name)"
    WriteRegStr HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\$(^Name)" DisplayVersion "${VERSION}"
    WriteRegStr HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\$(^Name)" Publisher "${COMPANY}"
    WriteRegStr HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\$(^Name)" URLInfoAbout "${URL}"
    WriteRegStr HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\$(^Name)" DisplayIcon $INSTDIR\uninstall.exe
    WriteRegStr HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\$(^Name)" UninstallString $INSTDIR\uninstall.exe
    WriteRegDWORD HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\$(^Name)" NoModify 1
    WriteRegDWORD HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\$(^Name)" NoRepair 1
SectionEnd

; This section installs the IR Server Suite
Section "IR Server Suite" SEC0002
    SetOutPath $INSTDIR
    SetOverwrite on
    
    ; Now Copy the IR Server Suite installer, which will be executed as part of the install
    File "IR Server Suite.exe"

    DetailPrint "Installing IR Server Suite Package"
    ExecWait '"$INSTDIR\IR Server Suite.exe"'
    DetailPrint "Finished Installing IR Server Suite Package"
    Delete /REBOOTOK  "$INSTDIR\IR Server Suite.exe"
    WriteRegStr HKLM "${INSTDIR_REG_KEY}\Components" "IR Server Suite" 1
SectionEnd

; This section installs the VC++ Redist Library
Section "Visual C++ Redist" SEC0003
    SetOutPath $INSTDIR
    SetOverwrite on
    
    ; Now Copy the VC Redist File, which will be executed as part of the install
    File vcredist_x86.exe

    ; Installing VC++ Redist Package
    DetailPrint "Installing VC++ Redist Package"
    ExecWait '"$INSTDIR\vcredist_x86.exe" /q:a /c:"VCREDI~3.EXE /q:a /c:""msiexec /i vcredist.msi /qb!"" "'
    DetailPrint "Finished Installing VC++ Redist Package"
    Delete /REBOOTOK  $INSTDIR\vcredist_x86.exe
    WriteRegStr HKLM "${INSTDIR_REG_KEY}\Components" "Visual C++ Redist" 1
SectionEnd

LangString DESC_SEC0002 ${LANG_ENGLISH} "Installs the IR Server Suite for handling input via Remotes"
LangString DESC_SEC0003 ${LANG_ENGLISH} "Installs the Visual C++ Runtime (recommended)."

!insertmacro MUI_FUNCTION_DESCRIPTION_BEGIN
  !insertmacro MUI_DESCRIPTION_TEXT ${SEC0002} $(DESC_SEC0002)
  !insertmacro MUI_DESCRIPTION_TEXT ${SEC0003} $(DESC_SEC0003)
!insertmacro MUI_FUNCTION_DESCRIPTION_END


# Installer functions
Function .onInit
    InitPluginsDir
    
    ; Get Windows Version
    Call GetWindowsVersion
    Pop $R0
    StrCpy $WindowsVersion $R0
    ${if} $WindowsVersion == "95" 
    ${OrIf} $WindowsVersion == "98" 
    ${OrIf} $WindowsVersion == "ME"  
    ${OrIf} $WindowsVersion == "NT 4.0"
        MessageBox MB_OK|MB_ICONSTOP "MediaPortal is not support on Windows $WindowsVersion. Installation aborted"
        Abort
    ${EndIf}
    ${If} $WindowsVersion == "2003"
        ; MS Reports also XP 64 as NT 5.2. So we default on XP
        StrCpy $WindowsVersion 'XP'
    ${EndIf}   
    
    ; Check if .Net is installed
    Call GetDotNETVersion
    Pop $0
    ${If} $0 == "not found"
      MessageBox MB_OK|MB_ICONSTOP ".NET runtime library is not installed."
      Abort
    ${EndIf}
     
    StrCpy $0 $0 "" 1 # skip "v"
     
    ; Make 2.0 the default .Net version
    ${VersionCompare} $0 "2.0" $1
    ${If} $1 == 2
      MessageBox MB_OK|MB_ICONSTOP ".NET runtime library v2.0 or newer is required. You have $0."
      Abort
    ${EndIf}
    
    ; Get the Common Application Data Folder to Store Files for Vista
    ; Set the Context to alll, so that we get the All Users folder
    SetShellVarContext all
    StrCpy $CommonAppData "$APPDATA\MediaPortal\Config"
    ; Context back to current user
    SetShellVarContext current
    
    ; Needed for Library Install
    ; Look if we already have a registry entry for MP. if this is the case we don't need to install anymore the Shared Libraraies
    Push $0
    ReadRegStr $0 HKLM "${INSTDIR_REG_KEY}" Path
    ClearErrors
    StrCmp $0 "" +2
    StrCpy $LibInstall 1
    Pop $0
    
    ; Check if TSReader is already installed. For example by TVEngine3
    Push $0
    ReadRegStr $0 HKCR "Media Type\Extensions\.ts" "Source Filter"
    ClearErrors
    StrCmp $0 "{b9559486-e1bb-45d3-a2a2-9a7afe49b23f}" +2
    StrCpy $TSReader 1
    Pop $0
FunctionEnd

Function .onInstSuccess

FunctionEnd

; Start MP2 after the successfull install
; needed in an extra function to set the working directory
Function RunMP2
SetOutPath $INSTDIR
Exec "$INSTDIR\MediaPortal.exe"
FunctionEnd

# Macro for selecting uninstaller sections
!macro SELECT_UNSECTION SECTION_NAME UNSECTION_ID
    Push $R0
    ReadRegStr $R0 HKLM "${INSTDIR_REG_KEY}\Components" "${SECTION_NAME}"
    StrCmp $R0 1 0 next${UNSECTION_ID}
    !insertmacro SelectSection "${UNSECTION_ID}"
    GoTo done${UNSECTION_ID}
next${UNSECTION_ID}:
    !insertmacro UnselectSection "${UNSECTION_ID}"
done${UNSECTION_ID}:
    Pop $R0
!macroend

# Uninstaller sections

# Custom Page for Uninstall User settings
; This shows the Uninstall User Serrings Page
;..................................................................................................
LangString UNINSTALL_SETTINGS_TITLE ${LANG_ENGLISH} "Uninstall User settings"
LangString UNINSTALL_SETTINGS_SUBTITLE ${LANG_ENGLISH} "Attention: This will remove all your customised settings including Skins and Databases."

Function un.UninstallOpionsSelection ;Function name defined with Page command
  !insertmacro MUI_HEADER_TEXT "$(UNINSTALL_SETTINGS_TITLE)" "$(UNINSTALL_SETTINGS_SUBTITLE)"
  !insertmacro INSTALLOPTIONS_DISPLAY "UnInstallOptions.ini"
  
  ; Get the values selected in the Check Boxes
  !insertmacro INSTALLOPTIONS_READ $UninstAll "UninstallOptions.ini" "Field 1" "State"
FunctionEnd

LangString ^UninstallLink ${LANG_ENGLISH} "Uninstall $(^Name)"

Section /o -un.Main UNSEC0000    
    ; Remove the Folders
    RmDir /r /REBOOTOK $INSTDIR\\Burner
    RmDir /r /REBOOTOK $INSTDIR\\Databases
    RmDir /r /REBOOTOK $INSTDIR\\Docs
    RmDir /r /REBOOTOK $INSTDIR\\Language
    RmDir /r /REBOOTOK $INSTDIR\\log
    RmDir /r /REBOOTOK $INSTDIR\\Media
    RmDir /r /REBOOTOK $INSTDIR\\Models
    RmDir /r /REBOOTOK $INSTDIR\\MusicPlayer
    RmDir /r /REBOOTOK $INSTDIR\\nl
    RmDir /r /REBOOTOK $INSTDIR\\Plugins
    RmDir /r /REBOOTOK $INSTDIR\\skin
    RmDir /r /REBOOTOK $INSTDIR\\state
    RmDir /r /REBOOTOK $INSTDIR\\thumbs
    RmDir /r /REBOOTOK $INSTDIR\\ViewMapping
    RmDir /r /REBOOTOK $INSTDIR\\Views
     
   ; Remove Files in MP Root Directory
   Delete /REBOOTOK  $INSTDIR\ICSharpCode.SharpZipLib.dll
   Delete /REBOOTOK  $INSTDIR\MediaPortal.exe
   Delete /REBOOTOK  $INSTDIR\MediaPortal.Core.dll
   Delete /REBOOTOK  $INSTDIR\MediaPortal.Services.dll
   Delete /REBOOTOK  $INSTDIR\MediaPortal.Utilities.dll
   Delete /REBOOTOK  $INSTDIR\UPnP.DLL
   Delete /REBOOTOK  $INSTDIR\UPNP_AV.dll
   Delete /REBOOTOK  $INSTDIR\UPNPAVCDSML.dll
   Delete /REBOOTOK  $INSTDIR\UPNPAVMSCP.dll
   Delete /REBOOTOK  $INSTDIR\UPNPAVMSDV.dll
   Delete /REBOOTOK  $INSTDIR\UPnPServer.dll
   Delete /REBOOTOK  $INSTDIR\vmr9Helper.dll
   ;------------  End of Files in MP Root Directory --------------
    
  ; Do we need to deinstall everything? Then remove also the CommonAppData and InstDir
   ${If} $UninstAll == 1
       DetailPrint "Removing User Settings"
       RmDir /r /REBOOTOK $CommonAppData
   ${EndIf}
    
   # Was the Filter installed by MP2, then remove it as well
   ${If} $TSReader == 1
       !insertmacro UnInstallLib REGDLL SHARED REBOOT_NOTPROTECTED $FilterDir\TSReader.ax
       !insertmacro UnInstallLib REGDLL SHARED REBOOT_NOTPROTECTED $FilterDir\DVBSub2.ax
       DeleteRegKey HKCR "Media Type\Extensions\.ts"
   ${EndIf}

    ; Delete StartMenu- , Desktop ShortCuts and Registry Entry
    Delete /REBOOTOK $SMPROGRAMS\$StartMenuGroup\MediaPortal2.lnk
    Delete /REBOOTOK $DESKTOP\MediaPortal2.lnk
    DeleteRegValue HKLM "${INSTDIR_REG_KEY}\Components" Main
SectionEnd

Section -un.post UNSEC0001
    DeleteRegKey HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\$(^Name)"
    Delete /REBOOTOK "$SMPROGRAMS\$StartMenuGroup\$(^UninstallLink).lnk"
    Delete /REBOOTOK $INSTDIR\uninstall.exe
    DeleteRegValue HKLM "${INSTDIR_REG_KEY}" StartMenuGroup
    DeleteRegValue HKLM "${INSTDIR_REG_KEY}" Path
    DeleteRegValue HKLM "${INSTDIR_REG_KEY}" PathFilter
    DeleteRegKey /IfEmpty HKLM "${INSTDIR_REG_KEY}\Components"
    DeleteRegKey /IfEmpty HKLM "${INSTDIR_REG_KEY}"
    RmDir /REBOOTOK $SMPROGRAMS\$StartMenuGroup
    RmDir /REBOOTOK $INSTDIR
SectionEnd

# Uninstaller functions
Function un.onInit
    ReadRegStr $INSTDIR HKLM "${INSTDIR_REG_KEY}" Path
    ReadRegStr $FILTERDIR HKLM "${INSTDIR_REG_KEY}" PathFilter
    ReadRegStr $TSReader HKLM "${INSTDIR_REG_KEY}" TSReader
    ReadRegStr $WindowsVersion HKLM "${INSTDIR_REG_KEY}" WindowsVersion
    !insertmacro MUI_STARTMENU_GETFOLDER Application $StartMenuGroup
    !insertmacro SELECT_UNSECTION Main ${UNSEC0000}
    
    ; Extract the Uninstall Option Custom Page
    !insertmacro INSTALLOPTIONS_EXTRACT "UnInstallOptions.ini"
    
    ; Get the Common Application Data Folder to Store Files for Vista
    ; Set the Context to all, so that we get the All Users folder
    SetShellVarContext all
    StrCpy $CommonAppData "$APPDATA\MediaPortal\Config"
    ; Context back to current user
    SetShellVarContext current
FunctionEnd

# Various Functions that helps us during the installation
;...............................................................................
; GetDotNETVersion
;
; Usage:
;    Call GetDotNETVersion
;    Pop $0
;    ${If} $0 == "not found"
;       MessageBox MB_OK|MB_ICONSTOP ".NET runtime library is not installed."
;       Abort
;    ${EndIf}
; 
;    StrCpy $0 $0 "" 1 # skip "v"
Function GetDotNETVersion
  Push $0
  Push $1
 
  System::Call "mscoree::GetCORVersion(w .r0, i ${NSIS_MAX_STRLEN}, *i) i .r1 ?u"
  StrCmp $1 "error" 0 +2
    StrCpy $0 "not found"
 
  Pop $1
  Exch $0
FunctionEnd

; GetWindowsVersion
;
; Based on Yazno's function, http://yazno.tripod.com/powerpimpit/
; Updated by Joost Verburg
;
; Returns on top of stack
;
; Windows Version (95, 98, ME, NT x.x, 2000, XP, 2003, Vista)
; or
; '' (Unknown Windows Version)
;
; Usage:
;   Call GetWindowsVersion
;   Pop $R0
;   ; at this point $R0 is "NT 4.0" or whatnot
 
Function GetWindowsVersion
 
  Push $R0
  Push $R1
 
  ClearErrors
 
  ReadRegStr $R0 HKLM \
  "SOFTWARE\Microsoft\Windows NT\CurrentVersion" CurrentVersion
 
  IfErrors 0 lbl_winnt
  
  ; we are not NT
  ReadRegStr $R0 HKLM \
  "SOFTWARE\Microsoft\Windows\CurrentVersion" VersionNumber
 
  StrCpy $R1 $R0 1
  StrCmp $R1 '4' 0 lbl_error
 
  StrCpy $R1 $R0 3
 
  StrCmp $R1 '4.0' lbl_win32_95
  StrCmp $R1 '4.9' lbl_win32_ME lbl_win32_98
 
  lbl_win32_95:
    StrCpy $R0 '95'
  Goto lbl_done
 
  lbl_win32_98:
    StrCpy $R0 '98'
  Goto lbl_done
 
  lbl_win32_ME:
    StrCpy $R0 'ME'
  Goto lbl_done
 
  lbl_winnt:
 
  StrCpy $R1 $R0 1
 
  StrCmp $R1 '3' lbl_winnt_x
  StrCmp $R1 '4' lbl_winnt_x
 
  StrCpy $R1 $R0 3
 
  StrCmp $R1 '5.0' lbl_winnt_2000
  StrCmp $R1 '5.1' lbl_winnt_XP
  StrCmp $R1 '5.2' lbl_winnt_2003
  StrCmp $R1 '6.0' lbl_winnt_vista lbl_error
 
  lbl_winnt_x:
    StrCpy $R0 "NT $R0" 6
  Goto lbl_done
 
  lbl_winnt_2000:
    Strcpy $R0 '2000'
  Goto lbl_done
 
  lbl_winnt_XP:
    Strcpy $R0 'XP'
  Goto lbl_done
 
  lbl_winnt_2003:
    Strcpy $R0 '2003'
  Goto lbl_done
 
  lbl_winnt_vista:
    Strcpy $R0 'Vista'
  Goto lbl_done
 
  lbl_error:
    Strcpy $R0 ''
  lbl_done:
 
  Pop $R1
  Exch $R0
 
FunctionEnd