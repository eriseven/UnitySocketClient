using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class SocketReConnect : MonoBehaviour
{

    /// <summary>
    /// 最大重连次数
    /// </summary>
    private uint _maxReConnNum;

    /// <summary>
    /// 已重连次数
    /// </summary>
    private uint _curReConnNum;

    /// <summary>
    /// 已等待重连时长
    /// </summary>
    private float currenWaitTime;

    /// <summary>
    /// 重连时间间隔 单位毫秒
    /// </summary>
    private uint[] _reConnIntervals;

    /// <summary>
    /// 是否初始化
    /// </summary>
    private bool _isInit = false;

    /// <summary>
    /// 是否开启重连计时
    /// </summary>
    private bool isStartReconnTiming = false;

    /// <summary>
    /// 是否已开始重连中
    /// </summary>
    private bool _isStartReConnect = false;

    private Action _connect;
    private Action _reConnectFailCb;
    private NetworkAdapter _networkAdapter;

    /// <summary>
    /// 连接失败
    /// </summary>
    private bool _reConnectFail;

    // Start is called before the first frame update
    void Awake()
    {
        _isInit = false;
        _curReConnNum = 0;
        currenWaitTime = 0;
        _isStartReConnect = false;
        isStartReconnTiming = false;
    }

    void Update()
    {
        if (!_isInit) return;
        if (!isStartReconnTiming) return;

        currenWaitTime += Time.deltaTime;
        try
        {
            if (_reConnIntervals[_curReConnNum - 1] <= currenWaitTime)
            {
                currenWaitTime = 0;
                isStartReconnTiming = false;
                ReConnect();
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            //Odin.Log.Error( "_curReConnNum:" + _curReConnNum);
        }
       
    }

    /// <summary>
    /// 有重连结果后 调用
    /// </summary>
    /// <returns>反回重连次数是否已用完</returns>
    private void ConnRespResult(NetMsg netMsg)
    {
        if (netMsg.MsgType != NetMsgType.Error && 
            netMsg.MsgType != NetMsgType.Disconnect
            && netMsg.MsgType != NetMsgType.Connect)
        {
            return;
        }
        _networkAdapter.DelNetRespEvent(ConnRespResult);

        if (netMsg.MsgType == NetMsgType.Connect)
        {
            _curReConnNum = 0;
            _reConnectFail = false;
            _isStartReConnect = false;
            isStartReconnTiming = false;
            //Odin.Log.Info("ReConnect Succ");
            return;
        }

        //Odin.Log.Error("ReConnect:{0} Fail", _curReConnNum);

        _curReConnNum++;
        if (_curReConnNum < _maxReConnNum)
        {
            //重连失败开启重连计时
            isStartReconnTiming = true;
            return ;
        }

        _curReConnNum = 0;
        _isStartReConnect = false;

        _reConnectFail = true;
        _reConnectFailCb();
    }

    public void ResetData()
    {
        _reConnectFail = false;
    }


    public bool StartReConnect()
    {
        //正在重连中 或 连接失败
        if (_isStartReConnect || _reConnectFail)
        {
            return false;
        }
        _isStartReConnect = true;
        ReConnect();
        return true;
    }

    private void ReConnect()
    {
        _networkAdapter.AddNetRespEvent(ConnRespResult);
        Debug.LogWarningFormat("Start ReConnect:{0}", _curReConnNum);
        _networkAdapter.ReConnect();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="networkAdapter"></param>
    /// <param name="connFailCb"></param>
    /// <param name="reConnInterval">单位秒</param>
    /// <returns></returns>
    public bool Init(NetworkAdapter networkAdapter, Action connFailCb, uint[] reConnInterval = null)
    {
        if (_isInit)
        {
            return true;
        }
        _isInit = true;

        if (networkAdapter == null)
        {
            return false;
        }

        _reConnectFailCb = connFailCb;

        _networkAdapter = networkAdapter;

        if (reConnInterval == null)
        {
            reConnInterval = new uint[] { 1, 1, 1 };
        }

        _reConnIntervals = reConnInterval;
        _maxReConnNum = (uint)_reConnIntervals.Length;

        return true;
    }
}
