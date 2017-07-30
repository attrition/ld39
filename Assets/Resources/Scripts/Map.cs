using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Tile
{
    Stars               = 0,
    WallTop             = 1,
    Floor               = 2,    // Walkable Floor
    Wall1               = 3,    // Plain
    Wall2               = 4,    // Console
    Wall3               = 5,    // Console (broken)
    Wall4               = 6,    // Pipe
    ExitClosed          = 7,
    ExitOpen            = 8,
    ExitSignOn          = 9,
    ExitSignOff         = 10,
    DoorOpenTop         = 11,   // Walkable Floor
    DoorOpenBottom      = 12,   // Walkable Floor
    DoorClosedTop       = 13,
    DoorClosedBottom    = 14
}

public class Fire
{
    public int x;
    public int y;
    public GameObject gameObject;

    public Fire(int x, int y, GameObject gameObject)
    {
        this.x = x;
        this.y = y;
        this.gameObject = gameObject;
    }
}

public class OpenCloseTile
{
    public int x;
    public int y;
    public bool open;

    public OpenCloseTile(int x, int y, bool open)
    {
        this.x = x;
        this.y = y;
        this.open = open;
    }
}

public class Map
{
    public Sprite mapSprite;
    private Dictionary<Tile, Texture2D> mapTextures;

    public int level;
    public int width { get; private set; }
    public int height { get; private set; }
    public int startX { get; private set; }
    public int startY { get; private set; }
    public float secondsBetweenFires = 5f;

    public Tile[] mapTiles;

    // exits have a sign at y-1
    public List<OpenCloseTile> exits;

    // doors are stored by top position
    // they have a bottom piece at y+1
    public List<OpenCloseTile> doors;

    // all the injured crew for a level
    public List<InjuredCrew> injured;

    public List<Fire> fires;

    private int textureSize = 16 * 4; // size * scale

    private SpriteRenderer render;

    CrewmanDied crewDiedFunc;

    public Map(int level, SpriteRenderer render, CrewmanDied crewDiedFunc)
    {
        this.render = render;

        this.crewDiedFunc = crewDiedFunc;

        LoadTextures();
        LoadLevel(level);
    }

    private void LoadTextures()
    {
        if (mapTextures != null)
        {
            foreach (var kv in mapTextures)
                GameObject.Destroy(mapTextures[kv.Key]);

            mapTextures.Clear();
        }

        mapTextures = new Dictionary<Tile, Texture2D>()
        {
            { Tile.Stars,               Resources.Load<Texture2D>("Textures/stars") },
            { Tile.WallTop,             Resources.Load<Texture2D>("Textures/wall-top") },
            { Tile.Floor,               Resources.Load<Texture2D>("Textures/floor") },
            { Tile.Wall1,               Resources.Load<Texture2D>("Textures/wall1") },
            { Tile.Wall2,               Resources.Load<Texture2D>("Textures/wall2") },
            { Tile.Wall3,               Resources.Load<Texture2D>("Textures/wall3") },
            { Tile.Wall4,               Resources.Load<Texture2D>("Textures/wall4") },
            { Tile.ExitClosed,          Resources.Load<Texture2D>("Textures/exit-closed") },
            { Tile.ExitOpen,            Resources.Load<Texture2D>("Textures/exit-open") },
            { Tile.ExitSignOn,          Resources.Load<Texture2D>("Textures/exitsign-on") },
            { Tile.ExitSignOff,         Resources.Load<Texture2D>("Textures/exitsign-off") },
            { Tile.DoorOpenTop,         Resources.Load<Texture2D>("Textures/door-open-top") },
            { Tile.DoorOpenBottom,      Resources.Load<Texture2D>("Textures/door-open-bottom") },
            { Tile.DoorClosedTop,       Resources.Load<Texture2D>("Textures/door-closed-top") },
            { Tile.DoorClosedBottom,    Resources.Load<Texture2D>("Textures/door-closed-bottom") },
        };

        foreach (var kv in mapTextures)
            mapTextures[kv.Key].filterMode = FilterMode.Point;
    }

    public void LoadLevel(int level)
    {
        if (mapSprite != null)
            GameObject.Destroy(mapSprite);

        this.level = level;
        if (level == 1)
        {
            // these have to match the intTiles[] size below
            width = 30;
            height = 20;
        }

        doors = new List<OpenCloseTile>();
        exits = new List<OpenCloseTile>();

        var texWidth = width * textureSize;
        var texHeight = height * textureSize;

        var mapTex = new Texture2D(texWidth, texHeight, TextureFormat.RGB24, false, false);
        mapTex.filterMode = FilterMode.Point;
        mapTex.wrapMode = TextureWrapMode.Clamp;

        mapTiles = new Tile[width * height];

        int[] intTiles = GetTileLayoutForLevel(level);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // reverse height lookup because 0,0 is bottom left of texture
                // and we build our maps with 0,0 in top left
                var tileIdx = ((height - y - 1) * width + x);
                var tile = (Tile)intTiles[tileIdx];

                mapTiles[tileIdx] = tile;

                if (tile == Tile.DoorOpenTop)
                    doors.Add(new OpenCloseTile(x, y, true));
                if (tile == Tile.DoorClosedTop)
                    doors.Add(new OpenCloseTile(x, y, false));
                if (tile == Tile.ExitOpen)
                    exits.Add(new OpenCloseTile(x, y, true));
                if (tile == Tile.ExitClosed)
                    exits.Add(new OpenCloseTile(x, y, false));

                var tileX = x * textureSize;
                var tileY = y * textureSize;

                mapTex.SetPixels(
                    tileX,
                    tileY,
                    textureSize,
                    textureSize,
                    mapTextures[mapTiles[tileIdx]].GetPixels());
            }
        }
        mapTex.Apply();

        mapSprite = Sprite.Create(
            mapTex,
            new Rect(Vector2.zero, new Vector2(mapTex.width, mapTex.height)),
            new Vector2(0f, 0f),
            1f);

        mapSprite.name = "Level " + level;
        render.sprite = mapSprite;

        SpawnInjuredCrewForLevel(level);
        SpawnFiresForLevel(level);
    }

    private int[] GetTileLayoutForLevel(int level)
    {
        int[] intTiles = new int[0];

        if (level == 1)
        {
            startX = 24;
            startY = 16; // 0 Y is the BOTTOM

            intTiles = new int[30 * 20]
            {
                1,1,1,1,1,1,1,1,1,1,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
                1,3,6,3,3,4,3,3,6,3,1,0,0,0,0,0,0,0,1,1,1,9,9,9,9,9,1,1,1,0,
                1,2,2,2,2,2,2,2,2,2,1,0,0,0,0,0,0,0,1,3,4,8,8,8,8,8,4,3,1,0,
                1,2,2,2,2,2,2,2,2,2,1,0,0,0,0,0,0,0,1,2,2,2,2,2,2,2,2,2,1,0,
                1,1,1,1,1,1,1,11,1,1,1,1,1,1,1,1,0,0,1,2,2,2,2,2,2,2,2,2,1,0,
                3,3,3,1,3,3,3,12,4,3,3,3,5,3,3,1,0,0,1,1,1,1,1,1,2,2,2,2,1,0,
                0,0,0,1,2,2,2,2,2,2,2,2,2,2,2,1,0,0,1,3,3,6,5,3,2,2,2,2,1,0,
                0,0,0,1,2,2,2,2,2,2,2,2,2,2,2,1,1,1,1,2,2,2,2,2,2,2,2,2,1,0,
                0,0,0,1,1,1,1,1,1,1,1,2,2,2,2,3,6,3,1,2,2,2,2,2,2,2,2,2,1,0,
                0,0,0,1,3,6,3,5,6,3,1,2,2,2,2,2,2,2,1,1,11,1,1,1,2,2,2,2,1,0,
                0,0,0,1,2,2,2,2,2,2,1,2,2,2,2,2,2,2,3,3,12,4,3,1,2,2,2,2,1,0,
                0,0,0,1,1,1,1,11,1,1,1,2,2,2,2,2,2,2,2,2,2,2,2,1,2,2,2,2,1,0,
                0,0,0,1,3,3,3,12,4,3,3,2,2,2,2,2,2,2,2,2,2,2,2,1,2,2,2,2,1,0,
                0,0,0,1,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,1,2,2,2,2,1,0,
                1,1,1,1,1,1,1,11,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,2,2,2,2,1,0,
                1,3,5,3,3,3,3,12,4,3,3,6,3,1,3,3,3,3,1,3,4,6,3,3,2,2,2,2,1,0,
                1,2,2,2,2,2,2,2,2,2,2,2,2,1,0,0,0,0,1,2,2,2,2,2,2,2,2,2,1,0,
                1,2,2,2,2,2,2,2,2,2,2,2,2,1,0,0,0,0,1,2,2,2,2,2,2,2,2,2,1,0,
                1,1,1,1,1,1,1,1,1,1,1,1,1,1,0,0,0,0,1,1,1,1,1,1,1,1,1,1,1,0,
                3,3,3,3,3,3,3,3,3,3,3,3,3,3,0,0,0,0,3,3,3,3,3,3,3,3,3,3,3,0,
            };

            return intTiles;
        }

        // will explode, I don't care
        return intTiles;
    }

    private void SpawnFiresForLevel(int level)
    {
        fires = new List<Fire>();
        var levelFires = new Vector2[] {};

        if (level == 1)
        {
            secondsBetweenFires = 7f;

            levelFires = new Vector2[]
            {
                new Vector2(1, 2),
                new Vector2(14, 13)
            };
        }

        foreach (var fire in levelFires)
        {
            StartFire((int)fire.x, (int)fire.y);
        }
    }

    private void SpawnInjuredCrewForLevel(int level)
    {
        injured = new List<InjuredCrew>();
        var levelInjured = new Vector2[] {};

        if (level == 1)
        {
            levelInjured = new Vector2[]
            {
                new Vector2(8, 3),
                new Vector2(7, 9),
                new Vector2(4, 16),
                new Vector2(26, 4),
            };
        }

        foreach (var crew in levelInjured)
        {
            int crewRealX;
            int crewRealY;
            int crewTileX = (int)crew.x;
            int crewTileY = (int)crew.y;
            this.GetTextureCoordsAt(crewTileX, crewTileY, out crewRealX, out crewRealY);

            var go = GameObject.Instantiate(
                Resources.Load("Prefabs/InjuredCrew") as GameObject,
                new Vector3(crewRealX + 32f, crewRealY + 32f, -0.5f),
                Quaternion.identity
                );

            var injuredCrew = go.GetComponent<InjuredCrew>();
            injuredCrew.x = crewRealX;
            injuredCrew.y = crewRealY;
            injuredCrew.tileX = crewTileX;
            injuredCrew.tileY = crewTileY;

            var round = Random.Range(0, 2);
            if (round == 1)
                injuredCrew.GetComponent<SpriteRenderer>().flipX = true;

            injured.Add(injuredCrew);
        }
    }

    public void GetTextureCoordsAt(int x, int y, out int ox, out int oy)
    {
        ox = x * textureSize;
        oy = y * textureSize;
    }

    public Tile TileAt(int x, int y)
    {
        return mapTiles[(height - 1 - y) * width + x];
    }

    private void SetTileAt(int x, int y, Tile tile)
    {
        mapTiles[(height - 1 - y) * width + x] = tile;
    }

    public bool IsTileWalkable(int x, int y)
    {
        var tile = TileAt(x, y);
        return (
            tile == Tile.Floor ||
            tile == Tile.DoorOpenTop ||
            tile == Tile.DoorOpenBottom ||
            tile == Tile.DoorClosedBottom);
    }

    public void ShutExit(int exitIdx)
    {
        var exit = exits[exitIdx];
        exit.open = false;
        PaintTile(exit.x, exit.y, Tile.ExitClosed);
        PaintTile(exit.x, exit.y + 1, Tile.ExitSignOff);
        
        SetTileAt(exit.x, exit.y, Tile.ExitClosed);
        SetTileAt(exit.x, exit.y + 1, Tile.ExitSignOff);
    }

    public void ShutDoor(int doorIdx)
    {
        var door = doors[doorIdx];
        door.open = false;

        PaintTile(door.x, door.y, Tile.DoorClosedTop);
        PaintTile(door.x, door.y - 1, Tile.DoorClosedBottom);

        SetTileAt(door.x, door.y, Tile.DoorClosedTop);
        SetTileAt(door.x, door.y - 1, Tile.DoorClosedBottom);
    }

    private void PaintTile(int x, int y, Tile tile)
    {
        var mapTex = mapSprite.texture;
        mapTex.SetPixels(
            x * textureSize,
            y * textureSize,
            textureSize,
            textureSize,
            mapTextures[tile].GetPixels()
            );
        mapTex.Apply();
    }

    private void StartFire(int x, int y)
    {
        var go = GameObject.Instantiate(
                        Resources.Load("Prefabs/Fire") as GameObject,
                        new Vector3(x *  textureSize + 32f, y * textureSize, -0.5f),
                        Quaternion.identity
                        );

        fires.Add(new Fire(x, y, go));

        // look if we need to cinder a crewman
        var culled = new List<InjuredCrew>();

        foreach (var crew in injured)
        {
            if (crew.tileX == x && crew.tileY == y)
            {
                crewDiedFunc();
                crew.Pickup(); // pickup has the destroy logic
                culled.Add(crew);
            }
        }

        foreach (var cull in culled)
            injured.Remove(cull);
    }

    public void SpreadFires()
    {
        var newFires = new List<Vector2>();

        // get all new fire locations
        foreach (var fire in fires)
        {
            var checks = new Vector2[]
            {
                new Vector2(-1, 0),
                new Vector2(1, 0),
                new Vector2(0, -1),
                new Vector2(0, 1),
            };

            foreach (var check in checks)
            {
                var fireVec = new Vector2(fire.x + check.x, fire.y + check.y);
                if (fireVec.x >= 0 && fireVec.x < width  - 1                        &&
                    fireVec.y >= 0 && fireVec.y < height - 1                        &&
                    (
                        TileAt((int)fireVec.x, (int)fireVec.y) == Tile.Floor        ||
                        TileAt((int)fireVec.x, (int)fireVec.y) == Tile.DoorOpenTop  ||
                        TileAt((int)fireVec.x, (int)fireVec.y) == Tile.DoorOpenBottom)
                    )
                {
                    newFires.Add(fireVec);
                }
            }
        }

        // spawn each new fire
        foreach (var fire in newFires)
        {
            bool foundFire = false;
            foreach (var existing in fires)
            {
                if (fire.x == existing.x && fire.y == existing.y)
                {
                    foundFire = true;
                    break;
                }
            }
            
            // make sure fire doesn't already exist
            if (!foundFire)
            {
                // close door if we find any
                int fireX = (int)fire.x;
                int fireY = (int)fire.y;
                if (TileAt(fireX, fireY) == Tile.DoorOpenTop    ||
                    TileAt(fireX, fireY) == Tile.DoorOpenBottom)
                {
                    for (int i = 0; i < doors.Count; i++)
                    {
                        var door = doors[i];
                        if (door.x == fireX && door.y       == fireY ||
                            door.x == fireX && door.y - 1   == fireY)
                        {
                            ShutDoor(i);
                            
                            // no need to look for more doors
                            break;
                        }
                    }
                }
                else
                {
                    // otherwise just start a fire
                    StartFire(fireX, fireY);
                }
            }
        }
    }

    public void Clear()
    {
        foreach (var crew in injured)
            crew.Pickup();
        injured.Clear();

        foreach (var fire in fires)
            GameObject.Destroy(fire.gameObject);

        fires.Clear();
    }
}
