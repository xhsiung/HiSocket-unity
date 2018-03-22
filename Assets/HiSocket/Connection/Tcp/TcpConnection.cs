﻿//****************************************************************************
// Description:
// Author: hiramtan@live.com
//****************************************************************************
using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using System.Threading;

namespace HiSocket
{
    public class TcpConnection : Connection, ITcp
    {
        private readonly IByteArray _iByteArrayReceive = new ByteArray();
        private readonly IByteArray _iByteArraySend = new ByteArray();
        private readonly IPackage _iPackage;
        private int _timeOut = 5000;
        private string ip = "127.0.0.1";
        private int port = 1234;

        public static byte CONNECT = 0x00;
        public static byte DISCONNECT = 0x01;
        public static byte SUBSCIRUBE = 0x02;
        public static byte UNSUBSCIRUBE = 0x03;
        public static byte SEND = 0x04;

        public int delay = 200;
        public byte[] ID = {0x00,0x00};

        public static TcpConnection instance = null;
        public static TcpConnection getInstance(){
            if ( instance == null){                 
                instance = new TcpConnection( new Packer() );
                return instance;
            }
            return instance;
        }

        ~TcpConnection(){
            _socket.Disconnect(true);
            instance = null;
        }

        public int TimeOut
        {
            get { return _timeOut; }
            set { _timeOut = value; }
        }

        public bool IsConnected
        {
            get { return _socket != null && _socket.Connected; }
        }

        public  event Action<SocketState> StateChangeEvent;
        public  event Action<byte[]> ReceiveEvent;

        public TcpConnection(IPackage iPackage)
        {
            _iPackage = iPackage;
        }

        public override void Connect(string ip, int port )
        {
            this.ip = ip;
            this.port = port;
            if ( IsConnected )
            {
                Debug.Log("already connected");
                return;
            }
            ChangeState(SocketState.Connecting);
            var address = Dns.GetHostAddresses(ip)[0];
            IPEndPoint iep = new IPEndPoint(address, port);
            _socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _socket.NoDelay = true;
            _socket.SendTimeout = _socket.ReceiveTimeout = TimeOut;
  
            try
            {
                _socket.BeginConnect(iep, delegate (IAsyncResult ar)
                {
                    var tcp = ar.AsyncState as Socket;
                    if (tcp != null && tcp.Connected)
                    {
                        tcp.EndConnect(ar);
                        ChangeState(SocketState.Connected);
                        InitThread();
                    }
                    else
                    {
                        ChangeState(SocketState.DisConnected);
                        throw new Exception("socket is null or isconnected is false");
                    }
                }, _socket);
            }
            catch (Exception e)
            {
                ChangeState(SocketState.DisConnected);
                throw new Exception(e.ToString());
            }
        }

        public void Run()
        {
                while (_receiveQueue.Count > 0)
                {                 
                    if (ReceiveEvent != null)
                    {
                        ReceiveEvent(_receiveQueue.Dequeue());
                    }
                }

                //detect
                if (IsDisConnected())
                {
                    Debug.Log("not connect");
                    ChangeState(SocketState.DisConnected);
                    Reconnected();
                }
        }

        public void SubScribe(ref TcpConnection conn,byte chann){
            byte[] xdata = HIUtils.JoinHeaderBytes( TcpConnection.SUBSCIRUBE, chann, this.ID, new byte[]{0x00}) ;
            conn.Send(xdata);
        }
         
        public void UnSubscribe(ref TcpConnection conn,byte chann){
            byte[] xdata = HIUtils.JoinHeaderBytes( TcpConnection.UNSUBSCIRUBE, chann, new byte[]{0x00,0x00}, new byte[] { 0x00 });
            conn.Send(xdata);
        }

        public void HISend(ref TcpConnection conn,byte chann, byte[] data){
            byte[] xdata = HIUtils.JoinHeaderBytes( TcpConnection.SEND, chann, this.ID, data );
            conn.Send(xdata);
        }

        //alex add detect Connections
        public bool IsDisConnected()
        {
            bool part1 = false;
            bool part2 = false;
            try
            {
                part1 = _socket.Poll(3000, SelectMode.SelectRead);
                part2 = (_socket.Available == 0);
                return ((part1 && part2) || !_socket.Connected) ? true : false;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public void Reconnected(){
            Debug.Log("Reconnected");
            _socket.Disconnect(true);
            _socket = null;
            Connect(this.ip, this.port);
        }

        protected override void Send()
        {
            while (_isSendThreadOn)
            {
                if (!IsConnected) //连接断开
                {
                    throw new Exception("from send: disconnected");
                }
                if (_sendQueue.Count > 0)
                {
                    try
                    {
                        lock (_sendQueue)
                        {
                            _iPackage.Pack(_sendQueue, _iByteArraySend);
                        }
                    }
                    catch (Exception e)
                    {
                        throw new Exception("pack error: " + e);
                    }
                    var toSend = _iByteArraySend.Read(_iByteArraySend.Length);
                    try
                    {
                        _socket.BeginSend(toSend, 0, toSend.Length, SocketFlags.None, delegate (IAsyncResult ar)
                        {
                            var tcp = ar.AsyncState as Socket;
                            var sendLength = tcp.EndSend(ar);
                            if (sendLength != toSend.Length)
                            {
                                //todo: if this will happend, msdn is not handle this issue
                                throw new Exception("can not send whole bytes at one time");
                            }
                        }, _socket);
                    }
                    catch (Exception e)
                    {
                        throw new Exception(e.ToString());
                    }

                }
            }
        }


        protected override void Receive()
        {
            while (_isReceiveThreadOn)
            {
                if (!IsConnected)
                {
                    throw new Exception("from receive: disconnected");
                }
                if (_socket.Available > 0)
                {
                    try
                    {
                        _socket.BeginReceive(ReceiveBuffer, 0, ReceiveBuffer.Length, SocketFlags.None, delegate (IAsyncResult ar)
                        {
                            var tcp = ar.AsyncState as Socket;
                            int length = tcp.EndReceive(ar);
                            if (length > 0)
                            {
                                _iByteArrayReceive.Write(ReceiveBuffer, length);
                                try
                                {
                                    lock (_receiveQueue)
                                    {
                                        _iPackage.Unpack(_iByteArrayReceive, _receiveQueue);
                                    }
                                }
                                catch (Exception e)
                                {
                                    throw new Exception(e.ToString());
                                }
                            }
                        }, _socket);
                    }
                    catch (Exception e)
                    {
                        throw new Exception(e.ToString());
                    }
                }
            }
        }
        void ChangeState(SocketState state)
        {
            if (StateChangeEvent != null)
                StateChangeEvent(state);
        }
    }
}


//#region MainThread//havent finish
////#define MainThread
//#if MainThread
//using System;
//using System.Net.Sockets;

//namespace HiSocket
//{
//    public class TcpConnection : Connection
//    {
//        private TcpClient _client;
//        protected readonly IByteArray _iByteArrayReceive = new ByteArray();
//        protected readonly IByteArray _iByteArraySend = new ByteArray();
//        protected readonly IPackage _iPackage;

//        public override int TimeOut
//        {
//            get { return _timeOut; }
//            set
//            {
//                _timeOut = value;
//                _client.SendTimeout = _client.ReceiveTimeout = TimeOut;
//            }
//        }

//        public override bool IsConnected
//        {
//            get { return _client != null && _client.Client != null && _client.Connected; }
//        }
//        public TcpConnection(IPackage iPackage)
//        {
//            _client = new TcpClient();
//            _client.NoDelay = true;
//            _client.SendTimeout = _client.ReceiveTimeout = TimeOut;
//            _iPackage = iPackage;
//        }
//        public override void Connect(string ip, int port)
//        {
//            ChangeState(SocketState.Connecting);
//            if (IsConnected)
//            {
//                ChangeState(SocketState.Connected);
//                Console.WriteLine("already connected");
//                return;
//            }
//            try
//            {
//                _client.BeginConnect(ip, port, delegate (IAsyncResult ar)
//                {
//                    var tcp = ar.AsyncState as TcpClient;
//                    tcp.Client.EndConnect(ar);
//                    if (tcp.Connected)
//                    {
//                        ChangeState(SocketState.Connected);
//                        tcp.Client.BeginReceive(ReceiveBuffer, 0, ReceiveBuffer.Length, SocketFlags.None, Receive, tcp);
//                    }
//                    else
//                    {
//                        ChangeState(SocketState.DisConnected);
//                    }
//                }, _client);
//            }
//            catch (Exception e)
//            {
//                ChangeState(SocketState.DisConnected);
//                throw new Exception(e.ToString());
//            }
//        }

//        public override void Send(byte[] bytes)
//        {
//            if (!IsConnected)
//            {
//                ChangeState(SocketState.DisConnected);
//                throw new Exception("from send: disconnected");
//            }
//            if (_iByteArrayReceive.Length != 0)
//            {
//                throw new Exception("from send: send queue still have last time data");
//            }
//            _iByteArraySend.Write(bytes, bytes.Length);
//            try
//            {
//                _iPackage.Pack(_iByteArraySend);
//            }
//            catch (Exception e)
//            {
//                throw new Exception(e.ToString());
//            }
//            var toSend = _iByteArraySend.Read(_iByteArraySend.Length);
//            try
//            {
//                _client.Client.BeginSend(toSend, 0, toSend.Length, SocketFlags.None, delegate (IAsyncResult ar)
//                {
//                    var tcp = ar.AsyncState as TcpClient;
//                    tcp.Client.EndSend(ar);
//                }, _client);
//            }
//            catch (Exception e)
//            {
//                throw new Exception(e.ToString());
//            }
//        }
//        public override void DisConnect()
//        {
//            if (IsConnected)
//            {
//                _client.Client.Shutdown(SocketShutdown.Both);
//                _client.Close();
//                _client = null;
//            }
//            ChangeState(SocketState.DisConnected);
//            StateChangeHandler = null;
//        }
//        private void Receive(IAsyncResult ar)
//        {
//            if (!IsConnected)
//            {
//                ChangeState(SocketState.DisConnected);
//                throw new Exception("from receive: disconnected");
//            }
//            var tcp = ar.AsyncState as TcpClient;
//            var length = tcp.Client.EndReceive(ar);
//            if (length > 0)
//            {
//                _iByteArrayReceive.Write(ReceiveBuffer, length);
//                try
//                {
//                    _iPackage.Unpack(_iByteArrayReceive);
//                }
//                catch (Exception e)
//                {
//                    throw new Exception(e.ToString());
//                }
//            }
//            try
//            {
//                tcp.Client.BeginReceive(ReceiveBuffer, 0, ReceiveBuffer.Length, SocketFlags.None, Receive, tcp);
//            }
//            catch (Exception e)
//            {
//                throw new Exception(e.ToString());
//            }
//        }
//    }
//}
//#endif
//#endregion

//#region SingleThread//havent finish
////#define SingleThread
//#if SingleThread
//using HiSocket.Msg;
//using System;
//using System.Net;
//using System.Net.Sockets;
//using System.Threading;

//namespace HiSocket.TcpConnection
//{
//    // State object for receiving data from remote device.  
//    public class TcpConnection : ITcp
//    {
//        public int TimeOut
//        {
//            private get { return _timeOut; }
//            set { _timeOut = value; }
//        }

//        public int ReceiveBufferSize
//        {
//            private get { return _receiveBufferSize; }
//            set
//            {
//                _receiveBufferSize = value;
//                _receiveBuffer = new byte[ReceiveBufferSize];
//            }
//        }

//        public Action<SocketState> StateChangeHandler { get; set; }
//        public bool IsConnected { get { return _client != null && _client.Client != null && _client.Connected; } }

//        private string _ip;
//        private int _port;
//        private IPackage _iPackage;
//        private System.Net.Sockets.TcpConnection _client;
//        private int _receiveBufferSize = 1024 * 128;//128k
//        private byte[] _receiveBuffer;
//        private int _timeOut = 5000;//5s:收发超时时间
//        private readonly IByteArray _iByteArraySend = new ByteArray();
//        private readonly IByteArray _iByteArrayReceive = new ByteArray();


//        private static ManualResetEvent connectDone =
//            new ManualResetEvent(false);
//        private static ManualResetEvent sendDone =
//            new ManualResetEvent(false);
//        private static ManualResetEvent receiveDone =
//            new ManualResetEvent(false);
//        public TcpConnection(IPackage iPackage)
//        {
//            _receiveBuffer = new byte[ReceiveBufferSize];
//            this._iPackage = iPackage;
//            _client = new System.Net.Sockets.TcpConnection();
//            _client.NoDelay = true;
//            _client.SendTimeout = _client.ReceiveTimeout = TimeOut;
//        }

//        public void Connect(string ip, int port)
//        {
//            ChangeState(SocketState.Connecting);
//            if (IsConnected)
//            {
//                ChangeState(SocketState.Connected);
//                return;
//            }
//            try
//            {
//                _client.BeginConnect(ip, port,
//                    new AsyncCallback((x) =>
//                    {
//                        System.Net.Sockets.TcpConnection client = x.AsyncState as System.Net.Sockets.TcpConnection;
//                        client.EndConnect(x);
//                        connectDone.Set();
//                        _client.Client.BeginReceive(_receiveBuffer, 0, _receiveBuffer.Length, SocketFlags.None, Receive, _client);
//                        ChangeState(SocketState.Connected);
//                    }), _client);
//                connectDone.WaitOne();
//            }
//            catch (Exception e)
//            {
//                throw new Exception(e.ToString());
//            }
//        }

//        private void Receive(IAsyncResult ar)
//        {
//            receiveDone.WaitOne();
//            System.Net.Sockets.TcpConnection state = ar.AsyncState as System.Net.Sockets.TcpConnection;
//            int bytesRead = state.Client.EndReceive(ar);
//            if (bytesRead > 0)
//            {
//                _client.Client.BeginReceive(_receiveBuffer, 0, _receiveBuffer.Length, SocketFlags.None, Receive, _client);
//            }
//            receiveDone.Set();
//        }

//        private void ChangeState(SocketState state)
//        {
//            if (StateChangeHandler != null)
//            {
//                StateChangeHandler(state);
//            }
//        }
//        public void DisConnect()
//        {

//        }

//        public void Send(byte[] bytes)
//        {
//            try
//            {
//                _client.Client.BeginSend(bytes, 0, bytes.Length, SocketFlags.None,
//                    new AsyncCallback((x) =>
//                    {
//                        System.Net.Sockets.TcpConnection client = x.AsyncState as System.Net.Sockets.TcpConnection;
//                        client.Client.EndSend(x);
//                        sendDone.Set();
//                    }), _client);
//                sendDone.WaitOne();
//            }
//            catch (Exception e)
//            {
//                throw new Exception(e.ToString());
//            }
//        }

//        public long Ping()
//        {
//            IPAddress ipAddress = IPAddress.Parse(_ip);
//            System.Net.NetworkInformation.Ping tempPing = new System.Net.NetworkInformation.Ping();
//            System.Net.NetworkInformation.PingReply temPingReply = tempPing.Send(ipAddress);
//            return temPingReply.RoundtripTime;
//        }
//    }

////    using System;  
////    using System.Net;  
////    using System.Net.Sockets;  
////    using System.Threading;  
////    using System.Text;  

////// State object for receiving data from remote device.  
////    public class StateObject
////    {
////        // Client socket.  
////        public Socket workSocket = null;
////        // Size of receive buffer.  
////        public const int BufferSize = 256;
////        // Receive buffer.  
////        public byte[] buffer = new byte[BufferSize];
////        // Received data string.  
////        public StringBuilder sb = new StringBuilder();
////    }

////    public class AsynchronousClient
////    {
////        // The port number for the remote device.  
////        private const int port = 11000;

////        // ManualResetEvent instances signal completion.  
////        private static ManualResetEvent connectDone =
////            new ManualResetEvent(false);
////        private static ManualResetEvent sendDone =
////            new ManualResetEvent(false);
////        private static ManualResetEvent receiveDone =
////            new ManualResetEvent(false);

////        // The response from the remote device.  
////        private static String response = String.Empty;

////        private static void StartClient()
////        {
////            // Connect to a remote device.  
////            try
////            {
////                // Establish the remote endpoint for the socket.  
////                // The name of the   
////                // remote device is "host.contoso.com".  
////                IPHostEntry ipHostInfo = Dns.Resolve("host.contoso.com");
////                IPAddress ipAddress = ipHostInfo.AddressList[0];
////                IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);

////                // Create a TCP/IP socket.  
////                Socket client = new Socket(AddressFamily.InterNetwork,
////                    SocketType.Stream, ProtocolType.TcpConnection);

////                // Connect to the remote endpoint.  
////                client.BeginConnect(remoteEP,
////                    new AsyncCallback(ConnectCallback), client);
////                connectDone.WaitOne();

////                // Send test data to the remote device.  
////                Send(client, "This is a test<EOF>");
////                sendDone.WaitOne();

////                // Receive the response from the remote device.  
////                Receive(client);
////                receiveDone.WaitOne();

////                // Write the response to the console.  
////                Console.WriteLine("Response received : {0}", response);

////                // Release the socket.  
////                client.Shutdown(SocketShutdown.Both);
////                client.Close();

////            }
////            catch (Exception e)
////            {
////                Console.WriteLine(e.ToString());
////            }
////        }

////        private static void ConnectCallback(IAsyncResult ar)
////        {
////            try
////            {
////                // Retrieve the socket from the state object.  
////                Socket client = (Socket)ar.AsyncState;

////                // Complete the connection.  
////                client.EndConnect(ar);

////                Console.WriteLine("Socket connected to {0}",
////                    client.RemoteEndPoint.ToString());

////                // Signal that the connection has been made.  
////                connectDone.Set();
////            }
////            catch (Exception e)
////            {
////                Console.WriteLine(e.ToString());
////            }
////        }

////        private static void Receive(Socket client)
////        {
////            try
////            {
////                // Create the state object.  
////                StateObject state = new StateObject();
////                state.workSocket = client;

////                // Begin receiving the data from the remote device.  
////                client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
////                    new AsyncCallback(ReceiveCallback), state);
////            }
////            catch (Exception e)
////            {
////                Console.WriteLine(e.ToString());
////            }
////        }

////        private static void ReceiveCallback(IAsyncResult ar)
////        {
////            try
////            {
////                // Retrieve the state object and the client socket   
////                // from the asynchronous state object.  
////                StateObject state = (StateObject)ar.AsyncState;
////                Socket client = state.workSocket;

////                // Read data from the remote device.  
////                int bytesRead = client.EndReceive(ar);

////                if (bytesRead > 0)
////                {
////                    // There might be more data, so store the data received so far.  
////                    state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));

////                    // Get the rest of the data.  
////                    client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
////                        new AsyncCallback(ReceiveCallback), state);
////                }
////                else
////                {
////                    // All the data has arrived; put it in response.  
////                    if (state.sb.Length > 1)
////                    {
////                        response = state.sb.ToString();
////                    }
////                    // Signal that all bytes have been received.  
////                    receiveDone.Set();
////                }
////            }
////            catch (Exception e)
////            {
////                Console.WriteLine(e.ToString());
////            }
////        }

////        private static void Send(Socket client, String data)
////        {
////            // Convert the string data to byte data using ASCII encoding.  
////            byte[] byteData = Encoding.ASCII.GetBytes(data);

////            // Begin sending the data to the remote device.  
////            client.BeginSend(byteData, 0, byteData.Length, 0,
////                new AsyncCallback(SendCallback), client);
////        }

////        private static void SendCallback(IAsyncResult ar)
////        {
////            try
////            {
////                // Retrieve the socket from the state object.  
////                Socket client = (Socket)ar.AsyncState;

////                // Complete sending the data to the remote device.  
////                int bytesSent = client.EndSend(ar);
////                Console.WriteLine("Sent {0} bytes to server.", bytesSent);

////                // Signal that all bytes have been sent.  
////                sendDone.Set();
////            }
////            catch (Exception e)
////            {
////                Console.WriteLine(e.ToString());
////            }
////        }

////        public static int Main(String[] args)
////        {
////            StartClient();
////            return 0;
////        }
////    }
//}
//#endif
//#endregion