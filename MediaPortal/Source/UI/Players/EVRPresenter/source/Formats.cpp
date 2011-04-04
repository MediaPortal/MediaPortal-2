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

#include <mfapi.h>

#include "EVRCustomPresenter.h"
#include "MediaType.h"

// Sets or clears the presenter's media type. 
HRESULT EVRCustomPresenter::SetMediaType(IMFMediaType *pMediaType)
{
  Log("EVRCustomPresenter::SetMediaType");

  // Note: pMediaType can be NULL (to clear the type)

  // Clearing the media type is allowed in any state (including shutdown).
  if (pMediaType == NULL)
  {
    SAFE_RELEASE(m_pMediaType);
    ReleaseResources();
    return S_OK;
  }

  HRESULT hr = S_OK;
  MFRatio fps = { 0, 0 };
  VideoSampleList sampleQueue;
  
  IMFSample *pSample = NULL;

  // Cannot set the media type after shutdown.
  hr = CheckShutdown();
  if (FAILED(hr))
  {
    ReleaseResources();
    return hr;
  }

  // Check if the new type is actually different.
  // Note: This function safely handles NULL input parameters.
  if (AreMediaTypesEqual(m_pMediaType, pMediaType))  
  {
    return S_OK; // Nothing more to do.
  }

  // We're really changing the type. First get rid of the old type.
  SAFE_RELEASE(m_pMediaType);
  ReleaseResources();

  // Initialize the presenter engine with the new media type.
  // The presenter engine allocates the samples. 

  hr = m_pD3DPresentEngine->CreateVideoSamples(pMediaType, sampleQueue);
  if (FAILED(hr))
  {
    ReleaseResources();
    return hr;
  }

  // Mark each sample with our token counter. If this batch of samples becomes
  // invalid, we increment the counter, so that we know they should be discarded. 
  for (VideoSampleList::POSITION pos = sampleQueue.FrontPosition(); pos != sampleQueue.EndPosition(); pos = sampleQueue.Next(pos))
  {
    hr = sampleQueue.GetItemPos(pos, &pSample);
    if (FAILED(hr))
    {
      ReleaseResources();
      return hr;
    }
    hr = pSample->SetUINT32(MFSamplePresenter_SampleCounter, m_TokenCounter);
    if (FAILED(hr))
    {
      ReleaseResources();
      return hr;
    }
    SAFE_RELEASE(pSample);
  }

  // Add the samples to the sample pool.
  hr = m_SamplePool.Initialize(sampleQueue);
  if (FAILED(hr))
  {
    ReleaseResources();
    return hr;
  }

  // Helper object for reading the proposed type.
  VideoType videoType(pMediaType);

  // Set the frame rate on the scheduler. 
  if (SUCCEEDED(videoType.GetFrameRate(pMediaType, &fps)) && (fps.Numerator != 0) && (fps.Denominator != 0))
  {
    m_scheduler.SetFrameRate(fps);
  }
  else
  {
    // NOTE: The mixer's proposed type might not have a frame rate, in which case 
    // we'll use an arbitary default. (Although it's unlikely the video source
    // does not have a frame rate.)
    m_scheduler.SetFrameRate(g_DefaultFrameRate);
  }

  // Store the media type.
  assert(pMediaType != NULL);
  m_pMediaType = pMediaType;
  m_pMediaType->AddRef();

  return hr;
}


// Queries whether the presenter can use a proposed format from the mixer.
HRESULT EVRCustomPresenter::IsMediaTypeSupported(IMFMediaType *pMediaType)
{
  Log("EVRCustomPresenter::IsMediaTypeSupported");
  
  HRESULT                 hr = S_OK;
  D3DFORMAT               d3dFormat = D3DFMT_UNKNOWN;
  BOOL                    bCompressed = FALSE;
  MFVideoInterlaceMode    InterlaceMode = MFVideoInterlace_Unknown;
  MFVideoArea             VideoCropArea;
  UINT32                  width = 0, height = 0;

  // Helper object for reading the proposed type.
  VideoType               mtProposed(pMediaType);

  // Reject compressed media types.
  hr = mtProposed.IsCompressedFormat(&bCompressed);
  CHECK_HR(hr, "EVRCustomPresenter::IsMediaTypeSupported VideoType::IsCompressedFormat() failed");
  if (bCompressed)
  {
    hr = MF_E_INVALIDMEDIATYPE;
    CHECK_HR(hr, "EVRCustomPresenter::IsMediaTypeSupported compressed format");
  }

  // Validate the format.
  hr = mtProposed.GetFourCC((DWORD*)&d3dFormat);
  CHECK_HR(hr, "EVRCustomPresenter::IsMediaTypeSupported VideoType::GetFourCC() failed");

  // The D3DPresentEngine checks whether the format can be used as
  // the back-buffer format for the swap chains.
  hr = m_pD3DPresentEngine->CheckFormat(d3dFormat);
  CHECK_HR(hr, "EVRCustomPresenter::IsMediaTypeSupported D3DPresentEngine::CheckFormat() failed");

  // Reject interlaced formats.
  hr = mtProposed.GetInterlaceMode(&InterlaceMode);
  CHECK_HR(hr, "EVRCustomPresenter::IsMediaTypeSupported VideoType::GetInterlaceMode() failed");
  if (InterlaceMode != MFVideoInterlace_Progressive)
  {
    hr = MF_E_INVALIDMEDIATYPE;
    CHECK_HR(hr, "EVRCustomPresenter::IsMediaTypeSupported interlaced mode");
  }

  hr = mtProposed.GetFrameDimensions(&width, &height);
  CHECK_HR(hr, "EVRCustomPresenter::IsMediaTypeSupported VideoType::GetFrameDimensions() failed");

  // Validate the various apertures (cropping regions) against the frame size.
  // Any of these apertures may be unspecified in the media type, in which case 
  // we ignore it. We just want to reject invalid apertures.
  if (SUCCEEDED(mtProposed.GetPanScanAperture(&VideoCropArea)))
  {
    ValidateVideoArea(VideoCropArea, width, height);
  }
  if (SUCCEEDED(mtProposed.GetGeometricAperture(&VideoCropArea)))
  {
    ValidateVideoArea(VideoCropArea, width, height);
  }
  if (SUCCEEDED(mtProposed.GetMinDisplayAperture(&VideoCropArea)))
  {
    ValidateVideoArea(VideoCropArea, width, height);
  }

  return hr;
}


// Converts a proposed media type from the mixer into a type that is suitable for the presenter.
HRESULT EVRCustomPresenter::CreateOptimalVideoType(IMFMediaType* pProposedType, IMFMediaType **ppOptimalType)
{
  Log("EVRCustomPresenter::CreateOptimalVideoType");

  HRESULT hr = S_OK;
    
  RECT rcOutput;
  ZeroMemory(&rcOutput, sizeof(rcOutput));

  MFVideoArea displayArea;
  ZeroMemory(&displayArea, sizeof(displayArea));

  // Helper object to manipulate the optimal type.
  VideoType mtOptimal;

  // Create empty MediaType
  hr = mtOptimal.CreateEmptyType();
  CHECK_HR(hr, "EVRCustomPresenter:CreateOptimalVideoType MediaType::CreateEmptyType() failed");

  // Clone the proposed type.
  hr = mtOptimal.CopyFrom(pProposedType);
  CHECK_HR(hr, "EVRCustomPresenter::CreateOptimalVideoType MediaType::CopyFrom() failed");

  // Modify the new type.

  // For purposes of this SDK sample, we assume 
  // 1) The monitor's pixels are square.
  // 2) The presenter always preserves the pixel aspect ratio.

  // Set the pixel aspect ratio (PAR) to 1:1 (see assumption #1, above)
  hr = mtOptimal.SetPixelAspectRatio(1, 1);
  CHECK_HR(hr, "EVRCustomPresenter::CreateOptimalVideoType VideoType::SetPixelAspectRatio() failed");

  // Calculate the output rectangle based on the media type.
  hr = CalculateOutputRectangle(pProposedType, &rcOutput);
  CHECK_HR(hr, "EVRCustomPresenter::CreateOptimalVideoType EVRCustomPresenter::CalculateOutputRectangle() failed");

  // Set the extended color information: Use BT.709 
  hr = mtOptimal.SetYUVMatrix(MFVideoTransferMatrix_BT709);
  CHECK_HR(hr, "EVRCustomPresenter::CreateOptimalVideoType VideoType::SetYUVMatrix() failed");

  hr = mtOptimal.SetTransferFunction(MFVideoTransFunc_709);
  CHECK_HR(hr, "EVRCustomPresenter::CreateOptimalVideoType VideoType::SetTransferFunction() failed");

  hr = mtOptimal.SetVideoPrimaries(MFVideoPrimaries_BT709);
  CHECK_HR(hr, "EVRCustomPresenter::CreateOptimalVideoType VideoType::SetVideoPrimaries() failed");

  hr = mtOptimal.SetVideoNominalRange(MFNominalRange_0_255);
  CHECK_HR(hr, "EVRCustomPresenter::CreateOptimalVideoType VideoType::SetVideoNominalRange() failed");

  hr = mtOptimal.SetVideoLighting(MFVideoLighting_dim);
  CHECK_HR(hr, "EVRCustomPresenter::CreateOptimalVideoType VideoType::SetVideoLightning() failed");

  // Set the target rect dimensions. 
  hr = mtOptimal.SetFrameDimensions(rcOutput.right, rcOutput.bottom);
  CHECK_HR(hr, "EVRCustomPresenter::CreateOptimalVideoType VideoType::SetFrameDimensions() failed");

  // Set the geometric aperture, and disable pan/scan.
  displayArea = mtOptimal.MakeArea(0, 0, rcOutput.right, rcOutput.bottom);

  hr = mtOptimal.SetPanScanEnabled(FALSE);
  CHECK_HR(hr, "EVRCustomPresenter::CreateOptimalVideoType VideoType::SetPanScanEnabled() failed");

  hr = mtOptimal.SetGeometricAperture(displayArea);
  CHECK_HR(hr, "EVRCustomPresenter::CreateOptimalVideoType VideoType::SetGeometricAperture() failed");

  // Set the pan/scan aperture and the minimum display aperture. We don't care
  // about them per se, but the mixer will reject the type if these exceed the 
  // frame dimentions.
  hr = mtOptimal.SetPanScanAperture(displayArea);
  CHECK_HR(hr, "EVRCustomPresenter::CreateOptimalVideoType VideoType::SetPanScanAperture() failed");

  hr = mtOptimal.SetMinDisplayAperture(displayArea);
  CHECK_HR(hr, "EVRCustomPresenter::CreateOptimalVideoType VideoType::SetMinDisplayAperture() failed");

  // Return the pointer to the caller.
  *ppOptimalType = mtOptimal.Detach();

  return hr;
}


// Calculates the destination rectangle based on the mixer's proposed format.
HRESULT EVRCustomPresenter::CalculateOutputRectangle(IMFMediaType *pProposedType, RECT *prcOutput)
{
  HRESULT hr = S_OK;
  UINT32  srcWidth = 0, srcHeight = 0;

  MFRatio inputPAR = { 0, 0 };
  MFRatio outputPAR = { 0, 0 };
  RECT    rcOutput = { 0, 0, 0, 0};

  MFVideoArea displayArea;
  ZeroMemory(&displayArea, sizeof(displayArea));

  // Helper object to read the media type.
  VideoType mtProposed(pProposedType);

  // Get the source's frame dimensions.
  hr = mtProposed.GetFrameDimensions(&srcWidth, &srcHeight);
  CHECK_HR(hr, "EVRCustomPresenter::CalculateOutputRectangle VideoType::GetFrameDimensions() failed");

  // Get the source's display area. 
  hr = mtProposed.GetVideoDisplayArea(&displayArea);
  CHECK_HR(hr, "EVRCustomPresenter::CalculateOutputRectangle VideoType::GetVideoDisplayArea() failed");

  // Calculate the x,y offsets of the display area.
  LONG offsetX = (LONG)MFOffsetToFloat(displayArea.OffsetX);
  LONG offsetY = (LONG)MFOffsetToFloat(displayArea.OffsetY);

  // Use the display area if valid. Otherwise, use the entire frame.
  if (displayArea.Area.cx != 0 &&
    displayArea.Area.cy != 0 &&
    offsetX + displayArea.Area.cx <= (LONG)(srcWidth) &&
    offsetY + displayArea.Area.cy <= (LONG)(srcHeight))
  {
    rcOutput.left   = offsetX;
    rcOutput.right  = offsetX + displayArea.Area.cx;
    rcOutput.top    = offsetY;
    rcOutput.bottom = offsetY + displayArea.Area.cy;
  }
  else
  {
    rcOutput.left = 0;
    rcOutput.top = 0;
    rcOutput.right = srcWidth;
    rcOutput.bottom = srcHeight;
  }

  // rcOutput is now either a sub-rectangle of the video frame, or the entire frame.

  // If the pixel aspect ratio of the proposed media type is different from the monitor's, 
  // letterbox the video. We stretch the image rather than shrink it.

  inputPAR = mtProposed.GetPixelAspectRatio();    // Defaults to 1:1

  outputPAR.Denominator = outputPAR.Numerator = 1; // This is an assumption of the sample.

  // Adjust to get the correct picture aspect ratio.
  *prcOutput = CorrectAspectRatio(rcOutput, inputPAR, outputPAR);

  return hr;
}


