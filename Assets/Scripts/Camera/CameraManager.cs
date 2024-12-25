using UnityEngine;
using Unity.Cinemachine;
using System.Collections;

namespace TarodevController
{
    public class CameraManager : MonoBehaviour
    {
        public static CameraManager instance;

        [SerializeField] private CinemachineCamera[] _allCamera;

        [Header("Controls for lerping the Y Damping during player jump/fall")]
        [SerializeField] private Vector3 _fallPanAmout = new Vector3(0, 0.25f, 0);
        [SerializeField] private float _fallYPanTime = 0.35f;
        public float _fallSpeedYDampingChangeThreshold = -1f;

        public bool IsLerpingYDamping { get; private set; }
        public bool LerpedFromPlayerFalling { get; set; }

        private Coroutine _LerpYPanCoroutine;

        private CinemachineCamera _currentCamera;
        private CinemachinePositionComposer _positionComposer;

        private Vector3 _normYPanAmout = Vector3.zero;

        private void Awake()
        {

            Debug.Log("Hello");
            if (instance == null)
            {
                instance = this;
            }

            for (int i = 0; i < _allCamera.Length; i++)
            {
                if (_allCamera[i].enabled)
                {
                    // Set the current active camera
                    _currentCamera = _allCamera[i];

                    // Set the Position Composer
                    _positionComposer = _currentCamera.GetComponentInChildren<CinemachinePositionComposer>();
                }
            }

            // Set the YDamping amount
            _normYPanAmout = _positionComposer.Damping;
        }

        private void OnEnable()
        {
            Debug.Log("enable");
            PlayerController.OnVerticalVelocityChanged += HandleVerticalVelocityChanged;
        }
        
        private void OnDisable()
        {
            Debug.Log("disable");
            PlayerController.OnVerticalVelocityChanged -= HandleVerticalVelocityChanged;
        }
        
        private void HandleVerticalVelocityChanged(float verticalVelocity)
        {
            if (verticalVelocity < _fallSpeedYDampingChangeThreshold)
            {
                LerpYDamping(true);
            }
            else
            {
                LerpYDamping(false);
            }
        }

        #region Lerp Y Damping

        public void LerpYDamping(bool isPlayerFalling)
        {
            if (_LerpYPanCoroutine != null)
            {
                StopCoroutine(_LerpYPanCoroutine);
            }

            _LerpYPanCoroutine = StartCoroutine(LerpYAction(isPlayerFalling));
        }

        private IEnumerator LerpYAction(bool isPlayerFalling)
        {
            IsLerpingYDamping = true;

            // Grab the starting damping amount
            Vector3 startDampAmount = _positionComposer.Damping;
            Vector3 endDampAmount = Vector3.zero;

            // Determine the end damping amount
            if (isPlayerFalling)
            {
                endDampAmount = _fallPanAmout;
                LerpedFromPlayerFalling = true;
            }
            else
            {
                endDampAmount = _normYPanAmout;
            }

            // Lerp the pan amount
            float elapsedTime = 0f;

            while (elapsedTime < _fallYPanTime)
            {
                elapsedTime += Time.deltaTime;

                Vector3 lerpedPanAmount = Vector3.Lerp(startDampAmount, endDampAmount, elapsedTime / _fallYPanTime);
                _positionComposer.Damping = new Vector3(_positionComposer.Damping.x, lerpedPanAmount.y, _positionComposer.Damping.z);

                yield return null;
            }

            IsLerpingYDamping = false;
        }

        #endregion
    }
}
