============================================
Media.Players.BassPlayer.ASIOInterop readme.
============================================

This project is part of the Media.Players.BassPlayer module. It uses parts of the Steinberg ASIO SDK. Due to licensing restrictions these parts cannot be published in the SVN repository.

Because of this, this project is not included in the main MediaPortal solution. Instead the Media.Players.BassPlayer refers a prebuild dll in this project's outputdir and copies that into the main MediaPortal outputdir on build. This way the main MediaPortal solution can still be build without the ASIO SDK.

If you want to build this project you need to register at www.steinberg.net and obtain an ASIO SDK. It is available at no cost. Add the file "asio.h" to this project and it can be build. Note that regarding distributing the resulting dll you may be bound by licencing restrictions imposed by Steinberg. Check with them prior to distributing anything.

Note: the enduser needs to have the "Microsoft Visual C++ 2008 Redistributable Package (x86)" installed in order to run this dll.

ASIO is a trademark and software of Steinberg Media Technologies GmbH.