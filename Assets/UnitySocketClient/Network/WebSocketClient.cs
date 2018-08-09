using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using WebSocket4Net;

namespace UnitySocketClient
{
    public class WebSocketClient : Socket
    {
        WebSocket client;

        string uri = "";

        public WebSocketClient(string uri)
        {
            this.uri = uri;
            client = new WebSocket(uri);
            client.Opened += new EventHandler(ConnectedHandler);
            client.Error += new EventHandler<SuperSocket.ClientEngine.ErrorEventArgs>(ErrorHandler);
            client.Closed += new EventHandler(ClosedHandler);
            client.MessageReceived += new EventHandler<MessageReceivedEventArgs>(MessageHandler);
            client.DataReceived += new EventHandler<DataReceivedEventArgs>(DataHandler);
        }


        public override void Close()
        {
            if (client.State == WebSocketState.Closed || client.State == WebSocketState.Closing)
            {
                return;
            }
            client.Close();
        }

        public override void Connect()
        {
            if (client.State == WebSocketState.Open || client.State == WebSocketState.Connecting)
            {
                client.Close();
            }

            client.Open();
        }

        void ConnectedHandler(object sender, EventArgs e)
        {
            OnConnected(e);
            //Debug.Log("ConnectedHandler");
        }

        void ClosedHandler(object sender, EventArgs e)
        {
            OnClosed(e);
            //Debug.Log("ClosedHandler");
        }

        void ErrorHandler(object sender, SuperSocket.ClientEngine.ErrorEventArgs e)
        {
            OnError(e);
            //Debug.Log("ErrorHandler");
        }

        void MessageHandler(object sender, MessageReceivedEventArgs e)
        {
            OnMessageReceived(e);
            //Debug.Log("MessageHandler");
        }

        void DataHandler(object sender, DataReceivedEventArgs e)
        {
            OnDataReceived(e);
        }

        public override void SendData(byte[] data)
        {
            if (client.State == WebSocketState.Open)
            {
                client.Send(data, 0, data.Length);
            }
        }

        public override void SendMessage(string message)
        {
            if (client.State == WebSocketState.Open)
            {
                client.Send(message);
            }
        }

        protected override void ReleaseSocket()
        {
            base.ReleaseSocket();
            client.Dispose();
            client = null;
        }

    }
}

