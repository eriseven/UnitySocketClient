

//using System;
//using System.Collections.Generic;
//using System.Net;
//using System.Net.Sockets;
//using System.Text;
//using UnityEngine;

//namespace MMOGame
//{
//    public class UdpSocketClient : SocketClient
//    {
//        private string host;
//        private ushort maxPackSize;
//        private Socket clientSocket = null;
//        //private MsgEvent onMsgEvent = null;
//        private SocketAsyncEventArgs recvEventArgs;
//        private SocketAsyncEventArgs sendEventArgs;
//        private readonly object lockObject = new object();
//        private readonly Queue<byte[]> sendQueue = new Queue<byte[]>();

//        public void SendConnect(string host)
//        {
//            this.host = host;
//            SendConnect(host, 80);
//        }

//        public void SendConnect(string host, int port, ushort maxPackSize = 8092)
//        {
//            this.host = host;
//            //由系统分配本地IP和port,接收端能获到这些信息
//            var localEndPoint = new IPEndPoint(IPAddress.Any, 0);
//            var remoteEndPoint = new IPEndPoint(IPAddress.Parse(host), port);

//            this.maxPackSize = maxPackSize < (ushort)2048 ? (ushort)2048 : maxPackSize;

//            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
//            clientSocket.Bind(localEndPoint);

//            recvEventArgs = new SocketAsyncEventArgs();
//            recvEventArgs.RemoteEndPoint = remoteEndPoint;
//            recvEventArgs.SetBuffer(new byte[this.maxPackSize], 0, this.maxPackSize);
//            recvEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(IoCompleted);

//            sendEventArgs = new SocketAsyncEventArgs();
//            sendEventArgs.RemoteEndPoint = remoteEndPoint;
//            sendEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(IoCompleted);

//            ReceiveFromAsync();
//        }

//        public bool Close()
//        {
//            if (clientSocket != null)
//            {
//                clientSocket.Close();
//                clientSocket = null;
//            }
//            return true;
//        }

//        public void SendMessage(byte[] message)
//        {
//            SendMessage(message, null);
//        }

//        public void SendMessage(byte[] message, Dictionary<string, string> requestHeader)
//        {
//            lock (lockObject)
//            {
//                if (sendQueue.Count>0)
//                {
//                    lock (lockObject)
//                    {
//                        sendQueue.Enqueue(message);
//                    }
//                    return;
//                }
//            }
            
//            sendEventArgs.SetBuffer(message, 0, message.Length);
//            if (!clientSocket.SendToAsync(sendEventArgs))
//            {
//                ProcessSendCompleted(sendEventArgs);
//            }
//        }

//        public void SendMessage(string message)
//        {
//            SendMessage(message, null);
//        }

//        public void SendMessage(string message, Dictionary<string, string> requestHeader)
//        {
//            var bytes = Encoding.UTF8.GetBytes(message);
//            lock (lockObject)
//            {
//                if (sendQueue.Count > 0)
//                {
//                    lock (lockObject)
//                    {
//                        sendQueue.Enqueue(bytes);
//                    }
//                    return;
//                }
//            }
//            sendEventArgs.SetBuffer(bytes, 0, bytes.Length);
//            if (!clientSocket.SendToAsync(sendEventArgs))
//            {
//                ProcessSendCompleted(sendEventArgs);
//            }
//        }

        
//        private void ReceiveFromAsync()
//        {
//            recvEventArgs.SetBuffer(0, maxPackSize);
//            if (!clientSocket.ReceiveFromAsync(recvEventArgs))
//            {
//                IoCompleted(clientSocket, recvEventArgs);
//            }
//        }

//        private void IoCompleted(object sender, SocketAsyncEventArgs e)
//        {
//            switch (e.LastOperation)
//            {
//                case SocketAsyncOperation.ReceiveFrom:
//                    ProcessReceiveCompleted(e);
//                    break;
//                case SocketAsyncOperation.SendTo:
//                    ProcessSendCompleted(e);
//                    break;
//            }
//        }

//        private void ProcessReceiveCompleted(SocketAsyncEventArgs e)
//        {
//            if (e.BytesTransferred == 0 || e.SocketError != SocketError.Success)
//            {
//               Odin.Log.Error(string.Concat("Recv Bytes:", e.BytesTransferred.ToString(), " SocketError:",
//                    e.SocketError.ToString()));
//                return;
//            }

//            try
//            {
//                if (onMsgEvent != null)
//                {
//                    var socketEvent = new NetRespEvent();
//                    socketEvent.socket = this;
//                    socketEvent.eventType = NetRespEvent.EventType.BinMessage;

//                    socketEvent.rawData = new byte[e.BytesTransferred - e.Offset];
//                    Array.Copy(e.Buffer, e.Offset, socketEvent.rawData, 0, e.BytesTransferred);

//                    onMsgEvent(socketEvent);
//                }
//            }
//            catch (Exception exception)
//            {
//                Odin.Log.Error(string.Concat("Call ReceiveCompletedEvent:", exception.Message));
//            }

//            ReceiveFromAsync();
//        }

//        private void ProcessSendCompleted(SocketAsyncEventArgs e)
//        {
//            if (e.SocketError != SocketError.Success)
//            {
//                Odin.Log.Error(string.Concat("SendToAsync SocketError:", e.SocketError.ToString()));
//            }

//            lock (lockObject)
//            {
//                if (sendQueue.Count > 0)
//                {
//                    SendMessage(sendQueue.Dequeue());
//                }
//            }
//        }

//        public int Id
//        {
//            get { return GetHashCode(); }
//        }

//        public string Name
//        {
//            get { return "Udp"; }
//        }
//        public string Host
//        {
//            get { return host; }
//        }
//        //public MsgEvent OnMsgEvent
//        //{
//        //    get { return onMsgEvent; }
//        //    set { onMsgEvent = value; }
//        //}

//        public bool IsConnected()
//        {
//            return true;
//        }
//    }
//}
