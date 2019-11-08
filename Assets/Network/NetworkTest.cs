using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkTest : MonoBehaviour
{
    NetworkAdapter adapter;
    // Start is called before the first frame update
    void Start()
    {
        adapter = new NetworkAdapter();
        adapter.Connect("tcp://127.0.0.1:7000", new TcpEnDecode(), this.NetRespEvent);
    }

    void NetRespEvent(NetMsg netMsg)
    {

        Debug.Log(netMsg.MsgType);
        switch (netMsg.MsgType)
        {
            case NetMsgType.Message:
                //if (netMsg.cmd != (uint)ProtoCmdId.HeartbeatAck)
                //{
                //    Odin.Log.Info("ReceiveMessage:{0}->{1}", (ProtoCmdId)netMsg.cmd, netMsg.cmd);
                //}
                //ListenRespEvent.CallRespCb(netMsg);
                break;
            case NetMsgType.Connect:
                //_IsConnected = true;
                //Odin.Log.Info("Network Connect Success");
                ////如果已进入游戏 发送重连协议
                //if (isEnterGame) ReconnectReq();
                break;
            case NetMsgType.Error:
            case NetMsgType.Disconnect:
                //Disconnect(netMsg);
                break;
            case NetMsgType.Debug:
//#if UNITY_EDITOR
                //if (netMsg.cmd != (uint)ProtoCmdId.HeartbeatAck 
                //    && netMsg.cmd != (uint)ProtoCmdId.HeartbeatReq)
                //{
                //    var debugStr = Encoding.UTF8.GetString(netMsg.Data);
                //    Odin.Log.Info("Network Debug :{0}", debugStr);
                //}
//#endif
                break;
            default:
                //Odin.Log.Error("Unkown MsgType:" + netMsg.MsgType);
                break;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (adapter != null)
        {
            adapter.Update();
        }
    }
}
