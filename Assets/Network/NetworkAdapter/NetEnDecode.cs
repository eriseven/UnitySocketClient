using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public interface NetEnDecode
{
    //bool Decode(MemoryStream bw, NetRespEvent respEventCb);
    bool Decode(BinaryReader bw, NetRespData netRespData);
    void Encode(BinaryWriter bw, UInt32 cmd, byte packId, object data);
}
