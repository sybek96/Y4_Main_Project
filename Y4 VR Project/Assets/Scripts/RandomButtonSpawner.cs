using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//===================== Random Button Spawner =================================
//
// A simple class that generates one of positions for the button to alter 
// throughout the experiment
//
//=============================================================================

public class RandomButtonSpawner : MonoBehaviour {

    public Transform buttonTransform;
	// Use this for initialization
	void Start () {
        int rand = Random.Range(0, 14);
        if (rand > 0 && rand <= 2)
        {
            buttonTransform.position = new Vector3(-3.3472f, 1.1648f, 4.49f);
            buttonTransform.rotation = Quaternion.Euler(-90f, 0.0f, 0.0f);
        }
        else if (rand > 2 && rand <= 4)
        {
            buttonTransform.position = new Vector3(1.185f, 0.832f, 4.49f);
            buttonTransform.rotation = Quaternion.Euler(-90f, 0.0f, 0.0f);
        }
        else if (rand > 4 && rand <= 6)
        {
            buttonTransform.position = new Vector3(4.534f, 1.667f, 2.497f);
            buttonTransform.rotation = Quaternion.Euler(-90f, 0.0f, 90.0f);
        }
        else if (rand > 6 && rand <= 8)
        {
            buttonTransform.position = new Vector3(4.534f, 0.334f, -0.4836f);
            buttonTransform.rotation = Quaternion.Euler(-90f, 0.0f, 90.0f);
        }
        else if (rand > 8 && rand <= 10)
        {
            buttonTransform.position = new Vector3(0.594f, 1.9985f, -4.485f);
            buttonTransform.rotation = Quaternion.Euler(-90f, 0.0f, -180.0f);
        }
        else if (rand > 10 && rand <= 12)
        {
            buttonTransform.position = new Vector3(2.0453f, 0.83f, -4.485f);
            buttonTransform.rotation = Quaternion.Euler(-90f, 0.0f, -180.0f);
        }
        else if (rand > 12 && rand <= 14)
        {
            buttonTransform.position = new Vector3(-4.456f, 1.166f, -1.80606f);
            buttonTransform.rotation = Quaternion.Euler(-90f, 0.0f, -90.0f);
        }
        Debug.Log(rand);
    }
}
