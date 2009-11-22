#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.Text;
using System.Globalization;
using MediaPortal.Core;
using MediaPortal.Core.Localisation;
using MediaPortal.Core.ExifReader;

namespace MediaPortal.UI.Services.ExifReader
{
  public class ExifReader : IExifReader
  {

    DateTimeFormatInfo m_dateTimeFormat = new DateTimeFormatInfo();

    public ExifReader()
    {
      m_dateTimeFormat.ShortDatePattern = "yyyy:MM:dd HH:mm:ss";
    }

    public void Dispose()
    {
    }

    public struct MetadataItem
    {
      public string Hex;
      public string RawValueAsString;
      public string DisplayValue;
      public string Caption;
    }

    public struct Metadata
    {
      public MetadataItem ViewerComments;
      public MetadataItem EquipmentMake;
      public MetadataItem CameraModel;
      public MetadataItem ExposureTime;
      public MetadataItem Fstop;
      public MetadataItem DatePictureTaken;
      public MetadataItem ShutterSpeed;
      public MetadataItem ExposureCompensation;
      public MetadataItem MeteringMode;
      public MetadataItem Flash;
      public MetadataItem Resolution;
      public MetadataItem ImageDimensions;
      public MetadataItem Orientation;

    }

    public ExifTag GetExifData(string filename)
    {
      Metadata fileMetadata = GetExifMetadata(filename);
      ExifTag newTags = new ExifTag();
      newTags.CameraModel = fileMetadata.CameraModel.DisplayValue;
      newTags.EquipmentMake = fileMetadata.EquipmentMake.DisplayValue;
      newTags.Date = fileMetadata.DatePictureTaken.DisplayValue;
      newTags.ExposureCompensation = fileMetadata.ExposureCompensation.DisplayValue;
      newTags.ExposureTime = fileMetadata.ExposureTime.DisplayValue;
      newTags.Flash = fileMetadata.Flash.DisplayValue;
      newTags.Fstop = fileMetadata.Fstop.DisplayValue;
      newTags.ImgDimensions = fileMetadata.ImageDimensions.DisplayValue;
      newTags.MeteringMod = fileMetadata.MeteringMode.DisplayValue;
      newTags.Resolutions = fileMetadata.Resolution.DisplayValue;
      newTags.ShutterSpeed = fileMetadata.ShutterSpeed.DisplayValue;
      newTags.ViewComment = fileMetadata.ViewerComments.DisplayValue;
      return newTags;
    }

    public int Count()
    {
      return 13;// TODO fix hard code later
    }

    public string LookupExifValue(string Description, string Value)
    {
      string DescriptionValue = null;

      if (Description == "MeteringMode")
      {
        switch (Value)
        {
          case "0": DescriptionValue = ServiceScope.Get<ILocalisation>().ToString("system", "1"); ; break;
          case "1":
            DescriptionValue = "Average";
            break;
          case "2":
            DescriptionValue = "Center Weighted Average";
            break;
          case "3":
            DescriptionValue = "Spot";
            break;
          case "4":
            DescriptionValue = "Multi-spot";
            break;
          case "5":
            DescriptionValue = "Multi-segment";
            break;
          case "6":
            DescriptionValue = "Partial";
            break;
          case "255":
            DescriptionValue = "Other";
            break;
        }
      }

      if (Description == "ResolutionUnit")
      {
        switch (Value)
        {
          case "1":
            DescriptionValue = "No Units";
            break;
          case "2":
            DescriptionValue = "Inch";
            break;
          case "3":
            DescriptionValue = "Centimeter";
            break;
        }
      }

      if (Description == "Flash")
      {
        switch (Value)
        {
          case "0":
            DescriptionValue = "Flash did not fire";
            break;
          case "1":
            DescriptionValue = "Flash fired";
            break;
          case "5":
            DescriptionValue = "Flash fired but strobe return light not detected";
            break;
          case "7":
            DescriptionValue = "Flash fired and strobe return light detected";
            break;
        }
      }

      if (Description == "Orientation")
      {
        switch (Value)
        {
          case "0":
            DescriptionValue = "Undefined";
            break;
          case "1":
            DescriptionValue = "Normal";
            break;
          case "2":
            DescriptionValue = "Flip Horizontal";
            break;
          case "3":
            DescriptionValue = "Rotate 180";
            break;
          case "4":
            DescriptionValue = "Flip Vertical";
            break;
          case "5":
            DescriptionValue = "Transpose";
            break;
          case "6":
            DescriptionValue = "Rotate 90";
            break;
          case "7":
            DescriptionValue = "Transverse";
            break;
          case "8":
            DescriptionValue = "Rotate 270";
            break;
        }
      }

      return DescriptionValue;
    }

    private void setStuff(ref MetadataItem item, PropertyItem propItem, string tag, string caption)
    {
      try
      {
        item.Caption = caption;
        item.Hex = tag;
        string proptext = propItem.Id.ToString("x");
        if (proptext == tag)
        {
          System.Text.ASCIIEncoding Value = new System.Text.ASCIIEncoding();
          item.DisplayValue = Value.GetString(propItem.Value);
        }
      }
      catch (Exception)
      { }
    }


    public Metadata GetExifMetadata(string photoName)
    {
      // Create an instance of Metadata 
      Metadata MyMetadata = new Metadata();
      try
      {
        // Create an instance of the image to gather metadata from 
        using (Image MyImage = Image.FromFile(photoName))
        {
          // Create an integer array to hold the property id list,
          // and populate it with the list from my image.
          // Note: this only generates a list of integers, one for for each PropertyID.
          // We will populate the PropertyItem values later. 
          int[] MyPropertyIdList = MyImage.PropertyIdList;

          // Create an array of PropertyItems, but don't populate it yet.
          /* 
          * Note: there is a bug in .net framework v1.0 SP2 and also in 1.1 beta:
          * If any particular PropertyItem has a length of 0, you will get an unhandled error
          * when you populate the array directly from the image.
          * So, rather than create an array of PropertyItems and then populate it directly
          * from the image, we will create an empty one of the appropriate length, and then
          * test each of the PropertyItems ourselves, one at a time, and not add any that
          * would cause an error. 
          */

          //MyMetadata.ExposureTime.Hex			= "829a";
          //MyMetadata.Fstop.Hex				= "829d";
          //MyMetadata.ShutterSpeed.Hex			= "9201";
          //MyMetadata.ExposureCompensation.Hex = "9204";
          //MyMetadata.MeteringMode.Hex			= "9207";
          //MyMetadata.Flash.Hex				= "9209";
          //MyMetadata.Orientation.Hex				= "112";

          // Declare an ASCIIEncoding to use for returning string values from bytes
          System.Text.ASCIIEncoding Value = new System.Text.ASCIIEncoding();

          // Populate MyPropertyItemList.
          int index = 0;
          foreach (int MyPropertyId in MyPropertyIdList)
          {
            // ... try to call GetPropertyItem (it crashes if PropertyItem has length 0,so use Try/Catch)
            try
            {
              PropertyItem propItem = MyImage.GetPropertyItem(MyPropertyId);
              string proptext = propItem.Id.ToString("x");
              Trace.WriteLine("proptext:" + proptext);

              setStuff(ref MyMetadata.ViewerComments, propItem, "10e", "Viewer Comments");
              setStuff(ref MyMetadata.EquipmentMake, propItem, "10f", "Equipment Make");
              setStuff(ref MyMetadata.CameraModel, propItem, "110", "Camera Model");
              if (proptext == "9003")
              {
                setStuff(ref MyMetadata.DatePictureTaken, propItem, "9003", "Date Picture Taken");
                string dtstr = MyMetadata.DatePictureTaken.DisplayValue;
                if (dtstr != null)
                {
                  try
                  {
                    dtstr = dtstr.Substring(0, m_dateTimeFormat.ShortDatePattern.Length);
                    DateTime dat = DateTime.ParseExact(dtstr, "d", m_dateTimeFormat);
                    MyMetadata.DatePictureTaken.DisplayValue = Convert.ToString(dat, System.Threading.Thread.CurrentThread.CurrentCulture);
                  }
                  catch (Exception) { }
                }
              }


              MyMetadata.MeteringMode.Caption = "Metering Mode";
              if (proptext == "9207")
              {
                MyMetadata.MeteringMode.DisplayValue = LookupExifValue("MeteringMode", BitConverter.ToInt16(propItem.Value, 0).ToString());
              }


              MyMetadata.Flash.Caption = "Flash";
              if (proptext == "9209")
              {
                MyMetadata.Flash.DisplayValue = LookupExifValue("Flash", BitConverter.ToInt16(propItem.Value, 0).ToString());
              }


              MyMetadata.ExposureTime.Caption = "Exposure Time";
              if (proptext == "829a")
              {
                string StringValue = "";
                for (int Offset = 0; Offset < propItem.Len; Offset = Offset + 4)
                {
                  StringValue += BitConverter.ToInt32(propItem.Value, Offset).ToString() + "/";
                }
                MyMetadata.ExposureTime.DisplayValue = StringValue.Substring(0, StringValue.Length - 1);
              }


              MyMetadata.Fstop.Caption = "Fstop";
              if (proptext == "829d")
              {
                int int1;
                int int2;
                int1 = BitConverter.ToInt32(propItem.Value, 0);
                int2 = BitConverter.ToInt32(propItem.Value, 4);
                MyMetadata.Fstop.DisplayValue = "F/" + ((float)int1 / (float)int2);
              }


              MyMetadata.ShutterSpeed.Caption = "Shutter Speed:";
              if (proptext == "9201")
              {
                string StringValue = BitConverter.ToString(propItem.Value).Substring(0, 2);
                MyMetadata.ShutterSpeed.DisplayValue = "1/" + StringValue;
              }

              MyMetadata.ExposureCompensation.Caption = "ExposureCompensation";
              if (proptext == "9204")
              {
                string StringValue = BitConverter.ToString(propItem.Value).Substring(0, 1);
                MyMetadata.ExposureCompensation.DisplayValue = StringValue;// + " (Needs work to confirm accuracy)";
              }

              MyMetadata.Orientation.Caption = "Orientation";
              if (proptext == "112")
              {
                MyMetadata.Orientation.DisplayValue = LookupExifValue("Orientation", BitConverter.ToInt16(propItem.Value, 0).ToString());
                MyMetadata.Orientation.Hex = BitConverter.ToInt16(propItem.Value, 0).ToString();
              }
            }

            catch (Exception exc)
            {
              // if it is the	expected error, do nothing
              if (exc.GetType().ToString() != "System.ArgumentNullException")
              {
              }
            }
            finally
            {
              index++;
            }
          }

          MyMetadata.Resolution.DisplayValue =
            MyImage.HorizontalResolution.ToString()
            + "x" + MyImage.VerticalResolution.ToString();
          MyMetadata.Resolution.Caption = "Resolution ";

          MyMetadata.ImageDimensions.DisplayValue =
            MyImage.Width.ToString()
            + "x" + MyImage.Height.ToString();
          MyMetadata.ImageDimensions.Caption = "Dimensions";
        }
      }
      catch (Exception )
      {
      }
      return MyMetadata;
    }
  }
}
