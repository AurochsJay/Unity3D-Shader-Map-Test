using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RectIntTest : MonoBehaviour
{
    public RectInt rectangle = new RectInt(3, 3, 10, 10);


    void Start()
    {
        transform.position = new Vector3Int(rectangle.x, 0, rectangle.y);
        transform.localScale = new Vector3Int(rectangle.width, (int)transform.localScale.y, rectangle.height);

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
