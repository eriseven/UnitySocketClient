using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using WebSocketSharp;
using ErrorEventArgs = WebSocketSharp.ErrorEventArgs;

public class WebSocketClient : SocketClient, IDisposable
{
    private string host;
    private byte packId = 0;
    private WebSocket webSocket;

    private readonly MemoryStream encodeMs;
    private readonly BinaryWriter encodeBw;

    private readonly NetEnDecode netEnDecode = null;
    private readonly NetRespEvent onNetRespEvent = null;

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
        if (webSocket == null)
        {
            return false;
            
        }
        return webSocket.ReadyState == WebSocketState.Open || webSocket.ReadyState == WebSocketState.Closing;
    }

    public WebSocketClient(NetRespEvent respEvent, NetEnDecode enDecode)
    {
        onNetRespEvent = respEvent;
        netEnDecode = enDecode;
        encodeMs = new MemoryStream();
        encodeBw = new BinaryWriter(encodeMs);
    }

    public void ReConnect()
    {
        Connect(host);
    }

    public void Connect(string host, int port = -1)
    {
        this.host = host;
        Close("Start Connect");
        webSocket = null;

        try
        {
            webSocket = new WebSocket(host);
        }
        catch (System.ArgumentException e)
        {
            Debug.LogException(e);
        }

        webSocket.Origin = "http://www.baidu.com";
        webSocket.OnClose += OnClose;
        webSocket.OnOpen += OnOpen;
        webSocket.OnMessage += OnMessage;
        webSocket.OnError += OnError;

        webSocket.ConnectAsync();
        //webSocket.Connect();
    }

    void OnError(object sender, ErrorEventArgs arg)
    {
        //Odin.Log.Error(arg.Message);
    }

    void OnClose(object sender, CloseEventArgs arg)
    {
        if (onNetRespEvent != null)
        {
            onNetRespEvent(NetMsg.Create(NetMsgType.Disconnect, "OnClose"));
        }
    }

    void OnOpen(object sender, System.EventArgs arg)
    {
        if (onNetRespEvent != null)
        {
            onNetRespEvent(NetMsg.Create(NetMsgType.Connect));
        }
    }

    void OnMessage(object sender, MessageEventArgs arg)
    {
        if (onNetRespEvent != null)
        {
            var _event = NetMsg.Create(NetMsgType.Connect);
            if (arg.IsBinary)
            {
                _event.Data = arg.RawData;
                _event.MsgType = NetMsgType.Message;
            }
            else if (arg.IsText)
            {
                _event.MsgType = NetMsgType.Message;
                _event.Data = Encoding.UTF8.GetBytes(arg.Data);
            }

            onNetRespEvent(_event);
        }
    }

    void OnSend(bool compeleted)
    {

    }

    public bool SendMessage(UInt32 cmd, object data)
    {
        encodeMs.Seek(0, SeekOrigin.Begin);
        netEnDecode.Encode(encodeBw, cmd, PackId, data);
        return SendMessage(encodeMs.ToArray());
    }


    public bool SendMessage(byte[] message)
    {
        if (webSocket != null && webSocket.IsAlive)
        {
            webSocket.SendAsync(message, OnSend);
            return true;
        }
        else
        {
            return false;
        }
    }

    public void Dispose()
    {
        Close("Dispose");
    }

    public bool Close(string reason)
    {
        bool result = false;
        if (webSocket != null && webSocket.IsAlive)
        {
            webSocket.Close();
            ((IDisposable)webSocket).Dispose();
            webSocket = null;
            result = true;
        }
        onNetRespEvent(NetMsg.Create(NetMsgType.Disconnect, reason));
        return result;
    }

}
