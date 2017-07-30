using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrameAnimator : MonoBehaviour
{
    public Texture2D[] textures;
    public int[] frameTime;

    private SpriteRenderer render;

    public bool loop = true;
    public float frameStartTime;
    private int currentFrame = 0;

    // Use this for initialization
    void Start()
    {
        currentFrame = 0;
        render = this.gameObject.GetComponent<SpriteRenderer>();
        frameStartTime = Time.realtimeSinceStartup;
        render.sprite = Sprite.Create(textures[currentFrame], new Rect(Vector2.zero, new Vector2(64, 64)), new Vector2(0.5f, 0f));
    }

    // Update is called once per frame
    void Update()
    {
        var timeNow = Time.realtimeSinceStartup;

        if ((timeNow - frameStartTime) * 1000f > frameTime[currentFrame])
        {
            currentFrame++;
            frameStartTime = timeNow;

            if (currentFrame >= textures.Length)
            {
                if (loop)
                    currentFrame = 0;
                else
                    currentFrame--;
            }

            Destroy(render.sprite);
            render.sprite = Sprite.Create(textures[currentFrame], new Rect(Vector2.zero, new Vector2(64, 64)), new Vector2(0.5f, 0f));
        }
    }
}
