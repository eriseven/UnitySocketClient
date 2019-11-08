

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;

public enum NetMsgType
{
    Debug,
    Error,
    Message,
    Connect,
    Disconnect,
}

public class NetMsg
{
    public UInt32 cmd;
    public byte[] Data;
    public NetMsgType MsgType;
    public static NetMsg Create(NetMsgType msgType)
    {
        var msgEvent = new NetMsg();
        msgEvent.MsgType = msgType;
        return msgEvent;
    }
    public static NetMsg Create(NetMsgType msgType, string msg)
    {
        var msgEvent = new NetMsg();
        msgEvent.MsgType = msgType;
        if (!string.IsNullOrEmpty(msg))
        {
            msgEvent.Data = Encoding.UTF8.GetBytes(msg);
        }
        return msgEvent;
    }

    public static NetMsg Create(NetMsgType msgType, byte[] msg)
    {
        var msgEvent = new NetMsg();
        msgEvent.MsgType = msgType;
        msgEvent.Data = msg;
        return msgEvent;
    }
}

public delegate void NetRespEvent(NetMsg ev);
public delegate void NetRespData(UInt32 cmd, byte[] data);

public sealed class NetworkAdapter
{
    private string _url = null;
    private SocketClient _client;
    private const ushort _reqQueueMaxNum = 32;
    private static readonly object LockObject = new object();
    //private readonly Queue<byte[]> _reqQueue = new Queue<byte[]>();
    private readonly Queue<NetMsg> _respQueue = new Queue<NetMsg>();

    //public NetRespEvent _NetRespEvent = null;

    private event NetRespEvent NetRespEvent = null;


    public string ProtocolName
    {
        get
        {
            if (_client != null)
            {
                return _client.Name;
            }
            else
            {
                return string.Empty;
            }
        }
    }

    public bool IsConnected()
    {
        if (_client != null)
        {
            return _client.IsConnected();
        }
        return false;
    }

    public void AddNetRespEvent(NetRespEvent netRespEvent)
    {
        if (NetRespEvent == null)
        {
            NetRespEvent = netRespEvent;
        }
        else
        {
            NetRespEvent += netRespEvent;
        }
    }

    public void DelNetRespEvent(NetRespEvent netRespEvent)
    {
        if (NetRespEvent != null)
        {
            NetRespEvent -= netRespEvent;
        }
    }

    public bool ReConnect()
    {
        if (_client == null)
        {
            return false;
        }
        _client.ReConnect();
        return true;
    }

    public static void ParseUrl(string url, out string ip, out int port, out string conn)
    {
        var idx = url.IndexOf("://");
        conn = url.Substring(0, idx);
        SplitIpPort(url, out ip, out port);
    }

    /// <summary>
    /// ws://127.0.0.1:8080
    /// tcp://127.0.0.1:8080
    /// udp://127.0.0.1:8080
    /// </summary>
    /// <param name="url"></param>
    /// <param name="netEnDecode"></param>
    /// <param name="netRespEvent"></param>
    /// <returns></returns>
    public bool Connect(string url, NetEnDecode netEnDecode, NetRespEvent netRespEvent)
    {

        if (string.IsNullOrEmpty(url))
        {
            //Odin.Log.Error("Url Is Null");
            return false;
        }

        if (netEnDecode == null)
        {
            //Odin.Log.Error("NetEnDecode Is Null");
            return false;
        }

        if (netRespEvent == null)
        {
            //Odin.Log.Error("NetRespEvent Is Null");
            return false;
        }

        if (NetRespEvent == null)
        {
            NetRespEvent += netRespEvent;
        }
        else
        {
            bool isAddEvent = false;
            foreach (var @delegate in NetRespEvent.GetInvocationList())
            {
                if (@delegate.GetHashCode() == netRespEvent.GetHashCode())
                {
                    isAddEvent = true;
                    break;
                }
            }
            if (!isAddEvent) NetRespEvent += netRespEvent;
        }


        if (string.IsNullOrEmpty(url))
        {
            //Odin.Log.Error("url error");
            return false;
        }
        this._url = url;

        var idx = url.IndexOf("://");
        string prefix = url.Substring(0, idx);

        switch (prefix.ToLower())
        {
            case "tcp":
                _client = new CustomTcpClient(OnRespMsgEvent, netEnDecode);
                return StartConnect(url);
            case "udp":
                _client = new CustomUdpClient(OnRespMsgEvent, netEnDecode);
                return StartConnect(url);
            case "ws":
                _client = new WebSocketClient(OnRespMsgEvent, netEnDecode);
                _client.Connect(url, 0);
                return true;
            default:
                //Odin.Log.Error("does not support protocol");
                return false;
        }
    }


    private bool StartConnect(string url)
    {
        int port; string ip;
        var urlRet = SplitIpPort(url, out ip, out port);
        if (urlRet == string.Empty)
        {
            _client.Connect(ip, port);
            return true;
        }
        else
        {
            //Odin.Log.Error(urlRet);
            return false;
        }
    }


    private static string SplitIpPort(string url, out string ip, out int port)
    {
        url = url.Substring(url.IndexOf("://", StringComparison.Ordinal) + 3);

        var split = url.Split(':');

        port = -1;
        ip = string.Empty;
        if (split.Length == 1)
        {
            return "url error";
        }
        IPAddress iPAddress;
        if (!IPAddress.TryParse(split[0], out iPAddress))
        {
            return "url ip error";
        }
        ip = split[0];
        if (!int.TryParse(split[1], out port))
        {
            return "url port error";
        }
        return string.Empty;
    }

    private void OnRespMsgEvent(NetMsg ev)
    {
        RespQueueOp(ev);
    }

    public void CloseSocket(string reason)
    {
        _client?.Close(reason);
    }

    public void Close(string reason)
    {
        if (_client != null)
        {
            _client.Close(reason);
            lock (LockObject)
            {
                _respQueue.Clear();
                NetRespEvent = null;
            }
        }
    }

    public bool SendMessage(UInt32 cmd, object data = null)
    {
        if (_client == null)
        {
            //Odin.Log.Error("Not Connect Server");
            return false;
        }
        return _client.SendMessage(cmd, data);
    }

    private void RespQueueOp(NetMsg ev)
    {
        lock (LockObject)
        {
            if (ev != null)
            {
                _respQueue.Enqueue(ev);
                return;
            }

            if (_respQueue.Count > 0)
            {
                do
                {
                    var msg = _respQueue.Dequeue();
                    if (msg != null)
                    {
                        try
                        {
                            if (NetRespEvent != null)
                            {
                                NetRespEvent(msg);
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogException(ex);
                        }
                    }
                } while (_respQueue.Count > 0);

            }
        }
    }

    /// <summary>
    /// 获取网络消息
    /// </summary>
    /// <param name="realtime"></param>
    public void Update()
    {
        RespQueueOp(null);
    }

}
