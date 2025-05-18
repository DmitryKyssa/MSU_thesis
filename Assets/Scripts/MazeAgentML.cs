using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.InputSystem;

public class MazeAgentML : Agent
{
    private Rigidbody2D _rigidbody;
    private CircleCollider2D _circleCollider;
    private readonly Vector2 _offset = new Vector2(0.5f, 0.5f);
    private readonly Vector3 _startPosition = new Vector3(0.5f, 0.5f, 0f);
    private Vector3 _targetPosition = Vector3.zero;
    private Vector3 _previousPosition;
    private float _previousDistanceToTarget;
    private List<Vector2> _hintRendererPath = new List<Vector2>();
    private int _lastCheckpointIndex = 0;
    private readonly List<Vector2> _visitedPositions = new List<Vector2>();
    private readonly List<int> _visitedHintEdges = new List<int>();
    private readonly Dictionary<Vector2, int> _positionVisitCount = new Dictionary<Vector2, int>();
    [SerializeField] private int _maxHistorySize = 100;
    [SerializeField] private int _maxVisitPenalty = 3;
    [SerializeField] private int _speed = 1;
    private int _episodes = 0;
    private int _successfulEpisodes = 0;
    private InputAction _action;
    private BehaviorParameters _parameters;
    private MazeSpawner _spawner;
    private HintRenderer _hintRenderer;
    [SerializeField] private float _raycastDistance = 0.6f;
    private readonly Vector2[] _raycastDirections = {
        Vector2.up, Vector2.down, Vector2.left, Vector2.right
    };
    private bool[] _wallDetected;
    private readonly float _raycastDuration = 3f;
    private readonly int _checkpointStep = 5;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetCircleColliderEnableStatus(bool status)
    {
        _circleCollider.enabled = status;
    }

    protected override void Awake()
    {
        base.Awake();
        _rigidbody = GetComponent<Rigidbody2D>();
        _circleCollider = GetComponent<CircleCollider2D>();
        _parameters = GetComponent<BehaviorParameters>();
        _circleCollider.enabled = true;

        _spawner = MazeSpawner.Instance;
        _hintRenderer = HintRenderer.Instance;

        _wallDetected = new bool[_raycastDirections.Length];
    }

    protected override void OnEnable()
    {
        _action = new InputAction(nameof(ChangeBehaviourType), InputActionType.Button, "<Keyboard>/space");
        _action.performed += _ => ChangeBehaviourType();
        _action.Enable();

        _circleCollider.enabled = true;
        base.OnEnable();
    }

    private void ChangeBehaviourType()
    {
        _parameters.BehaviorType = _parameters.BehaviorType == BehaviorType.Default
            ? BehaviorType.HeuristicOnly : BehaviorType.Default;
    }

    protected override void OnDisable()
    {
        _action.performed -= _ => ChangeBehaviourType();
        _action.Disable();

        _circleCollider.enabled = false;
        base.OnDisable();
    }

    private void DetectWalls()
    {
        for (int i = 0; i < _raycastDirections.Length; i++)
        {
            RaycastHit2D hit = Physics2D.Raycast(transform.position, _raycastDirections[i], _raycastDistance);
            _wallDetected[i] = hit.collider != null;

            Color rayColor = _wallDetected[i] ? Color.red : Color.green;
            Debug.DrawRay(transform.position, _raycastDirections[i] * _raycastDistance, rayColor, _raycastDuration);
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        DetectWalls();

        sensor.AddObservation(transform.position.x);
        sensor.AddObservation(transform.position.y);

        sensor.AddObservation(_targetPosition.x);
        sensor.AddObservation(_targetPosition.y);

        sensor.AddObservation(Vector2.Distance(transform.position, _targetPosition));

        Vector2 directionToTarget = (_targetPosition - transform.position).normalized;
        sensor.AddObservation(directionToTarget.x);
        sensor.AddObservation(directionToTarget.y);

        foreach (bool wallPresent in _wallDetected)
        {
            sensor.AddObservation(wallPresent ? 1.0f : 0.0f);
        }

        sensor.AddObservation(GetDistanceFromPath());
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float moveX = actions.ContinuousActions[0];
        float moveY = actions.ContinuousActions[1];

        Vector2 movement = new Vector2(moveX, moveY).normalized;
        _rigidbody.MovePosition(_rigidbody.position + _speed * Time.fixedDeltaTime * movement);

        Vector2 moveDirection = movement.normalized;
        for (int i = 0; i < _raycastDirections.Length; i++)
        {
            if (_wallDetected[i] && Vector2.Dot(moveDirection, _raycastDirections[i]) > 0.8f)
            {
                AddReward(-0.01f);
                break;
            }
        }

        Vector2 roundedPosition = new Vector2(
            Mathf.Round(transform.position.x),
            Mathf.Round(transform.position.y)
        );

        if (_visitedPositions.Count >= _maxHistorySize)
        {
            _visitedPositions.RemoveAt(0);
        }
        _visitedPositions.Add(roundedPosition);

        if (_positionVisitCount.ContainsKey(roundedPosition))
        {
            _positionVisitCount[roundedPosition]++;

            if (_positionVisitCount[roundedPosition] > _maxVisitPenalty)
            {
                AddReward(-0.01f);
            }
        }
        else
        {
            _positionVisitCount[roundedPosition] = 1;
            AddReward(0.005f);
        }

        float currentDistanceToTarget = Vector2.Distance(transform.position, _targetPosition);
        float distanceReward = _previousDistanceToTarget - currentDistanceToTarget;
        AddReward(distanceReward * 0.05f);
        _previousDistanceToTarget = currentDistanceToTarget;

        int currentIndex = GetDistanceFromPath();
        if (currentIndex < _lastCheckpointIndex)
        {
            AddReward((_hintRendererPath.Count - currentIndex) * 0.05f);
            _lastCheckpointIndex -= _checkpointStep;
        }

        if (!_visitedHintEdges.Contains(currentIndex))
        {
            _visitedHintEdges.Add(currentIndex);
            AddReward(0.2f);
        }

        if (Vector2.Distance(transform.position, _startPosition) < 0.5f)
        {
            AddReward(-0.01f);
        }

        if (currentDistanceToTarget < 0.5f)
        {
            SetReward(10.0f);
            _successfulEpisodes++;
            Debug.Log($"Reached the exit! {_successfulEpisodes}/{_episodes}");
            EndEpisode();
        }
    }

    private int GetDistanceFromPath()
    {
        Vector2 agentPosition = new Vector2(transform.position.x, transform.position.y);
        int closestPointIndex = -1;
        int minIndex = Mathf.Clamp(_lastCheckpointIndex - _checkpointStep, 0, _hintRendererPath.Count);
        for (int i = minIndex; i < _hintRendererPath.Count; i++)
        {
            float distance = Vector2.Distance(agentPosition, _hintRendererPath[i]);
            if (distance < 1f)
            {
                closestPointIndex = i;
                break;
            }
        }

        return closestPointIndex;
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<float> continuousActions = actionsOut.ContinuousActions;

        float moveX = 0f;
        float moveY = 0f;

        if (Input.GetKey(KeyCode.W))
        {
            moveY = 1f;
        }
        else if (Input.GetKey(KeyCode.S))
        {
            moveY = -1f;
        }

        if (Input.GetKey(KeyCode.A))
        {
            moveX = -1f;
        }
        else if (Input.GetKey(KeyCode.D))
        {
            moveX = 1f;
        }

        continuousActions[0] = moveX;
        continuousActions[1] = moveY;
    }

    public override void OnEpisodeBegin()
    {
        _episodes++;

        _visitedPositions.Clear();
        _positionVisitCount.Clear();
        _visitedHintEdges.Clear();

        if (!_spawner.IsMazeGeneratedAtStart)
        {
            _hintRenderer.PathIsDrawn = false;
            _hintRenderer.ResetLineRendererPositions();
            foreach (Transform child in _spawner.transform)
            {
                Destroy(child.gameObject);
            }
            _spawner.GenerateMaze();
        }

        _targetPosition = new Vector3(_spawner.maze.finishPosition.x + _offset.x, _spawner.maze.finishPosition.y + _offset.y, 0f);

        _spawner.IsMazeGeneratedAtStart = false;
        _hintRenderer.DrawPath();
        transform.position = _startPosition;
        _previousPosition = _startPosition;
        _previousDistanceToTarget = Vector2.Distance(_startPosition, _targetPosition);

        _hintRendererPath.Clear();
        if (_hintRenderer.ComponentEdgeCollider != null)
        {
            _hintRendererPath = _hintRenderer.ComponentEdgeCollider.points.ToList();
        }

        _lastCheckpointIndex = _hintRendererPath.Count - _checkpointStep;
    }
}