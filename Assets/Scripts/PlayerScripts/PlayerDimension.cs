using System.Collections.Generic;
using UnityEngine;

// Depth/pane handling stays separate from the original movement states.
public class PlayerDimension : MonoBehaviour
{
    private PlayerStateManager player;
    private PaneManager panes;
    private Collider[] body;
    private readonly List<Collider> ignored = new List<Collider>();

    private void Awake()
    {
        player = GetComponent<PlayerStateManager>();
        body = GetComponents<Collider>();
        panes = PaneManager.Get();
    }

    private void Start() { ResetDepth(); }

    public void ResetDepth()
    {
        RestoreCollisions();
        panes.TrackDepth(transform.position.z);
        LockDepth();
    }

    public void BeginFlip()
    {
        panes.TrackDepth(transform.position.z);
    }

    public void CompleteFlip()
    {
        panes.TrackDepth(transform.position.z);
        player.worldStateManager.CompleteFlip();
        // All pane members and extended hitboxes have now applied their final shapes.
        Physics.SyncTransforms();
        if (player.is2d)
        {
            IgnoreNewOverlaps();
            TryRecenter();
        }
        LockDepth();
    }

    private void LockDepth()
    {
        if (player.is2d) player.rb.constraints |= RigidbodyConstraints.FreezePositionZ;
        else player.rb.constraints &= ~RigidbodyConstraints.FreezePositionZ;
    }

    private void FixedUpdate()
    {
        if (!player.is2d && !player.currentlyFlipping) panes.TrackDepth(transform.position.z);
        for (int i = ignored.Count - 1; i >= 0; i--)
        {
            Collider other = ignored[i];
            if (other != null && other.enabled && OverlapsBody(other)) continue;
            SetIgnored(other, false);
            ignored.RemoveAt(i);
        }
        if (player.is2d && !player.currentlyFlipping) TryRecenter();
    }

    // Check the whole depth corridor, not just the destination. A blocked
    // return keeps its depth and retries after the player walks clear in 2D.
    public bool TryRecenter()
    {
        if (ignored.Count != 0) return false;
        float delta = panes.CurrentDepth - player.rb.position.z;
        if (Mathf.Abs(delta) < 0.001f) return true;
        Physics.SyncTransforms();
        foreach (Collider own in body)
        {
            if (!own.enabled || own.isTrigger) continue;
            Bounds bounds = own.bounds;
            Vector3 half = bounds.extents - Vector3.one * 0.01f;
            half = Vector3.Max(half, Vector3.one * 0.001f);
            half.z += Mathf.Abs(delta) * 0.5f;
            Vector3 center = bounds.center + Vector3.forward * (delta * 0.5f);
            foreach (Collider other in Physics.OverlapBox(center, half, Quaternion.identity,
                         ~0, QueryTriggerInteraction.Ignore))
            {
                if (other.attachedRigidbody == player.rb) continue;
                if (Physics.GetIgnoreLayerCollision(own.gameObject.layer, other.gameObject.layer)) continue;
                if (Physics.GetIgnoreCollision(own, other)) continue;
                if (other.bounds.max.y <= bounds.min.y + 0.02f) continue;
                return false;
            }
        }
        Vector3 position = new Vector3(player.rb.position.x, player.rb.position.y, panes.CurrentDepth);
        // The flip also writes Transform rotation. Keep both poses in sync so
        // SyncTransforms cannot restore the pre-snap Z from that dirty transform.
        player.transform.position = position;
        player.rb.position = position;
        player.rb.velocity = new Vector3(player.rb.velocity.x, player.rb.velocity.y, 0f);
        Physics.SyncTransforms();
        return true;
    }

    private bool OverlapsBody(Collider other)
    {
        foreach (Collider own in body)
            if (own.enabled && !own.isTrigger && own.bounds.Intersects(other.bounds)) return true;
        return false;
    }

    private void IgnoreNewOverlaps()
    {
        foreach (Collider own in body)
        {
            if (!own.enabled || own.isTrigger) continue;
            Bounds bounds = own.bounds;
            foreach (Collider other in Physics.OverlapBox(bounds.center, bounds.extents,
                         Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
            {
                if (other.attachedRigidbody == player.rb || ignored.Contains(other)) continue;
                if (Physics.GetIgnoreLayerCollision(own.gameObject.layer, other.gameObject.layer)) continue;
                if (Physics.GetIgnoreCollision(own, other)) continue;
                if (!Physics.ComputePenetration(own, own.transform.position, own.transform.rotation,
                    other, other.transform.position, other.transform.rotation, out Vector3 direction, out float distance)) continue;
                // A short obstacle may report UP as its shortest escape direction.
                // It is only supporting ground if its top is actually below our feet.
                if (distance < 0.001f || other.bounds.max.y <= bounds.min.y + 0.02f) continue;
                ignored.Add(other);
                SetIgnored(other, true);
            }
        }
    }

    private void SetIgnored(Collider other, bool value)
    {
        if (other == null) return;
        foreach (Collider own in body)
            if (own != null && !own.isTrigger) Physics.IgnoreCollision(own, other, value);
    }

    private void RestoreCollisions()
    {
        foreach (Collider other in ignored) SetIgnored(other, false);
        ignored.Clear();
    }

    public bool IsGrounded()
    {
        if (player.groundCheck == null) return false;
        foreach (Collider hit in Physics.OverlapBox(player.groundCheck.position, player.boxSize,
                     player.transform.rotation, player.ground, QueryTriggerInteraction.Ignore))
            if (hit.attachedRigidbody != player.rb && !ignored.Contains(hit)) return true;
        return false;
    }

    private void OnDisable() { RestoreCollisions(); }
}
