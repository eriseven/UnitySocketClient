

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Timers;
//using proto.cmd;
using UnityEngine;

public class CustomTcpClient : SocketClient, IDisposable
{

    class MsgInfo
    {
        public UInt32 Cmd;
        public object Data;
    }

    private int _port = 0;
    private string _host = "";
    private byte _packId = 0;

    private Socket _socket = null;

    private Timer _timer = null;
    private const int Timeout = 3000;

    private readonly MemoryStream _encodeMs;
    private readonly BinaryWriter _encodeBw;

    private readonly MemoryStream _decodeMs;
    private readonly BinaryReader _decodeBr;

    private const ushort MaxPackSize = 16384;
    private SocketAsyncEventArgs _recvEventArgs;
    private SocketAsyncEventArgs _sendEventArgs;
    private readonly object _lockObject = new object();
    private readonly Queue<MsgInfo> _sendQueue = new Queue<MsgInfo>();

    private readonly NetEnDecode _netEnDecode;
    private readonly NetRespEvent _onNetRespEvent;

    public int Id
    {
        get { return GetHashCode(); }
    }

    private byte PackId
    {
        get { return _packId++; }
    }

    private void PackIdReset()
    {
        _packId = 0;
    }

    public string Name
    {
        get { return this.GetType().Name; }
    }

    public bool IsConnected()
    {
        if (_socket == null)
        {
            return false;
        }
        if (!_socket.Connected)
        {
            return false;
        }
        return true;
    }

    public BinaryWriter EncodeBw
    {
        get { return _encodeBw; }
    }

    public CustomTcpClient(NetRespEvent respEvent, NetEnDecode enDecode)
    {
        _onNetRespEvent = respEvent;
        _netEnDecode = enDecode;
        _decodeMs = new MemoryStream();
        _decodeBr = new BinaryReader(_decodeMs);

        _encodeMs = new MemoryStream();
        _encodeBw = new BinaryWriter(_encodeMs);
    }

    public void ReConnect()
    {
        Connect(_host, _port);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="host"></param>
    /// <param name="port"></param>
    /// <param name="connTimeout">毫秒</param>
    public void Connect(string host, int port)
    {
        this._host = host;
        this._port = port;

        _encodeMs.SetLength(0);
        _decodeMs.SetLength(0);
        _decodeMs.Seek(0, SeekOrigin.Begin);
        _encodeMs.Seek(0, SeekOrigin.Begin);

        //Odin.Log.Warning("Host:{0} Port:{1}", host, port);
        //由系统分配本地IP和port,接收端能获到这些信息
        var remoteEndPoint = new IPEndPoint(IPAddress.Parse(host), port);

        _socket = new Socket(remoteEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

        CreateSocketAsyncEvent(remoteEndPoint);

        StartAsyncConnectSocket();
    }

    private void CreateSocketAsyncEvent(EndPoint iPEndPoint)
    {
        _sendEventArgs?.Dispose();
        _recvEventArgs?.Dispose();

        _sendEventArgs = new SocketAsyncEventArgs {RemoteEndPoint = iPEndPoint};
        _sendEventArgs.Completed += IoCompleted;

        _recvEventArgs = new SocketAsyncEventArgs {RemoteEndPoint = iPEndPoint};
        _recvEventArgs.SetBuffer(new byte[MaxPackSize], 0, MaxPackSize);
        _recvEventArgs.Completed += IoCompleted;
    }


    private void StartAsyncConnectSocket()
    {
        if (!_socket.ConnectAsync(_sendEventArgs))
        {
            IoCompleted(_socket, _sendEventArgs);
        }

        _timer = new Timer
        {
            AutoReset = false,
            Interval = Timeout
        };

        _timer.Elapsed += (o, e) =>
        {
            _timer.Dispose();
            _timer = null;

            if (!IsConnected())
            {
                Close("AsyncConnectSocket Timeout");
            }
        };
        _timer.Start();
    }


    public void Dispose()
    {
        Close("CustomTcpClient.Dispose");
    }

    public bool Close(string reason)
    {
        lock (_lockObject)
        {
            if (_socket == null)
            {
                return false;
            }
            if (_socket.Connected)
            {
                _socket.Close();
            }
            _socket.Dispose();
            _socket = null;
            _onNetRespEvent(NetMsg.Create(NetMsgType.Disconnect, reason));
            return true;
        }
    }

    public bool SendMessage(UInt32 cmd, object data = null)
    {
        //if ((ProtoCmdId) cmd != ProtoCmdId.HeartbeatReq)
        //{
        //    Odin.Log.Info("SendMessage-->cmd:{0} {1} Socket Connected {2}", (ProtoCmdId) cmd, cmd, IsConnected());
        //}

        var msgInfo = new MsgInfo {Cmd = cmd, Data = data};

        if (_packId == 0)
        {
            //发送连接成功的第一个包
            //如果已断开丢失第一个包
            //重连验证包或登录验证包
            if (!IsConnected())
            {
                _packId++;
                return false;
            }
            return SendSocket(msgInfo);
        }
        return SendMessage(msgInfo);
    }

    private bool SendMessage(MsgInfo msgInfo)
    {
        lock (_lockObject)
        {
            if (_socket == null)
            {
                _sendQueue.Enqueue(msgInfo);
                return false;
            }

            if (!IsConnected())
            {
                _sendQueue.Enqueue(msgInfo);
                return false;
            }
            if (_sendQueue.Count > 0)
            {
                _sendQueue.Enqueue(msgInfo);
                return true;
            }

            _sendQueue.Enqueue(msgInfo);
            return SendSocket(msgInfo);
        }
    }

    private bool SendSocket(MsgInfo msgInfo)
    {
        try
        {
            _encodeMs.Seek(0, SeekOrigin.Begin);
            _netEnDecode.Encode(_encodeBw, msgInfo.Cmd, PackId, msgInfo.Data);
            var bytes = _encodeMs.ToArray();

            //if ((ProtoCmdId) msgInfo.Cmd != ProtoCmdId.HeartbeatReq)
            //{
            //    var msg = string.Format("Socket Send Cmd:{0} {1}", (ProtoCmdId)msgInfo.Cmd, msgInfo.Cmd);
            //    _onNetRespEvent(NetMsg.Create(NetMsgType.Debug, msg));
            //}

            _sendEventArgs.SetBuffer(bytes, 0, bytes.Length);
            if (!_socket.SendAsync(_sendEventArgs))
            {
                SendCompleted(_sendEventArgs);
            }
            return true;
        }
        catch (Exception e)
        {
            Close("SendToAsync Error:" + e.Message + " Cmd:" + msgInfo.Cmd);
            return false;
        }
    }

    private void ReceiveAsync()
    {
        if (!IsConnected()) return;
        try
        {
            _recvEventArgs.SetBuffer(0, MaxPackSize);
            if (!_socket.ReceiveAsync(_recvEventArgs))
            {
                IoCompleted(_socket, _recvEventArgs);
            }
        }
        catch (Exception ex)
        {
            Close("ReceiveAsync:" + ex.Message);
        }
    }

    private bool CheckSocketState(SocketAsyncEventArgs e)
    {
        if (e.SocketError == SocketError.Success && IsConnected()) return true;

        Close("SocketAsyncEventArgs:" + e.SocketError + " IsConnected:" + IsConnected());
        return false;
    }

    private void IoCompleted(object sender, SocketAsyncEventArgs e)
    {
        try
        {
            if (!CheckSocketState(e)) return;
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    ReceiveCompleted(e);
                    break;
                case SocketAsyncOperation.Send:
                    SendCompleted(e);
                    break;
                case SocketAsyncOperation.Connect:
                    PackIdReset();
                    _onNetRespEvent(NetMsg.Create(NetMsgType.Connect));
                    ReceiveAsync();
                    break;
                case SocketAsyncOperation.Disconnect:
                    Close("LastOperation.Disconnect");
                    break;
                default:
                    Close("LastOperation" + e.LastOperation);
                    break;
            }
        }
        catch (Exception ex)
        {
            Close("IoCompleted:\n" + ex);
        }
    }

    private void ReceiveCompleted(SocketAsyncEventArgs e)
    {
        if (e.BytesTransferred == 0)
        {
            Close("Receive Bytes:0");
            return;
        }
        try
        {
            _decodeMs.Seek(0, SeekOrigin.End);
            _decodeMs.Write(e.Buffer, e.Offset, e.BytesTransferred);

            if (!_netEnDecode.Decode(_decodeBr, DecodeMsgCallback))
            {
                Close("Decode Buffer Pack Error");
                return;
            }

        }
        catch (Exception ex)
        {
            Close("Receive Completed Error: " + ex.Message);
            return;
        }

        ReceiveAsync();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="cmd"></param>
    /// <param name="data"></param>
    private void DecodeMsgCallback(UInt32 cmd, byte[] data)
    {
        var netMsg = new NetMsg
        {
            cmd = cmd,
            Data = data,
            MsgType = NetMsgType.Message
        };
        _onNetRespEvent(netMsg);
    }

    private void SendCompleted(SocketAsyncEventArgs e)
    {
        if (!IsConnected())
        {
            Close("IsConnected() == false");
            return;
        }

        if (!CheckSocketState(e))
            return;

        lock (_lockObject)
        {
            //发送完再减掉
            if (_sendQueue.Count == 0)
            {
                return;
            }
            //第一包没放到队列中
            if (_packId > 1)
                _sendQueue.Dequeue();

            if (_sendQueue.Count > 0)
            {
                SendSocket(_sendQueue.Peek());
            }
        }
    }
}

