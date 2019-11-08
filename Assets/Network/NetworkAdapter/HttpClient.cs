using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using WebSocketSharp;


public class HttpClient : MonoBehaviour//, SocketClient
{
    private string _host;
    private int _timeout;
    private NetRespEvent _onNetRespEvent;
    private UnityWebRequest unityWebRequest = null;

    private void Awake()
    {
        _timeout = 60;
    }

    public int Timeout
    {
        set { _timeout = value; }
    }

    public bool SendTextMessage(string host)
    {
        return SendTextMessage(host, null, null);
    }

    public bool SendTextMessage(string host, string message)
    {
        return SendTextMessage(host, message, null);
    }

    public bool SendTextMessage(string host, string message, Dictionary<string, string> requestHeader)
    {
        if (string.IsNullOrEmpty(message))
        {
            return SendBinaryMessage(host, (byte[]) null, requestHeader);
        }
        else
        {
            return SendBinaryMessage(host, Encoding.UTF8.GetBytes(message), requestHeader);
        }
    }

    public bool SendBinaryMessage(string host, byte[] message)
    {
        return SendBinaryMessage(host, message, null);
    }

    public bool SendBinaryMessage(string host, byte[] message, Dictionary<string, string> requestHeader)
    {
        if (unityWebRequest != null)
        {
            unityWebRequest.Dispose();
            unityWebRequest = null;
        }

        this._host = host;
        unityWebRequest = new UnityWebRequest();
        unityWebRequest.url = host;
        unityWebRequest.timeout = _timeout;

        if (message == null)
        {
            if (unityWebRequest.method != UnityWebRequest.kHttpVerbGET)
            {
                unityWebRequest.method = UnityWebRequest.kHttpVerbGET;
            }

            if (unityWebRequest.downloadHandler == null)
            {
                unityWebRequest.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            }
        }
        else
        {
            if (unityWebRequest.method != UnityWebRequest.kHttpVerbPOST)
            {
                unityWebRequest.method = UnityWebRequest.kHttpVerbPOST;
            }

            if (unityWebRequest.uploadHandler == null)
            {
                unityWebRequest.uploadHandler = (UploadHandler)new UploadHandlerRaw(message);
            }

            if (unityWebRequest.downloadHandler == null)
            {
                unityWebRequest.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            }
        }

        if (requestHeader != null)
        {
            foreach (var kv in requestHeader)
            {
                unityWebRequest.SetRequestHeader(kv.Key, kv.Value);
            }
        }

        StartCoroutine(SendWebRequest());
        return true;
    }

    private void CallRespEvent(NetMsgType msgType, byte[] data)
    {
        if (_onNetRespEvent != null)
        {
            var msgEvent = new NetMsg
            {
                MsgType = msgType,
                Data = data
            };
            _onNetRespEvent(msgEvent);
        }
    }

    private IEnumerator SendWebRequest()
    {
        yield return unityWebRequest.SendWebRequest();
        if (unityWebRequest == null)
        {
            Destroy();
            var msg = "unityWebRequest.SendWebRequest() == null";
            CallRespEvent(NetMsgType.Error, Encoding.UTF8.GetBytes(msg));
            yield break;

        }
        if (unityWebRequest.error != null || unityWebRequest.isHttpError || unityWebRequest.isNetworkError)
        {
            var err = unityWebRequest.error;
            Destroy();
            if (err == null) err = "unknow error";
            CallRespEvent(NetMsgType.Error, Encoding.UTF8.GetBytes(err));
        }
        else
        {
            if (unityWebRequest.responseCode == 200)
            {
                var data = unityWebRequest.downloadHandler.data;
                Destroy();
                CallRespEvent(NetMsgType.Message, data);
            }
            else
            {
                Destroy();
                var err = "ResponseCode:" + unityWebRequest.responseCode;
                CallRespEvent(NetMsgType.Error, Encoding.UTF8.GetBytes(err));
            }
        }
    }

    public bool Destroy()
    {
        if (this == null)
        {
            return false;
        }
        if (unityWebRequest != null)
        {
            unityWebRequest.Dispose();
            unityWebRequest = null;
            return true;
        }
        GameObject.Destroy(gameObject);
        return false;
    }

    public int Id
    {
        get { return GetHashCode(); }
    }

    public string Name
    {
        get { return "HttpClient"; }
    }

    public string Host
    {
        get { return _host; }
    }

    public NetRespEvent OnNetRespEvent
    {
        get { return _onNetRespEvent; }
        set { _onNetRespEvent = value; }
    }

    public bool IsConnected()
    {
        return true;
    }
}