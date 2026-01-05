using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class HammerWeapon : MonoBehaviour
{
    [Header("Hammer FX Settings")]
    [Tooltip("Hit effect prefab (e.g. particle or VFX)")]
    [SerializeField] private GameObject hitFX;

    [Tooltip("Layers that trigger hit FX (e.g. Enemy, Default)")]
    [SerializeField] private LayerMask hitLayers;

    [Tooltip("Minimum hammer speed before triggering FX")]
    [SerializeField] private float minImpactSpeed = 1.5f;

    [Tooltip("Destroy spawned FX after this many seconds (0 = never)")]
    [SerializeField] private float fxLifetime = 2f;

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (hitFX == null)
            Debug.LogWarning("[HammerWeapon] No hit effect assigned!", this);
    }

    void OnCollisionEnter(Collision collision)
    {
        // Check if hit object layer is included
        if (((1 << collision.gameObject.layer) & hitLayers) == 0)
            return;

        // Check hammer speed
        if (rb != null && rb.velocity.magnitude < minImpactSpeed)
            return;

        // Spawn FX at first contact point
        if (hitFX != null && collision.contactCount > 0)
        {
            ContactPoint contact = collision.contacts[0];
            Quaternion rot = Quaternion.LookRotation(contact.normal);
            GameObject fx = Instantiate(hitFX, contact.point, rot);

            if (fxLifetime > 0)
                Destroy(fx, fxLifetime);
        }
    }
}
