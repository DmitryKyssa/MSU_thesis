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

        sensor.AddObservation(CheckWall(Vector3.up));
        sensor.AddObservation(CheckWall(Vector3.down));
        sensor.AddObservation(CheckWall(Vector3.left));
        sensor.AddObservation(CheckWall(Vector3.right));
    }

    private float CheckWall(Vector2 direction)
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, 0.5f);
        Debug.DrawRay(transform.position, direction, Color.blue, 3f);
        return hit.collider != null ? 1f : 0f;
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

        float distanceToExit = Vector3.Distance(transform.position,
            new Vector3(_spawner.maze.finishPosition.x + 0.5f, _spawner.maze.finishPosition.y + 0.5f));

        AddReward(-distanceToExit * 0.01f);

        if (distanceToExit < 0.5f)
        {
            SetReward(1f);
            EndEpisode();
        }

        if (Physics2D.OverlapCircle(transform.position, 0.2f))
        {
            AddReward(-0.1f);
        }
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
        Debug.Log($"Completed episodes: {CompletedEpisodes}, reward: {GetCumulativeReward()}");
    }
}