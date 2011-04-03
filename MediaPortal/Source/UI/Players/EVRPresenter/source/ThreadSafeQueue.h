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

#include "ComPtrList.h"
#include "CritSec.h"


template <class T>
class ThreadSafeQueue
{
public:
  HRESULT Enqueue(T *p)
  {
    AutoLock lock(m_lock);
    return m_list.InsertBack(p);
  }


  HRESULT Dequeue(T **pp)
  {
    AutoLock lock(m_lock);

    if (m_list.IsEmpty())
    {
      *pp = NULL;
      return S_FALSE;
    }

    return m_list.RemoveFront(pp);
  }


  HRESULT PutBack(T *p)
  {
    AutoLock lock(m_lock);
    return m_list.InsertFront(p);
  }


  void Clear() 
  {
    AutoLock lock(m_lock);
    m_list.Clear();
  }


  bool IsEmpty()
  {
    AutoLock lock(m_lock);
    return m_list.IsEmpty();
  }

private:
  CritSec         m_lock; 
  ComPtrList<T>   m_list;
};

