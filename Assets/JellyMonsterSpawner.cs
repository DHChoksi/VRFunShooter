using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JellyMonsterSpawner : MonoBehaviour
{
    [SerializeField]
    private GameObject m_Player = null;

    [SerializeField]
    private float m_EnemyRadius = 1.5f;

    [SerializeField]
    private bool m_EnemyDefeatedFlag = false;

    [SerializeField]
    private bool m_EnemyActive = false;

    [SerializeField]
    private GameObject m_Enemy = null;

    public float m_MoveDistance = 0.5f; // Distance to move up and down
    public float m_Duration = 1f;     // Duration of one up/down cycle
    public float m_Speed = 1f;

    void Start()
    {
        m_Player = GameObject.FindGameObjectWithTag("Player");
        m_Enemy = transform.GetChild(0).gameObject;
        StartCoroutine(AnimateEnemy());
    }

    IEnumerator AnimateEnemy()
    {

        yield return new WaitForSeconds(0.5f);
        MoveEnemy();
    }

    void MoveEnemy()
    {
        transform.DOMoveY(transform.position.y + m_MoveDistance, m_Duration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetSpeedBased(true);

    }

    void Update()
    {
        if (m_EnemyDefeatedFlag)
        {
            return;
        }

        m_Enemy.transform.LookAt(m_Player.transform.position);

        if(Vector3.Distance(m_Player.transform.position, gameObject.transform.position) < m_EnemyRadius && !m_EnemyActive)
        {
            m_EnemyActive = !m_EnemyActive;
            m_Enemy.SetActive(true);
        }
    }

    public void DestroyEnemy() 
    {
        Debug.Log("EnemyGone");
       
        m_Enemy.SetActive(false);
        Destroy(m_Enemy);
        m_EnemyActive = false;
        gameObject.SetActive(false);
    }

}
