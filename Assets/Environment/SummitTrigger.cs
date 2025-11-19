using UnityEngine;
using UnityEngine.Events;
using StarterAssets;

/// <summary>
/// Detecta cuando el jugador llega a la cima de la montaña
/// VERSIÓN CORREGIDA CON DEBUG
/// </summary>
[RequireComponent(typeof(Collider))]
public class SummitTrigger : MonoBehaviour
{
    [Header("Configuración")]
    [Tooltip("Tiempo de delay antes de activar el evento (segundos)")]
    public float activationDelay = 0.5f;

    [Header("Eventos")]
    [Tooltip("Se dispara cuando el jugador llega a la cima")]
    public UnityEvent onSummitReached;

    [Header("Visual")]
    public Color gizmoColor = new Color(1f, 0.84f, 0f, 0.4f);

    [Header("Debug")]
    public bool showDebugLogs = true;

    private bool _summitReached = false;
    private float _timeInZone = 0f;
    private GameObject _playerInZone = null;

    void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (!col.isTrigger)
        {
            Debug.LogWarning($"SummitTrigger '{gameObject.name}': El Collider debe ser un TRIGGER");
            col.isTrigger = true;
        }

        if (!gameObject.CompareTag("Summit"))
        {
            Debug.LogWarning($"SummitTrigger '{gameObject.name}': Debe tener el tag 'Summit'");
        }

        if (showDebugLogs)
        {
            Debug.Log($"🏔️ SummitTrigger '{gameObject.name}' inicializado | Delay: {activationDelay}s");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !_summitReached)
        {
            Debug.Log($"🏔️ ¡Jugador detectado en la zona de la cima!");
            Debug.Log($"   Collider: {other.name} | Tag: {other.tag}");
            _timeInZone = 0f;
            _playerInZone = other.gameObject;

            if (showDebugLogs)
            {
                Debug.Log($"   Timer iniciado - esperando {activationDelay}s");
            }
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player") && !_summitReached && _playerInZone != null)
        {
            _timeInZone += Time.deltaTime;

            // Log cada segundo para debug
            if (showDebugLogs && Mathf.FloorToInt(_timeInZone) != Mathf.FloorToInt(_timeInZone - Time.deltaTime))
            {
                Debug.Log($"⏰ Tiempo en zona: {_timeInZone:F1}s / {activationDelay}s");
            }

            if (_timeInZone >= activationDelay)
            {
                Debug.Log($"✅ ACTIVANDO CIMA - Tiempo alcanzado: {_timeInZone:F2}s");
                ReachSummit(other.gameObject);
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (!_summitReached)
            {
                if (showDebugLogs)
                {
                    Debug.Log($"⚠️ Jugador salió de la zona (tiempo: {_timeInZone:F2}s)");
                }
                _timeInZone = 0f;
                _playerInZone = null;
            }
            else
            {
                Debug.Log("⚠️ Jugador intentó salir del Summit después de alcanzarlo - BLOQUEADO");
            }
        }
    }

    void ReachSummit(GameObject player)
    {
        if (_summitReached)
        {
            Debug.LogWarning("⚠️ ReachSummit ya fue llamado - IGNORANDO");
            return;
        }

        _summitReached = true;
        Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        Debug.Log("🎉 ¡CIMA ALCANZADA! - Iniciando secuencia");
        Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

        // 1. Notificar al controlador del jugador
        var controller = player.GetComponent<ThirdPersonController>();
        if (controller != null)
        {
            Debug.Log("📞 Llamando a ThirdPersonController.OnReachedSummit()...");
            controller.OnReachedSummit();
            Debug.Log("✅ ThirdPersonController.OnReachedSummit() ejecutado");
        }
        else
        {
            Debug.LogError("❌ ERROR: No se encontró ThirdPersonController en el jugador!");
            Debug.LogError($"   GameObject: {player.name}");
           // Debug.LogError($"   Componentes: {string.Join(", ", player.GetComponents<Component>())}");
        }

        // 2. Invocar eventos externos después de un delay
        StartCoroutine(InvokeEventsAfterFreeze());
    }

    private System.Collections.IEnumerator InvokeEventsAfterFreeze()
    {
        yield return new WaitForSeconds(0.2f);

        if (onSummitReached != null && onSummitReached.GetPersistentEventCount() > 0)
        {
            Debug.Log($"📢 Invocando {onSummitReached.GetPersistentEventCount()} evento(s) externo(s)");
            onSummitReached?.Invoke();
        }
        else
        {
            if (showDebugLogs)
            {
                Debug.Log("ℹ️ No hay eventos externos configurados en onSummitReached");
            }
        }
    }

    public void ResetTrigger()
    {
        _summitReached = false;
        _timeInZone = 0f;
        _playerInZone = null;
        Debug.Log("🔄 Summit trigger reiniciado");
    }

    void OnDrawGizmos()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = gizmoColor;

            if (col is SphereCollider sphereCol)
            {
                Gizmos.DrawSphere(transform.position + sphereCol.center, sphereCol.radius);
                Gizmos.color = new Color(1f, 0.84f, 0f, 1f);
                Gizmos.DrawWireSphere(transform.position + sphereCol.center, sphereCol.radius);
            }
            else if (col is BoxCollider boxCol)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(boxCol.center, boxCol.size);
            }
        }

        Gizmos.color = Color.yellow;
        Vector3 flagPos = transform.position + Vector3.up * 2f;
        Gizmos.DrawLine(transform.position, flagPos);
        Gizmos.DrawWireSphere(flagPos, 0.3f);
    }

    void OnDrawGizmosSelected()
    {
        Collider col = GetComponent<Collider>();
        if (col is SphereCollider sphereCol)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position + sphereCol.center, sphereCol.radius);
        }
    }

    
}