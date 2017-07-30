using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InjuredCrew : MonoBehaviour
{
    public float x;
    public float y;

    public int tileX;
    public int tileY;

    public FrameAnimator fireAnim;
    public bool onFire = false;

    public void SetOnFire()
    {
        // only do this once
        if (onFire)
            return;

        GameObject.Find("AudioShortFire").GetComponent<AudioSource>().Play();
        fireAnim.enabled = true;

        // i'm not sure why this is necessary exactly but no time to debug
        gameObject.transform.localScale = new Vector3(100f, 100f, 1f);

        gameObject.transform.position += new Vector3(0f, -32f, 0f);
        onFire = true;
    }

    public void Pickup()
    {
        var rend = this.GetComponent<SpriteRenderer>();
        Destroy(rend);
        Destroy(this.gameObject);
    }
}
