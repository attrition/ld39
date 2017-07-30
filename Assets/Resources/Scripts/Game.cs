using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public delegate void CrewmanRescued();
public delegate void CrewmanDied();
public delegate void MapReset();

public class Game : MonoBehaviour
{
    private Map map;
    public SpriteRenderer render;
    public Camera playerCamera;

    public Player player;

    public float timeSinceLastFire = 0f;

    public int crewTotal = 0;
    public int crewDead = 0;
    public int crewRescued = 0;
    public int crewLeft = 0;

    public int level = 1;

    // Use this for initialization
    void Start()
    {
        render = this.gameObject.AddComponent<SpriteRenderer>();
        InitPlayer();
        InitLevel(level);

        timeSinceLastFire = Time.realtimeSinceStartup;
    }

    private void InitLevel(int level)
    {
        map = new Map(level, render, CrewmanDied);

        int playerX;
        int playerY;
        map.GetTextureCoordsAt(map.startX, map.startY, out playerX, out playerY);
        player.gameObject.transform.position = new Vector3(playerX + 32f, playerY, -5f);

        player.x = playerX + 32f;
        player.y = playerY;
        player.tileX = map.startX;
        player.tileY = map.startY;
        player.map = map;
        player.followCamera = playerCamera;
        player.crewRescuedFunc = CrewmanRescued;
        player.mapResetFunc = ResetMap;

        crewTotal = map.injured.Count;
        crewDead = 0;
        crewRescued = 0;
        crewLeft = crewTotal;
    }

    private void InitPlayer()
    {
        var playerGO = GameObject.Instantiate(Resources.Load("Prefabs/PlayerCharacter")) as GameObject;
        player = playerGO.GetComponent<Player>();
    }

    private void ResetMap()
    {
        Destroy(player.gameObject);
        map.Clear();
        InitPlayer();
        InitLevel(level);
    }

    private void CrewmanRescued()
    {
        Debug.Log("rescued callback");
        crewRescued++;
        crewLeft--;
    }

    private void CrewmanDied()
    {
        Debug.Log("crewman died callback");
        crewDead++;
        crewLeft--;
    }

    // Update is called once per frame
    void Update()
    {
        var timeNow = Time.realtimeSinceStartup;
        
        if (timeNow - timeSinceLastFire > map.secondsBetweenFires)
        {
            map.SpreadFires();
            timeSinceLastFire = timeNow;
        }
    }
}
