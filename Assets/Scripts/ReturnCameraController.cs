using UnityEngine;
using System.Collections;

/// <summary>
/// Controla la cámara durante el regreso del personaje
/// Cambia a vista cenital/aérea para mejor visibilidad
/// </summary>
public class ReturnCameraController : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Transform del jugador a seguir")]
    public Transform playerTransform;

    [Tooltip("Cámara principal del juego")]
    public Camera mainCamera;

    [Header("Configuración Vista Normal")]
    [Tooltip("Offset de la cámara normal (tercera persona)")]
    public Vector3 normalOffset = new Vector3(0f, 2f, -5f);

    [Header("Configuración Vista Aérea")]
    [Tooltip("Altura de la cámara sobre el jugador durante el regreso")]
    public float aerialHeight = 15f;

    [Tooltip("Distancia horizontal desde el jugador")]
    public float aerialDistance = 8f;

    [Tooltip("Ángulo de inclinación de la cámara (grados)")]
    [Range(30f, 90f)]
    public float aerialAngle = 60f;

    [Tooltip("Suavidad de la cámara aérea al seguir")]
    public float aerialSmoothSpeed = 5f;

    [Header("Transiciones")]
    [Tooltip("Velocidad de transición entre vistas")]
    public float transitionSpeed = 2f;

    [Tooltip("Usar transición suave (true) o cambio instantáneo (false)")]
    public bool smoothTransition = true;

    [Header("Debug")]
    [Tooltip("Mostrar gizmos de posición de cámaras")]
    public bool showDebugGizmos = true;

    // Estado interno
    private bool _isAerialView = false;
    private Vector3 _targetPosition;
    private Quaternion _targetRotation;
    private Vector3 _normalCameraPosition;
    private Quaternion _normalCameraRotation;

    public GameObject playerCameraRoot;
    public GameObject playerFollowCamera;
    public StarterAssets.ThirdPersonController controller;
    public StarterAssets.StarterAssetsInputs input;

    private void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        controller = FindObjectOfType<StarterAssets.ThirdPersonController>();
        input = FindObjectOfType<StarterAssets.StarterAssetsInputs>();

        playerCameraRoot = GameObject.Find("PlayerCameraRoot");
        playerFollowCamera = GameObject.Find("PlayerFollowCamera");
    }


    private void LateUpdate()
    {
        if (mainCamera == null || playerTransform == null) return;

        if (_isAerialView)
        {
            UpdateAerialCamera();
        }
    }

    /// <summary>
    /// Activar vista aérea para el regreso
    /// </summary>



    public void EnableAerialView()
    {
        if (_isAerialView) return;
        _isAerialView = true;

        // Desactivar sistemas de cámara de StarterAssets
        if (playerFollowCamera != null)
            playerFollowCamera.SetActive(false);

        if (playerCameraRoot != null)
            playerCameraRoot.SetActive(false);

        if (controller != null)
            controller.enabled = false;

        if (input != null)
            input.enabled = false;

        Debug.Log("📸 Aerial View ACTIVADA");

        if (smoothTransition)
            StartCoroutine(TransitionToAerial());
        else
            PositionAerialCamera();
    }



    /// <summary>
    /// Volver a vista normal
    /// </summary>
    public void DisableAerialView()
    {
        if (!_isAerialView) return;
        _isAerialView = false;

        Debug.Log("📸 Vista normal RESTAURADA");

        // Reactivar sistema de cámara normal
        if (playerFollowCamera != null)
            playerFollowCamera.SetActive(true);

        if (playerCameraRoot != null)
            playerCameraRoot.SetActive(true);

        if (controller != null)
            controller.enabled = true;

        if (input != null)
            input.enabled = true;

        if (smoothTransition)
            StartCoroutine(TransitionToNormal());
    }



    private void UpdateAerialCamera()
    {
        // Calcular posición objetivo de la cámara aérea
        Vector3 targetPos = CalculateAerialPosition();
        Quaternion targetRot = CalculateAerialRotation();

        // Mover suavemente la cámara
        mainCamera.transform.position = Vector3.Lerp(
            mainCamera.transform.position,
            targetPos,
            Time.deltaTime * aerialSmoothSpeed
        );

        mainCamera.transform.rotation = Quaternion.Slerp(
            mainCamera.transform.rotation,
            targetRot,
            Time.deltaTime * aerialSmoothSpeed
        );
    }

    private Vector3 CalculateAerialPosition()
    {
        // Posición arriba y atrás del jugador
        Vector3 offset = new Vector3(0f, aerialHeight, -aerialDistance);

        // Aplicar rotación del jugador (opcional, para que siga la dirección)
        offset = playerTransform.rotation * offset;

        return playerTransform.position + offset;
    }

    private Quaternion CalculateAerialRotation()
    {
        Vector3 direction = (playerTransform.position - mainCamera.transform.position);

        // EVITAR VECTOR CERO
        if (direction.sqrMagnitude < 0.0001f)
        {
            // Si está encima del jugador, mirar hacia adelante del jugador
            direction = playerTransform.forward;
        }

        direction.Normalize();

        Quaternion lookRotation = Quaternion.LookRotation(direction);

        return Quaternion.Euler(aerialAngle, lookRotation.eulerAngles.y, 0f);
    }


    private void PositionAerialCamera()
    {
        mainCamera.transform.position = CalculateAerialPosition();
        mainCamera.transform.rotation = CalculateAerialRotation();
    }

    private IEnumerator TransitionToAerial()
    {
        Vector3 startPosition = mainCamera.transform.position;
        Quaternion startRotation = mainCamera.transform.rotation;

        Vector3 endPosition = CalculateAerialPosition();
        Quaternion endRotation = CalculateAerialRotation();

        float elapsed = 0f;
        float duration = 1f / transitionSpeed;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Curva ease-in-out para transición suave
            t = t * t * (3f - 2f * t);

            mainCamera.transform.position = Vector3.Lerp(startPosition, endPosition, t);
            mainCamera.transform.rotation = Quaternion.Slerp(startRotation, endRotation, t);

            yield return null;
        }

        mainCamera.transform.position = endPosition;
        mainCamera.transform.rotation = endRotation;
    }

    private IEnumerator TransitionToNormal()
    {
        // Obtener posición de la cámara normal actual
        Vector3 normalPos = playerTransform.position + playerTransform.TransformDirection(normalOffset);
        Quaternion normalRot = Quaternion.LookRotation(playerTransform.position - normalPos);

        Vector3 startPosition = mainCamera.transform.position;
        Quaternion startRotation = mainCamera.transform.rotation;

        float elapsed = 0f;
        float duration = 1f / transitionSpeed;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            t = t * t * (3f - 2f * t);

            mainCamera.transform.position = Vector3.Lerp(startPosition, normalPos, t);
            mainCamera.transform.rotation = Quaternion.Slerp(startRotation, normalRot, t);

            yield return null;
        }
    }

    // Gizmos para visualización en el editor
    private void OnDrawGizmos()
    {
        if (!showDebugGizmos || playerTransform == null) return;

        // Posición de cámara aérea
        Vector3 aerialPos = playerTransform.position + new Vector3(0f, aerialHeight, -aerialDistance);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(aerialPos, 1f);
        Gizmos.DrawLine(aerialPos, playerTransform.position);

        // Dibujar campo de visión aproximado
        Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
        Gizmos.DrawLine(aerialPos, playerTransform.position + Vector3.left * 5f);
        Gizmos.DrawLine(aerialPos, playerTransform.position + Vector3.right * 5f);
    }

    private void OnDrawGizmosSelected()
    {
        if (playerTransform == null) return;

        // Dibujar zona de visión de la cámara aérea
        Vector3 aerialPos = playerTransform.position + new Vector3(0f, aerialHeight, -aerialDistance);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(aerialPos, 2f);

        // Línea al jugador
        Gizmos.color = Color.green;
        Gizmos.DrawLine(aerialPos, playerTransform.position);
    }
}