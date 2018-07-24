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
        //byte[] length1 = new byte[2];
        //bufferStream.Read(length1, 0, 2);
        //int bodyLength = GetBodyLength(length1);

        //byte[] data1 = new byte[bodyLength];
        //bufferStream.Read(data1, 0, bodyLength);

        //BinaryRequestInfo requestInfo = new BinaryRequestInfo(Encoding.UTF8.GetString(data1), data1);

        //return requestInfo;
        return new BinaryRequestInfo();
    }

    protected override int GetBodyLengthFromHeader(IBufferStream bufferStream, int length)
    {
        int bodyLength = bufferStream.ReadUInt16();
        return bodyLength;
    }
}
