#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
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

using MediaPortal.Presentation.DataObjects;

namespace Models.Pictures
{
  public class PictureInfo
  {
    #region Variables

    private Property m_CameraModel;
    private Property m_EquipmentMake;
    private Property m_ExposureCompensation;
    private Property m_ExposureTime;
    private Property m_ImgTitle;
    private Property m_MeteringMod;
    private Property m_Flash;
    private Property m_Fstop;
    private Property m_ImgDimensions;
    private Property m_ShutterSpeed;
    private Property m_Resolutions;
    private Property m_ViewComment;
    private Property m_Date;
    private Property m_AbsolutePath;
    private Property m_Tags;
    #endregion

    #region ctor
    
    /// <summary>
    /// copy constructor
    /// </summary>
    /// <param name="tag"></param>
    public PictureInfo()
    {
      m_CameraModel = new Property(typeof(string), "");
      m_EquipmentMake = new Property(typeof(string), "");
      m_ExposureCompensation = new Property(typeof(string), "");
      m_ExposureTime = new Property(typeof(string), "");
      m_Flash = new Property(typeof(string), "");
      m_ImgTitle = new Property(typeof(string), "");
      m_MeteringMod = new Property(typeof(string), "");
      m_Fstop = new Property(typeof(string), "");
      m_ImgDimensions = new Property(typeof(string), "");
      m_ShutterSpeed = new Property(typeof(string), "");
      m_Resolutions = new Property(typeof(string), "");
      m_ViewComment = new Property(typeof(string), "");
      m_Date = new Property(typeof(string), "");
      m_AbsolutePath = new Property(typeof(string), "");
      m_Tags = new Property(typeof(string), "");
      //ServiceScope.Get<ISettingsManager>().Load(settings);

    }


    #endregion

    #region Methods

    /// <summary>
    /// Method to clear the current item
    /// </summary>
    public void Clear()
    {
      m_CameraModel = new Property(typeof(string), "");
      m_EquipmentMake = new Property(typeof(string), "");
      m_ExposureCompensation = new Property(typeof(string), "");
      m_ExposureTime = new Property(typeof(string), "");
      m_Flash = new Property(typeof(string), "");
      m_ImgTitle = new Property(typeof(string), "");
      m_MeteringMod = new Property(typeof(string), "");
      m_Fstop = new Property(typeof(string), "");
      m_ImgDimensions = new Property(typeof(string), "");
      m_ShutterSpeed = new Property(typeof(string), "");
      m_Resolutions = new Property(typeof(string), "");
      m_ViewComment = new Property(typeof(string), "");
      m_Date = new Property(typeof(string), "");
      m_AbsolutePath = new Property(typeof(string), "");
      m_Tags = new Property(typeof(string), "");
    }

    #endregion

    #region Properties


    /*
    internal string m_CameraModel = "";
    internal string m_EquipmentMake = "";
    internal string m_ExposureCompensation = "";
    internal string m_ExposureTime = "";
    internal string m_ImgTitle = "";
    internal string m_MeteringMod = "";
    internal string m_Flash = "";
    internal string m_Fstop = "";
    internal string m_ImgDimensions = "";
    internal string m_ShutterSpeed = "";
    internal string m_Resolutions = "";
    internal string m_ViewComment = "";
    internal string m_Date = ""; */

    /// <summary>
    /// Property to get/set the CameraModel field of the picture file
    /// </summary>
    /// 
    public string CameraModel
    {
      get { return (string)m_CameraModel.GetValue(); }
      set { m_CameraModel.SetValue(value); }
    }

    /// <summary>
    /// Gets or sets the title property.
    /// </summary>
    /// <value>The title property.</value>
    public Property CameraModelProperty
    {
      get { return m_CameraModel; }
      set { m_CameraModel = value; }
    }

    /// <summary>
    /// Property to get/set the EquipmentMake field of the picture file
    /// </summary>
    /// 
    public string EquipamentMake
    {
      get { return (string)m_EquipmentMake.GetValue(); }
      set { m_EquipmentMake.SetValue(value); }
    }

    /// <summary>
    /// Gets or sets the EquipmentMake property.
    /// </summary>
    /// <value>The title property.</value>
    public Property EquipmentMakeProperty
    {
      get { return m_EquipmentMake; }
      set { m_EquipmentMake = value; }
    }

    /// <summary>
    /// Property to get/set the AbsolutePath field of the picture file
    /// </summary>
    /// 
    public string AbsolutePath
    {
      get { return (string)m_AbsolutePath.GetValue(); }
      set { m_AbsolutePath.SetValue(value); }
    }

    /// <summary>
    /// Gets or sets the AbsolutePath property.
    /// </summary>
    /// <value>The title property.</value>
    public Property AbsolutePathProperty
    {
      get { return m_AbsolutePath; }
      set { m_AbsolutePath = value; }
    }

    /// <summary>
    /// Property to get/set the ImgTitle field of the picture file
    /// </summary>
    /// 
    public string ImgTitle
    {
      get { return (string)m_ImgTitle.GetValue(); }
      set { m_ImgTitle.SetValue(value); }
    }

    /// <summary>
    /// Gets or sets the ImgTitleProperty property.
    /// </summary>
    /// <value>The title property.</value>
    public Property ImgTitleProperty
    {
      get { return m_ImgTitle; }
      set { m_ImgTitle = value; }
    }

    /// <summary>
    /// Property to get/set the Date field of the picture file
    /// </summary>
    /// 
    public string Date
    {
      get { return (string)m_Date.GetValue(); }
      set { m_Date.SetValue(value); }
    }

    /// <summary>
    /// Gets or sets the Date property.
    /// </summary>
    /// <value>The title property.</value>
    public Property DateProperty
    {
      get { return m_Date; }
      set { m_Date = value; }
    }

    /// <summary>
    /// Property to get/set the Date field of the picture file
    /// </summary>
    /// 
    public string ExposureCompensation
    {
      get { return (string)m_ExposureCompensation.GetValue(); }
      set { m_ExposureCompensation.SetValue(value); }
    }

    /// <summary>
    /// Gets or sets the Date property.
    /// </summary>
    /// <value>The title property.</value>
    public Property ExposureCompensationProperty
    {
      get { return m_ExposureCompensation; }
      set { m_ExposureCompensation = value; }
    }

    /// <summary>
    /// Property to get/set the m_ExposureTime field of the picture file
    /// </summary>
    /// 
    public string  ExposureTime
    {
      get { return (string)m_ExposureTime.GetValue(); }
      set { m_ExposureTime.SetValue(value); }
    }

    /// <summary>
    /// Gets or sets the m_ExposureTime property.
    /// </summary>
    /// <value>The m_ExposureTime property.</value>
    public Property ExposureTimeProperty
    {
      get { return m_ExposureTime; }
      set { m_ExposureTime = value; }
    }

    /// <summary>
    /// Property to get/set the m_Flash field of the picture file
    /// </summary>
    /// 
    public string Flash
    {
      get { return (string)m_Flash.GetValue(); }
      set { m_Flash.SetValue(value); }
    }

    /// <summary>
    /// Gets or sets the m_Flash property.
    /// </summary>
    /// <value>The m_Flash property.</value>
    public Property FlashProperty
    {
      get { return m_Flash; }
      set { m_Flash = value; }
    }

    /// <summary>
    /// Property to get/set the m_MeteringMod field of the picture file
    /// </summary>
    /// 
    public string MeteringMod
    {
      get { return (string)m_MeteringMod.GetValue(); }
      set { m_MeteringMod.SetValue(value); }
    }

    /// <summary>
    /// Gets or sets the m_MeteringMod property.
    /// </summary>
    /// <value>The m_MeteringMod property.</value>
    public Property MeteringModProperty
    {
      get { return m_MeteringMod; }
      set { m_MeteringMod = value; }
    }

    /// <summary>
    /// Property to get/set the m_Fstop field of the picture file
    /// </summary>
    /// 
    public string Fstop
    {
      get { return (string)m_Fstop.GetValue(); }
      set { m_Fstop.SetValue(value); }
    }

    /// <summary>
    /// Gets or sets the m_Fstop property.
    /// </summary>
    /// <value>The m_Fstop property.</value>
    public Property FstopProperty
    {
      get { return m_Fstop; }
      set { m_Fstop = value; }
    }

    /// <summary>
    /// Property to get/set the m_Fstop field of the picture file
    /// </summary>
    /// 
    public string ImgDimensions
    {
      get { return (string)m_ImgDimensions.GetValue(); }
      set { m_ImgDimensions.SetValue(value); }
    }

    /// <summary>
    /// Gets or sets the m_ImgDimensions property.
    /// </summary>
    /// <value>The m_ImgDimensions property.</value>
    public Property ImgDimensionsProperty
    {
      get { return m_ImgDimensions; }
      set { m_ImgDimensions = value; }
    }

    /// <summary>
    /// Property to get/set the m_ShutterSpeed field of the picture file
    /// </summary>
    /// 
    public string ShutterSpeed
    {
      get { return (string)m_ShutterSpeed.GetValue(); }
      set { m_ShutterSpeed.SetValue(value); }
    }

    /// <summary>
    /// Gets or sets the m_ShutterSpeed property.
    /// </summary>
    /// <value>The m_ShutterSpeed property.</value>
    public Property ShutterSpeedProperty
    {
      get { return m_ShutterSpeed; }
      set { m_ShutterSpeed = value; }
    }

        /// <summary>
    /// Property to get/set the m_Resolutions field of the picture file
    /// </summary>
    /// 
    public string Resolutions
    {
      get { return (string)m_Resolutions.GetValue(); }
      set { m_Resolutions.SetValue(value); }
    }

    /// <summary>
    /// Gets or sets the m_Resolutions property.
    /// </summary>
    /// <value>The m_Resolutions property.</value>
    public Property ResolutionsProperty
    {
      get { return m_Resolutions; }
      set { m_Resolutions = value; }
    }

            /// <summary>
    /// Property to get/set the m_ViewComment field of the picture file
    /// </summary>
    /// 
    public string ViewComment
    {
      get { return (string)m_ViewComment.GetValue(); }
      set { m_ViewComment.SetValue(value); }
    }

    /// <summary>
    /// Gets or sets the m_ViewComment property.
    /// </summary>
    /// <value>The m_ViewComment property.</value>
    public Property ViewCommentProperty
    {
      get { return m_ViewComment; }
      set { m_ViewComment = value; }
    }

    #endregion
    }
}

