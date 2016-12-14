#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Runtime.InteropServices;
using DirectShow;
using MediaPortal.UI.Players.Video.Settings;
using SharpDX;

namespace MediaPortal.UI.Players.Video.Subtitles
{
  public class MpcSubtitles
  {
    //set default subtitle's style (call before LoadSubtitles to take effect)
    [DllImport("mpcSubs.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true, CharSet = CharSet.Unicode)]
    public static extern void SetDefaultStyle([In] ref SubtitleStyle style, bool overrideUserStyle);

    //load subtitles for video file filename, with given (rendered) graph 
    [DllImport("mpcSubs.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true, CharSet = CharSet.Unicode)]
    public static extern bool LoadSubtitles(IntPtr d3DDev, Size2 size, string filename, IGraphBuilder graphBuilder,
                                            string paths, int lcidCI);

    //set sample time (set from EVR presenter, not used in case of vmr9)
    [DllImport("mpcSubs.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void SetTime(long nsSampleTime);

    [DllImport("mpcSubs.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void SaveToDisk();

    [DllImport("mpcSubs.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern bool IsModified();

    ////
    //subs management functions
    ///
    [DllImport("mpcSubs.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int GetCount();

    [DllImport("mpcSubs.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.BStr)]
    public static extern string GetLanguage(int i);

    [DllImport("mpcSubs.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.BStr)]
    public static extern string GetTrackName(int i);

    [DllImport("mpcSubs.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int GetCurrent();

    [DllImport("mpcSubs.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void SetCurrent(int current);

    [DllImport("mpcSubs.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern bool GetEnable();

    [DllImport("mpcSubs.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void SetEnable(bool enable);

    [DllImport("mpcSubs.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void Render(int x, int y, int width, int height);

    [DllImport("mpcSubs.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int GetDelay();

    //in milliseconds

    [DllImport("mpcSubs.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void SetDelay(int delay_ms);

    [DllImport("mpcSubs.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void FreeSubtitles();

    [DllImport("mpcSubs.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void SetAdvancedOptions(int subPicsBufferAhead, Size2 textureSize, bool pow2tex,
                                                 bool disableAnimation);

    [DllImport("mpcSubs.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void SetShowForcedOnly(bool onlyShowForcedSubs);
  }
}
