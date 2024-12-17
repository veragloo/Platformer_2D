using System;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

namespace TarodevController
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class PlayerController : MonoBehaviour, IPlayerController
    {
        [SerializeField] private InputActionAsset inputActions;
        private InputAction moveAction;
        private InputAction jumpAction;
        private InputAction dashAction;
        private InputAction grabAction;
        
        [SerializeField] private ScriptableStats _stats;
        private Rigidbody2D _rb;
        private Animator _anim;
        private CapsuleCollider2D _col;
        private FrameInput _frameInput;
        private Vector2 _frameVelocity;
        private bool _cachedQueryStartInColliders;

        [SerializeField] private Transform wallCheck;
        [SerializeField] private float wallCheckDistance;
        [SerializeField] private LayerMask WhatIsGround;
        [SerializeField] private LayerMask PushableLayer;
        private bool isWallDetected;
        private bool isWallDetectedLeft;
        private bool isWallSliding;
        
        private bool isObjectDetected;
        private bool isObjectDetectedLeft;
        
        [SerializeField] private GameObject leftWallDetector; 
        [SerializeField] private GameObject rightWallDetector; 
        [SerializeField] private Vector2 offset1Right; 
        [SerializeField] private Vector2 offset2Right; 
        [SerializeField] private Vector2 offset1Left;  
        [SerializeField] private Vector2 offset2Left;  
        private Vector2 climbBegunPosition;
        private Vector2 climbOverPosition;
        private bool canGrabLedge = true;
        public bool ledgeDetectedLeft;
        public bool ledgeDetected;

        
        #region Interface

        public Vector2 FrameInput => _frameInput.Move;
        public Vector2 Velocity => _rb.linearVelocity;
        public event Action<bool, float> GroundedChanged;
        public event Action Jumped;
        public event Action WallJumped;
        public bool IsGrounded => _grounded;
        public bool IsDashing => _isDashing;
        public bool IsGrabbingWall => _isGrabbingWall;
        public bool IsWallSliding => isWallSliding;
        public bool canClimb { get; private set; }
        public float CurrentClimbSpeed => _currentClimbSpeed;

        #endregion

        
        private float _time;

        private SpriteRenderer _spriteRenderer;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _col = GetComponent<CapsuleCollider2D>();
            _spriteRenderer = GetComponent<SpriteRenderer>(); 
            _anim = GetComponent<Animator>();

            _cachedQueryStartInColliders = Physics2D.queriesStartInColliders;

            var playerControls = inputActions.FindActionMap("Player");
            moveAction = playerControls.FindAction("Move");
            jumpAction = playerControls.FindAction("Jump");
            dashAction = playerControls.FindAction("Dash");
            grabAction = playerControls.FindAction("Grab");

            moveAction.Enable();
            jumpAction.Enable();
            dashAction.Enable();
            grabAction.Enable();
        }

        private void OnEnable()
        {
            jumpAction.Enable();
            dashAction.Enable();
            grabAction.Enable();
        }

        private void OnDisable()
        {
            jumpAction.Disable();
            dashAction.Disable();
            grabAction.Disable();
        }

        private void OnDestroy()
        {
            moveAction.Disable();
            jumpAction.Disable();
            dashAction.Disable();
            grabAction.Disable();
        }

                private void Update()
                {
                    _time += Time.deltaTime;
                    GatherInput();

                    // Don't touch this unless you hate yourself.
                    HandleWallGrab();
                }

        private void GatherInput()
        {
            Vector2 move = moveAction.ReadValue<Vector2>();
            _frameInput = new FrameInput
            {
                JumpDown = jumpAction.triggered, 
                JumpHeld = jumpAction.ReadValue<float>() > 0, 
                Move = move,
                GrabDown = grabAction.triggered,
                GrabHeld = grabAction.ReadValue<float>() > 0
            };

            // Why here?
            bool dashPressed = dashAction.triggered;
            if (dashPressed && _canDash && !_anim.GetCurrentAnimatorStateInfo(0).IsName("LedgeClimb"))
            {
                if (_isGrabbingWall)
                {
                    _isGrabbingWall = false;
                    _rb.gravityScale = 1f;
                }
                StartDash();
            }

            if (jumpAction.triggered) 
            {
                _jumpToConsume = true;
                _timeJumpWasPressed = _time;
                _timeWallJumpWasPressed = _time;
            }

            if (_stats.SnapInput)
            {
                _frameInput.Move.x = Mathf.Abs(_frameInput.Move.x) < _stats.HorizontalDeadZoneThreshold ? 0 : Mathf.Sign(_frameInput.Move.x);
                _frameInput.Move.y = Mathf.Abs(_frameInput.Move.y) < _stats.VerticalDeadZoneThreshold ? 0 : Mathf.Sign(_frameInput.Move.y);
            }

            if (_frameInput.JumpDown)
            {
                _jumpToConsume = true;
                _timeJumpWasPressed = _time;
            }

            if (!_frameInput.JumpHeld && _rb.linearVelocity.y > 0)
            {
                _endedJumpEarly = true;
            }
        }

        private void FixedUpdate()
        {
            // Fix this later (or never). REF z8#2/Origin#1
            if (canClimb)
            {
                _rb.linearVelocity = Vector2.zero; 
                transform.position = climbBegunPosition; 
                return; 
            }

            if (!_isGrabbingWall && (ledgeDetected || ledgeDetectedLeft))
            {
                ResetLedgeDetection();
            }

            if (_isGrabbingWall && _rb.linearVelocity.y > 0)
            {
               CheckForLedge();
            }
            
            CheckCollisions();
            HandleJump();
            HandleDirection();
            HandleGravity();
            HandleDash();
            HandleWallMovement();
            HandleWallSliding();
            HandlePushableObject();
            
            ApplyMovement();
        }

        #region Collisions
        
        private float _frameLeftGrounded = float.MinValue;
        public PhysicsMaterial2D zeroFrictionMaterial;
        public PhysicsMaterial2D FrictionMaterial;
        private bool _forceZeroFriction = false;
        private bool _grounded;

        private void CheckCollisions()
        {
            Physics2D.queriesStartInColliders = false;

            // Ground and Ceiling
            bool groundHit = Physics2D.CapsuleCast(_col.bounds.center, _col.size, _col.direction, 0, Vector2.down, _stats.GrounderDistance, ~_stats.PlayerLayer);
            bool ceilingHit = Physics2D.CapsuleCast(_col.bounds.center, _col.size, _col.direction, 0, Vector2.up, _stats.GrounderDistance, ~_stats.PlayerLayer);

            // Wall detection
            RaycastHit2D hitLeft = Physics2D.Raycast(new Vector2(_col.bounds.min.x, _col.bounds.center.y), Vector2.left, _stats.GrounderDistance, ~_stats.PlayerLayer);
            RaycastHit2D hitRight = Physics2D.Raycast(new Vector2(_col.bounds.max.x, _col.bounds.center.y), Vector2.right, _stats.GrounderDistance, ~_stats.PlayerLayer);

            if ((hitLeft.collider != null || hitRight.collider != null))
            {
                _canWallJump = true; 
                _wallNormal = hitLeft.collider != null ? hitLeft.normal : hitRight.normal;
            }
            else
            {
                _canWallJump = false; 
            }

            // Hit a Ceiling
            if (ceilingHit) _frameVelocity.y = Mathf.Min(0, _frameVelocity.y);

            // Landed on the Ground
            if (!_grounded && groundHit)
            {
                _grounded = true;
                _coyoteUsable = true;
                _bufferedJumpUsable = true;
                _endedJumpEarly = false;
                GroundedChanged?.Invoke(true, Mathf.Abs(_frameVelocity.y));
            }
            // Left the Ground
            else if (_grounded && !groundHit)
            {
                _grounded = false;
                _frameLeftGrounded = _time;
                GroundedChanged?.Invoke(false, 0);
            }

            Physics2D.queriesStartInColliders = _cachedQueryStartInColliders;
        }

        private void HandleWallSliding()
        {
            // Wall detection (Slide)
            isWallDetected = Physics2D.Raycast(wallCheck.position, Vector2.right, wallCheckDistance, WhatIsGround);
            isWallDetectedLeft = Physics2D.Raycast(wallCheck.position, Vector2.left, wallCheckDistance, WhatIsGround);

            // Nouvelle condition d'input vers le mur
            bool isMovingTowardsWall = false;

            // Lire l'input horizontal avec le Input System
            float horizontalInput = moveAction.ReadValue<Vector2>().x;  

            // Vérifie si l'input est dirigé vers le mur détecté
            if (isWallDetected && horizontalInput > 0)
            {
                isMovingTowardsWall = true;
                
            }
            else if (isWallDetectedLeft && horizontalInput < 0)
            {
                isMovingTowardsWall = true;
                
            }

            // Appliquer le matériau en fonction de la direction, sauf si zeroFriction est forcé
            if (_col != null)
            {
                if (_forceZeroFriction)
                {
                    _col.sharedMaterial = zeroFrictionMaterial;
                }
                else
                {
                    _col.sharedMaterial = isMovingTowardsWall ? FrictionMaterial : zeroFrictionMaterial;
                }
            }

            // Determine if sliding is active
            isWallSliding = !_isGrabbingWall && !_grounded && _rb.linearVelocity.y < 0 && isMovingTowardsWall;

            // Set animation state
            _anim.SetBool("isWallSliding", isWallSliding);
        }

        private void HandlePushableObject()
        {
            // Object/Wall detection (Slide)
            isObjectDetected = Physics2D.Raycast(wallCheck.position, Vector2.right, wallCheckDistance, WhatIsGround | PushableLayer);
            isObjectDetectedLeft = Physics2D.Raycast(wallCheck.position, Vector2.left, wallCheckDistance, WhatIsGround | PushableLayer);

            // condition d'input vers le mur
            bool isMovingTowardsPushable = false;

            // Lire l'input horizontal avec le Input System
            float horizontalInput = moveAction.ReadValue<Vector2>().x;  

            // Vérifie si l'input est dirigé vers le mur détecté
            if (isObjectDetected && horizontalInput > 0)
            {
                isMovingTowardsPushable = true;
            }
            else if (isObjectDetectedLeft && horizontalInput < 0)
            {
                isMovingTowardsPushable = true;
            }

            // Determine if Pushing is active
            bool isPushingWall = isMovingTowardsPushable && _grounded && !isWallSliding && !_isGrabbingWall;

            // Set animation state
            _anim.SetBool("isPushingWall", isPushingWall);
        }

        
        #endregion

       #region Jumping

        private bool _jumpToConsume;
        private bool _bufferedJumpUsable;
        private bool _endedJumpEarly;
        private bool _coyoteUsable;
        private float _timeJumpWasPressed;
        private bool _canWallJump;
        
        private bool HasBufferedJump => _bufferedJumpUsable && _time < _timeJumpWasPressed + _stats.JumpBuffer;
        private bool CanUseCoyote => _coyoteUsable && !_grounded && _time < _frameLeftGrounded + _stats.CoyoteTime;
        
        private float _timeWallJumpWasPressed; 
        private bool HasBufferedWallJump => _canWallJump && _time < _timeWallJumpWasPressed + _stats.JumpBuffer;

        

        private void HandleJump()
        {
            // Si aucun saut n'est à consommer ou de saut bufferé disponible, sortir
            if (!_jumpToConsume && !HasBufferedJump && !HasBufferedWallJump) 
                return;


            // Prio de saut
            if (_isGrabbingWall && _grounded)
            {
                HandleWallJump();
            }
            
            // Saut depuis le sol ou avec le Coyote Time
            else if (_grounded || CanUseCoyote)
            {
                if (_isDashing)
                {
                    _isDashing = false; 
                    _frameVelocity.y = _stats.JumpPower; 
                    _frameVelocity.x += _dashDirection.x * _stats.DashHorizontalBoost; 
                }
                else
                {
                    ExecuteJump();
                    StartCoroutine(ApplyZeroFriction());
                }
            }
            
            // Saut depuis un mur
            else if (_canWallJump || HasBufferedWallJump)
            {
                HandleWallJump();
            }

            _jumpToConsume = false;
        }

        private void ExecuteJump()
        {
            if (_isGrabbingWall)
            {
                return;
            }
            else
            {
                _endedJumpEarly = false;
                _timeJumpWasPressed = 0;
                _bufferedJumpUsable = false;
                _coyoteUsable = false;
                _timeWallJumpWasPressed = 0;
                _frameVelocity.y = _stats.JumpPower;
                Jumped?.Invoke();
            }
        }

        private void HandleWallJump()
        {
            _endedJumpEarly = false;
            _timeWallJumpWasPressed = 0; 
            _timeJumpWasPressed = 0;
            _bufferedJumpUsable = false;
            _coyoteUsable = false;

            if (_isGrabbingWall)
            {
                // Forcer zéro friction pendant le Wall Jump
                StartCoroutine(ApplyZeroFriction());
                _frameVelocity = new Vector2(0, _stats.JumpPower);
                _isGrabbingWall = false; 
                _rb.gravityScale = 1f;  
                _canGrab = false;
                _grabCooldownRemaining = _grabCooldownTime;
            }
            else
            {
                _frameVelocity = new Vector2(_wallNormal.x * _stats.WallJumpPushForce, _stats.JumpPower);
            }

            // Désactiver temporairement Wall Jump
            _canWallJump = false;

            Jumped?.Invoke();
            WallJumped?.Invoke();
        }

        // Coroutine pour appliquer le matériau de friction nulle
        private IEnumerator ApplyZeroFriction()
        {
            if (_col != null && zeroFrictionMaterial != null)
            {
                // Activer le flag de priorité
                _forceZeroFriction = true;

                // Appliquer le matériau avec zéro friction
                _col.sharedMaterial = zeroFrictionMaterial;

                // Attendre pendant 0.2 secondes
                yield return new WaitForSeconds(0.2f);

                // Désactiver le flag de priorité
                _forceZeroFriction = false;

                // Restaurer le matériau en fonction de la logique du Wall Sliding
                _col.sharedMaterial = isWallDetected || isWallDetectedLeft ? FrictionMaterial : zeroFrictionMaterial;
            }
        }
        
        #endregion

        #region Dash

        private bool _canDash = true;
        private bool _isDashing = false;
        private float _dashTimeRemaining;
        private Vector2 _dashDirection;
        private bool _canGrab = true; 
        private float _grabCooldownTime = 0.2f; 
        private float _grabCooldownRemaining = 0f;
        private float dashProgress;
        [SerializeField] private AnimationCurve dashDecelerationCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

        
        private void StartDash()
        {
            if (_isDashing || !_canDash) return;
        
            // Désactivation forcée du grab
            if (_isGrabbingWall)
            {
                _isGrabbingWall = false; 
                _frameVelocity = Vector2.zero; 
                _rb.gravityScale = 1f; 
            }
        
            _canGrab = false; 
            _grabCooldownRemaining = _grabCooldownTime; 
        
            _isDashing = true;
            _canDash = false;
            _dashTimeRemaining = _stats.DashDuration;
            dashProgress = 0f;
        
            // Calculer la direction du dash
            if (_frameInput.Move.x != 0)
            {
                _dashDirection = _frameInput.Move.normalized;
            }
            else if (_frameInput.Move.y != 0)
            {
                _dashDirection = new Vector2(0, Mathf.Sign(_frameInput.Move.y));
            }
            else
            {
                _dashDirection = _spriteRenderer.flipX ? Vector2.left : Vector2.right;
            }
        }

        
        private void HandleDash()
        {
            if (_isDashing)
            {
                StartCoroutine(ApplyZeroFriction());
                // Mise à jour du temps et du progrès du dash
                dashProgress += Time.fixedDeltaTime / _stats.DashDuration;
                _dashTimeRemaining -= Time.fixedDeltaTime;
        
                // Application de la décélération progressive via une courbe
                float dashSpeedModifier = dashDecelerationCurve.Evaluate(dashProgress);
                _frameVelocity = _dashDirection * _stats.DashSpeed * dashSpeedModifier;
        
                _rb.gravityScale = 0.3f; 
        
                // Si le temps de dash est écoulé, on arrête le dash
                if (_dashTimeRemaining <= 0)
                {
                    _isDashing = false;
                    _frameVelocity = Vector2.zero; 
                    _rb.gravityScale = 1f; 
                    _grabCooldownRemaining = _grabCooldownTime; 
                }
            }
        
            // Recharge du dash au contact du sol
            if (_grounded && !_isDashing)
            {
                _canDash = true;
            }
        
            // Si le délai du grab est écoulé, on réactive le grab
            if (_grabCooldownRemaining > 0)
            {
                _grabCooldownRemaining -= Time.fixedDeltaTime;
            }
            else
            {
                _canGrab = true;
            }
        }
        
        #endregion


        #region Grab
        
        private bool _isTouchingWall;
        private bool _isGrabbingWall;
        private Vector2 _wallNormal;
        private float _currentClimbSpeed; 
        private bool isWallAbove;
        
        private void HandleWallGrab()
        {
            if (!_canGrab) return; 
        
            // On détermine la direction du mur
            RaycastHit2D hitLeft = Physics2D.Raycast(new Vector2(_col.bounds.min.x, _col.bounds.center.y), Vector2.left, _stats.GrounderDistance, ~_stats.PlayerLayer);
            RaycastHit2D hitRight = Physics2D.Raycast(new Vector2(_col.bounds.max.x, _col.bounds.center.y), Vector2.right, _stats.GrounderDistance, ~_stats.PlayerLayer);
        
            bool isTouchingLeftWall = hitLeft.collider != null;
            bool isTouchingRightWall = hitRight.collider != null;

            bool isTouchingWallAboveLeft = leftWallDetector.GetComponent<Collider2D>().IsTouchingLayers(LayerMask.GetMask("Ground"));
            bool isTouchingWallAboveRight = rightWallDetector.GetComponent<Collider2D>().IsTouchingLayers(LayerMask.GetMask("Ground"));
            isWallAbove = isTouchingWallAboveLeft || isTouchingWallAboveRight;
        
            // Si le joueur appuie sur "Grab" et est proche d'un mur
            if (_frameInput.GrabHeld && isWallAbove)
            {
                // Vérifie si le joueur regarde dans la bonne direction
                bool canGrab = false;
        
                // Si le joueur regarde à droite et est proche d'un mur à droite
                if (!_spriteRenderer.flipX && isTouchingRightWall)
                {
                    canGrab = true;
                }
                // Si le joueur regarde à gauche et est proche d'un mur à gauche
                else if (_spriteRenderer.flipX && isTouchingLeftWall)
                {
                    canGrab = true;
                }
        
                if (canGrab && !_isGrabbingWall)
                {
                    _isGrabbingWall = true;  
                    _frameVelocity = Vector2.zero; 
                    _currentClimbSpeed = 0;  
                }
            }
        
            if (_isGrabbingWall)
            {
                // Si le joueur n'est plus en contact avec le mur, on le fait décrocher
                if (!(isTouchingLeftWall || isTouchingRightWall))
                {
                    _isGrabbingWall = false;  
                    _frameVelocity.y = 0;  
                }
                else
                {
                    // Si le joueur maintient la touche "Grab", il peut se déplacer verticalement
                    float targetSpeed = _frameInput.Move.y * _stats.ClimbSpeed; 
        
                    if (targetSpeed != 0)
                    {
                        // Accélérer ou décélérer en fonction de la direction de l'entrée verticale
                        _currentClimbSpeed = Mathf.MoveTowards(_currentClimbSpeed, targetSpeed, _stats.ClimbAcceleration * Time.fixedDeltaTime);  
                    }
                    else
                    {
                        // Si aucune entrée, on applique la décélération
                        _currentClimbSpeed = Mathf.MoveTowards(_currentClimbSpeed, 0, _stats.ClimbDeceleration * Time.fixedDeltaTime);  
                    }
        
                    // Applique la vitesse calculée au mouvement vertical
                    _frameVelocity.y = _currentClimbSpeed;
                }
            }
        
            // Si le joueur relâche la touche Grab, on décroche
            if (_isGrabbingWall && !_frameInput.GrabHeld)
            {
                // Vérifie si l'animation de grimpe (ledge climb) est en cours
                if (!_anim.GetCurrentAnimatorStateInfo(0).IsName("LedgeClimb"))
                {
                    // Si l'animation de grimpe n'est pas en cours, alors on peut relâcher le grab
                    _isGrabbingWall = false;  
                    _frameVelocity.y = 0;  
                }
            }

        }
        
        private void HandleWallMovement()
        {
            if (_isGrabbingWall)
            {
                // Empêche tout mouvement horizontal pendant le grab
                _frameVelocity.x = 0;
        
                _frameVelocity.y = _currentClimbSpeed;  
            }
        }
        
        #endregion

        #region Ledge

        private void CheckForLedge()
        {
            // Vérification de la détection de ledge à droite
            if (ledgeDetected && canGrabLedge)
            {
                canGrabLedge = false;

                Vector2 ledgePosition = GetComponentInChildren<LedgeDetection>().transform.position;

                climbBegunPosition = ledgePosition + offset1Right;
                climbOverPosition = ledgePosition + offset2Right;

                canClimb = true;
            }
            // Vérification de la détection de ledge à gauche
            else if (ledgeDetectedLeft && canGrabLedge)
            {
                canGrabLedge = false;

                Vector2 ledgePosition = GetComponentInChildren<LedgeDetection>().transform.position;

                climbBegunPosition = ledgePosition + offset1Left;
                climbOverPosition = ledgePosition + offset2Left;

                canClimb = true;
            }

            // on place le joueur à la position de début de l'escalade
            if (canClimb)
                transform.position = climbBegunPosition;
        }

        private void ResetLedgeDetection()
        {
            ledgeDetected = false;
            ledgeDetectedLeft = false;
            canClimb = false;
        }

        private void LedgeClimbOver()
        {
            canClimb = false;
            transform.position = climbOverPosition;
            ResetLedgeDetection();
            Invoke("AllowLedgeGrab", .1f);
        }

        private void AllowLedgeGrab() => canGrabLedge = true;
        
        #endregion

        #region Horizontal

        private void HandleDirection()
        {
            if (_frameInput.Move.x == 0)
            {
                var deceleration = _grounded ? _stats.GroundDeceleration : _stats.AirDeceleration;
                _frameVelocity.x = Mathf.MoveTowards(_frameVelocity.x, 0, deceleration * Time.fixedDeltaTime);
            }
            else
            {
                // Mise à jour de la direction du personnage
                facingDirection = Mathf.Sign(_frameInput.Move.x);

                _frameVelocity.x = Mathf.MoveTowards(_frameVelocity.x, _frameInput.Move.x * _stats.MaxSpeed, _stats.Acceleration * Time.fixedDeltaTime);
            }
        }


        #endregion

        #region Gravity

        private void HandleGravity()
        {
            if (canClimb)
            {
                _rb.gravityScale = 0; 
                _frameVelocity = Vector2.zero; 
                return; 
            }
            
            if (_isGrabbingWall)
            {
                _rb.gravityScale = 0f; 
            }
            else
            {
                _rb.gravityScale = 1f;
            }
        
            // Autres logiques de gravité comme la chute
            if (_grounded && _frameVelocity.y <= 0f)
            {
                _frameVelocity.y = _stats.GroundingForce;
            }
            else
            {
                float fallSpeed = _stats.MaxFallSpeed;
                if (_frameInput.Move.y < 0)  
                {
                    fallSpeed *= _stats.FastFallMultiplier;
                }
        
                var inAirGravity = _stats.FallAcceleration;
                if (_endedJumpEarly && _frameVelocity.y > 0) inAirGravity *= _stats.JumpEndEarlyGravityModifier;
                _frameVelocity.y = Mathf.MoveTowards(_frameVelocity.y, -fallSpeed, inAirGravity * Time.fixedDeltaTime);
            }
        }


        #endregion

        private void ApplyMovement() => _rb.linearVelocity = _frameVelocity;
        
        // Cleaning ? pls
        private float facingDirection = 1f;
        private void OnDrawGizmos()
        {
            Gizmos.DrawLine(wallCheck.position, new Vector3(wallCheck.position.x + wallCheckDistance * facingDirection,
                                                           wallCheck.position.y,
                                                           wallCheck.position.z));
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_stats == null) Debug.LogWarning("Please assign a ScriptableStats asset to the Player Controller's Stats slot", this);
        }
#endif
    }

    public struct FrameInput
    {
        public bool JumpDown;
        public bool JumpHeld;
        public Vector2 Move;
        public bool DashDown;
        public bool GrabDown;
        public bool GrabHeld;
    }

    public interface IPlayerController
    {
        public event Action<bool, float> GroundedChanged;

        public event Action Jumped;
        public event Action WallJumped;
        public Vector2 FrameInput { get; }
        public Vector2 Velocity { get; }
        bool IsDashing { get; }
        public bool IsGrabbingWall { get; }
        public bool IsWallSliding { get; }
        bool canClimb { get; }
        float CurrentClimbSpeed { get; }
        bool IsGrounded { get; }
    }
}