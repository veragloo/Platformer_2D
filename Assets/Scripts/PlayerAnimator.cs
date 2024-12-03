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
        [SerializeField] private ParticleSystem _launchParticles;
        [SerializeField] private ParticleSystem _moveParticles;
        [SerializeField] private ParticleSystem _landParticles;

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
            _player.GroundedChanged += OnGroundedChanged;

            _moveParticles.Play();
        }

        private void OnDisable()
        {
            _player.Jumped -= OnJumped;
            _player.GroundedChanged -= OnGroundedChanged;

            _moveParticles.Stop();
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
        }

        private void HandleSpriteFlip()
        {
            if (_player.FrameInput.x != 0 && !_player.IsGrabbingWall && !_player.canClimb && !_player.IsDashing)
            {
                _sprite.flipX = _player.FrameInput.x < 0;
            }
        }

        private void HandleIdleSpeed()
        {
            var inputStrength = Mathf.Abs(_player.FrameInput.x);
            _anim.SetFloat(IdleSpeedKey, Mathf.Lerp(1, _maxIdleSpeed, inputStrength));
            _moveParticles.transform.localScale = Vector3.MoveTowards(_moveParticles.transform.localScale, Vector3.one * inputStrength, 2 * Time.deltaTime);
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

        private void OnJumped()
        {
            _anim.SetTrigger(JumpKey);
            _anim.ResetTrigger(GroundedKey);

            if (_player.IsGrounded)
            {
                SetColor(_jumpParticles);
                SetColor(_launchParticles);
                _jumpParticles.Play();
            }
        }

        private void OnGroundedChanged(bool grounded, float impact)
        {
            if (grounded)
            {
                DetectGroundColor();
                SetColor(_landParticles);

                _anim.SetTrigger(GroundedKey);
                _source.PlayOneShot(_footsteps[Random.Range(0, _footsteps.Length)]);
                _moveParticles.Play();

                _landParticles.transform.localScale = Vector3.one * Mathf.InverseLerp(0, 40, impact);
                _landParticles.Play();
            }
            else
            {
                _moveParticles.Stop();
            }
        }

        private void DetectGroundColor()
        {
            var hit = Physics2D.Raycast(transform.position, Vector3.down, 2);

            if (!hit || hit.collider.isTrigger || !hit.transform.TryGetComponent(out SpriteRenderer r)) return;
            var color = r.color;
            _currentGradient = new ParticleSystem.MinMaxGradient(color * 0.9f, color * 1.2f);
            SetColor(_moveParticles);
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
