using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Projectile : MonoBehaviour
{
    public float damage = 10f;
    public float lifetime = 5f;

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            var health = other.GetComponent<PlayerHealth>();
            if (health != null) health.TakeDamage(damage);
            Destroy(gameObject);
        }

        if (other.CompareTag("Wall"))
        {
            Destroy(gameObject);
        }
    }
}
