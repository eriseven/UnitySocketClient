using UnityEngine;
using SuperSocket.ProtoBase;
using System;

public class BinaryRequestInfo : IPackageInfo<int>
{
    public BinaryRequestInfo(byte[] data)
    {
        this.Data = data;
    }

    public int Key { get; protected set; }
    public byte[] Data { get; protected set; }
}
