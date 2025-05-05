using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.InputSystem;

public class MazeAgent : Agent
{
    private Rigidbody2D _rigidbody;
    private CircleCollider2D _circleCollider;
    private readonly Vector2 _offset = new Vector2(0.5f, 0.5f);
    private readonly Vector3 _startPosition = new Vector3(0.5f, 0.5f, 0f);
    private Vector3 _targetPosition = Vector3.zero;
    private List<Vector2> _colliderPoints = new List<Vector2>();
    private int _lastCheckpointIndex = 0;
    private readonly List<Vector2> _visitedPositions = new List<Vector2>();
    private readonly List<int> _visitedHintEdges = new List<int>();
    private readonly Dictionary<Vector2, int> _positionVisitCount = new Dictionary<Vector2, int>();
    [SerializeField] private int _maxHistorySize = 100;
    [SerializeField] private int _maxVisitPenalty = 5;
    [SerializeField] private int _speed = 1;
    private int _episodes = 0;
    private int _successfulEpisodes = 0;
    private InputAction _action;
    private BehaviorParameters _parameters;
    private MazeSpawner _spawner;
    private HintRenderer _hintRenderer;

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

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.position.x);
        sensor.AddObservation(transform.position.y);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float moveX = actions.ContinuousActions[0];
        float moveY = actions.ContinuousActions[1];

        _rigidbody.MovePosition(_rigidbody.position + _speed * Time.fixedDeltaTime * new Vector2(moveX, moveY));

        if (_visitedPositions.Count >= _maxHistorySize)
        {
            _visitedPositions.RemoveAt(0);
        }
        _visitedPositions.Add(transform.position);

        if (_visitedPositions.Count(pos => pos == (Vector2)transform.position) > 1)
        {
            AddReward(-0.1f);
        }

        Vector2 currentPosition = new Vector2(transform.position.x, transform.position.y);
        if (_positionVisitCount.ContainsKey(currentPosition))
        {
            _positionVisitCount[currentPosition]++;
        }
        else
        {
            _positionVisitCount[currentPosition] = 1;
        }

        if (_positionVisitCount[currentPosition] > _maxVisitPenalty)
        {
            AddReward(-0.001f);
        }

        int currentIndex = GetDistanceFromPath();
        if (currentIndex < _lastCheckpointIndex)
        {
            AddReward(0.5f);
            _lastCheckpointIndex -= 10;
            Debug.Log("Checkpoint reached!");
        }

        if (_visitedHintEdges.Contains(currentIndex))
        {
            AddReward(-0.1f);
        }
        else
        {
            _visitedHintEdges.Add(currentIndex);
            AddReward(1f);
        }

        float distanceToExit = Vector2.Distance(transform.position, _targetPosition);
        if (distanceToExit < 0.5f)
        {
            SetReward(1.0f);
            _successfulEpisodes++;
            Debug.Log($"Reached the exit! {_successfulEpisodes}/{_episodes}");
            EndEpisode();
        }

        if (Vector2.Distance(transform.position, _startPosition) < 1f)
        {
            AddReward(-0.1f);
        }
    }

    private int GetDistanceFromPath()
    {
        Vector2 agentPosition = new Vector2(transform.position.x, transform.position.y);

        float minDistance = _colliderPoints.Count;
        int closestPointIndex = -1;

        int minIndex = Mathf.Clamp(_lastCheckpointIndex - 10, 0, _lastCheckpointIndex);
        for (int i = minIndex; i < _colliderPoints.Count; i++)
        {
            float distance = Vector2.Distance(agentPosition, _colliderPoints[i]);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestPointIndex = i;
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
            moveY = 0.5f;
        }
        else if (Input.GetKey(KeyCode.S))
        {
            moveY = -0.5f;
        }

        if (Input.GetKey(KeyCode.A))
        {
            moveX = -0.5f;
        }
        else if (Input.GetKey(KeyCode.D))
        {
            moveX = 0.5f;
        }

        continuousActions[0] = moveX;
        continuousActions[1] = moveY;
    }

    public override void OnEpisodeBegin()
    {
        _episodes++;

        _visitedPositions.Clear();
        _positionVisitCount.Clear();

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

        _colliderPoints.Clear();
        if (_hintRenderer.ComponentEdgeCollider != null)
        {
            _colliderPoints = _hintRenderer.ComponentEdgeCollider.points.ToList();
        }
        _lastCheckpointIndex = _colliderPoints.Count - 10;
    }
}