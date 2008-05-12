using System;

namespace GeometryUtility
{
	/// <summary>
	///To define the common types used in 
	///Analytical Geometry calculations.
	/// </summary>
	
	//To define some constant Values 
	//used for local judgment 
	public struct ConstantValue
	{
		internal const  double SmallValue=double.Epsilon;
		internal const double BigValue=double.MaxValue;
	}
	
	public enum VertexType
	{
		ErrorPoint,
		ConvexPoint,
		ConcavePoint		
	}

	public enum PolygonType
	{
		Unknown,
		Convex, 
		Concave	
	}

	public enum PolygonDirection
	{
		Unknown,
		Clockwise,
		Count_Clockwise
	}
}
