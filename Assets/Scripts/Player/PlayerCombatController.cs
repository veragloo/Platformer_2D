using System;
using UnityEngine;
using System.Collections;

namespace TarodevController
{
    public class CombatController : MonoBehaviour
    {
        private IPlayerController _player;
        private Animator _animator;
        private int _combatLayerIndex;

        [SerializeField] private float attackCooldown = 0.5f;
        private float _lastAttackTime;

        [SerializeField] private LayerMask enemyLayer;
        [SerializeField] private float attackRange = 1.5f;

        private void Awake()
        {
            // Récupérer le PlayerController et l'Animator
            _player = GetComponent<IPlayerController>();
            _animator = GetComponent<Animator>();
            _combatLayerIndex = _animator.GetLayerIndex("Combat Layer");
        }

        private void OnEnable()
        {
            _player.Attack += OnAttack;
        }

        private void OnDisable()
        {
            _player.Attack -= OnAttack;
        }

        // Activer le layer Combat
        private void ActivateCombatLayer()
        {
            _animator.SetLayerWeight(_combatLayerIndex, 1); // Poids max
        }

        // Désactiver le layer Combat
        private void DeactivateCombatLayer()
        {
            _animator.SetLayerWeight(_combatLayerIndex, 0); // Poids nul
        }

        private void OnAttack()
        {
            Debug.Log("Attack triggered!");

            if (Time.time < _lastAttackTime + attackCooldown) return;
            _lastAttackTime = Time.time;

            ActivateCombatLayer();

            // Déterminer la direction d'attaque
            Vector2 attackDirection = GetAttackDirection();
            if (attackDirection == Vector2.zero) return;

            // Envoyer la direction au Blend Tree
            _animator.SetFloat("AttackX", attackDirection.x);
            _animator.SetFloat("AttackY", attackDirection.y);

            // Déterminer l'état (Idle, Run ou Air)
            if (_player.IsGrounded)
            {
                if (_player.IsRunning)
                {
                    _animator.SetTrigger("RunAttack");
                }
                else
                {
                    _animator.SetTrigger("Attack");
                }
            }
            else
            {
                _animator.SetTrigger("AirAttack");
            }

            StartCoroutine(DisableCombatLayerAfterAnimation());

            // Vérifier les collisions via Raycast
            RaycastHit2D hit = Physics2D.Raycast(transform.position, attackDirection, attackRange, enemyLayer);
            if (hit.collider != null)
            {
                Debug.Log($"Hit {hit.collider.name}!");
                // Exemple : hit.collider.GetComponent<Enemy>()?.TakeDamage();
            }
        }

        private IEnumerator DisableCombatLayerAfterAnimation()
        {
            yield return new WaitForSeconds(attackCooldown); // Durée de l'animation d'attaque
            DeactivateCombatLayer();
        }

        private Vector2 GetAttackDirection()
        {
            // Utiliser l'entrée du joueur pour déterminer la direction
            var input = _player.FrameInput;  // Utiliser _player.FrameInput ici
            if (input == Vector2.zero) return Vector2.right; // Valeur par défaut si aucune direction n'est donnée

            // Normaliser la direction pour éviter les diagonales non prévues
            if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
                return new Vector2(Mathf.Sign(input.x), 0); // Attaque horizontale
            else
                return new Vector2(0, Mathf.Sign(input.y)); // Attaque verticale
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, transform.position + Vector3.right * attackRange);
        }
    }
}
