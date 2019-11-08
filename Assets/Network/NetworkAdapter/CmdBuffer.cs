using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace MMOGame
{
    public class CmdBuffer: ByteBuffer
    {
        public CmdBuffer() : base() { }
        public CmdBuffer(byte[] data) : base(data) { }
        public void EncodeCmd(ushort cmdId, LuaByteBuffer cmd)
        {
            //ushort cmdLen = (ushort)(cmd.buffer.Length + 4);
            //WriteShort(cmdLen);
            WriteShort(cmdId);
            WriteBytes(cmd.buffer);
        }

        public int DecodeCmd(out byte[] cmd)
        {
            //int cmdLen = ReadShort();
            int cmdId = ReadShort();
            cmd = reader.ReadBytes((int)(stream.Length - stream.Position));
            //cmd = new LuaByteBuffer(buffer);
            return cmdId;
        }
    }

    public struct LuaByteBuffer
    {
        public LuaByteBuffer(IntPtr source, int len)
            : this()
        {
            buffer = new byte[len];
            Length = len;
            Marshal.Copy(source, buffer, 0, len);
        }

        public LuaByteBuffer(byte[] buf)
            : this()
        {
            buffer = buf;
            Length = buf.Length;
        }

        public LuaByteBuffer(byte[] buf, int len)
            : this()
        {
            buffer = buf;
            Length = len;
        }

        public LuaByteBuffer(System.IO.MemoryStream stream)
            : this()
        {
            buffer = stream.GetBuffer();
            Length = (int)stream.Length;
        }

        public static implicit operator LuaByteBuffer(System.IO.MemoryStream stream)
        {
            return new LuaByteBuffer(stream);
        }

        public byte[] buffer;

        public int Length
        {
            get;
            private set;
        }
    }
}
