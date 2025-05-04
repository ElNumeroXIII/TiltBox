using UnityEngine;

public class CajaTest : MonoBehaviour
{
    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        Input.gyro.enabled = true;
    }

    void FixedUpdate()
    {
        if (rb == null)
        {
            Debug.LogWarning("Rigidbody2D no asignado.");
            return;
        }

        // Usa el giroscopio
        Vector3 tilt = Input.gyro.gravity;

        // Aplica fuerza basada en la inclinación (escala ajustable)
        Vector2 fuerza = new Vector2(tilt.x, tilt.y) * 10f; // Puedes ajustar el multiplicador
        rb.AddForce(fuerza, ForceMode2D.Force);
    }

    void OnTriggerEnter2D(Collider2D other)
{
    if (other.CompareTag("Meta"))
    {
        Debug.Log("¡Nivel completado!");
        // Aquí puedes mostrar UI, pausar el juego, cargar otro nivel, etc.
    }
}

}


