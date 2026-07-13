using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using UnityEditor.Animations;
using System;
using System.IO;
using System.Collections.Generic;
using TMPro;

public class Level4_Builder : EditorWindow
{
    [MenuItem("Tools/Level 4 Builder")]
    public static void ShowWindow()
    {
        GetWindow<Level4_Builder>("Level 4 Builder");
    }

    [MenuItem("Tools/Build Level 4 Now")]
    public static void QuickBuild()
    {
        var builder = CreateInstance<Level4_Builder>();
        builder.BuildLevel();
    }

    private static int OBSTACLE_LAYER = 10;
    private static int ENEMY_LAYER = 9;
    private static int WALL_HEIGHT = 24;
    private static int WALL_WIDTH = 28;

    private string tileBasePath = "Assets/Assets/Tileset/tilewarehouse/Tileinsidewarehouse";
    private string pathKaleng = "Assets/Assets/Gambar/Kaleng 1.png";
    private string premanSpritesFolder = "Assets/Assets/Character/Preman AI/thug_with_leather_jacket/rotations";
    private string pathWhiteCircle = "Assets/Assets/Textures/WhiteCircle.png";

    void OnGUI()
    {
        if (GUILayout.Button("Build Level 4 Warehouse"))
        {
            BuildLevel();
        }
        if (GUILayout.Button("Clear Tiles (Keep Gameplay Objects)"))
        {
            ClearTiles();
        }
    }

    private Sprite LoadSingleSprite(string path)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private Sprite EnsureSpriteImport(string path, float pixelsPerUnit = 32f, bool pointFilter = true)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            bool dirty = false;
            if (importer.textureType != TextureImporterType.Sprite) { importer.textureType = TextureImporterType.Sprite; dirty = true; }
            if (importer.spriteImportMode != SpriteImportMode.Single) { importer.spriteImportMode = SpriteImportMode.Single; dirty = true; }
            if (importer.alphaIsTransparency != true) { importer.alphaIsTransparency = true; dirty = true; }
            if (importer.mipmapEnabled != false) { importer.mipmapEnabled = false; dirty = true; }
            if (pointFilter && importer.filterMode != FilterMode.Point) { importer.filterMode = FilterMode.Point; dirty = true; }
            if (Mathf.Abs(importer.spritePixelsPerUnit - pixelsPerUnit) > 0.01f) { importer.spritePixelsPerUnit = pixelsPerUnit; dirty = true; }
            if (dirty) importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private GameObject CreateTile(GameObject parent, Sprite sprite, Vector3 pos, string name, int sortingOrder = 0, float scale = 1f)
    {
        if (sprite == null) return null;
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(scale, scale, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = sortingOrder;
        return go;
    }

    private GameObject CreateObstacleTile(GameObject parent, Sprite sprite, Vector3 pos, string name, float scale = 0.7f, int sortingOrder = 5)
    {
        GameObject go = CreateTile(parent, sprite, pos, name, sortingOrder, scale);
        if (go != null)
        {
            go.layer = OBSTACLE_LAYER;
            var sr = go.GetComponent<SpriteRenderer>();
            var zeroFrictionMat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/Assets/Textures/ZeroFrictionMat.physicsMaterial2D");

            if (name.Contains("Tong"))
            {
                var col = go.AddComponent<CircleCollider2D>();
                if (zeroFrictionMat != null) col.sharedMaterial = zeroFrictionMat;
                if (sr != null && sr.sprite != null)
                    col.radius = Mathf.Min(sr.sprite.bounds.size.x, sr.sprite.bounds.size.y) * 0.5f * 0.75f;
            }
            else
            {
                var col = go.AddComponent<BoxCollider2D>();
                if (zeroFrictionMat != null) col.sharedMaterial = zeroFrictionMat;
                if (sr != null && sr.sprite != null)
                    col.size = sr.sprite.bounds.size * 0.75f;
            }
        }
        return go;
    }

    public void BuildLevel()
    {
        Physics2D.IgnoreLayerCollision(9, 9, true);
        Physics2D.IgnoreLayerCollision(9, 8, true);
        Physics2D.IgnoreLayerCollision(9, 10, false);
        Physics2D.IgnoreLayerCollision(8, 10, false);

        // Create gameplay objects first (ensures they exist)
        CreateGameManager();
        CreatePlayer();
        CreateGuards();
        CreateCanPickups();
        CreateUI();
        CreateEscapeZone();
        CreateLootObjective();
        CreateEventSystem();

        // Build tileset
        ClearTiles();
        GameObject root = new GameObject("_Tileset_Level");
        root.transform.position = Vector3.zero;

        // Create Grid for Tilemaps
        GameObject gridGO = new GameObject("_TilemapRoot");
        gridGO.transform.SetParent(root.transform);
        var grid = gridGO.AddComponent<Grid>();
        grid.cellSize = Vector3.one;

        BuildFloorTilemap(gridGO);
        BuildWallTilemap(gridGO);
        BuildObstacles(root);

        // Setup configurations
        AssignGuardWaypoints();
        AssignAudio();
        SetupPlayerThrowable();

        // Setup camera
        var mainCam = GameObject.Find("Main Camera");
        if (mainCam != null)
        {
            var cf = mainCam.GetComponent<CameraFollow>();
            if (cf == null) cf = mainCam.AddComponent<CameraFollow>();
            cf.targetZoom = 3.8f;
            cf.smoothSpeed = 5f;
            EditorUtility.SetDirty(cf);
        }

        EditorUtility.SetDirty(root);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        Debug.Log("Level 4 build complete!");
    }

    void ClearTiles()
    {
        string[] rootNames = { "_Tileset_Level", "_TilemapRoot", "Grid" };
        foreach (var rn in rootNames)
        {
            GameObject go = GameObject.Find(rn);
            if (go != null) DestroyImmediate(go);
        }
    }

    private Tile CreateOrGetTileAsset(Sprite sprite, string assetName, Tile.ColliderType colliderType)
    {
        if (sprite == null) return null;
        string tilesDir = $"{tileBasePath}/Tiles";
        string path = $"{tilesDir}/{assetName}.asset";

        if (!AssetDatabase.IsValidFolder(tilesDir))
        {
            string parent = tileBasePath;
            string folderName = "Tiles";
            AssetDatabase.CreateFolder(parent, folderName);
        }

        var existing = AssetDatabase.LoadAssetAtPath<Tile>(path);
        if (existing != null) return existing;

        var tile = ScriptableObject.CreateInstance<Tile>();
        tile.sprite = sprite;
        tile.colliderType = colliderType;
        AssetDatabase.CreateAsset(tile, path);
        AssetDatabase.SaveAssets();
        return tile;
    }

    void BuildFloorTilemap(GameObject gridParent)
    {
        Sprite tile1 = EnsureSpriteImport($"{tileBasePath}/Tile1Normal.png", 32f);
        Sprite tile2 = EnsureSpriteImport($"{tileBasePath}/Tile2Normal.png", 32f);
        Sprite bt1 = EnsureSpriteImport($"{tileBasePath}/brokentile1.png", 32f);
        Sprite bt2 = EnsureSpriteImport($"{tileBasePath}/brokentile2.png", 32f);
        Sprite bt3 = EnsureSpriteImport($"{tileBasePath}/brokentile3.png", 32f);
        Sprite bt3_1 = EnsureSpriteImport($"{tileBasePath}/brokentile3-1.png", 32f);

        Tile floorBase = CreateOrGetTileAsset(tile1, "floor_base", Tile.ColliderType.None);
        Tile floorVar = CreateOrGetTileAsset(tile2, "floor_var", Tile.ColliderType.None);
        Tile broken1 = CreateOrGetTileAsset(bt1, "broken_1", Tile.ColliderType.None);
        Tile broken2 = CreateOrGetTileAsset(bt2, "broken_2", Tile.ColliderType.None);
        Tile broken3 = CreateOrGetTileAsset(bt3, "broken_3", Tile.ColliderType.None);
        Tile broken3_1 = CreateOrGetTileAsset(bt3_1, "broken_3_1", Tile.ColliderType.None);
        Tile[] brokenVariants = new Tile[] { broken1, broken2, broken3, broken3_1 };

        GameObject floorGO = new GameObject("FloorTM");
        floorGO.transform.SetParent(gridParent.transform);
        var floorTM = floorGO.AddComponent<Tilemap>();
        var floorTMR = floorGO.AddComponent<TilemapRenderer>();
        floorTMR.sortingOrder = 0;

        for (int x = 0; x < WALL_WIDTH; x++)
        {
            for (int y = 0; y < WALL_HEIGHT; y++)
            {
                Tile tile;
                float rand = UnityEngine.Random.value;
                if (rand < 0.7f)
                    tile = floorBase;
                else if (rand < 0.85f)
                    tile = floorVar;
                else
                    tile = brokenVariants[UnityEngine.Random.Range(0, brokenVariants.Length)];

                if (tile == null) tile = floorBase;
                floorTM.SetTile(new Vector3Int(x, y, 0), tile);
            }
        }
    }

    void BuildWallTilemap(GameObject gridParent)
    {
        Sprite wallMerah = EnsureSpriteImport($"{tileBasePath}/dindingmerah.png", 32f);
        Sprite wallMerahK1 = EnsureSpriteImport($"{tileBasePath}/dindingmerahkotor1.png", 32f);
        Sprite wallMerahK2 = EnsureSpriteImport($"{tileBasePath}/dindingmerahkotor2.png", 32f);
        Sprite wallMerahK3 = EnsureSpriteImport($"{tileBasePath}/dindingmerahkotor3.png", 32f);
        Sprite wallBiruK1 = EnsureSpriteImport($"{tileBasePath}/dindingbirukotor1.png", 32f);
        Sprite wallBiruK2 = EnsureSpriteImport($"{tileBasePath}/dindingbirukotor2.png", 32f);
        Sprite wallPolos = EnsureSpriteImport($"{tileBasePath}/dindingpolos.png", 32f);
        Sprite wallSudut = EnsureSpriteImport($"{tileBasePath}/dindingsudut.png", 32f);

        Tile[] wallTiles = new Tile[] {
            CreateOrGetTileAsset(wallMerah, "wall_merah", Tile.ColliderType.Grid),
            CreateOrGetTileAsset(wallMerahK1, "wall_merahk1", Tile.ColliderType.Grid),
            CreateOrGetTileAsset(wallMerahK2, "wall_merahk2", Tile.ColliderType.Grid),
            CreateOrGetTileAsset(wallMerahK3, "wall_merahk3", Tile.ColliderType.Grid),
            CreateOrGetTileAsset(wallBiruK1, "wall_biruk1", Tile.ColliderType.Grid),
            CreateOrGetTileAsset(wallBiruK2, "wall_biruk2", Tile.ColliderType.Grid),
            CreateOrGetTileAsset(wallPolos, "wall_polos", Tile.ColliderType.Grid),
        };
        Tile wallCorner = CreateOrGetTileAsset(wallSudut, "wall_corner", Tile.ColliderType.Grid);

        System.Func<Tile> getRandomWall = () => wallTiles[UnityEngine.Random.Range(0, wallTiles.Length)];

        GameObject wallGO = new GameObject("WallsTM");
        wallGO.transform.SetParent(gridParent.transform);
        wallGO.layer = OBSTACLE_LAYER;
        var wallTM = wallGO.AddComponent<Tilemap>();
        var wallTMR = wallGO.AddComponent<TilemapRenderer>();
        wallTMR.sortingOrder = 1;
        var wallCol = wallGO.AddComponent<TilemapCollider2D>();
        var zeroFrictionMat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/Assets/Textures/ZeroFrictionMat.physicsMaterial2D");
        if (zeroFrictionMat != null) wallCol.sharedMaterial = zeroFrictionMat;

        // Top wall (y=23)
        for (int x = 0; x < WALL_WIDTH; x++)
        {
            if (x >= 12 && x <= 15) continue;
            Tile t = (x == 0 || x == WALL_WIDTH - 1) ? wallCorner : getRandomWall();
            wallTM.SetTile(new Vector3Int(x, 23, 0), t);
        }

        // Bottom wall (y=0)
        for (int x = 0; x < WALL_WIDTH; x++)
        {
            Tile t = (x == 0 || x == WALL_WIDTH - 1) ? wallCorner : getRandomWall();
            wallTM.SetTile(new Vector3Int(x, 0, 0), t);
        }

        // Left wall (x=0)
        for (int y = 1; y < WALL_HEIGHT - 1; y++)
        {
            wallTM.SetTile(new Vector3Int(0, y, 0), getRandomWall());
        }

        // Right wall (x=27)
        for (int y = 1; y < WALL_HEIGHT - 1; y++)
        {
            wallTM.SetTile(new Vector3Int(27, y, 0), getRandomWall());
        }

        // Door indicators at top opening (pintukuning) - individual GO with collider
        Sprite doorSprite = EnsureSpriteImport($"{tileBasePath}/Asset/pintukuning.png", 100f);
        if (doorSprite != null)
        {
            for (int i = 0; i < 2; i++)
            {
                float px = 12.5f + i * 3f;
                GameObject door = new GameObject($"Door_{i}");
                door.transform.SetParent(gridParent.transform);
                door.transform.position = new Vector3(px, 23.5f, 0);
                door.transform.localScale = new Vector3(0.35f, 0.5f, 1f);
                var sr = door.AddComponent<SpriteRenderer>();
                sr.sprite = doorSprite;
                sr.sortingOrder = 2;
                sr.color = new Color(1f, 0.9f, 0.4f);
                door.layer = OBSTACLE_LAYER;
                var col = door.AddComponent<BoxCollider2D>();
                if (zeroFrictionMat != null) col.sharedMaterial = zeroFrictionMat;
                col.size = new Vector2(0.8f, 1.5f);
                col.isTrigger = false;
            }
        }

        // Horizontal Divider 1 (y=5) - gap at x=12-15
        for (int x = 1; x < WALL_WIDTH - 1; x++)
        {
            if (x >= 12 && x <= 15) continue;
            wallTM.SetTile(new Vector3Int(x, 5, 0), getRandomWall());
        }

        // Horizontal Divider 2 (y=11) - gap at x=20-23
        for (int x = 1; x < WALL_WIDTH - 1; x++)
        {
            if (x >= 20 && x <= 23) continue;
            wallTM.SetTile(new Vector3Int(x, 11, 0), getRandomWall());
        }

        // Horizontal Divider 3 (y=17) - gap at x=4-7
        for (int x = 1; x < WALL_WIDTH - 1; x++)
        {
            if (x >= 4 && x <= 7) continue;
            wallTM.SetTile(new Vector3Int(x, 17, 0), getRandomWall());
        }
    }

    void BuildObstacles(GameObject root)
    {
        Sprite box = EnsureSpriteImport($"{tileBasePath}/Asset/box.png", 100f);
        Sprite box2 = EnsureSpriteImport($"{tileBasePath}/Asset/box2.png", 100f);
        Sprite box3 = EnsureSpriteImport($"{tileBasePath}/Asset/box3.png", 100f);
        Sprite lemari = EnsureSpriteImport($"{tileBasePath}/Asset/lemari.png", 100f);
        Sprite lemari2 = EnsureSpriteImport($"{tileBasePath}/Asset/lemari2.png", 100f);
        Sprite papan = EnsureSpriteImport($"{tileBasePath}/Asset/papan.png", 100f);
        Sprite rak = EnsureSpriteImport($"{tileBasePath}/Asset/rak.png", 100f);
        Sprite rak2 = EnsureSpriteImport($"{tileBasePath}/Asset/rak2.png", 100f);
        Sprite tong = EnsureSpriteImport($"{tileBasePath}/Asset/tong.png", 100f);

        GameObject obstacles = new GameObject("_Obstacles");
        obstacles.transform.SetParent(root.transform);

        Action<Sprite, Vector3, string, float> addObj = (sp, pos, name, scale) => {
            CreateObstacleTile(obstacles, sp, pos, name, scale);
        };

        // ZONE 1 (rows 1-5): Tutorial area
        addObj(box, new Vector3(4.5f, 2.0f, 0), "Box_Z1_1", 0.7f);
        addObj(tong, new Vector3(8.5f, 1.8f, 0), "Tong_Z1_1", 0.6f);
        addObj(papan, new Vector3(18.5f, 2.0f, 0), "Papan_Z1_1", 0.65f);
        addObj(box2, new Vector3(21.5f, 1.8f, 0), "Box2_Z1_1", 0.7f);
        addObj(box3, new Vector3(10.5f, 2.5f, 0), "Box3_Z1_1", 0.7f);

        // ZONE 2 (rows 7-11): Split left/right
        // Left side
        addObj(lemari, new Vector3(2.5f, 8.5f, 0), "Lemari_Z2_L1", 0.65f);
        addObj(rak, new Vector3(2.5f, 10.5f, 0), "Rak_Z2_L1", 0.6f);
        addObj(box, new Vector3(8.5f, 9.5f, 0), "Box_Z2_L1", 0.7f);
        addObj(tong, new Vector3(8.5f, 7.5f, 0), "Tong_Z2_L1", 0.6f);

        // Right side
        addObj(lemari2, new Vector3(25.5f, 8.5f, 0), "Lemari2_Z2_R1", 0.65f);
        addObj(rak2, new Vector3(25.5f, 10.5f, 0), "Rak2_Z2_R1", 0.6f);
        addObj(box3, new Vector3(18.5f, 9.5f, 0), "Box3_Z2_R1", 0.7f);
        addObj(papan, new Vector3(18.5f, 7.5f, 0), "Papan_Z2_R1", 0.65f);

        // ZONE 3 (rows 13-17): Central
        addObj(box, new Vector3(9.5f, 15.0f, 0), "Box_Z3_1", 0.7f);
        addObj(lemari, new Vector3(13.5f, 15.0f, 0), "Lemari_Z3_1", 0.65f);
        addObj(box2, new Vector3(16.5f, 15.0f, 0), "Box2_Z3_1", 0.7f);
        addObj(tong, new Vector3(5.5f, 14.5f, 0), "Tong_Z3_L1", 0.6f);
        addObj(tong, new Vector3(21.5f, 14.5f, 0), "Tong_Z3_R1", 0.6f);
        addObj(rak, new Vector3(4.5f, 16.0f, 0), "Rak_Z3_L1", 0.6f);
        addObj(rak2, new Vector3(22.5f, 16.0f, 0), "Rak2_Z3_R1", 0.6f);

        // ZONE 4 (rows 19-23): Final approach
        addObj(lemari2, new Vector3(10.5f, 21.5f, 0), "Lemari2_Z4_1", 0.65f);
        addObj(box3, new Vector3(16.5f, 21.5f, 0), "Box3_Z4_1", 0.7f);
        addObj(papan, new Vector3(9.5f, 18.0f, 0), "Papan_Z4_1", 0.65f);
        addObj(papan, new Vector3(17.5f, 18.0f, 0), "Papan_Z4_2", 0.65f);
        addObj(box, new Vector3(13.5f, 21.0f, 0), "Box_Z4_1", 0.7f);
    }

    // ====== GAMEPLAY OBJECT CREATION ======

    void CreateGameManager()
    {
        GameObject gmObj = GameObject.Find("GameManager");
        if (gmObj != null) DestroyImmediate(gmObj);

        gmObj = new GameObject("GameManager");
        gmObj.AddComponent<GameManager>();
    }

    void CreatePlayer()
    {
        GameObject existing = GameObject.Find("Player");
        if (existing != null) DestroyImmediate(existing);

        GameObject player = new GameObject("Player");
        player.tag = "Player";
        player.layer = 8;
        player.transform.position = new Vector3(13.5f, 1.5f, -1.0f);
        player.transform.localScale = new Vector3(1.2f, 1.2f, 1f);

        var sr = player.AddComponent<SpriteRenderer>();
        var subSprites = AssetDatabase.LoadAllAssetRepresentationsAtPath("Assets/Level 2/Aksa Lari.png");
        Sprite playerSprite = null;
        foreach (var s in subSprites)
        {
            if (s.name == "aksa_run_0" && s is Sprite)
            { playerSprite = (Sprite)s; break; }
        }
        if (playerSprite == null && subSprites.Length > 0)
            playerSprite = subSprites[0] as Sprite;
        if (playerSprite != null)
        {
            sr.sprite = playerSprite;
            sr.color = Color.white;
        }
        sr.sortingOrder = 20;

        var rb = player.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.linearDamping = 0f;

        var col = player.AddComponent<CircleCollider2D>();
        col.radius = 0.3f;
        var zeroFrictionMat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/Assets/Textures/ZeroFrictionMat.physicsMaterial2D");
        if (zeroFrictionMat != null) col.sharedMaterial = zeroFrictionMat;

        var animator = player.AddComponent<Animator>();
        var playerAnimController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Assets/Animations/AksaAnimator.controller");
        if (playerAnimController != null) animator.runtimeAnimatorController = playerAnimController;

        var audio = player.AddComponent<AudioSource>();
        audio.spatialBlend = 0f;
        audio.volume = 0.7f;
        audio.playOnAwake = false;

        var pc = player.AddComponent<PlayerController4>();
        pc.audioSource = audio;
        pc.maxThrowables = 3;

        // Add throw spawn point child
        GameObject spawnPoint = new GameObject("ThrowSpawn");
        spawnPoint.transform.SetParent(player.transform);
        spawnPoint.transform.localPosition = new Vector3(0.4f, 0f, 0f);
        pc.throwSpawnPoint = spawnPoint.transform;

        // Find and assign throwable prefab
        var throwablePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Assets/Prefabs/Level3/Throwable.prefab");
        if (throwablePrefab != null)
        {
            pc.throwablePrefab = throwablePrefab;
            var kalengSprite = EnsureSpriteImport(pathKaleng, 1200f);
            if (kalengSprite != null)
            {
                var throwSr = throwablePrefab.GetComponent<SpriteRenderer>();
                if (throwSr != null) throwSr.sprite = kalengSprite;
            }
        }

        EditorUtility.SetDirty(player);
    }

    void CreateGuards()
    {
        // Load guard directional sprites (N, NE, E, SE, S, SW, W, NW)
        Sprite[] guardSprites = new Sprite[8];
        string[] guardSpriteFiles = { "north.png", "north-east.png", "east.png", "south-east.png", "south.png", "south-west.png", "west.png", "north-west.png" };
        for (int i = 0; i < 8; i++)
            guardSprites[i] = EnsureSpriteImport($"{premanSpritesFolder}/{guardSpriteFiles[i]}", 68f);

        var guardAnimController = EnsureGuardAnimatorController();
        var zeroFrictionMat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/Assets/Textures/ZeroFrictionMat.physicsMaterial2D");

        // Guard configs: {name, posX, posY, spriteIndex}
        var guardConfigs = new (string name, float x, float y, int spriteIdx)[]
        {
            ("Guard_1", 13.5f, 4.5f, 2),
            ("Guard_2", 5.5f, 9.0f, 4),
            ("Guard_3", 21.5f, 9.0f, 4),
            ("Guard_4", 13.5f, 14.5f, 4),
            ("Guard_5", 13.5f, 19.5f, 4)
        };

        for (int gi = 0; gi < guardConfigs.Length; gi++)
        {
            var cfg = guardConfigs[gi];
            GameObject existing = GameObject.Find(cfg.name);
            if (existing != null) DestroyImmediate(existing);

            GameObject guard = new GameObject(cfg.name);
            guard.layer = ENEMY_LAYER;
            guard.transform.position = new Vector3(cfg.x, cfg.y, -1.0f);
            guard.transform.localScale = new Vector3(1.7f, 1.7f, 1f);
            guard.transform.rotation = Quaternion.identity;

            var sr = guard.AddComponent<SpriteRenderer>();
            sr.sprite = guardSprites[cfg.spriteIdx];
            sr.color = Color.white;
            sr.sortingOrder = 20;

            var rb = guard.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.linearDamping = 0f;

            var col = guard.AddComponent<CircleCollider2D>();
            col.radius = 0.35f;
            if (zeroFrictionMat != null) col.sharedMaterial = zeroFrictionMat;

            var audio = guard.AddComponent<AudioSource>();
            audio.spatialBlend = 0f;
            audio.volume = 0.6f;
            audio.playOnAwake = false;

            var ai = guard.AddComponent<GuardAI>();
            ai.patrolSpeed = 1.8f;
            ai.chaseSpeed = 3.2f;
            ai.investigateSpeed = 2.2f;
            ai.visionRange = 3.5f;
            ai.visionAngle = 60f;
            ai.hearingRange = 4f;
            ai.catchDistance = 0.8f;
            ai.currentState = GuardState.Patrol;
            ai.directionSprites = guardSprites;
            ai.playerMask = 1 << 8;
            ai.obstacleMask = 1 << 10;
            ai.guardAnimatorController = guardAnimController;

            var player = GameObject.Find("Player")?.transform;
            if (player != null) ai.player = player;

            var gm = FindAnyObjectByType<GameManager>();
            if (gm != null) ai.gameManager = gm;

            EditorUtility.SetDirty(guard);
        }
    }

    void CreateCanPickups()
    {
        Sprite kalengSprite = EnsureSpriteImport(pathKaleng, 1200f);

        // Destroy existing can pickups
        var existingCans = FindObjectsByType<CanPickup>(FindObjectsSortMode.None);
        foreach (var c in existingCans) DestroyImmediate(c.gameObject);

        Vector3[] canPositions = {
            new Vector3(10.5f, 2.0f, -0.5f),
            new Vector3(3.5f, 9.0f, -0.5f),
            new Vector3(23.5f, 9.0f, -0.5f),
            new Vector3(15.5f, 12.5f, -0.5f),
            new Vector3(13.5f, 19.0f, -0.5f)
        };

        for (int i = 0; i < canPositions.Length; i++)
        {
            GameObject can = new GameObject($"CanPickup ({i + 1})");
            can.transform.position = canPositions[i];
            can.transform.localScale = new Vector3(0.8f, 0.8f, 1f);
            var sr = can.AddComponent<SpriteRenderer>();
            if (kalengSprite != null) sr.sprite = kalengSprite;
            sr.sortingOrder = 10;
            var col = can.AddComponent<CircleCollider2D>();
            col.radius = 0.35f;
            col.isTrigger = true;
            can.AddComponent<CanPickup>();
            EditorUtility.SetDirty(can);
        }
    }

    void CreateUI()
    {
        GameObject canvasObj = GameObject.Find("UI_Canvas");
        if (canvasObj != null) DestroyImmediate(canvasObj);

        var fontAsset = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/Assets/Font/Pix32 SDF.asset");
        if (fontAsset == null)
        {
            var allFonts = AssetDatabase.FindAssets("t:TMP_FontAsset", new[] { "Assets/Assets/Font" });
            if (allFonts.Length > 0)
                fontAsset = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(allFonts[0]));
        }

        canvasObj = new GameObject("UI_Canvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        var gm = FindAnyObjectByType<GameManager>();

        // Cans Count Text (Top Left)
        GameObject cansTextObj = new GameObject("CansCountText");
        cansTextObj.transform.SetParent(canvasObj.transform, false);
        var cansRect = cansTextObj.AddComponent<RectTransform>();
        cansRect.anchorMin = new Vector2(0f, 1f);
        cansRect.anchorMax = new Vector2(0f, 1f);
        cansRect.pivot = new Vector2(0f, 1f);
        cansRect.anchoredPosition = new Vector2(40f, -40f);
        cansRect.sizeDelta = new Vector2(400f, 50f);
        var cansText = cansTextObj.AddComponent<TextMeshProUGUI>();
        cansText.text = "[Kaleng: 3]";
        cansText.fontSize = 32;
        cansText.color = Color.white;
        cansText.alignment = TextAlignmentOptions.Left;
        if (fontAsset != null) cansText.font = fontAsset;

        // Tutorial Text
        GameObject tutorialTextObj = new GameObject("TutorialText");
        tutorialTextObj.transform.SetParent(canvasObj.transform, false);
        var tutRect = tutorialTextObj.AddComponent<RectTransform>();
        tutRect.anchorMin = new Vector2(0f, 1f);
        tutRect.anchorMax = new Vector2(0f, 1f);
        tutRect.pivot = new Vector2(0f, 1f);
        tutRect.anchoredPosition = new Vector2(40f, -100f);
        tutRect.sizeDelta = new Vector2(600f, 150f);
        var tutText = tutorialTextObj.AddComponent<TextMeshProUGUI>();
        tutText.text = "Petunjuk:\n- Klik Kanan untuk melempar kaleng ke depan.\n- Klik Kiri pada kaleng untuk mengambilnya.\n- Gerakan dengan WASD / Arrow Keys.";
        tutText.fontSize = 20;
        tutText.color = new Color(0.9f, 0.9f, 0.9f, 1f);
        tutText.alignment = TextAlignmentOptions.TopLeft;
        if (fontAsset != null) tutText.font = fontAsset;

        // Subtitle Text (bottom center)
        GameObject subtitleTextObj = new GameObject("SubtitleText");
        subtitleTextObj.transform.SetParent(canvasObj.transform, false);
        var subRect = subtitleTextObj.AddComponent<RectTransform>();
        subRect.anchorMin = new Vector2(0.5f, 0.1f);
        subRect.anchorMax = new Vector2(0.5f, 0.1f);
        subRect.pivot = new Vector2(0.5f, 0f);
        subRect.anchoredPosition = new Vector2(0f, 40f);
        subRect.sizeDelta = new Vector2(1000f, 60f);
        var subText = subtitleTextObj.AddComponent<TextMeshProUGUI>();
        subText.text = "";
        subText.fontSize = 28;
        subText.color = Color.white;
        subText.alignment = TextAlignmentOptions.Center;
        if (fontAsset != null) subText.font = fontAsset;

        // Lose Panel
        GameObject losePanelObj = new GameObject("LosePanel");
        losePanelObj.transform.SetParent(canvasObj.transform, false);
        var loseRect = losePanelObj.AddComponent<RectTransform>();
        loseRect.anchorMin = Vector2.zero;
        loseRect.anchorMax = Vector2.one;
        loseRect.sizeDelta = Vector2.zero;
        var loseImage = losePanelObj.AddComponent<UnityEngine.UI.Image>();
        loseImage.color = new Color(0.4f, 0.05f, 0.05f, 0.8f);

        GameObject loseTitleObj = new GameObject("LoseTitleText");
        loseTitleObj.transform.SetParent(losePanelObj.transform, false);
        var loseTitleRect = loseTitleObj.AddComponent<RectTransform>();
        loseTitleRect.anchorMin = new Vector2(0.5f, 0.5f);
        loseTitleRect.anchorMax = new Vector2(0.5f, 0.5f);
        loseTitleRect.pivot = new Vector2(0.5f, 0.5f);
        loseTitleRect.anchoredPosition = new Vector2(0f, 100f);
        loseTitleRect.sizeDelta = new Vector2(800f, 150f);
        var loseTitle = loseTitleObj.AddComponent<TextMeshProUGUI>();
        loseTitle.text = "PERMAINAN BERAKHIR!\nAKSA TERTANGKAP";
        loseTitle.fontSize = 48;
        loseTitle.color = Color.white;
        loseTitle.alignment = TextAlignmentOptions.Center;
        if (fontAsset != null) loseTitle.font = fontAsset;

        GameObject loseBtnObj = new GameObject("RetryButton");
        loseBtnObj.transform.SetParent(losePanelObj.transform, false);
        var loseBtnRect = loseBtnObj.AddComponent<RectTransform>();
        loseBtnRect.anchorMin = new Vector2(0.5f, 0.5f);
        loseBtnRect.anchorMax = new Vector2(0.5f, 0.5f);
        loseBtnRect.pivot = new Vector2(0.5f, 0.5f);
        loseBtnRect.anchoredPosition = new Vector2(0f, -50f);
        loseBtnRect.sizeDelta = new Vector2(250f, 70f);
        var loseBtnImage = loseBtnObj.AddComponent<UnityEngine.UI.Image>();
        loseBtnImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        var loseBtn = loseBtnObj.AddComponent<UnityEngine.UI.Button>();
        GameObject loseBtnTextObj = new GameObject("Text");
        loseBtnTextObj.transform.SetParent(loseBtnObj.transform, false);
        var loseBtnTextRect = loseBtnTextObj.AddComponent<RectTransform>();
        loseBtnTextRect.anchorMin = Vector2.zero;
        loseBtnTextRect.anchorMax = Vector2.one;
        loseBtnTextRect.sizeDelta = Vector2.zero;
        var loseBtnText = loseBtnTextObj.AddComponent<TextMeshProUGUI>();
        loseBtnText.text = "Coba Lagi";
        loseBtnText.fontSize = 24;
        loseBtnText.color = Color.white;
        loseBtnText.alignment = TextAlignmentOptions.Center;
        if (fontAsset != null) loseBtnText.font = fontAsset;
        if (gm != null)
            UnityEditor.Events.UnityEventTools.AddPersistentListener(loseBtn.onClick, gm.RestartLevel);
        losePanelObj.SetActive(false);

        // Win Panel
        GameObject winPanelObj = new GameObject("WinPanel");
        winPanelObj.transform.SetParent(canvasObj.transform, false);
        var winRect = winPanelObj.AddComponent<RectTransform>();
        winRect.anchorMin = Vector2.zero;
        winRect.anchorMax = Vector2.one;
        winRect.sizeDelta = Vector2.zero;
        var winImage = winPanelObj.AddComponent<UnityEngine.UI.Image>();
        winImage.color = new Color(0.05f, 0.3f, 0.05f, 0.8f);

        GameObject winTitleObj = new GameObject("WinTitleText");
        winTitleObj.transform.SetParent(winPanelObj.transform, false);
        var winTitleRect = winTitleObj.AddComponent<RectTransform>();
        winTitleRect.anchorMin = new Vector2(0.5f, 0.5f);
        winTitleRect.anchorMax = new Vector2(0.5f, 0.5f);
        winTitleRect.pivot = new Vector2(0.5f, 0.5f);
        winTitleRect.anchoredPosition = new Vector2(0f, 100f);
        winTitleRect.sizeDelta = new Vector2(800f, 150f);
        var winTitle = winTitleObj.AddComponent<TextMeshProUGUI>();
        winTitle.text = "MISI SELESAI!\nBERHASIL MASUK KE WAREHOUSE";
        winTitle.fontSize = 48;
        winTitle.color = Color.white;
        winTitle.alignment = TextAlignmentOptions.Center;
        if (fontAsset != null) winTitle.font = fontAsset;

        GameObject winBtnObj = new GameObject("PlayAgainButton");
        winBtnObj.transform.SetParent(winPanelObj.transform, false);
        var winBtnRect = winBtnObj.AddComponent<RectTransform>();
        winBtnRect.anchorMin = new Vector2(0.5f, 0.5f);
        winBtnRect.anchorMax = new Vector2(0.5f, 0.5f);
        winBtnRect.pivot = new Vector2(0.5f, 0.5f);
        winBtnRect.anchoredPosition = new Vector2(0f, -50f);
        winBtnRect.sizeDelta = new Vector2(250f, 70f);
        var winBtnImage = winBtnObj.AddComponent<UnityEngine.UI.Image>();
        winBtnImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        var winBtn = winBtnObj.AddComponent<UnityEngine.UI.Button>();
        GameObject winBtnTextObj = new GameObject("Text");
        winBtnTextObj.transform.SetParent(winBtnObj.transform, false);
        var winBtnTextRect = winBtnTextObj.AddComponent<RectTransform>();
        winBtnTextRect.anchorMin = Vector2.zero;
        winBtnTextRect.anchorMax = Vector2.one;
        winBtnTextRect.sizeDelta = Vector2.zero;
        var winBtnText = winBtnTextObj.AddComponent<TextMeshProUGUI>();
        winBtnText.text = "Main Lagi";
        winBtnText.fontSize = 24;
        winBtnText.color = Color.white;
        winBtnText.alignment = TextAlignmentOptions.Center;
        if (fontAsset != null) winBtnText.font = fontAsset;
        if (gm != null)
            UnityEditor.Events.UnityEventTools.AddPersistentListener(winBtn.onClick, gm.RestartLevel);
        winPanelObj.SetActive(false);

        // Link to GameManager
        if (gm != null)
        {
            gm.winUI = winPanelObj;
            gm.loseUI = losePanelObj;
            gm.canCountText = cansText;
            EditorUtility.SetDirty(gm);
        }

        // Apply Pix32 font to all TextMeshProUGUI
        if (fontAsset != null)
        {
            foreach (var tmp in FindObjectsByType<TMPro.TextMeshProUGUI>(FindObjectsSortMode.None))
            {
                tmp.font = fontAsset;
                EditorUtility.SetDirty(tmp);
            }
        }
    }

    void CreateEscapeZone()
    {
        GameObject existing = GameObject.Find("EscapeZone");
        if (existing != null) DestroyImmediate(existing);

        GameObject escObj = new GameObject("EscapeZone");
        escObj.transform.position = new Vector3(13.5f, 22.0f, -0.5f);
        escObj.transform.localScale = new Vector3(2.0f, 1.0f, 1.0f);
        var col = escObj.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        escObj.AddComponent<EscapeZone>();
        EditorUtility.SetDirty(escObj);
    }

    void CreateLootObjective()
    {
        GameObject existing = GameObject.Find("WinZone");
        if (existing != null) DestroyImmediate(existing);

        // The "WinZone" serves as the LootObjective (access card pickup)
        GameObject winObj = new GameObject("WinZone");
        winObj.transform.position = new Vector3(13.5f, 11.0f, -0.5f);
        winObj.transform.localScale = new Vector3(0.8f, 0.8f, 1f);
        var sr = winObj.AddComponent<SpriteRenderer>();
        // Use a white circle sprite as placeholder for access card
        var circleSprite = LoadSingleSprite(pathWhiteCircle);
        if (circleSprite != null)
        {
            sr.sprite = circleSprite;
            sr.color = new Color(0.2f, 0.8f, 0.2f, 1f);
        }
        sr.sortingOrder = 15;
        var col = winObj.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(0.8f, 0.8f);
        winObj.AddComponent<LootObjective>();
        EditorUtility.SetDirty(winObj);
    }

    void CreateEventSystem()
    {
        GameObject existing = GameObject.Find("EventSystem");
        if (existing != null) return;

        var es = new GameObject("EventSystem");
        es.AddComponent<UnityEngine.EventSystems.EventSystem>();
        es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
    }

    void AssignGuardWaypoints()
    {
        // Guard waypoints
        var waypointConfigs = new Dictionary<string, Vector3[]>
        {
            { "Guard_1_WPs", new Vector3[] { new Vector3(7.5f, 4.5f, -1.0f), new Vector3(19.5f, 4.5f, -1.0f) } },
            { "Guard_2_WPs", new Vector3[] { new Vector3(5.5f, 7.5f, -1.0f), new Vector3(5.5f, 10.5f, -1.0f) } },
            { "Guard_3_WPs", new Vector3[] { new Vector3(21.5f, 7.5f, -1.0f), new Vector3(21.5f, 10.5f, -1.0f) } },
            { "Guard_4_WPs", new Vector3[] { new Vector3(7.5f, 13.5f, -1.0f), new Vector3(19.5f, 13.5f, -1.0f), new Vector3(19.5f, 16.5f, -1.0f), new Vector3(7.5f, 16.5f, -1.0f) } },
            { "Guard_5_WPs", new Vector3[] { new Vector3(8.5f, 19.5f, -1.0f), new Vector3(18.5f, 19.5f, -1.0f) } }
        };

        foreach (var kvp in waypointConfigs)
        {
            string parentName = kvp.Key;
            Vector3[] positions = kvp.Value;

            GameObject parent = GameObject.Find(parentName);
            if (parent == null)
            {
                parent = new GameObject(parentName);
            }

            // Clear existing children
            var children = new List<GameObject>();
            foreach (Transform child in parent.transform)
                children.Add(child.gameObject);
            foreach (var c in children)
                DestroyImmediate(c);

            Transform[] wps = new Transform[positions.Length];
            for (int i = 0; i < positions.Length; i++)
            {
                GameObject wp = new GameObject("WP_" + i);
                wp.transform.SetParent(parent.transform);
                wp.transform.position = positions[i];
                wps[i] = wp.transform;
            }

            // Assign to guard
            int guardIndex = int.Parse(parentName.Split('_')[1].Replace("WPs", "").Replace("_", "").Trim());
            
            // Try different naming: Guard_1_WPs -> Guard_1
            string guardName = $"Guard_{guardIndex}";
            var guard = GameObject.Find(guardName);
            if (guard != null)
            {
                var ai = guard.GetComponent<GuardAI>();
                if (ai != null)
                {
                    ai.waypoints = wps;
                    EditorUtility.SetDirty(ai);
                }
            }
        }
    }

    void SetupPlayerThrowable()
    {
        var player = GameObject.Find("Player");
        if (player == null) return;

        var pc = player.GetComponent<PlayerController4>();
        if (pc == null) return;

        // Ensure throwable prefab is assigned
        if (pc.throwablePrefab == null)
        {
            var throwablePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Assets/Prefabs/Level3/Throwable.prefab");
            if (throwablePrefab != null)
            {
                pc.throwablePrefab = throwablePrefab;
            }
        }

        // Ensure NoiseSource prefab reference on throwable
        if (pc.throwablePrefab != null)
        {
            var to = pc.throwablePrefab.GetComponent<ThrowableObject>();
            if (to != null && to.noiseSourcePrefab == null)
            {
                var noisePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Assets/Prefabs/Level3/NoiseSource.prefab");
                if (noisePrefab != null)
                {
                    to.noiseSourcePrefab = noisePrefab;
                }
            }
        }
    }

    void AssignAudio()
    {
        string footstepDir = "Assets/Classic Footstep SFX/Floor";
        var floorSteps = new AudioClip[] {
            AssetDatabase.LoadAssetAtPath<AudioClip>($"{footstepDir}/Floor_step0.wav"),
            AssetDatabase.LoadAssetAtPath<AudioClip>($"{footstepDir}/Floor_step1.wav"),
            AssetDatabase.LoadAssetAtPath<AudioClip>($"{footstepDir}/Floor_step2.wav"),
            AssetDatabase.LoadAssetAtPath<AudioClip>($"{footstepDir}/Floor_step3.wav"),
            AssetDatabase.LoadAssetAtPath<AudioClip>($"{footstepDir}/Floor_step4.wav"),
        };
        var kalengClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Assets/Musik/soundeffect/kaleng_throw.mp3");
        var caughtClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Assets/Musik/soundeffect/guard_caught_player.mp3");

        // Player audio
        var playerObj = GameObject.Find("Player");
        if (playerObj != null)
        {
            var audio = playerObj.GetComponent<AudioSource>();
            if (audio == null) audio = playerObj.AddComponent<AudioSource>();
            audio.spatialBlend = 0f;
            audio.volume = 0.7f;
            audio.playOnAwake = false;

            var pc = playerObj.GetComponent<PlayerController4>();
            if (pc != null)
            {
                pc.audioSource = audio;
                pc.footstepClips = floorSteps;
                EditorUtility.SetDirty(pc);
            }
            EditorUtility.SetDirty(audio);
        }

        // Guard audio
        for (int i = 1; i <= 5; i++)
        {
            var guardObj = GameObject.Find($"Guard_{i}");
            if (guardObj == null) continue;

            var audio = guardObj.GetComponent<AudioSource>();
            if (audio == null) audio = guardObj.AddComponent<AudioSource>();
            audio.spatialBlend = 0f;
            audio.volume = 0.6f;
            audio.playOnAwake = false;

            var ai = guardObj.GetComponent<GuardAI>();
            if (ai != null)
            {
                ai.audioSource = audio;
                ai.footstepClips = floorSteps;
                ai.caughtClip = caughtClip;
                EditorUtility.SetDirty(ai);
            }
            EditorUtility.SetDirty(audio);
        }

        // Throwable audio
        var pc4 = playerObj?.GetComponent<PlayerController4>();
        if (pc4 != null && pc4.throwablePrefab != null)
        {
            var to = pc4.throwablePrefab.GetComponent<ThrowableObject>();
            if (to != null)
            {
                to.throwClip = kalengClip;
                EditorUtility.SetDirty(pc4.throwablePrefab);
            }
        }
    }

    private RuntimeAnimatorController EnsureGuardAnimatorController()
    {
        string path = "Assets/Assets/Animations/GuardAnimator.controller";
        var existing = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(path);
        if (existing != null) return existing;

        var controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(path);
        if (controller == null) return null;

        controller.AddParameter("Speed", UnityEngine.AnimatorControllerParameterType.Float);

        var stateMachine = controller.layers[0].stateMachine;
        var idleState = stateMachine.AddState("Idle", new Vector3(200, 0, 0));
        var moveState = stateMachine.AddState("Move", new Vector3(400, 0, 0));
        stateMachine.defaultState = idleState;

        var idleToMove = idleState.AddTransition(moveState);
        idleToMove.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 0.01f, "Speed");
        idleToMove.duration = 0f;

        var moveToIdle = moveState.AddTransition(idleState);
        moveToIdle.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0.01f, "Speed");
        moveToIdle.duration = 0f;

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Created GuardAnimator.controller at " + path);
        return controller;
    }
}
