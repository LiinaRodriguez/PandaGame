using UnityEngine;
using System.Collections;
#if ENABLE_INPUT_SYSTEM 
using UnityEngine.InputSystem;
#endif
using UnityEngine.SceneManagement;

namespace StarterAssets
{
    [RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM 
    [RequireComponent(typeof(PlayerInput))]
#endif
    public class ThirdPersonController : MonoBehaviour
    {
        #region Enums
        public enum MovementState
        {
            Walking,
            Climbing,
            Celebrating,
            Frozen
        }
        #endregion

        #region Player Settings
        [Header("Spirometer")]
        [Tooltip("Referencia al input del espirómetro")]
        public SpirometerInput spirometer;

        [Tooltip("Velocidad máxima con espirómetro (m/s)")]
        public float maxFlowSpeed = 5f;

        [Tooltip("Velocidad mínima con espirómetro (m/s)")]
        public float minFlowSpeed = 0.5f;

        [Tooltip("Tiempo sin flujo antes de caer (segundos)")]
        public float timeoutSinFlujo = 2f;

        [Header("Climbing Timer")]
        [Tooltip("Tiempo máximo de escalada (segundos)")]
        public float maxClimbTime = 10f;

        [Tooltip("Mostrar UI del timer")]
        public bool showClimbTimer = true;

        [Header("Return Camera")]
        public ReturnCameraController returnCamera;
        public bool useReturnCamera = true;

        [Header("Movement")]
        public float MoveSpeed = 2.0f;
        public float SprintSpeed = 5.335f;

        [Range(0.0f, 0.3f)]
        public float RotationSmoothTime = 0.12f;
        public float SpeedChangeRate = 10.0f;

        [Header("Climbing")]
        public float ClimbSpeed = 2.5f;
        public float ClimbDetectionDistance = 1.5f;
        public float ClimbTransitionSpeed = 5f;

        [Header("Jump & Gravity")]
        public float JumpHeight = 1.2f;
        public float Gravity = -15.0f;
        public float JumpTimeout = 0.50f;
        public float FallTimeout = 0.15f;

        [Header("Ground Detection")]
        public bool Grounded = true;
        public float GroundedOffset = -0.14f;
        public float GroundedRadius = 0.28f;
        public LayerMask GroundLayers;

        [Header("Summit & Respawn")]
       
        public float CelebrationDuration = 5.0f;
        public float FinalArrivalDistance = 1.0f;

        [Header("Camera")]
        public GameObject CinemachineCameraTarget;
        public float TopClamp = 70.0f;
        public float BottomClamp = -30.0f;
        public float CameraAngleOverride = 0.0f;
        public bool LockCameraPosition = false;

        [Header("Debug")]
        public bool ShowDebugInfo = true;
        public bool ShowDebugGizmos = true;
        #endregion

        #region Private Variables
        private MovementState _currentState = MovementState.Walking;
        private ClimbableZone _currentClimbZone = null;

        private float _cinemachineTargetYaw;
        private float _cinemachineTargetPitch;

        private float _speed;
        private float _animationBlend;
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;

        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;
        private int _animIDClimbing;
        private int _animIDCelebrate;

#if ENABLE_INPUT_SYSTEM 
        private PlayerInput _playerInput;
#endif
        private Animator _animator;
        private CharacterController _controller;
        private StarterAssetsInputs _input;
        private GameObject _mainCamera;

        private const float _threshold = 0.01f;
        private bool _hasAnimator;

        private Vector3 _climbingNormal;
        private bool _wasClimbing = false;

        private float _climbStartTime = 0f;
        private float _remainingClimbTime = 0f;
        private bool _climbTimerActive = false;
        private float _lastFlowTime = 0f;
        private bool _isFalling = false;
        private bool inputDisabled = false;

        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (_mainCamera == null)
            {
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            }

            
        }

        private void Start()
        {
            _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;

            _hasAnimator = TryGetComponent(out _animator);
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<StarterAssetsInputs>();

#if ENABLE_INPUT_SYSTEM 
            _playerInput = GetComponent<PlayerInput>();
#endif

            if (spirometer == null)
            {
                spirometer = GetComponent<SpirometerInput>();

                if (spirometer != null)
                {
                    Debug.Log("Espirómetro detectado automáticamente");
                }
                else
                {
                    Debug.LogWarning("No se encontró SpirometerInput. Usando teclado/gamepad.");
                }
            }

            AssignAnimationIDs();

            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;

            Debug.Log("ThirdPersonController inicializado");
        }

        private void Update()
        {
            _hasAnimator = TryGetComponent(out _animator);

            // 🚨 Bloquear TODO durante celebración
            if (_currentState == MovementState.Celebrating)
            {
                return;
            }

            if (inputDisabled)
            {
                return;
            }

            switch (_currentState)
            {
                case MovementState.Walking:
                    UpdateWalking();
                    break;

                case MovementState.Climbing:
                    UpdateClimbing();
                    break;

                case MovementState.Frozen:
                    break;
            }
        }

        private void LateUpdate()
        {
            if (_currentState == MovementState.Celebrating)
            {
                // Re-verificar que CharacterController esté desactivado
                if (_controller != null && _controller.enabled)
                {
                    Debug.LogWarning("⚠️ CharacterController fue reactivado externamente - DESACTIVANDO");
                    _controller.enabled = false;
                }
            }
        }

        private void OnAnimatorMove()
        {
            // Si está celebrando, NO aplicar movimiento del animator
            if (_currentState == MovementState.Celebrating)
            {
                return;
            }
        }
        #endregion

        #region State Updates
        private void UpdateWalking()
        {
            GroundedCheck();
            DetectClimbableZones();
            JumpAndGravity();
            Move();
        }

        private void UpdateClimbing()
        {
            if (spirometer != null && spirometer.EstaConectado())
            {
                float flowNormalized = spirometer.ObtenerInputNormalizado();

                if (flowNormalized > 0.05f)
                {
                    _lastFlowTime = Time.time;
                }
                else
                {
                    if (Time.time - _lastFlowTime > timeoutSinFlujo)
                    {
                        Debug.Log("Sin flujo detectado - CAYENDO");
                        IniciarCaida();
                        return;
                    }
                }
            }

            if (_climbTimerActive)
            {
                _remainingClimbTime = maxClimbTime - (Time.time - _climbStartTime);

                if (_remainingClimbTime <= 0f)
                {
                    Debug.Log("TIEMPO AGOTADO - CAYENDO");
                    IniciarCaida();
                    return;
                }
            }

            if (_currentClimbZone == null || !IsInClimbZone())
            {
                ExitClimbingState();
                return;
            }

            ClimbMove();
        }
        #endregion

        #region Animation
        private void AssignAnimationIDs()
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
            _animIDClimbing = Animator.StringToHash("Climbing");
            _animIDCelebrate = Animator.StringToHash("Celebrate");
        }

        private void UpdateAnimator()
        {
            if (!_hasAnimator) return;

            if (_currentState == MovementState.Climbing)
            {
                float climbAnimSpeed = Mathf.Max(_animationBlend, 0.5f);
                _animator.SetFloat(_animIDSpeed, climbAnimSpeed);
                _animator.SetFloat(_animIDMotionSpeed, 1f);
            }
            else
            {
                _animator.SetFloat(_animIDSpeed, _animationBlend);
                _animator.SetFloat(_animIDMotionSpeed, _input.analogMovement ? _input.move.magnitude : 1f);
            }

            _animator.SetBool(_animIDGrounded, Grounded);
            _animator.SetBool(_animIDClimbing, _currentState == MovementState.Climbing);
        }
        #endregion

        #region Ground Detection
        private void GroundedCheck()
        {
            Vector3 spherePosition = new Vector3(
                transform.position.x,
                transform.position.y - GroundedOffset,
                transform.position.z
            );

            Grounded = Physics.CheckSphere(
                spherePosition,
                GroundedRadius,
                GroundLayers,
                QueryTriggerInteraction.Ignore
            );

            if (_hasAnimator)
            {
                _animator.SetBool(_animIDGrounded, Grounded);
            }
        }
        #endregion

        #region Climbing System
        private void DetectClimbableZones()
        {
            if (_currentState == MovementState.Climbing) return;

            Vector3 rayOrigin = transform.position + Vector3.up * 1f;
            Vector3 rayDirection = transform.forward;

            Debug.DrawRay(rayOrigin, rayDirection * ClimbDetectionDistance, Color.cyan, 0.1f);

            if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit, ClimbDetectionDistance))
            {
                if (ShowDebugInfo && Time.frameCount % 30 == 0)
                {
                    Debug.Log($"🔍 Raycast HIT: {hit.collider.name}");
                }

                ClimbableZone climbZone = hit.collider.GetComponent<ClimbableZone>();

                if (climbZone != null)
                {
                    if (ShowDebugInfo && Time.frameCount % 30 == 0)
                    {
                        Debug.Log($"Zona escalable detectada: {climbZone.name}");
                    }

                    bool shouldClimb = false;

                    if (spirometer != null && spirometer.EstaConectado())
                    {
                        float flowNormalized = spirometer.ObtenerInputNormalizado();
                        shouldClimb = flowNormalized > 0.05f;

                        if (ShowDebugInfo && Time.frameCount % 30 == 0)
                        {
                            Debug.Log($"Flujo para escalar: {flowNormalized:F3} | Should climb: {shouldClimb}");
                        }
                    }
                    else
                    {
                        shouldClimb = _input.move.y > 0.1f;
                    }

                    if (shouldClimb)
                    {
                        EnterClimbingState(climbZone, hit.normal);
                    }
                }
                else
                {
                    if (ShowDebugInfo && Time.frameCount % 60 == 0)
                    {
                        Debug.LogWarning($"⚠️ Objeto detectado pero sin ClimbableZone: {hit.collider.name}");
                    }
                }
            }
        }

        private void EnterClimbingState(ClimbableZone zone, Vector3 normal)
        {
            Debug.Log($"🧗‍♂️ ENTRANDO A MODO ESCALADA: {zone.gameObject.name}");

            _currentState = MovementState.Climbing;
            _currentClimbZone = zone;
            _climbingNormal = -normal;
            _verticalVelocity = 0f;
            _wasClimbing = true;

            _climbStartTime = Time.time;
            _remainingClimbTime = maxClimbTime;
            _climbTimerActive = true;
            _lastFlowTime = Time.time;
            _isFalling = false;

            Quaternion targetRotation = Quaternion.LookRotation(_climbingNormal);
            transform.rotation = targetRotation;

            if (_hasAnimator)
            {
                _animator.SetBool(_animIDClimbing, true);
                _animator.SetBool(_animIDGrounded, false);
                _animator.SetFloat(_animIDSpeed, 0.5f);

                Debug.Log($"✅ Animator configurado - Climbing: {_animator.GetBool(_animIDClimbing)}");
            }
            else
            {
                Debug.LogError("❌ No hay Animator disponible!");
            }

            Debug.Log($"✅ Escalada iniciada - Timer: {maxClimbTime}s | Estado: {_currentState}");
        }

        private void ExitClimbingState()
        {
            _currentState = MovementState.Walking;
            _currentClimbZone = null;
            _wasClimbing = false;
            _climbTimerActive = false;
            _isFalling = false;

            if (_hasAnimator)
            {
                _animator.SetBool(_animIDClimbing, false);
            }

            Debug.Log("🚶 Saliendo del modo escalada");
        }

        private bool IsInClimbZone()
        {
            if (_currentClimbZone == null) return false;
            return _currentClimbZone.IsPointInZone(transform.position);
        }

        private void ClimbMove()
        {
            float climbSpeed = _currentClimbZone.climbSpeed;
            float inputVertical;
            _verticalVelocity = 0f;

            if (spirometer != null && spirometer.EstaConectado())
            {
                inputVertical = spirometer.ObtenerInputNormalizado();

                if (ShowDebugInfo && Time.frameCount % 30 == 0)
                {
                    Debug.Log($"🧗 Escalando - Flujo: {inputVertical:F3} | Timer: {_remainingClimbTime:F1}s");
                }
            }
            else
            {
                inputVertical = _input.move.y;
            }

            if (inputVertical < 0.05f)
            {
                inputVertical = 0f;
            }

            Vector3 climbMovement = Vector3.up * inputVertical * climbSpeed * Time.deltaTime;

            _controller.Move(climbMovement);

            _speed = Mathf.Abs(inputVertical) * climbSpeed;
            float targetBlend = Mathf.Max(_speed, 0.5f);
            _animationBlend = Mathf.Lerp(_animationBlend, targetBlend, Time.deltaTime * SpeedChangeRate);

            UpdateAnimator();
        }

        private void IniciarCaida()
        {
            if (_isFalling) return;

            _isFalling = true;
            _climbTimerActive = false;

            Debug.Log("💀 CAÍDA INICIADA - GAME OVER");

            _currentState = MovementState.Frozen;
            _currentClimbZone = null;

            if (_hasAnimator)
            {
                _animator.SetBool(_animIDClimbing, false);
                _animator.SetBool(_animIDFreeFall, true);
            }

            StartCoroutine(CaidaYGameOver());
        }

        private IEnumerator CaidaYGameOver()
        {
            float fallDuration = 5f;
            float elapsedTime = 0f;

            while (elapsedTime < fallDuration)
            {
                _verticalVelocity += Gravity * Time.deltaTime;
                _controller.Move(new Vector3(0, _verticalVelocity, 0) * Time.deltaTime);

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            Debug.Log("GAME OVER - Reiniciando escena...");
            yield return new WaitForSeconds(1f);
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        #endregion

        #region Normal Movement
        private void Move()
        {
            float targetSpeed = 0f;
            bool usingSpirometer = false;

            if (spirometer != null && spirometer.EstaConectado())
            {
                float flowNormalized = spirometer.ObtenerInputNormalizado();

                if (ShowDebugInfo && Time.frameCount % 30 == 0)
                {
                    Debug.Log($"Flujo: {flowNormalized:F3} | Speed: {targetSpeed:F2} m/s");
                }

                if (flowNormalized > 0.05f)
                {
                    targetSpeed = minFlowSpeed + (flowNormalized * (maxFlowSpeed - minFlowSpeed));
                    usingSpirometer = true;
                }
                else
                {
                    targetSpeed = 0f;
                }
            }
            else
            {
                if (_input.move != Vector2.zero)
                {
                    targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;
                }
            }

            float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;
            float speedOffset = 0.1f;
            float inputMagnitude = usingSpirometer ? 1f : (_input.analogMovement ? _input.move.magnitude : 1f);

            if (currentHorizontalSpeed < targetSpeed - speedOffset ||
                currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * SpeedChangeRate);
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
            if (_animationBlend < 0.01f) _animationBlend = 0f;

            Vector3 inputDirection;

            if (usingSpirometer)
            {
                inputDirection = Vector3.forward;
            }
            else
            {
                inputDirection = new Vector3(0f, 0.0f, _input.move.y).normalized;
            }

            if (usingSpirometer)
            {
                _targetRotation = _mainCamera.transform.eulerAngles.y;

                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation,
                    ref _rotationVelocity, RotationSmoothTime);
                transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
            }
            else if (_input.move.y != 0)
            {
                _targetRotation = _mainCamera.transform.eulerAngles.y;

                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation,
                    ref _rotationVelocity, RotationSmoothTime);
                transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
            }

            Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

            _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) +
                             new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

            UpdateAnimator();
        }

        private void JumpAndGravity()
        {
            if (Grounded)
            {
                _fallTimeoutDelta = FallTimeout;

                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, false);
                    _animator.SetBool(_animIDFreeFall, false);
                }

                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2f;
                }

                if (_input.jump && _jumpTimeoutDelta <= 0.0f)
                {
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDJump, true);
                    }
                }

                if (_jumpTimeoutDelta >= 0.0f)
                {
                    _jumpTimeoutDelta -= Time.deltaTime;
                }
            }
            else
            {
                _jumpTimeoutDelta = JumpTimeout;

                if (_fallTimeoutDelta >= 0.0f)
                {
                    _fallTimeoutDelta -= Time.deltaTime;
                }
                else
                {
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDFreeFall, true);
                    }
                }

                _input.jump = false;
            }

            if (_verticalVelocity < _terminalVelocity)
            {
                _verticalVelocity += Gravity * Time.deltaTime;
            }
        }
        #endregion

        #region Summit & Respawn
        public void OnReachedSummit()
        {
            if (_currentState == MovementState.Celebrating)
            {
                Debug.LogWarning("⚠️ OnReachedSummit llamado múltiples veces - IGNORANDO");
                return;
            }

            Debug.Log("🎉 ========== LLEGASTE A LA CIMA ========== ");
            Debug.Log($"Posición inicial: {transform.position}");
            Debug.Log($"Rotación inicial: {transform.rotation.eulerAngles}");

            _currentState = MovementState.Celebrating;
            inputDisabled = true;
            Debug.Log("✅ Estado cambiado a Celebrating");

            _climbTimerActive = false;

            _verticalVelocity = 0f;
            _speed = 0f;
            _animationBlend = 0f;
            _rotationVelocity = 0f;
            Debug.Log("✅ Variables de física congeladas");

#if ENABLE_INPUT_SYSTEM
            if (_playerInput != null)
            {
                _playerInput.enabled = false;
                Debug.Log("✅ PlayerInput desactivado");
            }
#endif

            if (_input != null)
            {
                _input.move = Vector2.zero;
                _input.look = Vector2.zero;
                _input.jump = false;
                _input.sprint = false;
                Debug.Log("✅ StarterAssetsInputs limpiado");
            }

            if (_hasAnimator)
            {
                bool wasRootMotion = _animator.applyRootMotion;
                _animator.applyRootMotion = false;

                _animator.SetBool(_animIDClimbing, false);
                _animator.SetBool(_animIDGrounded, true);
                _animator.SetBool(_animIDJump, false);
                _animator.SetBool(_animIDFreeFall, false);
                _animator.SetFloat(_animIDSpeed, 0f);
                _animator.SetFloat(_animIDMotionSpeed, 0f);

                Debug.Log($"✅ Animaciones limpiadas | Root Motion: {wasRootMotion} → false");
            }

            if (_controller != null)
            {
                _controller.enabled = false;
                Debug.Log("✅ CharacterController DESACTIVADO");
            }

            if (returnCamera != null)
            {
                returnCamera.enabled = false;
                Debug.Log("✅ ReturnCameraController desactivado");
            }

            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                Debug.Log("✅ Rigidbody congelado");
            }

            StartCoroutine(FreezePositionDebug());
            StartCoroutine(SummitCelebrationSequence());

            Debug.Log("========================================");
        }

        private IEnumerator FreezePositionDebug()
        {
            Vector3 frozenPosition = transform.position;
            Quaternion frozenRotation = transform.rotation;

            Debug.Log($"🔒 POSICIÓN CONGELADA EN: {frozenPosition}");
            Debug.Log($"🔒 ROTACIÓN CONGELADA EN: {frozenRotation.eulerAngles}");

            float checkInterval = 0.5f;
            float nextCheck = Time.time + checkInterval;

            while (_currentState == MovementState.Celebrating)
            {
                float distanceMoved = Vector3.Distance(transform.position, frozenPosition);
                float rotationDiff = Quaternion.Angle(transform.rotation, frozenRotation);

                if (distanceMoved > 0.01f || rotationDiff > 0.1f)
                {
                    Debug.LogWarning($"⚠️ MOVIMIENTO DETECTADO!");
                    Debug.LogWarning($"   Distancia: {distanceMoved:F4}m");
                    Debug.LogWarning($"   Rotación: {rotationDiff:F2}°");
                    Debug.LogWarning($"   Pos actual: {transform.position}");
                    Debug.LogWarning($"   Pos objetivo: {frozenPosition}");

                    transform.position = frozenPosition;
                    transform.rotation = frozenRotation;

                    Debug.LogWarning("   ✅ Posición RESTAURADA");
                }

                if (Time.time >= nextCheck)
                {
                    Debug.Log($"🔒 Verificación: Posición {(distanceMoved < 0.01f ? "OK" : "CORREGIDA")}");
                    nextCheck = Time.time + checkInterval;
                }

                yield return null;
            }

            Debug.Log("🔓 FreezePosition terminado");
        }

        private IEnumerator SummitCelebrationSequence()
        {
            Debug.Log("▶️ Iniciando secuencia de celebración...");

            yield return new WaitForSeconds(0.1f);

            if (_hasAnimator)
            {
                _animator.SetTrigger(_animIDCelebrate);
                Debug.Log($"✅ Animación Celebrate activada (trigger: {_animIDCelebrate})");
            }

            float timeLeft = CelebrationDuration;
            while (timeLeft > 0)
            {
                if (timeLeft == Mathf.Floor(timeLeft))
                {
                    Debug.Log($"⏰ Reiniciando en: {timeLeft}s");
                }
                timeLeft -= Time.deltaTime;
                yield return null;
            }

            Debug.Log("🔄 ========== REINICIANDO ESCENA ==========");
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        #endregion

        #region Debug & Gizmos
        private void OnDrawGizmosSelected()
        {
            if (!ShowDebugGizmos) return;

            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

            Gizmos.color = Grounded ? transparentGreen : transparentRed;
            Gizmos.DrawSphere(
                new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z),
                GroundedRadius
            );

            if (_currentState == MovementState.Walking)
            {
                Gizmos.color = Color.cyan;
                Vector3 rayOrigin = transform.position + Vector3.up * 1f;
                Gizmos.DrawRay(rayOrigin, transform.forward * ClimbDetectionDistance);
            }

            if (_currentClimbZone != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, _currentClimbZone.transform.position);
            }
        }

        private void OnGUI()
        {
            if (!ShowDebugInfo) return;

            GUIStyle style = new GUIStyle
            {
                fontSize = 14,
                normal = { textColor = Color.white },
                padding = new RectOffset(10, 10, 5, 5)
            };

            // Posición: esquina superior izquierda
            int boxHeight = 170 + ((_climbTimerActive && showClimbTimer) ? 35 : 0) +
                            (_currentClimbZone != null ? 25 : 0);
            GUI.Box(new Rect(10, 10, 300, boxHeight), "");

            int yPos = 20;

            GUI.Label(new Rect(20, yPos, 280, 25), $"Estado: {_currentState}", style);
            yPos += 25;

            GUI.Label(new Rect(20, yPos, 280, 25), $"Velocidad: {_speed:F2} m/s", style);
            yPos += 25;

            GUI.Label(new Rect(20, yPos, 280, 25), $"En suelo: {(Grounded ? "Sí" : "No")}", style);
            yPos += 25;

            GUI.Label(new Rect(20, yPos, 280, 25),
                $"Escalando: {(_currentState == MovementState.Climbing ? "SÍ" : "NO")}", style);
            yPos += 25;

            // Mostrar input del inspirómetro (solo el valor normalizado, sin duplicar info)
            if (spirometer != null && spirometer.EstaConectado())
            {
                float flow = spirometer.ObtenerInputNormalizado();
                GUI.Label(new Rect(20, yPos, 280, 25), $"Input Inspirómetro: {flow:F2}", style);
                yPos += 25;
            }

            // Timer de escalada
            if (_climbTimerActive && showClimbTimer)
            {
                GUIStyle timerStyle = new GUIStyle(style)
                {
                    fontSize = 18,
                    fontStyle = FontStyle.Bold
                };

                if (_remainingClimbTime > 5f)
                    timerStyle.normal.textColor = Color.green;
                else if (_remainingClimbTime > 3f)
                    timerStyle.normal.textColor = Color.yellow;
                else
                    timerStyle.normal.textColor = Color.red;

                GUI.Label(new Rect(20, yPos, 280, 30),
                    $"⏰ Tiempo: {_remainingClimbTime:F1}s", timerStyle);
                yPos += 30;
            }

            if (_currentClimbZone != null)
            {
                GUI.Label(new Rect(20, yPos, 280, 25), $"Zona: {_currentClimbZone.name}", style);
            }
        }
        #endregion
    }
}