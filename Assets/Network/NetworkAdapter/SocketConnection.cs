using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MMOGame
{
        public class SocketConnection {
            TcpSocketClient socket = new TcpSocketClient();
            public int socketId { get { return socket.Id; } }
        }

}

