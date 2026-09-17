using UnityEngine;

public interface IEnemyMovementSource
{
    Vector3 CurrentMoveDirection { get; }
}

public class EnemyVisualFacing : MonoBehaviour
{
    [SerializeField] private Transform visualRoot;
    [SerializeField, Min(0f)] private float rotationSpeed = 720f;
    [Tooltip("Enable when the source sprite is drawn looking left.")]
    [SerializeField] private bool artworkFacesLeft = true;

    private IEnemyMovementSource movement;
    private EnemyDimension dimension;
    private Vector3 initialEuler;

    private void Awake()
    {
        if (visualRoot == null)
        {
            Renderer foundVisual = GetComponentInChildren<Renderer>();
            if (foundVisual != null) visualRoot = foundVisual.transform;
        }

        foreach (MonoBehaviour behaviour in GetComponents<MonoBehaviour>())
        {
            IEnemyMovementSource source = behaviour as IEnemyMovementSource;
            if (source == null) continue;
            movement = source;
            break;
        }

        dimension = GetComponent<EnemyDimension>();
        if (visualRoot != null) initialEuler = visualRoot.eulerAngles;
    }

    private void LateUpdate()
    {
        if (visualRoot == null || movement == null) return;

        Vector3 direction = movement.CurrentMoveDirection;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) return;

        float targetY;
        bool usesFlatFacing = dimension == null ||
                              dimension.DimensionPresence == EnemyDimension.Presence.Flat2DOnly;
        if (usesFlatFacing)
        {
            bool movingRight = direction.x >= 0f;
            targetY = movingRight == artworkFacesLeft ? 180f : 0f;
        }
        else
        {
            bool movingForward = direction.z >= 0f;
            targetY = movingForward == artworkFacesLeft ? 90f : 270f;
        }

        Quaternion target = Quaternion.Euler(initialEuler.x, targetY, initialEuler.z);
        visualRoot.rotation = Quaternion.RotateTowards(visualRoot.rotation, target,
            rotationSpeed * Time.deltaTime);
    }
}
