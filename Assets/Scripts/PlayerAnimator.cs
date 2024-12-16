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
        [SerializeField] private float _maxTilt = 5;
        [SerializeField] private float _tiltSpeed = 20;

        [Header("Particles")]
        [SerializeField] private ParticleSystem _jumpParticles;
        [SerializeField] private ParticleSystem _wallJumpParticles;
        [SerializeField] private ParticleSystem _landParticles;
        [SerializeField] private ParticleSystem _wallSlideParticles;
        [SerializeField] private ParticleSystem _wallGrabParticles;
        [SerializeField] private ParticleSystem _pushParticles;

        [Header("Audio Clips")]
        [SerializeField] private AudioClip[] _footsteps;

        private AudioSource _source;
        private IPlayerController _player;
        private ParticleSystem.MinMaxGradient _currentGradient;
        private Rigidbody2D _rigidbody;

        private void Awake()
        {
            _source = GetComponent<AudioSource>();
            _player = GetComponentInParent<IPlayerController>();
            _rigidbody = GetComponent<Rigidbody2D>();
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

            float verticalSpeed = _rigidbody.linearVelocity.y; // Vitesse verticale
            _anim.SetFloat("VerticalSpeed", verticalSpeed);

            _anim.SetBool("Grounded", _player.IsGrounded);

            _anim.SetBool("canClimb", _player.canClimb);

            DetectGroundColor();
            HandleSpriteFlip();
            HandleIdleSpeed();
            HandleCharacterTilt();
            HandleDashAnimation();
            HandleGrabClimbing();
            OnWallSlide();
            OnWallGrab();
        }

        private bool isFlipped = false; 
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

        private void HandleCharacterTilt()
        {
            if (_player.IsGrabbingWall)
            {
                // Réinitialiser l'inclinaison lorsque le joueur s'accroche au mur
                _anim.transform.up = Vector2.up;
            }
            else
            {
                var runningTilt = _player.IsGrounded ? Quaternion.Euler(0, 0, _maxTilt * _player.FrameInput.x) : Quaternion.identity;
                _anim.transform.up = Vector3.RotateTowards(_anim.transform.up, runningTilt * Vector2.up, _tiltSpeed * Time.deltaTime, 0f);
            }
        }


        private void HandleDashAnimation()
        {
            _anim.SetBool("isDashing", _player.IsDashing);
        }

        private bool isClimbing = false;
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
                _anim.SetBool("isGrabbingWall", true);

                float climbSpeed = _player.CurrentClimbSpeed;
                _anim.SetFloat("climbSpeed", climbSpeed);

                if (Mathf.Abs(climbSpeed) > climbSpeedThreshold && !isClimbing)
                {
                    _anim.Play("WallGrabClimb");
                    isClimbing = true;
                }
                else if (Mathf.Abs(climbSpeed) <= climbSpeedThreshold && isClimbing)
                {
                    _anim.Play("WallGrabIdle");
                    isClimbing = false;
                }
            }
            else
            {
                _anim.SetBool("isGrabbingWall", false);
                isClimbing = false;
            }
        }

        private void OnWallSlide()
        {
            if (_player.IsWallSliding)
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
        }

        private void OnWallGrab()
        {
            if (_player.IsGrabbingWall && _rigidbody.linearVelocity.y < - 1f) 
            {
                // Utilisation de _wallGrabParticles pour l'agrippement au mur
                float offsetX = isFlipped ? -0.17f : 0.17f; // Décalage selon le flip X
                _wallGrabParticles.transform.position = new Vector3(transform.position.x + offsetX, transform.position.y, transform.position.z);

                if (!_wallGrabParticles.isPlaying)
                {
                    _wallGrabParticles.Play();
                }
            }
            else
            {
                if (_wallGrabParticles.isPlaying)
                {
                    _wallGrabParticles.Stop();
                }
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
                float offsetX = isFlipped ? -0.17f : 0.17f;
                _wallJumpParticles.transform.position = new Vector3(transform.position.x + offsetX, transform.position.y, transform.position.z);
                _wallJumpParticles.Play();
            }
        }

        private void OnPush()
        {
            var velocityOverLifetime = _pushParticles.velocityOverLifetime;
            velocityOverLifetime.x = isFlipped ? 100f : -100f;
        
            _pushParticles.Play();
        }

        private void OnGroundedChanged(bool grounded, float impact)
        {
            if (grounded) 
            {
                _anim.SetTrigger(GroundedKey);
                // _source.PlayOneShot(_footsteps[Random.Range(0, _footsteps.Length)]);

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
