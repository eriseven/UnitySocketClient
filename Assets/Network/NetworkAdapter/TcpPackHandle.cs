

//using System;
//using System.IO;

//public class TcpPackHandle
//{
//    private MemoryStream DecodeMs;
//    private BinaryReader DecodeBr;

//    private MemoryStream EncodeMs;
//    private BinaryWriter EncodeBw;

//    private UInt16 packId = 0;
//    private UInt16 PackId
//    {
//        get { return packId++; }
//    }

//    public TcpPackHandle()
//    {
//        DecodeMs = new MemoryStream();
//        DecodeBr = new BinaryReader(DecodeMs);

//        EncodeMs = new MemoryStream();
//        EncodeBw = new BinaryWriter(EncodeMs);
//    }

//    protected long RemainingBytes()
//    {
//        return DecodeMs.Length - DecodeMs.Position;
//    }


//    //解包头部分
//    private virtual byte[] DecodeHead(byte[] input, int length)
//    {
//        return null;
//    }

//    //组包头部分
//    public byte[] Encode(byte[] input, int length)
//    {
//        UInt16 len = Converter.GetLittleEndian((UInt16)(length + 14));

//        Byte flag = 0;
//        UInt32 cmd = 0;
//        UInt16 seqId = PackId;
//        UInt32 sessionId = 0;
//        UInt64 uid = 0;

//        EncodeMs.Position = 0;

//        EncodeBw.Write(len);
//        EncodeBw.Write(flag);
//        EncodeBw.Write(cmd);
//        EncodeBw.Write(seqId);
//        EncodeBw.Write(sessionId);
//        EncodeBw.Write(uid);

//        EncodeBw.Write(input);
//        EncodeBw.Flush();
//        return EncodeMs.ToArray();
//    }

//    public byte[] Decode(byte[] input, int length)
//    {
//        DecodeMs.Seek(0, SeekOrigin.End);
//        DecodeMs.Write(input, 0, length);
//        //Reset to beginning
//        DecodeMs.Seek(0, SeekOrigin.Begin);

//        byte[] result = null;
//        while (RemainingBytes() >= 4)
//        {
//            uint messageLen = DecodeBr.ReadUInt32();
//            // messageLen -= 2;
//            if (RemainingBytes() >= messageLen)
//            {
//                MemoryStream ms = new MemoryStream();
//                BinaryWriter writer = new BinaryWriter(ms);
//                writer.Write(DecodeBr.ReadBytes((int)messageLen));
//                ms.Seek(0, SeekOrigin.Begin);
//                result = ms.ToArray();
//                //OnReceivedMessage(ms);
//            }
//            else
//            {
//                //Back up the position four bytes
//                DecodeMs.Position = DecodeMs.Position - 4;
//                break;
//            }
//        }

//        byte[] leftover = DecodeBr.ReadBytes((int)RemainingBytes());
//        DecodeMs.SetLength(0);     //Clear
//        DecodeMs.Write(leftover, 0, leftover.Length);

//        return result;
//    }
//}