
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;


public enum DisType {
        Exception,
        Disconnect,
}

public class TcpSocketClient// : SocketClient
{
    private TcpClient client = null;
    private NetworkStream outStream = null;
    private MemoryStream memStream;
    private BinaryReader reader;

    private NetRespEvent _onNetRespEvent;

    private const int MAX_READ = 8192;
    private byte[] byteBuffer = new byte[MAX_READ];

    private string host = "";

    private int port = -1;

    public int Port
    {
        get { return port; }
    }



    //public delegate void NetMsg(int socket, BinaryReader streamReader);
    //public delegate void ConnectEvent(int socket);
    //public ConnectEvent onConnected;
    //public ConnectEvent onDisConnected;
    //public NetMsg onReceived;

    public TcpBinDecoder decoder;
    //public int Id { get { return GetHashCode(); } }

    /// <summary>
    /// 注册代理
    /// </summary>
    public void OnRegister()
    {
        memStream = new MemoryStream();
        reader = new BinaryReader(memStream);
        //if (onReceived == null)
        //{
        //    onReceived = OnReceive;
        //}
    }

    /// <summary>
    /// 移除代理
    /// </summary>
    public void OnRemove()
    {
        this.Close();
        reader.Close();
        memStream.Close();
    }

    /// <summary>
    /// 连接服务器
    /// </summary>
    void ConnectServer(string host, int port)
    {
        client = null;
        this.host = host;
        this.port = port;
        try
        {
            IPAddress[] address = Dns.GetHostAddresses(host);
            if (address.Length == 0)
            {
                //Odin.Log.Error("host invalid");
                return;
            }
            if (address[0].AddressFamily == AddressFamily.InterNetworkV6)
            {
                client = new TcpClient(AddressFamily.InterNetworkV6);
            }
            else
            {
                client = new TcpClient(AddressFamily.InterNetwork);
            }
            client.SendTimeout = 1000;
            client.ReceiveTimeout = 1000;
            client.NoDelay = true;
            client.BeginConnect(host, port, new AsyncCallback(OnConnect), null);
        }
        catch (Exception e)
        {
            OnDisconnected(DisType.Exception, e.Message);
            //Close(); Odin.Log.Error(e.Message);
        }
    }

    /// <summary>
    /// 连接上服务器
    /// </summary>
    void OnConnect(IAsyncResult asr)
    {
        outStream = client.GetStream();
        client.GetStream().BeginRead(byteBuffer, 0, MAX_READ, new AsyncCallback(OnRead), null);

        if (_onNetRespEvent != null)
        {
            OnNetRespEvent(NetMsg.Create(NetMsgType.Connect));
        }
    }

    /// <summary>
    /// 写数据
    /// </summary>
    bool WriteMessage(byte[] message)
    {
        MemoryStream ms = null;
        using (ms = new MemoryStream())
        {
            ms.Position = 0;
            BinaryWriter writer = new BinaryWriter(ms);

            writer.Write(message);
            writer.Flush();
            if (client != null && client.Connected)
            {
                //NetworkStream stream = client.GetStream();
                byte[] payload = ms.ToArray();
                outStream.BeginWrite(payload, 0, payload.Length, new AsyncCallback(OnWrite), null);
                return true;
            }
            else
            {
                //Odin.Log.Error("client.connected----->>false");
                return false;
            }
        }
    }

    /// <summary>
    /// 读取消息
    /// </summary>
    void OnRead(IAsyncResult asr)
    {
        int bytesRead = 0;
        try
        {
            lock (client.GetStream())
            {
                //读取字节流到缓冲区
                bytesRead = client.GetStream().EndRead(asr);
            }
            if (bytesRead < 1)
            {
                //包尺寸有问题，断线处理
                OnDisconnected(DisType.Disconnect, "bytesRead < 1");
                return;
            }

            if (decoder != null)
            {
                byte[] msg = decoder.Decode(byteBuffer, bytesRead);
                if (msg != null)
                {
                    var _event = NetMsg.Create(NetMsgType.Message);
                    _event.Data = msg;
                    OnNetRespEvent(_event);
                }
            }
            //OnReceive(byteBuffer, bytesRead);   //分析数据包内容，抛给逻辑层

            lock (client.GetStream())
            {
                //分析完，再次监听服务器发过来的新消息
                Array.Clear(byteBuffer, 0, byteBuffer.Length); //清空数组
                client.GetStream().BeginRead(byteBuffer, 0, MAX_READ, new AsyncCallback(OnRead), null);
            }
        }
        catch (Exception ex)
        {
            //PrintBytes();
            OnDisconnected(DisType.Exception, ex.Message);
        }
    }

    /// <summary>
    /// 丢失链接
    /// </summary>
    void OnDisconnected(DisType dis, string msg)
    {
        this.host = "";
        this.port = -1;

        Close(); //关掉客户端链接
        //int protocal = dis == DisType.Exception ? Protocal.Exception : Protocal.Disconnect;

        //Odin.Log.Error("Connection was closed by the server:>" + msg + " Distype:>" + dis);
    }

    /// <summary>
    /// 打印字节
    /// </summary>
    /// <param name="bytes"></param>
    void PrintBytes()
    {
        string returnStr = string.Empty;
        for (int i = 0; i < byteBuffer.Length; i++)
        {
            returnStr += byteBuffer[i].ToString("X2");
        }
        //Odin.Log.Error(returnStr);
    }

    /// <summary>
    /// 向链接写入数据流
    /// </summary>
    void OnWrite(IAsyncResult r)
    {
        try
        {
            outStream.EndWrite(r);
        }
        catch (Exception ex)
        {
            //Odin.Log.Error("OnWrite--->>>" + ex.Message);
        }
    }

    /// <summary>
    /// 剩余的字节
    /// </summary>
    private long RemainingBytes()
    {
        return memStream.Length - memStream.Position;
    }

    /// <summary>
    /// 会话发送
    /// </summary>
    bool SessionSend(byte[] bytes)
    {
        return WriteMessage(bytes);
    }

    public void SendConnect(string host)
    {
        ConnectServer(host, 80);
    }

    public void SendConnect(string host, int port, ushort maxPackSize = 0)
    {
        ConnectServer(host, port);
    }

    public bool SendMessage(byte[] message)
    {
        return SendMessage(message, null);
    }

    public bool SendMessage(byte[] message, Dictionary<string, string> requestHeader)
    {
        return SessionSend(message);
    }

    public bool SendMessage(string message)
    {
        var buffer = Encoding.UTF8.GetBytes(message);
        return SendMessage(buffer);
    }

    public bool SendMessage(string message, Dictionary<string, string> requestHeader)
    {
        return SendMessage(message);
    }

    public bool Close()
    {
        if (client != null)
        {
            bool state = client.Connected;
            if (state) client.Close();

            client = null;
            if (state && _onNetRespEvent != null)
            {
                OnNetRespEvent(NetMsg.Create(NetMsgType.Disconnect));
                //onDisConnected(Id);
            }
            return state;
        }
        return false;
    }

    public bool IsConnected()
    {
        if (client != null)
        {
            return client.Connected;
        }

        return false;
    }

    public int Id
    {
        get { return GetHashCode(); }
    }

    public string Name
    {
        get { return "TcpSocketClient"; }
    }

    public string Host
    {
        get { return host; }
    }

    public NetRespEvent OnNetRespEvent
    {
        get { return _onNetRespEvent; }
        set { _onNetRespEvent = value; }
    }
}
