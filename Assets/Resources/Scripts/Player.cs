using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public Map map;
    public Camera followCamera;

    public FrameAnimator idleAnim;
    public FrameAnimator walkAnim;
    public GameObject carryAnimObject;

    public float x;
    public float y;

    public int tileX;
    public int tileY;

    public bool carrying;

    public CrewmanRescued crewRescuedFunc;
    public MapReset mapResetFunc;

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
        if (Input.GetKey(KeyCode.R))
        {
            mapResetFunc();
            return;
        }

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

            followCamera.transform.position = new Vector3(x + 32f, y + 32f, z);
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
                if (injured.tileX == tileX && injured.tileY == tileY)
                { 
                    injured.Pickup();
                    cull = injured;

                    carrying = true;

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
                    crewRescuedFunc();
                    carrying = false;
                }
                else
                {
                    // level ends
                    Debug.Log("player jumped");
                }
                map.ShutExit(i);
            }
        }
    }
}
