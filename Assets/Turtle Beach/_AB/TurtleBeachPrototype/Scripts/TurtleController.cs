using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Fusion.XR.Shared.Rig;

namespace AB.TurtleBeach
{
    public interface IObstacle
    {
        void DoCollision(TurtleController controller);
    }

    public interface IQuest
    {
        void Start();
        void Complete();
        void Reset();
        void AddTask(string name, int type, int val, object obj);
    }

    public class DummyQuest : IQuest
    {
        public void Start() { Debug.Log("[Quest] Sea Turtle Express Started!"); }
        public void Complete() { Debug.Log("[Quest] Sea Turtle Express Completed!"); }
        public void Reset() { Debug.Log("[Quest] Sea Turtle Express Reset!"); }
        public void AddTask(string name, int type, int val, object obj) { }
    }

    public class ChildTriggerDelegate : MonoBehaviour
    {
        public System.Action<Collider> onTriggerEnter;
        public System.Action<Collider> onTriggerExit;

        private void OnTriggerEnter(Collider other)
        {
            onTriggerEnter?.Invoke(other);
        }

        private void OnTriggerExit(Collider other)
        {
            onTriggerExit?.Invoke(other);
        }
    }

    public class TurtleController : MonoBehaviour
    {
        public static System.Action<TurtleController> OnReachedGoal;
        public static System.Action<TurtleController, bool> OnHidingStateChanged;

        [SerializeField] GameObject interactable; // Kept as GameObject to preserve prefab reference!
        [SerializeField] GameObject turtleExitCanvas;
        [SerializeField] Button turtleExitButton;
        [SerializeField] GameObject cameraTarget;
        [SerializeField] GameObject turtleVisual;
        [SerializeField] GameObject successVFX;
        public GameObject interactionCanvasPrefab;

        private bool _isControlled = false;
        private bool _isMoving = false;
        private bool _isActive = true;

        Vector2 movementVector = Vector2.zero;
        Rigidbody _rb;
        BoxCollider _boxCollider;
        float _rotationSpeed = 10f;
        float _moveSpeed = 4.5f;
        float _boostedMoveSpeed = 8f;
        float _powerupCounter = 0f;
        float _powerupDuration = 3.5f;
        bool _hasPowerup = false;
        bool _respawnTurtle = false;
        float _respawnTime = 0f; // in seconds
        float _respawnCounter = 0f;
        bool _turtleReachedGoal = false;
        bool _isHiding = false;

        Animator _animator;
        readonly string _walkAnimation = "Walk";
        readonly string _idleAAnimation = "Idle";

        Vector3 _startPosition;
        Quaternion _startRotation;
        Vector3 _playerStartPosition;
        Quaternion _playerStartRotation;
        WaitForSeconds _returnControlDelay = new WaitForSeconds(2f);
        WaitForSeconds _playerAppearDelay = new WaitForSeconds(2.5f);

        IQuest _turtleQuest;
        private HardwareRig _playerRig;
        private bool playerInside = false;
        private GameObject interactionCanvas;

        private void Awake()
        {
            _startPosition = transform.position;
            _startRotation = transform.rotation;

            if (interactable == null)
            {
                Transform childInteractable = transform.Find("Interactable");
                if (childInteractable != null) interactable = childInteractable.gameObject;
            }

            // Setup interaction prompt canvas inside the interactable
            if (interactable != null)
            {
                // Ensure interactable child has a trigger collider
                var col = interactable.GetComponent<Collider>();
                if (col == null)
                {
                    var sphere = interactable.AddComponent<SphereCollider>();
                    sphere.isTrigger = true;
                    sphere.radius = 2.0f;
                }
                else
                {
                    col.isTrigger = true;
                }

                // Attach trigger delegate to child to forward triggers to this script
                var triggerDelegate = interactable.GetComponent<ChildTriggerDelegate>();
                if (triggerDelegate == null)
                {
                    triggerDelegate = interactable.AddComponent<ChildTriggerDelegate>();
                }
                triggerDelegate.onTriggerEnter = OnInteractTriggerEnter;
                triggerDelegate.onTriggerExit = OnInteractTriggerExit;

                Transform canvasT = interactable.transform.Find("InteractionCanvas");
                if (canvasT != null)
                {
                    interactionCanvas = canvasT.gameObject;
                    interactionCanvas.SetActive(false);
                }
                else if (interactionCanvasPrefab != null)
                {
                    interactionCanvas = Instantiate(interactionCanvasPrefab, interactable.transform);
                    interactionCanvas.name = "InteractionCanvas";
                    interactionCanvas.transform.localPosition = new Vector3(0f, 0.4f, 0f);
                    interactionCanvas.transform.localRotation = Quaternion.identity;
                    interactionCanvas.transform.localScale = new Vector3(0.012f, 0.012f, 0.012f);
                    interactionCanvas.SetActive(false);
                }

                if (interactionCanvas != null)
                {
                    var btn = interactionCanvas.GetComponentInChildren<UnityEngine.UI.Button>();
                    if (btn != null)
                    {
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(BecomeTheTurtle);
                    }
                }
            }
        }

        private void Start()
        {
            _animator = GetComponent<Animator>();
            _rb = GetComponent<Rigidbody>();
            _boxCollider = GetComponent<BoxCollider>();
            if (_rb != null) _rb.isKinematic = false;
            if (_boxCollider != null) _boxCollider.isTrigger = false; // Rigid solid body on terrain!
            if (turtleExitCanvas != null) turtleExitCanvas.SetActive(false);
        }        

        private void OnEnable()
        {
            if (turtleExitButton != null)
            {
                turtleExitButton.onClick.RemoveAllListeners();
                turtleExitButton.onClick.AddListener(ExitTurtle);
            }
        }        

        private void OnDisable()
        {
            if (turtleExitButton != null)
            {
                turtleExitButton.onClick.RemoveAllListeners();
            }
        }

        private void Update()
        {
            // Input capture and movement vector generation
            if (_isControlled)
            {
                Vector2 input = Vector2.zero;
                if (WebXR.FusionBridge.WebXRFusionBridge.Active)
                {
                    input = WebXR.FusionBridge.WebXRFusionBridge.GetMoveInput(true, true);
                }
                else
                {
#if ENABLE_INPUT_SYSTEM
                    if (UnityEngine.InputSystem.Keyboard.current != null)
                    {
                        var kb = UnityEngine.InputSystem.Keyboard.current;
                        if (kb.wKey.isPressed || kb.upArrowKey.isPressed) input.y += 1f;
                        if (kb.sKey.isPressed || kb.downArrowKey.isPressed) input.y -= 1f;
                        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) input.x -= 1f;
                        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) input.x += 1f;
                    }
#else
                    input.x = Input.GetAxis("Horizontal");
                    input.y = Input.GetAxis("Vertical");
#endif
                }

                movementVector = input.normalized;
                _isMoving = movementVector.magnitude > 0.01f;
            }
            else
            {
                _isMoving = false;
                movementVector = Vector2.zero;
            }

            if (_isMoving)
            {
                if (!_turtleReachedGoal)
                {
                    if (turtleVisual != null && turtleVisual.activeSelf)
                    {
                        // Direct vehicle (tank) controls: turn left/right and move forward/backward
                        float turn = movementVector.x * _rotationSpeed * 8f * Time.deltaTime;
                        transform.Rotate(0f, turn, 0f);

                        float currentSpeed = _hasPowerup ? _boostedMoveSpeed : _moveSpeed;
                        Vector3 moveDirection = transform.forward * movementVector.y;
                        transform.position += moveDirection * currentSpeed * Time.deltaTime;
                    }
                }

                if (_animator != null) _animator.SetTrigger(_walkAnimation);
            }
            else
            {
                if (_animator != null) _animator.SetTrigger(_idleAAnimation);
            }

            if(_respawnTurtle)
            {
                _respawnCounter += Time.deltaTime;
                if(_respawnCounter > _respawnTime)
                {
                    _respawnTurtle = false;
                    RespawnTurtle();
                }    
            }

            if (_hasPowerup)
            {
                _powerupCounter += Time.deltaTime;
                if(_powerupCounter >= _powerupDuration)
                {
                    _powerupCounter = 0;
                    _hasPowerup = false;                    
                }
            }

            // Keyboard shortcut (E key) to control turtle
            if (playerInside && !_isControlled && interactionCanvas != null && interactionCanvas.activeSelf)
            {
#if ENABLE_INPUT_SYSTEM
                if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.eKey.wasPressedThisFrame)
                {
                    BecomeTheTurtle();
                }
#else
                if (Input.GetKeyDown(KeyCode.E))
                {
                    BecomeTheTurtle();
                }
#endif
            }
        }

        private HardwareRig FindLocalRig()
        {
            var rigs = FindObjectsOfType<HardwareRig>();
            foreach (var r in rigs)
            {
                if (r.gameObject.activeInHierarchy)
                {
                    return r;
                }
            }
            return null;
        }

        private void BecomeTheTurtle()
        {
            if (_turtleQuest == null)
                _turtleQuest = CreateQuest();

            _turtleQuest.Start();

            var localRig = FindLocalRig();
            if (localRig != null)
            {
                _playerRig = localRig;
                _playerStartPosition = localRig.transform.position;
                _playerStartRotation = localRig.transform.rotation;

                // Move and parent player rig to follow turtle's camera target beautifully
                localRig.transform.SetParent(cameraTarget.transform);
                localRig.transform.localPosition = Vector3.zero;
                localRig.transform.localRotation = Quaternion.identity;

                // Hide player hands
                if (localRig.leftHand != null) localRig.leftHand.gameObject.SetActive(false);
                if (localRig.rightHand != null) localRig.rightHand.gameObject.SetActive(false);
            }

            _isControlled = true;

            ShowTurtleUI();

            if (interactable != null) interactable.SetActive(false);
            if (interactionCanvas != null) interactionCanvas.SetActive(false);

            if (PredatorManager.Instance != null)
            {
                PredatorManager.Instance.ClearCurrentTarget(this);
            }
        }

        private void SetTheTurtleFree(bool reachedGoal)
        {
            if(_isControlled)
                StartCoroutine(ReturnControlToPlayer());

            _isMoving = false;

            _isActive = false;
            if (turtleVisual != null) turtleVisual.SetActive(false);

            if (_rb != null) _rb.isKinematic = true;
            if (_boxCollider != null) _boxCollider.enabled = false;

            SetIsHiding(true);
            if (turtleExitCanvas != null) turtleExitCanvas.SetActive(false);

            _turtleReachedGoal = reachedGoal;

            if (_rb != null) _rb.linearVelocity = Vector3.zero;
            if (_turtleReachedGoal)
            {
                _turtleQuest.Complete();
                if (successVFX != null) successVFX.SetActive(true);

                Debug.Log("[AB] Display a success message above the water, where the player can see it");

                OnReachedGoal?.Invoke(this);

                if (PredatorManager.Instance != null)
                {
                    PredatorManager.Instance.ClearCurrentTarget(this);
                }
            }

            _respawnTurtle = true;
            _respawnTime = Random.Range(5f, 10f);
        }

        private void ShowTurtleUI()
        {
            if (turtleExitCanvas != null) turtleExitCanvas.SetActive(true);
        }

        private void ExitTurtle()
        {            
            SetTheTurtleFree(false);
        }

        public void EnableSpeedBoost()
        {
            _hasPowerup = true;
        }

        IEnumerator ReturnControlToPlayer()
        {            
            if (_playerRig != null)
            {
                _playerRig.transform.SetParent(null);
                _playerRig.transform.position = _playerStartPosition;
                _playerRig.transform.rotation = _playerStartRotation;

                // Wait a few moments
                yield return _returnControlDelay;            

                // Re-enable player hands
                if (_playerRig.leftHand != null) _playerRig.leftHand.gameObject.SetActive(true);
                if (_playerRig.rightHand != null) _playerRig.rightHand.gameObject.SetActive(true);

                _playerRig = null;
            }
            _isControlled = false;
        }

        void RespawnTurtle()
        {
            _respawnCounter = 0f;
            if (interactable != null) interactable.SetActive(true);
            if (successVFX != null) successVFX.SetActive(false);            

            transform.SetPositionAndRotation(_startPosition, _startRotation);

            if (_rb != null) _rb.isKinematic = false;
            if (_boxCollider != null) _boxCollider.enabled = true;

            _isActive = true;
            if (turtleVisual != null) turtleVisual.SetActive(true);
            _isControlled = false;

            _turtleReachedGoal = false;
            _isHiding = false;            

            if (_turtleQuest != null) _turtleQuest.Reset();
        }

        private void OnTriggerEnter(Collider other)
        {            
            if(other.TryGetComponent<Goal>(out var goal))
            {
                if (_isControlled)
                    SetTheTurtleFree(true);
                return;
            }

            if (other.gameObject.TryGetComponent<IObstacle>(out var obstacle))
                obstacle.DoCollision(this);
            else if (other.gameObject.TryGetComponent<DriftwoodObstacle>(out var woodObstacle))
            {
                SetIsHiding(true);
                if (PredatorManager.Instance != null)
                {
                    PredatorManager.Instance.ClearCurrentTarget(this);
                }
            } 
        }

        private void OnTriggerExit(Collider other)
        {
            if(other.gameObject.TryGetComponent<DriftwoodObstacle>(out var driftwoodObstacle))
            {
                SetIsHiding(false);
            }
        }

        // Delegated trigger callbacks from ChildTriggerDelegate
        private void OnInteractTriggerEnter(Collider other)
        {
            var rig = other.GetComponentInParent<HardwareRig>();
            if (rig != null && !_isControlled)
            {
                playerInside = true;
                if (interactionCanvas != null && !interactionCanvas.activeSelf)
                {
                    interactionCanvas.SetActive(true);
                }
            }
        }

        private void OnInteractTriggerExit(Collider other)
        {
            var rig = other.GetComponentInParent<HardwareRig>();
            if (rig != null)
            {
                playerInside = false;
                if (interactionCanvas != null)
                {
                    interactionCanvas.SetActive(false);
                }
            }
        }

        public int GetLocalActorNumber()
        {
            return _isControlled ? 1 : -1;
        }

        public bool IsHiding() { return _isHiding; }
        public void SetIsHiding(bool isHiding) 
        { 
            _isHiding = isHiding; 
            OnHidingStateChanged?.Invoke(this, _isHiding);
        }

        public void TurtleCaught()
        {
            SetTheTurtleFree(false);
        }

        private IQuest CreateQuest()
        {
            return new DummyQuest();
        }
    }
}