// Copyright (C) 2007-2014 Team MediaPortal
// http://www.team-mediaportal.com
// 
// This file is part of MediaPortal 2
// 
// MediaPortal 2 is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// MediaPortal 2 is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.

#include <Shlobj.h>
#include <stdio.h>

#include "EVRPresenter.h"

void LogPath(TCHAR* dest, TCHAR* name)
{
  TCHAR folder[MAX_PATH];
  SHGetSpecialFolderPath(NULL, folder, CSIDL_COMMON_APPDATA, FALSE);
  sprintf_s(dest, MAX_PATH, "%s\\Team MediaPortal\\MP2-Client\\Log\\Evr.%s", folder, name);
}

// write message to EVR Log
void Log(const char *fmt, ...)
{
  va_list ap;

  char buffer[1000];
  int tmp;
  va_start(ap, fmt);
  tmp = vsprintf_s(buffer, fmt, ap);
  va_end(ap);

  TCHAR fileName[MAX_PATH];
  LogPath(fileName, "log");

  FILE* fp;
  int err = fopen_s(&fp, fileName, "a+");
  if (err == 0)
  {
    SYSTEMTIME systemTime;
    GetLocalTime(&systemTime);
    fprintf(fp, "%04.4d-%02.2d-%02.2d %02.2d:%02.2d:%02.2d.%03.3d [%04x] %s\n",
      systemTime.wYear, systemTime.wMonth, systemTime.wDay,
      systemTime.wHour, systemTime.wMinute, systemTime.wSecond,
      systemTime.wMilliseconds,
      GetCurrentThreadId(),
      buffer);
    fclose(fp);
  }
}


