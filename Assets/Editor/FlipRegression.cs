using System;
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Batch entry point: -executeMethod FlipRegression.Run (without -quit).
// Uses a temporary empty scene and the real player prefab in Play Mode.
[InitializeOnLoad]
public static class FlipRegression
{
    private static IEnumerator checks;
    private static double deadline;
    private static int passed;

    static FlipRegression()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("FlipRegression", false)) return;
            checks = Check();
            deadline = EditorApplication.timeSinceStartup + 90;
            EditorApplication.update += Tick;
        };
    }

    public static void Run()
    {
        DepthPaneDemo.Create();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SessionState.SetBool("FlipRegression", true);
        EditorApplication.EnterPlaymode();
    }

    private static void Tick()
    {
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Test timeout");
            if (checks.MoveNext()) return;
            Finish(0, "PASS: " + passed + " checks");
        }
        catch (Exception error) { Finish(1, error.ToString()); }
    }

    private static void Finish(int code, string message)
    {
        EditorApplication.update -= Tick;
        SessionState.SetBool("FlipRegression", false);
        Directory.CreateDirectory("Logs");
        File.WriteAllText("Logs/flip-regression-result.txt", message);
        Debug.Log(message);
        EditorApplication.Exit(code);
    }

    private static void Require(bool result, string message)
    {
        if (!result) throw new Exception(message);
        passed++;
        Debug.Log("PASS " + message);
    }

    private static IEnumerator Check()
    {
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Test floor";
        floor.transform.position = new Vector3(0, -0.5f, 0);
        floor.transform.localScale = new Vector3(40, 1, 40);
        floor.layer = 6;

        GameObject holder = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PREFABS/PlayerHolder.prefab"));
        PlayerStateManager player = holder.GetComponentInChildren<PlayerStateManager>();
        player.transform.position = new Vector3(0, 0.9f, 3);
        player.ground = 1 << 6;
        // Leave a frame for Awake/Start before testing state transitions.
        for (int i = 0; i < 15; i++) yield return null;
        player.enabled = false; // Drive state updates explicitly; physical simulation still runs.
        PlayerGravity gravity = player.GetComponent<PlayerGravity>();
        if (gravity != null) gravity.enabled = false;
        player.rb.useGravity = false;
        player.rb.velocity = Vector3.zero;
        player.rb.position = new Vector3(0, 2, 3);
        player.dimension.ResetDepth();
        Require(player.dimension != null, "Existing prefab receives depth helper");

        foreach (bool moving in new[] { false, true })
        {
            player.isGrounded = true;
            player.SwitchState(moving ? (PlayerBaseState)player.flatMoveState : player.idleState);
            Require(player.TryFlip(), "Flip accepted from " + (moving ? "moving" : "idle"));
            Require(!player.TryFlip(), "Repeated request during transition rejected");
            while (player.currentlyFlipping) { player.currentState.UpdateState(player); yield return null; }
            Require(!player.is2d && player.worldStateManager.GetWorldState() == WorldState.Flipped3d, "Entered 3D");
            Require((player.rb.constraints & RigidbodyConstraints.FreezePositionZ) == 0, "3D depth unlocked");
            player.isGrounded = true;
            player.SwitchState(moving ? (PlayerBaseState)player.flippedMoveState : player.idleState);
            Require(player.TryFlip(), "Return flip accepted");
            while (player.currentlyFlipping) { player.currentState.UpdateState(player); yield return null; }
            Require(player.is2d && Mathf.Abs(player.rb.position.z) < 0.01f,
                "Clear return recenters on pane: " + player.rb.position + " center=" + PaneManager.Get().CurrentDepth);
        }

        // A solid only projected through depth in 2D must not trap the player.
        player.isGrounded = true;
        player.TryFlip();
        while (player.currentlyFlipping) { player.currentState.UpdateState(player); yield return null; }
        GameObject wall = new GameObject("Projected wall");
        wall.layer = 6;
        wall.transform.position = new Vector3(0, 2, 0);
        BoxCollider box = wall.AddComponent<BoxCollider>();
        box.size = new Vector3(2, 4, 20);
        wall.AddComponent<HitboxExtender>();
        Require(!box.enabled, "Extended hitbox disabled in 3D");
        player.isGrounded = true;
        player.TryFlip();
        Require(!box.enabled, "Extended hitbox stays disabled during return animation");
        while (player.currentlyFlipping) { player.currentState.UpdateState(player); yield return null; }
        Collider body = player.GetComponent<Collider>();
        Require(box.enabled && Physics.GetIgnoreCollision(body, box), "Overlapping projected wall temporarily ignored");
        Require(!Physics.GetIgnoreCollision(body, floor.GetComponent<Collider>()), "Supporting floor remains solid");
        player.rb.velocity = Vector3.right * 7;
        float until = Time.time + 0.6f;
        while (Time.time < until) yield return null;
        Require(player.rb.position.x > 2, "Player walks out of projected wall");
        Require(!Physics.GetIgnoreCollision(body, box), "Wall collision restored after leaving");
        player.rb.velocity = Vector3.left * 7;
        until = Time.time + 0.6f;
        while (Time.time < until) yield return null;
        Require(player.rb.position.x > 1, "Same wall blocks re-entry");
        player.rb.velocity = Vector3.zero;

        // The destination is empty, but a thin wall lies halfway to it.
        player.transform.position = new Vector3(10, 2, 3);
        player.rb.position = player.transform.position;
        GameObject corridorWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        corridorWall.transform.position = new Vector3(10, 2, 1.5f);
        corridorWall.transform.localScale = new Vector3(2, 4, 0.2f);
        Physics.SyncTransforms();
        Require(!player.dimension.TryRecenter(), "Intermediate wall prevents snapping through depth");
        Require(Mathf.Abs(player.rb.position.z - 3) < 0.01f, "Blocked corridor leaves position unchanged");
        corridorWall.GetComponent<Collider>().isTrigger = true;
        Require(player.dimension.TryRecenter(), "Trigger pickup does not block recentering");
        Require(Mathf.Abs(player.rb.position.z) < 0.01f, "Clear corridor reaches pane center");
        UnityEngine.Object.Destroy(corridorWall);

        GameObject areaA = new GameObject("Area A");
        areaA.transform.position = new Vector3(0, 0, 30);
        Pane a = areaA.AddComponent<Pane>();
        GameObject propA = GameObject.CreatePrimitive(PrimitiveType.Cube);
        propA.transform.SetParent(areaA.transform, false);
        PaneMember memberA = propA.AddComponent<PaneMember>();
        memberA.pane = a;
        GameObject areaB = new GameObject("Area B");
        areaB.transform.position = new Vector3(0, 0, 50);
        Pane b = areaB.AddComponent<Pane>();
        GameObject propB = GameObject.CreatePrimitive(PrimitiveType.Cube);
        propB.transform.SetParent(areaB.transform, false);
        PaneMember memberB = propB.AddComponent<PaneMember>();
        memberB.pane = b;
        PaneManager panes = PaneManager.Get();
        panes.TrackDepth(50);
        Require(panes.CurrentPane == b, "Depth selects second area");
        Require(!propA.GetComponent<Renderer>().enabled && propB.GetComponent<Renderer>().enabled, "2D only draws selected area");
        Require(!propA.GetComponent<Collider>().enabled && propB.GetComponent<Collider>().enabled, "2D only collides with selected area");
        player.worldStateManager.BeginFlip(WorldState.Flipped3d, 0.35f);
        Require(propA.GetComponent<Renderer>().enabled && propB.GetComponent<Renderer>().enabled, "3D reveals both areas");
        player.worldStateManager.CompleteFlip();

        // Load the real scene to catch broken prefab/script wiring too.
        EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/SampleScene.unity", new LoadSceneParameters(LoadSceneMode.Single));
        for (int i = 0; i < 30; i++) yield return null;
        Require(UnityEngine.Object.FindObjectOfType<PlayerStateManager>() != null, "SampleScene starts with restored player");

        EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/DepthPaneDemo.unity", new LoadSceneParameters(LoadSceneMode.Single));
        until = Time.time + 0.75f;
        while (Time.time < until) yield return null;
        player = UnityEngine.Object.FindObjectOfType<PlayerStateManager>();
        Require(player.isGrounded, "Demo player lands on front area floor");
        Capture("pane-front-2d");
        Require(player.TryFlip(), "Demo flips using normal grounded rule");
        while (player.currentlyFlipping) yield return null;
        for (int i = 0; i < 10; i++) yield return null;
        Capture("pane-3d");
        player.rb.position = new Vector3(-7, 1.2f, 12);
        until = Time.time + 0.5f;
        while (Time.time < until) yield return null;
        Require(player.isGrounded && player.TryFlip(), "Back area supports return flip");
        while (player.currentlyFlipping) yield return null;
        for (int i = 0; i < 10; i++) yield return null;
        Require(PaneManager.Get().CurrentPane.paneIndex == 1, "Player finishes in back area");
        Capture("pane-back-2d");

        // Reproduce the reported pop using BOTH actual demo platforms, from
        // their front and back. The short box's minimum escape direction is up.
        foreach (float depth in new[] { 0f, 12f })
        foreach (float side in new[] { -3f, 3f })
        {
            player.isGrounded = true;
            Require(player.TryFlip(), "Enter 3D for short-platform regression");
            while (player.currentlyFlipping) yield return null;
            player.rb.position = new Vector3(5, 1.2f, depth + side);
            player.rb.velocity = Vector3.zero;
            until = Time.time + 0.5f;
            while (Time.time < until) yield return null;
            float beforeY = player.rb.position.y;
            Require(player.isGrounded && player.TryFlip(), "Return behind/in front of short platform");
            while (player.currentlyFlipping) yield return null;
            until = Time.time + 0.2f;
            while (Time.time < until) yield return null;
            Require(player.rb.position.y < beforeY + 0.08f, "Short platform does not pop player upward");
            Require(Mathf.Abs(player.rb.position.z - (depth + side)) < 0.02f, "Blocked return retains depth");
            // Drive the original movement state while allowing normal physics.
            player.enabled = false;
            player.moveInput = Vector2.right;
            until = Time.time + 0.6f;
            while (Time.time < until)
            {
                player.flatMoveState.FixedUpdateState(player);
                yield return null;
            }
            player.rb.velocity = Vector3.zero;
            player.enabled = true;
            Require(Mathf.Abs(player.rb.position.z - depth) < 0.02f, "Walking clear safely recenters");
        }
    }

    private static void Capture(string name)
    {
        if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return;
        Camera camera = UnityEngine.Object.FindObjectOfType<MoveCamera>().GetComponent<Camera>();
        RenderTexture target = RenderTexture.GetTemporary(960, 540, 24);
        RenderTexture previous = RenderTexture.active;
        camera.targetTexture = target;
        camera.Render();
        RenderTexture.active = target;
        Texture2D image = new Texture2D(960, 540, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, 960, 540), 0, 0);
        image.Apply();
        Directory.CreateDirectory("Logs");
        File.WriteAllBytes("Logs/" + name + ".png", image.EncodeToPNG());
        camera.targetTexture = null;
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(target);
        UnityEngine.Object.Destroy(image);
    }
}
