using NoClue.Constancts;
using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class CastusController : MonoBehaviour
{

    [Header("Patrol")]
    [SerializeField] private Transform m_CheckpointA;
    [SerializeField] private Transform m_CheckpointB;
    [SerializeField] private float m_PatrolSpeed = 2f;
    [SerializeField] private float m_TurnSpeed = 2f;
    
    [Header("Chase & Attack")]
    [SerializeField] private float m_ChaseSpeed = 3.5f;
    [SerializeField] private float m_DetectionRadius = 3f;
    [SerializeField] private float m_DetectionMaxRadius = 6f;
    [SerializeField] private float m_AttackRange = 1.5f;
    [SerializeField] private float m_AttackCooldown = 1.2f;

    [Header("Death")]
    [SerializeField] private GameObject m_DeathSplashFX;

    [SerializeField] private Transform m_Player;
    [SerializeField] private Animator m_Animator;
    [SerializeField] private LayerMask m_PlayerLayer;

    [SerializeField]
    private Cactuse m_Castuse;

    private bool m_OnPointB = false;
    private CactusState currentState, previousState;
    private float m_IdleTime = 0;
    private float m_CurrentIdleTime = 0;

    private AnimationName m_CurrentAnimation;

    private void Start()
    {
        if(m_Animator == null) m_Animator = GetComponent<Animator>();
        if (m_Player == null)  m_Player = GameObject.FindGameObjectWithTag("Player").transform;

        m_IdleTime = Random.Range(3f, 6f);
        ManageCactusState(CactusState.Patrol);
    }


    private void Update()
    {
        if (currentState == CactusState.Dead)
            return;

        UpdateState();
    }
    private void UpdateState()
    {
        if (previousState == currentState)
            return;

        switch (currentState)
        {
            case CactusState.Patrol:
                HandlePatrolState();
                break;

            case CactusState.Idle:
                HandleIdleState();
                break;

            case CactusState.Chase:
                HandleChaseState();
                break;

            case CactusState.Attack:
                HandleAttackState();
                break;
        }
    }

    private void HandlePatrolState()
    {
        DetactPlayer();
        CactuseWalk();
        SetWalkToIdleTime();
    }

    private void HandleIdleState()
    {
        DetactPlayer();
    }

    private void HandleChaseState()
    {
        // Chase logic later
    }

    private void HandleAttackState()
    {
        // Attack logic later
    }

    private void ManageCactusState(CactusState newState)
    {
        if (currentState == newState)
            return;

        previousState = currentState;
        currentState = newState;
        AnimateCactus(newState);

        if (newState == CactusState.Idle)
        {
            m_IdleTime = Random.Range(3f, 6f);
            m_CurrentIdleTime = 0;
        }
    }

    private void CactuseWalk() 
    {
        Vector3 targetPosition = m_OnPointB ? m_CheckpointA.position : m_CheckpointB.position;
        
        targetPosition = new Vector3(targetPosition.x, transform.position.y, targetPosition.z);
        Vector3 currentPosition = transform.position;
        
        transform.position = Vector3.MoveTowards(currentPosition, targetPosition, Time.deltaTime * m_PatrolSpeed);

        if(Vector3.Distance(transform.position, targetPosition) < 0.001f)
        {
            m_OnPointB = !m_OnPointB;
            transform.LookAt(currentPosition);
        }
    }

    private void SetWalkToIdleTime()
    {

        if (currentState != CactusState.Patrol)
            return;

        m_CurrentIdleTime += Time.deltaTime;
        if (m_CurrentIdleTime >= m_IdleTime)
        {
            ManageCactusState(CactusState.Idle);
            m_CurrentIdleTime = 0;
        }
    }

    private void CactuseIdle()
    {
        m_IdleTime = Random.Range(3f, 6f);
        ManageCactusState(CactusState.Idle);
    }

    private void AnimateCactus(CactusState cactusState)
    {
        AnimationName anim = ChooseAnimation(cactusState);

        if (m_CurrentAnimation == anim)
            return;

        m_CurrentAnimation = anim;
        m_Animator.Play(anim.ToString());
    }

    private AnimationName ChooseAnimation(CactusState cactusState)
    {
        List<AnimationName> animationName = new List<AnimationName>();
        animationName = m_Castuse.GetCastuseAnumations(cactusState);
         
        return animationName[Random.Range(0, animationName.Count)]; 
    }

    private void OnDrawGizmosSelected() 
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, m_DetectionRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, m_AttackRange);
    }

    private void DetactPlayer()
    {
        Vector3 origin = transform.position;
        Vector3 direction = transform.forward;
        RaycastHit hit;

        if (Physics.SphereCast(origin, m_DetectionRadius, direction, out hit, m_DetectionMaxRadius, m_PlayerLayer))
        {
            Debug.Log("Hit player: " + hit.collider.name);
             
            ManageCactusState((Vector3.Distance(origin, hit.transform.position) >= m_DetectionRadius) ? CactusState.Chase : CactusState.Attack);
        }
    }
}
