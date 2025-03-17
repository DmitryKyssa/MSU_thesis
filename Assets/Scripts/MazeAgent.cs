using System.Collections.Generic;
using System.Linq;
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
    private readonly Vector2[] _intermediateRewards = new Vector2[10];
    private int _nextRewardIndex;
    private Vector2 offset = new Vector2(0.5f, 0.5f);
    private List<Vector2> _colliderPoints = new List<Vector2>();
    private int _lastCheckpointIndex = 0;

    public bool CircleColliderDisable
    {
        set => _circleCollider.enabled = value;
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
        int moveAction = actions.DiscreteActions[0];

        Vector3 move = Vector3.zero;
        switch (moveAction)
        {
            case 0:
                move = Vector3.up;
                break;
            case 1:
                move = Vector3.down;
                break;
            case 2:
                move = Vector3.left;
                break;
            case 3:
                move = Vector3.right;
                break;
        }

        _rigidbody.MovePosition(_rigidbody.position + (Vector2)move);

        int currentIndex = GetDistanceFromPath();

        if (currentIndex > _lastCheckpointIndex)
        {
            AddReward(0.1f);
            _lastCheckpointIndex = currentIndex;
        }
        else if (currentIndex < _lastCheckpointIndex)
        {
            AddReward(-0.2f);
        }

        float distanceToPath = GetDistanceToNearestPathPoint();
        if (distanceToPath > 0.5f)
        {
            AddReward(-0.05f);
        }

        if (distanceToPath > 1.0f)
        {
            AddReward(-0.5f);
        }

        if (distanceToPath < 0.2f)
        {
            AddReward(0.2f);
        }

        if (_nextRewardIndex >= 0)
        {
            float distanceToNextReward = Vector2.Distance(transform.position, _intermediateRewards[_nextRewardIndex]);

            if (distanceToNextReward < 0.2f)
            {
                AddReward(100f);
                _nextRewardIndex--;
                Debug.Log($"Reached checkpoint {_nextRewardIndex}, reward given!");
            }
        }

        if (currentIndex == 0)
        {
            AddReward(1000f);
            Debug.Log("<color=green>Agent has found exit!</color>");
            EndEpisode();
        }

        if (Physics2D.OverlapCircle(transform.position, 0.2f))
        {
            AddReward(-1f);
        }
    }

    private float GetDistanceToNearestPathPoint()
    {
        Vector2 agentPosition = new Vector2(transform.position.x, transform.position.y);
        float minDistance = float.MaxValue;

        Vector2 tmp = Vector2.zero;
        foreach (var point in _colliderPoints)
        {
            float distance = Vector2.Distance(agentPosition, point);
            if (distance < minDistance)
            {
                tmp = point;
                minDistance = distance;
            }
        }

        return minDistance;
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

    public override void OnEpisodeBegin()
    {
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
            int stepSize = Mathf.Max(1, _colliderPoints.Count / _intermediateRewards.Length);

            for (int i = 0; i < _intermediateRewards.Length; i++)
            {
                int index = Mathf.Min(i * stepSize, _colliderPoints.Count - 1);
                _intermediateRewards[i] = new Vector2()
                {
                    x = _colliderPoints[index].x + offset.x,
                    y = _colliderPoints[index].y + offset.y
                };
            }
            _nextRewardIndex = _intermediateRewards.Length - 1;
        }
        _lastCheckpointIndex = _colliderPoints.Count;
    }
}