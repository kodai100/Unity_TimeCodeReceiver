// LTC Timecode Reader for Unity C#
// http://blog.mobilehackerz.jp/
// https://twitter.com/MobileHackerz

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeCodeManager : MonoBehaviour
{
    

    // デコードしたタイムコード
    public string timeCode = "";

    TimeCodeReceiver timeCodeReceiver = new TimeCodeReceiver();
    

    private GUIStyle timeCodeStyle;
    
    // ---------------------------------------------------------------------------------
    // Use this for initialization
    void Start()
    {
        
        timeCodeStyle = new GUIStyle();
        timeCodeStyle.fontSize = 64;
        timeCodeStyle.normal.textColor = Color.white;

        timeCodeReceiver.OnReceiveTimecode += (timecode) =>
        {
            timeCode = timecode;
        };
    }

    private void Update()
    {
        timeCodeReceiver.DecodeAudioToTCFrames();
    }

    private void OnGUI()
    {
        GUI.Label(new Rect(0, 0, 200, 100), timeCode, timeCodeStyle);
    }
    

}