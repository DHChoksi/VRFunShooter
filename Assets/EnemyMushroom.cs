using System.Collections;
using UnityEngine;
using static NoClue.Constancts.Constants;

public class EnemyMushroom : MonoBehaviour
{
    [Header("Enemy Settings")]
    [SerializeField] private EnemyState currentState = EnemyState.Idle;
    [SerializeField] private Animator animator;
    [SerializeField] private Transform player;
    [SerializeField] private HealthBar healthBar;

    [Header("State Timings")]
    [SerializeField] private float idleDurationMin = 5f;
    [SerializeField] private float idleDurationMax = 8f;
    [SerializeField] private float moveSpeed = 2f;

    [Header("Detection Settings")]
    [SerializeField, Tooltip("Distance at which the enemy begins to sense the player")]
    private float senseDistance = 5f;
    [SerializeField, Tooltip("Distance at which the enemy starts chasing/attacking the player")]
    private float attackDistance = 2.5f;
    [SerializeField, Tooltip("Distance after which the enemy stops chasing and returns to idle")]
    private float loseSightDistance = 6f;

    [Header("Combat Settings")]
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private int currentHealth;

    private bool isDead = false;
    private bool isBusy = false;
    private Vector2 startPosition;

    // --- Animation Pools ---
    [Header("Idle Animations")]
    [SerializeField]
    private string[] idleAnimations = {
        "Mushroom_IdleNormalAngry",
        "Mushroom_IdleBattleAngry",
        "Mushroom_IdlePlantToBattleAngry"
    };

    [Header("Walk Animations")]
    [SerializeField]
    private string[] walkAnimations = {
        "Mushroom_walkFWDAngry",
        "Mushroom_walkLFTAngry",
        "Mushroom_walkRGTAngry"
    };

    [Header("Attack Animations")]
    [SerializeField]
    private string[] attackAnimations = {
        "Mushroom_Attack01Angry",
        "Mushroom_Attack02Angry",
        "Mushroom_Attack03Angry"
    };

    [Header("Other Animations")]
    [SerializeField] private string hurtAnimation = "Mushroom_GetHitAngry";
    [SerializeField] private string deathAnimation = "Mushroom_DieAngry";
    [SerializeField] private string senseAnimation = "Mushroom_SenseSomethingStartAngry";
    [SerializeField] private string runAnimation = "Mushroom_runFWDAngry";

    private Coroutine stateRoutine;

    private void OnEnable()
    {
        startPosition = transform.position;
        currentHealth = maxHealth;
        isDead = false;
        healthBar.SetMaxHealth(maxHealth);

        // Always start idle
        currentState = EnemyState.Idle;
        stateRoutine = StartCoroutine(StateMachine());
    }

    private IEnumerator StateMachine()
    {
        while (!isDead)
        {
            switch (currentState)
            {
                case EnemyState.Idle:
                    yield return IdleState();
                    break;
                case EnemyState.Walk:
                    yield return WalkState();
                    break;
                case EnemyState.SensePlayer:
                    yield return SensePlayerState();
                    break;
                case EnemyState.Chase:
                    yield return ChaseState();
                    break;
                case EnemyState.Attack:
                    yield return AttackState();
                    break;
                case EnemyState.Hurt:
                    yield return HurtState();
                    break;
                case EnemyState.Dead:
                    yield return DeadState();
                    break;
            }

            yield return null;
        }
    }

    // --- State Methods ---
    private IEnumerator IdleState()
    {
        PlayRandomAnimation(idleAnimations);
        float waitTime = Random.Range(idleDurationMin, idleDurationMax);
        float timer = 0f;

        while (timer < waitTime)
        {
            float distance = Vector2.Distance(transform.position, player.position);

            if (distance <= senseDistance)
            {
                currentState = EnemyState.SensePlayer;
                yield break;
            }

            timer += Time.deltaTime;
            yield return null;
        }

        // Random chance to start walking after idle
        if (Random.value > 0.5f)
            currentState = EnemyState.Walk;
        else
            currentState = EnemyState.Idle;
    }

    private IEnumerator WalkState()
    {
        PlayRandomAnimation(walkAnimations);
        Vector2 targetPos = startPosition + new Vector2(Random.Range(-2f, 2f), 0f);
        float walkDuration = Random.Range(2f, 4f);
        float timer = 0f;

        while (timer < walkDuration)
        {
            float distance = Vector2.Distance(transform.position, player.position);
            if (distance <= senseDistance)
            {
                currentState = EnemyState.SensePlayer;
                yield break;
            }

            transform.position = Vector2.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
            timer += Time.deltaTime;
            yield return null;
        }

        currentState = EnemyState.Idle;
    }

    private IEnumerator SensePlayerState()
    {
        animator.Play(senseAnimation);
        yield return new WaitForSeconds(1f);
        currentState = EnemyState.Chase;
    }

    private IEnumerator ChaseState()
    {
        animator.Play(runAnimation);

        while (true)
        {
            float distance = Vector2.Distance(transform.position, player.position);

            if (distance > loseSightDistance)
            {
                // Lost the player, resume normal idle/walk behavior
                currentState = EnemyState.Idle;
                yield break;
            }

            if (distance <= attackDistance)
            {
                currentState = EnemyState.Attack;
                yield break;
            }

            // Move towards player
            transform.position = Vector2.MoveTowards(transform.position, player.position, moveSpeed * 1.5f * Time.deltaTime);
            yield return null;
        }
    }

    private IEnumerator AttackState()
    {
        PlayRandomAnimation(attackAnimations);
        yield return new WaitForSeconds(1f);

        // If player is still close, attack again
        float distance = Vector2.Distance(transform.position, player.position);
        if (distance <= attackDistance)
            currentState = EnemyState.Attack;
        else
            currentState = EnemyState.Idle;
    }

    private IEnumerator HurtState()
    {
        animator.Play(hurtAnimation);
        yield return new WaitForSeconds(0.5f);
        currentState = EnemyState.Idle;
    }

    private IEnumerator DeadState()
    {
        isDead = true;
        animator.Play(deathAnimation);
        yield return new WaitForSeconds(1.5f);
        gameObject.SetActive(false); // Object pooling disable
    }

    // --- Damage Handling ---
    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        healthBar.SetHealth(currentHealth);

        if (currentHealth <= 0)
        {
            currentState = EnemyState.Dead;
        }
        else
        {
            currentState = EnemyState.Hurt;
        }
    }

    // --- Helper Method ---
    private void PlayRandomAnimation(string[] animationList)
    {
        if (animationList == null || animationList.Length == 0)
            return;

        int index = Random.Range(0, animationList.Length);
        animator.Play(animationList[index]);
    }
}
