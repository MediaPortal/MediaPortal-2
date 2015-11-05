using System;
using System.Text;
using System.IO;
using System.IO.Compression;
using System.Collections;

namespace Deusty.Net
{
	/// <summary>
	/// Provides an immutable (unchangeable) container for raw data.
	/// </summary>
	public class Data
	{
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////
		#region Static Method
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////

		/// <summary>
		/// Converts the data to a hexadecimal string format for easy displaying or saving.
		/// </summary>
		public static string ToHexString(byte[] bytes)
		{
			if (bytes == null) return null;

			// I can't seem to find a simple way (one-liner) to convert from a byte array to a hex string.
			// Looping seems to be the only option, and this technique is faster than using StringBuilder.

			char[] hexDigits = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f' };

			char[] chars = new char[bytes.Length * 2];

			for (int i = 0; i < bytes.Length; i++)
			{
				int b = bytes[i];
				chars[i * 2] = hexDigits[b >> 4];
				chars[i * 2 + 1] = hexDigits[b & 0xF];
			}
			return new String(chars);
		}

		public static bool IsEqual(Data d1, Data d2)
		{
			return IsEqual(d1.ByteArray, d2.ByteArray);
		}

		public static bool IsEqual(Data d1, int d1Offset, Data d2, int d2Offset, int length)
		{
			return IsEqual(d1.ByteArray, d1Offset, d2.ByteArray, d2Offset, length);
		}

		/// <summary>
		/// Compares two byte arrays for equality.
		/// </summary>
		/// <returns>
		/// True if both byte arrays are non-null, the same length, and are bit-wise equals.
		/// False otherwise.
		/// </returns>
		public static bool IsEqual(byte[] b1, byte[] b2)
		{
			if (b1 == null) return false;
			if (b2 == null) return false;

			if (b1.Length != b2.Length) return false;

			bool match = true;

			for (int i = 0; i < b1.Length && match; i++)
			{
				match = (b1[i] == b2[i]);
			}

			return match;
		}

		/// <summary>
		/// Compares byte arrays for equality, starting at the designated offsets, for the given length.
		/// </summary>
		/// <returns>
		/// True if both arrays are non-null, are greater than or equal to offset + length,
		/// and are bit-wise equal for that length.
		/// False otherwise.
		/// </returns>
		public static bool IsEqual(byte[] b1, int b1Offset, byte[] b2, int b2Offset, int length)
		{
			if (b1 == null) return false;
			if (b2 == null) return false;

			if (b1Offset + length > b1.Length) return false;
			if (b2Offset + length > b2.Length) return false;

			bool match = true;

			for (int i = 0; i < length && match; i++)
			{
				match = (b1[b1Offset + i] == b2[b2Offset + i]);
			}

			return match;
		}

		/// <summary>
		/// Reads the entire file, and stores the result in a Data object wrapping the read bytes.
		/// Warning: This method is only to be used for small files.
		/// </summary>
		/// <param name="filePath">
		///		Relative or absolute path to file.
		/// </param>
		/// <returns>
		///		A regular Data object, which wraps the read bytes from the file.
		/// </returns>
		public static Data ReadFile(string filePath)
		{
			Data result = null;
			FileStream fs = null;

			try
			{
				fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
				result = new Data((int)fs.Length);

				int amountRead = fs.Read(result.ByteArray, 0, result.Length);
				int totalAmountRead = amountRead;

				while((amountRead > 0) && (totalAmountRead < result.Length))
				{
					amountRead = fs.Read(result.ByteArray, totalAmountRead, result.Length - totalAmountRead);
					totalAmountRead += amountRead;
				}

				if (totalAmountRead < result.Length)
				{
					result = null;
				}
			}
			catch
			{
				result = null;
			}
			finally
			{
				if(fs != null) fs.Close();
			}

			return result;
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////
		#endregion
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////

		protected byte[] buffer;

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////
		#region Constructors
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////

		/// <summary>
		/// Creates a new Data object using the given buffer.
		/// The buffer is not copied.
		/// That is, the new Data object is simply a wrapper around the given byte array.
		/// </summary>
		/// <param name="buffer">
		///		Byte array to use as underlying data.
		///	</param>
		public Data(byte[] buffer) : this(buffer, false)
		{
			// Nothing to do here
		}

		/// <summary>
		/// Creates a new Data object using the given buffer.
		/// If the copy flag is set, this method will create a new buffer, and copy the data from the given buffer into it.
		/// Thus changes to the given buffer will not affect this Data object.
		/// Otherwise the new Data object will simply form a wrapper around the given data (without copying anything).
		/// </summary>
		/// <param name="buffer">
		///		Byte array to use for underlying data.
		/// </param>
		/// <param name="copy">
		///		Whether or not to copy data from the given buffer into a new buffer.
		///	</param>
		public Data(byte[] buffer, bool copy)
		{
			if (copy)
			{
				this.buffer = new byte[buffer.Length];
				Buffer.BlockCopy(buffer, 0, this.buffer, 0, buffer.Length);
			}
			else
			{
				this.buffer = buffer;
			}
		}

		/// <summary>
		/// Creates a new Data object using a specified subset of the given data.
		/// The data must necessarily be copied (otherwise it would be unsafe).
		/// </summary>
		/// <param name="buffer">
		///		Byte array to extract data from.
		/// </param>
		/// <param name="offset">
		///		The offset within buffer to start reading from.
		/// </param>
		/// <param name="length">
		///		The amount to read from buffer.
		/// </param>
		public Data(byte[] buffer, int offset, int length)
		{
			this.buffer = new byte[length];
			Buffer.BlockCopy(buffer, offset, this.buffer, 0, length);
		}

		/// <summary>
		/// Creates a new Data object using the data.
		/// The data is not copied.
		/// That is, the new Data object is simply a wrapper around that same data.
		/// </summary>
		/// <param name="buffer">
		///		Data to use as underlying data.
		///	</param>
		public Data(Data data) : this(data.ByteArray, false)
		{
			// Nothing to do here
		}

		/// <summary>
		/// Creates a new Data object using the given data.
		/// If the copy flag is set, this method will create a new buffer, and copy the buffer from the given data into it.
		/// Thus changes to the given data will not affect this Data object.
		/// Otherwise the new Data object will simply form a wrapper around the given data (without copying anything).
		/// 
		/// Note: If you pass a Data object which uses an internal stream (IsStream = true), the data is always copied.
		/// </summary>
		/// <param name="buffer">
		///		Byte array to use for underlying data.
		/// </param>
		/// <param name="copy">
		///		Whether or not to copy data from the given buffer into a new buffer.
		///	</param>
		public Data(Data data, bool copy) : this(data.ByteArray, copy)
		{
			// Nothing to do here
		}

		/// <summary>
		/// Creates a new Data object using a specified subset of the given data.
		/// The data must necessarily be copied (otherwise it would be unsafe).
		/// </summary>
		/// <param name="buffer">
		///		Byte array to use for underlying data.
		/// </param>
		/// <param name="offset">
		///		The offset within data to start reading from.
		/// </param>
		/// <param name="data">
		///		The amount to read from data.
		/// </param>
		public Data(Data data, int offset, int length) : this(data.ByteArray, offset, length)
		{
			// Nothing to do here
		}

		/// <summary>
		/// Creates a new Data object wrapping a newly created byte array of the given size.
		/// </summary>
		/// <param name="length">
		///		The size to make the underlying byte array.
		///	</param>
		public Data(int length)
		{
			buffer = new byte[length];
		}

		/// <summary>
		/// Creates a new Data object, converting the string using UTF8.
		/// </summary>
		public Data(String str) : this(str, Encoding.UTF8)
		{
			// Nothing to do here
		}

		/// <summary>
		/// Creates a new Data object, converting the string with the given encoding.
		/// </summary>
		public Data(String str, Encoding enc)
		{
			this.buffer = enc.GetBytes(str);
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////
		#endregion
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////

		/// <summary>
		/// Returns the length of the data.
		/// </summary>
		public int Length
		{
			get { return buffer.Length; }
		}

		/// <summary>
		/// Returns the entire underlying data as a byte array.
		/// Use this method when you need to pass a byte array as a method parameter.
		/// </summary>
		public byte[] ByteArray
		{
			get { return buffer; }
		}

		/// <summary>
		/// Reads a byte at the given index.
		/// </summary>
		/// <param name="index">
		///		The index at which to read.
		///	</param>
		public byte this[int index]
		{
			get { return buffer[index]; }
		}

		/// <summary>
		/// Copies a portion of the data into a given byte array.
		/// </summary>
		public void Copy(out byte[] result, int offset, int length)
		{
			int available = buffer.Length - offset;
			int realLength = (available < length) ? available : length;

			result = new byte[realLength];
			Buffer.BlockCopy(buffer, offset, result, 0, realLength);
		}

		/// <summary>
		/// Reads data up to and including the given termination sequence.
		/// Then encodes and returns the resulting data in the set encoding.
		/// This method is a simple extension to LookForTerm.
		/// </summary>
		/// <param name="offset">
		///		The offset to start reading data from the underlying byte array.
		/// </param>
		/// <param name="term">
		///		The termination sequence. Ex "\n", "\r\n", ",", etc.
		/// </param>
		/// <param name="length">
		///		The length of data that was read.
		/// </param>
		/// <returns>
		///		The read data, generated by reading from the offset, up to and including the terminator,
		///		and encoding the result in the currently set encoding.
		///		Null if the terminator is not found.
		///	</returns>
		///	<exception cref="System.ArgumentException">
		///		Thrown when offset >= Data.Length, or if term.Length == 0
		///	</exception>
		///	<exception cref="System.ArgumentNullException">
		///		Thrown when term is null.
		///	</exception>
		public String ReadThroughTerm(int offset, byte[] term, out int length)
		{
			int termStartIndex = LookForTerm(offset, term);

			if (termStartIndex < 0)
			{
				length = 0;
				return null;
			}
			else
			{
				length = termStartIndex + term.Length - offset;
				return Encoding.UTF8.GetString(buffer, offset, length);
			}
		}

		/// <summary>
		/// Looks for the given termination sequence in the data, starting from the given offset.
		/// 
		/// Example:
		/// If the underlying data represents the following string:
		/// Host: deusty.com\r\nCheese: Yes Please
		/// 
		/// And this method is called with offset=0, and term="\r\n",
		/// then this method will return 16.
		/// </summary>
		/// <param name="offset">
		///		The offset from which to start looking for the termination sequence.
		/// </param>
		/// <param name="term">
		///		The termination sequence to look for.
		/// </param>
		/// <returns>
		///		Returns the starting position of the given term, if found.
		///		Otherwise, returns -1.
		/// </returns>
		/// <exception cref="System.ArgumentException">
		///		Thrown when offset >= Data.Length, or if term.Length == 0
		///	</exception>
		///	<exception cref="System.ArgumentNullException">
		///		Thrown when term is null.
		///	</exception>
		public int LookForTerm(int offset, byte[] term)
		{
			if (offset >= buffer.Length) throw new ArgumentException("offset is >= Length", "offset");
			
			if (term == null) throw new ArgumentNullException("term");
			if (term.Length == 0) throw new ArgumentException("term.Length == 0", "term");

			// Look for the terminating sequence in the buffer
			int i = offset;
			bool found = false;

			while (i < buffer.Length && !found)
			{
				bool match = i + term.Length < buffer.Length;

				for (int j = 0; match && j < term.Length; j++)
				{
					match = (buffer[i + j] == term[j]);
				}

				if (match)
					found = true;
				else
					i++;
			}

			if (found)
			{
				return i - offset;
			}
			else
			{
				return -1;
			}
		}

		/// <summary>
		/// Reads the entire data into a string.
		/// Uses the default encoding (UTF8).
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return Encoding.UTF8.GetString(buffer);
		}

		/// <summary>
		/// Reads the entire data into a string using the given encoding.
		/// </summary>
		/// <param name="encoding">
		///		The encoding to use when converting from raw bytes to a string.
		/// </param>
		/// <returns>
		///		A string from the data in the given encoding.
		/// </returns>
		public string ToString(Encoding encoding)
		{
			return encoding.GetString(buffer);
		}

		/// <summary>
		/// Converts the data to a hexadecimal string format.
		/// </summary>
		public string ToHexString()
		{
			return Data.ToHexString(buffer);
		}

		/// <summary>
		/// Writes the data to the given filepath.
		/// If the file doesn't exist, it is created.
		/// If it does exist, it is overwritten.
		/// </summary>
		/// <param name="filepath">
		///		The filepath (relative or absolute) to write to.
		/// </param>
		/// <returns>
		///		True if the write finished successfully.
		///		False otherwise.
		/// </returns>
		public bool WriteToFile(String filepath)
		{
			Exception e;
			return WriteToFile(filepath, out e);
		}

		/// <summary>
		/// Writes the data to the given filepath.
		/// If the file doesn't exist, it is created.
		/// If it does exist, it is overwritten.
		/// </summary>
		/// <param name="filepath">
		///		The filepath (relative or absolute) to write to.
		/// </param>
		/// <param name="e">
		///		If this method returns false, e will be set to the exception that occurred.
		///		Otherwise e will be set to null.
		/// </param>
		/// <returns>
		///		True if the write finished successfully.
		///		False otherwise.
		/// </returns>
		public bool WriteToFile(String filepath, out Exception e)
		{
			e = null;

			bool result = false;

			FileStream fs = null;
			try
			{
				fs = File.Create(filepath);

				fs.Write(buffer, 0, buffer.Length);

				result = true;
			}
			catch (Exception ex)
			{
				e = ex;
			}
			finally
			{
				if (fs != null) fs.Close();
			}

			return result;
		}
	}

	/// <summary>
	/// Provides a mutable (changeable) container for raw data.
	/// </summary>
	public class MutableData : Data
	{
		/// <summary>
		/// Creates a new zero-length MutableData object.
		/// </summary>
		public MutableData() : base(new byte[0], false)
		{
			// Nothing to do here
		}

		public MutableData(byte[] buffer) : base(buffer, false)
		{
			// Nothing to do here
		}

		public MutableData(byte[] buffer, bool copy) : base(buffer, copy)
		{
			// Nothing to do here
		}

		public MutableData(byte[] buffer, int offset, int length) : base(buffer, offset, length)
		{
			// Nothing to do here
		}

		public MutableData(Data data) : base(data, false)
		{
			// Nothing to do here
		}

		public MutableData(Data data, bool copy) : base(data, copy)
		{
			// Nothing to do here
		}

		public MutableData(Data data, int offset, int length) : base(data, offset, length)
		{
			// Nothing to do here
		}

		public MutableData(int length) : base(length)
		{
			// Nothing to do here
		}

		public MutableData(String str) : base(str, Encoding.UTF8)
		{
			// Nothing to do here
		}

		public MutableData(String str, Encoding enc) : base(str, enc)
		{
			// Nothing to do here
		}

		/// <summary>
		/// Increases the length of the underlying byte array by the given length.
		/// Does nothing if the given length is non-positive.
		/// To truncate data, use the setLength method, or one of the trim methods.
		/// </summary>
		/// <param name="extraLength">
		///		The length in bytes.
		///	</param>
		public void IncreaseLength(int extraLength)
		{
			// Ignore the request if non-positive extra length given
			if (extraLength <= 0) return;

			int newLength = buffer.Length + extraLength;

			byte[] newBuffer = new byte[newLength];

			Buffer.BlockCopy(buffer, 0, newBuffer, 0, buffer.Length);
			buffer = newBuffer;
		}

		/// <summary>
		/// Sets the length of the underlying byte array.
		/// If the given length is the same as the current length, this method does nothing.
		/// If the given length is greater than the current length,
		/// a new bigger array will be created and the bytes will be copied into it.
		/// If the given length is less than the current length,
		/// a new smaller array will be created and the bytes will be copied into it, with excess data being truncated.
		/// </summary>
		/// <param name="length">
		///		The length in bytes.
		/// </param>
		/// <exception cref="System.ArgumentException">
		///		Thrown only if the length parameter is negative.
		///	</exception>
		public void SetLength(int length)
		{
			// Throw exception if negative length given
			if (length < 0) throw new ArgumentException("length must be >= 0", "length");

			// Ignore the request if we're already using the given length
			if (length == buffer.Length) return;

			byte[] newBuffer = new byte[length];

			// Depending on length, we may be increasing or decreasing our buffer
			// If we're increasing it, we need to copy the full buffer
			// If we're decreasing it, we can only copy a portion of the buffer
			int count = (buffer.Length < length) ? buffer.Length : length;

			Buffer.BlockCopy(buffer, 0, newBuffer, 0, count);
			buffer = newBuffer;
		}

		/// <summary>
		/// Trims a given amount from the beginning of the underlying buffer.
		/// </summary>
		/// <param name="length">
		///		The number of bytes to trim.
		///	</param>
		public void TrimStart(int length)
		{
			// Throw exception if negative length given
			if (length < 0) throw new ArgumentException("length must be >= 0", "length");

			// Ignore the request if length is zero
			if (length == 0) return;

			// Make sure we don't try to trim more than what exists
			int offset = (length < buffer.Length) ? length : buffer.Length;
			int count = buffer.Length - offset;

			byte[] newBuffer = new byte[count];

			Buffer.BlockCopy(buffer, offset, newBuffer, 0, count);
			buffer = newBuffer;
		}

		/// <summary>
		/// Trims a given amount from the end of the underlying buffer.
		/// </summary>
		/// <param name="length">
		///		The number of bytes to trim.
		///	</param>
		public void TrimEnd(int length)
		{
			// Throw exception if negative length given
			if (length < 0) throw new ArgumentException("length must be >= 0", "length");

			// Ignore the request if length is zero
			if (length == 0) return;

			// Make sure we don't try to trim more than what exists
			int count = (length < buffer.Length) ? length : buffer.Length;
			int offset = buffer.Length - count;

			byte[] newBuffer = new byte[count];

			Buffer.BlockCopy(buffer, offset, newBuffer, 0, count);
			buffer = newBuffer;
		}

		/// <summary>
		/// This method automatically increases the length of the data by the proper length,
		/// and copies the bytes from the given data object into the mutable data array.
		/// </summary>
		/// <param name="data">
		///		A Data object to copy bytes from.
		///	</param>
		public void AppendData(Data data)
		{
			// We're not going to bother checking to see if data is null.
			// The NullReferenceException will automatically get thrown for us if it is.

			AppendData(data.ByteArray);
		}

		/// <summary>
		/// Reads from the given data, starting at the given offset and reading the given length,
		/// and appends the read data to the underlying buffer.
		/// The underlying buffer length is automatically increased as needed.
		/// 
		/// This method properly handles reading from stream data (data.IsStream == true).
		/// </summary>
		/// <param name="data">
		///		The data to append to the end of the underlying buffer.
		/// </param>
		/// <param name="offset">
		///		The offset from which to start copying from the given data.
		/// </param>
		/// <param name="length">
		///		The amount to copy from the given data.
		/// </param>
		public void AppendData(Data data, int offset, int length)
		{
			AppendData(data.ByteArray, offset, length);
		}

		/// <summary>
		/// This method automatically increases the length of the data by the proper length,
		/// and copies the data from the given byte array into the mutable data array.
		/// </summary>
		/// <param name="byteArray">
		///		The array of bytes to append to the end of the current array.
		///	</param>
		public void AppendData(byte[] byteArray)
		{
			AppendData(byteArray, 0, byteArray.Length);
		}

		/// <summary>
		/// This method automatically increases the length of the data by the proper length,
		/// and copies the data from the given byte array into the underlying buffer.
		/// The data is copied starting at the given offset up to the given length.
		/// </summary>
		/// <param name="byteArray">
		///		The array of bytes to append to the end of the underlying buffer.
		/// </param>
		/// <param name="offset">
		///		The offset from which to start copying data from the given byteArray.
		/// </param>
		/// <param name="length">
		///		The amount of data to copy from the given byteArray.
		/// </param>
		public void AppendData(byte[] byteArray, int offset, int length)
		{
			byte[] newBuffer = new byte[buffer.Length + length];

			Buffer.BlockCopy(buffer, 0, newBuffer, 0, buffer.Length);
			Buffer.BlockCopy(byteArray, offset, newBuffer, buffer.Length, length);
			buffer = newBuffer;
		}

		/// <summary>
		/// This method automatically increases the length of the data by the proper length,
		/// and copies the data from the given byte array into the underlying buffer.
		/// The data is copied starting at the given offset up to the given length.
		/// The data is inserted into the underlying buffer at the given index.
		/// </summary>
		/// <param name="index">
		///		The position in this instance where insertion begins.
		/// </param>
		/// <param name="data">
		///		The data to insert into the underlying buffer.
		/// </param>
		public void InsertData(int index, Data data)
		{
			InsertData(index, data.ByteArray);
		}

		/// <summary>
		/// This method automatically increases the length of the data by the proper length,
		/// and copies the data from the given byte array into the underlying buffer.
		/// The data is copied starting at the given offset up to the given length.
		/// The data is inserted into the underlying buffer at the given index.
		/// </summary>
		/// <param name="index">
		///		The position in this instance where insertion begins.
		/// </param>
		/// <param name="data">
		///		The data to insert into the underlying buffer.
		/// </param>
		/// <param name="offset">
		///		The offset from which to start copying data from the given data.
		/// </param>
		/// <param name="length">
		///		The amount of data to copy from the given data.
		///	</param>
		public void InsertData(int index, Data data, int offset, int length)
		{
			InsertData(index, data.ByteArray, offset, length);
		}

		/// <summary>
		/// This method automatically increases the length of the data by the proper length,
		/// and copies the data from the given byte array into the underlying buffer.
		/// The data is copied starting at the given offset up to the given length.
		/// The data is inserted into the underlying buffer at the given index.
		/// </summary>
		/// <param name="index">
		///		The position in this instance where insertion begins.
		/// </param>
		/// <param name="byteArray">
		///		The array of bytes to insert into the underlying buffer.
		/// </param>
		public void InsertData(int index, byte[] byteArray)
		{
			InsertData(index, byteArray, 0, byteArray.Length);
		}

		/// <summary>
		/// This method automatically increases the length of the data by the proper length,
		/// and copies the data from the given byte array into the underlying buffer.
		/// The data is copied starting at the given offset up to the given length.
		/// The data is inserted into the underlying buffer at the given index.
		/// </summary>
		/// <param name="index">
		///		The position in this instance where insertion begins.
		/// </param>
		/// <param name="byteArray">
		///		The array of bytes to insert into the underlying buffer.
		/// </param>
		/// <param name="offset">
		///		The offset from which to start copying data from the given byteArray.
		/// </param>
		/// <param name="length">
		///		The amount of data to copy from the given byteArray.
		///	</param>
		public void InsertData(int index, byte[] byteArray, int offset, int length)
		{
			byte[] newBuffer = new byte[buffer.Length + length];

			Buffer.BlockCopy(buffer, 0, newBuffer, 0, index);
			Buffer.BlockCopy(byteArray, offset, newBuffer, index, length);
			Buffer.BlockCopy(buffer, index, newBuffer, index + length, buffer.Length - index);
			buffer = newBuffer;
		}
	}
}
