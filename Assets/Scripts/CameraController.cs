using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using static UnityEngine.Debug;

namespace Control
{
    [RequireComponent(typeof(Camera))]
    public class CameraController : MonoBehaviour
    {
        [SerializeField]
        private KeyCode _rotateKey = KeyCode.Mouse0;

        [SerializeField]
        private KeyCode _planarShiftKey = KeyCode.Mouse2;

        [SerializeField]
        private Vector3 _origin;

        private Vector3 _targetVector;

        private Vector3 _targetUpVector;

        private Vector3 _currentVector;

        private Vector3 _currentUpVector;

        private float _size;

        [SerializeField]
        [Range(1f, 32f)]
        private float _transformInterpolation = 1f;

        [SerializeField]
        [Range(1f, 8f)]
        private float _mouseDeltaMultiplier = 1f;

        [SerializeField]
        [Range(1f, 64)]
        private float _mouseScrollMultiplier = 1f;

        [SerializeField]
        [Range(1f, 256f)]
        private float _offset = 1f;

        [SerializeField]
        [Range(1f, 64f)]
        private float _maxSize = 1f;

        [SerializeField]
        [Range(1f, 32f)]
        private float _sizeInterpolation = 1f;

        private bool _isMouseInViewport;

        [SerializeField]
        [Range(0f, 24f)]
        private float _anchorAngle = 8f;

        [SerializeField]
        private bool _isActive;

        #region Editor
#if UNITY_EDITOR
        [Header("Debug")]
        [SerializeField]
        private bool _drawDebugLines;
#endif
        #endregion

        public Camera Camera { get; private set; }

        public float Size
        {
            get { return _size; }
            set { _size = Mathf.Clamp(value, 1f, _maxSize); }
        }

        public bool IsActive { get => _isActive; set => _isActive = value; }

        private void Awake()
        {
            Camera = GetComponent<Camera>();
            SetTargetsToCurrentState();
        }

        private void Update()
        {
            CalculateTargetSize();

            SetTransformToTargetsOrAnchor();
            SetSizeToTarget();

            CheckPlanarShifting();

            #region Editor
#if UNITY_EDITOR
            if (_drawDebugLines) DrawDebugLines();
#endif
            #endregion
        }

        private void CalculateTargetTransform()
        {
            if (!_isActive) return;

            Vector2 mouseDeltas = Input.GetKey(_rotateKey) ? new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) : Vector2.zero;
            mouseDeltas *= _mouseDeltaMultiplier;

            Quaternion deltaXRotation = Quaternion.Euler(0f, mouseDeltas.x, 0f);
            _targetVector = deltaXRotation * _targetVector;
            _targetUpVector = deltaXRotation * _targetUpVector;

            Quaternion yRotation = Quaternion.LookRotation(_targetVector, _targetUpVector) * Quaternion.Euler(mouseDeltas.y, 0f, 0f);
            _targetVector = yRotation * Vector3.forward;
            _targetUpVector = yRotation * Vector3.up;
        }

        private void CalculateTargetSize()
        {
            if (!_isMouseInViewport || !_isActive) return;

            Size += -Input.GetAxis("Mouse ScrollWheel") * _mouseScrollMultiplier;
        }

        private void SetTransformToTargetsOrAnchor()
        {
            if (IsAnchorMode())
            {
                if (GetAnchoredVectors(out Vector3 positionVector, out Vector3 upVector))
                {
                    SetTrasnsform(positionVector, upVector);
                }
                else
                {
                    SetTransformToCurrent();
                }
            }
            else if (IsAnchorModeTurnedOffRightNow())
            {
                if (GetAnchoredVectors(out Vector3 positionVector, out Vector3 upVector))
                {
                    _targetVector = _currentVector = positionVector;
                    _targetUpVector = _currentUpVector = upVector;
                }
            }
            else
            {
                SetTransformToCurrent();
            }
        }

        private bool IsAnchorMode() => Input.GetKey(KeyCode.LeftAlt);

        private bool IsAnchorModeTurnedOffRightNow() => Input.GetKeyUp(KeyCode.LeftAlt);

        private void SetTransformToCurrent()
        {
            _currentVector = Quaternion.Lerp(Quaternion.LookRotation(_currentVector, _targetUpVector), Quaternion.LookRotation(_targetVector, _targetUpVector), _transformInterpolation * Time.deltaTime) * Vector3.forward;
            _currentUpVector = Quaternion.Lerp(Quaternion.LookRotation(_currentUpVector, _currentVector), Quaternion.LookRotation(_targetUpVector, _targetVector), _transformInterpolation * Time.deltaTime) * Vector3.forward;

            SetTrasnsform(_currentVector, _currentUpVector);
        }

        private void SetTrasnsform(Vector3 positionVector, Vector3 upVector)
        {
            transform.position = _origin + (positionVector * _offset);
            transform.LookAt(_origin, upVector);
        }

        private bool GetAnchoredVectors(out Vector3 positionVector, out Vector3 upVector)
        {
            positionVector = upVector = Vector3.zero;

            if (TryAnchorToAxis(Vector3.right, Vector3.up, ref positionVector, ref upVector))
                return true;

            if (TryAnchorToAxis(-Vector3.right, Vector3.up, ref positionVector, ref upVector))
                return true;

            if (TryAnchorToAxis(Vector3.up, Vector3.forward, ref positionVector, ref upVector))
                return true;

            if (TryAnchorToAxis(-Vector3.up, -Vector3.forward, ref positionVector, ref upVector))
                return true;

            if (TryAnchorToAxis(Vector3.forward, Vector3.up, ref positionVector, ref upVector))
                return true;

            if (TryAnchorToAxis(-Vector3.forward, Vector3.up, ref positionVector, ref upVector))
                return true;

            return false;
        }

        private bool TryAnchorToAxis(Vector3 axis, Vector3 associatedUpVector, ref Vector3 positionVector, ref Vector3 upVector)
        {
            if (Vector3.Angle(_targetVector, axis) <= _anchorAngle)
            {
                positionVector = axis;
                upVector = associatedUpVector;

                return true;
            }

            return false;
        }

        private void SetSizeToTarget()
        {
            Camera.orthographicSize = Mathf.Lerp(Camera.orthographicSize, Size, _sizeInterpolation * Time.deltaTime);
        }

        public void SetTargetsToCurrentState()
        {
            _size = Camera.orthographicSize;

            _targetVector = transform.position - _origin;

            _targetVector.Normalize();
            _targetUpVector = transform.up;

            _currentVector = _targetVector;
            _currentUpVector = _targetUpVector;
        }

        public void CheckPlanarShifting()
        {
            if (_isMouseInViewport && Input.GetKeyDown(_planarShiftKey))
                StartCoroutine(PlanarShiftRoutine());
        }

        private IEnumerator PlanarShiftRoutine()
        {
            var originStartPosition = _origin;
            var right = transform.right;
            var up = transform.up;

            var mouseStartPosition = Input.mousePosition;

            while (Input.GetKey(_planarShiftKey))
            {
                var mouseDelta = Input.mousePosition - mouseStartPosition;

                var deltaX = mouseDelta.x / Screen.width * Camera.orthographicSize * Camera.aspect * 2f;
                var deltaY = mouseDelta.y / Screen.height * Camera.orthographicSize * 2f;

                _origin = originStartPosition - (right * deltaX) - (up * deltaY);

                yield return null;
            }
        }

        public void ViewFromDirection(Vector3 direction, Vector3 up)
        {
            _targetVector = direction;
            _targetUpVector = up;
        }

        public void FocusOn(Vector3 point) => _origin = point;

        #region Editor
#if UNITY_EDITOR
        private void DrawDebugLines()
        {
            DrawLine(_origin, _origin + _targetVector, Color.red);
            DrawLine(_origin, _origin + _targetUpVector, Color.green);
            DrawLine(_origin, _origin + _currentVector, Color.yellow);
            DrawLine(_origin, _origin + _currentUpVector, Color.white);
        }
#endif
        #endregion

        #region Event handlers
        public void CameraViewport_PointerEnter() => _isMouseInViewport = true;

        public void CameraViewport_PointerExit() => _isMouseInViewport = false;

        public void CameraViewport_Drag() => CalculateTargetTransform();
        #endregion
    }
}