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
#include <winioctl.h>
#include <stdio.h>
#include "mount.h"

typedef struct _REPARSE_DATA_BUFFER {
    ULONG  ReparseTag;
    USHORT ReparseDataLength;
    USHORT Reserved;
    union {
        struct {
            USHORT SubstituteNameOffset;
            USHORT SubstituteNameLength;
            USHORT PrintNameOffset;
            USHORT PrintNameLength;
            ULONG Flags;
            WCHAR PathBuffer[1];
        } SymbolicLinkReparseBuffer;
        struct {
            USHORT SubstituteNameOffset;
            USHORT SubstituteNameLength;
            USHORT PrintNameOffset;
            USHORT PrintNameLength;
            WCHAR PathBuffer[1];
        } MountPointReparseBuffer;
        struct {
            UCHAR  DataBuffer[1];
        } GenericReparseBuffer;
    } DUMMYUNIONNAME;
} REPARSE_DATA_BUFFER, *PREPARSE_DATA_BUFFER;

#define REPARSE_DATA_BUFFER_HEADER_SIZE   FIELD_OFFSET(REPARSE_DATA_BUFFER, GenericReparseBuffer)

BOOL
CreateMountPoint(
	PWCHAR	ReparsePointName,
	PWCHAR	TargetDeviceName)
{
	HANDLE handle;
	PREPARSE_DATA_BUFFER reparseData;
	USHORT	bufferLength;
	USHORT	targetLength;
	BOOL	result;
	ULONG	resultLength;
	
	handle = CreateFile(
		ReparsePointName, GENERIC_WRITE, 0, NULL, OPEN_EXISTING,
		FILE_FLAG_OPEN_REPARSE_POINT | FILE_FLAG_BACKUP_SEMANTICS, NULL);

	if (handle == INVALID_HANDLE_VALUE) {
		DbgPrintW(L"CreateFile failed: %s (%d)\n", ReparsePointName, GetLastError());
		return FALSE;
	}

	targetLength = wcslen(TargetDeviceName) * sizeof(WCHAR);
	bufferLength = FIELD_OFFSET(REPARSE_DATA_BUFFER, MountPointReparseBuffer.PathBuffer) +
		targetLength + sizeof(WCHAR) + sizeof(WCHAR);

	reparseData = malloc(bufferLength);

	ZeroMemory(reparseData, bufferLength);

	reparseData->ReparseTag = IO_REPARSE_TAG_MOUNT_POINT;
	reparseData->ReparseDataLength = bufferLength - REPARSE_DATA_BUFFER_HEADER_SIZE;

	reparseData->MountPointReparseBuffer.SubstituteNameOffset = 0;
	reparseData->MountPointReparseBuffer.SubstituteNameLength = targetLength;
	reparseData->MountPointReparseBuffer.PrintNameOffset = targetLength + sizeof(WCHAR);
	reparseData->MountPointReparseBuffer.PrintNameLength = 0;

	RtlCopyMemory(reparseData->MountPointReparseBuffer.PathBuffer, TargetDeviceName, targetLength);

	result = DeviceIoControl(
				handle,
				FSCTL_SET_REPARSE_POINT,
				reparseData,
				bufferLength,
				NULL,
				0,
				&resultLength,
				NULL);
	
	CloseHandle(handle);
	free(reparseData);

	if (result) {
		DbgPrintW(L"CreateMountPoint %s -> %s success\n",
			ReparsePointName, TargetDeviceName);
	} else {
		DbgPrintW(L"CreateMountPoint %s -> %s failed: %d\n",
			ReparsePointName, TargetDeviceName, GetLastError());
	}
	return result;
}

BOOL
DeleteMountPoint(
	PWCHAR	ReparsePointName)
{
	HANDLE	handle;
	BOOL	result;
	ULONG	resultLength;
	REPARSE_GUID_DATA_BUFFER	reparseData = { 0 };

	handle = CreateFile(
		ReparsePointName, GENERIC_WRITE, 0, NULL, OPEN_EXISTING,
		FILE_FLAG_OPEN_REPARSE_POINT | FILE_FLAG_BACKUP_SEMANTICS, NULL);

	if (handle == INVALID_HANDLE_VALUE) {
		DbgPrintW(L"CreateFile failed: %s (%d)\n", ReparsePointName, GetLastError());
		return FALSE;
	}

	reparseData.ReparseTag = IO_REPARSE_TAG_MOUNT_POINT;

	result = DeviceIoControl(
				handle,
				FSCTL_DELETE_REPARSE_POINT,
				&reparseData,
				REPARSE_GUID_DATA_BUFFER_HEADER_SIZE,
				NULL,
				0,
				&resultLength,
				NULL);
	
	CloseHandle(handle);

	if (result) {
		DbgPrintW(L"DeleteMountPoint success\n");
	} else {
		DbgPrintW(L"DeleteMountPoint failed: %d\n", GetLastError());
	}
	return result;
}


BOOL
DokanControlMount(
	ULONG	DeviceNumber,
	WCHAR	DriveLetter)
{
	WCHAR   volumeName[] = L"\\\\.\\ :";
	WCHAR	driveLetterAndSlash[] = L"C:\\";
	HANDLE  device;
	WCHAR	deviceName[MAX_PATH];
	WCHAR	mountPoint[MAX_PATH];
	
	wsprintf(deviceName, DOKAN_RAW_DEVICE_NAME, DeviceNumber);
	wsprintf(mountPoint, DOKAN_MOUNT_DEVICE_NAME, DeviceNumber);

	volumeName[4] = DriveLetter;
	driveLetterAndSlash[0] = DriveLetter;

	DbgPrintW(L"DeviceNumber %d DriveLetter %c\n", DeviceNumber, DriveLetter);
	DbgPrintW(L"DeviceName %s\n",deviceName);

	device = CreateFile(
		volumeName,
		GENERIC_READ | GENERIC_WRITE,
		FILE_SHARE_READ | FILE_SHARE_WRITE,
		NULL,
		OPEN_EXISTING,
		FILE_FLAG_NO_BUFFERING,
		NULL
		);

    if (device != INVALID_HANDLE_VALUE) {
		DbgPrintW(L"DokanControl Mount failed: %wc: is alredy used\n", DriveLetter);
		CloseHandle(device);
        return FALSE;
    }

    if (!DefineDosDevice(DDD_RAW_TARGET_PATH, &volumeName[4], deviceName)) {
		DbgPrintW(L"DokanControl DefineDosDevice failed: %d\n", GetLastError());
        return FALSE;
    }

	/* NOTE: IOCTL_MOUNTDEV_QUERY_DEVICE_NAME in sys/device.cc handles
	   GetVolumeNameForVolumeMountPoint. But it returns error even if driver
	   return success.
	   */

	//wsprintf(deviceName, L"\\\\?\\Volume{dca0e0a5-d2ca-4f0f-8416-a6414657a77a}\\");
	//DbgPrintW(L"DeviceName %s\n",deviceName);

	/*
	if (!GetVolumeNameForVolumeMountPoint(
			driveLetterAndSlash, 
			deviceName,
			MAX_PATH)) {

		DbgPrint("Error: GetVolumeNameForVolumeMountPoint failed : %d\n", GetLastError());
	} else {
	
		DbgPrintW(L"UniqueVolumeName %s\n", deviceName);
		DefineDosDevice(DDD_REMOVE_DEFINITION,
						&volumeName[4],
				        NULL);

		if (!SetVolumeMountPoint(driveLetterAndSlash, deviceName)) {
			DbgPrint("Error: SetVolumeMountPoint failed : %d\n", GetLastError());
			return FALSE;
		}
	}
	*/

	//CreateMountPoint(L"C:\\mount\\dokan", L"\\??\\E:\\test4");
	//CreateMountPoint(L"C:\\mount\\dokan", L"\\??\\Volume{dca0e0a5-d2ca-4f0f-8416-a6414657a77a}\\");
	//CreateMountPoint(L"C:\\mount\\dokan", mountPoint);

    device = CreateFile(
        volumeName,
        GENERIC_READ | GENERIC_WRITE,
        FILE_SHARE_READ | FILE_SHARE_WRITE,
        NULL,
        OPEN_EXISTING,
        FILE_FLAG_NO_BUFFERING,
        NULL
        );

    if (device == INVALID_HANDLE_VALUE) {
		DbgPrintW(L"DokanControl Mount %ws failed:%d\n", volumeName, GetLastError());
        DefineDosDevice(DDD_REMOVE_DEFINITION, &volumeName[4], NULL);
        return FALSE;
    }

	CloseHandle(device);

    return TRUE;
}


BOOL
DokanControlUnmount(
	WCHAR DriveLetter)
{
    WCHAR   volumeName[] = L"\\\\.\\ :";
    HANDLE  device;

    volumeName[4] = DriveLetter;
/*
    device = CreateFile(
        volumeName,
        GENERIC_READ | GENERIC_WRITE,
        FILE_SHARE_READ | FILE_SHARE_WRITE,
        NULL,
        OPEN_EXISTING,
        FILE_FLAG_NO_BUFFERING,
        NULL
        );

    if (device == INVALID_HANDLE_VALUE) {
		DbgPrintW(L"DriveLetter %wc\n", DriveLetter);
        DbgPrintW(L"DokanControl Unmount failed\n");
        return FALSE;
    }

    CloseHandle(device);
*/
    if (!DefineDosDevice(DDD_REMOVE_DEFINITION, &volumeName[4], NULL)) {
		DbgPrintW(L"DriveLetter %wc\n", DriveLetter);
        DbgPrintW(L"DokanControl DefineDosDevice failed\n");
        return FALSE;
	} else {
		DbgPrintW(L"DokanControl DD_REMOVE_DEFINITION success\n");
	}

	DeleteMountPoint(L"C:\\mount\\dokan");

	return TRUE;
}
