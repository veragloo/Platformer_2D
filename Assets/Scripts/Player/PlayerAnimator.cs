using UnityEngine;

namespace TarodevController
{
    public class PlayerAnimator : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Animator _anim;
        [SerializeField] private SpriteRenderer _sprite;

        [Header("Settings")]
        [SerializeField, Range(1f, 3f)] private float _maxIdleSpeed = 2;

        [Header("Particles")]
        [SerializeField] private ParticleSystem _jumpParticles;
        [SerializeField] private ParticleSystem _wallJumpParticles;
        [SerializeField] private ParticleSystem _landParticles;
        [SerializeField] private ParticleSystem _wallSlideParticles;
        [SerializeField] private ParticleSystem _pushParticles;
        [SerializeField] private ParticleSystem _dashParticles;

        private AudioSource _source;
        private IPlayerController _player;
        private ParticleSystem.MinMaxGradient _currentGradient;
        private Rigidbody2D _rigidbody;
        private bool previousValue;

        private void Awake()
        {
            _player = GetComponentInParent<IPlayerController>();
            _rigidbody = GetComponent<Rigidbody2D>();

            previousValue = _anim.GetBool("isLookingBehind");
        }

        private void OnEnable()
        {
            _player.Jumped += OnJumped;
            _player.WallJumped += OnWallJumped;
            _player.GroundedChanged += OnGroundedChanged;
        }

        private void OnDisable()
        {
            _player.Jumped -= OnJumped;
            _player.WallJumped -= OnWallJumped;
            _player.GroundedChanged -= OnGroundedChanged;
        }

        private void Update()
        {
            if (_player == null) return;

            float verticalSpeed = _rigidbody.linearVelocity.y; 
            _anim.SetFloat("VerticalSpeed", verticalSpeed);

            _anim.SetBool("Grounded", _player.IsGrounded);

            _anim.SetBool("canClimb", _player.canClimb);
            
            // DetectGroundColor(); A VOIR
            
            HandleSpriteFlip();
            HandleIdleSpeed();
            HandleDashAnimation();
            HandleGrabClimbing();
            OnWallSlide();

            
        }

        public bool isFlipped = false; 
        private void HandleSpriteFlip()
        {
            if (_player.FrameInput.x != 0 && !_player.IsGrabbingWall && !_player.canClimb)
            {
                _sprite.flipX = _player.FrameInput.x < 0;

                isFlipped = _sprite.flipX;
            }
        }

        private void HandleIdleSpeed()
        {
            var inputStrength = Mathf.Abs(_player.FrameInput.x);
            _anim.SetFloat(IdleSpeedKey, Mathf.Lerp(1, _maxIdleSpeed, inputStrength));
            
        }

        private void HandleDashAnimation()
        {
            _anim.SetBool("isDashing", _player.IsDashing);
        }

        private void OnDash() // Animation event
        {
            if (_player.IsDashing)
            {
                _dashParticles.Play();
            }
            else
            {
                _dashParticles.Stop();
            }
        }

        private float climbSpeedThreshold = 0.2f;
        private void HandleGrabClimbing()
        {
            if (_anim.GetCurrentAnimatorStateInfo(0).IsName("LedgeClimb"))
            {
                _anim.SetBool("isGrabbingWall", false);
                return;
            }

            if (_player.IsGrabbingWall)
            {
                // Détermine si le joueur est en pause verticale (aucune vitesse verticale)
                bool isVerticalIdle = Mathf.Abs(_rigidbody.linearVelocity.y) < 0.1f; // Le joueur est à l'arrêt verticalement
                bool isLookingBehind = isVerticalIdle && DetermineLookingBehind(); // Se déplace uniquement si aucune vitesse verticale
        
                _anim.SetBool("isGrabbingWall", true);
                _anim.SetBool("isLookingBehind", isLookingBehind);
        
                if (isVerticalIdle && !isLookingBehind)
                {
                    _anim.SetBool("isWallGrabIdle", true);
                }
                else
                {
                    _anim.SetBool("isWallGrabIdle", false);
                }
        
                // Gérer la logique d'ascension du mur
                float climbSpeed = _player.CurrentClimbSpeed;
                _anim.SetFloat("climbSpeed", climbSpeed);
        
                // Vérifie la direction du mouvement vertical
                bool isMovingUp = _rigidbody.linearVelocity.y > 0; // Se déplace vers le haut
                bool isMovingDown = _rigidbody.linearVelocity.y < 0; // Se déplace vers le bas
        
                // Si le joueur se déplace vers le haut, il grimpe
                if (isMovingUp && Mathf.Abs(climbSpeed) > climbSpeedThreshold)
                {
                    _anim.SetBool("isWallClimbing", true);
                }
                else if (isMovingDown || Mathf.Abs(climbSpeed) <= climbSpeedThreshold)
                {
                    // Si le joueur se déplace vers le bas ou s'il est en pause verticale, on passe à WallGrabIdle
                    _anim.SetBool("isWallClimbing", false);
                    if (!isVerticalIdle && !isLookingBehind)
                    {
                        _anim.SetBool("isWallGrabIdle", true); // Si le joueur descend, on utilise WallGrabIdle
                    }
                }
            }
            else
            {
                // Si le joueur n'est pas accroché au mur
                _anim.SetBool("isWallClimbing", false);
                _anim.SetBool("isGrabbingWall", false);
                _anim.SetBool("isWallGrabIdle", false);
                _anim.SetBool("isLookingBehind", false);
            }
        }

        private float lookBehindBufferTime = 0.05f;
        private float lookBehindTimer = 0f;
        private bool DetermineLookingBehind()
        {
            bool isWallOnRight = !_sprite.flipX;
            bool isHorizontalInput = Mathf.Abs(_player.FrameInput.x) > 0.1f && Mathf.Abs(_player.FrameInput.y) < 0.1f;

            if (!isHorizontalInput)
            {
                lookBehindTimer = 0f;
                return false;
            }

            // Ajoute un buffer pour éviter un changement instantané
            lookBehindTimer += Time.deltaTime;

            if (lookBehindTimer >= lookBehindBufferTime)
            {
                return (isWallOnRight && _player.FrameInput.x < 0) || (!isWallOnRight && _player.FrameInput.x > 0);
            }

            return false;
        }

        private void OnWallSlide()
        {
            if (_player.IsSliding)
            {
                // Utilise la variable isFlipped pour savoir si le joueur est en flip X
                float offsetX = isFlipped ? -0.17f : 0.17f; // Décalage à gauche si flip X, à droite sinon
                _wallSlideParticles.transform.position = new Vector3(transform.position.x + offsetX, transform.position.y, transform.position.z);

                if (!_wallSlideParticles.isPlaying)
                {
                    _wallSlideParticles.Play();
                }
            }
            else
            {
                if (_wallSlideParticles.isPlaying)
                {
                    _wallSlideParticles.Stop();
                }
            }

            // LedgeClimb Bug Fix REF z34 #7/z#7
            if (_player.canClimb)
            {
                _wallSlideParticles.Stop();
            }
        }

        private void OnJumped()
        {
            _anim.SetTrigger(JumpKey);
            _anim.ResetTrigger(GroundedKey);

            if (_jumpParticles != null && _player.IsGrounded)
            {
                _jumpParticles.transform.position = new Vector3(transform.position.x, transform.position.y - 0.65f, transform.position.z);

                _jumpParticles.Play();
            }
        }

        private void OnWallJumped()
        {
            if (_wallJumpParticles != null)
            {
                float offsetX = isFlipped ? -0.18f : 0.18f;
                _wallJumpParticles.transform.position = new Vector3(transform.position.x + offsetX, transform.position.y, transform.position.z);
                _wallJumpParticles.Play();
            }
        }

        private void OnPush()
        {
            if (_player.IsGrounded)
            {
                var velocityOverLifetime = _pushParticles.velocityOverLifetime;
                velocityOverLifetime.x = isFlipped ? 70f : -70f;

                _pushParticles.Play();
            }
            else
            {
                _pushParticles.Stop();
            }
        }

        private void OnGroundedChanged(bool grounded, float impact)
        {
            if (grounded) 
            {
                _anim.SetTrigger(GroundedKey);

                _landParticles.transform.position = new Vector3(transform.position.x, transform.position.y - 0.65f, transform.position.z);

                _landParticles.Play(); // ajouter un if velocity.y min ?? dono
            }
        }

        private void DetectGroundColor()
        {
            var hit = Physics2D.Raycast(transform.position, Vector3.down, 2);

            if (!hit || hit.collider.isTrigger || !hit.transform.TryGetComponent(out SpriteRenderer r)) return;
            var color = r.color;
            _currentGradient = new ParticleSystem.MinMaxGradient(color * 0.9f, color * 1.2f);
        }

        private void SetColor(ParticleSystem ps)
        {
            var main = ps.main;
            main.startColor = _currentGradient;
        }

        private static readonly int GroundedKey = Animator.StringToHash("Grounded");
        private static readonly int IdleSpeedKey = Animator.StringToHash("IdleSpeed");
        private static readonly int JumpKey = Animator.StringToHash("Jump");
    }
}
