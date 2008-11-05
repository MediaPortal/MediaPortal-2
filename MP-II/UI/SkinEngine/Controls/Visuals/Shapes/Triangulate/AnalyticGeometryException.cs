using System;

namespace GeometryUtility
{
	/// <summary>
	/// Summary description for NoValidReturnException.
	/// </summary>
	public class NonValidReturnException: ApplicationException
	{
		public NonValidReturnException():base()
		{
		
		}
		public NonValidReturnException(string msg)
			:base(msg)
		{
			string errMsg="\nThere is no valid return value available!";
			throw new NonValidReturnException(errMsg);
		}
		public NonValidReturnException(string msg,
			Exception inner): base(msg, inner)
		{
		
		}
	}

	public class InvalidInputGeometryDataException: ApplicationException
	{
		public InvalidInputGeometryDataException():base()
		{
		
		}
		public InvalidInputGeometryDataException(string msg)
			:base(msg)
		{

		}
		public InvalidInputGeometryDataException(string msg,
			Exception inner): base(msg, inner)
		{
		
		}
	}
}
