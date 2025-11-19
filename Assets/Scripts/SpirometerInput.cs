using StarterAssets;
using System;
using System.IO.Ports;
using UnityEngine;

public class SpirometerInput : MonoBehaviour
{
    [Header("Configuración Puerto Serial")]
    [Tooltip("Nombre del puerto COM")]
    public string portName = "COM4";

    [Tooltip("Velocidad de comunicación del Arduino")]
    public int baudRate = 115200;

    [Header("Configuración de Input")]
    [Tooltip("Flujo mínimo en L/min para activar movimiento")]
    public float flujoMinimo = 5f;

    [Tooltip("Flujo máximo en L/min para normalizar a 1.0")]
    public float flujoMaximo = 100f;

    [Tooltip("Si está apagado el espirómetro, usar teclado como alternativa")]
    public bool usarTecladoFallback = true;

    [Header("Debug")]
    public bool mostrarDebug = true;

    private SerialPort serial;
    private bool conectado = false;

    private float flujoActual = 0f;
    private float flujoNormalizado = 0f;

    private StarterAssetsInputs starterInput;

    // ===============================
    // INICIO DEL SISTEMA
    // ===============================
    private void Start()
    {
        starterInput = GetComponent<StarterAssetsInputs>();
        if (starterInput == null)
        {
            Debug.LogError("No se encontró StarterAssetsInputs en el GameObject!");
            return;
        }

        ConectarPuerto();
    }

    private void Update()
    {
        if (conectado && serial != null && serial.IsOpen)
        {
            LeerDatos();
            AplicarFlujoComoInput();

            // BLOQUEAR INPUT DE TECLADO/GAMEPAD
            BloquearInputManual();
        }
        else if (usarTecladoFallback)
        {
            // Si no hay espirómetro, permitir input normal (no hacer nada)
        }
    }

    // ===============================
    // BLOQUEAR INPUT MANUAL
    // ===============================
    private void BloquearInputManual()
    {
        // Resetear todos los inputs que no sean del espirómetro
        starterInput.jump = false;
        starterInput.sprint = false;
    }

    // ===============================
    // CONEXIÓN AL PUERTO
    // ===============================
    private void ConectarPuerto()
    {
        try
        {
            Debug.Log($"Intentando conectar a {portName}");
            serial = new SerialPort(portName, baudRate)
            {
                ReadTimeout = 100,
                DtrEnable = true,
                RtsEnable = true,
            };

            serial.Open();
            conectado = true;

            if (mostrarDebug)
                Debug.Log($"✅ Espirómetro conectado en {portName} ({baudRate} baudios)");

            Invoke(nameof(IniciarPrueba), 1f);
        }
        catch (Exception e)
        {
            conectado = false;

            if (mostrarDebug)
            {
                Debug.LogWarning($"⚠️ No se pudo conectar al espirómetro en {portName}");
                Debug.LogWarning("Error: " + e.Message);

                if (usarTecladoFallback)
                    Debug.Log("✅ Fallback: teclado activado (W para avanzar)");
            }
        }
    }

    // ===============================
    // LECTURA Y PARSEO
    // ===============================
    private void LeerDatos()
    {
        if (serial.BytesToRead <= 0)
            return;

        try
        {
            string linea = serial.ReadLine().Trim();
            if (!string.IsNullOrEmpty(linea))
                ParsearLinea(linea);
        }
        catch (TimeoutException)
        {
            // Timeout normal, no hacer nada
        }
        catch (Exception e)
        {
            if (mostrarDebug)
                Debug.LogWarning("❌ Error leyendo datos: " + e.Message);
        }
    }

    private void ParsearLinea(string linea)
    {


        string[] partes = linea.Split(new char[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (string p in partes)
        {
            // Intentar parsear cada parte

            Debug.Log(p);
            string valorLimpio = p.Replace("L/min", "").Trim();
            if (float.TryParse(valorLimpio, out float valor))
            {
                // Validar que el valor esté en un rango razonable para flujo
                if (valor >= 0 && valor <= 90000)
                {
                    flujoActual = valor;

                    if (mostrarDebug && Time.frameCount % 30 == 0)
                        Debug.Log($"Flujo: {flujoActual:F1} L/min");

                    return;
                }
            }
        }
    }

    // ===============================
    // NORMALIZACIÓN Y APLICACIÓN AL INPUT
    // ===============================
    private void AplicarFlujoComoInput()
    {
        // Normalizar flujo a rango 0-1
        if (flujoActual >= flujoMinimo)
            flujoNormalizado = Mathf.Clamp01((flujoActual - flujoMinimo) / (flujoMaximo - flujoMinimo));
        else
            flujoNormalizado = 0f;

        // Aplicar solo movimiento hacia adelante (Y)
        starterInput.move = new Vector2(0f, flujoNormalizado);

        // Sprint cuando el flujo supera el 70% del máximo
        starterInput.sprint = flujoActual > (flujoMaximo * 0.7f);
    }

    // ===============================
    // CONTROL DEL ESPIROMETRO
    // ===============================
    private void IniciarPrueba()
    {
        EnviarComando("i", "🏁 Prueba iniciada");
    }

    private void PausarPrueba()
    {
        EnviarComando("p", "⏸️ Prueba pausada");
    }

    private void Reiniciar()
    {
        EnviarComando("r", "🔄 Espirómetro reiniciado");
        Invoke(nameof(IniciarPrueba), 0.5f);
    }

    private void EnviarComando(string cmd, string mensaje)
    {
        if (!conectado || serial == null || !serial.IsOpen)
            return;

        try
        {
            serial.WriteLine(cmd);
            if (mostrarDebug) Debug.Log(mensaje);
        }
        catch (Exception e)
        {
            Debug.LogWarning("❌ Error enviando comando: " + e.Message);
        }
    }

    // ===============================
    // GETTERS PÚBLICOS
    // ===============================
    public float ObtenerFlujoActual() => flujoActual;
    public float ObtenerInputNormalizado() => flujoNormalizado;
    public bool EstaConectado() => conectado;

    public static string[] ObtenerPuertosDisponibles() => SerialPort.GetPortNames();

    // ===============================
    // LIMPIEZA
    // ===============================
    private void OnDestroy() => CerrarPuerto();
    private void OnApplicationQuit() => CerrarPuerto();

    private void CerrarPuerto()
    {
        if (serial != null && serial.IsOpen)
        {
            try
            {
                PausarPrueba();
                serial.Close();
                if (mostrarDebug) Debug.Log("✅ Puerto serial cerrado correctamente");
            }
            catch (Exception e)
            {
                Debug.LogWarning("❌ Error cerrando puerto: " + e.Message);
            }
        }
    }

    // ===============================
    // GUI DEBUG
    // ===============================
    private void OnGUI()
    {
        if (!mostrarDebug) return;

        GUIStyle style = new GUIStyle
        {
            fontSize = 16,
            normal = { textColor = Color.white },
            padding = new RectOffset(10, 10, 10, 10)
        };

        // Posición: esquina superior derecha
        GUI.Box(new Rect(Screen.width - 320, 10, 310, 120), "");
        GUI.Label(new Rect(Screen.width - 310, 20, 290, 30),
            $"Inspirómetro: {(conectado ? "✅ CONECTADO" : "❌ DESCONECTADO")}", style);
        GUI.Label(new Rect(Screen.width - 310, 50, 290, 30),
            $"Flujo: {flujoActual:F1} L/min", style);
        GUI.Label(new Rect(Screen.width - 310, 80, 290, 30),
            $"Input: {flujoNormalizado:F2} ({flujoNormalizado * 100:F0}%)", style);
    }
}