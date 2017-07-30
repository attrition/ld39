using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InjuredCrew : MonoBehaviour
{
    public float x;
    public float y;

    public int tileX;
    public int tileY;

    public void Pickup()
    {
        var rend = this.GetComponent<SpriteRenderer>();
        Destroy(rend);
        Destroy(this.gameObject);
    }
}
