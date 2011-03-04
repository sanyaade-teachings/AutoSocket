/*
 *			Automatic Sockets
 *			Jerry Lee Williams Jr
 *			04 March 2011
 *	Use:
 *	Create the AutoSocket.
 *	Use Connect() to connect to the server.
 *	Set up your callback in DataRecv.
 *	Use Send() to send bytes.
 *	Send(string) encodes the string in UTF-8 to send.
 *	There is no Recv() because you will be notified.
 *	Use Dispose() or Close() to close.
 *	You can check the connection with Connected.
 *
 */

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

class AutoSocketDataEventArgs: EventArgs {
	public byte[]	Bytes;
	public int	Size;
	public AutoSocketDataEventArgs(byte[] bytes, int size) {
		this.Bytes = bytes;
		this.Size = size;
	}
}
delegate void AutoSocketDataEvent(object sender, AutoSocketDataEventArgs e);

class AutoSocket: IDisposable {
	public event AutoSocketDataEvent DataRecv;
	public Socket Socket;
	public bool IsConnected {
		get { return this.Socket.Connected; }
	}
	byte[]	buffer;
	
	public bool Connect(string server, int port) {
		IPHostEntry	host = Dns.GetHostEntry(server);
		foreach (IPAddress addr in host.AddressList) {
			IPEndPoint ipe = new IPEndPoint(addr, port);
			this.Socket = new Socket(ipe.AddressFamily,
				SocketType.Stream,
				ProtocolType.Tcp);
			this.Socket.Connect(ipe);
			if (this.Socket.Connected)
				break;
		}
		if (!this.Socket.Connected)
			return false;
		this.buffer = new byte[65536];
		this.Socket.BeginReceive(this.buffer,
			0, this.buffer.Length,
			SocketFlags.None,
			new AsyncCallback(this.OnRecv),
			this.Socket);
		return true;
	}
	
	public void Close() {
		this.Socket.Close();
		this.buffer = null;
	}
	
	public void Dispose() {
		this.Close();
	}
	
	public void Send(byte[] bytes) {
		this.Socket.BeginSend(bytes, 0, bytes.Length,
			SocketFlags.None,
			new AsyncCallback(this.OnSend),
			this.Socket);
	}
	
	public void Send(string data) {
		this.Send(Encoding.UTF8.GetBytes(data));
	}
	
	protected void OnRecv(IAsyncResult iar) {
		int sz = this.Socket.EndReceive(iar);
		if (this.DataRecv != null)
			this.DataRecv(this,
				new AutoSocketDataEventArgs(this.buffer,sz));
		this.Socket.BeginReceive(this.buffer,
			0, this.buffer.Length,
			SocketFlags.None,
			new AsyncCallback(this.OnRecv),
			this.Socket);
	}
	protected void OnSend(IAsyncResult iar) {
		int sz = this.Socket.EndSend(iar);
	}
}