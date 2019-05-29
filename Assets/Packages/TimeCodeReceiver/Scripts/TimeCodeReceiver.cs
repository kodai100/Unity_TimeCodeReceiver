using Lasp;
using System;
using UnityEngine;

[System.Serializable]
public class TimeCodeReceiver
{
    
    public int deviceSampleRate = 44100;

    public int bitThreshold = 44100 / 3100; // long or not bits (11)
    
    private int sameAudioLevelCount = 0;
    private int prevSign = 0;
    private int lastBitCount = 0;
    private string bitPattern = "";

    readonly string SYNC_WORD = "0011111111111101"; 

    public Action<string> OnReceiveTimecode;

    // 現在までのオーディオ入力を取得しフレーム情報にデコードしていく
    public void DecodeAudioToTCFrames()
    {
        float[] waveData = new float[1024];
        MasterInput.RetrieveWaveform(FilterType.Bypass, waveData);

        int pos = 0;

        while (pos < waveData.Length)
        {

            int count = CheckAudioSign(waveData, ref pos);

            if (count > 0)
            {
                if (count < bitThreshold)
                {
                    // 「レベル変化までが短い」パターンが2回続くと1
                    if (lastBitCount < bitThreshold)
                    {
                        bitPattern += "1";
                        lastBitCount = bitThreshold; // 次はここを通らないように
                    }
                    else
                    {
                        lastBitCount = count;
                    }
                }
                else
                {
                    // Long pattern
                    bitPattern += "0";
                    lastBitCount = count;
                }
            }
        }

        // Almost 1frame length ?
        if (bitPattern.Length >= 80)
        {
            // Finc sync word
            int syncWordIndex = bitPattern.IndexOf(SYNC_WORD);

            if (syncWordIndex > 0)
            {
                // get all bits including sync word
                string timeCodeBits = bitPattern.Substring(0, syncWordIndex + 16);
                
                if (timeCodeBits.Length >= 80)
                {
                    // get last 80 bits (Timecode signal group)
                    timeCodeBits = timeCodeBits.Substring(timeCodeBits.Length - 80);
                    Debug.Log($"Timecode bots : {timeCodeBits}");

                    OnReceiveTimecode(StringToBitDecoder.decodeBitsToFrame(timeCodeBits));
                }

                bitPattern = bitPattern.Substring(syncWordIndex + 16);
            }
        }

        // パターンマッチしなさすぎてビットパターンバッファ長くなっちゃったら削る
        if (bitPattern.Length > 160)
        {
            bitPattern = bitPattern.Substring(80);
        }
    }

    int CheckAudioSign(float[] data, ref int pos)
    {

        // Loop all data
        while (pos < data.Length)
        {
            int currentSign = Mathf.RoundToInt(Mathf.Sign(data[pos]));

            // Flipped or not
            if (IsFlipped(prevSign, currentSign))
            {
                int count = sameAudioLevelCount;
                sameAudioLevelCount = 0;

                prevSign = currentSign;

                return count;

            }
            else
            {
                // Same level
                sameAudioLevelCount++;
            }

            pos += 1;
        }

        // Final bit
        return -1;
    }

    bool IsFlipped(int prev, int current)
    {
        return prev != current;
    }
}
