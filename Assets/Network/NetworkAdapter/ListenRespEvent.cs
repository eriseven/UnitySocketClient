using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
//using proto.cmd;

public delegate void NetRespCb(object obj);

public class ListenRespEvent
{
    struct CmdCbData
    {
        public int cmd;
        public NetRespCb cb;
    }

    private static Int64 _CallCmd = Int64.MinValue;

    private static MemoryStream _mStream = new MemoryStream();
    private static readonly Queue<CmdCbData> _DeleteQueue = new Queue<CmdCbData>();
    //private static readonly ProtoBufCsSerializer _protoBufSerializer = new ProtoBufCsSerializer();
    private static readonly Dictionary<int, List<NetRespCb>> _DictRespCb = new Dictionary<int, List<NetRespCb>>();

    static NetRespCb _DefaultCb = delegate { };
    public static void RegitDefaultListener(NetRespCb callback)
    {
        _DefaultCb += callback;
    }

    private static object Deserialize(byte[] buffer, Type type)
    {
        if (buffer != null)
        {
            _mStream.Seek(0, SeekOrigin.Begin);
            _mStream.SetLength(buffer.Length);
            _mStream.Write(buffer, 0, buffer.Length);
            _mStream.Seek(0, SeekOrigin.Begin);
        }
        else
        {
            _mStream.Seek(0, SeekOrigin.Begin);
            _mStream.SetLength(0);
        }

#if UNITY_EDITOR
        //string str = "|";
        //for (int i = 0; i < buffer.Length; i++)
        //{
        //    str += buffer[i] + "|";
        //}
        //Debug.LogFormat("Deserialize type:{0} len:{1} Data:{2}", type.Name, buffer.Length, str);
#endif

        try
        {
            //return _protoBufSerializer.Deserialize(_mStream, null, type);
            return null;
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            return null;
        }
    }


    public static object Deserialize(NetMsg netMsg)
    {
        //var type = NetCmdToMsgName.Get((int)netMsg.cmd);
        //return Deserialize(netMsg.Data, type);
        return null;
    }

    public static void CallRespCb(NetMsg netMsg)
    {
        _DictRespCb.TryGetValue((int)netMsg.cmd, out var list);
        if (list == null && _DefaultCb == null)
        {
            //Odin.Log.Warning("CallRespCb Cmd:{0} Not Listener Event", netMsg.cmd);
            return;
        }

        try
        {
            _CallCmd = netMsg.cmd;
            object arg = null;

            /*
            if (netMsg.Data == null || netMsg.Data.Length == 0)
            {
                //foreach (var cb in list) cb(null);
            }
            else
            */
            {
                //var type = NetCmdToMsgName.Get((int)netMsg.cmd);
                //arg = Deserialize(netMsg.Data, type);
            }

            if (list != null)
            {
                foreach (var cb in list)
                {
                    try
                    {
                        cb(arg);
                    }
                    catch (Exception ex)
                    {
                        //Odin.Log.Error("CallRespCb Cmd:{0} Error:{1}", netMsg.cmd, ex.ToString());
                    }
                }
            }
            _DefaultCb?.Invoke(arg);
        }
        catch (Exception ex)
        {
            //Odin.Log.Error("CallRespCb Cmd:{0} Error:{1}", netMsg.cmd, ex.ToString());
            throw;
        }
        finally
        {
            _CallCmd = Int64.MinValue;
        }
        if (_DeleteQueue.Count == 0)
        {
            return;
        }
        foreach (var kv in _DeleteQueue)
        {
            DelRespCb(kv.cmd, kv.cb);
        }
        _DeleteQueue.Clear();
    }

    public static bool DelRespCb(int cmd, NetRespCb netRespCb = null)
    {
        if (!_DictRespCb.TryGetValue((int)cmd, out var list))
        {
            return false;
        }
        if (_CallCmd == cmd)
        {
            var data = new CmdCbData();
            data.cmd = cmd;
            data.cb = netRespCb;
            _DeleteQueue.Enqueue(data);
            return true;
        }

        if (netRespCb == null)
        {
            _DictRespCb.Remove(cmd);
            return true;
        }

        if (list.RemoveAll(m => m == netRespCb) > 0)
        {
            if (list.Count == 0) _DictRespCb.Remove(cmd);
            return true;
        }
        return false;
    }

    public static bool AddRespCb(int cmd, NetRespCb netRespCb)
    {
        if (!_DictRespCb.TryGetValue((int)cmd, out var list))
        {
            list = new List<NetRespCb>();
            _DictRespCb.Add(cmd, list);
        }
        if (list != null) list.Add(netRespCb);
        return true;
    }

}
