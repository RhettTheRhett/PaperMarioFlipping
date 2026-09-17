using System;
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Batch entry point: -executeMethod GoombaRegression.Run (without -quit).
[InitializeOnLoad]
public static class GoombaRegression
{
    private static IEnumerator checks;
    private static double deadline;
    private static int passed;

    static GoombaRegression()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("GoombaRegression", false)) return;
            checks = Check();
            deadline = EditorApplication.timeSinceStartup + 90;
            EditorApplication.update += Tick;
        };
    }

    public static void Run()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SessionState.SetBool("GoombaRegression", true);
        EditorApplication.EnterPlaymode();
    }

    private static void Tick()
    {
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Goomba test timeout");
            if (checks.MoveNext()) return;
            Finish(0, "PASS: " + passed + " Goomba checks");
        }
        catch (Exception error) { Finish(1, error.ToString()); }
    }

    private static void Finish(int code, string message)
    {
        EditorApplication.update -= Tick;
        SessionState.SetBool("GoombaRegression", false);
        Directory.CreateDirectory("Logs");
        File.WriteAllText("Logs/goomba-regression-result.txt", message);
        Debug.Log(message);
        EditorApplication.Exit(code);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        passed++;
        Debug.Log("PASS " + message);
    }

    private static IEnumerator Check()
    {
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.layer = 6;
        floor.transform.position = new Vector3(0f, -0.5f, 0f);
        floor.transform.localScale = new Vector3(4f, 1f, 4f);
        WorldStateManager world = WorldStateManager.Get();
        PaneManager.Get();
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PREFABS/Goomba.prefab");
        Require(source != null, "Goomba prefab imports");
        GameObject enemyObject = UnityEngine.Object.Instantiate(source, new Vector3(0f, 0.05f, 0f), Quaternion.identity);
        Goomba enemy = enemyObject.GetComponent<Goomba>();
        EnemyHealth enemyHealth = enemyObject.GetComponent<EnemyHealth>();
        EnemyDimension enemyDimension = enemyObject.GetComponent<EnemyDimension>();
        EnemyMovement enemyMovement = enemyObject.GetComponent<EnemyMovement>();
        EnemyContactCombat enemyCombat = enemyObject.GetComponent<EnemyContactCombat>();
        Require(enemy != null && enemyHealth != null && enemyDimension != null &&
                enemyMovement != null && enemyCombat != null,
            "Goomba is composed from generic enemy components");
        Require(enemy.Health == 1 && enemy.ContactDamage == 1 &&
                enemyHealth.Defense == 0 &&
                enemyMovement.Space == EnemyMovement.MovementSpace.Flat2D,
            "Default combat values and fixed 2D mode");
        Require(enemyObject.GetComponent<Rigidbody>().isKinematic, "Player cannot push the enemy rigidbody");

        float waitUntil = Time.time + 0.2f;
        while (Time.time < waitUntil) yield return null;
        float startX = enemyObject.transform.position.x;
        waitUntil = Time.time + 0.3f;
        while (Time.time < waitUntil) yield return null;
        float initialPatrolDirection = Mathf.Sign(enemyObject.transform.position.x - startX);
        Require(Mathf.Abs(enemyObject.transform.position.x - startX) > 0.2f,
            "2D patrol walks sideways: x=" + enemyObject.transform.position.x +
            " velocity=" + enemyObject.GetComponent<Rigidbody>().velocity +
            " collider=" + enemyObject.GetComponent<BoxCollider>().enabled);
        float furthestPatrolPosition = enemyObject.transform.position.x * initialPatrolDirection;
        waitUntil = Time.time + 2f;
        while (Time.time < waitUntil)
        {
            furthestPatrolPosition = Mathf.Max(furthestPatrolPosition,
                enemyObject.transform.position.x * initialPatrolDirection);
            yield return null;
        }
        Require(furthestPatrolPosition < 2f &&
                enemyObject.transform.position.x * initialPatrolDirection <
                furthestPatrolPosition - 0.2f,
            "2D patrol turns before the ledge");

        world.ChangeWorldState(WorldState.Flipped3d);
        enemyObject.transform.position = new Vector3(0f, 0.05f, 0f);
        waitUntil = Time.time + 0.35f;
        while (Time.time < waitUntil) yield return null;
        Require(Mathf.Abs(enemyObject.transform.position.x) > 0.2f &&
                !enemyObject.GetComponent<BoxCollider>().enabled,
            "2D enemy remains non-interactive but keeps moving when the player flips");
        UnityEngine.Object.Destroy(enemyObject);

        GameObject dimensionTarget = UnityEngine.Object.Instantiate(source);
        EnemyDimension targetDimension = dimensionTarget.GetComponent<EnemyDimension>();
        var presenceField = typeof(EnemyDimension).GetField("presence",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        presenceField.SetValue(targetDimension, EnemyDimension.Presence.Flat3DOnly);
        world.ChangeWorldState(WorldState.Flat2d);
        Require(!dimensionTarget.GetComponent<Collider>().enabled &&
                !dimensionTarget.GetComponentInChildren<Renderer>().enabled,
            "Flat 3D enemy is hidden and non-interactive in 2D");
        world.ChangeWorldState(WorldState.Flipped3d);
        Require(dimensionTarget.GetComponent<Collider>().enabled &&
                dimensionTarget.GetComponentInChildren<Renderer>().enabled,
            "Flat 3D enemy is visible and interactive in 3D");
        UnityEngine.Object.Destroy(dimensionTarget);

        GameObject hudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PREFABS/PlayerHUD.prefab");
        Require(hudPrefab != null && hudPrefab.GetComponent<PlayerHUD>() != null,
            "Standalone player HUD prefab imports");
        Require(source.GetComponent<EnemyGravity>() != null &&
                source.GetComponent<EnemyGravity>().GravityEnabled &&
                source.GetComponent<EnemyVisualFacing>() != null,
            "Ground enemy uses gravity and paper-style visual facing");

        GameObject swoopPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PREFABS/Swoop.prefab");
        Require(swoopPrefab != null && swoopPrefab.GetComponent<SwoopAI>() != null &&
                !swoopPrefab.GetComponent<EnemyGravity>().GravityEnabled &&
                swoopPrefab.GetComponent<EnemyVisualFacing>() != null,
            "Swoop prefab uses shared enemy systems without gravity");
        GameObject cleftPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PREFABS/Cleft.prefab");
        Require(cleftPrefab != null && cleftPrefab.GetComponent<CleftAI>() != null &&
                cleftPrefab.GetComponent<EnemyHealth>().MaxHealth == 10 &&
                cleftPrefab.GetComponent<EnemyHealth>().Defense == 5,
            "Cleft prefab has dash AI and high-defense combat stats");

        foreach (WorldState mode in new[] { WorldState.Flat2d })
        {
            GameObject playerObject = UnityEngine.Object.Instantiate(
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PREFABS/player.prefab"));
            PlayerStateManager player = playerObject.GetComponentInChildren<PlayerStateManager>();
            PlayerHealth health = player.GetComponent<PlayerHealth>();
            PlayerDamage damage = player.GetComponent<PlayerDamage>();
            Require(health != null && damage != null && damage.StompDamage == 1,
                mode + " player has base stomp damage and health");
            Require(health.Defense == 0 && Mathf.Approximately(damage.NoDamageStunDuration, 0.5f),
                "Player defense and blocked-stomp stun defaults");
            player.enabled = false;
            PlayerGravity gravity = player.GetComponent<PlayerGravity>();
            if (gravity != null) gravity.enabled = false;
            player.rb.useGravity = false;
            player.rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            world.ChangeWorldState(mode);
            player.is2d = mode == WorldState.Flat2d;
            if (mode == WorldState.Flipped3d) player.rb.constraints &= ~RigidbodyConstraints.FreezePositionZ;

            GameObject target = UnityEngine.Object.Instantiate(source, new Vector3(0f, 0.05f, 0f), Quaternion.identity);
            target.GetComponent<EnemyMovement>().enabled = false;
            EnemyHealth targetHealth = target.GetComponent<EnemyHealth>();
            player.rb.position = new Vector3(0f, 2.5f, 0f);
            player.rb.velocity = new Vector3(0f, -4f, 0f);
            float until = Time.time + 1f;
            while (target.activeSelf && Time.time < until) yield return null;
            Require(!target.activeSelf && targetHealth.CurrentHealth == 0 && health.hp == health.maxHp,
                mode + " downward top contact stomps without hurting player");

            target = UnityEngine.Object.Instantiate(source, new Vector3(0f, 0.05f, 0f), Quaternion.identity);
            target.GetComponent<EnemyMovement>().enabled = false;
            int before = health.hp;
            player.rb.position = mode == WorldState.Flat2d ? new Vector3(-2f, 1f, 0f) : new Vector3(0f, 1f, -2f);
            player.rb.velocity = mode == WorldState.Flat2d ? Vector3.right * 4f : Vector3.forward * 4f;
            until = Time.time + 1f;
            while (health.hp == before && Time.time < until) yield return null;
            Require(health.hp == before - 1 && target.activeSelf,
                mode + " side contact damages player without stomping enemy");
            Require(player.IsMovementLocked && player.rb.velocity.y >= 0f,
                mode + " side contact knocks back and briefly locks movement");
            UnityEngine.Object.Destroy(target);
            UnityEngine.Object.Destroy(playerObject);
            yield return null;
        }

        EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/GoombaDemo.unity",
            new LoadSceneParameters(LoadSceneMode.Single));
        for (int i = 0; i < 30; i++) yield return null;

        world = WorldStateManager.Get();
        PaneManager panes = PaneManager.Get();
        Pane frontPane = null;
        Pane backPane = null;
        foreach (Pane pane in UnityEngine.Object.FindObjectsOfType<Pane>())
        {
            if (pane.paneIndex == 0) frontPane = pane;
            if (pane.paneIndex == 1) backPane = pane;
        }
        Require(frontPane != null && backPane != null, "GoombaDemo has front and back panes");
        world.ChangeWorldState(WorldState.Flat2d);
        panes.SetCurrentPane(frontPane);

        foreach (string prefabPath in new[]
                 { "Assets/PREFABS/Goomba.prefab", "Assets/PREFABS/Cleft.prefab" })
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            GameObject groundedEnemy = UnityEngine.Object.Instantiate(prefab,
                new Vector3(prefabPath.Contains("Cleft") ? 2f : 0f, 0.05f, 0f),
                Quaternion.identity);
            PaneMember member = groundedEnemy.AddComponent<PaneMember>();
            member.pane = frontPane;
            panes.RefreshMembers();

            float settleUntil = Time.time + 0.35f;
            while (Time.time < settleUntil) yield return null;
            Vector3 activePosition = groundedEnemy.transform.position;

            panes.SetCurrentPane(backPane);
            float inactiveUntil = Time.time + 0.6f;
            while (Time.time < inactiveUntil) yield return null;
            EnemyGravity enemyGravity = groundedEnemy.GetComponent<EnemyGravity>();
            Require(!groundedEnemy.GetComponent<EnemyDimension>().IsInteractive &&
                    Vector3.Distance(groundedEnemy.transform.position, activePosition) < 0.01f &&
                    Mathf.Approximately(enemyGravity.VerticalSpeed, 0f),
                groundedEnemy.name + " pauses with cleared gravity while its pane is inactive");

            panes.SetCurrentPane(frontPane);
            float resumedUntil = Time.time + 0.35f;
            while (Time.time < resumedUntil) yield return null;
            Require(groundedEnemy.GetComponent<EnemyDimension>().IsInteractive &&
                    groundedEnemy.transform.position.y > -0.05f,
                groundedEnemy.name + " resumes without falling through the GoombaDemo floor");
            UnityEngine.Object.Destroy(groundedEnemy);
        }

        GameObject facingEnemy = UnityEngine.Object.Instantiate(
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PREFABS/Goomba.prefab"),
            new Vector3(0f, 0.05f, 0f), Quaternion.identity);
        EnemyMovement facingMovement = facingEnemy.GetComponent<EnemyMovement>();
        facingMovement.enabled = false;
        facingMovement.SetVisualDirection(Vector3.right);
        float facingUntil = Time.time + 0.3f;
        while (Time.time < facingUntil) yield return null;
        Quaternion facingBeforeFlip = facingEnemy.GetComponentInChildren<Renderer>().transform.rotation;
        world.ChangeWorldState(WorldState.Flipped3d);
        facingUntil = Time.time + 0.3f;
        while (Time.time < facingUntil) yield return null;
        Require(Quaternion.Angle(facingBeforeFlip,
                    facingEnemy.GetComponentInChildren<Renderer>().transform.rotation) < 0.1f,
            "Enemy facing remains tied to its fixed presence when the player changes dimensions");
    }
}
