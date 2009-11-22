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

// All this Enums are besed on MSDN Article Property Item Descriptions
// MSDN Home >  MSDN Library >  Graphics and Multimedia >  GDI+ >  GDI+ Reference >  Constants >  Image Property Tag Constants 

namespace Media.Importers.PictureImporter
{
	///<summary>
	///Unit of measure used for the horizontal resolution and the vertical resolution.
	///</summary>
	public enum ResolutionUnit: ushort
	{
		///<summary>Dots Per Inch</summary>
		dpi  			= 2,
		///<summary>Centimeters Per Inch</summary>
		dpcm 			=3
	}
	
	///<summary>
	///Image orientation viewed in terms of rows and columns.
	///</summary>
	public enum Orientation: ushort
	{
		///<summary>The 0th row is at the top of the visual image, and the 0th column is the visual left side.</summary>
		TopLeft 				= 1,
		///<summary>The 0th row is at the visual top of the image, and the 0th column is the visual right side.</summary>
		TopRight 				= 2,
		///<summary>The 0th row is at the visual bottom of the image, and the 0th column is the visual right side.</summary>
		BottomLeft 				= 3,
		///<summary>The 0th row is at the visual bottom of the image, and the 0th column is the visual right side.</summary>
		BottomRight 			= 4,
		///<summary>The 0th row is the visual left side of the image, and the 0th column is the visual top.</summary>
		LeftTop 				= 5,
		///<summary>The 0th row is the visual right side of the image, and the 0th column is the visual top.</summary>
		RightTop 				= 6,
		///<summary>The 0th row is the visual right side of the image, and the 0th column is the visual bottom.</summary>
		RightBottom 			= 7,
		///<summary>The 0th row is the visual left side of the image, and the 0th column is the visual bottom.</summary>
		LeftBottom 				= 8
	}

	///<summary>
	/// Class of the program used by the camera to set exposure when the picture is taken.
	///</summary>
	public enum ExposureProg: ushort
	{
		///<summary>not defined</summary>
		Undefined 				= 0, 
		///<summary>manual</summary>
		Manual 					= 1,
		///<summary>normal program</summary>
		Normal 					= 2,
		///<summary>aperture priority</summary>
		Aperture 				= 3,
		///<summary>shutter priority</summary>
		Shutter 				= 4,
		///<summary>creative program (biased toward depth of field)</summary>
		Creative 				= 5,
		///<summary>action program (biased toward fast shutter speed)</summary>
		Action 					= 6,
		///<summary>portrait mode (for close-up photos with the background out of focus)</summary>
		Portrait 				= 7,
		///<summary>landscape mode (for landscape photos with the background in focus)</summary>
		Landscape 				= 8,
		///<summary>9 to 255 - reserved</summary>
		Reserved 				= 9
	}
	
	
	///<summary>
	/// Metering mode
	///</summary>
	public enum MeteringMode: ushort
	{
		///<summary>Unknown</summary>
		Unknown 							= 0,
		///<summary>Average</summary>
		Average 							= 1,
		///<summary>Center weighted average</summary>
		CenterWeightedAverage 				= 2,
		///<summary>Spot</summary>
		Spot 								= 3,
		///<summary>Multi Spot</summary>
		MultiSpot 							= 4,
		///<summary>Pattern</summary>
		Pattern 							= 5,
		///<summary>Partial</summary>
		Partial 							= 6,
		///<summary>Other</summary>
		Other 								= 255
	}

	///<summary>
	/// Specifies the data type of the values stored in the value data member of that same PropertyItem object.
	///</summary>
	public enum PropertyTagType: short
	{
		///<summary>Specifies that the format is 4 bits per pixel, indexed.</summary>
		PixelFormat4bppIndexed 					= 0,
		///<summary>Specifies that the value data member is an array of bytes.</summary>
		Byte 					= 1,
		///<summary>Specifies that the value data member is a null-terminated ASCII string. If you set the type data member of a PropertyItem object to PropertyTagTypeASCII, you should set the length data member to the length of the string including the NULL terminator. For example, the string HELLO would have a length of 6.</summary>
		ASCII 					= 2,
		///<summary>Specifies that the value data member is an array of unsigned short (16-bit) integers.</summary>
		Short 					= 3,
		///<summary>Specifies that the value data member is an array of unsigned long (32-bit) integers.</summary>
		Long 					= 4,
		///<summary>Specifies that the value data member is an array of pairs of unsigned long integers. Each pair represents a fraction; the first integer is the numerator and the second integer is the denominator.</summary>
		Rational 				= 5,
		///<summary>Specifies that the value data member is an array of bytes that can hold values of any data type.</summary>
		Undefined 				= 7,
		///<summary>Specifies that the value data member is an array of signed long (32-bit) integers.</summary>
		SLONG 					= 9,
		///<summary>Specifies that the value data member is an array of pairs of signed long integers. Each pair represents a fraction; the first integer is the numerator and the second integer is the denominator.</summary>
		SRational 				= 10
	}
	
	///<summary>
	/// The following Enumeration gives list (and descriptions) of the property items supported in EXIF format.
	///</summary>	
	public enum PropertyTagId: int
	{
		///<summary>Null-terminated character string that specifies the name of the person who created the image.</summary>
		Artist					=	0x013B	,
		///<summary>Number of bits per color component. See also SamplesPerPixel.</summary>
		BitsPerSample			=	0x0102	,
		///<summary></summary>
		CellHeight				=	0x0109	,
		///<summary></summary>
		CellWidth				=	0x0108	,
		///<summary></summary>
		ChrominanceTable		=	0x5091	,
		///<summary></summary>
		ColorMap				=	0x0140	,
		///<summary></summary>
		ColorTransferFunction	=	0x501A	,
		///<summary></summary>
		Compression				=	0x0103	,
		///<summary></summary>
		Copyright				=	0x8298	,
		///<summary></summary>
		DateTime				=	0x0132	,
		///<summary></summary>
		DocumentName			=	0x010D	,
		///<summary></summary>
		DotRange				=	0x0150	,
		///<summary></summary>
		EquipMake				=	0x010F	,
		///<summary></summary>
		EquipModel				=	0x0110	,
		///<summary></summary>
		ExifAperture			=	0x9202	,
		///<summary></summary>
		ExifBrightness			=	0x9203	,
		///<summary></summary>
		ExifCfaPattern			=	0xA302	,
		///<summary></summary>
		ExifColorSpace			=	0xA001	,
		///<summary></summary>
		ExifCompBPP				=	0x9102	,
		///<summary></summary>
		ExifCompConfig			=	0x9101	,
		///<summary></summary>
		ExifDTDigitized			=	0x9004	,
		///<summary></summary>
		ExifDTDigSS				=	0x9292	,
		///<summary></summary>
		ExifDTOrig				=	0x9003	,
		///<summary></summary>
		ExifDTOrigSS			=	0x9291	,
		///<summary></summary>
		ExifDTSubsec			=	0x9290	,
		///<summary></summary>
		ExifExposureBias		=	0x9204	,
		///<summary></summary>
		ExifExposureIndex		=	0xA215	,
		///<summary></summary>
		ExifExposureProg		=	0x8822	,
		///<summary></summary>
		ExifExposureTime		=	0x829A	,
		///<summary></summary>
		ExifFileSource			=	0xA300	,
		///<summary></summary>
		ExifFlash				=	0x9209	,
		///<summary></summary>
		ExifFlashEnergy			=	0xA20B	,
		///<summary></summary>
		ExifFNumber				=	0x829D	,
		///<summary></summary>
		ExifFocalLength			=	0x920A	,
		///<summary></summary>
		ExifFocalResUnit		=	0xA210	,
		///<summary></summary>
		ExifFocalXRes			=	0xA20E	,
		///<summary></summary>
		ExifFocalYRes			=	0xA20F	,
		///<summary></summary>
		ExifFPXVer				=	0xA000	,
		///<summary></summary>
		ExifIFD					=	0x8769	,
		///<summary></summary>
		ExifInterop				=	0xA005	,
		///<summary></summary>
		ExifISOSpeed			=	0x8827	,
		///<summary></summary>
		ExifLightSource			=	0x9208	,
		///<summary></summary>
		ExifMakerNote			=	0x927C	,
		///<summary></summary>
		ExifMaxAperture			=	0x9205	,
		///<summary></summary>
		ExifMeteringMode		=	0x9207	,
		///<summary></summary>
		ExifOECF				=	0x8828	,
		///<summary></summary>
		ExifPixXDim				=	0xA002	,
		///<summary></summary>
		ExifPixYDim				=	0xA003	,
		///<summary></summary>
		ExifRelatedWav			=	0xA004	,
		///<summary></summary>
		ExifSceneType			=	0xA301	,
		///<summary></summary>
		ExifSensingMethod		=	0xA217	,
		///<summary></summary>
		ExifShutterSpeed		=	0x9201	,
		///<summary></summary>
		ExifSpatialFR			=	0xA20C	,
		///<summary></summary>
		ExifSpectralSense		=	0x8824	,
		///<summary></summary>
		ExifSubjectDist			=	0x9206	,
		///<summary></summary>
		ExifSubjectLoc			=	0xA214	,
		///<summary></summary>
		ExifUserComment			=	0x9286	,
		///<summary></summary>
		ExifVer					=	0x9000	,
		///<summary></summary>
		ExtraSamples			=	0x0152	,
		///<summary></summary>
		FillOrder				=	0x010A	,
		///<summary></summary>
		FrameDelay				=	0x5100	,
		///<summary></summary>
		FreeByteCounts			=	0x0121	,
		///<summary></summary>
		FreeOffset				=	0x0120	,
		///<summary></summary>
		Gamma					=	0x0301	,
		///<summary></summary>
		GlobalPalette			=	0x5102	,
		///<summary></summary>
		GpsAltitude				=	0x0006	,
		///<summary></summary>
		GpsAltitudeRef			=	0x0005	,
		///<summary></summary>
		GpsDestBear				=	0x0018	,
		///<summary></summary>
		GpsDestBearRef			=	0x0017	,
		///<summary></summary>
		GpsDestDist				=	0x001A	,
		///<summary></summary>
		GpsDestDistRef			=	0x0019	,
		///<summary></summary>
		GpsDestLat				=	0x0014	,
		///<summary></summary>
		GpsDestLatRef			=	0x0013	,
		///<summary></summary>
		GpsDestLong				=	0x0016	,
		///<summary></summary>
		GpsDestLongRef			=	0x0015	,
		///<summary></summary>
		GpsGpsDop				=	0x000B	,
		///<summary></summary>
		GpsGpsMeasureMode		=	0x000A	,
		///<summary></summary>
		GpsGpsSatellites		=	0x0008	,
		///<summary></summary>
		GpsGpsStatus			=	0x0009	,
		///<summary></summary>
		GpsGpsTime				=	0x0007	,
		///<summary></summary>
		GpsIFD					=	0x8825	,
		///<summary></summary>
		GpsImgDir				=	0x0011	,
		///<summary></summary>
		GpsImgDirRef			=	0x0010	,
		///<summary></summary>
		GpsLatitude				=	0x0002	,
		///<summary></summary>
		GpsLatitudeRef			=	0x0001	,
		///<summary></summary>
		GpsLongitude			=	0x0004	,
		///<summary></summary>
		GpsLongitudeRef			=	0x0003	,
		///<summary></summary>
		GpsMapDatum				=	0x0012	,
		///<summary></summary>
		GpsSpeed				=	0x000D	,
		///<summary></summary>
		GpsSpeedRef				=	0x000C	,
		///<summary></summary>
		GpsTrack				=	0x000F	,
		///<summary></summary>
		GpsTrackRef				=	0x000E	,
		///<summary></summary>
		GpsVer					=	0x0000	,
		///<summary></summary>
		GrayResponseCurve		=	0x0123	,
		///<summary></summary>
		GrayResponseUnit		=	0x0122	,
		///<summary></summary>
		GridSize				=	0x5011	,
		///<summary></summary>
		HalftoneDegree			=	0x500C	,
		///<summary></summary>
		HalftoneHints			=	0x0141	,
		///<summary></summary>
		HalftoneLPI				=	0x500A	,
		///<summary></summary>
		HalftoneLPIUnit			=	0x500B	,
		///<summary></summary>
		HalftoneMisc			=	0x500E	,
		///<summary></summary>
		HalftoneScreen			=	0x500F	,
		///<summary></summary>
		HalftoneShape			=	0x500D	,
		///<summary></summary>
		HostComputer			=	0x013C	,
		///<summary></summary>
		ICCProfile				=	0x8773	,
		///<summary></summary>
		ICCProfileDescriptor	=	0x0302	,
		///<summary></summary>
		ImageDescription		=	0x010E	,
		///<summary></summary>
		ImageHeight				=	0x0101	,
		///<summary></summary>
		ImageTitle				=	0x0320	,
		///<summary></summary>
		ImageWidth				=	0x0100	,
		///<summary></summary>
		IndexBackground			=	0x5103	,
		///<summary></summary>
		IndexTransparent		=	0x5104	,
		///<summary></summary>
		InkNames				=	0x014D	,
		///<summary></summary>
		InkSet					=	0x014C	,
		///<summary></summary>
		JPEGACTables			=	0x0209	,
		///<summary></summary>
		JPEGDCTables			=	0x0208	,
		///<summary></summary>
		JPEGInterFormat			=	0x0201	,
		///<summary></summary>
		JPEGInterLength			=	0x0202	,
		///<summary></summary>
		JPEGLosslessPredictors	=	0x0205	,
		///<summary></summary>
		JPEGPointTransforms		=	0x0206	,
		///<summary></summary>
		JPEGProc				=	0x0200	,
		///<summary></summary>
		JPEGQTables				=	0x0207	,
		///<summary></summary>
		JPEGQuality				=	0x5010	,
		///<summary></summary>
		JPEGRestartInterval		=	0x0203	,
		///<summary></summary>
		LoopCount				=	0x5101	,
		///<summary></summary>
		LuminanceTable			=	0x5090	,
		///<summary></summary>
		MaxSampleValue			=	0x0119	,
		///<summary></summary>
		MinSampleValue			=	0x0118	,
		///<summary></summary>
		NewSubfileType			=	0x00FE	,
		///<summary></summary>
		NumberOfInks			=	0x014E	,
		///<summary></summary>
		Orientation				=	0x0112	,
		///<summary></summary>
		PageName				=	0x011D	,
		///<summary></summary>
		PageNumber				=	0x0129	,
		///<summary></summary>
		PaletteHistogram		=	0x5113	,
		///<summary></summary>
		PhotometricInterp		=	0x0106	,
		///<summary></summary>
		PixelPerUnitX			=	0x5111	,
		///<summary></summary>
		PixelPerUnitY			=	0x5112	,
		///<summary></summary>
		PixelUnit				=	0x5110	,
		///<summary></summary>
		PlanarConfig			=	0x011C	,
		///<summary></summary>
		Predictor				=	0x013D	,
		///<summary></summary>
		PrimaryChromaticities	=	0x013F	,
		///<summary></summary>
		PrintFlags				=	0x5005	,
		///<summary></summary>
		PrintFlagsBleedWidth	=	0x5008	,
		///<summary></summary>
		PrintFlagsBleedWidthScale =	0x5009	,
		///<summary></summary>
		PrintFlagsCrop			=	0x5007	,
		///<summary></summary>
		PrintFlagsVersion		=	0x5006	,
		///<summary></summary>
		REFBlackWhite			=	0x0214	,
		///<summary></summary>
		ResolutionUnit			=	0x0128	,
		///<summary></summary>
		ResolutionXLengthUnit	=	0x5003	,
		///<summary></summary>
		ResolutionXUnit			=	0x5001	,
		///<summary></summary>
		ResolutionYLengthUnit	=	0x5004	,
		///<summary></summary>
		ResolutionYUnit			=	0x5002	,
		///<summary></summary>
		RowsPerStrip			=	0x0116	,
		///<summary></summary>
		SampleFormat			=	0x0153	,
		///<summary></summary>
		SamplesPerPixel			=	0x0115	,
		///<summary></summary>
		SMaxSampleValue			=	0x0155	,
		///<summary></summary>
		SMinSampleValue			=	0x0154	,
		///<summary></summary>
		SoftwareUsed			=	0x0131	,
		///<summary></summary>
		SRGBRenderingIntent		=	0x0303	,
		///<summary></summary>
		StripBytesCount			=	0x0117	,
		///<summary></summary>
		StripOffsets			=	0x0111	,
		///<summary></summary>
		SubfileType				=	0x00FF	,
		///<summary></summary>
		T4Option				=	0x0124	,
		///<summary></summary>
		T6Option				=	0x0125	,
		///<summary></summary>
		TargetPrinter			=	0x0151	,
		///<summary></summary>
		ThreshHolding			=	0x0107	,
		///<summary></summary>
		ThumbnailArtist			=	0x5034	,
		///<summary></summary>
		ThumbnailBitsPerSample	=	0x5022	,
		///<summary></summary>
		ThumbnailColorDepth		=	0x5015	,
		///<summary></summary>
		ThumbnailCompressedSize	=	0x5019	,
		///<summary></summary>
		ThumbnailCompression	=	0x5023	,
		///<summary></summary>
		ThumbnailCopyRight		=	0x503B	,
		///<summary></summary>
		ThumbnailData			=	0x501B	,
		///<summary></summary>
		ThumbnailDateTime		=	0x5033	,
		///<summary></summary>
		ThumbnailEquipMake		=	0x5026	,
		///<summary></summary>
		ThumbnailEquipModel		=	0x5027	,
		///<summary></summary>
		ThumbnailFormat			=	0x5012	,
		///<summary></summary>
		ThumbnailHeight			=	0x5014	,
		///<summary></summary>
		ThumbnailImageDescription=	0x5025	,
		///<summary></summary>
		ThumbnailImageHeight	=	0x5021	,
		///<summary></summary>
		ThumbnailImageWidth		=	0x5020	,
		///<summary></summary>
		ThumbnailOrientation	=	0x5029	,
		///<summary></summary>
		ThumbnailPhotometricInterp=	0x5024	,
		///<summary></summary>
		ThumbnailPlanarConfig	=	0x502F	,
		///<summary></summary>
		ThumbnailPlanes			=	0x5016	,
		///<summary></summary>
		ThumbnailPrimaryChromaticities=	0x5036	,
		///<summary></summary>
		ThumbnailRawBytes		=	0x5017	,
		///<summary></summary>
		ThumbnailRefBlackWhite	=	0x503A	,
		///<summary></summary>
		ThumbnailResolutionUnit	=	0x5030	,
		///<summary></summary>
		ThumbnailResolutionX	=	0x502D	,
		///<summary></summary>
		ThumbnailResolutionY	=	0x502E	,
		///<summary></summary>
		ThumbnailRowsPerStrip	=	0x502B	,
		///<summary></summary>
		ThumbnailSamplesPerPixel=	0x502A	,
		///<summary></summary>
		ThumbnailSize			=	0x5018	,
		///<summary></summary>
		ThumbnailSoftwareUsed	=	0x5032	,
		///<summary></summary>
		ThumbnailStripBytesCount=	0x502C	,
		///<summary></summary>
		ThumbnailStripOffsets	=	0x5028	,
		///<summary></summary>
		ThumbnailTransferFunction=	0x5031	,
		///<summary></summary>
		ThumbnailWhitePoint		=	0x5035	,
		///<summary></summary>
		ThumbnailWidth			=	0x5013	,
		///<summary></summary>
		ThumbnailYCbCrCoefficients=	0x5037	,
		///<summary></summary>
		ThumbnailYCbCrPositioning=	0x5039	,
		///<summary></summary>
		ThumbnailYCbCrSubsampling=	0x5038	,
		///<summary></summary>
		TileByteCounts			=	0x0145	,
		///<summary></summary>
		TileLength				=	0x0143	,
		///<summary></summary>
		TileOffset				=	0x0144	,
		///<summary></summary>
		TileWidth				=	0x0142	,
		///<summary></summary>
		TransferFunction		=	0x012D	,
		///<summary></summary>
		TransferRange			=	0x0156	,
		///<summary></summary>
		WhitePoint				=	0x013E	,
		///<summary></summary>
		XPosition				=	0x011E	,
		///<summary></summary>
		XResolution				=	0x011A	,
		///<summary></summary>
		YCbCrCoefficients		=	0x0211	,
		///<summary></summary>
		YCbCrPositioning		=	0x0213	,
		///<summary></summary>
		YCbCrSubsampling		=	0x0212	,
		///<summary></summary>
		YPosition				=	0x011F	,
		///<summary></summary>
		YResolution				=	0x011B
	}


}
