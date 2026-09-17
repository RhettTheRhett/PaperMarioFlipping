using UnityEngine;

// A pane is one depth slice of this map. Parent level geometry under a Pane
// to make it part of that area when returning to 2D.
public class Pane : MonoBehaviour
{
    [Header("Identity")]
    public int paneIndex = 0;

    [Header("Depth")]
    [Tooltip("When true the pane depth is this object's world Z, otherwise depthOverride is used.")]
    public bool useTransformZ = true;
    public float depthOverride = 0f;

    [Tooltip("How deep the slice is. Hitboxes in this pane are extruded to this thickness while flat.")]
    public float thickness = 10f;

    [Header("Members")]
    [Tooltip("Give every child that has a collider or renderer a PaneMember on startup.")]
    public bool autoAssignChildren = true;

    [Header("Camera")]
    public bool overrideCameraOffset = false;
    public Vector3 flatCameraOffset = new Vector3(0f, 0f, -10f);

    public float Depth { get { return useTransformZ ? transform.position.z : depthOverride; } }
    public float HalfThickness { get { return Mathf.Max(0.01f, thickness) * 0.5f; } }

    public bool Contains(float worldZ)
    {
        return Mathf.Abs(worldZ - Depth) <= HalfThickness;
    }

    public float DistanceTo(float worldZ)
    {
        return Mathf.Abs(worldZ - Depth);
    }

    private void Awake()
    {
        if (autoAssignChildren) AssignChildren();
    }

    private void OnEnable()
    {
        PaneManager.Get().RegisterPane(this);
    }

    private void OnDisable()
    {
        if (PaneManager.Exists) PaneManager.Get().UnregisterPane(this);
    }

    public void AssignChildren()
    {
        Transform[] children = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];
            if (child == transform) continue;

            // Only the highest node of a model needs a member; it drives the
            // colliders and renderers underneath it.
            if (child.parent != transform) continue;
            if (child.GetComponent<PaneMember>() != null) continue;

            bool interactive = child.GetComponentInChildren<Collider>(true) != null
                               || child.GetComponentInChildren<Renderer>(true) != null;
            if (!interactive) continue;

            PaneMember member = child.gameObject.AddComponent<PaneMember>();
            member.pane = this;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 center = new Vector3(transform.position.x, transform.position.y, Depth);
        Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.35f);
        Gizmos.DrawWireCube(center, new Vector3(40f, 20f, Mathf.Max(0.01f, thickness)));
    }
}
