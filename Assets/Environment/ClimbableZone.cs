using UnityEngine;

/// <summary>
/// Marca una zona como escalable y define sus propiedades
/// Debe tener un Collider con Is Trigger activado
/// </summary>
[RequireComponent(typeof(Collider))]
public class ClimbableZone : MonoBehaviour
{
    [Header("Configuración de Escalada")]
    [Tooltip("Velocidad de escalada vertical en esta zona (m/s)")]
    public float climbSpeed = 2.5f;

    [Tooltip("¿Permitir movimiento horizontal mientras escala?")]
    public bool allowHorizontalMovement = false;

    [Tooltip("Si está activo, el jugador puede saltar mientras escala")]
    public bool canJumpWhileClimbing = false;

    [Header("Visual Feedback")]
    [Tooltip("Color del gizmo en el editor")]
    public Color gizmoColor = new Color(0.2f, 0.8f, 0.2f, 0.3f);

    private Collider _collider;

    void Awake()
    {
        _collider = GetComponent<Collider>();

        // Validación de seguridad
        if (!_collider.isTrigger)
        {
            Debug.LogWarning($"ClimbableZone '{gameObject.name}': El Collider debe ser un TRIGGER. Configurando automáticamente...");
            _collider.isTrigger = true;
        }

        // Asegurar que está en el layer correcto
        if (gameObject.layer != LayerMask.NameToLayer("ClimbableWall"))
        {
            Debug.LogWarning($" ClimbableZone '{gameObject.name}': Se recomienda usar el layer 'ClimbableWall'");
        }
    }

    /// <summary>
    /// Verifica si un punto (posición) está dentro de la zona escalable
    /// </summary>
    /// <param name="point">Posición a verificar (típicamente transform.position del jugador)</param>
    /// <returns>true si el punto está dentro del collider, false si está fuera</returns>
    public bool IsPointInZone(Vector3 point)
    {
        // Verificar que tenemos un collider válido
        if (_collider == null)
        {
            _collider = GetComponent<Collider>();
        }

        // Si aún no hay collider, devolver false
        if (_collider == null)
        {
            return false;
        }

        // Verificar si el punto está dentro de los límites del collider
        return _collider.bounds.Contains(point);
    }

    // Dibujar el área en el editor
    void OnDrawGizmos()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = gizmoColor;

            if (col is BoxCollider boxCol)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(boxCol.center, boxCol.size);

                // Wireframe para mejor visibilidad
                Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 1f);
                Gizmos.DrawWireCube(boxCol.center, boxCol.size);
            }
            else if (col is SphereCollider sphereCol)
            {
                Gizmos.DrawSphere(transform.position + sphereCol.center, sphereCol.radius);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        // Resaltar cuando está seleccionado
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = Color.green;

            if (col is BoxCollider boxCol)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireCube(boxCol.center, boxCol.size);
            }
        }
    }
}