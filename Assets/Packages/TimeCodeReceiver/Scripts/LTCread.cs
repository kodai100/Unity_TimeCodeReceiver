// LTC Timecode Reader for Unity C#
// http://blog.mobilehackerz.jp/
// https://twitter.com/MobileHackerz

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LTCread : MonoBehaviour
{
    // LTCを入力するオーディオデバイスの設定
    public string deviceName;
    public int deviceSampleRate = 44100;

    // デコードしたタイムコード
    public string timeCode = "";

    // 内部で録音するバッファの長さ
    private int deviceRecLength = 10;

    //
    private AudioClip ltcAudioInput;
    private int lastAudioPos = 0;
    private int sameAudioLevelCount = 0;
    private int lastAudioLevel = 0;
    private int lastBitCount = 0;
    private string bitPattern = "";

    private GUIStyle timeCodeStyle;



    // ---------------------------------------------------------------------------------
    // Use this for initialization
    void Start()
    {
        string targetDevice = "";
       
        foreach (var device in Microphone.devices)
        {
            Debug.Log(device);
            if (device.Contains(deviceName))
            {
                targetDevice = device;
            }
        }

        Debug.Log(targetDevice);
        ltcAudioInput = Microphone.Start(targetDevice, true, deviceRecLength, deviceSampleRate);
        //
        timeCodeStyle = new GUIStyle();
        timeCodeStyle.fontSize = 64;
        timeCodeStyle.normal.textColor = Color.white;
    }

    // Update is called once per frame
    void Update()
    {
        // これを毎フレーム呼ぶ
        decodeAudioToTCFrames();
    }

    private void OnGUI()
    {
        GUI.Label(new Rect(0, 0, 200, 100), timeCode, timeCodeStyle);
    }



    // ---------------------------------------------------------------------------------
    // フレームデコード
    int decode1Bit(string b, int pos)
    {
        return int.Parse(b.Substring(pos, 1));
    }
    int decode2Bits(string b, int pos)
    {
        int r = 0;
        r = r + decode1Bit(b, pos);
        r = r + decode1Bit(b, pos + 1) * 2;
        return r;
    }
    int decode3Bits(string b, int pos)
    {
        int r = 0;
        r = r + decode1Bit(b, pos);
        r = r + decode1Bit(b, pos + 1) * 2;
        r = r + decode1Bit(b, pos + 2) * 4;
        return r;
    }
    int decode4Bits(string b, int pos)
    {
        int r = 0;
        r = r + decode1Bit(b, pos);
        r = r + decode1Bit(b, pos + 1) * 2;
        r = r + decode1Bit(b, pos + 2) * 4;
        r = r + decode1Bit(b, pos + 3) * 8;
        return r;
    }
    string decodeBitsToFrame(string bits)
    {
        // https://en.wikipedia.org/wiki/Linear_timecode

        int frames = decode4Bits(bits, 0) + decode2Bits(bits, 8) * 10;
        int secs = decode4Bits(bits, 16) + decode3Bits(bits, 24) * 10;
        int mins = decode4Bits(bits, 32) + decode3Bits(bits, 40) * 10;
        int hours = decode4Bits(bits, 48) + decode2Bits(bits, 56) * 10;

        return string.Format("{0:D2}:{1:D2}:{2:D2};{3:D2}", hours, mins, secs, frames);
    }

    // 現在までのオーディオ入力を取得しフレーム情報にデコードしていく
    void decodeAudioToTCFrames()
    {
        float[] waveData = getUpdatedAudio(ltcAudioInput);
        int pos = 0;

        int bitThreshold = ltcAudioInput.frequency / 3100; // 適当
        while (pos < waveData.Length)
        {
            int count = checkAudioLevelChanged(waveData, ref pos, ltcAudioInput.channels);
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
                    // 「レベル変化までが長い」パターンは0
                    bitPattern += "0";
                    lastBitCount = count;
                }
            }
        }

        // 1フレームぶん取れたかな？
        if (bitPattern.Length >= 80)
        {
            int bpos = bitPattern.IndexOf("0011111111111101"); // SYNC WORD
            if (bpos > 0)
            {
                string timeCodeBits = bitPattern.Substring(0, bpos + 16);
                bitPattern = bitPattern.Substring(bpos + 16);
                if (timeCodeBits.Length >= 80)
                {
                    timeCodeBits = timeCodeBits.Substring(timeCodeBits.Length - 80);
                    timeCode = decodeBitsToFrame(timeCodeBits);
                }
            }
        }

        // パターンマッチしなさすぎてビットパターンバッファ長くなっちゃったら削る
        if (bitPattern.Length > 160)
        {
            bitPattern = bitPattern.Substring(80);
        }
    }


    // マイク入力から録音データの生データを得る。
    // オーディオ入力が進んだぶんだけ処理して float[] に返す
    float[] getUpdatedAudio(AudioClip aud)
    {
        int maxAudioPos = aud.samples * aud.channels;
        int nowAudioPos = Microphone.GetPosition(aud.name);
        int audioCount = 0;
        float[] waveData = new float[0];

        if (lastAudioPos < nowAudioPos)
        {
            audioCount = nowAudioPos - lastAudioPos;
            waveData = new float[audioCount];
            aud.GetData(waveData, lastAudioPos);
        }
        else if (lastAudioPos > nowAudioPos)
        {
            int audioCount1 = maxAudioPos - lastAudioPos;
            int audioCount2 = nowAudioPos;
            audioCount = audioCount1 + audioCount2;
            waveData = new float[audioCount];

            float[] wave1 = new float[audioCount1];
            aud.GetData(wave1, lastAudioPos);
            float[] wave2 = new float[audioCount2];
            aud.GetData(wave2, 0);
            wave1.CopyTo(waveData, 0);
            wave2.CopyTo(waveData, audioCount1);
        }
        lastAudioPos = nowAudioPos;

        return waveData;
    }

    // 録音データの生データから、0<1, 1>0の変化が発生するまでのカウント数を得る。
    // もしデータの最後に到達したら-1を返す。
    int checkAudioLevelChanged(float[] data, ref int pos, int channels)
    {
        while (pos < data.Length)
        {
            int nowLevel = Mathf.RoundToInt(Mathf.Sign(data[pos]));
            if (lastAudioLevel != nowLevel)
            {
                // レベル変化があった
                int count = sameAudioLevelCount;
                sameAudioLevelCount = 0;
                lastAudioLevel = nowLevel;
                return count;

            }
            else
            {
                // 同じレベルだった
                sameAudioLevelCount++;
            }
            pos += channels;
        }
        return -1;
    }

}