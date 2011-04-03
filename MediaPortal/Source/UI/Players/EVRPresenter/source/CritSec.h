//////////////////////////////////////////////////////////////////////////
//
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO
// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
//
// Copyright (c) Microsoft Corporation. All rights reserved.
//
//////////////////////////////////////////////////////////////////////////

#pragma once

class CritSec
{
private:
  CRITICAL_SECTION m_criticalSection;
public:
  CritSec()
  {
    InitializeCriticalSection(&m_criticalSection);
  }

  ~CritSec()
  {
    DeleteCriticalSection(&m_criticalSection);
  }

  void Lock()
  {
    EnterCriticalSection(&m_criticalSection);
  }

  void Unlock()
  {
    LeaveCriticalSection(&m_criticalSection);
  }
};



class AutoLock
{
private:
  CritSec *m_pCriticalSection;
public:
  AutoLock(CritSec& crit)
  {
    m_pCriticalSection = &crit;
    m_pCriticalSection->Lock();
  }
  ~AutoLock()
  {
    m_pCriticalSection->Unlock();
  }
};


