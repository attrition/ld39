using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public delegate void PlayerOnFire();
public delegate void PlayerEscaped();
public delegate void CrewmanPickedUp();
public delegate void CrewmanRescued();
public delegate void CrewmanDied();

public class Game : MonoBehaviour
{
    private Map map;
    public SpriteRenderer render;
    public Camera playerCamera;

    public AudioSource music;
    private bool musicOn = true;

    public Player player;

    public Text statusText;
    public Text instructionText;
    public Text mapEndText;
    public Image panel;

    public float timeSinceLastFire = 0f;
    public float timeSincePlayerOnFire = 0f;

    public int crewTotal = 0;
    public int crewDead = 0;
    public int crewRescued = 0;
    public int crewLeft = 0;

    public int level = 0;
    private int maxLevels = 4;
    public float levelTimerStart = 0f;
    public bool playerEscaped = false;

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
        player.onFireFunc = PlayerOnFire;
        player.escapedFunc = PlayerEscaped;
        player.crewRescuedFunc = CrewmanRescued;
        player.crewPickedUpFunc = CrewmanPickedUp;
        playerEscaped = false;

        crewTotal = map.injured.Count;
        crewDead = 0;
        crewRescued = 0;
        crewLeft = crewTotal;

        levelTimerStart = Time.realtimeSinceStartup;

        panel.enabled = false;
        statusText.enabled = true;
        instructionText.enabled = true;
        mapEndText.enabled = false;

        UpdateStatusText();
        UpdateInstructionText();

        if (musicOn)
            music.Play();
    }

    private void UpdateInstructionText()
    {
        if (playerEscaped)
        {
            instructionText.enabled = false;
            mapEndText.text = "You escaped!\n";
            mapEndText.text += "\n";
            mapEndText.text += crewRescued + " crew rescued\n";
            mapEndText.text += crewDead + " crew died\n";
            mapEndText.text += "You took " + levelTimerStart + " seconds\n";
            mapEndText.text += "\n\n";

            if (level + 1 < maxLevels)
            {
                mapEndText.text += "Spacebar for next level";
            }
            else
            {
                mapEndText.text += "You beat all the levels!\n";
                mapEndText.text += "Thanks for playing!\n\n";
                mapEndText.text += "Maybe try beating your previous times\n\n";
                mapEndText.text += "Spacebar to return to tutorial";
            }

            panel.enabled = true;
            mapEndText.enabled = true;
            return;
        }
        else
        {
            if (player.onFire)
            {
                instructionText.enabled = false;
                if (Time.realtimeSinceStartup - timeSincePlayerOnFire > 2f)
                {
                    mapEndText.text = "You died! Try again!\n";
                    mapEndText.text += "\n";
                    mapEndText.text += crewRescued + " crew rescued\n";
                    mapEndText.text += crewDead + " crew died\n";
                    mapEndText.text += "You took " + levelTimerStart + " seconds\n";
                    mapEndText.text += "\n\n\n\n\n\n\n\n\n";
                    mapEndText.text += "R to reset level";
                    panel.enabled = true;
                    mapEndText.enabled = true;
                }
                return;
            }
            else
            {
                if (level == 0)
                {
                    if (player.carrying)
                        instructionText.text = "Bring crew to escape pod, Spacebar to activate";
                    else
                    {
                        if (crewLeft > 0)
                            instructionText.text = "WASD to move, Spacebar to pick-up crew\n";
                        else
                            instructionText.text = "All crew rescued! Get to an escape pod!\n";

                        instructionText.text += "When at an escape pod use Spacebar to escape";
                    }
                }
                else
                {
                    instructionText.text = "";
                }
            }
        }
        instructionText.text += "\nR to reset level";
    }

    private string LevelTimer()
    {
        return (Time.realtimeSinceStartup - levelTimerStart).ToString("n3");
    }

    private void UpdateStatusText()
    {
        statusText.text = "Level " + (map.level + 1) + ": " + map.name + "\n";
        statusText.text += "Crew Left: " + crewLeft + "\n";
        statusText.text += "Crew Rescued: " + crewRescued + "\n";
        statusText.text += "Crew Dead: " + crewDead + "\n";
        statusText.text += "Time: " + LevelTimer() + "\n";
    }

    private void InitPlayer()
    {
        var playerGO = GameObject.Instantiate(Resources.Load("Prefabs/PlayerCharacter")) as GameObject;
        player = playerGO.GetComponent<Player>();
    }

    private void PlayerEscaped()
    {
        playerEscaped = true;
        UpdateInstructionText();
        Destroy(player.gameObject);
    }

    private void PlayerOnFire()
    {
        music.Stop();
        timeSincePlayerOnFire = Time.realtimeSinceStartup;
        UpdateInstructionText();
    }

    private void CrewmanPickedUp()
    {
        UpdateInstructionText();
    }
    
    private void ResetMap()
    {
        if (player != null && player.gameObject != null)
            Destroy(player.gameObject);

        Destroy(map.mapSprite.texture);
        Destroy(map.mapSprite);
        Destroy(player.carryAnimObject);
        Destroy(player.idleAnim);
        Destroy(player.walkAnim);
        Resources.UnloadUnusedAssets();
        map.Clear();
        InitPlayer();
        InitLevel(level);
        timeSinceLastFire = Time.realtimeSinceStartup;

        if (musicOn)
            music.Play();
    }

    private void CrewmanRescued()
    {
        Debug.Log("rescued callback");
        crewRescued++;
        crewLeft--;
        UpdateStatusText();
        UpdateInstructionText();
    }

    private void CrewmanDied()
    {
        Debug.Log("crewman died callback");
        crewDead++;
        crewLeft--;
        UpdateStatusText();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            musicOn = !musicOn;
            if (musicOn)
                music.Play();
            else
                music.Stop();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetMap();
            return;
        }

        if (playerEscaped && Input.GetKeyDown(KeyCode.Space))
        {
            if (level + 1 < maxLevels)
                level++;
            else
                level = 0;

            ResetMap();
            return;
        }

        if (!playerEscaped && !player.onFire)
            UpdateStatusText();
        else
            statusText.enabled = false;

        UpdateInstructionText();

        var timeNow = Time.realtimeSinceStartup;
        
        if (timeNow - timeSinceLastFire > map.secondsBetweenFires)
        {
            map.SpreadFires();
            timeSinceLastFire = timeNow;
        }
    }
}
