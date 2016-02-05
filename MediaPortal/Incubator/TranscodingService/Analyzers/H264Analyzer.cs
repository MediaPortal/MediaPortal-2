#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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

//Thanks goes to Jay Codec over at https://github.com/jcodec/jcodec
//His code provided insights into the decoding of the Annex B header
//The support classes in this class keep their properties named after the standard and do not follow MediaPortal naming conventions

namespace MediaPortal.Plugins.Transcoding.Service.Analyzers
{
  public class H264Analyzer
  {
    private byte[] buffer;
    private int profile = 0;
    private int level = 0;
    private int refFrames = 0;
    private long currrentPos = 0;

    public H264HeaderProfile HeaderProfile
    {
      get
      {
        return (H264HeaderProfile)profile;
      }
    }

    public float HeaderLevel
    {
      get
      {
        return (float)level / 10F;
      }
    }

    public int HeaderRefFrames
    {
      get
      {
        return refFrames;
      }
    }

    #region Enums

    public enum H264HeaderProfile
    {
      Unknown = 0,
      ConstrainedBaseline = 44,
      Baseline = 66,
      Main = 77,
      Extended = 88,
      High = 100,
      High_10 = 110,
      High_422 = 122,
      High_444 = 244
    }

    public enum ChromaFormat
    {
      Monochrome,
      Yuv420,
      Yuv422,
      Yuv444
    }

    private enum NALUnitType
    {
      Unknown,
      NonIDRSlice,
      PartASlice,
      PartBSlice,
      PartCSlice,
      IDRSlice,
      SupplementalEnhancementInformation,
      SequenceParameterSet,
      PictureParameterSet,
      AccessUnitDelimiter,
      EndOfSequence,
      EndOfStream,
      FillerData,
      SequenceParameterSetExtension,
      Prefix,
      SubsetSequenceParameterSet,
      Reserved1,
      Reserved2,
      Reserved3,
      AuxiliarySlice,
      ExtensionSlice,
      DepthExtensionSlice
    }

    #endregion

    #region Classes

    private class BitStreamReader
    {
      private int curByte;
      private int nBitLeft;
      private int nByte = -1;
      private byte[] data = null;

      public BitStreamReader(byte[] BinaryArray)
      {
        data = BinaryArray;
        curByte = GetByte();
      }

      private int GetByte()
      {
        if (nByte < data.Length - 1)
        {
          nBitLeft = 8;
          nByte++;
          return data[nByte];
        }
        return 0;
      }

      public int Read1Bit()
      {
        int res = 0;
        if (nBitLeft == 0 && nByte == data.Length - 1)
        {
          return 0;
        }
        nBitLeft--;
        res = (curByte >> nBitLeft) & 0x01;
        if (nBitLeft == 0)
        {
          curByte = GetByte();
        }
        return res;
      }

      public int ReadNBit(int n)
      {
        if (n > 32)
        {
          throw new Exception("Cannot read more than 32 bit");
        }
        int res = 0;
        for (int i = 0; i < n; i++)
        {
          res |= (Read1Bit() << (n - i - 1));
        }
        return res;
      }

      public int ReadUE()
      {
        int cnt = 0;
        while (Read1Bit() == 0 && cnt < 32)
        {
          cnt++;
        }

        int res = 0;
        if (cnt > 0)
        {
          res = ReadNBit(cnt);
          res += (1 << cnt) - 1;
        }
        return res;
      }

      public int ReadSE()
      {
        int res = ReadUE();
        if ((res & 0x01) > 0)
        {
          res = (res + 1) / 2;
        }
        else
        {
          res = -(res / 2);
        }
        return res;
      }

      public bool ReadBool()
      {
        return Read1Bit() == 1;
      }

      public int ReadU(int n)
      {
        return ReadNBit(n);
      }
    }

    private class ScalingList
    {
      public int[] scalingList;
      public bool useDefaultScalingMatrixFlag;
      public static ScalingList Read(BitStreamReader bitReader, int sizeOfScalingList)
      {

        ScalingList sl = new ScalingList();
        sl.scalingList = new int[sizeOfScalingList];
        int lastScale = 8;
        int nextScale = 8;
        for (int j = 0; j < sizeOfScalingList; j++)
        {
          if (nextScale != 0)
          {
            int deltaScale = bitReader.ReadSE();
            nextScale = (lastScale + deltaScale + 256) % 256;
            sl.useDefaultScalingMatrixFlag = (j == 0 && nextScale == 0);
          }
          sl.scalingList[j] = nextScale == 0 ? lastScale : nextScale;
          lastScale = sl.scalingList[j];
        }
        return sl;
      }
    }

    private class ScalingMatrix
    {
      public ScalingList[] ScalingList4x4;
      public ScalingList[] ScalingList8x8;
    }

    private class HRDParameters
    {
      public int cpb_cnt_minus1;
      public int bit_rate_scale;
      public int cpb_size_scale;
      public int[] bit_rate_value_minus1;
      public int[] cpb_size_value_minus1;
      public bool[] cbr_flag;
      public int initial_cpb_removal_delay_length_minus1;
      public int cpb_removal_delay_length_minus1;
      public int dpb_output_delay_length_minus1;
      public int time_offset_length;
    }

    private class VUIParameters
    {
      public class BitstreamRestriction
      {
        public bool motion_vectors_over_pic_boundaries_flag;
        public int max_bytes_per_pic_denom;
        public int max_bits_per_mb_denom;
        public int log2_max_mv_length_horizontal;
        public int log2_max_mv_length_vertical;
        public int num_reorder_frames;
        public int max_dec_frame_buffering;
      }

      public bool aspect_ratio_info_present_flag;
      public int sar_width;
      public int sar_height;
      public bool overscan_info_present_flag;
      public bool overscan_appropriate_flag;
      public bool video_signal_type_present_flag;
      public int video_format;
      public bool video_full_range_flag;
      public bool colour_description_present_flag;
      public int colour_primaries;
      public int transfer_characteristics;
      public int matrix_coefficients;
      public bool chroma_loc_info_present_flag;
      public int chroma_sample_loc_type_top_field;
      public int chroma_sample_loc_type_bottom_field;
      public bool timing_info_present_flag;
      public int num_units_in_tick;
      public int time_scale;
      public bool fixed_frame_rate_flag;
      public bool low_delay_hrd_flag;
      public bool pic_struct_present_flag;
      public HRDParameters nalHRDParams = null;
      public HRDParameters vclHRDParams = null;

      public BitstreamRestriction bitstreamRestriction;
      public int aspect_ratio;
    }

    private class SeqParameterSet
    {
      public int pic_order_cnt_type;
      public bool delta_pic_order_always_zero_flag;
      public bool mb_adaptive_frame_field_flag;
      public bool direct_8x8_inference_flag;
      public ChromaFormat chroma_format_idc;
      public int log2_max_frame_num_minus4;
      public int log2_max_pic_order_cnt_lsb_minus4;
      public int pic_height_in_map_units_minus1;
      public int pic_width_in_mbs_minus1;
      public int bit_depth_luma_minus8;
      public int bit_depth_chroma_minus8;
      public bool qpprime_y_zero_transform_bypass_flag;
      public int profile_idc;
      public bool constraint_set_0_flag;
      public bool constraint_set_1_flag;
      public bool constraint_set_2_flag;
      public bool constraint_set_3_flag;
      public int level_idc;
      public int seq_parameter_set_id;
      public bool residual_color_transform_flag;
      public int offset_for_non_ref_pic;
      public int offset_for_top_to_bottom_field;
      public int num_ref_frames;
      public bool gaps_in_frame_num_value_allowed_flag;
      public bool frame_mbs_only_flag;
      public bool frame_cropping_flag;
      public int frame_crop_left_offset;
      public int frame_crop_right_offset;
      public int frame_crop_top_offset;
      public int frame_crop_bottom_offset;
      public int[] offsetForRefFrame;
      public VUIParameters vuiParams = null;
      public ScalingMatrix scalingMatrix = null;
      public int num_ref_frames_in_pic_order_cnt_cycle;

      public static SeqParameterSet Read(byte[] NALData)
      {
        BitStreamReader bitReader = new BitStreamReader(NALData);
        SeqParameterSet sps = new SeqParameterSet();

        sps.profile_idc = bitReader.ReadNBit(8);
        sps.constraint_set_0_flag = bitReader.ReadBool();
        sps.constraint_set_1_flag = bitReader.ReadBool();
        sps.constraint_set_2_flag = bitReader.ReadBool();
        sps.constraint_set_3_flag = bitReader.ReadBool();
        bitReader.ReadNBit(4); //reserved_zero_4bits
        sps.level_idc = bitReader.ReadNBit(8);
        sps.seq_parameter_set_id = bitReader.ReadUE();
        sps.chroma_format_idc = ChromaFormat.Yuv420;
        if (sps.profile_idc == 100 || sps.profile_idc == 110 || sps.profile_idc == 122 || sps.profile_idc == 244 || sps.profile_idc == 44)
        {
          sps.chroma_format_idc = (ChromaFormat)bitReader.ReadUE();
          if (sps.chroma_format_idc == ChromaFormat.Yuv444)
          {
            sps.residual_color_transform_flag = bitReader.ReadBool();
          }
          sps.bit_depth_luma_minus8 = bitReader.ReadUE();
          sps.bit_depth_chroma_minus8 = bitReader.ReadUE();
          sps.qpprime_y_zero_transform_bypass_flag = bitReader.ReadBool();
          bool seqScalingMatrixPresent = bitReader.ReadBool();
          if (seqScalingMatrixPresent)
          {
            ReadScalingListMatrix(bitReader, ref sps.scalingMatrix);
          }
        }
        sps.log2_max_frame_num_minus4 = bitReader.ReadUE();
        sps.pic_order_cnt_type = bitReader.ReadUE();
        if (sps.pic_order_cnt_type == 0)
        {
          sps.log2_max_pic_order_cnt_lsb_minus4 = bitReader.ReadUE();
        }
        else if (sps.pic_order_cnt_type == 1)
        {
          sps.delta_pic_order_always_zero_flag = bitReader.ReadBool();
          sps.offset_for_non_ref_pic = bitReader.ReadSE();
          sps.offset_for_top_to_bottom_field = bitReader.ReadSE();
          sps.num_ref_frames_in_pic_order_cnt_cycle = bitReader.ReadUE();
          sps.offsetForRefFrame = new int[sps.num_ref_frames_in_pic_order_cnt_cycle];
          for (int i = 0; i < sps.num_ref_frames_in_pic_order_cnt_cycle; i++)
          {
            sps.offsetForRefFrame[i] = bitReader.ReadSE();
          }
        }
        sps.num_ref_frames = bitReader.ReadUE();
        sps.gaps_in_frame_num_value_allowed_flag = bitReader.ReadBool();
        sps.pic_width_in_mbs_minus1 = bitReader.ReadUE();
        sps.pic_height_in_map_units_minus1 = bitReader.ReadUE();
        sps.frame_mbs_only_flag = bitReader.ReadBool();
        if (!sps.frame_mbs_only_flag)
        {
          sps.mb_adaptive_frame_field_flag = bitReader.ReadBool();
        }
        sps.direct_8x8_inference_flag = bitReader.ReadBool();
        sps.frame_cropping_flag = bitReader.ReadBool();
        if (sps.frame_cropping_flag)
        {
          sps.frame_crop_left_offset = bitReader.ReadUE();
          sps.frame_crop_right_offset = bitReader.ReadUE();
          sps.frame_crop_top_offset = bitReader.ReadUE();
          sps.frame_crop_bottom_offset = bitReader.ReadUE();
        }
        bool vui_parameters_present_flag = bitReader.ReadBool();
        if (vui_parameters_present_flag)
          ReadVUIParameters(bitReader, ref sps.vuiParams);

        return sps;
      }

      private static void ReadScalingListMatrix(BitStreamReader bitReader, ref ScalingMatrix matrix)
      {
        matrix = new ScalingMatrix();
        for (int i = 0; i < 8; i++)
        {
          bool seqScalingListPresentFlag = bitReader.ReadBool();
          if (seqScalingListPresentFlag)
          {
            matrix.ScalingList4x4 = new ScalingList[8];
            matrix.ScalingList8x8 = new ScalingList[8];
            if (i < 6)
            {
              matrix.ScalingList4x4[i] = ScalingList.Read(bitReader, 16);
            }
            else
            {
              matrix.ScalingList8x8[i - 6] = ScalingList.Read(bitReader, 64);
            }
          }
        }
      }

      private static void ReadVUIParameters(BitStreamReader bitReader, ref VUIParameters vuip)
      {
        vuip = new VUIParameters();
        vuip.aspect_ratio_info_present_flag = bitReader.ReadBool();
        if (vuip.aspect_ratio_info_present_flag)
        {
          vuip.aspect_ratio = bitReader.ReadNBit(8);
          if (vuip.aspect_ratio == 255) //Extended SAR
          {
            vuip.sar_width = bitReader.ReadNBit(16);
            vuip.sar_height = bitReader.ReadNBit(16);
          }
        }
        vuip.overscan_info_present_flag = bitReader.ReadBool();
        if (vuip.overscan_info_present_flag)
        {
          vuip.overscan_appropriate_flag = bitReader.ReadBool();
        }
        vuip.video_signal_type_present_flag = bitReader.ReadBool();
        if (vuip.video_signal_type_present_flag)
        {
          vuip.video_format = bitReader.ReadNBit(3);
          vuip.video_full_range_flag = bitReader.ReadBool();
          vuip.colour_description_present_flag = bitReader.ReadBool();
          if (vuip.colour_description_present_flag)
          {
            vuip.colour_primaries = bitReader.ReadNBit(8);
            vuip.transfer_characteristics = bitReader.ReadNBit(8);
            vuip.matrix_coefficients = bitReader.ReadNBit(8);
          }
        }
        vuip.chroma_loc_info_present_flag = bitReader.ReadBool();
        if (vuip.chroma_loc_info_present_flag)
        {
          vuip.chroma_sample_loc_type_top_field = bitReader.ReadUE();
          vuip.chroma_sample_loc_type_bottom_field = bitReader.ReadUE();
        }
        vuip.timing_info_present_flag = bitReader.ReadBool();
        if (vuip.timing_info_present_flag)
        {
          vuip.num_units_in_tick = bitReader.ReadNBit(32);
          vuip.time_scale = bitReader.ReadNBit(32);
          vuip.fixed_frame_rate_flag = bitReader.ReadBool();
        }
        bool nal_hrd_parameters_present_flag = bitReader.ReadBool();
        if (nal_hrd_parameters_present_flag)
          ReadHRDParameters(bitReader, ref vuip.nalHRDParams);
        bool vcl_hrd_parameters_present_flag = bitReader.ReadBool();
        if (vcl_hrd_parameters_present_flag)
          ReadHRDParameters(bitReader, ref vuip.vclHRDParams);
        if (nal_hrd_parameters_present_flag || vcl_hrd_parameters_present_flag)
        {
          vuip.low_delay_hrd_flag = bitReader.ReadBool();
        }
        vuip.pic_struct_present_flag = bitReader.ReadBool();
        bool bitstream_restriction_flag = bitReader.ReadBool();
        if (bitstream_restriction_flag)
        {
          vuip.bitstreamRestriction = new VUIParameters.BitstreamRestriction();
          vuip.bitstreamRestriction.motion_vectors_over_pic_boundaries_flag = bitReader.ReadBool();
          vuip.bitstreamRestriction.max_bytes_per_pic_denom = bitReader.ReadUE();
          vuip.bitstreamRestriction.max_bits_per_mb_denom = bitReader.ReadUE();
          vuip.bitstreamRestriction.log2_max_mv_length_horizontal = bitReader.ReadUE();
          vuip.bitstreamRestriction.log2_max_mv_length_vertical = bitReader.ReadUE();
          vuip.bitstreamRestriction.num_reorder_frames = bitReader.ReadUE();
          vuip.bitstreamRestriction.max_dec_frame_buffering = bitReader.ReadUE();
        }
      }

      private static void ReadHRDParameters(BitStreamReader bitReader, ref HRDParameters hrd)
      {
        hrd = new HRDParameters();
        hrd.cpb_cnt_minus1 = bitReader.ReadUE();
        hrd.bit_rate_scale = (int)bitReader.ReadNBit(4);
        hrd.cpb_size_scale = (int)bitReader.ReadNBit(4);
        hrd.bit_rate_value_minus1 = new int[hrd.cpb_cnt_minus1 + 1];
        hrd.cpb_size_value_minus1 = new int[hrd.cpb_cnt_minus1 + 1];
        hrd.cbr_flag = new bool[hrd.cpb_cnt_minus1 + 1];

        for (int SchedSelIdx = 0; SchedSelIdx <= hrd.cpb_cnt_minus1; SchedSelIdx++)
        {
          hrd.bit_rate_value_minus1[SchedSelIdx] = bitReader.ReadUE();
          hrd.cpb_size_value_minus1[SchedSelIdx] = bitReader.ReadUE();
          hrd.cbr_flag[SchedSelIdx] = bitReader.ReadBool();
        }
        hrd.initial_cpb_removal_delay_length_minus1 = (int)bitReader.ReadNBit(5);
        hrd.cpb_removal_delay_length_minus1 = (int)bitReader.ReadNBit(5);
        hrd.dpb_output_delay_length_minus1 = (int)bitReader.ReadNBit(5);
        hrd.time_offset_length = (int)bitReader.ReadNBit(5);
      }
    }

    private class NALUnit
    {
      public NALUnitType Type { get; private set; }
      public byte[] Data { get; private set; }
      public int RefIDC { get; private set; }

      public static NALUnit Read(byte[] nalData)
      {
        int nalu = nalData[0] & 0xFF;
        int refIdc = (nalu >> 5) & 0x03;
        int type = nalu & 0x1F;
        if (Enum.IsDefined(typeof(NALUnitType), type) == false)
        {
          type = 0;
        }

        NALUnit nal = new NALUnit();
        nal.Type = (NALUnitType)type;
        nal.RefIDC = refIdc;
        nal.Data = new byte[nalData.Length - 1];
        Array.Copy(nalData, 1, nal.Data, 0, nal.Data.Length);
        return nal;
      }
    }

    #endregion

    [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern int memcmp(byte[] array1, byte[] array2, long count);

    private bool CompareBytes(byte[] array1, byte[] array2)
    {
      // Validate buffers are the same length.
      // This also ensures that the count does not exceed the length of either buffer.  
      return array1.Length == array2.Length && memcmp(array1, array2, array1.Length) == 0;
    }

    private void ReadBytes(byte[] readBuffer)
    {
      Array.Copy(buffer, currrentPos, readBuffer, 0, readBuffer.LongLength);
    }

    private byte[] GetArraySegment(long startPos, long length)
    {
      byte[] result = new byte[length];
      Array.Copy(buffer, startPos, result, 0, length);
      return result;
    }

    private byte[] ReadNALUnit()
    {
      long start = -1;
      byte[] sequenceBuffer = new byte[4];
      byte[] nalStart = new byte[] { 0x00, 0x00, 0x00, 0x01 };
      while ((buffer.LongLength - currrentPos) >= sequenceBuffer.Length)
      {
        ReadBytes(sequenceBuffer);
        if (CompareBytes(nalStart, sequenceBuffer) == true)
        {
          if (start == -1)
          {
            //First NAL found
            currrentPos += 4;
            start = currrentPos;
          }
          else
          {
            //Second NAL found and size of first NAL
            return GetArraySegment(start, currrentPos - start + 1);
          }
        }
        else if (sequenceBuffer[2] != 0)
        {
          currrentPos += 3;
        }
        else if (sequenceBuffer[1] != 0)
        {
          currrentPos += 2;
        }
        else
        {
          currrentPos += 1;
        }
      }
      if (start >= 0)
      {
        return GetArraySegment(start, buffer.LongLength - start);
      }
      return null;
    }

    public bool Parse(byte[] h264Stream)
    {
      profile = 0;
      level = 0;
      refFrames = 0;
      currrentPos = 0;
      buffer = h264Stream;

      byte[] nal = new byte[0];
      while (nal != null)
      {
        nal = ReadNALUnit();
        if (nal != null)
        {
          NALUnit nu = NALUnit.Read(nal);
          if (nu.Type == NALUnitType.SequenceParameterSet)
          {
            SeqParameterSet param = SeqParameterSet.Read(nu.Data);
            if (Enum.IsDefined(typeof(H264HeaderProfile), param.profile_idc) == true)
            {
              profile = param.profile_idc;
              level = param.level_idc;
              refFrames = param.num_ref_frames;
              return true;
            }
          }
        }
      }

      return false;
    }
  }
}
