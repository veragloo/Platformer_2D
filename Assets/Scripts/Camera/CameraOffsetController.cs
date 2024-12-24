using UnityEngine;
using Unity.Cinemachine;

namespace TarodevController
{
    public class CameraTargetOffset : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CinemachineCamera cinemachineCamera;  
        [SerializeField] private SpriteRenderer _sprite;  

        [Header("Settings")]
        [SerializeField] private Vector3 defaultOffset = new Vector3(1, 0, 0);  
        [SerializeField] private Vector3 flippedOffset = new Vector3(-1, 0, 0);  
        [SerializeField] private float transitionSpeed = 0f; 

        private CinemachinePositionComposer positionComposer;
        private Vector3 currentOffset; 

        private void Start()
        {
            // Obtient le composant CinemachinePositionComposer
            positionComposer = cinemachineCamera.GetComponentInChildren<CinemachinePositionComposer>();

            // Initialise l'offset actuel avec l'offset par défaut
            currentOffset = positionComposer.TargetOffset;
        }

        private void Update()
        {
            if (_sprite != null && positionComposer != null)
            {
                // Détermine l'offset cible en fonction de l'état de flipX
                Vector3 targetOffset = _sprite.flipX ? flippedOffset : defaultOffset;

                // Interpole entre l'offset actuel et l'offset cible
                currentOffset = Vector3.Lerp(currentOffset, targetOffset, transitionSpeed * Time.deltaTime);

                // Applique l'offset interpolé à la caméra
                positionComposer.TargetOffset = currentOffset;
            }
        }
    }
}
