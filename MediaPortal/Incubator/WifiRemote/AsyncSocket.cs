using System;
using System.IO;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Diagnostics;
using System.Net.Security;
using System.Text;
using System.Security.Cryptography.X509Certificates;
using WifiRemote;

namespace Deusty.Net
{
	/// <summary>
	/// The AsyncSocket class allows for asynchronous socket activity,
	/// and has usefull methods that allow for controlled reading of a certain length,
	/// or until a specified terminator.
	/// It also has the ability to timeout asynchronous operations, and has several useful events.
	/// </summary>
	public class AsyncSocket
	{
		public delegate void SocketDidAccept(AsyncSocket sender, AsyncSocket newSocket);
		public delegate bool SocketWillConnect(AsyncSocket sender, Socket socket);
		public delegate void SocketDidConnect(AsyncSocket sender, IPAddress address, UInt16 port);
		public delegate void SocketDidRead(AsyncSocket sender, byte[] data, long tag);
		public delegate void SocketDidReadPartial(AsyncSocket sender, int partialLength, long tag);
		public delegate void SocketDidWrite(AsyncSocket sender, long tag);
		public delegate void SocketDidWritePartial(AsyncSocket sender, int partialLength, long tag);
		public delegate void SocketDidSecure(AsyncSocket sender, X509Certificate localCert, X509Certificate remoteCert);
		public delegate void SocketWillClose(AsyncSocket sender, Exception e);
		public delegate void SocketDidClose(AsyncSocket sender);

		public event SocketDidAccept DidAccept;
		public event SocketWillConnect WillConnect;
		public event SocketDidConnect DidConnect;
		public event SocketDidRead DidRead;
		public event SocketDidReadPartial DidReadPartial;
		public event SocketDidWrite DidWrite;
		public event SocketDidWritePartial DidWritePartial;
		public event SocketDidSecure DidSecure;
		public event SocketWillClose WillClose;
		public event SocketDidClose DidClose;

		private Socket socket4;
		private Socket socket6;
		private Stream stream;
		private NetworkStream socketStream;
		private SslStream secureSocketStream;

		private const int INIT_READQUEUE_CAPACITY = 5;
		private const int INIT_WRITEQUEUE_CAPACITY = 5;
		private const int INIT_EVENTQUEUE_CAPACITY = 5;

		private const int CONNECTION_QUEUE_CAPACITY = 10;

		private const int READ_CHUNKSIZE    = (1024 * 16);
		private const int READALL_CHUNKSIZE = (1024 * 32);
		private const int WRITE_CHUNKSIZE   = (1024 * 32);

		private volatile byte flags;
		private const byte kDidPassConnectMethod  = 1 << 0;  // If set, disconnection results in delegate call
		private const byte kPauseReads            = 1 << 1;  // If set, reads are not dequeued until further notice
		private const byte kPauseWrites           = 1 << 2;  // If set, writes are not dequeued until further notice
		private const byte kForbidReadsWrites     = 1 << 3;  // If set, no new reads or writes are allowed
		private const byte kCloseAfterReads       = 1 << 4;  // If set, disconnect after no more reads are queued
		private const byte kCloseAfterWrites      = 1 << 5;  // If set, disconnect after no more writes are queued
		private const byte kClosingWithError      = 1 << 6;  // If set, socket is being closed due to an error
		private const byte kClosed                = 1 << 7;  // If set, socket is considered closed

		private Queue readQueue;
		private Queue writeQueue;
		private Queue eventQueue;

		private AsyncReadPacket currentRead;
		private AsyncWritePacket currentWrite;

		private System.Threading.Timer connectTimer;
		private System.Threading.Timer readTimer;
		private System.Threading.Timer writeTimer;

		private MutableData readOverflow;

		// We use a seperate lock object instead of locking on 'this'.
		// This is necessary to avoid a tricky deadlock situation.
		// The generated methods that handle += and -= calls to events actually lock on 'this'.
		// So the following is possible:
		// - We invoke one of our OnEventHandler methods from within a lock(this) block.
		// - There is a SynchronizedObject set, and we invoke callbacks on it.
		// - A registered delegate receives the callback on a seperate thread.
		// - The registered delegate then attempts to add a delegate to one of our events.
		// - Deadlock!
		// - The += method is blocking until we finish our lock(this) block.
		// - We won't finish our lock(this) block until the delegate methods complete. 
		private Object lockObj = new Object();

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////
		#region Utility Classes
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////

		/// <summary>
		/// The AsyncReadPacket encompasses the instructions for a read.
		/// The content of a read packet allows the code to determine if we're:
		/// reading to a certain length, reading to a certain separator, or simply reading the first chunk of data.
		/// </summary>
		private class AsyncReadPacket
		{
			public MutableData buffer;
			public int bytesDone;
			public int bytesProcessing;
			public int timeout;
			public int maxLength;
			public long tag;
			public bool readAllAvailableData;
			public bool fixedLengthRead;
			public byte[] term;

			public AsyncReadPacket(MutableData buffer,
			                               int timeout,
			                               int maxLength,
			                              long tag,
			                              bool readAllAvailableData,
			                              bool fixedLengthRead,
			                            byte[] term)
			{
				this.buffer = buffer;
				this.bytesDone = 0;
				this.bytesProcessing = 0;
				this.timeout = timeout;
				this.maxLength = maxLength;
				this.tag = tag;
				this.readAllAvailableData = readAllAvailableData;
				this.fixedLengthRead = fixedLengthRead;
				this.term = term;
			}
		}

		/// <summary>
		/// The AsyncWritePacket encompasses the instructions for a write.
		/// </summary>
		private class AsyncWritePacket
		{
			public byte[] buffer;
			public int offset;
			public int length;
			public int bytesDone;
			public int bytesProcessing;
			public int timeout;
			public long tag;

			public AsyncWritePacket(byte[] buffer,
			                         int offset,
			                         int length,
			                         int timeout,
			                        long tag)
			{
				this.buffer = buffer;
				this.offset = offset;
				this.length = length;
				this.bytesDone = 0;
				this.bytesProcessing = 0;
				this.timeout = timeout;
				this.tag = tag;
			}
		}

		/// <summary>
		/// Encompasses special instructions for interruptions in the read/write queues.
		/// This class my be altered to support more than just TLS in the future.
		/// </summary>
		private class AsyncSpecialPacket
		{
			public bool startTLS;

			public AsyncSpecialPacket(bool startTLS)
			{
				this.startTLS = startTLS;
			}
		}

		private class ConnectParameters
		{
			public String host;
			public UInt16 port;

			public ConnectParameters(String host, UInt16 port)
			{
				this.host = host;
				this.port = port;
			}
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////
		#endregion
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////
		#region Setup
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////

		/// <summary>
		/// Default Constructor.
		/// </summary>
		public AsyncSocket()
		{
			// Initialize an empty set of flags
			// During execution, various flags are set to allow us to track what's been done
			// and what needs to be done.
			flags = 0;

			// Initialize queues (thread safe)
			readQueue = Queue.Synchronized(new Queue(INIT_READQUEUE_CAPACITY));
			writeQueue = Queue.Synchronized(new Queue(INIT_WRITEQUEUE_CAPACITY));
			eventQueue = Queue.Synchronized(new Queue(INIT_EVENTQUEUE_CAPACITY));
		}

		private Object mTag;
		/// <summary>
		/// Gets or sets the object that contains data about the socket.
		/// <remarks>
		///		Any type derived from the Object class can be assigned to this property.
		///		A common use for the Tag property is to store data that is closely associated with the socket.
		/// </remarks>
		/// </summary>
		public Object Tag
		{
			get { return mTag; }
			set { mTag = value; }
		}

		private System.ComponentModel.ISynchronizeInvoke synchronizingObject = null;
		/// <summary>
		/// Set the <see cref="System.ComponentModel.ISynchronizeInvoke">ISynchronizeInvoke</see>
		/// object to use as the invoke object. When returning results from asynchronous calls,
		/// the Invoke method on this object will be called to pass the results back
		/// in a thread safe manner.
		/// </summary>
		/// <remarks>
		/// If using in conjunction with a form, it is highly recommended
		/// that you pass your main <see cref="System.Windows.Forms.Form">form</see> (window) in.
		/// </remarks>
		/// <remarks>
		/// You should configure your invoke options before you start reading/writing.
		/// It's recommended you don't change your invoke options in the middle of reading/writing.
		/// </remarks>
		public System.ComponentModel.ISynchronizeInvoke SynchronizingObject
		{
			get { return synchronizingObject; }
			set { synchronizingObject = value; }
		}

		private bool allowApplicationForms = true;
		/// <summary>
		/// Allows the application to attempt to post async replies over the
		/// application "main loop" by using the message queue of the first available
		/// open form (window). This is retrieved through
		/// <see cref="System.Windows.Forms.Application.OpenForms">Application.OpenForms</see>.
		/// 
		/// Note: This is true by default.
		/// </summary>
		/// <remarks>
		/// You should configure your invoke options before you start reading/writing.
		/// It's recommended you don't change your invoke options in the middle of reading/writing.
		/// </remarks>
		public bool AllowApplicationForms
		{
			get { return allowApplicationForms; }
			set { allowApplicationForms = value; }
		}

		private bool allowMultithreadedCallbacks = false;
		/// <summary>
		/// If set to true, <see cref="AllowApplicationForms">AllowApplicationForms</see>
		/// is set to false and <see cref="SynchronizingObject">SynchronizingObject</see> is set
		/// to null. Any time an asynchronous method needs to invoke a delegate method
		/// it will run the method in its own thread.
		/// </summary>
		/// <remarks>
		/// If set to true, you will have to handle any synchronization needed.
		/// If your application uses Windows.Forms or any other non-thread safe
		/// library, then you will have to do your own invoking.
		/// </remarks>
		/// <remarks>
		/// You should configure your invoke options before you start reading/writing.
		/// It's recommended you don't change your invoke options in the middle of reading/writing.
		/// </remarks>
		public bool AllowMultithreadedCallbacks
		{
			get { return allowMultithreadedCallbacks; }
			set
			{
				allowMultithreadedCallbacks = value;
				if (allowMultithreadedCallbacks)
				{
					allowApplicationForms = false;
					synchronizingObject = null;
				}
			}
		}
			
		// What is going on with the event handler methods below?
		// 
		// The asynchronous nature of this class means that we're very multithreaded.
		// But the client may not be. The client may be using a SynchronizingObject,
		// or has requested we use application forms for invoking.
		//
		// A problem arises from this situation:
		// If a client calls the Disconnect method, then he/she does NOT
		// expect to receive any other delegate methods after the
		// call to Diconnect completes.
		// 
		// Primitive invoking from a background thread will not solve this problem.
		// So what we do instead is invoke into the same thread as the client,
		// then check to make sure the socket hasn't been closed,
		// and then execute the delegate method.

		protected virtual void OnSocketDidAccept(AsyncSocket newSocket)
		{
			// SYNCHRONOUS
			// This allows the newly accepted socket to register to receive the DidConnect event.

			if (DidAccept != null)
			{
				if (synchronizingObject != null)
				{
					object[] args = { newSocket };
					synchronizingObject.Invoke(new DoDidAcceptDelegate(DoDidAccept), args);
				}
				else if (allowApplicationForms)
				{
					System.Windows.Forms.Form appForm = GetApplicationForm();
					if (appForm != null)
					{
						appForm.Invoke(new DoDidAcceptDelegate(DoDidAccept), newSocket);
					}
				}
				else if (allowMultithreadedCallbacks)
				{
					object[] delPlusArgs = { DidAccept, this, newSocket };
					eventQueue.Enqueue(delPlusArgs);
					ProcessEvent();
				}
			}
		}

		protected virtual bool OnSocketWillConnect(Socket socket)
		{
			// SYNCHRONOUS
			// This must be synchronous because we need to return a boolean value.

			if (WillConnect != null)
			{
				object result = null;

				if (synchronizingObject != null)
				{
					object[] args = { socket };
					result = synchronizingObject.Invoke(new DoWillConnectDelegate(DoWillConnect), args);
				}
				else if (allowApplicationForms)
				{
					System.Windows.Forms.Form appForm = GetApplicationForm();
					if (appForm != null)
					{
						result = appForm.Invoke(new DoWillConnectDelegate(DoWillConnect), socket);
					}
				}
				else if (allowMultithreadedCallbacks)
				{
					// Note: This is the first event that occurs (for outgoing connection)

					object[] delPlusArgs = { WillConnect, this, socket };
					eventQueue.Enqueue(delPlusArgs);
					result = ProcessEvent();
				}

				return (result == null || !(result is bool)) ? true : (bool)result;
			}

			return true;
		}

		protected virtual void OnSocketDidConnect(IPAddress address, UInt16 port)
		{
			// ASYNCHRONOUS
			// This allows the socket to quickly move on to previously scheduled operations,
			// such as TLS or reading/writing.

			if (DidConnect != null)
			{
				if (synchronizingObject != null)
				{
					object[] args = { address, port };
					synchronizingObject.BeginInvoke(new DoDidConnectDelegate(DoDidConnect), args);
				}
				else if (allowApplicationForms)
				{
					System.Windows.Forms.Form appForm = GetApplicationForm();
					if (appForm != null)
					{
						appForm.BeginInvoke(new DoDidConnectDelegate(DoDidConnect), address, port);
					}
				}
				else if (allowMultithreadedCallbacks)
				{
					object[] delPlusArgs = { DidConnect, this, address, port };
					eventQueue.Enqueue(delPlusArgs);
					ThreadPool.QueueUserWorkItem(new WaitCallback(ProcessEvent));
				}
			}
		}

		protected virtual void OnSocketDidRead(byte[] data, long tag)
		{
			// ASYNCHRONOUS
			// This allows the socket to quickly move on to the next read/write operation.

			if (DidRead != null)
			{
				if (synchronizingObject != null)
				{
					object[] args = { data, tag };
					synchronizingObject.BeginInvoke(new DoDidReadDelegate(DoDidRead), args);
				}
				else if (allowApplicationForms)
				{
					System.Windows.Forms.Form appForm = GetApplicationForm();
					if (appForm != null)
					{
						appForm.BeginInvoke(new DoDidReadDelegate(DoDidRead), data, tag);
					}
				}
				else if (allowMultithreadedCallbacks)
				{
					object[] delPlusArgs = { DidRead, this, data, tag };
					eventQueue.Enqueue(delPlusArgs);
					ThreadPool.QueueUserWorkItem(new WaitCallback(ProcessEvent));
				}
			}
		}

		protected virtual void OnSocketDidReadPartial(int partialLength, long tag)
		{
			// ASYNCHRONOUS
			// This allows the socket to quickly continue reading.

			if (DidReadPartial != null)
			{
				if (synchronizingObject != null)
				{
					object[] args = { partialLength, tag };
					synchronizingObject.BeginInvoke(new DoDidReadPartialDelegate(DoDidReadPartial), args);
				}
				else if (allowApplicationForms)
				{
					System.Windows.Forms.Form appForm = GetApplicationForm();
					if (appForm != null)
					{
						appForm.BeginInvoke(new DoDidReadPartialDelegate(DoDidReadPartial), partialLength, tag);
					}
				}
				else if (allowMultithreadedCallbacks)
				{
					object[] delPlusArgs = { DidReadPartial, this, partialLength, tag };
					eventQueue.Enqueue(delPlusArgs);
					ThreadPool.QueueUserWorkItem(new WaitCallback(ProcessEvent));
				}
			}
		}

		protected virtual void OnSocketDidWrite(long tag)
		{
			// ASYNCHRONOUS
			// This allows the socket to quickly move on to the next read/write operation.

			if (DidWrite != null)
			{
				if (synchronizingObject != null)
				{
					object[] args = { tag };
					synchronizingObject.BeginInvoke(new DoDidWriteDelegate(DoDidWrite), args);
				}
				else if (allowApplicationForms)
				{
					System.Windows.Forms.Form appForm = GetApplicationForm();
					if (appForm != null)
					{
						appForm.BeginInvoke(new DoDidWriteDelegate(DoDidWrite), tag);
					}
				}
				else if (allowMultithreadedCallbacks)
				{
					object[] delPlusArgs = { DidWrite, this, tag };
					eventQueue.Enqueue(delPlusArgs);
					ThreadPool.QueueUserWorkItem(new WaitCallback(ProcessEvent));
				}
			}
		}

		protected virtual void OnSocketDidWritePartial(int partialLength, long tag)
		{
			// ASYNCHRONOUS
			// This allows the socket to quickly continue writing.

			if (DidWritePartial != null)
			{
				if (synchronizingObject != null)
				{
					object[] args = { partialLength, tag };
					synchronizingObject.BeginInvoke(new DoDidWritePartialDelegate(DoDidWritePartial), args);
				}
				else if (allowApplicationForms)
				{
					System.Windows.Forms.Form appForm = GetApplicationForm();
					if (appForm != null)
					{
						appForm.BeginInvoke(new DoDidWritePartialDelegate(DoDidWritePartial), partialLength, tag);
					}
				}
				else if (allowMultithreadedCallbacks)
				{
					object[] delPlusArgs = { DidWritePartial, this, partialLength, tag };
					eventQueue.Enqueue(delPlusArgs);
					ThreadPool.QueueUserWorkItem(new WaitCallback(ProcessEvent));
				}
			}
		}

		protected virtual void OnSocketDidSecure(X509Certificate localCert, X509Certificate remoteCert)
		{
			// ASYNCHRONOUS
			// This allows the socket to quickly move on to previously scheduled read/write operations.

			if (DidSecure != null)
			{
				if (synchronizingObject != null)
				{
					object[] args = { localCert, remoteCert };
					synchronizingObject.BeginInvoke(new DoDidSecureDelegate(DoDidSecure), args);
				}
				else if (allowApplicationForms)
				{
					System.Windows.Forms.Form appForm = GetApplicationForm();
					if (appForm != null)
					{
						appForm.BeginInvoke(new DoDidSecureDelegate(DoDidSecure), localCert, remoteCert);
					}
				}
				else if (allowMultithreadedCallbacks)
				{
					object[] delPlusArgs = { DidSecure, this, localCert, remoteCert };
					eventQueue.Enqueue(delPlusArgs);
					ThreadPool.QueueUserWorkItem(new WaitCallback(ProcessEvent));
				}
			}
		}

		protected virtual void OnSocketWillClose(Exception e)
		{
			// SYNCHRONOUS
			// The unread data buffer is only available during this callback.

			if (WillClose != null)
			{
				if (synchronizingObject != null)
				{
					object[] args = { e };
					synchronizingObject.Invoke(new DoWillCloseDelegate(DoWillClose), args);
				}
				else if (allowApplicationForms)
				{
					System.Windows.Forms.Form appForm = GetApplicationForm();
					if (appForm != null)
					{
						appForm.Invoke(new DoWillCloseDelegate(DoWillClose), e);
					}
				}
				else if (allowMultithreadedCallbacks)
				{
					// Note: This is the second to last event that occurs (for outgoing connection)

					object[] delPlusArgs = { WillClose, this, e };
					eventQueue.Enqueue(delPlusArgs);
					ProcessEvent();
				}
			}
		}

		protected virtual void OnSocketDidClose()
		{
			// SYNCHRONOUS
			// It might as well be synchronous as we have nothing else to do afterwards.

			if (DidClose != null)
			{
				if (synchronizingObject != null)
				{
					synchronizingObject.Invoke(new DoDidCloseDelegate(DoDidClose), null);
				}
				else if (allowApplicationForms)
				{
					System.Windows.Forms.Form appForm = GetApplicationForm();
					if (appForm != null)
					{
						appForm.Invoke(new DoDidCloseDelegate(DoDidClose));
					}
				}
				else if (allowMultithreadedCallbacks)
				{
					// Note: This is the last event that occurs (for outgoing connection)

					object[] delPlusArgs = { DidClose, this };
					eventQueue.Enqueue(delPlusArgs);
					ProcessEvent();
				}
			}
		}

		private delegate void DoDidAcceptDelegate(AsyncSocket newSocket);
		private void DoDidAccept(AsyncSocket newSocket)
		{
			// Threading Notes:
			// This method is called when using a SynchronizingObject or AppForms,
			// so method is executed on the same thread that the delegate is using.
			// Thus, the kClosed flag prevents any callbacks after the delegate calls the close method.

			if ((flags & kClosed) != 0) return;
			
			try
			{
				if (DidAccept != null)
				{
					DidAccept(this, newSocket);
				}
			}
			catch { }
		}

		private delegate bool DoWillConnectDelegate(Socket socket);
		private bool DoWillConnect(Socket socket)
		{
			// Threading Notes:
			// This method is called when using a SynchronizingObject or AppForms,
			// so method is executed on the same thread that the delegate is using.
			// Thus, the kClosed flag prevents any callbacks after the delegate calls the close method.

			if ((flags & kClosed) != 0) return false;

			try
			{
				if (WillConnect != null)
				{
					return WillConnect(this, socket);
				}
			}
			catch { }

			return true;
		}

		private delegate void DoDidConnectDelegate(IPAddress address, UInt16 port);
		private void DoDidConnect(IPAddress address, UInt16 port)
		{
			// Threading Notes:
			// This method is called when using a SynchronizingObject or AppForms,
			// so method is executed on the same thread that the delegate is using.
			// Thus, the kClosed flag prevents any callbacks after the delegate calls the close method.

			if ((flags & kClosed) != 0) return;

			try
			{
				if (DidConnect != null)
				{
					DidConnect(this, address, port);
				}
			}
			catch { }
		}

		private delegate void DoDidReadDelegate(byte[] data, long tag);
		private void DoDidRead(byte[] data, long tag)
		{
			// Threading Notes:
			// This method is called when using a SynchronizingObject or AppForms,
			// so method is executed on the same thread that the delegate is using.
			// Thus, the kClosed flag prevents any callbacks after the delegate calls the close method.

			if ((flags & kClosed) != 0) return;

			try
			{
				if (DidRead != null)
				{
					DidRead(this, data, tag);
				}
			}
			catch { }
		}
		
		private delegate void DoDidReadPartialDelegate(int partialLength, long tag);
		private void DoDidReadPartial(int partialLength, long tag)
		{
			// Threading Notes:
			// This method is called when using a SynchronizingObject or AppForms,
			// so method is executed on the same thread that the delegate is using.
			// Thus, the kClosed flag prevents any callbacks after the delegate calls the close method.

			if ((flags & kClosed) != 0) return;

			try
			{
				if (DidReadPartial != null)
				{
					DidReadPartial(this, partialLength, tag);
				}
			}
			catch { }
		}

		private delegate void DoDidWriteDelegate(long tag);
		private void DoDidWrite(long tag)
		{
			// Threading Notes:
			// This method is called when using a SynchronizingObject or AppForms,
			// so method is executed on the same thread that the delegate is using.
			// Thus, the kClosed flag prevents any callbacks after the delegate calls the close method.

			if ((flags & kClosed) != 0) return;

			try
			{
				if (DidWrite != null)
				{
					DidWrite(this, tag);
				}
			}
			catch { }
		}

		private delegate void DoDidWritePartialDelegate(int partialLength, long tag);
		private void DoDidWritePartial(int partialLength, long tag)
		{
			// Threading Notes:
			// This method is called when using a SynchronizingObject or AppForms,
			// so method is executed on the same thread that the delegate is using.
			// Thus, the kClosed flag prevents any callbacks after the delegate calls the close method.

			if ((flags & kClosed) != 0) return;

			try
			{
				if (DidWritePartial != null)
				{
					DidWritePartial(this, partialLength, tag);
				}
			}
			catch { }
		}

		private delegate void DoDidSecureDelegate(X509Certificate localCert, X509Certificate remoteCert);
		private void DoDidSecure(X509Certificate localCert, X509Certificate remoteCert)
		{
			// Threading Notes:
			// This method is called when using a SynchronizingObject or AppForms,
			// so method is executed on the same thread that the delegate is using.
			// Thus, the kClosed flag prevents any callbacks after the delegate calls the close method.

			if ((flags & kClosed) != 0) return;

			try
			{
				if (DidSecure != null)
				{
					DidSecure(this, localCert, remoteCert);
				}
			}
			catch { }
		}

		private delegate void DoWillCloseDelegate(Exception e);
		private void DoWillClose(Exception e)
		{
			// Threading Notes:
			// This method is called when using a SynchronizingObject or AppForms,
			// so method is executed on the same thread that the delegate is using.
			// Thus, the kClosed flag prevents any callbacks after the delegate calls the close method.

			if ((flags & kClosed) != 0) return;

			try
			{
				if (WillClose != null)
				{
					WillClose(this, e);
				}
			}
			catch { }
		}

		private delegate void DoDidCloseDelegate();
		private void DoDidClose()
		{
			// Threading Notes:
			// This method is called when using a SynchronizingObject or AppForms,
			// so method is executed on the same thread that the delegate is using.
			// Thus, the kClosed flag prevents any callbacks after the delegate calls the close method.

			try
			{
				if (DidClose != null)
				{
					DidClose(this);
				}
			}
			catch { }
		}

		/// <summary>
		/// Processes a single event in the event queue.
		/// This method is used in muliti-threaded mode for asynchronous events.
		/// </summary>
		private void ProcessEvent(object ignore)
		{
			lock (eventQueue)
			{
				if (eventQueue.Count == 0) return;

				object[] delPlusArgs = (object[])eventQueue.Dequeue();
				object[] args = new object[delPlusArgs.Length - 1];

				Delegate del = (Delegate)delPlusArgs[0];
				Array.Copy(delPlusArgs, 1, args, 0, delPlusArgs.Length - 1);

				bool shouldInvokeDelegate = true;

				if (del == null)
				{
					shouldInvokeDelegate = false;
				}
				else if ((flags & kClosed) > 0)
				{
					// Note: The DidClose event is synchronous, so it wouldn't use this method
					shouldInvokeDelegate = false;
				}

				if (shouldInvokeDelegate)
				{
					try
					{
						del.DynamicInvoke(args);
					}
					catch { }
				}
			}
		}

		/// <summary>
		/// Processes every event in the event queue, and returns the value from the last event.
		/// This method is used in multi-threaded mode for synchronous events.
		/// </summary>
		private object ProcessEvent()
		{
			lock (eventQueue)
			{
				object result = null;

				while (eventQueue.Count > 0)
				{
					object[] delPlusArgs = (object[])eventQueue.Dequeue();
					object[] args = new object[delPlusArgs.Length - 1];
					
					Delegate del = (Delegate)delPlusArgs[0];
					Array.Copy(delPlusArgs, 1, args, 0, delPlusArgs.Length - 1);

					bool shouldInvokeDelegate = true;

					if (del == null)
					{
						shouldInvokeDelegate = false;
					}
					else if ((flags & kClosed) > 0)
					{
						shouldInvokeDelegate = (del == (Delegate)DidClose);
					}

					if (shouldInvokeDelegate)
					{
						try
						{
							result = del.DynamicInvoke(args);
						}
						catch { }
					}
				}

				return result;
			}
		}

		/// <summary>
		/// Returns a form that can be used to invoke an event.
		/// </summary>
		private System.Windows.Forms.Form GetApplicationForm()
		{
			System.Windows.Forms.FormCollection forms = System.Windows.Forms.Application.OpenForms;

			if (forms != null && forms.Count > 0)
			{
				return forms[0];
			}

			return null;
		}

		/// <summary>
		/// Allows invoke options to be inherited from another AsyncSocket.
		/// This is usefull when accepting connections.
		/// </summary>
		/// <param name="fromSocket">
		///		AsyncSocket object to copy invoke options from.
		///	</param>
		protected void InheritInvokeOptions(AsyncSocket fromSocket)
		{
			// We set the MultiThreadedCallback property first,
			// as it has the potential to affect the other properties.
			AllowMultithreadedCallbacks = fromSocket.AllowMultithreadedCallbacks;

			AllowApplicationForms = fromSocket.AllowApplicationForms;
			SynchronizingObject = fromSocket.SynchronizingObject;
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////
		#endregion
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////
		#region Progress
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////

		public float ProgressOfCurrentRead()
		{
			long tag;
			int bytesDone;
			int total;

			float result = ProgressOfCurrentRead(out tag, out bytesDone, out total);
			return result;
		}

		public float ProgressOfCurrentRead(out long tag, out int bytesDone, out int total)
		{
			// First get a reference to the current read.
			// We do this because the currentRead pointer could be changed in a separate thread.
			// And locking should not be done in this method
			// because it's public, and could potentially cause deadlock.
			AsyncReadPacket thisRead = null;
			Interlocked.Exchange(ref thisRead, currentRead);
			
			// Check to make sure we're actually reading something right now
			if (thisRead == null)
			{
				tag = ((long)0);
				bytesDone = 0;
				total = 0;
				return float.NaN;
			}
			
			// It's only possible to know the progress of our read if we're reading to a certain length
			// If we're reading to data, we of course have no idea when the data will arrive
			// If we're reading to timeout, then we have no idea when the next chunk of data will arrive.
			bool hasTotal = thisRead.fixedLengthRead;
			
			tag = thisRead.tag;
			bytesDone = thisRead.bytesDone;
			total = hasTotal ? thisRead.buffer.Length : 0;
			
			if (total > 0)
				return (((float)bytesDone) / ((float)total));
			else
				return 1.0f;
		}

		public float ProgressOfCurrentWrite()
		{
			long tag;
			int bytesDone;
			int total;

			float result = ProgressOfCurrentWrite(out tag, out bytesDone, out total);
			return result;
		}

		public float ProgressOfCurrentWrite(out long tag, out int bytesDone, out int total)
		{
			// First get a reference to the current write.
			// We do this because the currentWrite pointer could be changed in a separate thread.
			// And locking should not be done in this method
			// because it's public, and could potentially cause deadlock.
			AsyncWritePacket thisWrite = null;
			Interlocked.Exchange(ref thisWrite, currentWrite);
			
			// Check to make sure we're actually writing something right now
			if (thisWrite == null)
			{
				tag = ((long)0);
				bytesDone = 0;
				total = 0;
				return float.NaN;
			}
			
			tag = thisWrite.tag;
			bytesDone = thisWrite.bytesDone;
			total = thisWrite.buffer.Length;
			
			return (((float)bytesDone) / ((float)total));
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////
		#endregion
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////
		#region Accepting
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////

		/// <summary>
		/// Tells the socket to begin accepting connections on the given port.
		/// The socket will listen on all interfaces.
		/// Be sure to register to receive DidAccept events.
		/// </summary>
		/// <param name="port">
		///		The port to accept connections on. Pass 0 to allow the OS to pick any available port.
		/// </param>
		/// <returns>
		///		True if the socket was able to begin listening for connections on the given address and port.
		///		False otherwise.  If false consult the error parameter for more information.
		/// </returns>
		public bool Accept(UInt16 port)
		{
			Exception error;
			return Accept(null, port, out error);
		}

		/// <summary>
		/// Tells the socket to begin accepting connections on the given port.
		/// The socket will listen on all interfaces.
		/// Be sure to register to receive DidAccept events.
		/// </summary>
		/// <param name="port">
		///		The port to accept connections on. Pass 0 to allow the OS to pick any available port.
		/// </param>
		/// <param name="error">
		///		If this method returns false, the error will contain the reason for it's failure.
		/// </param>
		/// <returns>
		///		True if the socket was able to begin listening for connections on the given address and port.
		///		False otherwise.  If false consult the error parameter for more information.
		/// </returns>
		public bool Accept(UInt16 port, out Exception error)
		{
			return Accept(null, port, out error);
		}

		/// <summary>
		/// Tells the socket to begin accepting connections on the given address and port.
		/// Be sure to register to receive DidAccept events.
		/// </summary>
		/// <param name="hostaddr">
		///		A string that contains an IP address in dotted-quad notation for IPv4
		///		or in colon-hexadecimal notation for IPv6.
		///		For convenience, you may also pass the strings "loopback" or "localhost".
		/// </param>
		/// <param name="port">
		///		The port to accept connections on. Pass 0 to allow the OS to pick any available port.
		/// </param>
		/// <returns>
		///		True if the socket was able to begin listening for connections on the given address and port.
		///		False otherwise.  If false consult the error parameter for more information.
		/// </returns>
		public bool Accept(String hostaddr, UInt16 port)
		{
			Exception error;
			return Accept(hostaddr, port, out error);
		}

		/// <summary>
		/// Tells the socket to begin accepting connections on the given address and port.
		/// Be sure to register to receive DidAccept events.
		/// </summary>
		/// <param name="hostaddr">
		///		A string that contains an IP address in dotted-quad notation for IPv4
		///		or in colon-hexadecimal notation for IPv6.
		///		For convenience, you may also pass the strings "loopback" or "localhost".
		///		Pass null to listen on all interfaces.
		/// </param>
		/// <param name="port">
		///		The port to accept connections on. Pass 0 to allow the OS to pick any available port.
		/// </param>
		/// <param name="error">
		///		If this method returns false, the error will contain the reason for it's failure.
		/// </param>
		/// <returns>
		///		True if the socket was able to begin listening for connections on the given address and port.
		///		False otherwise.  If false consult the error parameter for more information.
		/// </returns>
		public bool Accept(String hostaddr, UInt16 port, out Exception error)
		{
			error = null;
			
			// Make sure we're not closed
			if ((flags & kClosed) != 0)
			{
				String msg = "Socket is closed.";
				error = new Exception(msg);
				return false;
			}

			// Make sure we're not already listening for connections, or already connected
			if ((flags & kDidPassConnectMethod) != 0)
			{
				String msg = "Attempting to connect while connected or accepting connections.";
				error = new Exception(msg);
				return false;
			}

			// Extract proper IPAddress(es) from the given hostaddr
			IPAddress address4 = null;
			IPAddress address6 = null;

			if (hostaddr == null)
			{
				address4 = IPAddress.Any;
				address6 = IPAddress.IPv6Any;
			}
			else
			{
				if (hostaddr.Equals("loopback") || hostaddr.Equals("localhost"))
				{
					address4 = IPAddress.Loopback;
					address6 = IPAddress.IPv6Loopback;
				}
				else
				{
					try
					{
						IPAddress addr = IPAddress.Parse(hostaddr);
						if(addr.AddressFamily == AddressFamily.InterNetwork)
						{
							address4 = addr;
							address6 = null;
						}
						else if(addr.AddressFamily == AddressFamily.InterNetworkV6)
						{
							address4 = null;
							address6 = addr;
						}
					}
					catch (Exception e)
					{
						error = e;
						return false;
					}

					if ((address4 == null) && (address6 == null))
					{
						String msg = String.Format("hostaddr ({0}) is not a valid IPv4 or IPv6 address", hostaddr);
						error = new Exception(msg);
						return false;
					}
				}
			}
			
			// Watch out for versions of XP that don't support IPv6
			if (!Socket.OSSupportsIPv6)
			{
				if (address4 == null)
				{
					error = new Exception("Requesting IPv6, but OS does not support it.");
					return false;
				}
				address6 = null;
			}

			// Attention: Lock within public method.
			// Note: Should be fine since we can only get this far if the socket is null.
			lock (lockObj)
			{
				try
				{
					// Initialize socket(s)
					if(address4 != null)
					{
						// Initialize socket
						socket4 = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
						
						// Always reuse address
						socket4.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
						
						// Bind the socket to the proper address/port
						socket4.Bind(new IPEndPoint(address4, port));
						
						// Start listening (using the preset max pending connection queue size)
						socket4.Listen(CONNECTION_QUEUE_CAPACITY);
						
						// Start accepting connections
						socket4.BeginAccept(new AsyncCallback(socket_DidAccept), socket4);
					}
					if(address6 != null)
					{
						// Initialize socket
						socket6 = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
						
						// Always reuse address
						socket6.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
						
						// Bind the socket to the proper address/port
						socket6.Bind(new IPEndPoint(address6, port));
						
						// Start listening (using the preset max pending connection queue size)
						socket6.Listen(CONNECTION_QUEUE_CAPACITY);
						
						// Start accepting connections
						socket6.BeginAccept(new AsyncCallback(socket_DidAccept), socket6);
					}
				}
				catch (Exception e)
				{
					error = e;
					if (socket4 != null)
					{
						socket4.Close();
						socket4 = null;
					}
					if (socket6 != null)
					{
						socket6.Close();
						socket6 = null;
					}
					return false;
				}

				flags |= kDidPassConnectMethod;
			}

			return true;
		}

		/// <summary>
		/// Description forthcoming
		/// </summary>
		/// <param name="iar"></param>
		private void socket_DidAccept(IAsyncResult iar)
		{
			lock (lockObj)
			{
				if ((flags & kClosed) > 0) return;

				try
				{
					Socket socket = (Socket)iar.AsyncState;

					Socket newSocket = socket.EndAccept(iar);
					AsyncSocket newAsyncSocket = new AsyncSocket();

					newAsyncSocket.InheritInvokeOptions(this);
					newAsyncSocket.PreConfigure(newSocket);

					OnSocketDidAccept(newAsyncSocket);

					newAsyncSocket.PostConfigure();

					// And listen for more connections
					socket.BeginAccept(new AsyncCallback(socket_DidAccept), socket);
				}
				catch (Exception e)
				{
					CloseWithException(e);
				}
			}
		}

		/// <summary>
		/// Called to configure an AsyncSocket after an accept has occured.
		/// This is called before OnSocketDidAccept.
		/// </summary>
		/// <param name="socket">
		///		The newly accepted socket.
		/// </param>
		private void PreConfigure(Socket socket)
		{
			// Store socket
			if(socket.AddressFamily == AddressFamily.InterNetwork)
			{
				this.socket4 = socket;
			}
			else
			{
				this.socket6 = socket;
			}

			// Create NetworkStream from new socket
			socketStream = new NetworkStream(socket);
			stream = socketStream;
			flags |= kDidPassConnectMethod;
		}

		/// <summary>
		/// Called to configure an AsyncSocket after an accept has occured.
		/// This is called after OnSocketDidAccept.
		/// </summary>
		private void PostConfigure()
		{
			// Notify the delegate
			OnSocketDidConnect(RemoteAddress, RemotePort);

			// Immediately deal with any already-queued requests.
			// Notice that we delay the call to allow execution in socket_DidAccept().
			ThreadPool.QueueUserWorkItem(new WaitCallback(MaybeDequeueRead));
			ThreadPool.QueueUserWorkItem(new WaitCallback(MaybeDequeueWrite));
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////
		#endregion
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////
		#region Connecting
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////

		/// <summary>
		/// Begins an asynchronous connection attempt to the specified host and port.
		/// Returns false if the connection attempt immediately fails.
		/// If this method succeeds, the delegate will be informed of the
		/// connection success/failure via the proper delegate methods.
		/// </summary>
		/// <param name="host">
		///		The host name or IP address to connect to.
		///		E.g. "deusty.com" or "70.85.193.226" or "2002:cd9:3ea8:0:88c8:b211:b605:ab59"
		/// </param>
		/// <param name="port">
		///		The port to connect to (eg. 80)
		/// </param>
		/// <returns>
		/// 	True if the socket was able to begin attempting to connect to the given host and port.
		///		False otherwise.
		/// </returns>
		public bool Connect(String host, UInt16 port)
		{
			Exception error;
			return Connect(host, port, out error);
		}

		/// <summary>
		/// Begins an asynchronous connection attempt to the specified host and port.
		/// Returns false if the connection attempt immediately fails.
		/// If this method succeeds, the delegate will be informed of the
		/// connection success/failure via the proper delegate methods.
		/// </summary>
		/// <param name="host">
		///		The host name or IP address to connect to.
		///		E.g. "deusty.com" or "70.85.193.226" or "2002:cd9:3ea8:0:88c8:b211:b605:ab59"
		/// </param>
		/// <param name="port">
		///		The port to connect to (eg. 80)
		/// </param>
		/// <param name="timeout">
		///		Timeout in milliseconds. Specify a negative value if no timeout is desired.
		/// </param>
		/// <returns>
		///		True if the socket was able to begin attempting to connect to the given host and port.
		///		False otherwise.
		///	</returns>
		public bool Connect(String host, UInt16 port, int timeout)
		{
			Exception error;
			return Connect(host, port, timeout, out error);
		}

		/// <summary>
		/// Begins an asynchronous connection attempt to the specified host and port.
		/// Returns false if the connection attempt immediately failed, in which case the error parameter will be set.
		/// If this method succeeds, the delegate will be informed of the
		/// connection success/failure via the proper delegate methods.
		/// </summary>
		/// <param name="host">
		///		The host name or IP address to connect to.
		///		E.g. "deusty.com" or "70.85.193.226" or "2002:cd9:3ea8:0:88c8:b211:b605:ab59"
		/// </param>
		/// <param name="port">
		///		The port to connect to (eg. 80)
		/// </param>
		/// <param name="error">
		///		If this method returns false, the error will contain the reason for it's failure.
		/// </param>
		/// <returns>
		/// 	True if the socket was able to begin attempting to connect to the given host and port.
		///		False otherwise.  If false consult the error parameter for more information.
		/// </returns>
		public bool Connect(String host, UInt16 port, out Exception error)
		{
			return Connect(host, port, -1, out error);
		}

		/// <summary>
		/// Begins an asynchronous connection attempt to the specified host and port.
		/// Returns false if the connection attempt immediately failed, in which case the error parameter will be set.
		/// If this method succeeds, the delegate will be informed of the
		/// connection success/failure via the proper delegate methods.
		/// </summary>
		/// <param name="host">
		///		The host name or IP address to connect to.
		///		E.g. "deusty.com" or "70.85.193.226" or "2002:cd9:3ea8:0:88c8:b211:b605:ab59"
		/// </param>
		/// <param name="port">
		///		The port to connect to (eg. 80)
		/// </param>
		/// <param name="timeout">
		///		Timeout in milliseconds. Specify a negative value if no timeout is desired.
		/// </param>
		/// <param name="error">
		///		If this method returns false, the error will contain the reason for it's failure.
		/// </param>
		/// <returns>
		///		True if the socket was able to begin attempting to connect to the given host and port.
		///		False otherwise.  If false consult the error parameter for more information.
		///	</returns>
		public bool Connect(String host, UInt16 port, int timeout, out Exception error)
		{
			error = null;
			
			// Make sure we're not closed
			if ((flags & kClosed) != 0)
			{
				String msg = "Socket is closed.";
				error = new Exception(msg);
				return false;
			}

			// Make sure we're not already connected, or listening for connections
			if ((flags & kDidPassConnectMethod) > 0)
			{
				String e = "Attempting to connect while connected or accepting connections.";
				error = new Exception(e);
				return false;
			}

			// Attention: Lock within public method.
			// Note: Should be fine since we can only get this far if the socket is null.
			lock (lockObj)
			{
				try
				{
					// We're about to start resolving the host name asynchronously.
					ConnectParameters parameters = new ConnectParameters(host, port);

					// Start time-out timer
					if (timeout >= 0)
					{
						connectTimer = new System.Threading.Timer(new TimerCallback(socket_DidNotConnect),
						                                          parameters,
						                                          timeout,
						                                          Timeout.Infinite);
					}

					// Start resolving the host
					Dns.BeginGetHostAddresses(host, new AsyncCallback(Dns_DidResolve), parameters);
				}
				catch (Exception e)
				{
					error = e;
					return false;
				}

				flags |= kDidPassConnectMethod;
			}

			return true;
		}

		/// <summary>
		/// Callback method when dns has resolved the host (or was unable to resolve it).
		/// 
		/// This method is thread safe.
		/// </summary>
		/// <param name="iar">
		///		The state of the IAsyncResult refers to the ConnectRequest object
		///		containing the parameters of the original call to the Connect() method.
		/// </param>
		private void Dns_DidResolve(IAsyncResult iar)
		{
			ConnectParameters parameters = (ConnectParameters)iar.AsyncState;

			lock (lockObj)
			{
				// Check to make sure the async socket hasn't been closed.
				if ((flags & kClosed) > 0)
				{
					// We no longer need the result of the dns query.
					// Properly end the async procedure, but ignore the result.
					try
					{
						Dns.EndGetHostAddresses(iar);
					}
					catch { }
					
					return;
				}

				IPAddress[] addresses = null;

				bool done = false;
				bool cancelled = false;

				try
				{
					addresses = Dns.EndGetHostAddresses(iar);

					for (int i = 0; i < addresses.Length && !done && !cancelled; i++)
					{
						IPAddress address = addresses[i];

						if (address.AddressFamily == AddressFamily.InterNetwork)
						{
							// Initialize a new socket
							socket4 = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

							// Allow delegate to configure the socket if needed
							if (OnSocketWillConnect(socket4))
							{
								// Attempt to connect with the given information
								socket4.BeginConnect(address, parameters.port, new AsyncCallback(socket_DidConnect), socket4);

								// Stop looping through addresses
								done = true;
							}
							else
							{
								cancelled = true;
							}
						}
						else if (address.AddressFamily == AddressFamily.InterNetworkV6)
						{
							if (Socket.OSSupportsIPv6)
							{
								// Initialize a new socket
								socket6 = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);

								// Allow delegate to configure the socket if needed
								if (OnSocketWillConnect(socket6))
								{
									// Attempt to connect with the given information
									socket6.BeginConnect(address, parameters.port, new AsyncCallback(socket_DidConnect), socket6);

									// Stop looping through addresses
									done = true;
								}
								else
								{
									cancelled = true;
								}
							}
						}
					}
				}
				catch(Exception e)
				{
					CloseWithException(e);
				}

				if ((addresses == null) || (addresses.Length == 0))
				{
					String msg = String.Format("Unable to resolve host \"{0}\"", parameters.host);
					CloseWithException(new Exception(msg));
				}

				if (cancelled)
				{
					String msg = "Connection attempt cancelled in WillConnect delegate";
					CloseWithException(new Exception(msg));
				}

				if (!done)
				{
					String format = "Unable to resolve host \"{0}\" to valid IPv4 or IPv6 address";
					String msg = String.Format(format, parameters.host);
					CloseWithException(new Exception(msg));
				}
			}
		}

		/// <summary>
		/// Callback method when socket has connected (or was unable to connect).
		/// 
		/// This method is thread safe.
		/// </summary>
		/// <param name="iar">
		///		The state of the IAsyncResult refers to the socket that called BeginConnect().
		/// </param>
		private void socket_DidConnect(IAsyncResult iar)
		{
			// We lock in this method to ensure that the SocketDidConnect delegate fires before
			// processing any reads or writes. ScheduledDequeue methods may be lurking.
			// Also this ensures the flags are properly updated prior to any other locked method executing.
			lock (lockObj)
			{
				if ((flags & kClosed) > 0) return;

				try
				{
					Socket socket = (Socket)iar.AsyncState;

					socket.EndConnect(iar);

					socketStream = new NetworkStream(socket);
					stream = socketStream;

					// Notify the delegate
					OnSocketDidConnect(RemoteAddress, RemotePort);

					// Cancel the connect timer
					if (connectTimer != null)
					{
						connectTimer.Dispose();
						connectTimer = null;
					}

					// Immediately deal with any already-queued requests.
					MaybeDequeueRead(null);
					MaybeDequeueWrite(null);
				}
				catch (Exception e)
				{
					CloseWithException(e);
				}
			}
		}

		/// <summary>
		/// Called after a connect timeout timer fires.
		/// This will fire on an available thread from the thread pool.
		/// 
		/// This method is thread safe.
		/// </summary>
		private void socket_DidNotConnect(object ignore)
		{
			lock (lockObj)
			{
				if ((flags & kClosed) > 0) return;

				// The timer may have fired in the middle of the socket_DidConnect method above.
				// In this case, the lock would have prevented both methods from running at the same time.
				// Check to make sure we still don't have a socketStream, because if we do then we've connected.
				if (socketStream == null)
				{
					CloseWithException(GetConnectTimeoutException());
				}
			}
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////
		#endregion
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////
		#region Security
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////

		// Temporary variables for storing StartTLS information before we can actually start TLS
		private bool isTLSClient;
		private String tlsServerName;
		private RemoteCertificateValidationCallback tlsRemoteCallback;
		private LocalCertificateSelectionCallback tlsLocalCallback;
		private X509Certificate localCertificate;

		/// <summary>
		/// Secures the stream using SSL/TLS.
		/// The socket is secured immediately following any pending reads/writes already in the queue.
		/// Any reads/writes scheduled after this call will travel over a secure connection.
		/// 
		/// Note: You can't just call this on any old connection.
		/// TLS requires support on both ends, and should be called in accordance with the protocol in use.
		/// 
		/// Note: You cannot pass a null serverName.
		/// If you don't know the server name, pass an empty string, and use the remote callback as needed.
		/// 
		/// If TLS fails to authenticate, the event WillCloseWithException will fire with the reason.
		/// </summary>
		/// <param name="serverName">
		///		The expected server name on the remote certificate.
		///		This cannot be null. If you don't know, pass an empty stream and use the remote callback as needed.
		/// </param>
		/// <param name="rcvc">
		///		A RemoteCertificateValidationCallback delegate responsible for
		///		validating the certificate supplied by the remote party.
		///		Pass null if you don't need this functionality.
		/// </param>
		/// <param name="lcsc">
		///		A LocalCertificateSelectionCallback delegate responsible for
		///		selecting the certificate used for authentication.
		///		Pass null if you don't need this functionality.
		/// </param>
		public void StartTLSAsClient(String serverName, RemoteCertificateValidationCallback rcvc,
		                                                  LocalCertificateSelectionCallback lcsc)
		{
			// Update tls variables - we'll need to refer to these later when we actually start tls
			isTLSClient = true;
			tlsServerName = serverName;
			tlsRemoteCallback = rcvc;
			tlsLocalCallback = lcsc;

			// Inject StartTLS packets into read and write queues.
			// Once all pending reads and writes have completed, the StartTLS procedure will commence.
			AsyncSpecialPacket startTlsPacket = new AsyncSpecialPacket(true);
			readQueue.Enqueue(startTlsPacket);
			writeQueue.Enqueue(startTlsPacket);

			// Queue calls to MaybeDequeueRead and MaybeDequeueWrite
			ThreadPool.QueueUserWorkItem(new WaitCallback(MaybeDequeueRead));
			ThreadPool.QueueUserWorkItem(new WaitCallback(MaybeDequeueWrite));
		}
		
		/// <summary>
		/// Secures the stream using SSL/TLS.
		/// The socket is secured immediately following any pending reads/writes already in the queue.
		/// Any reads/writes scheduled after this call will travel over a secure connection.
		/// 
		/// Note: You must use the LocalCertificateSelectionCallback to return the required certificate for a server.
		/// 
		/// If TLS fails to authenticate, the event WillCloseWithException will fire with the reason.
		/// </summary>
		/// <param name="rcvc">
		///		A RemoteCertificateValidationCallback delegate responsible for
		///		validating the certificate supplied by the remote party.
		///		Pass null if you don't need this functionality.
		/// </param>
		/// <param name="lcsc">
		///		A LocalCertificateSelectionCallback delegate responsible for
		///		selecting the certificate used for authentication.
		///		Pass null if you don't need this functionality.
		/// </param>
		public void StartTLSAsServer(X509Certificate serverCertificate, RemoteCertificateValidationCallback rcvc,
		                                                                  LocalCertificateSelectionCallback lcsc)
		{
			// Update tls variables - we'll need to refer to these later when we actually start tls
			isTLSClient = false;
			localCertificate = serverCertificate;
			tlsRemoteCallback = rcvc;
			tlsLocalCallback = lcsc;

			// Inject StartTLS packets into read and write queues.
			// Once all pending reads and writes have completed, the StartTLS procedure will commence.
			AsyncSpecialPacket startTlsPacket = new AsyncSpecialPacket(true);
			readQueue.Enqueue(startTlsPacket);
			writeQueue.Enqueue(startTlsPacket);

			// Queue calls to MaybeDequeueRead and MaybeDequeueWrite
			ThreadPool.QueueUserWorkItem(new WaitCallback(MaybeDequeueRead));
			ThreadPool.QueueUserWorkItem(new WaitCallback(MaybeDequeueWrite));
		}

		/// <summary>
		/// Starts the TLS procedure ONLY if it's the correct time to do so.
		/// This is dependent on several variables, such as the kPause flags, connected property, etc.
		/// 
		/// This method is NOT thread safe, and should only be invoked via thread safe methods.
		/// </summary>
		private void MaybeStartTLS()
		{
			Debug.Assert(socketStream != null, "Attempting to start tls without a connected socket");
			Trace.Assert(secureSocketStream == null, "Attempting to start tls after tls has already completed");

			// We can't start TLS until:
			// - Any queued reads prior to the user calling StartTLS are complete
			// - Any queued writes prior to the user calling StartTLS are complete

			if (((flags & kPauseReads) > 0) && ((flags & kPauseWrites) > 0))
			{
				try
				{
					secureSocketStream = new SslStream(socketStream, true, tlsRemoteCallback, tlsLocalCallback);

					if (isTLSClient)
					{
						secureSocketStream.BeginAuthenticateAsClient(tlsServerName,
												   new AsyncCallback(secureSocketStream_DidFinish), null);
					}
					else
					{
						secureSocketStream.BeginAuthenticateAsServer(localCertificate,
												   new AsyncCallback(secureSocketStream_DidFinish), null);
					}
				}
				catch (Exception e)
				{
					// The most likely cause of this exception is a null tlsServerName.
					CloseWithException(e);
				}
			}
		}
		
		/// <summary>
		/// Called when the secureSocketStream has finished TLS initialization.
		/// If it failed, then the End methods will throw an exception detailing the problem.
		/// 
		/// This method is thread safe.
		/// </summary>
		/// <param name="iar"></param>
		private void secureSocketStream_DidFinish(IAsyncResult iar)
		{
			lock (lockObj)
			{
				if ((flags & kClosed) > 0) return;

				try
				{
					if(isTLSClient)
					{
						secureSocketStream.EndAuthenticateAsClient(iar);
					}
					else
					{
						secureSocketStream.EndAuthenticateAsServer(iar);
					}
					
					// Update generic stream - everything goes through our encrypted stream now
					stream = secureSocketStream;
					
					// Update flags - unset pause flags
					flags ^= kPauseReads;
					flags ^= kPauseWrites;

					// Extract X509 certificates

					X509Certificate localCert = null;
					try
					{
						localCert = secureSocketStream.LocalCertificate;
					}
					catch { }

					X509Certificate remoteCert = null;
					try
					{
						remoteCert = secureSocketStream.RemoteCertificate;
					}
					catch { }

					// Invoke delegate method if needed
					OnSocketDidSecure(localCert, remoteCert);
					
					// And finally, resume reading and writing
					MaybeDequeueRead(null);
					MaybeDequeueWrite(null);
				}
				catch (Exception e)
				{
					CloseWithException(e);
				}
			}
		}
		
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////
		#endregion
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////
		
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////
		#region Disconnecting
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////

		/// <summary>
		/// Fires the WillDisconnect event, and then closes the socket.
		/// 
		/// This method is NOT thread safe, and should only be invoked via thread safe methods.
		/// </summary>
		/// <param name="e">
		/// 	The exception that occurred, to be sent to the client.
		/// </param>
		private void CloseWithException(Exception e)
		{
			flags |= kClosingWithError;

			if ((flags & kDidPassConnectMethod) > 0)
			{
				// Try to salvage what data we can
				RecoverUnreadData();

				// Let the delegate know, so it can try to recover if it likes.
				OnSocketWillClose(e);
			}
			
			Close(null);
		}

		/// <summary>
		/// This method extracts any unprocessed data, and makes it available to the client.
		/// 
		/// Called solely from CloseWithException, which is only called from thread safe methods.
		/// </summary>
		private void RecoverUnreadData()
		{
			if (currentRead != null)
			{
				// We never finished the current read.
				
				int bytesAvailable = currentRead.bytesDone + currentRead.bytesProcessing;

				if (readOverflow == null)
				{
					readOverflow = new MutableData(currentRead.buffer, 0, bytesAvailable);
				}
				else
				{
					// We need to move the data into the front of the read overflow
					readOverflow.InsertData(0, currentRead.buffer, 0, bytesAvailable);
				}
			}
		}

		/// <summary>
		/// Clears the read and writes queues.
		/// Remember that the queues are synchronized/thread-safe.
		/// </summary>
		private void EmptyQueues()
		{
			if (currentRead != null) EndCurrentRead();
			if (currentWrite != null) EndCurrentWrite();

			readQueue.Clear();
			writeQueue.Clear();
		}

		/// <summary>
		/// Drops pending reads and writes, closes all sockets and stream, and notifies delegate if needed.
		/// </summary>
		private void Close(object ignore)
		{
			lock (lockObj)
			{
				EmptyQueues();

				if (secureSocketStream != null)
				{
					secureSocketStream.Close();
					secureSocketStream = null;
				}
				if (socketStream != null)
				{
					socketStream.Close();
					socketStream = null;
				}
				if (stream != null)
				{
					// Stream is just a pointer to the real stream we're using
					// I.e. it points to either socketStream of secureSocketStream
					// Thus we don't close it
					stream = null;
				}
				if (socket6 != null)
				{
					socket6.Close();
					socket6 = null;
				}
				if (socket4 != null)
				{
					socket4.Close();
					socket4 = null;
				}

				if (connectTimer != null)
				{
					connectTimer.Dispose();
					connectTimer = null;
				}

				// The readTimer and writeTimer are cleared in the EmptyQueues method above.

				if ((flags & kDidPassConnectMethod) > 0)
				{
					// Clear flags to signal closed socket
					flags = (kForbidReadsWrites | kClosed);

					// Notify delegate that we're now disconnected
					OnSocketDidClose();
				}
				else
				{
					// Clear flags to signal closed socket
					flags = (kForbidReadsWrites | kClosed);
				}
			}
		}

		/// <summary>
		/// Immediately stops all transfers, and releases any socket and stream resources.
		/// Any pending reads or writes are dropped.
		/// 
		/// If the socket is already closed, this method does nothing.
		/// 
		/// Note: The SocketDidClose method will be called.
		/// </summary>
		public void Close()
		{
			flags |= kClosed;

			ThreadPool.QueueUserWorkItem(new WaitCallback(Close));
		}
		
		/// <summary>
		/// Closes the socket after all pending reads have completed.
		/// After calling this, the read and write methods will do nothing.
		/// The socket will close even if there are still pending writes.
		/// </summary>
		public void CloseAfterReading()
		{
			flags |= (kForbidReadsWrites | kCloseAfterReads);
			
			// Queue a call to MaybeClose
			ThreadPool.QueueUserWorkItem(new WaitCallback(MaybeClose));
		}
		
		/// <summary>
		/// Closes after all pending writes have completed.
		/// After calling this, the read and write methods will do nothing.
		/// The socket will close even if there are still pending reads.
		/// </summary>
		public void CloseAfterWriting()
		{
			flags |= (kForbidReadsWrites | kCloseAfterWrites);
			
			// Queue a call to MaybeClose
			ThreadPool.QueueUserWorkItem(new WaitCallback(MaybeClose));
		}

		/// <summary>
		/// Closes after all pending reads and writes have completed.
		/// After calling this, the read and write methods will do nothing.
		/// </summary>
		public void CloseAfterReadingAndWriting()
		{
			flags |= (kForbidReadsWrites | kCloseAfterReads | kCloseAfterWrites);

			// Queue a call to MaybeClose
			ThreadPool.QueueUserWorkItem(new WaitCallback(MaybeClose));
		}
		
		private void MaybeClose(object ignore)
		{
			lock (lockObj)
			{
				if ((flags & kCloseAfterReads) > 0)
				{
					if ((readQueue.Count == 0) && (currentRead == null))
					{
						if ((flags & kCloseAfterWrites) > 0)
						{
							if ((writeQueue.Count == 0) && (currentWrite == null))
							{
								Close(null);
							}
						}
						else
						{
							Close(null);
						}
					}
				}
				else if ((flags & kCloseAfterWrites) > 0)
				{
					if ((writeQueue.Count == 0) && (currentWrite == null))
					{
						Close(null);
					}
				}
			}
		}

		/// <summary>
		/// In the event of an error, this method may be called during SocketWillClose
		/// to read any data that's left on the socket.
		/// </summary>
		public byte[] GetUnreadData()
		{
			// Ensure this method will only return data in the event of an error
			if ((flags & kClosingWithError) == 0) return null;

			if (readOverflow == null)
				return new byte[0];
			else
				return readOverflow.ByteArray;
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////
		#endregion
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////
		#region Errors
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////

		private Exception GetEndOfStreamException()
		{
			return new Exception("Socket reached end of stream.");
		}

		private Exception GetConnectTimeoutException()
		{
			return new Exception("Connect operation timed out.");
		}

		private Exception GetReadTimeoutException()
		{
			return new Exception("Read operation timed out.");
		}

		private Exception GetWriteTimeoutException()
		{
			return new Exception("Write operation timed out.");
		}

		private Exception GetReadMaxedOutException()
		{
			return new Exception("Read operation reached set maximum length");
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////
		#endregion
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////
		#region Diagnostics
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////

		/// <summary>
		/// The Connected property gets the connection state of the Socket as of the last I/O operation.
		/// When it returns false, the Socket was either never connected, or is no longer connected.
		/// 
		/// Note that this functionallity matches normal Socket.Connected functionallity.
		/// </summary>
		public bool Connected
		{
			get
			{
				if(socket4 != null)
					return socket4.Connected;
				else
					return ((socket6 != null) && (socket6.Connected));
			}
		}

		/// <summary>
		/// Non-retarded method of Connected.
		/// Returns the logical answer to the question "Is this socket connected."
		/// </summary>
		public bool SmartConnected
		{
			get
			{
				if(socket4 != null)
					return GetIsSmartConnected(socket4);
				else
					return GetIsSmartConnected(socket6);
			}
		}

		private bool GetIsSmartConnected(Socket socket)
		{
			if (socket != null && socket.Connected)
			{
				bool blockingState = socket.Blocking;
				try
				{
					byte[] tmp = new byte[1];

					socket.Blocking = false;
					socket.Send(tmp, 0, 0);
				}
				catch (SocketException e)
				{
					// 10035 == WSAEWOULDBLOCK
					if (e.NativeErrorCode == 10035)
					{
						// Still Connected, but the Send would block
					}
					else
					{
						// Disconnected
					}
				}
				finally
				{
					socket.Blocking = blockingState;
				}

				return socket.Connected;
			}
			
			return false;
		}

		public IPAddress RemoteAddress
		{
			get
			{
				if(socket4 != null)
					return GetRemoteAddress(socket4);
				else
					return GetRemoteAddress(socket6);
			}
		}

		public UInt16 RemotePort
		{
			get
			{
				if(socket4 != null)
					return GetRemotePort(socket4);
				else
					return GetRemotePort(socket6);
			}
		}

		public IPAddress LocalAddress
		{
			get
			{
				if(socket4 != null)
					return GetLocalAddress(socket4);
				else
					return GetLocalAddress(socket6);
			}
		}

		public UInt16 LocalPort
		{
			get
			{
				if (socket4 != null)
					return GetLocalPort(socket4);
				else
					return GetLocalPort(socket6);
			}
		}

		private IPAddress GetRemoteAddress(Socket socket)
		{
			if (socket != null && socket.Connected)
			{
				IPEndPoint ep = (IPEndPoint)socket.RemoteEndPoint;
				if (ep != null)
					return ep.Address;
			}
			return null;
		}

		private UInt16 GetRemotePort(Socket socket)
		{
			if (socket != null && socket.Connected)
			{
				IPEndPoint ep = (IPEndPoint)socket.RemoteEndPoint;
				if (ep != null)
					return (UInt16)ep.Port;
			}
			return 0;
		}

		private IPAddress GetLocalAddress(Socket socket)
		{
			if (socket != null)
			{
				IPEndPoint ep = (IPEndPoint)socket.LocalEndPoint;
				if (ep != null)
					return ep.Address;
			}
			return null;
		}

		private UInt16 GetLocalPort(Socket socket)
		{
			if (socket != null)
			{
				IPEndPoint ep = (IPEndPoint)socket.LocalEndPoint;
				if (ep != null)
					return (UInt16)ep.Port;
			}
			return 0;
		}

		public int Available
		{
			get
			{
				if (socket4 != null)
					return socket4.Available;
				else if(socket6 != null)
					return socket6.Available;
				else
					return 0;
			}
		}

		public override string ToString()
		{
			// Todo: Add proper description for AsyncSocket
			return base.ToString();
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////
		#endregion
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////
		#region Reading
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////

		/// <summary>
		/// Reads the first available bytes on the socket.
		/// </summary>
		/// <param name="timeout">
		///		Timeout in milliseconds. Specify negative value for no timeout.
		/// </param>
		/// <param name="tag">
		///		Tag to identify read request.
		///	</param>
		public void Read(int timeout, long tag)
		{
			if ((flags & kForbidReadsWrites) > 0) return;

			MutableData buffer = new MutableData(0);

			// readQueue is synchronized
			readQueue.Enqueue(new AsyncReadPacket(buffer, timeout, -1, tag, true, false, null));

			// Queue a call to maybeDequeueRead
			ThreadPool.QueueUserWorkItem(new WaitCallback(MaybeDequeueRead));
		}

		/// <summary>
		/// Reads a certain number of bytes, and calls the delegate method when those bytes have been read.
		/// If length is 0, this method does nothing and no delgate methods are called.
		/// </summary>
		/// <param name="length">
		///		The number of bytes to read.
		/// </param>
		/// <param name="timeout">
		///		Timeout in milliseconds. Specify negative value for no timeout.
		/// </param>
		/// <param name="tag">
		///		Tag to identify read request.
		///	</param>
		public void Read(int length, int timeout, long tag)
		{
			if (length <= 0) return;
			if ((flags & kForbidReadsWrites) > 0) return;

			MutableData buffer = new MutableData(length);

			// readQueue is synchronized
			readQueue.Enqueue(new AsyncReadPacket(buffer, timeout, -1, tag, false, true, null));

			// Queue a call to maybeDequeueRead
			ThreadPool.QueueUserWorkItem(new WaitCallback(MaybeDequeueRead));
		}

		/// <summary>
		/// Reads bytes up to and including the passed data paramter, which acts as a separator.
		/// The bytes and the separator are returned by the delegate method.
		/// 
		/// If you pass null or zero-length data as the separator, this method will do nothing.
		/// To read a line from the socket, use the line separator (e.g. CRLF for HTTP) as the data parameter.
		/// Note that this method is not character-set aware, so if a separator can occur naturally
		/// as part of the encoding for a character, the read will prematurely end.
		/// </summary>
		/// <param name="term">
		///		The separator/delimeter to use.
		/// </param>
		/// <param name="timeout">
		///		Timeout in milliseconds. Specify negative value for no timeout.
		/// </param>
		/// <param name="tag">
		///		Tag to identify read request.
		/// </param>
		public void Read(byte[] term, int timeout, long tag)
		{
			if ((term == null) || (term.Length == 0)) return;
			if ((flags & kForbidReadsWrites) > 0) return;

			MutableData buffer = new MutableData(0);

			// readQueue is synchronized
			readQueue.Enqueue(new AsyncReadPacket(buffer, timeout, -1, tag, false, false, term));

			// Queue a call to MaybeDequeueRead
			ThreadPool.QueueUserWorkItem(new WaitCallback(MaybeDequeueRead));
		}

		/// <summary>
		/// Reads bytes up to and including the passed data parameter, which acts as a separator.
		/// The bytes and the separator are returned by the delegate method.
		/// 
		/// The amount of data read may not surpass the given maxLength (specified in bytes).
		/// If the max length is surpassed, it is treated the same as a timeout - the socket is closed.
		/// Pass -1 as maxLength if no length restriction is desired, or simply use the other Read method.
		/// 
		/// If you pass null or zero-length data as the separator, or if you pass a maxLength parameter that is
		/// less than the length of the data parameter, this method will do nothing.
		/// To read a line from the socket, use the line separator (e.g. CRLF for HTTP) as the data parameter.
		/// Not that this method is not character-set aware, so if a separator can occur naturally
		/// as part of the encoding for a character, the read will prematurely end.
		/// </summary>
		/// <param name="term">
		///		The separator/delimeter to use.
		/// </param>
		/// <param name="timeout">
		///		Timeout in milliseconds. Specify negative value for no timeout.
		/// </param>
		/// <param name="maxLength">
		///		Max length of data to read (in bytes). Specify negative value for no max length.
		/// </param>
		/// <param name="tag">
		///		Tag to identify read request.
		///	</param>
		public void Read(byte[] term, int timeout, int maxLength, long tag)
		{
			if ((term == null) || (term.Length == 0)) return;
			if ((maxLength >= 0) && (maxLength < term.Length)) return;
			if ((flags & kForbidReadsWrites) > 0) return;

			MutableData buffer = new MutableData(0);

			// readQueue is synchronized
			readQueue.Enqueue(new AsyncReadPacket(buffer, timeout, maxLength, tag, false, false, term));

			// Queue a call to MaybeDequeueRead
			ThreadPool.QueueUserWorkItem(new WaitCallback(MaybeDequeueRead));
		}

		/// <summary>
		/// If possible, this method dequeues a read from the read queue and starts it.
		/// This is only possible if all of the following are true:
		///  1) any previous read has completed
		///  2) there's a read in the queue
		///  3) and the stream is ready.
		/// 
		/// This method is thread safe.
		/// </summary>
		private void MaybeDequeueRead(object ignore)
		{
			lock (lockObj)
			{
				if ((flags & kClosed) > 0)
				{
					readQueue.Clear();
					return;
				}

				if ((currentRead == null) && (stream != null))
				{
					if((flags & kPauseReads) > 0)
					{
						// Don't do any reads yet.
						// We're waiting for TLS negotiation to start and/or finish.
					}
					else if(readQueue.Count > 0)
					{
						// Get the next object in the read queue
						Object nextRead = readQueue.Dequeue();

						if (nextRead is AsyncSpecialPacket)
						{
							// Next read packet is a special instruction packet.
							// Right now this can only mean a StartTLS instruction.
							AsyncSpecialPacket specialRead = (AsyncSpecialPacket)nextRead;

							// Update flags - this flag will be unset when TLS finishes
							flags |= kPauseReads;

							// And attempt to start TLS
							// This method won't do anything unless both kPauseReads and kPauseWrites are set.
							MaybeStartTLS();
						}
						else
						{
							// Get the new current read AsyncReadPacket
							currentRead = (AsyncReadPacket)nextRead;

							// Start time-out timer
							if (currentRead.timeout >= 0)
							{
								readTimer = new System.Threading.Timer(new TimerCallback(stream_DidNotRead),
																	   currentRead,
																	   currentRead.timeout,
																	   Timeout.Infinite);
							}

							// Do we have any overflow data that we've already read from the stream?
							if (readOverflow != null)
							{
								// Start reading from the overflow
								DoReadOverflow();
							}
							else
							{
								// Start reading from the stream
								DoStartRead();
							}
						}
					}
					else if((flags & kCloseAfterReads) > 0)
					{
						if ((flags & kCloseAfterWrites) > 0)
						{
							if ((writeQueue.Count == 0) && (currentWrite == null))
							{
								Close(null);
							}
						}
						else
						{
							Close(null);
						}
					}
				}
			}
		}

		/// <summary>
		/// This method fills the currentRead buffer with data from the readOverflow variable.
		/// After this is properly completed, DoFinishRead is called to process the bytes.
		/// 
		/// This method is called from MaybeDequeueRead().
		/// 
		/// The above method is thread safe, so this method is inherently thread safe.
		/// It is not explicitly thread safe though, and should not be called outside thread safe methods.
		/// </summary>
		private void DoReadOverflow()
		{
			Debug.Assert(currentRead.bytesDone == 0);
			Debug.Assert(readOverflow.Length > 0);

			try
			{
				if (currentRead.readAllAvailableData)
				{
					// We're supposed to read what's available.
					// What we have in the readOverflow is what we have available, so just use it.

					currentRead.buffer = readOverflow;
					currentRead.bytesProcessing = readOverflow.Length;

					readOverflow = null;
				}
				else if (currentRead.fixedLengthRead)
				{
					// We're reading a certain length of data.

					if (currentRead.buffer.Length < readOverflow.Length)
					{
						byte[] src = readOverflow.ByteArray;
						byte[] dst = currentRead.buffer.ByteArray;

						Buffer.BlockCopy(src, 0, dst, 0, dst.Length);

						currentRead.bytesProcessing = dst.Length;

						readOverflow.TrimStart(dst.Length);

						// Note that this is the only case in which the readOverflow isn't emptied.
						// This is OK because the read is guaranteed to finish in DoFinishRead().
					}
					else
					{
						byte[] src = readOverflow.ByteArray;
						byte[] dst = currentRead.buffer.ByteArray;

						Buffer.BlockCopy(src, 0, dst, 0, src.Length);

						currentRead.bytesProcessing = src.Length;

						readOverflow = null;
					}
				}
				else
				{
					// We're reading up to a termination sequence
					// So we can just set the currentRead buffer to the readOverflow
					// and the DoStartRead method will automatically handle any further overflow.

					currentRead.buffer = readOverflow;
					currentRead.bytesProcessing = readOverflow.Length;

					readOverflow = null;
				}

				// At this point we've filled a currentRead buffer with some data
				// And the currentRead.bytesProcessing is set to the amount of data we filled it with
				// It's now time to process the data.
				DoFinishRead();
			}
			catch (Exception e)
			{
				CloseWithException(e);
			}
		}

		/// <summary>
		/// This method is called when either:
		///  A) a new read is taken from the read queue
		///  B) or when data has just been read from the stream, and we need to read more.
		/// 
		/// More specifically, it is called from either:
		///  A) MaybeDequeueRead()
		///  B) DoFinishRead()
		/// 
		/// The above methods are thread safe, or inherently thread safe, so this method is inherently thread safe.
		/// It is not explicitly thread safe though, and should not be called outside thread safe methods.
		/// </summary>
		private void DoStartRead()
		{
			try
			{
				// Perform an AsyncRead to notify us of when data becomes available on the socket.

				// Determine how much to read
				int size;
				if (currentRead.readAllAvailableData)
				{
					size = READALL_CHUNKSIZE;

					// Ensure the buffer is big enough to fit all the data
					if (currentRead.buffer.Length < (currentRead.bytesDone + size))
					{
						currentRead.buffer.SetLength(currentRead.bytesDone + size);
					}
				}
				else if (currentRead.fixedLengthRead)
				{
					// We're reading a fixed amount of data, into a fixed size buffer
					// We'll read up to the chunksize amount

					// The read method is supposed to return smaller chunks as they become available.
					// However, it doesn't seem to always follow this rule in practice.
					// 
					// size = currentRead.buffer.Length - currentRead.bytesDone;

					int left = currentRead.buffer.Length - currentRead.bytesDone;
					size = Math.Min(left, READ_CHUNKSIZE);
				}
				else
				{
					// We're reading up to a termination sequence
					size = READ_CHUNKSIZE;

					// Ensure the buffer is big enough to fit all the data
					if (currentRead.buffer.Length < (currentRead.bytesDone + size))
					{
						currentRead.buffer.SetLength(currentRead.bytesDone + size);
					}
				}

				// The following should be spelled out:
				// If the stream can immediately complete the requested opertion, then
				// it may not fork off a background thread, meaning
				// that it's possible for the stream_DidRead method to get called before the
				// stream.BeginRead method returns below.

				stream.BeginRead(currentRead.buffer.ByteArray,      // buffer to read data into
				                 currentRead.bytesDone,             // buffer offset
				                 size,                              // max amout of data to read
				                 new AsyncCallback(stream_DidRead), // callback method
				                 currentRead);                      // callback info
			}
			catch (Exception e)
			{
				CloseWithException(e);
			}
		}
		
		/// <summary>
		/// Called after we've read data from the stream.
		/// We now call DoBytesAvailable, which will read and process further available data via the stream.
		/// 
		/// This method is thread safe.
		/// </summary>
		/// <param name="iar">AsyncState is AsyncReadPacket.</param>
		private void stream_DidRead(IAsyncResult iar)
		{
			lock (lockObj)
			{
				if (iar.AsyncState == currentRead)
				{
					try
					{
						currentRead.bytesProcessing = stream.EndRead(iar);

						if (currentRead.bytesProcessing > 0)
						{
							DoFinishRead();
						}
						else
						{
							CloseWithException(GetEndOfStreamException());
						}
					}
					catch (Exception e)
					{
						CloseWithException(e);
					}
				}
			}
		}

		/// <summary>
		/// Called after a read timeout timer fires.
		/// This will generally fire on an available thread from the thread pool.
		/// 
		/// This method is thread safe.
		/// </summary>
		/// <param name="state">state is AsyncReadPacket.</param>
		private void stream_DidNotRead(object state)
		{
			lock (lockObj)
			{
				if (state == currentRead)
				{
					EndCurrentRead();
					CloseWithException(GetReadTimeoutException());
				}
			}
		}

		/// <summary>
		/// This method is called when either:
		///  A) a new read is taken from the read queue
		///  B) or when data has just been read from the stream.
		/// 
		/// More specifically, it is called from either:
		///  A) DoReadOverflow()
		///  B) stream_DidRead()
		/// 
		/// The above methods are thread safe, so this method is inherently thread safe.
		/// It is not explicitly thread safe though, and should not be called outside thread safe methods.
		/// </summary>
		private void DoFinishRead()
		{
			Debug.Assert(currentRead != null);
			Debug.Assert(currentRead.bytesProcessing > 0);

			int totalBytesRead = 0;
			bool done = false;
			bool maxoutError = false;

			if(currentRead.readAllAvailableData)
			{
				// We're done because we read everything that was available (up to a max size).
				currentRead.bytesDone += currentRead.bytesProcessing;
				totalBytesRead = currentRead.bytesProcessing;
				currentRead.bytesProcessing = 0;

				done = true;
			}
			else if (currentRead.fixedLengthRead)
			{
				// We're reading up to a fixed size
				currentRead.bytesDone += currentRead.bytesProcessing;
				totalBytesRead = currentRead.bytesProcessing;
				currentRead.bytesProcessing = 0;

				done = currentRead.buffer.Length == currentRead.bytesDone;
			}
			else
			{
				// We're reading up to a terminator
				// So let's start searching for the termination sequence in the new data

				while (!done && !maxoutError && (currentRead.bytesProcessing > 0))
				{
					currentRead.bytesDone++;
					totalBytesRead++;
					currentRead.bytesProcessing--;

					bool match = currentRead.bytesDone >= currentRead.term.Length;
					int offset = currentRead.bytesDone - currentRead.term.Length;

					for (int i = 0; match && i < currentRead.term.Length; i++)
					{
						match = (currentRead.term[i] == currentRead.buffer[offset + i]);
					}
					done = match;

					if (!done && (currentRead.maxLength >= 0) && (currentRead.bytesDone >= currentRead.maxLength))
					{
						maxoutError = true;
					}
				}
			}

			// If there was any overflow data, extract it and save it.
			// This may occur if our read maxed out.
			// Or if we received Y bytes, but only needed X bytes to finish the read (X < Y).
			if (currentRead.bytesProcessing > 0)
			{
				readOverflow = new MutableData(currentRead.buffer, currentRead.bytesDone, currentRead.bytesProcessing);
				currentRead.bytesProcessing = 0;
			}

			if (done)
			{
				// Truncate any excess unused buffer space in the read packet
				currentRead.buffer.SetLength(currentRead.bytesDone);

				CompleteCurrentRead();
				ThreadPool.QueueUserWorkItem(new WaitCallback(MaybeDequeueRead));
			}
			else if (maxoutError)
			{
				CloseWithException(GetReadMaxedOutException());
			}
			else
			{
				// We're not done yet, but we have read in some bytes
				OnSocketDidReadPartial(totalBytesRead, currentRead.tag);

				// It appears that we've read all immediately available data on the socket
				// So begin asynchronously reading data again
				DoStartRead();
			}
		}

		/// <summary>
		/// Completes the current read by ending it, and then informing the delegate that it's complete.
		/// 
		/// This method is called from DoFinishRead, which is inherently thread safe.
		/// Therefore this method is also inherently thread safe.
		/// It is not explicitly thread safe though, and should not be called outside thread safe methods.
		/// </summary>
		private void CompleteCurrentRead()
		{
			// Save reference to currentRead
			AsyncReadPacket completedRead = currentRead;

			// End the current read (this will nullify the currentRead variable)
			EndCurrentRead();

			// Notify delegate if possible
			OnSocketDidRead(completedRead.buffer.ByteArray, completedRead.tag);
		}

		/// <summary>
		/// Ends the current read by disposing and nullifying the read timer,
		/// and then nullifying the current read.
		/// </summary>
		private void EndCurrentRead()
		{
			if (readTimer != null)
			{
				readTimer.Dispose();
				readTimer = null;
			}

			currentRead = null;
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////
		#endregion
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////
		#region Writing
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////

		/// <summary>
		/// Writes the specified data to the socket.
		/// </summary>
		/// <param name="data">
		///		The data to send.
		/// </param>
		/// <param name="timeout">
		///		Timeout in milliseconds. Specify a negative value if no timeout is desired.
		/// </param>
		/// <param name="tag">
		///		A tag that can be used to track the write.
		///		This tag will be returned in the callback methods.
		/// </param>
		public void Write(byte[] data, int timeout, long tag)
		{
			if ((data == null) || (data.Length == 0)) return;
			if ((flags & kForbidReadsWrites) > 0) return;

			// writeQueue is synchronized
			writeQueue.Enqueue(new AsyncWritePacket(data, 0, data.Length, timeout, tag));

			// Queue a call to MaybeDequeueWrite
			ThreadPool.QueueUserWorkItem(new WaitCallback(MaybeDequeueWrite));
		}

		/// <summary>
		/// Writes the specified data to the socket.
		/// </summary>
		/// <param name="data">
		///		The buffer that contains the data to write.
		/// </param>
		/// <param name="offset">
		///		The offset within the given data to start writing from.
		/// </param>
		/// <param name="length">
		///		The amount of data (in bytes) to write from the given data, starting from the given offset.
		/// </param>
		/// <param name="timeout">
		///		Timeout in milliseconds. Specify a negative value if no timeout is desired.
		/// </param>
		/// <param name="tag">
		///		A tag that can be used to track the write.
		///		This tag will be returned in the callback methods.
		///	</param>
		public void Write(byte[] data, int offset, int length, int timeout, long tag)
		{
			if ((data == null) || (data.Length == 0)) return;
			if ((flags & kForbidReadsWrites) > 0) return;

			// writeQueue is synchronized
			writeQueue.Enqueue(new AsyncWritePacket(data, offset, length, timeout, tag));

			// Queue a call to MaybeDequeueWrite
			ThreadPool.QueueUserWorkItem(new WaitCallback(MaybeDequeueWrite));
		}

		/// <summary>
		/// If possible, this method dequeues a write from the write queue and starts it.
		/// This is only possible if all of the following are true:
		///  1) any previous write has completed
		///  2) there's a write in the queue
		///  3) and the socket is connected.
		/// 
		/// This method is thread safe.
		/// </summary>
		private void MaybeDequeueWrite(object ignore)
		{
			lock (lockObj)
			{
				if ((flags & kClosed) > 0)
				{
					writeQueue.Clear();
					return;
				}

				if ((currentWrite == null) && (stream != null))
				{
					if ((flags & kPauseWrites) > 0)
					{
						// Don't do any reads yet.
						// We're waiting for TLS negotiation to start and/or finish.
					}
					else if (writeQueue.Count > 0)
					{
						// Get the next object in the read queue
						Object nextWrite = writeQueue.Dequeue();

						if (nextWrite is AsyncSpecialPacket)
						{
							// Next write packet is a special instruction packet.
							// Right now this can only mean a StartTLS instruction.
							AsyncSpecialPacket specialWrite = (AsyncSpecialPacket)nextWrite;

							// Update flags - this flag will be unset when TLS finishes
							flags |= kPauseWrites;

							// And attempt to start TLS
							// This method won't do anything unless both kPauseReads and kPauseWrites are set.
							MaybeStartTLS();
						}
						else
						{
							// Get the current write AsyncWritePacket
							currentWrite = (AsyncWritePacket)nextWrite;

							// Start time-out timer
							if (currentWrite.timeout >= 0)
							{
								writeTimer = new System.Threading.Timer(new TimerCallback(stream_DidNotWrite),
																		currentWrite,
																		currentWrite.timeout,
																		Timeout.Infinite);
							}

							try
							{
								DoSendBytes();
							}
							catch (Exception e)
							{
								CloseWithException(e);
							}
						}
					}
					else if ((flags & kCloseAfterWrites) > 0)
					{
						if ((flags & kCloseAfterReads) > 0)
						{
							if ((readQueue.Count == 0) && (currentRead == null))
							{
								Close(null);
							}
						}
						else
						{
							Close(null);
						}
					}
				}
			}
		}

		/// <summary>
		/// This method is called when either:
		///  A) a new write is taken from the write queue
		///  B) or when a previos write has finished.
		/// 
		/// More specifically, it is called from either:
		///  A) MaybeDequeueWrite()
		///  B) stream_DidWrite()
		/// 
		/// The above methods are thread safe, so this method is inherently thread safe.
		/// It is not explicitly thread safe though, and should not be called outside the above named methods.
		/// </summary>
		private void DoSendBytes()
		{
			int available = currentWrite.length - currentWrite.bytesDone;
			int size = (available < WRITE_CHUNKSIZE) ? available : WRITE_CHUNKSIZE;

			// The following should be spelled out:
			// If the stream can immediately complete the requested opertion, then
			// it may not fork off a background thread, meaning
			// that it's possible for the stream_DidWrite method to get called before the
			// stream.BeginWrite method returns below.

			currentWrite.bytesProcessing = size;
				
			stream.BeginWrite(currentWrite.buffer,                          // buffer to write from
			                  currentWrite.offset + currentWrite.bytesDone, // buffer offset
			                  size,                                         // amount of data to send
			                  new AsyncCallback(stream_DidWrite),           // callback method
			                  currentWrite);                                // callback info
		}

		/// <summary>
		/// Called when an asynchronous write has finished.
		/// This may just be a chunk of the data, and not the entire thing.
		/// 
		/// This method is thread safe.
		/// </summary>
		/// <param name="iar"></param>
		private void stream_DidWrite(IAsyncResult iar)
		{
			lock (lockObj)
			{
				if (iar.AsyncState == currentWrite)
				{
					try
					{
						// Note: EndWrite is void
						// Instead we must store and retrieve the amount of data we were trying to send
						stream.EndWrite(iar);
						currentWrite.bytesDone += currentWrite.bytesProcessing;

						if (currentWrite.bytesDone == currentWrite.length)
						{
							CompleteCurrentWrite();
							ThreadPool.QueueUserWorkItem(new WaitCallback(MaybeDequeueWrite));
						}
						else
						{
							// We're not done yet, but we have written out some bytes
							OnSocketDidWritePartial(currentWrite.bytesProcessing, currentWrite.tag);

							DoSendBytes();
						}
					}
					catch (Exception e)
					{
						CloseWithException(e);
					}
				}
			}
		}

		/// <summary>
		/// Called when a timeout occurs. (Called via thread timer).
		/// 
		/// This method is thread safe.
		/// </summary>
		/// <param name="state">
		/// 	The AsyncWritePacket that the timeout applies to.
		/// </param>
		private void stream_DidNotWrite(object state)
		{
			lock (lockObj)
			{
				if (state == currentWrite)
				{
					EndCurrentWrite();
					CloseWithException(GetWriteTimeoutException());
				}
			}
		}

		/// <summary>
		/// Completes the current write by ending it, and then informing the delegate that it's complete.
		/// 
		/// This method is called from stream_DidWrite, which is thread safe.
		/// Therefore this method is inherently thread safe.
		/// It is not explicitly thread safe though, and should not be called outside thread safe methods.
		/// </summary>
		private void CompleteCurrentWrite()
		{
			// Save reference to currentRead
			AsyncWritePacket completedWrite = currentWrite;

			// End the current write (this will nullify the currentWrite variable)
			EndCurrentWrite();

			// Notify delegate if possible
			OnSocketDidWrite(completedWrite.tag);
		}

		public void EndCurrentWrite()
		{
			if (writeTimer != null)
			{
				writeTimer.Dispose();
				writeTimer = null;
			}
			
			currentWrite = null;
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////
		#endregion
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////
		#region Static Methods
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////

		public static byte[] CRLFData
		{
			get { return Encoding.UTF8.GetBytes("\r\n"); }
		}

		public static byte[] CRData
		{
			get { return Encoding.UTF8.GetBytes("\r"); }
		}

		public static byte[] LFData
		{
			get { return Encoding.UTF8.GetBytes("\n"); }
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////
		#endregion
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////
	}
}
