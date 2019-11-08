using System;
using System.IO;

public class TcpEnDecode : NetEnDecode
{
    private const byte PackLenSize = 2;
    private const byte PackHeadSize = 12;
    private const uint MaxPackSize = 32768;

    //private readonly ProtoBufCsSerializer _protoBufSerializer = new ProtoBufCsSerializer();

    private void MoveData(BinaryReader br)
    {
        var ms = br.BaseStream;
        var len = ms.Length - ms.Position;
        var b = br.ReadBytes((int)len);
        ms.SetLength(len);
        ms.Seek(0, SeekOrigin.Begin);
        ms.Write(b, 0, (int)len);
    }

    public bool Decode(BinaryReader br, NetRespData netRespData)
    {
        var ms = br.BaseStream;
        ms.Seek(0, SeekOrigin.Begin);
        while (true)
        {
            //判断data长度是否少于包头长度
            var dataLen = ms.Length - ms.Position;

            if (dataLen == 0)
            {
                ms.SetLength(0);
                ms.Position = 0;
                return true;
            }

            if (dataLen < PackHeadSize)
            {
                if (ms.Position > 0) MoveData(br);
                return true;
            }

            //len(2)
            var bodySize = br.ReadUInt16();
            if (bodySize > MaxPackSize)
            {
                //数据有误
                return false;
            }
            //判断data长度是否少于包体长度
            if (dataLen < bodySize)
            {
                //- dataSize = br.ReadUInt16();
                ms.Position -= PackLenSize;
                if(ms.Position > 0)MoveData(br);
                return true;
            }

            //flag(1)
            ms.Position++;
            //packId
            ms.Position++;
            //cmd(4)
            var cmd = br.ReadUInt32();
            //SessionId(4)
            ms.Position += 4;

            if (bodySize == PackHeadSize)
            {
                //只有cmd
                netRespData(cmd, null);
            }
            else
            {
                var data = br.ReadBytes(bodySize - PackHeadSize);
                netRespData(cmd, data);
            }
        }
    }

    public void Encode(BinaryWriter bw, UInt32 cmd, Byte packId, object data)
    {
        UInt16 dataSize = PackHeadSize;
        bw.BaseStream.SetLength(dataSize);
        if (data != null)
        {
            bw.Seek(PackHeadSize, SeekOrigin.Begin);
            //_protoBufSerializer.Serialize(bw.BaseStream, data);
            dataSize = (UInt16)bw.BaseStream.Length;
        }
        NetEncode(bw, cmd, dataSize, packId, 0);
    }

    private void NetEncode(BinaryWriter bw, UInt32 cmd, UInt16 dataSize, Byte packId, Byte flag)
    {
        bw.Seek(0, SeekOrigin.Begin);

        bw.Write(dataSize);
        bw.Write(flag);
        bw.Write(packId);
        bw.Write(cmd);
        bw.Write((UInt32)0);
    }
}

