using UnityEngine;

/// <summary>
/// Connects reusable enemy gameplay events to parameters in an Animator Controller.
/// An enemy's controller may omit parameters it does not use.
/// </summary>
[RequireComponent(typeof(EnemyHealth))]
public class EnemyAnimator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;

    [Header("Standard Parameters")]
    [SerializeField] private string movingParameter = "Moving";
    [SerializeField] private string hurtParameter = "Hurt";
    [SerializeField] private string startleParameter = "Startle";
    [SerializeField] private string attackParameter = "Attack";

    private EnemyHealth health;
    private IEnemyMovementSource movement;
    private int movingHash;
    private int hurtHash;
    private int startleHash;
    private int attackHash;
    private bool hasMoving;
    private bool hasHurt;
    private bool hasStartle;
    private bool hasAttack;

    private void Awake()
    {
        health = GetComponent<EnemyHealth>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        foreach (MonoBehaviour behaviour in GetComponents<MonoBehaviour>())
        {
            IEnemyMovementSource source = behaviour as IEnemyMovementSource;
            if (source == null) continue;
            movement = source;
            break;
        }

        CacheParameters();
    }

    private void Start()
    {
        if (animator == null)
        {
            Debug.LogError("EnemyAnimator could not find an Animator.", this);
            return;
        }

        if (movement != null && !hasMoving)
            Debug.LogWarning("Animator Controller needs a Bool parameter named '" +
                             movingParameter + "' to play movement animations.", this);
        if (!hasHurt)
            Debug.LogWarning("Animator Controller needs a Trigger parameter named '" +
                             hurtParameter + "' to play the hurt animation.", this);
    }

    private void OnEnable()
    {
        if (health != null) health.OnDamaged += HandleDamaged;
    }

    private void OnDisable()
    {
        if (health != null) health.OnDamaged -= HandleDamaged;
    }

    private void Update()
    {
        if (animator == null || movement == null || !hasMoving) return;
        animator.SetBool(movingHash, movement.CurrentMoveDirection.sqrMagnitude > 0.0001f);
    }

    private void HandleDamaged(int amount, int remainingHealth)
    {
        PlayHurt();
    }

    [ContextMenu("Test Hurt Animation")]
    public void PlayHurt()
    {
        if (animator != null && hasHurt) animator.SetTrigger(hurtHash);
    }

    public void PlayStartle()
    {
        if (animator != null && hasStartle) animator.SetTrigger(startleHash);
    }

    public void PlayAttack()
    {
        if (animator != null && hasAttack) animator.SetTrigger(attackHash);
    }

    private void CacheParameters()
    {
        if (animator == null) return;

        movingHash = Animator.StringToHash(movingParameter);
        hurtHash = Animator.StringToHash(hurtParameter);
        startleHash = Animator.StringToHash(startleParameter);
        attackHash = Animator.StringToHash(attackParameter);

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.nameHash == movingHash && parameter.type == AnimatorControllerParameterType.Bool)
                hasMoving = true;
            else if (parameter.nameHash == hurtHash && parameter.type == AnimatorControllerParameterType.Trigger)
                hasHurt = true;
            else if (parameter.nameHash == startleHash && parameter.type == AnimatorControllerParameterType.Trigger)
                hasStartle = true;
            else if (parameter.nameHash == attackHash && parameter.type == AnimatorControllerParameterType.Trigger)
                hasAttack = true;
        }
    }
}
