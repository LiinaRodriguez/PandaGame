using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraIntroManager : MonoBehaviour
{
    [Header("Referencias de Cámara")]
    public Transform cameraTransform;
    public Transform cameraStartPosition; // Posición inicial (lejos)
    public Transform cameraGamePosition;  // Posición de juego (detrás del panda)

    [Header("Configuración de Animación")]
    public float introDuration = 3f;
    public AnimationCurve easeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("UI References")]
    public GameObject menuUI;           // Panel con "Press to Start"
    public GameObject gameUI;           // UI del juego (score, etc)

    [Header("Player Reference")]
    public GameObject playerController; // Script de control del panda

    private bool introComplete = false;

    void Start()
    {
        // Configuración inicial
        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;

        // Posicionar cámara al inicio
        cameraTransform.position = cameraStartPosition.position;
        cameraTransform.rotation = cameraStartPosition.rotation;

        // Desactivar control del jugador
        if (playerController != null)
            playerController.SetActive(false);

        // Mostrar menú, ocultar UI de juego
        menuUI.SetActive(true);
        gameUI.SetActive(false);

        // Iniciar animación de cámara
        StartCoroutine(PlayCameraIntro());
    }

    IEnumerator PlayCameraIntro()
    {
        float elapsedTime = 0f;

        Vector3 startPos = cameraStartPosition.position;
        Quaternion startRot = cameraStartPosition.rotation;

        Vector3 endPos = cameraGamePosition.position;
        Quaternion endRot = cameraGamePosition.rotation;

        // Animar cámara
        while (elapsedTime < introDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = easeCurve.Evaluate(elapsedTime / introDuration);

            cameraTransform.position = Vector3.Lerp(startPos, endPos, t);
            cameraTransform.rotation = Quaternion.Lerp(startRot, endRot, t);

            yield return null;
        }

        // Asegurar posición final
        cameraTransform.position = endPos;
        cameraTransform.rotation = endRot;

        introComplete = true;
    }

    void Update()
    {
        // Esperar a que termine la intro y el jugador presione
        if (introComplete && Input.anyKeyDown)
        {
            StartGame();
        }
    }

    void StartGame()
    {
        // Ocultar menú, mostrar UI de juego
        menuUI.SetActive(false);
        gameUI.SetActive(true);

        // Activar control del jugador
        if (playerController != null)
            playerController.SetActive(true);

        // Desactivar este script
        this.enabled = false;
    }
}

