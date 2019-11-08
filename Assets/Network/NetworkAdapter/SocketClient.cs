
using System;
using System.IO;

public interface SocketClient
{
    int Id { get; }

    string Name { get; }

    bool IsConnected();

    bool Close(string reason);

    void ReConnect();

    void Connect(string host, int port);

    bool SendMessage(UInt32 cmd, object data);

   
}

