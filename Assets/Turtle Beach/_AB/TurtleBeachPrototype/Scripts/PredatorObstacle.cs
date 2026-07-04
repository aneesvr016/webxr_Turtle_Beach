using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using TMPro;

namespace AB.TurtleBeach
{
    public enum PredatorState
    {
        Idle = 0,
        Patrolling = 1,
        Chasing = 2
    }

    public enum PredatorType
    {
        None,
        Land,
        Bird
    }

    public class PredatorObstacle : MonoBehaviour, IObstacle 
    {
        [SerializeField] Collider turtleDetector;
        [SerializeField] Collider turtleToucher;
        [SerializeField] Terrain terrain;
        [SerializeField] BoxCollider spawnVolume;
        [SerializeField] PredatorType predatorType;
        [SerializeField] float chaseSpeedBonus = .1f;
        [SerializeField] TextMeshPro debugLabel;
        NavMeshAgent _navAgent;
        int _predatorState = -1;
        Vector3 _wayPoint;
        TurtleController _currentTarget;

        float _idleTime = 5f;
        float _idleMin = 2f;
        float _idleMax = 5f;
        float _idleCounter = 0f;
        float _initialSpeed;
        bool _turtleCaught = false;

        float _lastDestinationUpdate = 0f;
        float _destinationUpdateRate = 0.1f; // Update every 0.1 seconds
        float _minDistanceToUpdate = 0.9f;

        Animator _animator;
        readonly string _walkAnimation = "Walk";
        readonly string _idleAAnimation = "Idle";
        readonly string _runAnimation = "Run";
        readonly string _flyAnimation = "Fly";

        private void Awake()
        {
            _navAgent = GetComponent<NavMeshAgent>();
            if (_navAgent != null)
            {
                _navAgent.updateRotation = true;
                _initialSpeed = _navAgent.speed;
            }
            _animator = GetComponent<Animator>();
            if (turtleDetector != null) turtleDetector.enabled = true;
            if (turtleToucher != null) turtleToucher.enabled = false;

            if (debugLabel != null) debugLabel.enabled = false;
        }

        private void OnEnable()
        {
            TurtleController.OnReachedGoal += HandleTurtleReachedGoal;
            TurtleController.OnHidingStateChanged += HandleTurtleHidingStateChanged;
            SetPredatorState(PredatorState.Idle);
        }

        private void OnDisable()
        {
            TurtleController.OnReachedGoal -= HandleTurtleReachedGoal;
            TurtleController.OnHidingStateChanged -= HandleTurtleHidingStateChanged;
        }

        private void HandleTurtleReachedGoal(TurtleController turtle)
        {
            if (_currentTarget == turtle)
            {
                _currentTarget = null;
                SetPredatorState(PredatorState.Idle);
            }
        }

        private void HandleTurtleHidingStateChanged(TurtleController turtle, bool isHiding)
        {
            if (_currentTarget == turtle && isHiding)
            {
                _turtleCaught = false;
                _currentTarget = null;
                if (_navAgent != null) _navAgent.isStopped = true;
                SetPredatorState(PredatorState.Idle);
            }
        }

        public void SetPredatorState(PredatorState pState)
        {
            _predatorState = (int)pState;
            OnPredatorStateChanged();
        }

        private void OnPredatorStateChanged()
        {
            if (_navAgent == null) return;

            switch(_predatorState)
            {
                case 0: //PredatorState.Idle:  
                    _idleTime = Random.Range(_idleMin, _idleMax);                    
                    _wayPoint = GetNewWaypoint();
                    FaceWaypoint();
                    _navAgent.isStopped = false;
                    if (turtleDetector != null) turtleDetector.enabled = true;
                    if (turtleToucher != null) turtleToucher.enabled = false;
                    break;
                case 1: //PredatorState.Patrolling:                    
                    _idleCounter = 0f;
                    _navAgent.speed = _initialSpeed;                    

                    if (_animator != null) _animator.SetTrigger(_walkAnimation);
                    _navAgent.SetDestination(_wayPoint);
                    break;
                case 2:  //PredatorState.Chasing:
                    FaceWaypoint();
                    _navAgent.speed += chaseSpeedBonus;                                        
                    if (turtleDetector != null) turtleDetector.enabled = false;
                    if (turtleToucher != null) turtleToucher.enabled = true;
                    break;
                default:
                    break;
            }
        }

        private void Update()
        {            
            if (_navAgent == null) return;

            switch (_predatorState)
            {                    
                case 1:  //PredatorState.Patrolling:
                    if (_animator != null) _animator.SetTrigger(_walkAnimation);
                    if(_navAgent.remainingDistance < _navAgent.stoppingDistance)
                        SetPredatorState(PredatorState.Idle);

                    break;
                case 2:  //PredatorState.Chasing:
                    if (_currentTarget == null)
                    {
                        SetPredatorState(PredatorState.Idle);
                        break;
                    }

                    if (Time.time >= _lastDestinationUpdate + _destinationUpdateRate)
                    {
                        Vector3 targetNavMeshPoint = GetClosestPointOnNavMesh(_currentTarget.transform.position);
                        float distanceToCurrentDestination = Vector3.Distance(_navAgent.destination, targetNavMeshPoint);                       

                        if (distanceToCurrentDestination > _minDistanceToUpdate)
                        {                            
                            NavMeshPath testPath = new NavMeshPath();
                            if (_navAgent.CalculatePath(targetNavMeshPoint, testPath))
                            {
                                if (testPath.status == NavMeshPathStatus.PathComplete ||
                                    testPath.status == NavMeshPathStatus.PathPartial)
                                {
                                    _navAgent.SetPath(testPath);
                                    _lastDestinationUpdate = Time.time;
                                }
                            }
                        }
                    }

                    if (_animator != null)
                    {
                        if (predatorType == PredatorType.Bird)
                            _animator.SetTrigger(_flyAnimation);
                        else
                            _animator.SetTrigger(_runAnimation);
                    }

                    break;
                case 0:  //PredatorState.Idle:
                    _idleCounter += Time.deltaTime;
                    if (_animator != null) _animator.SetTrigger(_idleAAnimation);
                    if (_idleCounter > _idleTime)
                    {
                        _idleCounter = 0f;
                        SetPredatorState(PredatorState.Patrolling);
                    }
                    break;
                default:
                    break;
            }           
        }

        private void OnTriggerEnter(Collider other)
        {               
            if (turtleDetector != null && turtleDetector.enabled)
            {   
                if (other.gameObject.TryGetComponent<TurtleController>(out var turtle))
                {                    
                    if(spawnVolume != null && spawnVolume.bounds.Contains(turtle.transform.position))
                    {
                        if (turtle.IsHiding()) return;

                        _navAgent.isStopped = true;
                        _navAgent.ResetPath();
                        _currentTarget = turtle;
                        if (turtleDetector != null) turtleDetector.enabled = false;
                        if (turtleToucher != null) turtleToucher.enabled = true;
                        SetPredatorState(PredatorState.Chasing);
                    }
                }
            }
            else if (turtleToucher != null && turtleToucher.enabled)
            {
                if (other.gameObject.TryGetComponent<TurtleController>(out var turtle))
                {                    
                    if (turtle.IsHiding()) return;

                    _turtleCaught = true;
                    if (turtleDetector != null) turtleDetector.enabled = true;
                    if (turtleToucher != null) turtleToucher.enabled = false;
                }
            }
        }

        #region IObstacle implementation

        public void DoCollision(TurtleController controller)
        {            
            if(_turtleCaught)
            {                
                _turtleCaught = false;
                controller.TurtleCaught();
                if (PredatorManager.Instance != null)
                {
                    PredatorManager.Instance.ClearCurrentTarget(controller);
                }
                SetPredatorState(PredatorState.Idle);
            }
        }

        #endregion

        Vector3 GetNewWaypoint()
        {
            var newPoint = new Vector3();

            if (spawnVolume == null) return transform.position;

            Bounds bounds = spawnVolume.bounds;
            newPoint.x = Random.Range(bounds.min.x, bounds.max.x);
            newPoint.y = Random.Range(bounds.min.y, bounds.max.y);
            newPoint.z = Random.Range(bounds.min.z, bounds.max.z);

            if (terrain != null)
            {
                newPoint.y = terrain.SampleHeight(newPoint);
            }

            return newPoint;
        }

        public TurtleController GetCurrentTarget()
        {
            return _currentTarget;
        }

        public void ClearCurrentTarget() { _currentTarget = null; }

        void FaceWaypoint()
        {
            if (_navAgent == null) return;

            var turnTowardNavSteeringTarget = _predatorState == (int)PredatorState.Chasing ? 
                (_currentTarget != null ? _currentTarget.transform.position : transform.position) : 
                _navAgent.steeringTarget;

            Vector3 direction = (turnTowardNavSteeringTarget - transform.position).normalized;
            if (direction != Vector3.zero)
            {
                Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
                transform.rotation = _predatorState == (int)PredatorState.Chasing ? 
                    lookRotation : 
                    Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 2);
            }
        }

        Vector3 GetClosestPointOnNavMesh(Vector3 position, float maxDistance = 10f)
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(position, out hit, maxDistance, NavMesh.AllAreas))
            {
                return hit.position;
            }
            return position;
        }
    }
}