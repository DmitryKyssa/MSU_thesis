﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MazeAgentRecursivePathfinding : Singleton<MazeAgentRecursivePathfinding>
{
    [SerializeField] private float _raycastDistance = 0.6f;
    private readonly Vector2[] _directions = {
        Vector2.up, Vector2.down, Vector2.left, Vector2.right
    };

    private Vector2 _startPosition;
    private Rigidbody2D _rigidbody2D;
    private readonly Dictionary<Vector2, bool> _movingVariants = new Dictionary<Vector2, bool>();
    private readonly Stack<Vector2> _path = new Stack<Vector2>();
    private readonly HashSet<Vector2> _visitedPositions = new HashSet<Vector2>(); 
    private Vector2 _lastDirection = Vector2.zero;

    private void Start()
    {
        _startPosition = transform.position;
        _rigidbody2D = GetComponent<Rigidbody2D>();

        foreach (var dir in _directions)
        {
            _movingVariants[dir] = false;
        }
    }

    public void ActivateAgent()
    {
        StartCoroutine(StartAdventure());
    }

    public void ResetPosition()
    {
        transform.position = new Vector3(0.5f, 0.5f, 0f);
        _path.Clear();
        _visitedPositions.Clear();
        _lastDirection = Vector3.zero;
    }

    private IEnumerator StartAdventure()
    {
        WaitForSeconds waitTime = new(.01f);
        Vector3 castedVector = new(MazeSpawner.Instance.maze.finishPosition.x + 0.5f, MazeSpawner.Instance.maze.finishPosition.y + 0.5f, 0f);
        while (transform.position != castedVector)
        {
            CheckDirections();
            yield return waitTime;
        }
    }

    private void CheckDirections()
    {
        foreach (var dir in _directions)
        {
            _movingVariants[dir] = false;
        }

        _path.Push(transform.position);
        _visitedPositions.Add(transform.position);

        foreach (Vector3 dir in _directions)
        {
            if (!Physics2D.Raycast(transform.position, dir, _raycastDistance))
            {
                Debug.DrawRay(transform.position, dir * _raycastDistance, Color.green, 1f);
                _movingVariants[dir] = true;
            }
            else
            {
                Debug.DrawRay(transform.position, dir * _raycastDistance, Color.red, 1f);
            }
        }

        List<Vector2> possibleMoves = _movingVariants
            .Where(kvp => kvp.Value)
            .Select(kvp => kvp.Key)
            .Where(dir => !_visitedPositions.Contains((Vector2)transform.position + dir) && dir != -_lastDirection)
            .ToList();

        if (possibleMoves.Count > 0)
        {
            _lastDirection = possibleMoves[Random.Range(0, possibleMoves.Count)];
            MoveInDirection(_lastDirection);
        }
        else if (_path.Count > 1)
        {
            _path.Pop();
            Vector2 lastPosition = _path.Peek();

            if (lastPosition == _startPosition)
            {
                _path.Clear();
                _visitedPositions.Clear();
                _lastDirection = Vector3.zero;
                return;
            }

            MoveInDirection((Vector3)lastPosition - transform.position);
            _lastDirection = (Vector3)lastPosition - transform.position;
        }
    }

    private void MoveInDirection(Vector2 direction)
    {
        _rigidbody2D.MovePosition(_rigidbody2D.position + direction);
    }
}