using UnityEngine;
using SuperSocket.ProtoBase;
using System;

public class BinaryRequestInfo : IPackageInfo<int>
{
    public BinaryRequestInfo()
    {
    }

    public int Key { get; protected set; }
}
