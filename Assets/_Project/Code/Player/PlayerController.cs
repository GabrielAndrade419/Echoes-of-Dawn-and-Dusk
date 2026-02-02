using UnityEngine;
using UnityEngine.InputSystem;
using Echoes.Inputs;
using Unity.Cinemachine;

public class PlayerController : MonoBehaviour
{
    [Header("Settings")]
    public float walkSpeed = 3.5f;
    public float runSpeed = 7f;
    public float rotationSpeed = 15f;

    [Header("Camera Zoom")] 
    public CinemachineCamera cineCamera; 
    public float minZoom = 5f;  
    public float maxZoom = 25f; 
    public float zoomSpeed = 2f; 
    public float zoomDamping = 5f;
    
    [Header("Combat Settings")]
    public float combatModeDuration = 5f;

    [Header("Combat Visuals")]
    public GameObject weaponInHandModel;
    public GameObject weaponInSheathModel;
    public bool startArmed = false;

    [Header("References")]
    public CharacterController characterController;
    public Animator animator;

    private InputSystem_Actions _inputActions;
    private Camera _mainCamera;
    
    private Vector2 _moveInput;
    private Vector2 _mousePos;
    private bool _isSprinting;
    private float _targetZoom; 
    private CinemachinePositionComposer _posComposer;
    private bool _isArmed;
    private bool _isInCombatMode;
    private float _lastAttackTime;

    private Vector2 _currentAnimationBlend;
    private Vector2 _animationVelocity; 

    private void Awake()
    {
        _inputActions = new InputSystem_Actions();
        _mainCamera = Camera.main;
        
        if (characterController == null) characterController = GetComponent<CharacterController>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (cineCamera != null)
        {
            _posComposer = cineCamera.GetComponent<CinemachinePositionComposer>();
            
            if (_posComposer != null)
            {
                //Debug.Log("ALTURA PADRÃO DA CÂMERA: " + _posComposer.CameraDistance);
                _targetZoom = _posComposer.CameraDistance;
            }
        }

        SetVisualWeaponState(startArmed);
    }

    private void OnEnable() => _inputActions.Player.Enable();
    private void OnDisable() => _inputActions.Player.Disable();

    private void Update()
    {
        ReadInput();
        HandleZoom();
        HandleRotation();
        HandleMovement();
        HandleCombatStanceTimer();
    }

    private void ReadInput()
    {
        _moveInput = _inputActions.Player.Move.ReadValue<Vector2>();
        _mousePos = Mouse.current.position.ReadValue();
        _isSprinting = _inputActions.Player.Sprint.IsPressed();

        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            SetVisualWeaponState(!_isArmed);
        }

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            TriggerAttack();
        }
    }

    private void HandleZoom()
    {
        if (_posComposer == null) return;

        float scrollInput = _inputActions.Player.Zoom.ReadValue<float>();

        //if (scrollInput != 0) Debug.Log("Rodinha detectada: " + scrollInput);

        if (scrollInput != 0)
        {
            _targetZoom -= scrollInput * zoomSpeed; 
            _targetZoom = Mathf.Clamp(_targetZoom, minZoom, maxZoom);
        }
        _posComposer.CameraDistance = Mathf.Lerp(_posComposer.CameraDistance, _targetZoom, Time.deltaTime * zoomDamping);
    }

    private void HandleMovement()
    {
        Vector3 camForward = _mainCamera.transform.forward;
        Vector3 camRight = _mainCamera.transform.right;
        camForward.y = 0; camRight.y = 0;
        camForward.Normalize(); camRight.Normalize();

        Vector3 moveDirection = (camForward * _moveInput.y + camRight * _moveInput.x).normalized;

        float targetSpeed = _isSprinting ? runSpeed : walkSpeed;

        if (_moveInput.magnitude < 0.1f) targetSpeed = 0;

        characterController.Move(moveDirection * targetSpeed * Time.deltaTime);

        Vector3 localMove = transform.InverseTransformDirection(moveDirection);

        float animationWeight = _isSprinting ? 1f : 0.5f;

        _currentAnimationBlend = Vector2.SmoothDamp(_currentAnimationBlend, 
            new Vector2(localMove.x, localMove.z) * animationWeight, 
            ref _animationVelocity, 
            0.1f);

        if (_moveInput.magnitude < 0.1f) _currentAnimationBlend = Vector2.zero;

        if (animator != null)
        {
            animator.SetFloat("InputX", _currentAnimationBlend.x);
            animator.SetFloat("InputZ", _currentAnimationBlend.y);
        }
    }

    private void HandleRotation()
    {
        Ray ray = _mainCamera.ScreenPointToRay(_mousePos);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

        if (groundPlane.Raycast(ray, out float enter))
        {
            Vector3 hitPoint = ray.GetPoint(enter);
            Vector3 lookDir = hitPoint - transform.position;
            lookDir.y = 0;

            if (lookDir != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }
    }

    private void TriggerAttack()
    {
        if (!_isArmed)
        {
            SetVisualWeaponState(true);
        }

        _isInCombatMode = true;
        _lastAttackTime = Time.time;

        if (animator != null)
        {
            animator.SetBool("InCombat", true);
            animator.SetTrigger("Attack");
        }
    }

    private void HandleCombatStanceTimer()
    {
        if (!_isInCombatMode) return;

        if (Time.time - _lastAttackTime > combatModeDuration)
        {
            _isInCombatMode = false;
            
            if (animator != null)
            {
                animator.SetBool("InCombat", false);
            }
        }
    }

    public void SetVisualWeaponState(bool armed)
    {
        _isArmed = armed;

        if (weaponInHandModel != null) weaponInHandModel.SetActive(_isArmed);
        if (weaponInSheathModel != null) weaponInSheathModel.SetActive(!_isArmed);
    }
}