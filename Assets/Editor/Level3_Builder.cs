using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System;
using System.IO;
using System.Collections.Generic;
using TMPro;

public class Level3_Builder : EditorWindow
{
    [MenuItem("Tools/Level 3 Builder")]
    public static void ShowWindow()
    {
        GetWindow<Level3_Builder>("Level 3 Builder");
    }

    [MenuItem("Tools/Build Level 3 Now")]
    public static void QuickBuild()
    {
        var builder = CreateInstance<Level3_Builder>();
        builder.BuildLevel();
    }

    private static int OBSTACLE_LAYER = 10;

    // Asset Paths
    private string pathParking = "Assets/Assets/Tileset/parking_lot_tileset.png";
    private string pathBarrel = "Assets/Assets/Tileset/barrel.png";
    private string pathBarrier = "Assets/Assets/Tileset/barrier.png";
    private string pathDumpster = "Assets/Assets/Tileset/dumpster.png";
    private string pathPallet = "Assets/Assets/Tileset/pallet.png";
    private string pathTires = "Assets/Assets/Tileset/tires.png";
    private string pathLightPole = "Assets/Assets/Tileset/light_pole.png";
    private string pathWhiteCircle = "Assets/Assets/Textures/WhiteCircle.png";
    private string pathKaleng = "Assets/Assets/Gambar/Kaleng 1.png";
    private string premanSpritesFolder = "Assets/Assets/Character/Preman AI/thug_with_leather_jacket/rotations";

    private void OnGUI()
    {
        if (GUILayout.Button("Build Level 3 Tileset Scene"))
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

    private Sprite EnsureSpriteImport(string path, float pixelsPerUnit = 68f)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            bool dirty = false;
            if (importer.textureType != TextureImporterType.Sprite) { importer.textureType = TextureImporterType.Sprite; dirty = true; }
            if (importer.spriteImportMode != SpriteImportMode.Single) { importer.spriteImportMode = SpriteImportMode.Single; dirty = true; }
            if (importer.alphaIsTransparency != true) { importer.alphaIsTransparency = true; dirty = true; }
            if (importer.mipmapEnabled != false) { importer.mipmapEnabled = false; dirty = true; }
            if (importer.filterMode != FilterMode.Point) { importer.filterMode = FilterMode.Point; dirty = true; }
            if (Mathf.Abs(importer.spritePixelsPerUnit - pixelsPerUnit) > 0.01f) { importer.spritePixelsPerUnit = pixelsPerUnit; dirty = true; }
            if (dirty)
            {
                importer.SaveAndReimport();
            }
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private Sprite[] LoadSlicedSprites(string path)
    {
        var objs = AssetDatabase.LoadAllAssetRepresentationsAtPath(path);
        var list = new List<Sprite>();
        foreach (var o in objs)
        {
            if (o is Sprite s) list.Add(s);
        }
        list.Sort((a, b) => GetNumberFromName(a.name).CompareTo(GetNumberFromName(b.name)));
        return list.ToArray();
    }

    private int GetNumberFromName(string name)
    {
        int lastUnderscore = name.LastIndexOf('_');
        if (lastUnderscore >= 0 && lastUnderscore < name.Length - 1)
        {
            int val;
            if (int.TryParse(name.Substring(lastUnderscore + 1), out val))
                return val;
        }
        return 0;
    }

    private void ClearTiles()
    {
        string[] rootNames = { "_Tileset_Level", "Floor", "_ParkingGround", "_WarehouseFloor", "_Obstacles", "_Walls_walls", "Fences", "WarehouseWalls", "EntranceGate", "CoverObjects", "Props", "EscapeZone", "UI_Canvas", "_TallPerimeter" };
        foreach (var rn in rootNames)
        {
            GameObject go = GameObject.Find(rn);
            if (go != null) DestroyImmediate(go);
        }
    }

    public void BuildLevel()
    {
        // Ignore collision between Enemies (Guards) so they don't block each other and get stuck
        Physics2D.IgnoreLayerCollision(9, 9, true);
        Physics2D.IgnoreLayerCollision(9, 8, true); // Guards pass through Player — distance-based catch
        Physics2D.IgnoreLayerCollision(9, 10, false); // Make sure Enemies detect Obstacles
        Physics2D.IgnoreLayerCollision(8, 10, false); // Make sure Player detects Obstacles

        ClearTiles();

        GameObject root = new GameObject("_Tileset_Level");
        root.transform.position = Vector3.zero;

        // 1. Build Ground (Clean solid dark-grey asphalt floor)
        BuildConcreteGround(root);

        // 2. Build Walls, Fences & Gates (Organic boundaries + 4 Zone Dividers)
        BuildWallsAndFences(root);

        // 3. Build Tall Perimeter (very tall barrel/tire stacks around the edge)
        BuildTallPerimeter(root);

        // 4. Build Maze Cover Obstacles for each of the 4 Zones
        BuildObstaclesAndProps(root);

        // 6. Update Positions of Players, Guards, WinZone, Pickups
        UpdateGameplayPositions();

        // 7. Draw Patrol Dots along waypoints
        DrawPatrolDots(root);

        // 8. Build and link UI Canvas
        BuildUI();

        // Setup Camera TargetZoom programmatically
        var mainCam = GameObject.Find("Main Camera");
        if (mainCam != null)
        {
            var cf = mainCam.GetComponent<CameraFollow>();
            if (cf != null)
            {
                cf.targetZoom = 3.8f;
                EditorUtility.SetDirty(cf);
            }
        }

        EditorUtility.SetDirty(root);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        Debug.Log("Level 3 build complete! Reduce detection ranges & auto-assigned guard animator.");
    }

    private GameObject CreateTile(GameObject parent, Sprite sprite, Vector3 pos, string name, int sortingOrder = 0)
    {
        if (sprite == null) return null;
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform);
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = sortingOrder;
        return go;
    }

    private GameObject CreateWallTile(GameObject parent, Sprite sprite, Vector3 pos, string name, float scale = 0.5f)
    {
        GameObject go = CreateTile(parent, sprite, pos, name, 1);
        if (go != null)
        {
            go.transform.localScale = new Vector3(scale, scale, 1f);
            go.layer = OBSTACLE_LAYER;
            
            var sr = go.GetComponent<SpriteRenderer>();
            var collider = go.AddComponent<BoxCollider2D>();
            
            // Assign zero friction physics material
            var zeroFrictionMat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/Assets/Textures/ZeroFrictionMat.physicsMaterial2D");
            if (zeroFrictionMat != null) collider.sharedMaterial = zeroFrictionMat;

            if (sr != null && sr.sprite != null)
            {
                collider.size = sr.sprite.bounds.size;
            }
        }
        return go;
    }

    // Shrinks obstacle colliders slightly to allow easier movement
    private GameObject CreateObstacleTile(GameObject parent, Sprite sprite, Vector3 pos, string name, float scale = 0.7f, int sortingOrder = 5)
    {
        GameObject go = CreateTile(parent, sprite, pos, name, sortingOrder);
        if (go != null)
        {
            go.transform.localScale = new Vector3(scale, scale, 1f);
            go.layer = OBSTACLE_LAYER;
            
            var sr = go.GetComponent<SpriteRenderer>();
            var zeroFrictionMat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/Assets/Textures/ZeroFrictionMat.physicsMaterial2D");
            
            // Round objects (barrels, tires) use CircleCollider2D; rectangular objects use BoxCollider2D
            if (name.Contains("Barrel") || name.Contains("Tires"))
            {
                var collider = go.AddComponent<CircleCollider2D>();
                if (zeroFrictionMat != null) collider.sharedMaterial = zeroFrictionMat;

                if (sr != null && sr.sprite != null)
                {
                    // Circle collider with 75% size padding
                    collider.radius = Mathf.Min(sr.sprite.bounds.size.x, sr.sprite.bounds.size.y) * 0.5f * 0.75f;
                }
            }
            else
            {
                var collider = go.AddComponent<BoxCollider2D>();
                if (zeroFrictionMat != null) collider.sharedMaterial = zeroFrictionMat;

                if (sr != null && sr.sprite != null)
                {
                    // Box collider with 75% size padding
                    collider.size = sr.sprite.bounds.size * 0.75f;
                }
            }
        }
        return go;
    }

    private void placeDividerProp(GameObject parent, Sprite barrel, Sprite tires, Sprite pallet, Vector3 pos, string name)
    {
        int hash = Mathf.Abs((Mathf.RoundToInt(pos.x) * 73 + Mathf.RoundToInt(pos.y) * 31) % 3);
        int baseSort = Mathf.RoundToInt(pos.y * 100) + 100;
        
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform);
        go.transform.position = pos;
        go.layer = OBSTACLE_LAYER;
        
        var zeroFrictionMat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/Assets/Textures/ZeroFrictionMat.physicsMaterial2D");

        if (hash == 0 && barrel != null)
        {
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = barrel;
            sr.sortingOrder = baseSort;
            go.transform.localScale = new Vector3(0.7f, 0.7f, 1f);
            
            GameObject topBarrel = new GameObject("BarrelStackTop");
            topBarrel.transform.SetParent(go.transform, false);
            topBarrel.transform.localPosition = new Vector3(0f, 0.18f, 0f);
            var srTop = topBarrel.AddComponent<SpriteRenderer>();
            srTop.sprite = barrel;
            srTop.sortingOrder = baseSort + 1;
            
            var col = go.AddComponent<CircleCollider2D>();
            if (zeroFrictionMat != null) col.sharedMaterial = zeroFrictionMat;
            col.radius = Mathf.Min(barrel.bounds.size.x, barrel.bounds.size.y) * 0.5f * 0.75f;
        }
        else if (hash == 1 && tires != null)
        {
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = tires;
            sr.sortingOrder = baseSort;
            go.transform.localScale = new Vector3(0.7f, 0.7f, 1f);
            
            GameObject topTire = new GameObject("TireStackTop");
            topTire.transform.SetParent(go.transform, false);
            topTire.transform.localPosition = new Vector3(0f, 0.18f, 0f);
            var srTop = topTire.AddComponent<SpriteRenderer>();
            srTop.sprite = tires;
            srTop.sortingOrder = baseSort + 1;
            
            var col = go.AddComponent<CircleCollider2D>();
            if (zeroFrictionMat != null) col.sharedMaterial = zeroFrictionMat;
            col.radius = Mathf.Min(tires.bounds.size.x, tires.bounds.size.y) * 0.5f * 0.75f;
        }
        else if (pallet != null)
        {
            var sr1 = go.AddComponent<SpriteRenderer>();
            sr1.sprite = pallet;
            sr1.sortingOrder = baseSort;
            go.transform.localScale = new Vector3(0.7f, 0.7f, 1f);
            
            GameObject mid = new GameObject("PalletStackMid");
            mid.transform.SetParent(go.transform, false);
            mid.transform.localPosition = new Vector3(0f, 0.2f, 0f);
            var srMid = mid.AddComponent<SpriteRenderer>();
            srMid.sprite = pallet;
            srMid.sortingOrder = baseSort + 1;
            
            GameObject top = new GameObject("PalletStackTop");
            top.transform.SetParent(go.transform, false);
            top.transform.localPosition = new Vector3(0f, 0.4f, 0f);
            var srTop = top.AddComponent<SpriteRenderer>();
            srTop.sprite = pallet;
            srTop.sortingOrder = baseSort + 2;
            
            var col = go.AddComponent<BoxCollider2D>();
            if (zeroFrictionMat != null) col.sharedMaterial = zeroFrictionMat;
            col.size = pallet.bounds.size * 0.75f;
        }
        EditorUtility.SetDirty(go);
    }

    private void BuildTallPerimeter(GameObject root)
    {
        Sprite barrel = LoadSingleSprite(pathBarrel);
        Sprite tires = LoadSingleSprite(pathTires);
        if (barrel == null || tires == null) return;

        GameObject perim = new GameObject("_TallPerimeter");
        perim.transform.SetParent(root.transform);

        var zeroFrictionMat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/Assets/Textures/ZeroFrictionMat.physicsMaterial2D");

        Action<Vector3, string, Sprite, int> placeStack = (pos, name, sprite, count) =>
        {
            int baseSort = Mathf.RoundToInt(pos.y * 100) + 50;
            GameObject go = new GameObject(name);
            go.transform.SetParent(perim.transform);
            go.transform.position = pos;
            go.layer = OBSTACLE_LAYER;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = baseSort;
            go.transform.localScale = new Vector3(0.7f, 0.7f, 1f);

            for (int i = 1; i < count; i++)
            {
                GameObject child = new GameObject($"{name}_Stack{i}");
                child.transform.SetParent(go.transform, false);
                child.transform.localPosition = new Vector3(0f, 0.18f * i, 0f);
                var csr = child.AddComponent<SpriteRenderer>();
                csr.sprite = sprite;
                csr.sortingOrder = baseSort + i;
            }

            var col = go.AddComponent<CircleCollider2D>();
            if (zeroFrictionMat != null) col.sharedMaterial = zeroFrictionMat;
            col.radius = Mathf.Min(sprite.bounds.size.x, sprite.bounds.size.y) * 0.5f * 0.75f;
        };

        int width = 28, height = 24;

        for (int x = 1; x < width - 1; x++)
        {
            if (x >= 13 && x <= 14) continue;
            int hash = Mathf.Abs((x * 73 + 0 * 31) % 4);
            Sprite sp = (hash < 2) ? barrel : tires;
            int count = (hash % 2 == 0) ? 4 : 5;
            placeStack(new Vector3(x + 0.5f, 0.5f, 0), $"Perim_B_{x}", sp, count);
        }
        for (int x = 1; x < width - 1; x++)
        {
            if (x >= 13 && x <= 14) continue;
            int hash = Mathf.Abs((x * 73 + 1 * 31) % 4);
            Sprite sp = (hash < 2) ? barrel : tires;
            int count = (hash % 2 == 0) ? 4 : 5;
            placeStack(new Vector3(x + 0.5f, 23.5f, 0), $"Perim_T_{x}", sp, count);
        }
        for (int y = 1; y < height - 1; y++)
        {
            int hash = Mathf.Abs((0 * 73 + y * 31) % 4);
            Sprite sp = (hash < 2) ? barrel : tires;
            int count = (hash % 2 == 0) ? 4 : 5;
            placeStack(new Vector3(0.5f, y + 0.5f, 0), $"Perim_L_{y}", sp, count);
        }
        for (int y = 1; y < height - 1; y++)
        {
            int hash = Mathf.Abs((27 * 73 + y * 31) % 4);
            Sprite sp = (hash < 2) ? barrel : tires;
            int count = (hash % 2 == 0) ? 4 : 5;
            placeStack(new Vector3(27.5f, y + 0.5f, 0), $"Perim_R_{y}", sp, count);
        }
    }

    private void BuildConcreteGround(GameObject root)
    {
        var sprites = LoadSlicedSprites(pathParking);
        if (sprites.Length == 0)
        {
            Debug.LogError($"No sprites found in {pathParking}");
            return;
        }

        GameObject ground = new GameObject("_ParkingGround");
        ground.transform.SetParent(root.transform);

        int width = 28;  // Enlarged to 28
        int height = 24; // Enlarged to 24

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Use slice index 6 (which is a clean solid dark-grey asphalt tile)
                Sprite tile = sprites[Mathf.Min(6, sprites.Length - 1)];
                CreateTile(ground, tile, new Vector3(x + 0.5f, y + 0.5f, 0), $"asphalt_{x}_{y}", 0);
            }
        }
    }

    private void BuildWallsAndFences(GameObject root)
    {
        Sprite barrierSprite = LoadSingleSprite(pathBarrier);
        Sprite dumpsterSprite = LoadSingleSprite(pathDumpster);
        Sprite tiresSprite = LoadSingleSprite(pathTires);
        Sprite palletSprite = LoadSingleSprite(pathPallet);
        Sprite barrelSprite = LoadSingleSprite(pathBarrel);

        if (barrierSprite == null)
        {
            Debug.LogError("barrier.png not found!");
            return;
        }

        GameObject walls = new GameObject("_Walls_walls");
        walls.transform.SetParent(root.transform);

        // Helper to place dark barrier (horizontal rotation by default)
        Action<Vector3, string> placeDarkBarrier = (pos, name) => {
            GameObject go = CreateWallTile(walls, barrierSprite, pos, name);
            if (go != null) {
                var sr = go.GetComponent<SpriteRenderer>();
                if (sr != null) sr.color = new Color(0.2f, 0.2f, 0.2f, 1f); // Dark charcoal
            }
        };

        // Helper to place dark barrier with rotation (used for left/right edges)
        Action<Vector3, string, float> placeDarkBarrierRot = (pos, name, rotZ) => {
            GameObject go = CreateWallTile(walls, barrierSprite, pos, name);
            if (go != null) {
                go.transform.rotation = Quaternion.Euler(0, 0, rotZ);
                var sr = go.GetComponent<SpriteRenderer>();
                if (sr != null) sr.color = new Color(0.2f, 0.2f, 0.2f, 1f); // Dark charcoal
            }
        };

        // Helper to place organic prop along the perimeter
        Action<Vector3, string, Sprite, float> placePerimeterProp = (pos, name, sprite, scale) => {
            CreateWallTile(walls, sprite, pos, name, scale);
        };

        // 1. Build solid dark charcoal barrier perimeter
        int width = 28;
        
        // Top and Bottom walls (horizontal barriers)
        for (int x = 0; x < width; x++)
        {
            if (x >= 13 && x <= 14) continue; // Skip gateways (centered at x=13, 14)
            placeDarkBarrier(new Vector3(x + 0.5f, 0.5f, 0), $"Wall_Bottom_{x}");
            placeDarkBarrier(new Vector3(x + 0.5f, 23.5f, 0), $"Wall_Top_{x}");
        }
        
        // Left and Right walls (vertical barriers rotated by 90 degrees for symmetry)
        for (int y = 1; y < 23; y++)
        {
            placeDarkBarrierRot(new Vector3(0.5f, y + 0.5f, 0), $"Wall_Left_{y}", 90f);
            placeDarkBarrierRot(new Vector3(27.5f, y + 0.5f, 0), $"Wall_Right_{y}", 90f);
        }

        // 2. Build second layer of cluttered organic props against the walls
        // Left wall junk piles
        placePerimeterProp(new Vector3(1.5f, 4.5f, 0), "Junk_L_1", tiresSprite, 0.7f);
        placePerimeterProp(new Vector3(1.5f, 6.5f, 0), "Junk_L_2", palletSprite, 0.7f);
        placePerimeterProp(new Vector3(1.5f, 11.5f, 0), "Junk_L_3", barrelSprite, 0.7f);
        placePerimeterProp(new Vector3(1.5f, 15.5f, 0), "Junk_L_4", tiresSprite, 0.7f);
        placePerimeterProp(new Vector3(1.5f, 19.5f, 0), "Junk_L_5", palletSprite, 0.7f);

        // Right wall junk piles
        placePerimeterProp(new Vector3(26.5f, 4.5f, 0), "Junk_R_1", tiresSprite, 0.7f);
        placePerimeterProp(new Vector3(26.5f, 6.5f, 0), "Junk_R_2", palletSprite, 0.7f);
        placePerimeterProp(new Vector3(26.5f, 11.5f, 0), "Junk_R_3", barrelSprite, 0.7f);
        placePerimeterProp(new Vector3(26.5f, 15.5f, 0), "Junk_R_4", tiresSprite, 0.7f);
        placePerimeterProp(new Vector3(26.5f, 19.5f, 0), "Junk_R_5", palletSprite, 0.7f);

        // Bottom wall junk piles
        placePerimeterProp(new Vector3(3.5f, 1.5f, 0), "Junk_B_1", tiresSprite, 0.7f);
        placePerimeterProp(new Vector3(5.5f, 1.5f, 0), "Junk_B_2", palletSprite, 0.7f);
        placePerimeterProp(new Vector3(7.5f, 1.5f, 0), "Junk_B_3", barrelSprite, 0.7f);
        placePerimeterProp(new Vector3(19.5f, 1.5f, 0), "Junk_B_4", tiresSprite, 0.7f);
        placePerimeterProp(new Vector3(21.5f, 1.5f, 0), "Junk_B_5", palletSprite, 0.7f);
        placePerimeterProp(new Vector3(23.5f, 1.5f, 0), "Junk_B_6", barrelSprite, 0.7f);

        // Top wall junk piles
        placePerimeterProp(new Vector3(3.5f, 22.5f, 0), "Junk_T_1", tiresSprite, 0.7f);
        placePerimeterProp(new Vector3(5.5f, 22.5f, 0), "Junk_T_2", palletSprite, 0.7f);
        placePerimeterProp(new Vector3(7.5f, 22.5f, 0), "Junk_T_3", barrelSprite, 0.7f);
        placePerimeterProp(new Vector3(19.5f, 22.5f, 0), "Junk_T_4", tiresSprite, 0.7f);
        placePerimeterProp(new Vector3(21.5f, 22.5f, 0), "Junk_T_5", palletSprite, 0.7f);
        placePerimeterProp(new Vector3(23.5f, 22.5f, 0), "Junk_T_6", barrelSprite, 0.7f);

        // Dumpsters at the 4 corners
        placePerimeterProp(new Vector3(1.5f, 1.5f, 0), "Corner_BL", dumpsterSprite, 0.9f);
        placePerimeterProp(new Vector3(1.5f, 22.5f, 0), "Corner_TL", dumpsterSprite, 0.9f);
        placePerimeterProp(new Vector3(26.5f, 1.5f, 0), "Corner_BR", dumpsterSprite, 0.9f);
        placePerimeterProp(new Vector3(26.5f, 22.5f, 0), "Corner_TR", dumpsterSprite, 0.9f);

        // 3. Build Horizontal Divider Walls (Perfect equal height 6-row spacing using organic cluttered piles)
        // Zone 1 -> 2 divider wall at y = 5.5f (with single gap at x = 12..15)
        for (int x = 1; x < 27; x++)
        {
            if (x >= 12 && x <= 15) continue;
            placeDividerProp(walls, barrelSprite, tiresSprite, palletSprite, new Vector3(x + 0.5f, 5.5f, 0), $"Divider_1_2_{x}");
        }

        // Zone 2 -> 3 divider wall at y = 11.5f (with single gap at x = 20..23)
        for (int x = 1; x < 27; x++)
        {
            if (x >= 20 && x <= 23) continue;
            placeDividerProp(walls, barrelSprite, tiresSprite, palletSprite, new Vector3(x + 0.5f, 11.5f, 0), $"Divider_2_3_{x}");
        }

        // Zone 3 -> 4 divider wall at y = 17.5f (with single gap at x = 4..7)
        for (int x = 1; x < 27; x++)
        {
            if (x >= 4 && x <= 7) continue;
            placeDividerProp(walls, barrelSprite, tiresSprite, palletSprite, new Vector3(x + 0.5f, 17.5f, 0), $"Divider_3_4_{x}");
        }

        // Warehouse Entrance Doors (centered at x=13.5)
        if (palletSprite != null) {
            for (int i = 0; i < 2; i++) {
                float px = 13.5f + i;
                GameObject door = new GameObject($"WH_Door_{i}");
                door.transform.SetParent(walls.transform);
                door.transform.position = new Vector3(px, 23.5f, 0);
                door.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
                var sr = door.AddComponent<SpriteRenderer>();
                sr.sprite = palletSprite;
                sr.color = new Color(0.6f, 0.4f, 0.2f);
                door.layer = OBSTACLE_LAYER;
                door.AddComponent<BoxCollider2D>();
            }
        }
    }

    private void BuildObstaclesAndProps(GameObject root)
    {
        Sprite barrier = LoadSingleSprite(pathBarrier);
        Sprite dumpster = LoadSingleSprite(pathDumpster);
        Sprite pallet = LoadSingleSprite(pathPallet);
        Sprite tires = LoadSingleSprite(pathTires);
        Sprite barrel = LoadSingleSprite(pathBarrel);
        Sprite lightPole = LoadSingleSprite(pathLightPole);

        GameObject obstacles = new GameObject("_Obstacles");
        obstacles.transform.SetParent(root.transform);

        // Helper to place obstacle
        Action<Sprite, Vector3, string, float> addObj = (sp, pos, name, scale) => {
            CreateObstacleTile(obstacles, sp, pos, name, scale);
        };

        // --- ZONE 1 (Entrance / Tutorial - rows 1..5) ---
        addObj(barrier, new Vector3(4.5f, 2.0f, 0), "Crate_Z1_1", 0.7f);
        addObj(pallet, new Vector3(6.5f, 1.5f, 0), "Pallet_Z1_1", 0.7f);
        addObj(barrel, new Vector3(18.5f, 2.0f, 0), "Barrel_Z1_1", 0.7f);
        addObj(pallet, new Vector3(21.5f, 1.5f, 0), "Pallet_Z1_2", 0.7f);
        addObj(barrel, new Vector3(10.5f, 2.0f, 0), "Barrel_Z1_2", 0.7f); // Replaced lightPole with barrel

        // --- ZONE 2 (Split Left/Right - rows 7..11) ---
        // Left side lane obstacles (adjusted for wider Guard 2 clearance at x=5.5)
        addObj(dumpster, new Vector3(2.5f, 8.5f, 0), "Dumpster_Z2_L1", 0.9f);
        addObj(tires, new Vector3(2.5f, 6.5f, 0), "Tires_Z2_L2", 0.7f);
        addObj(pallet, new Vector3(8.5f, 10.5f, 0), "Pallet_Z2_L1", 0.7f);
        addObj(barrel, new Vector3(8.5f, 7.5f, 0), "Barrel_Z2_L1", 0.7f);

        // Right side lane obstacles (adjusted for wider Guard 3 clearance at x=21.5)
        addObj(dumpster, new Vector3(25.5f, 8.5f, 0), "Dumpster_Z2_R1", 0.9f);
        addObj(tires, new Vector3(25.5f, 6.5f, 0), "Tires_Z2_R2", 0.7f);
        addObj(pallet, new Vector3(17.5f, 10.5f, 0), "Pallet_Z2_R1", 0.7f);
        addObj(barrel, new Vector3(17.5f, 7.5f, 0), "Barrel_Z2_R1", 0.7f);

        // --- ZONE 3 (Convergence Area - rows 13..17) ---
        // Central block that G4 patrols around (at y=15.0)
        addObj(pallet, new Vector3(10.5f, 15.0f, 0), "Pallet_Z3_C1", 0.7f);
        addObj(dumpster, new Vector3(13.5f, 15.0f, 0), "Dumpster_Z3_C1", 0.9f);
        addObj(pallet, new Vector3(15.5f, 15.0f, 0), "Pallet_Z3_C2", 0.7f);

        // Side covers
        addObj(barrel, new Vector3(5.5f, 14.5f, 0), "Barrel_Z3_L1", 0.7f);
        addObj(barrel, new Vector3(21.5f, 14.5f, 0), "Barrel_Z3_R1", 0.7f);
        addObj(tires, new Vector3(4.5f, 16.0f, 0), "Tires_Z3_L1", 0.7f); // Replaced lightPole with tires
        addObj(tires, new Vector3(22.5f, 16.0f, 0), "Tires_Z3_R1", 0.7f); // Replaced lightPole with tires

        // --- ZONE 4 (Final Approach - rows 19..23) ---
        addObj(dumpster, new Vector3(10.5f, 21.5f, 0), "Dumpster_Z4_1", 0.9f);
        addObj(pallet, new Vector3(16.5f, 21.5f, 0), "Pallet_Z4_1", 0.7f);
        addObj(tires, new Vector3(9.5f, 17.8f, 0), "Tires_Z4_1", 0.7f);
        addObj(tires, new Vector3(17.5f, 17.8f, 0), "Tires_Z4_2", 0.7f);
        addObj(pallet, new Vector3(13.5f, 21.5f, 0), "Pallet_Z4_2", 0.7f); // Replaced lightPole with pallet
    }

    private void UpdateGameplayPositions()
    {
        Sprite circleSprite = LoadSingleSprite(pathWhiteCircle);
        Sprite kalengSprite = EnsureSpriteImport(pathKaleng, 1200f);

        // Load guard directional sprites (order: N, NE, E, SE, S, SW, W, NW)
        Sprite[] guardSprites = new Sprite[8];
        string[] guardSpriteFiles = { "north.png", "north-east.png", "east.png", "south-east.png", "south.png", "south-west.png", "west.png", "north-west.png" };
        for (int i = 0; i < 8; i++)
            guardSprites[i] = EnsureSpriteImport($"{premanSpritesFolder}/{guardSpriteFiles[i]}", 68f);

        GameObject player = GameObject.Find("Player");
        if (player != null)
        {
            player.transform.position = new Vector3(13.5f, 1.5f, -1.0f); // Spawn at bottom center (x=13.5)
            player.transform.localScale = new Vector3(1.2f, 1.2f, 1f);
            
            // Freeze Z rotation on player's Rigidbody2D to prevent spinning
            var rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            }

            var sr = player.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                var subSprites = AssetDatabase.LoadAllAssetRepresentationsAtPath("Assets/Level 2/Aksa Lari.png");
                Sprite playerSprite = null;
                foreach (var s in subSprites)
                {
                    if (s.name == "aksa_run_0" && s is Sprite)
                    {
                        playerSprite = (Sprite)s;
                        break;
                    }
                }
                if (playerSprite == null && subSprites.Length > 0)
                {
                    playerSprite = subSprites[0] as Sprite;
                }

                if (playerSprite != null)
                {
                    sr.sprite = playerSprite;
                    sr.color = Color.white; // Reset tint
                }
                else if (circleSprite != null)
                {
                    sr.sprite = circleSprite;
                    sr.color = new Color(0.2f, 0.6f, 1.0f, 1f);
                }
                sr.sortingOrder = 20;
                EditorUtility.SetDirty(sr);
            }
            EditorUtility.SetDirty(player);
        }

        // Setup Warehouse Entrance EscapeZone GameObject at the top doors (y=22.0)
        GameObject escObj = GameObject.Find("EscapeZone");
        if (escObj == null)
        {
            escObj = new GameObject("EscapeZone");
        }
        escObj.transform.position = new Vector3(13.5f, 22.0f, -0.5f); // Placed at top-center Warehouse Doors (13.5, 22.0)
        escObj.transform.localScale = new Vector3(2.0f, 1.0f, 1.0f);
        var escCollider = escObj.GetComponent<BoxCollider2D>();
        if (escCollider == null) escCollider = escObj.AddComponent<BoxCollider2D>();
        escCollider.isTrigger = true;
        
        var escScript = escObj.GetComponent<EscapeZone>();
        if (escScript == null) escScript = escObj.AddComponent<EscapeZone>();
        
        var escSr = escObj.GetComponent<SpriteRenderer>();
        if (escSr != null) DestroyImmediate(escSr);
        
        EditorUtility.SetDirty(escObj);

        // Setup Guard parameters for balanced stealth difficulty
        float pSpeed = 1.8f;
        float cSpeed = 3.2f;
        float iSpeed = 2.2f;
        float vRange = 3.5f;
        float vAngle = 60f;
        float hRange = 4f;
        int ENEMY_LAYER = 9;

        // Guard 1 (Zone 1): horizontal patrol lower area (starts facing left to avoid instant detection)
        GameObject g1 = GameObject.Find("Guard_1");
        if (g1 != null)
        {
            g1.layer = ENEMY_LAYER;
            g1.transform.position = new Vector3(13.5f, 4.5f, -1.0f);
            g1.transform.localScale = new Vector3(1.7f, 1.7f, 1f);
            g1.transform.rotation = Quaternion.identity;
            
            var grb = g1.GetComponent<Rigidbody2D>();
            if (grb != null) grb.constraints = RigidbodyConstraints2D.FreezeRotation;

            var sr = g1.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = guardSprites[2];
                sr.color = Color.white;
                sr.sortingOrder = 20;
                EditorUtility.SetDirty(sr);
            }
            var ai = g1.GetComponent<GuardAI>();
            if (ai != null)
            {
                ai.patrolSpeed = pSpeed;
                ai.chaseSpeed = cSpeed;
                ai.investigateSpeed = iSpeed;
                ai.visionRange = vRange;
                ai.visionAngle = vAngle;
                ai.currentState = GuardState.Patrol;
                ai.directionSprites = guardSprites;
                ai.playerMask = 1 << 8;
                ai.obstacleMask = 1 << 10;
                ai.hearingRange = hRange;
                EditorUtility.SetDirty(ai);
            }
            var wps = UpdateGuardWaypoints("Guard_1_WPs", new Vector3[] {
                new Vector3(7.5f, 4.5f, -1.0f),
                new Vector3(19.5f, 4.5f, -1.0f)
            });
            if (ai != null)
            {
                ai.waypoints = wps;
            }
            EditorUtility.SetDirty(g1);
        }

        // Guard 2 (Zone 2, Kiri): vertical patrol centered at y=9.0
        GameObject g2 = GameObject.Find("Guard_2");
        if (g2 != null)
        {
            g2.layer = ENEMY_LAYER;
            g2.transform.position = new Vector3(5.5f, 9.0f, -1.0f);
            g2.transform.localScale = new Vector3(1.7f, 1.7f, 1f);
            
            var grb = g2.GetComponent<Rigidbody2D>();
            if (grb != null) grb.constraints = RigidbodyConstraints2D.FreezeRotation;

            var sr = g2.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = guardSprites[4];
                sr.color = Color.white;
                sr.sortingOrder = 20;
                EditorUtility.SetDirty(sr);
            }
            var ai = g2.GetComponent<GuardAI>();
            if (ai != null)
            {
                ai.patrolSpeed = pSpeed;
                ai.chaseSpeed = cSpeed;
                ai.investigateSpeed = iSpeed;
                ai.visionRange = vRange;
                ai.visionAngle = vAngle;
                ai.currentState = GuardState.Patrol;
                ai.directionSprites = guardSprites;
                ai.playerMask = 1 << 8;
                ai.obstacleMask = 1 << 10;
                ai.hearingRange = hRange;
                EditorUtility.SetDirty(ai);
            }
            var wps = UpdateGuardWaypoints("Guard_2_WPs", new Vector3[] {
                new Vector3(5.5f, 7.5f, -1.0f), new Vector3(5.5f, 10.5f, -1.0f)
            });
            if (ai != null)
            {
                ai.waypoints = wps;
            }
            EditorUtility.SetDirty(g2);
        }

        // Guard 3 (Zone 2, Kanan): vertical patrol centered at y=9.0
        GameObject g3 = GameObject.Find("Guard_3");
        if (g3 != null)
        {
            g3.layer = ENEMY_LAYER;
            g3.transform.position = new Vector3(21.5f, 9.0f, -1.0f);
            g3.transform.localScale = new Vector3(1.7f, 1.7f, 1f);
            
            var grb = g3.GetComponent<Rigidbody2D>();
            if (grb != null) grb.constraints = RigidbodyConstraints2D.FreezeRotation;

            var sr = g3.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = guardSprites[4];
                sr.color = Color.white;
                sr.sortingOrder = 20;
                EditorUtility.SetDirty(sr);
            }
            var ai = g3.GetComponent<GuardAI>();
            if (ai != null)
            {
                ai.patrolSpeed = pSpeed;
                ai.chaseSpeed = cSpeed;
                ai.investigateSpeed = iSpeed;
                ai.visionRange = vRange;
                ai.visionAngle = vAngle;
                ai.currentState = GuardState.Patrol;
                ai.directionSprites = guardSprites;
                ai.playerMask = 1 << 8;
                ai.obstacleMask = 1 << 10;
                ai.hearingRange = hRange;
                EditorUtility.SetDirty(ai);
            }
            var wps = UpdateGuardWaypoints("Guard_3_WPs", new Vector3[] {
                new Vector3(21.5f, 7.5f, -1.0f), new Vector3(21.5f, 10.5f, -1.0f)
            });
            if (ai != null)
            {
                ai.waypoints = wps;
            }
            EditorUtility.SetDirty(g3);
        }

        // Guard 4 (Zone 3 & 4): rectangular loop patrol in wider Zone 3 (y=12..17)
        GameObject g4 = GameObject.Find("Guard_4");
        if (g4 != null)
        {
            g4.layer = ENEMY_LAYER;
            g4.transform.position = new Vector3(13.5f, 13.5f, -1.0f);
            g4.transform.localScale = new Vector3(1.7f, 1.7f, 1f);
            
            var grb = g4.GetComponent<Rigidbody2D>();
            if (grb != null) grb.constraints = RigidbodyConstraints2D.FreezeRotation;

            var sr = g4.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = guardSprites[4];
                sr.color = Color.white;
                sr.sortingOrder = 20;
                EditorUtility.SetDirty(sr);
            }
            var ai = g4.GetComponent<GuardAI>();
            if (ai != null)
            {
                ai.patrolSpeed = pSpeed;
                ai.chaseSpeed = cSpeed;
                ai.investigateSpeed = iSpeed;
                ai.visionRange = vRange;
                ai.visionAngle = vAngle;
                ai.currentState = GuardState.Patrol;
                ai.directionSprites = guardSprites;
                ai.playerMask = 1 << 8;
                ai.obstacleMask = 1 << 10;
                ai.hearingRange = hRange;
                EditorUtility.SetDirty(ai);
            }
            var wps = UpdateGuardWaypoints("Guard_4_WPs", new Vector3[] {
                new Vector3(7.5f, 13.5f, -1.0f), 
                new Vector3(19.5f, 13.5f, -1.0f), 
                new Vector3(19.5f, 16.5f, -1.0f), 
                new Vector3(7.5f, 16.5f, -1.0f)
            });
            if (ai != null)
            {
                ai.waypoints = wps;
            }
            EditorUtility.SetDirty(g4);
        }

        // Guard 5 (Zone 4): horizontal patrol in front of warehouse doors
        GameObject g5 = GameObject.Find("Guard_5");
        if (g5 != null)
        {
            g5.layer = ENEMY_LAYER;
            g5.transform.position = new Vector3(13.5f, 19.5f, -1.0f);
            g5.transform.localScale = new Vector3(1.7f, 1.7f, 1f);
            
            var grb = g5.GetComponent<Rigidbody2D>();
            if (grb == null) grb = g5.AddComponent<Rigidbody2D>();
            grb.constraints = RigidbodyConstraints2D.FreezeRotation;
            grb.gravityScale = 0f;
            grb.bodyType = RigidbodyType2D.Dynamic;

            var sr = g5.GetComponent<SpriteRenderer>();
            if (sr == null) sr = g5.AddComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = guardSprites[4];
                sr.color = Color.white;
                sr.sortingOrder = 20;
                EditorUtility.SetDirty(sr);
            }
            var ai = g5.GetComponent<GuardAI>();
            if (ai == null) ai = g5.AddComponent<GuardAI>();
            if (ai != null)
            {
                ai.patrolSpeed = pSpeed;
                ai.chaseSpeed = cSpeed;
                ai.investigateSpeed = iSpeed;
                ai.visionRange = vRange;
                ai.visionAngle = vAngle;
                ai.currentState = GuardState.Patrol;
                ai.directionSprites = guardSprites;
                ai.playerMask = 1 << 8;
                ai.obstacleMask = 1 << 10;
                ai.hearingRange = hRange;
                EditorUtility.SetDirty(ai);
            }
            
            // Create waypoints container if missing
            GameObject g5Wps = GameObject.Find("Guard_5_WPs");
            if (g5Wps == null) g5Wps = new GameObject("Guard_5_WPs");

            var wps = UpdateGuardWaypoints("Guard_5_WPs", new Vector3[] {
                new Vector3(8.5f, 19.5f, -1.0f), new Vector3(18.5f, 19.5f, -1.0f)
            });
            if (ai != null)
            {
                ai.waypoints = wps;
            }
            EditorUtility.SetDirty(g5);
        }

        GameObject[] allGOs = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        int canIdx = 0;
        // Distraction Cans positions C1 - C5 (centered around width=28)
        Vector3[] canPositions = {
            new Vector3(10.5f, 2.0f, -0.5f),   // C1
            new Vector3(3.5f, 9.0f, -0.5f),   // C2
            new Vector3(23.5f, 9.0f, -0.5f),  // C3
            new Vector3(15.5f, 12.5f, -0.5f), // C4
            new Vector3(13.5f, 19.5f, -0.5f)  // C5
        };
        foreach (var go in allGOs)
        {
            if (go.name == "CanPickup" && canIdx < canPositions.Length)
            {
                go.transform.position = canPositions[canIdx];
                if (kalengSprite != null)
                {
                    var canSr = go.GetComponent<SpriteRenderer>();
                    if (canSr != null)
                    {
                        canSr.sprite = kalengSprite;
                        canSr.sortingOrder = 10;
                    }
                }
                EditorUtility.SetDirty(go);
                canIdx++;
            }
        }

        // Assign Kaleng sprite to Player's throwable prefab
        if (kalengSprite != null && player != null)
        {
            var pc = player.GetComponent<PlayerController>();
            if (pc != null && pc.throwablePrefab != null)
            {
                var throwableSr = pc.throwablePrefab.GetComponent<SpriteRenderer>();
                if (throwableSr != null)
                {
                    throwableSr.sprite = kalengSprite;
                    EditorUtility.SetDirty(pc.throwablePrefab);
                }
            }
        }

        // Assign guardAnimatorController to all 5 guards
        var guardAnimController = EnsureGuardAnimatorController();
        if (guardAnimController != null)
        {
            for (int gi = 1; gi <= 5; gi++)
            {
                var guardObj = GameObject.Find($"Guard_{gi}");
                if (guardObj == null) continue;
                var ai = guardObj.GetComponent<GuardAI>();
                if (ai != null)
                {
                    ai.guardAnimatorController = guardAnimController;
                    EditorUtility.SetDirty(ai);
                }
            }
        }

        // Assign ZeroFrictionMat to Player and all Guards
        var zeroFrictionMat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/Assets/Textures/ZeroFrictionMat.physicsMaterial2D");
        var playerObj = GameObject.Find("Player");
        if (playerObj != null)
        {
            var col = playerObj.GetComponent<Collider2D>();
            if (col != null)
            {
                col.sharedMaterial = zeroFrictionMat;
                EditorUtility.SetDirty(playerObj);
            }
        }
        for (int i = 1; i <= 5; i++)
        {
            var guardObj = GameObject.Find($"Guard_{i}");
            if (guardObj != null)
            {
                var col = guardObj.GetComponent<Collider2D>();
                if (col != null)
                {
                    col.sharedMaterial = zeroFrictionMat;
                    EditorUtility.SetDirty(guardObj);
                }
            }
        }

        // Assign Audio clips via AssetDatabase (reliable in Editor)
        AssignAudio();
    }

    private void AssignAudio()
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

        var playerObj = GameObject.Find("Player");
        if (playerObj != null)
        {
            var audio = playerObj.GetComponent<AudioSource>();
            if (audio == null) audio = playerObj.AddComponent<AudioSource>();
            audio.spatialBlend = 0f;
            audio.volume = 0.7f;
            audio.playOnAwake = false;

            var pc = playerObj.GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.audioSource = audio;
                pc.footstepClips = floorSteps;
                EditorUtility.SetDirty(pc);
            }
            EditorUtility.SetDirty(audio);
        }

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

        var pc2 = playerObj?.GetComponent<PlayerController>();
        if (pc2 != null && pc2.throwablePrefab != null)
        {
            var to = pc2.throwablePrefab.GetComponent<ThrowableObject>();
            if (to != null)
            {
                to.throwClip = kalengClip;
                EditorUtility.SetDirty(pc2.throwablePrefab);
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

    private void DrawPatrolDots(GameObject root)
    {
        GameObject dotsRoot = new GameObject("_PatrolDots");
        dotsRoot.transform.SetParent(root.transform);
        Sprite dotSprite = LoadSingleSprite(pathWhiteCircle);
        if (dotSprite == null) return;

        // Find all guards and draw dots between their waypoints
        for (int i = 1; i <= 5; i++)
        {
            GameObject guard = GameObject.Find($"Guard_{i}");
            if (guard == null) continue;
            var ai = guard.GetComponent<GuardAI>();
            if (ai == null || ai.waypoints == null || ai.waypoints.Length < 2) continue;

            int wpCount = ai.waypoints.Length;
            bool isLoop = (ai.waypoints.Length > 2); // Guard 4 is a loop, others are ping-pong

            int segments = isLoop ? wpCount : wpCount - 1;
            for (int s = 0; s < segments; s++)
            {
                Vector3 start = ai.waypoints[s].position;
                Vector3 end = ai.waypoints[(s + 1) % wpCount].position;
                
                // Draw dots from start to end
                float dist = Vector3.Distance(start, end);
                float step = 0.6f; // dot spacing
                int numDots = Mathf.FloorToInt(dist / step);
                for (int d = 1; d < numDots; d++)
                {
                    float t = (float)d / numDots;
                    Vector3 pos = Vector3.Lerp(start, end, t);
                    pos.z = -0.6f; // Put them just above asphalt, below player/guards
                    
                    GameObject dot = new GameObject($"Dot_{i}_{s}_{d}");
                    dot.transform.SetParent(dotsRoot.transform);
                    dot.transform.position = pos;
                    dot.transform.localScale = new Vector3(0.12f, 0.12f, 1f);
                    var sr = dot.AddComponent<SpriteRenderer>();
                    sr.sprite = dotSprite;
                    sr.color = new Color(1f, 0.85f, 0.3f, 0.35f); // Soft gold yellow dots
                    sr.sortingOrder = 2;
                }
            }
        }
    }

    private void BuildUI()
    {
        var fontAsset = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/Assets/Font/Pix32 SDF.asset");
        if (fontAsset == null)
        {
            // Fallback: cari TMP_FontAsset lain di folder Font
            var allFonts = AssetDatabase.FindAssets("t:TMP_FontAsset", new[] {"Assets/Assets/Font"});
            if (allFonts.Length > 0)
                fontAsset = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(allFonts[0]));
        }

        // 1. Find or create Canvas
        GameObject canvasObj = GameObject.Find("UI_Canvas");
        if (canvasObj != null) DestroyImmediate(canvasObj);
        
        canvasObj = new GameObject("UI_Canvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // Find GameManager to link UI elements
        var gm = FindAnyObjectByType<GameManager>();

        // 2. Create Cans Count Text (Top Left)
        GameObject cansTextObj = new GameObject("CansCountText");
        cansTextObj.transform.SetParent(canvasObj.transform, false);
        var cansRect = cansTextObj.AddComponent<RectTransform>();
        cansRect.anchorMin = new Vector2(0f, 1f);
        cansRect.anchorMax = new Vector2(0f, 1f);
        cansRect.pivot = new Vector2(0f, 1f);
        cansRect.anchoredPosition = new Vector2(40f, -40f);
        cansRect.sizeDelta = new Vector2(400f, 50f);

        var cansText = cansTextObj.AddComponent<TextMeshProUGUI>();
        cansText.text = "[Kaleng: 0]";
        cansText.fontSize = 32;
        cansText.color = Color.white;
        cansText.alignment = TextAlignmentOptions.Left;
        if (fontAsset != null) cansText.font = fontAsset;

        // 3. Create Tutorial/Guide Text (Under Cans Count Text)
        GameObject tutorialTextObj = new GameObject("TutorialText");
        tutorialTextObj.transform.SetParent(canvasObj.transform, false);
        var tutRect = tutorialTextObj.AddComponent<RectTransform>();
        tutRect.anchorMin = new Vector2(0f, 1f);
        tutRect.anchorMax = new Vector2(0f, 1f);
        tutRect.pivot = new Vector2(0f, 1f);
        tutRect.anchoredPosition = new Vector2(40f, -100f);
        tutRect.sizeDelta = new Vector2(600f, 150f);

        var tutText = tutorialTextObj.AddComponent<TextMeshProUGUI>();
        tutText.text = "Petunjuk:\n- Klik Kanan untuk melempar kaleng ke depan.\n- Klik Kiri pada kaleng untuk mengambilnya.";
        tutText.fontSize = 20;
        tutText.color = new Color(0.9f, 0.9f, 0.9f, 1f);
        tutText.alignment = TextAlignmentOptions.TopLeft;
        if (fontAsset != null) tutText.font = fontAsset;

        // SubtitleText (Centered at bottom center, used for CanPickup prompts)
        GameObject subtitleTextObj = new GameObject("SubtitleText");
        subtitleTextObj.transform.SetParent(canvasObj.transform, false);
        var subRect = subtitleTextObj.AddComponent<RectTransform>();
        subRect.anchorMin = new Vector2(0.5f, 0.1f);
        subRect.anchorMax = new Vector2(0.5f, 0.1f);
        subRect.pivot = new Vector2(0.5f, 0f);
        subRect.anchoredPosition = new Vector2(0f, 40f);
        subRect.sizeDelta = new Vector2(1000f, 60f);

        var subText = subtitleTextObj.AddComponent<TextMeshProUGUI>();
        subText.text = ""; // Empty by default
        subText.fontSize = 28;
        subText.color = Color.white;
        subText.alignment = TextAlignmentOptions.Center;
        if (fontAsset != null) subText.font = fontAsset;

        // 4. Create Lose UI Panel (Game Over)
        GameObject losePanelObj = new GameObject("LosePanel");
        losePanelObj.transform.SetParent(canvasObj.transform, false);
        var loseRect = losePanelObj.AddComponent<RectTransform>();
        loseRect.anchorMin = Vector2.zero;
        loseRect.anchorMax = Vector2.one;
        loseRect.sizeDelta = Vector2.zero;

        var loseImage = losePanelObj.AddComponent<UnityEngine.UI.Image>();
        loseImage.color = new Color(0.4f, 0.05f, 0.05f, 0.8f); // Transparent dark red

        // Lose Title Text (Centered)
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

        // Lose Button (Retry - Centered)
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

        // Setup Button OnClick to call GameManager.RestartLevel
        if (gm != null)
        {
            UnityEditor.Events.UnityEventTools.AddPersistentListener(loseBtn.onClick, gm.RestartLevel);
        }

        losePanelObj.SetActive(false); // Inactive by default

        // 5. Create Win UI Panel (Victory)
        GameObject winPanelObj = new GameObject("WinPanel");
        winPanelObj.transform.SetParent(canvasObj.transform, false);
        var winRect = winPanelObj.AddComponent<RectTransform>();
        winRect.anchorMin = Vector2.zero;
        winRect.anchorMax = Vector2.one;
        winRect.sizeDelta = Vector2.zero;

        var winImage = winPanelObj.AddComponent<UnityEngine.UI.Image>();
        winImage.color = new Color(0.05f, 0.3f, 0.05f, 0.8f); // Transparent dark green

        // Win Title Text (Centered)
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

        // Win Button (Play Again - Centered)
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

        // Setup Button OnClick to call GameManager.RestartLevel
        if (gm != null)
        {
            UnityEditor.Events.UnityEventTools.AddPersistentListener(winBtn.onClick, gm.RestartLevel);
        }

        winPanelObj.SetActive(false); // Inactive by default

        // 6. Link to GameManager
        if (gm != null)
        {
            gm.winUI = winPanelObj;
            gm.loseUI = losePanelObj;
            gm.canCountText = cansText;
            var pc2 = GameObject.Find("Player")?.GetComponent<PlayerController>();
            if (pc2 != null) gm.player = pc2;
            EditorUtility.SetDirty(gm);
        }

        // Apply Pix32 font to all TextMeshPro and TextMeshProUGUI components in the scene
        if (fontAsset != null)
        {
            foreach (var tmp in FindObjectsByType<TMPro.TextMeshProUGUI>(FindObjectsSortMode.None))
            {
                tmp.font = fontAsset;
                EditorUtility.SetDirty(tmp);
            }
            foreach (var tmp in FindObjectsByType<TMPro.TextMeshPro>(FindObjectsSortMode.None))
            {
                tmp.font = fontAsset;
                EditorUtility.SetDirty(tmp);
            }
        }
    }

    private Transform[] UpdateGuardWaypoints(string parentName, Vector3[] positions)
    {
        GameObject parent = GameObject.Find(parentName);
        if (parent == null) return new Transform[0];

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
        return wps;
    }
}
