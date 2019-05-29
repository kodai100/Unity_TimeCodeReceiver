using Lasp;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveForm : MonoBehaviour
{

    List<Transform> objects = new List<Transform>();

    // Start is called before the first frame update
    void Start()
    {
        for(int i = 0; i < 1024; i++)
        {
            var o = GameObject.CreatePrimitive(PrimitiveType.Cube);
            o.transform.position = new Vector3(i, 0, 0);

            objects.Add(o.transform);
        }
        
    }

    // Update is called once per frame
    void Update()
    {
        float[] waveData = new float[1024];
        MasterInput.RetrieveWaveform(FilterType.Bypass, waveData);

        for (int i = 0; i < 1024; i++)
        {
            objects[i].position = new Vector3(objects[i].position.x, waveData[i], 0);
        }
        
    }
}
