using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;

public class AgentAStarAlgorithm : MonoBehaviour
{
    [SerializeField] private float _raycastDistance = 0.6f;
    [Inject] private readonly MazeSpawner _spawner;

    private readonly Vector3[] _directions = {
        Vector3.up, Vector3.down, Vector3.left, Vector3.right
    };

    private Rigidbody2D _rigidbody2D;
    private List<Vector3> _path = new List<Vector3>();
    private HashSet<Vector3> _visitedPositions = new HashSet<Vector3>();

    private Vector3 _targetPosition;

    private void Start()
    {
        _rigidbody2D = GetComponent<Rigidbody2D>();
    }

    public void ActivateAgent()
    {
        _targetPosition = new Vector3(_spawner.maze.finishPosition.x + 0.5f, _spawner.maze.finishPosition.y + 0.5f);
        StartCoroutine(FindPath());
    }

    private IEnumerator FindPath()
    {
        Dictionary<Vector3, Vector3> cameFrom = new Dictionary<Vector3, Vector3>();
        Dictionary<Vector3, float> gScore = new Dictionary<Vector3, float> { { transform.position, 0 } };
        Dictionary<Vector3, float> fScore = new Dictionary<Vector3, float> { { transform.position, Heuristic(transform.position, _targetPosition) } };
        List<Vector3> openSet = new List<Vector3> { transform.position };

        while (openSet.Count > 0)
        {
            Vector3 current = openSet.OrderBy(pos => fScore[pos]).First();

            if (current == _targetPosition)
            {
                _path = ReconstructPath(cameFrom, current);
                StartCoroutine(FollowPath());
                yield break;
            }

            openSet.Remove(current);
            _visitedPositions.Add(current);

            foreach (Vector3 dir in _directions)
            {
                Vector3 neighbor = current + dir;
                if (!IsWalkable(current, dir) || _visitedPositions.Contains(neighbor))
                {
                    continue;
                }

                float tentativeGScore = gScore[current] + 1;
                if (!gScore.ContainsKey(neighbor) || tentativeGScore < gScore[neighbor])
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeGScore;
                    fScore[neighbor] = tentativeGScore + Heuristic(neighbor, _targetPosition);

                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Add(neighbor);
                    }
                }
            }

            yield return null;
        }

        Debug.LogError("Path not found!"); //No way!
    }

    private IEnumerator FollowPath()
    {
        foreach (Vector3 step in _path)
        {
            _rigidbody2D.MovePosition(step);
            yield return new WaitForSeconds(0.1f);
        }
    }

    private List<Vector3> ReconstructPath(Dictionary<Vector3, Vector3> cameFrom, Vector3 current)
    {
        List<Vector3> path = new List<Vector3>();
        while (cameFrom.ContainsKey(current))
        {
            path.Add(current);
            current = cameFrom[current];
        }
        path.Reverse();
        return path;
    }

    private bool IsWalkable(Vector3 position, Vector3 direction)
    {
        return !Physics2D.Raycast(position, direction, _raycastDistance);
    }

    private float Heuristic(Vector3 a, Vector3 b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }
}
