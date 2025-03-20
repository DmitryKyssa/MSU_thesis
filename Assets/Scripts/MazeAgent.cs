using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using Zenject;

public class MazeAgent : Agent
{
    [Inject] private readonly MazeSpawner _spawner;
    [Inject] private readonly HintRenderer _hintRenderer;
    private Rigidbody2D _rigidbody;
    private CircleCollider2D _circleCollider;
    private readonly Vector2 offset = new Vector2(0.5f, 0.5f);
    private List<Vector2> _colliderPoints = new List<Vector2>();
    private int _lastCheckpointIndex = 0;
    private readonly List<Vector2> _visitedPositions = new List<Vector2>();
    private readonly Dictionary<Vector2, int> _positionVisitCount = new Dictionary<Vector2, int>();
    [SerializeField] private int _maxHistorySize = 100;
    [SerializeField] private int _maxVisitPenalty = 5;
    [SerializeField] private int _speed = 1;

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
        _circleCollider.enabled = true;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.position.x);
        sensor.AddObservation(transform.position.y);
        sensor.AddObservation(_spawner.maze.finishPosition.x);
        sensor.AddObservation(_spawner.maze.finishPosition.y);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float moveX = actions.ContinuousActions[0];
        float moveY = actions.ContinuousActions[1];

        _rigidbody.MovePosition(_rigidbody.position + new Vector2(moveX, moveY) * Time.fixedDeltaTime * _speed);

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
            AddReward(-0.2f); 
        }

        int currentIndex = GetDistanceFromPath();
        if (currentIndex > _lastCheckpointIndex)
        {
            AddReward(0.1f);
            _lastCheckpointIndex = currentIndex;
            Debug.Log("Checkpoint reached!");
        }
        else if (currentIndex < _lastCheckpointIndex)
        {
            AddReward(-0.2f);
        }

        float distanceToExit = Vector2.Distance(transform.position, _spawner.maze.finishPosition + offset);
        if (distanceToExit < 0.5f)
        {
            SetReward(1.0f);
            Debug.Log("Reached the exit!");
            EndEpisode();
        }

        AddReward(-0.001f);
    }

    private int GetDistanceFromPath()
    {
        Vector2 agentPosition = new Vector2(transform.position.x, transform.position.y);

        float minDistance = _colliderPoints.Count;
        int closestPointIndex = -1;

        for (int i = 0; i < _colliderPoints.Count; i++)
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
        _visitedPositions.Clear();
        _positionVisitCount.Clear();

        _hintRenderer.PathIsDrawn = false;
        _hintRenderer.ResetLineRendererPositions();
        Destroy(GameObject.Find(UI.CUBE));
        foreach (Transform child in _spawner.transform)
        {
            Destroy(child.gameObject);
        }
        _spawner.GenerateMaze();
        _hintRenderer.DrawPath();
        transform.position = new Vector3(0.5f, 0.5f, 0f);

        _colliderPoints.Clear();
        if (_hintRenderer.ComponentEdgeCollider != null)
        {
            _colliderPoints = _hintRenderer.ComponentEdgeCollider.points.ToList();
        }
        _lastCheckpointIndex = _colliderPoints.Count;
    }
}