using UnityEngine;

public class FlyingChair : MonoBehaviour
{
    public float speed = 5f;
    public float destroyX = 250f;
    private Vector2 direction = Vector2.right;

    void Update()
    {
        transform.Translate(direction * speed * Time.deltaTime);

        if (transform.position.x > destroyX)
            Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            var gm = FindAnyObjectByType<GameManager>();
            if (gm != null) gm.RestartLevel();
        }
    }
}
