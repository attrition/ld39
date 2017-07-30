using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public Map map;
    public Camera followCamera;

    public FrameAnimator idleAnim;
    public FrameAnimator walkAnim;
    public FrameAnimator fireAnim;
    public GameObject carryAnimObject;

    public float x;
    public float y;

    public int tileX;
    public int tileY;

    public bool carrying;
    public bool onFire; //shitshitshitshitshit

    public PlayerOnFire onFireFunc;
    public PlayerEscaped escapedFunc;

    public CrewmanRescued crewRescuedFunc;
    public CrewmanPickedUp crewPickedUpFunc;

    // Use this for initialization
    void Start()
    {
        this.gameObject.transform.position = new Vector3(x, y, -1f);

        carryAnimObject = GameObject.Instantiate(
            Resources.Load("Prefabs/CarriedCrew") as GameObject,
            this.gameObject.transform);
        carryAnimObject.gameObject.transform.position += new Vector3(0f, 0f, -0.5f);
        carryAnimObject.GetComponent<SpriteRenderer>().enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (map == null)
            return;

        if (onFire)
        {
            if (!fireAnim.enabled && (idleAnim.enabled || walkAnim.enabled))
            {
                idleAnim.enabled = false;
                walkAnim.enabled = false;
                fireAnim.enabled = true;
            }

            return;
        }

        // perform any action we're currently capable of
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ActivateNearby();

            if (carrying)
            {
                if (!carryAnimObject.GetComponent<SpriteRenderer>().enabled)
                    carryAnimObject.GetComponent<SpriteRenderer>().enabled = true;
            }
            else
            {
                if (carryAnimObject.GetComponent<SpriteRenderer>().enabled)
                    carryAnimObject.GetComponent<SpriteRenderer>().enabled = false;
            }
        }

        var moveX = 0f;
        var moveY = 0f;
        var speed = 250f;

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            moveY += speed;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            moveY -= speed;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            moveX -= speed;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            moveX += speed;
        if (Input.GetKey(KeyCode.Escape))
            Application.Quit();

        moveX *= Time.deltaTime;
        moveY *= Time.deltaTime;

        // we check different corners depending on movement directions
        var cornerChecks = new List<Vector2>();
        // which movements to disable if we hit a wall
        var blockMovement = new List<Vector2>();

        if (moveX < 0) // left
        {
            cornerChecks.Add(new Vector2(-16f, 3f));
            cornerChecks.Add(new Vector2(-16f, 47f));
            blockMovement.Add(new Vector2(0f, 1f));
            blockMovement.Add(new Vector2(0f, 1f));
        }
        if (moveX > 0) // right
        {
            cornerChecks.Add(new Vector2(16f, 3f));
            cornerChecks.Add(new Vector2(16f, 47f));
            blockMovement.Add(new Vector2(0f, 1f));
            blockMovement.Add(new Vector2(0f, 1f));
        }
        if (moveY < 0) // down
        {
            cornerChecks.Add(new Vector2(-16f, 0f));
            cornerChecks.Add(new Vector2(16f, 0f));
            blockMovement.Add(new Vector2(1f, 0f));
            blockMovement.Add(new Vector2(1f, 0f));
        }
        if (moveY > 0) // up
        {
            cornerChecks.Add(new Vector2(-16f, 50f));
            cornerChecks.Add(new Vector2(16f, 50f));
            blockMovement.Add(new Vector2(1f, 0f));
            blockMovement.Add(new Vector2(1f, 0f));
        }

        // check tile collision before allowing movement
        for (int i = 0; i < cornerChecks.Count; i++)
        {
            var check = cornerChecks[i];

            //Debug.DrawLine(
            //    new Vector3(x, y, -2), 
            //    new Vector3(x + check.x + moveX, y + check.y + moveY, -2), 
            //    Color.white, 
            //    1f);
            
            var newTileX = (int)((x + check.x + moveX) / 64f);
            var newTileY = (int)((y + check.y + moveY) / 64f);
            if (!map.IsTileWalkable(newTileX, newTileY))
            {
                moveX *= blockMovement[i].x;
                moveY *= blockMovement[i].y;
            }
        }

        CheckIsPlayerOnFire();
        if (onFire)
            return;

        if (idleAnim.enabled)
        {
            if (moveX != 0f || moveY != 0f)
            {
                idleAnim.enabled = false;
                walkAnim.enabled = true;
            }
        }
        else
        {
            if (moveX == 0f && moveY == 0f)
            {
                walkAnim.enabled = false;
                idleAnim.enabled = true;
            }
            else
            {
                // flip walk movement if we're moving left
                var rend = walkAnim.GetComponent<SpriteRenderer>();
                if (moveX < 0f)
                    rend.flipX = true;
                else if (moveX > 1f)
                    rend.flipX = false;
            }
        }        

        x += moveX;
        y += moveY;

        tileX = (int)((x + moveX) / 64f);
        tileY = (int)((y + moveY) / 64f);

        this.gameObject.transform.position += new Vector3(moveX, moveY, 0f);

        if (followCamera != null)
        {
            var z = followCamera.transform.position.z;
            // offset camera by half in each direction

            followCamera.transform.position = new Vector3(x, y + 32f, z);
        }
    }

    private void CheckIsPlayerOnFire()
    {
        // we're only trying to check if we weren't on fire
        // and if we are now. In the other case we can just
        // exit right away
        if (onFire)
            return;

        // see if we're standing in any fires
        foreach (var fire in map.fires)
        {
            if (fire.x == tileX && fire.y == tileY)
            {
                onFire = true;
                GameObject.Find("AudioFire").GetComponent<AudioSource>().Play();
                onFireFunc();
                return;
            }
        }
    }

    private void ActivateNearby()
    {
        // if we're not carrying, attempt to pickup
        // if we're carrying, attempt to dump into escape pod
        if (!carrying)
        {
            InjuredCrew cull = null;
            foreach (var injured in map.injured)
            {
                if (!injured.onFire && injured.tileX == tileX && injured.tileY == tileY)
                { 
                    injured.Pickup();
                    cull = injured;

                    carrying = true;
                    GameObject.Find("AudioPickup").GetComponent<AudioSource>().Play();
                    crewPickedUpFunc();

                    // can only carry one at a time
                    break;
                }
            }

            if (cull != null)
                map.injured.Remove(cull);
        }

        // look for escape pods nearby
        // if we're carrying, dump them
        // if we're not, jump in ourselves
        for (int i = 0; i < map.exits.Count; i++)
        {
            var pod = map.exits[i];
            if (pod.open && pod.x == tileX && pod.y - 1 == tileY)
            {
                if (carrying)
                {
                    carrying = false;
                    crewRescuedFunc();
                }
                else
                {
                    // player exited
                    escapedFunc();
                }

                GameObject.Find("AudioJettison").GetComponent<AudioSource>().Play();
                map.ShutExit(i);
            }
        }
    }
}
