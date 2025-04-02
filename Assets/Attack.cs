using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Attack : MonoBehaviour
{
    [SerializeField]
    private GameObject m_FX;

    void OnCollisionEnter(Collision collision)
    {
        // Check if the collided object has the tag "Enemy"
        if (collision.gameObject.CompareTag("Enemy"))
        {
            Debug.Log("EnemyDetected");
            StartCoroutine(PlayFX());
            collision.gameObject.transform.parent.gameObject.GetComponent<JellyMonsterSpawner>().DestroyEnemy();
        }
    }

    IEnumerator PlayFX() 
    {
        m_FX.SetActive(true);
        m_FX.transform.position = transform.position;
        yield return new WaitForSeconds(1f);
        m_FX.SetActive(false);
    }
}
