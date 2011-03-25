// Copyright (C) 2005-2010 Team MediaPortal
// http://www.team-mediaportal.com
// 
// MediaPortal is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 2 of the License, or
// (at your option) any later version.
// 
// MediaPortal is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with MediaPortal. If not, see <http://www.gnu.org/licenses/>.

#include <stdio.h>

#include "EVRPresenter.h"

// write message to EVR Log
void Log(const char *fmt, ...)
{
  va_list ap;
	va_start(ap,fmt);

	char buffer[1000]; 
	int tmp;
	va_start(ap,fmt);
	tmp = vsprintf(buffer, fmt, ap);
	va_end(ap); 

	FILE* fp = fopen("log/evr.log", "a+");
	if (fp != NULL)
	{
		SYSTEMTIME systemTime;
		GetLocalTime(&systemTime);
		fprintf(fp,"%02.2d-%02.2d-%04.4d %02.2d:%02.2d:%02.2d.%03.3d [%x]%s\n",
			systemTime.wDay, systemTime.wMonth, systemTime.wYear,
			systemTime.wHour,systemTime.wMinute,systemTime.wSecond,
			systemTime.wMilliseconds,
			GetCurrentThreadId(),
			buffer);
		fclose(fp);
	}
}


