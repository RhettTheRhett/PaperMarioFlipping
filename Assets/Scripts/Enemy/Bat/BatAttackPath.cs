using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BatAttackPath : MonoBehaviour {
    public QuadraticCurve path;
    public List<Vector3> pathPoints = new List<Vector3>();
    public float speed = 2f;
    public float attackDelay = 2f;
    public float reattackDelay = 2f;

    public Transform playerTransform;
    public Transform enemyTransform;
    public Transform attackEndTransform;

    private LineRenderer lineRenderer;
    public Material lineMaterial;
    public Color lineColor = Color.red;
    public float lineWidth = 0.1f;

    private bool isAttacking = false; // Prevents multiple attacks at once
    private bool playerInTrigger = false; // Tracks if player is in range
    private float sampleTime = 0f;

    void Start() {
        enemyTransform = this.transform;

        // Ensure attackEndTransform is valid
        if (attackEndTransform == null) {
            GameObject fallbackEnd = new GameObject("AttackEndPoint");
            fallbackEnd.transform.position = enemyTransform.position + new Vector3(0, -2, -2);
            attackEndTransform = fallbackEnd.transform;
        }

        // Setup LineRenderer
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.material = lineMaterial;
        lineRenderer.startColor = lineColor;
        lineRenderer.endColor = lineColor;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.positionCount = 0;
    }

    private void OnTriggerEnter(Collider collision) {
        if (collision.CompareTag("Player")) {
            playerTransform = collision.transform;
            playerInTrigger = true;

            if (!isAttacking) {
                StartCoroutine(PrepareAndAttack());
            }
        }
    }

    private void OnTriggerExit(Collider other) {
        if (other.CompareTag("Player")) {
            playerInTrigger = false;
            pathPoints.Clear();
        }
    }

    private IEnumerator PrepareAndAttack() {
        isAttacking = true;

        // Wait before creating the path
        yield return new WaitForSeconds(attackDelay);

        // Ensure player is still in the trigger zone
        if (playerInTrigger) {
            SetPath();
            DrawPath();
            yield return MoveBat();
        }

        isAttacking = false;

        // Wait a bit before checking for reattacks
        yield return new WaitForSeconds(reattackDelay);

        if (playerInTrigger) {
            StartCoroutine(PrepareAndAttack());
        }
    }

    private void SetPath() {
        pathPoints.Clear();
        for (int i = 0; i <= 20; i++) {
            float t = i / 20f;
            pathPoints.Add(path.evaluate3DCurve(t, enemyTransform, playerTransform, attackEndTransform));
        }
    }

    private IEnumerator MoveBat() {
        sampleTime = 0f;

        if (pathPoints.Count == 0) {
            Debug.LogError("Path points are empty! Aborting MoveBat.");
            yield break;
        }

        Vector3 startPosition = transform.position; // Capture the starting position's Z value.

        // First forward movement
        while (sampleTime < 1f) {
            sampleTime += Time.deltaTime * speed;
            int index = Mathf.Clamp(Mathf.FloorToInt(sampleTime * (pathPoints.Count - 1)), 0, pathPoints.Count - 1);

            Vector3 newPosition = pathPoints[index];

            // Keep the bat's Z value consistent with the starting Z value.
            newPosition.z = startPosition.z;

            transform.position = newPosition;

            yield return null;
        }

        // After reaching the end, reverse the movement
        sampleTime = 0f;  // Reset sampleTime for reverse movement

        while (sampleTime < 1f) {
            sampleTime += Time.deltaTime * speed;
            int index = Mathf.Clamp(Mathf.FloorToInt((1 - sampleTime) * (pathPoints.Count - 1)), 0, pathPoints.Count - 1);

            Vector3 newPosition = pathPoints[index];

            // Keep the bat's Z value consistent with the starting Z value.
            newPosition.z = startPosition.z;

            transform.position = newPosition;

            yield return null;
        }

        // Once it returns, you can call a function to "wait" for a bit before repeating the process or resetting.

        Debug.Log("Movement complete. Ready for another attack.");
    }




    private void DrawPath() {
        if (pathPoints.Count == 0) return;

        lineRenderer.positionCount = pathPoints.Count;
        lineRenderer.SetPositions(pathPoints.ToArray());
    }
}
