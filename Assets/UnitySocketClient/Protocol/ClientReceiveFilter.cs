using SuperSocket.ProtoBase;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class ClientReceiveFilter : FixedHeaderReceiveFilter<BinaryRequestInfo>
{
    public ClientReceiveFilter() : base(2)
    {
    }

    int GetBodyLength(byte[] length)
    {
        return (int)length[0] * 256 + (int)length[1];
    }

    public override BinaryRequestInfo ResolvePackage(IBufferStream bufferStream)
    {

        byte[] header = new byte[HeaderSize];
        bufferStream.Read(header, 0, HeaderSize);

        int bodyLength = Size - HeaderSize;

        byte[] data = new byte[bodyLength];
        bufferStream.Read(data, 0, bodyLength);

        //BinaryRequestInfo requestInfo = new BinaryRequestInfo(Encoding.UTF8.GetString(data1), data1);

        //return requestInfo;
        return new BinaryRequestInfo(data);
    }

    protected override int GetBodyLengthFromHeader(IBufferStream bufferStream, int length)
    {
        int bodyLength = bufferStream.ReadUInt16();
        return bodyLength;
    }
}
