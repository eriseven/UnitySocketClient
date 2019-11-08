

//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Net;
//using UnityEngine;
//using UnityEngine.Networking;


//public sealed class NetworkAdapter
//{
//    private string _url = null;
//    private SocketClient client;

//    private readonly Queue<string> _reqStrMsg = new Queue<string>();
//    private readonly Queue<byte[]> _reqByteMsg = new Queue<byte[]>();
//    private readonly Queue<NetMsg> _respMsg = new Queue<NetMsg>();

//    public NetRespEvent OnSocketEvent = null;

//    public string ProtocolName
//    {
//        get
//        {
//            if (client != null)
//            {
//                return client.Name;
//            }
//            else
//            {
//                return string.Empty;
//            }
//        }
//    }

//    public NetworkAdapter()
//    {

//    }

//    public bool IsConnected()
//    {
//        if (client != null)
//        {
//            return client.IsConnected();
//        }
//        return false;
//    }

//    /// <summary>
//    /// ws://127.0.0.1:8080
//    /// tcp://127.0.0.1:8080
//    /// udp://127.0.0.1:8080
//    /// http://127.0.0.1:8080
//    /// https://127.0.0.1:8080
//    /// </summary>
//    /// <param name="url"></param>
//    public bool Connect(string url)
//    {
//        if (string.IsNullOrEmpty(url))
//        {
//            Odin.Log.Error("url error");
//            return false;
//        }

//        var idx = url.IndexOf("://");
//        string prefix = url.Substring(0, idx);

//        int port;
//        string ip;
//        string result;
//        switch (prefix.ToLower())
//        {
//            case "tcp":
//                var tcpClient = new TcpSocketClient();
//                tcpClient.decoder = new TcpBinDecoder();
//                client = tcpClient;
//                result = SplitIpPort(url, out ip, out port);
//                if (result == string.Empty)
//                {
//                    client.SendConnect(ip, port, 8096);
//                }
//                else
//                {
//                    Odin.Log.Error(result);
//                    return false;
//                }
//                break;
//            case "udp":

//                client = new CustomUdpClient();
//                result = SplitIpPort(url, out ip, out port);
//                if (result == string.Empty)
//                {
//                    client.SendConnect(ip, port, 8096);
//                }
//                else
//                {
//                    Odin.Log.Error(result);
//                    return false;
//                }
//                break;
//            case "ws":
//                client = new WebSocketClient();
//                client.Connect(url);
//                break;
//            case "http":
//            case "https":
//                client = new GameObject("httpClient").AddComponent<HttpClient>();
//                client.Connect(url);
//                break;
//            default:
//                Odin.Log.Error("unkown protocol");
//                return false;
//        }
//        this._url = url;
//        //this.OnSocketEvent = OnSocketEvent;
//        client._OnRespEvent = OnRespMsgEvent;
//        return true;
//    }


//    private string SplitIpPort(string url, out string ip, out int port)
//    {
//        url = url.Substring(url.IndexOf("://", StringComparison.Ordinal) + 3);

//        var split = url.Split(':');

//        port = -1;
//        ip = string.Empty;
//        if (split.Length == 1)
//        {
//            return "url error";
//        }
//        IPAddress iPAddress;
//        if (!IPAddress.TryParse(split[0], out iPAddress))
//        {
//            return "url ip error";
//        }
//        ip = split[0];
//        if (!int.TryParse(split[1], out port))
//        {
//            return "url port error";
//        }
//        return string.Empty;
//    }

//    private void OnRespMsgEvent(NetMsg ev)
//    {
//        lock (_respMsg)
//        {
//            _respMsg.Enqueue(ev);

//        }
//    }

//    public void Close()
//    {
//        if (client != null)
//        {
//            client.Close();
//        }
//    }

//    public void SendMessage(byte[] message)
//    {
//        if (client != null && IsConnected())
//        {
//            client.SendMessage(message);
//        }
//        else
//        {
//            _reqByteMsg.Enqueue(message);
//            Odin.Log.Error("Connect Error");
//        }
//    }

//    public void SendMessage(byte[] message, Dictionary<string, string> requestHeader)
//    {
//        if (client != null && IsConnected())
//        {
//            client.SendMessage(message, requestHeader);
//        }
//        else
//        {
//            _reqByteMsg.Enqueue(message);
//            Odin.Log.Error("Connect Error");
//        }
//    }

//    public void SendMessage(string message)
//    {
//        if (client != null && IsConnected())
//        {
//            client.SendMessage(message);
//        }
//        else
//        {
//            _reqStrMsg.Enqueue(message);
//            Odin.Log.Error("Connect Error");
//        }
//    }

//    public void SendMessage(string message, Dictionary<string, string> requestHeader)
//    {
//        if (client != null && IsConnected())
//        {
//            client.SendMessage(message, requestHeader);
//        }
//        else
//        {
//            _reqStrMsg.Enqueue(message);
//            Odin.Log.Error("Connect Error");
//        }
//    }

//    /// <summary>
//    /// 
//    /// </summary>
//    /// <param name="realtime"></param>
//    public void Update()
//    {
//        if (OnSocketEvent != null)
//        {
//            lock (_respMsg)
//            {
//                if (_respMsg.Count > 0)
//                {
//                    foreach (var msg in _respMsg)
//                    {
//                        try
//                        {
//                            OnSocketEvent(msg);
//                        }
//                        catch (Exception ex)
//                        {
//                            Odin.Log.Error("ex:" + ex.ToString());
//                        }
//                    }
//                    _respMsg.Clear();
//                }
//            }
//        }

//        if (client != null && client.IsConnected())
//        {
//            if (_reqByteMsg.Count > 0)
//            {
//                foreach (var msg in _reqByteMsg)
//                {
//                    client.SendMessage(msg);
//                }
//                _reqByteMsg.Clear();
//            }

//            if (_reqStrMsg.Count > 0)
//            {
//                foreach (var msg in _reqStrMsg)
//                {
//                    client.SendMessage(msg);
//                }
//                _reqStrMsg.Clear();
//            }
//        }
//    }

//}

//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Net;
//using UnityEngine;
//using UnityEngine.Networking;


//public sealed class NetworkAdapter
//{
//    private string _url = null;
//    private SocketClient client;

//    private readonly Queue<string> _reqStrMsg = new Queue<string>();
//    private readonly Queue<byte[]> _reqByteMsg = new Queue<byte[]>();
//    private readonly Queue<NetMsg> _respMsg = new Queue<NetMsg>();

//    public NetRespEvent OnSocketEvent = null;

//    public string ProtocolName
//    {
//        get
//        {
//            if (client != null)
//            {
//                return client.Name;
//            }
//            else
//            {
//                return string.Empty;
//            }
//        }
//    }

//    public NetworkAdapter()
//    {

//    }

//    public bool IsConnected()
//    {
//        if (client != null)
//        {
//            return client.IsConnected();
//        }
//        return false;
//    }

//    /// <summary>
//    /// ws://127.0.0.1:8080
//    /// tcp://127.0.0.1:8080
//    /// udp://127.0.0.1:8080
//    /// http://127.0.0.1:8080
//    /// https://127.0.0.1:8080
//    /// </summary>
//    /// <param name="url"></param>
//    public bool Connect(string url)
//    {
//        if (string.IsNullOrEmpty(url))
//        {
//            Odin.Log.Error("url error");
//            return false;
//        }

//        var idx = url.IndexOf("://");
//        string prefix = url.Substring(0, idx);

//        int port;
//        string ip;
//        string result;
//        switch (prefix.ToLower())
//        {
//            case "tcp":
//                var tcpClient = new TcpSocketClient();
//                tcpClient.decoder = new TcpBinDecoder();
//                client = tcpClient;
//                result = SplitIpPort(url, out ip, out port);
//                if (result == string.Empty)
//                {
//                    client.SendConnect(ip, port, 8096);
//                }
//                else
//                {
//                    Odin.Log.Error(result);
//                    return false;
//                }
//                break;
//            case "udp":

//                client = new CustomUdpClient();
//                result = SplitIpPort(url, out ip, out port);
//                if (result == string.Empty)
//                {
//                    client.SendConnect(ip, port, 8096);
//                }
//                else
//                {
//                    Odin.Log.Error(result);
//                    return false;
//                }
//                break;
//            case "ws":
//                client = new WebSocketClient();
//                client.Connect(url);
//                break;
//            case "http":
//            case "https":
//                client = new GameObject("httpClient").AddComponent<HttpClient>();
//                client.Connect(url);
//                break;
//            default:
//                Odin.Log.Error("unkown protocol");
//                return false;
//        }
//        this._url = url;
//        //this.OnSocketEvent = OnSocketEvent;
//        client._OnRespEvent = OnRespMsgEvent;
//        return true;
//    }


//    private string SplitIpPort(string url, out string ip, out int port)
//    {
//        url = url.Substring(url.IndexOf("://", StringComparison.Ordinal) + 3);

//        var split = url.Split(':');

//        port = -1;
//        ip = string.Empty;
//        if (split.Length == 1)
//        {
//            return "url error";
//        }
//        IPAddress iPAddress;
//        if (!IPAddress.TryParse(split[0], out iPAddress))
//        {
//            return "url ip error";
//        }
//        ip = split[0];
//        if (!int.TryParse(split[1], out port))
//        {
//            return "url port error";
//        }
//        return string.Empty;
//    }

//    private void OnRespMsgEvent(NetMsg ev)
//    {
//        lock (_respMsg)
//        {
//            _respMsg.Enqueue(ev);

//        }
//    }

//    public void Close()
//    {
//        if (client != null)
//        {
//            client.Close();
//        }
//    }

//    public void SendMessage(byte[] message)
//    {
//        if (client != null && IsConnected())
//        {
//            client.SendMessage(message);
//        }
//        else
//        {
//            _reqByteMsg.Enqueue(message);
//            Odin.Log.Error("Connect Error");
//        }
//    }

//    public void SendMessage(byte[] message, Dictionary<string, string> requestHeader)
//    {
//        if (client != null && IsConnected())
//        {
//            client.SendMessage(message, requestHeader);
//        }
//        else
//        {
//            _reqByteMsg.Enqueue(message);
//            Odin.Log.Error("Connect Error");
//        }
//    }

//    public void SendMessage(string message)
//    {
//        if (client != null && IsConnected())
//        {
//            client.SendMessage(message);
//        }
//        else
//        {
//            _reqStrMsg.Enqueue(message);
//            Odin.Log.Error("Connect Error");
//        }
//    }

//    public void SendMessage(string message, Dictionary<string, string> requestHeader)
//    {
//        if (client != null && IsConnected())
//        {
//            client.SendMessage(message, requestHeader);
//        }
//        else
//        {
//            _reqStrMsg.Enqueue(message);
//            Odin.Log.Error("Connect Error");
//        }
//    }

//    /// <summary>
//    /// 
//    /// </summary>
//    /// <param name="realtime"></param>
//    public void Update()
//    {
//        if (OnSocketEvent != null)
//        {
//            lock (_respMsg)
//            {
//                if (_respMsg.Count > 0)
//                {
//                    foreach (var msg in _respMsg)
//                    {
//                        try
//                        {
//                            OnSocketEvent(msg);
//                        }
//                        catch (Exception ex)
//                        {
//                            Odin.Log.Error("ex:" + ex.ToString());
//                        }
//                    }
//                    _respMsg.Clear();
//                }
//            }
//        }

//        if (client != null && client.IsConnected())
//        {
//            if (_reqByteMsg.Count > 0)
//            {
//                foreach (var msg in _reqByteMsg)
//                {
//                    client.SendMessage(msg);
//                }
//                _reqByteMsg.Clear();
//            }

//            if (_reqStrMsg.Count > 0)
//            {
//                foreach (var msg in _reqStrMsg)
//                {
//                    client.SendMessage(msg);
//                }
//                _reqStrMsg.Clear();
//            }
//        }
//    }

//}
