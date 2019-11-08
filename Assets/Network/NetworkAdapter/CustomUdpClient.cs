

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

using UnityEngine;

public class CustomUdpClient : SocketClient, IDisposable
{
    private int port = 0;
    private string host = "";
    private byte packId = 0;
    private Socket socket = null;

    private readonly MemoryStream encodeMs;
    private readonly BinaryWriter encodeBw;

    private readonly NetEnDecode netEnDecode = null;
    private readonly NetRespEvent onNetRespEvent = null;

    private readonly ushort maxPackSize = 8192;
    private SocketAsyncEventArgs recvEventArgs;
    private SocketAsyncEventArgs sendEventArgs;
    private readonly object lockObject = new object();
    private readonly Queue<byte[]> sendQueue = new Queue<byte[]>();

    public int Id
    {
        get { return GetHashCode(); }
    }

    private byte PackId
    {
        get { return packId++; }
    }

    private void PackIdReset()
    {
        packId = 0;
    }

    public string Name
    {
        get { return this.GetType().Name; }
    }

    public bool IsConnected()
    {
        return true;
    }


    public CustomUdpClient(NetRespEvent respEvent, NetEnDecode enDecode)
    {
        onNetRespEvent = respEvent;
        netEnDecode = enDecode;
        encodeMs = new MemoryStream();
        encodeBw = new BinaryWriter(encodeMs);
    }

    public void ReConnect()
    {
        //由系统分配本地IP和port,接收端能获到这些信息
        var localEndPoint = new IPEndPoint(IPAddress.Any, 0);
        
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        socket.Bind(localEndPoint);

        var remoteEndPoint = new IPEndPoint(IPAddress.Parse(host), port);

        recvEventArgs.RemoteEndPoint = remoteEndPoint;
        sendEventArgs.RemoteEndPoint = remoteEndPoint;

        ReceiveFromAsync();
    }

    public void Connect(string host, int port)
    {
        //由系统分配本地IP和port,接收端能获到这些信息
        var localEndPoint = new IPEndPoint(IPAddress.Any, 0);
        var remoteEndPoint = new IPEndPoint(IPAddress.Parse(host), port);

        socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        socket.Bind(localEndPoint);

        recvEventArgs = new SocketAsyncEventArgs();
        recvEventArgs.RemoteEndPoint = remoteEndPoint;
        recvEventArgs.SetBuffer(new byte[maxPackSize], 0, maxPackSize);
        recvEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(IoCompleted);

        sendEventArgs = new SocketAsyncEventArgs();
        sendEventArgs.RemoteEndPoint = remoteEndPoint;
        sendEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(IoCompleted);

        ReceiveFromAsync();
    }

    public void Dispose()
    {
        Close("Dispose");
    }

    public bool Close(string reason)
    {
        if (socket == null)
        {
            return false;
        }
        //if (socket.Connected)
        {
            socket.Close();
        }
        socket = null;
        onNetRespEvent(NetMsg.Create(NetMsgType.Disconnect, reason));
        return true;
    }

    public bool SendMessage(UInt32 cmd, object data)
    {
        encodeMs.Seek(0, SeekOrigin.Begin);
        netEnDecode.Encode(encodeBw, cmd, PackId, data);
        return SendMessage(encodeMs.ToArray());
    }

    public byte[] NetEncode(UInt32 cmd, object data)
    {
        encodeMs.Seek(0, SeekOrigin.Begin);
        netEnDecode.Encode(encodeBw, cmd, PackId, data);
        return encodeMs.ToArray();
    }

    public bool SendMessage(byte[] message)
    {
        lock (lockObject)
        {
            if (sendQueue.Count > 0)
            {
                sendQueue.Enqueue(message);
                return true;
            }
            sendEventArgs.SetBuffer(message, 0, message.Length);
            if (!socket.SendToAsync(sendEventArgs))
            {
                ProcessSendCompleted(sendEventArgs);
            }
            return true;
        } 
    }

    private void ReceiveFromAsync()
    {
        if (socket == null) return;
        recvEventArgs.SetBuffer(0, maxPackSize);
        if (!socket.ReceiveFromAsync(recvEventArgs))
        {
            IoCompleted(socket, recvEventArgs);
        }
    }

    private void IoCompleted(object sender, SocketAsyncEventArgs e)
    {
        switch (e.LastOperation)
        {
            case SocketAsyncOperation.ReceiveFrom:
                ProcessReceiveCompleted(e);
                break;
            case SocketAsyncOperation.SendTo:
                ProcessSendCompleted(e);
                break;
        }
    }

    private void ProcessReceiveCompleted(SocketAsyncEventArgs e)
    {
        if (e.BytesTransferred == 0 || e.SocketError != SocketError.Success)
        {
            Close("BytesTransferred == 0 " + e.SocketError);
            return;
        }

        try
        {
            if (onNetRespEvent != null)
            {
                var socketEvent = new NetMsg();
                socketEvent.MsgType = NetMsgType.Message;
                socketEvent.Data = new byte[e.BytesTransferred - e.Offset];
                Array.Copy(e.Buffer, e.Offset, socketEvent.Data, 0, e.BytesTransferred);

                onNetRespEvent(socketEvent);
            }
        }
        catch (Exception ex)
        {
            Close("Call ReceiveCompletedEvent:" + ex.Message);
        }

        ReceiveFromAsync();
    }

    private void ProcessSendCompleted(SocketAsyncEventArgs e)
    {
        if (e.SocketError != SocketError.Success)
        {
            Close("SendToAsync SocketError:" + e.SocketError);
            //var netMsg = NetMsg.Create(NetMsgType.Disconnect);
            //var str = "SendToAsync SocketError:" + e.SocketError;
            //netMsg.Data = Encoding.UTF8.GetBytes(str);
            //onNetRespEvent(netMsg);
            //Odin.Log.Error(string.Concat("SendToAsync SocketError:", e.SocketError.ToString()));
        }

        lock (lockObject)
        {
            if (sendQueue.Count > 0)
            {
                var message = sendQueue.Dequeue();
                sendEventArgs.SetBuffer(message, 0, message.Length);
                if (!socket.SendToAsync(sendEventArgs))
                {
                    ProcessSendCompleted(sendEventArgs);
                }
            }
        }
    }

}

