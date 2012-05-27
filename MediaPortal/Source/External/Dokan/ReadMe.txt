In MP2, we use a slightly modified version of the Dokan Installer.
This directory contains the Dokan sources which are used to compile the Dokan
installer which is used from MP2.
The original Dokan code can be downloaded from Google Code ( http://code.google.com/p/dokan/ ).

The original files from svn (release, tags) have been checked out from Google Code ( http://code.google.com/p/dokan/ )
and only minor changes were made to the "dokan_install\install.nsi"-files of both the 0.53 and 0.6 versions.

Uninstalling in silent mode, if dokan is already installed, but different version has been added directly to the NSIS installer,
because Wix Bootstrapper does not support filesize comparison, yet.

.: Building the installer :.
==================

To build the installer you need to install latest NSIS:
        http://nsis.sourceforge.net/


.: Changes to the installer so far :.
=====================================

 * If Silent, don't open the explorer window at the end of the Dokan installation.

 * If Silent, don't show the message box to reboot the pc at the end of the Dokan uninstallation.

 * If Silent, don't show message boxes regarding a wrong operating system.
    These checks are done by the parent (MP2) installer.

 * If Silent, don't show message boxes when Dokan is already installed.
    If it is the same version, simply stop the installer.
    If it is a different version, silently uninstall the previous one.

 TODO: Fix Dokan installer when OS is Windows 8.
