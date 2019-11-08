using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class UWebRequest
{
    private readonly string url;
    private readonly int _timeout;
    private readonly Action<byte[]> _callback;
    private readonly UnityWebRequest _unityWebRequest;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="url"></param>
    /// <param name="callback"></param>
    /// <param name="timeout">单位秒</param>
    public UWebRequest(string url, Action<byte[]> callback, int timeout = 0)
    {
        this.url = url;
        _timeout = timeout;
        _callback = callback;
        //UnityWebRequest.EscapeURL("Fish & Chips");
        _unityWebRequest = new UnityWebRequest(url);
    }

    public bool SendTextMessage()
    {
        return SendTextMessage(null, null);
    }

    public bool SendTextMessage(string message)
    {
        return SendTextMessage(message, null);
    }

    public bool SendTextMessage(string message, Dictionary<string, string> requestHeader)
    {
        if (string.IsNullOrEmpty(message))
        {
            return SendBinaryMessage((byte[])null, requestHeader);
        }
        return SendBinaryMessage(Encoding.UTF8.GetBytes(message), requestHeader);
    }

    public bool SendBinaryMessage(byte[] message)
    {
        return SendBinaryMessage(message, null);
    }

    public bool SendBinaryMessage(byte[] message, Dictionary<string, string> requestHeader)
    {
        if (message == null)
        {
            if (_unityWebRequest.method != UnityWebRequest.kHttpVerbGET)
            {
                _unityWebRequest.method = UnityWebRequest.kHttpVerbGET;
            }

            if (_unityWebRequest.downloadHandler == null)
            {
                _unityWebRequest.downloadHandler = new DownloadHandlerBuffer();
            }
        }
        else
        {
            if (_unityWebRequest.method != UnityWebRequest.kHttpVerbPOST)
            {
                _unityWebRequest.method = UnityWebRequest.kHttpVerbPOST;
            }

            if (_unityWebRequest.uploadHandler == null)
            {
                _unityWebRequest.uploadHandler = new UploadHandlerRaw(message);
            }

            if (_unityWebRequest.downloadHandler == null)
            {
                _unityWebRequest.downloadHandler = new DownloadHandlerBuffer();
            }
        }

        if (requestHeader != null)
        {
            foreach (var kv in requestHeader)
            {
                _unityWebRequest.SetRequestHeader(kv.Key, kv.Value);
            }
        }
        _unityWebRequest.timeout = _timeout;

        var asyncOperation = _unityWebRequest.SendWebRequest();

        asyncOperation.completed += RequestCompleted;

        return true;
    }

    private void RequestCompleted(AsyncOperation asyncOperation)
    {
        if (!(asyncOperation is UnityWebRequestAsyncOperation operation) || operation.webRequest == null)
        {
            //Odin.Log.Error("Url:{0} SendWebRequest() == null", url);
            Close();
            _callback(null);
            return;
        }

        var request = operation.webRequest;
        if (request.isHttpError || request.isNetworkError)
        {
            //if (request.error == null)
            //    Odin.Log.Error("Url:{0} SendWebRequest() == null", url);
            //else
            //    Odin.Log.Error("Url:{0} SendWebRequest() error:{1}", url, request.error);
            Close();
            _callback(null);
        }
        else
        {
            if (request.responseCode == 200)
            {
                var data = request.downloadHandler.data;
                Close();
                if (data.Length == 0)
                {
                    //Odin.Log.Warning("Url:{0} SendWebRequest() Data Size:0 ", url);
                }
                _callback(data);
            }
            else
            {

                //Odin.Log.Error("Url:{0} ResponseCode:{1}", url, request.responseCode);
                Close();
                _callback(null);
            }
        }
    }

    public bool Close()
    {
        if (_unityWebRequest != null)
        {
            _unityWebRequest.Dispose();
            //_unityWebRequest = null;
            return true;
        }
        return false;
    }

}