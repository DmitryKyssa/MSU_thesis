using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using Zenject;

public class MazeAgent : Agent
{
    [SerializeField] private float _moveSpeed = 0.5f;
    [SerializeField] private int _wallLayerMask;
    [Inject] private readonly UI _ui;
    [Inject] private readonly MazeSpawner _spawner;

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.position.x);
        sensor.AddObservation(transform.position.y);
        sensor.AddObservation(_spawner.maze.finishPosition.x);
        sensor.AddObservation(_spawner.maze.finishPosition.y);

        sensor.AddObservation(CheckWall(Vector2.up));
        sensor.AddObservation(CheckWall(Vector2.down));
        sensor.AddObservation(CheckWall(Vector2.left));
        sensor.AddObservation(CheckWall(Vector2.right));
        Debug.Log(sensor.ObservationSize());
    }

    private float CheckWall(Vector2 direction)
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, 1f);
        Debug.DrawRay(transform.position, direction, Color.blue);
        return hit.collider != null ? 1f : 0f; 
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        Debug.Log(actions.ContinuousActions.Array);
        float moveX = actions.ContinuousActions[0];
        float moveY = actions.ContinuousActions[1];
        Debug.Log(moveY + " " + moveX);

        Vector3 move = _moveSpeed * Time.deltaTime * new Vector3(moveX, moveY, 0);
        transform.position += move;
        Debug.Log(transform.position);

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
        Debug.Log("Episod begin");
        _ui.RegenerateMaze();
        transform.position = new Vector3(0.5f, 0.5f, 0f);
    }
}