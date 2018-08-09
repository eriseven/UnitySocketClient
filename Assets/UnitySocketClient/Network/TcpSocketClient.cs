using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SuperSocket.ClientEngine;
using System.Net;
using System;

namespace UnitySocketClient
{
    public class TcpSocketClient : Socket
    {
        EndPoint endPoint;
        EasyClient client;

        public TcpSocketClient(string host, int port)
        {
            endPoint = new DnsEndPoint(host, port);
            client = new EasyClient();

            client.Initialize(new ClientReceiveFilter(), OnReceiveHandler);
            client.Connected += ConnectedHandler;
            client.Closed += ClosedHandler;
            client.Error += ErrorHandler;
        }

        void OnReceiveHandler(BinaryRequestInfo package)
        {
            var e = new WebSocket4Net.DataReceivedEventArgs(package.Data);
            OnDataReceived(e);
        }


        public override void Close()
        {
            if (client.IsConnected)
            {
                client.Close();
            }
        }

        public override void Connect()
        {
            if (client.IsConnected)
            {
                return;
            }

            client.BeginConnect(endPoint);
        }

        void ConnectedHandler(object sender, EventArgs e)
        {
            OnConnected(e);
        }


        void ClosedHandler(object sender, EventArgs e)
        {
            OnClosed(e);
        }


        void ErrorHandler(object sender, SuperSocket.ClientEngine.ErrorEventArgs e)
        {
            OnError(e);
        }

        public override void SendData(byte[] data)
        {
            if (client.IsConnected)
            {
                client.Send(data);
            }
        }

        public override void SendMessage(string message)
        {
        }

        protected override void ReleaseSocket()
        {
            base.ReleaseSocket();
            client.Connected -= ConnectedHandler;
            client.Closed -= ClosedHandler;
            client.Error -= ErrorHandler;

            if (client.IsConnected)
            {
                client.Close();
            }

            client = null;
        }
    }
}

