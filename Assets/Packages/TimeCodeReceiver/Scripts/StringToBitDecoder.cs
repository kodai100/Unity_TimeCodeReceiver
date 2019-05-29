using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StringToBitDecoder
{
    // ---------------------------------------------------------------------------------
    // フレームデコード
    static int decode1Bit(string b, int pos)
    {
        return int.Parse(b.Substring(pos, 1));
    }

    static int decode2Bits(string b, int pos)
    {
        int r = 0;
        r = r + decode1Bit(b, pos);
        r = r + decode1Bit(b, pos + 1) * 2;
        return r;
    }

    static int decode3Bits(string b, int pos)
    {
        int r = 0;
        r = r + decode1Bit(b, pos);
        r = r + decode1Bit(b, pos + 1) * 2;
        r = r + decode1Bit(b, pos + 2) * 4;
        return r;
    }

    static int decode4Bits(string b, int pos)
    {
        int r = 0;
        r = r + decode1Bit(b, pos);
        r = r + decode1Bit(b, pos + 1) * 2;
        r = r + decode1Bit(b, pos + 2) * 4;
        r = r + decode1Bit(b, pos + 3) * 8;
        return r;
    }

    public static string decodeBitsToFrame(string bits)
    {
        // https://en.wikipedia.org/wiki/Linear_timecode

        int frames = decode4Bits(bits, 0) + decode2Bits(bits, 8) * 10;
        int secs = decode4Bits(bits, 16) + decode3Bits(bits, 24) * 10;
        int mins = decode4Bits(bits, 32) + decode3Bits(bits, 40) * 10;
        int hours = decode4Bits(bits, 48) + decode2Bits(bits, 56) * 10;

        return $"{hours}:{mins}:{secs};{frames}";
    }
}
