/*

Copyright (c) 2007, 2008 Hiroki Asakawa asakaw@gmail.com

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/

#include <windows.h>
#include <stdio.h>
#include <sddl.h>
#include "mount.h"

static HANDLE                g_EventControl = NULL;
static SERVICE_STATUS        g_ServiceStatus;
static SERVICE_STATUS_HANDLE g_StatusHandle = NULL;

static HANDLE	g_EventLog = NULL;

static unsigned char g_MountTable[26];

BOOL g_DebugMode = TRUE;
BOOL g_UseStdErr = FALSE;

static VOID DokanControl(PDOKAN_CONTROL Control)
{
	DWORD written = 0;

	DbgPrint("DokanControl\n");
	
	Control->Status = DOKAN_CONTROL_FAIL;

	switch (Control->Type)
	{
	case DOKAN_CONTROL_MOUNT:

		DbgPrintW(L"DokanControl Mount\n");

		if (DokanControlMount(Control->Mount.Device, Control->Mount.Drive)) {
			// DeviceNumber is zero origin
			// add 1 to avoid to be the same number with DOKAN_CONTROL_FAIL
			g_MountTable[towlower(Control->Mount.Drive) - L'a'] =
				(unsigned char)Control->Mount.Device + 1;
			Control->Status = DOKAN_CONTROL_SUCCESS;
		} else {
			Control->Status = DOKAN_CONTROL_FAIL;
		}
		break;

	case DOKAN_CONTROL_UNMOUNT:

		DbgPrintW(L"DokanControl Unmount\n");

		if (DokanControlUnmount(Control->Unmount.Drive)) {
			g_MountTable[towlower(Control->Mount.Drive) - L'a'] = 0;
			Control->Status = DOKAN_CONTROL_SUCCESS;
		} else {
			Control->Status = DOKAN_CONTROL_FAIL;
		}

		break;

	case DOKAN_CONTROL_CHECK:
		{
			unsigned char device =
				g_MountTable[towlower(Control->Check.Drive) - L'a'];
			DbgPrintW(L"DokanControl Check : %d\n", device);
			Control->Status = device;
		}
		break;

	default:
		DbgPrintW(L"DokanControl UnknownType %u\n", Control->Type);
	}

	return;
}



static DWORD WINAPI HandlerEx(DWORD dwControl, DWORD dwEventType, LPVOID lpEventData, LPVOID lpContext)
{
	switch (dwControl) {
	case SERVICE_CONTROL_STOP:

		g_ServiceStatus.dwWaitHint     = 50000;
		g_ServiceStatus.dwCheckPoint   = 0;
		g_ServiceStatus.dwCurrentState = SERVICE_STOP_PENDING;
		SetServiceStatus(g_StatusHandle, &g_ServiceStatus);

		SetEvent(g_EventControl);

		break;
	
	case SERVICE_CONTROL_INTERROGATE:
		SetServiceStatus(g_StatusHandle, &g_ServiceStatus);
		break;

	default:
		break;
	}

	return NO_ERROR;
}


static VOID BuildSecurityAttributes(PSECURITY_ATTRIBUTES SecurityAttributes)
{
	LPTSTR sd = L"D:P(A;;GA;;;SY)(A;;GRGWGX;;;BA)(A;;GRGW;;;WD)(A;;GR;;;RC)";

	ZeroMemory(SecurityAttributes, sizeof(SECURITY_ATTRIBUTES));
	
	ConvertStringSecurityDescriptorToSecurityDescriptor(
		sd,
		SDDL_REVISION_1,
		&SecurityAttributes->lpSecurityDescriptor,
		NULL);

	SecurityAttributes->nLength = sizeof(SECURITY_ATTRIBUTES);
    SecurityAttributes->bInheritHandle = TRUE;
}


static VOID WINAPI ServiceMain(DWORD dwArgc, LPTSTR *lpszArgv)
{
	DWORD			eventNo;
	HANDLE			pipe, device;
	HANDLE			eventConnect, eventUnmount;
	HANDLE			eventArray[3];
	DOKAN_CONTROL	control, unmount;
	OVERLAPPED		ov, driver;
	ULONG			returnedBytes;
	EVENT_CONTEXT	eventContext;
	SECURITY_ATTRIBUTES sa;


	g_StatusHandle = RegisterServiceCtrlHandlerEx(L"DokanMounter", HandlerEx, NULL);

	// extend completion time
	g_ServiceStatus.dwServiceType				= SERVICE_WIN32_OWN_PROCESS;
	g_ServiceStatus.dwWin32ExitCode				= NO_ERROR;
	g_ServiceStatus.dwControlsAccepted			= SERVICE_ACCEPT_STOP;
	g_ServiceStatus.dwServiceSpecificExitCode	= 0;
	g_ServiceStatus.dwWaitHint					= 30000;
	g_ServiceStatus.dwCheckPoint				= 1;
	g_ServiceStatus.dwCurrentState				= SERVICE_START_PENDING;
	SetServiceStatus(g_StatusHandle, &g_ServiceStatus);

	BuildSecurityAttributes(&sa);

	pipe = CreateNamedPipe(DOKAN_CONTROL_PIPE,
		PIPE_ACCESS_DUPLEX | FILE_FLAG_OVERLAPPED, 
		PIPE_TYPE_MESSAGE | PIPE_READMODE_MESSAGE | PIPE_WAIT,
		1, sizeof(control), sizeof(control), 1000, &sa);

	if (pipe == INVALID_HANDLE_VALUE) {
		// TODO: should do something
		DbgPrintW(L"DokanMounter: failed to create named pipe: %d\n", GetLastError());
	}

	device = CreateFile(
				DOKAN_GLOBAL_DEVICE_NAME,			// lpFileName
				GENERIC_READ | GENERIC_WRITE,       // dwDesiredAccess
				FILE_SHARE_READ | FILE_SHARE_WRITE, // dwShareMode
				NULL,                               // lpSecurityAttributes
				OPEN_EXISTING,                      // dwCreationDistribution
				FILE_FLAG_OVERLAPPED,               // dwFlagsAndAttributes
				NULL                                // hTemplateFile
			);
	
	if (device == INVALID_HANDLE_VALUE) {
		// TODO: should do something
		DbgPrintW(L"DokanMounter: failed to open device: %d\n", GetLastError());
	}

	eventConnect = CreateEvent(NULL, FALSE, FALSE, NULL);
	eventUnmount = CreateEvent(NULL, FALSE, FALSE, NULL);
	g_EventControl = CreateEvent(NULL, TRUE, FALSE, NULL);

	g_ServiceStatus.dwWaitHint     = 0;
	g_ServiceStatus.dwCheckPoint   = 0;
	g_ServiceStatus.dwCurrentState = SERVICE_RUNNING;
	SetServiceStatus(g_StatusHandle, &g_ServiceStatus);

	for (;;) {
		ZeroMemory(&ov, sizeof(OVERLAPPED));
		ZeroMemory(&driver, sizeof(OVERLAPPED));
		ZeroMemory(&eventContext, sizeof(EVENT_CONTEXT));

		ov.hEvent = eventConnect;
		driver.hEvent = eventUnmount;

		ConnectNamedPipe(pipe, &ov);
		if (!DeviceIoControl(device, IOCTL_SERVICE_WAIT, NULL, 0,
			&eventContext, sizeof(EVENT_CONTEXT), NULL, &driver)) {
			DWORD error = GetLastError();
			if (error != 997) {
				DbgPrintW(L"DokanMounter: DeviceIoControl error: %d\n", error);
			}
		}

		eventArray[0] = eventConnect;
		eventArray[1] = eventUnmount;
		eventArray[2] = g_EventControl;

		eventNo = WaitForMultipleObjects(3, eventArray, FALSE, INFINITE) - WAIT_OBJECT_0;

		DbgPrintW(L"DokanMouner: get an event\n");

		if (eventNo == 0) {

			DWORD result = 0;

			ZeroMemory(&control, sizeof(control));
			if (ReadFile(pipe, &control, sizeof(control), &result, NULL)) {
				DbgPrintW(L"DokanMounter: Control->Type %d\n", control.Type);
				DokanControl(&control);
				WriteFile(pipe, &control, sizeof(control), &result, NULL);
			}
			FlushFileBuffers(pipe);
			DisconnectNamedPipe(pipe);
		
		} else if (eventNo == 1) {

			if (GetOverlappedResult(device, &driver, &returnedBytes, FALSE)) {
				if (returnedBytes == sizeof(EVENT_CONTEXT)) {
					DbgPrintW(L"DokanMounter: Unmount\n", control.Type);

					ZeroMemory(&unmount, sizeof(DOKAN_CONTROL));
					unmount.Type = DOKAN_CONTROL_UNMOUNT;
					unmount.Unmount.Drive = (WCHAR)eventContext.Flags;
					DokanControl(&unmount);
				} else {
					DbgPrintW(L"DokanMounter: Unmount error\n", control.Type);
				}
			}

		} else if (eventNo == 2) {
			DbgPrintW(L"DokanMounter: stop mounter service\n");
			g_ServiceStatus.dwWaitHint     = 0;
			g_ServiceStatus.dwCheckPoint   = 0;
			g_ServiceStatus.dwCurrentState = SERVICE_STOPPED;
			SetServiceStatus(g_StatusHandle, &g_ServiceStatus);

			break;
		}
		else
			break;
	}


	CloseHandle(pipe);
	CloseHandle(eventConnect);
	CloseHandle(g_EventControl);
	CloseHandle(device);
	CloseHandle(eventUnmount);

	return;
}



int WINAPI WinMain(HINSTANCE hinst, HINSTANCE hinstPrev, LPSTR lpszCmdLine, int nCmdShow)
{
	SERVICE_TABLE_ENTRY serviceTable[] = {
		{L"DokanMounter", ServiceMain}, {NULL, NULL}
	};

	ZeroMemory(g_MountTable, sizeof(g_MountTable));

	StartServiceCtrlDispatcher(serviceTable);

	return 0;
}


