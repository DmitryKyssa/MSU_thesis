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
    private List<Vector2> _hintRendererPoints = new List<Vector2>();
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

        sensor.AddObservation(_targetPosition.x);
        sensor.AddObservation(_targetPosition.y);

        Vector2 directionToTarget = (_targetPosition - transform.position).normalized;
        sensor.AddObservation(directionToTarget.x);
        sensor.AddObservation(directionToTarget.y);

        sensor.AddObservation(GetDistanceFromPath());
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float moveX = actions.ContinuousActions[0];
        float moveY = actions.ContinuousActions[1];

        Vector2 movement = new Vector2(moveX, moveY).normalized;
        _rigidbody.MovePosition(_rigidbody.position + _speed * Time.fixedDeltaTime * movement);

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

        int currentIndex = GetDistanceFromPath();
        if (currentIndex < _lastCheckpointIndex)
        {
            AddReward((_hintRendererPoints.Count - currentIndex) * 0.01f);
            _lastCheckpointIndex -= 10;
        }

        if (!_visitedHintEdges.Contains(currentIndex))
        {
            _visitedHintEdges.Add(currentIndex);
            AddReward(0.1f);
        }

        if (Vector2.Distance(transform.position, _startPosition) < 1f)
        {
            AddReward(-0.05f);
        }

        float currentDistanceToTarget = Vector2.Distance(transform.position, _targetPosition);
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

        float minDistance = _hintRendererPoints.Count;
        int closestPointIndex = -1;

        int minIndex = Mathf.Clamp(_lastCheckpointIndex - 10, 0, _lastCheckpointIndex);
        for (int i = minIndex; i < _hintRendererPoints.Count; i++)
        {
            float distance = Vector2.Distance(agentPosition, _hintRendererPoints[i]);
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

        _hintRendererPoints.Clear();
        _hintRendererPoints = _hintRenderer.ComponentEdgeCollider.points.ToList();

        _lastCheckpointIndex = _hintRendererPoints.Count - 10;
    }
}