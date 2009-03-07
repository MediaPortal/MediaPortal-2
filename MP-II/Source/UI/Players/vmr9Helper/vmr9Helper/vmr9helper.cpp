#include <windows.h>
#include <streams.h>
#include <stdio.h>
#include <atlbase.h>

#include <d3d9.h>
#include <dshow.h>
#include <vmr9.h>
#include <list>
#include <vector>
using namespace std;

#include <comutil.h>
#include <evr.h>
#include <dxva2api.h>
#include "IVmr9Callback.h"
#include "Vmr9CustomAllocator.h"
#include "EVRCustomPresenter.h"

void Log(const char *fmt, ...) ;

HMODULE m_hModuleDXVA2 = NULL;
HMODULE m_hModuleEVR = NULL;
HMODULE m_hModuleMFPLAT = NULL;

TDXVA2CreateDirect3DDeviceManager9* m_pDXVA2CreateDirect3DDeviceManager9 = NULL;
TMFCreateVideoSampleFromSurface* m_pMFCreateVideoSampleFromSurface = NULL;
TMFCreateMediaType* m_pMFCreateMediaType = NULL;
BOOL m_bEVRLoaded = false;

static list<Vmr9CustomAllocator*> m_vmr9Allocators;
typedef list<Vmr9CustomAllocator*>::iterator itvmr9Allocator;


static list<EVRCustomPresenter*> m_evrPresenters;
typedef list<EVRCustomPresenter*>::iterator itevrAllocator;

__declspec(dllexport) int Vmr9Init(IVMR9Callback* callback, DWORD dwD3DDevice, IBaseFilter* vmr9Filter,DWORD monitor)
{
  int id=0;
  while (true)
  {
    bool inUse=false;
    for (itvmr9Allocator it=m_vmr9Allocators.begin(); it !=m_vmr9Allocators.end();it++)
    {
      Vmr9CustomAllocator* allocator=*it;
      if (allocator->Id()==id)
      {
        inUse=true;
        break;
      }
    }
    if (!inUse) break;
    id++;
  }

  LPDIRECT3DDEVICE9 pDirect3dDevice= (LPDIRECT3DDEVICE9)(dwD3DDevice);
  Vmr9CustomAllocator* allocator = new Vmr9CustomAllocator(id, pDirect3dDevice,callback, (HMONITOR) monitor);
  m_vmr9Allocators.push_back(allocator);

  allocator->QueryInterface(IID_IVMRSurfaceAllocator9,(void**)&allocator);


  HRESULT hr;
  CComQIPtr<IVMRSurfaceAllocatorNotify9> pSAN = vmr9Filter;   
 	if(FAILED(hr = pSAN->AdviseSurfaceAllocator(id, allocator)))
 	{
   		Log("Vmr9:Init() AdviseSurfaceAllocator() failed 0x:%x",hr);
 		return -1;
 	}
 	if (FAILED(hr = allocator->AdviseNotify(pSAN)))
 	{
 		Log("Vmr9:Init() AdviseNotify() failed 0x:%x",hr);
 		return -1;
	}
  Log("Vmr9:Init() success id:%d",id);
  return id;

}
__declspec(dllexport) void Vmr9DeInit(int id)
{
   	Log("Vmr9DeInit() id:%d", id);
    for (itvmr9Allocator it=m_vmr9Allocators.begin(); it !=m_vmr9Allocators.end();it++)
    {
      Vmr9CustomAllocator* allocator=*it;
      if (allocator->Id()==id)
      {
        m_vmr9Allocators.erase(it);

   	    Log("Vmr9DeInit()   release allocator");
        
        return;
      }
    }
}

__declspec(dllexport) void Vmr9FreeResources(int id)
{
	  Log("Vmr9FreeResources:%d", id);
    for (itvmr9Allocator it=m_vmr9Allocators.begin(); it !=m_vmr9Allocators.end();it++)
    {
      Vmr9CustomAllocator* allocator=*it;
      if (allocator->Id()==id)
      {
        allocator->FreeDirectxResources();
        return;
      }
    }
}


__declspec(dllexport) void Vmr9ReAllocResources(int id)
{
	  Log("Vmr9ReAllocResources:%d", id);
    for (itvmr9Allocator it=m_vmr9Allocators.begin(); it !=m_vmr9Allocators.end();it++)
    {
      Vmr9CustomAllocator* allocator=*it;
      if (allocator->Id()==id)
      {
        allocator->ReAllocDirectxResources();
        
        return;
      }
    }
}





void UnloadEVR()
{
  Log("Unloading EVR libraries");
  if (m_hModuleDXVA2!=NULL)
  {

	  Log("Freeing library DXVA2.dll");

    if (!FreeLibrary(m_hModuleDXVA2))
	{
		Log("DXVA2.dll could not be unloaded!");
	}
	m_hModuleDXVA2 = NULL;
  }
  if (m_hModuleEVR!=NULL)
  {
	  Log("Freeing lib: EVR.dll");
	  if ( !FreeLibrary(m_hModuleEVR) )
	  {
		  Log("EVR.dll could not be unloaded");
	  }
	m_hModuleEVR = NULL;
  }
  if (m_hModuleMFPLAT!=NULL)
  {
	  Log("Freeing lib: MFPLAT.dll");
	  if ( !FreeLibrary(m_hModuleMFPLAT) )
	  {
		  Log("MFPLAT.dll could not be unloaded");
	  }
	m_hModuleMFPLAT = NULL;
  }
  
	  Log("Freeing lib: MFPLAT.dll done");
}

bool LoadEVR()
{
	Log("Loading EVR libraries");
  char systemFolder[MAX_PATH];
  char mfDLLFileName[MAX_PATH];
  GetSystemDirectory(systemFolder,sizeof(systemFolder));
  sprintf(mfDLLFileName,"%s\\dxva2.dll", systemFolder);
  m_hModuleDXVA2=LoadLibrary(mfDLLFileName);
  if (m_hModuleDXVA2!=NULL)
  {
	  Log("Found dxva2.dll");
    m_pDXVA2CreateDirect3DDeviceManager9=(TDXVA2CreateDirect3DDeviceManager9*)GetProcAddress(m_hModuleDXVA2,"DXVA2CreateDirect3DDeviceManager9");
    if (m_pDXVA2CreateDirect3DDeviceManager9!=NULL)
    {
		Log("Found method DXVA2CreateDirect3DDeviceManager9");
      sprintf(mfDLLFileName,"%s\\evr.dll", systemFolder);
      m_hModuleEVR=LoadLibrary(mfDLLFileName);
      m_pMFCreateVideoSampleFromSurface=(TMFCreateVideoSampleFromSurface*)GetProcAddress(m_hModuleEVR,"MFCreateVideoSampleFromSurface");
	  if ( m_pMFCreateVideoSampleFromSurface )
	  {
		  Log("Found method MFCreateVideoSampleFromSurface");
		  sprintf(mfDLLFileName,"%s\\mfplat.dll", systemFolder);
		  m_hModuleMFPLAT=LoadLibrary(mfDLLFileName);
		  m_pMFCreateMediaType=(TMFCreateMediaType*)GetProcAddress(m_hModuleMFPLAT,"MFCreateMediaType");
		  if ( m_pMFCreateMediaType )
		  {
			  Log("Found method MFCreateMediaType");
			  Log("Successfully loaded EVR dlls");
			  return TRUE;
		  }
	  }
	}
  }
  Log("Could not find all dependencies for EVR!");
  UnloadEVR();
  return FALSE;
}



__declspec(dllexport) int EvrInit(IVMR9Callback* callback, DWORD dwD3DDevice, IBaseFilter* evrFilter,DWORD monitor)
{
	HRESULT hr;
	m_bEVRLoaded = LoadEVR();
	if ( !m_bEVRLoaded ) 
	{
		Log("EVR libraries are not loaded. Cannot init EVR");
		return -1;
	}

	CComQIPtr<IMFVideoRenderer> pRenderer = evrFilter;
	if (!pRenderer) 
   {
		Log("Could not get IMFVideoRenderer");
		return -1;
	}
  

  int id=0;
  while (true)
  {
    bool inUse=false;
    for (itevrAllocator it=m_evrPresenters.begin(); it !=m_evrPresenters.end();it++)
    {
      EVRCustomPresenter* allocator=*it;
      if (allocator->Id()==id)
      {
        inUse=true;
        break;
      }
    }
    if (!inUse) break;
    id++;
  }

	EVRCustomPresenter* presenter = new EVRCustomPresenter(id,callback, (LPDIRECT3DDEVICE9)(dwD3DDevice), (HMONITOR)monitor);
  m_evrPresenters.push_back(presenter);
  hr = pRenderer->InitializeRenderer(NULL, presenter);
  if (FAILED(hr) ) 
  {
	  Log("InitializeRenderer failed: 0x%x", hr);
    pRenderer.Release();
	  return -1;
  }
  pRenderer.Release();
	return id;
}


__declspec(dllexport) void EvrDeinit(int id)
{
  try
  {
    
		Log("EvrDeinit: release:%d", id);
    for (itevrAllocator it=m_evrPresenters.begin(); it !=m_evrPresenters.end();it++)
    {
      EVRCustomPresenter* allocator=*it;
      if (allocator->Id()==id)
      {
        int hr;
        do
		    {
			    hr=allocator->Release();
		    } while (hr>0);
        m_evrPresenters.erase(it);

   	    Log("EvrDeinit()   release allocator");

	      allocator=NULL;
	      Log("EvrDeinit:m_evrPresenter released");
        break;
      }
    }

	  if ( m_evrPresenters.size() ==0 && m_bEVRLoaded )
	  {
		  UnloadEVR();
		  m_bEVRLoaded = FALSE;
	  }
  }
  catch(...)
  {
		  Log("EvrDeinit:exception");
  }
}
__declspec(dllexport) void EvrEnableFrameSkipping(int handle, bool onOff)
{
  for (itevrAllocator it=m_evrPresenters.begin(); it !=m_evrPresenters.end();it++)
  {
    EVRCustomPresenter* allocator=*it;
    if (allocator->Id()==handle)
    {
      allocator->EnableFrameSkipping(onOff);
    }
  }
}


__declspec(dllexport) void EvrFreeResources(int id)
{
	Log("EvrFreeResources:%d", id);
  for (itevrAllocator it=m_evrPresenters.begin(); it !=m_evrPresenters.end();it++)
  {
    EVRCustomPresenter* allocator=*it;
    if (allocator->Id()==id)
    {
        allocator->FreeDirectxResources();
        return;
    }
  }
}


__declspec(dllexport) void EvrReAllocResources(int id)
{
	Log("EvrReAllocResources:%d", id);
  for (itevrAllocator it=m_evrPresenters.begin(); it !=m_evrPresenters.end();it++)
  {
    EVRCustomPresenter* allocator=*it;
    if (allocator->Id()==id)
    {
        allocator->ReAllocDirectxResources();
        
        return;
    }
  }
}





void Log(const char *fmt, ...) 
{
	va_list ap;
	va_start(ap,fmt);

	char buffer[1000]; 
	int tmp;
	va_start(ap,fmt);
	tmp=vsprintf(buffer, fmt, ap);
	va_end(ap); 

	FILE* fp = fopen("log/vmr9.log","a+");
	if (fp!=NULL)
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
};

HRESULT __fastcall UnicodeToAnsi(LPCOLESTR pszW, LPSTR* ppszA)
{

    ULONG cbAnsi, cCharacters;
    DWORD dwError;

    // If input is null then just return the same.
    if (pszW == NULL)
    {
        *ppszA = NULL;
        return NOERROR;
    }

    cCharacters = wcslen(pszW)+1;
    // Determine number of bytes to be allocated for ANSI string. An
    // ANSI string can have at most 2 bytes per character (for Double
    // Byte Character Strings.)
    cbAnsi = cCharacters*2;

    // Use of the OLE allocator is not required because the resultant
    // ANSI  string will never be passed to another COM component. You
    // can use your own allocator.
    *ppszA = (LPSTR) CoTaskMemAlloc(cbAnsi);
    if (NULL == *ppszA)
        return E_OUTOFMEMORY;

    // Convert to ANSI.
    if (0 == WideCharToMultiByte(CP_ACP, 0, pszW, cCharacters, *ppszA,
                  cbAnsi, NULL, NULL))
    {
        dwError = GetLastError();
        CoTaskMemFree(*ppszA);
        *ppszA = NULL;
        return HRESULT_FROM_WIN32(dwError);
    }
    return NOERROR;

}
