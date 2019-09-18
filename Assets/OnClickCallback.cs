using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OnClickCallback : MonoBehaviour
{
    public delegate void Callback ();
    public Callback callback;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnMouseOver()
    {
        if (Input.GetMouseButtonDown(0))
        {
            callback();
        }
    }
}
