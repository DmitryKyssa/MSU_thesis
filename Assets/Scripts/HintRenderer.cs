using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class HintRenderer : MonoBehaviour
{
    [Inject] private readonly MazeSpawner _mazeSpawner;
    public bool PathIsDrawn { get; set; } = false;
    [SerializeField] private LineRenderer _componentLineRenderer;

    public void ResetLineRendererPositions()
    {
        _componentLineRenderer.positionCount = 0;
    }

    public void DrawPath() 
    {
        Maze maze = _mazeSpawner.maze; 
        int x = maze.finishPosition.x; 
        int y = maze.finishPosition.y; 
        List<Vector3> positions = new List<Vector3>();

        GameObject finish = GameObject.CreatePrimitive(PrimitiveType.Cube);
        finish.transform.localScale = _mazeSpawner.CellSize * 0.5f; 
        finish.transform.position = new Vector3(
            x * _mazeSpawner.CellSize.x, 
            y * _mazeSpawner.CellSize.y, 
            y * _mazeSpawner.CellSize.z) + _mazeSpawner.CellSize / 2; 

        while ((x != 0 || y != 0) && positions.Count < 1000)
        {
            positions.Add(new Vector3(x * _mazeSpawner.CellSize.x, y * _mazeSpawner.CellSize.y, y * _mazeSpawner.CellSize.z));

            MazeGeneratorCell currentCell = maze.cells[x, y];

            if (x > 0 &&
                !currentCell.WallLeft &&
                maze.cells[x - 1, y].DistanceFromStart < currentCell.DistanceFromStart)
            {
                x--;
            }
            else if (y > 0 &&
                !currentCell.WallBottom &&
                maze.cells[x, y - 1].DistanceFromStart < currentCell.DistanceFromStart)
            {
                y--;
            }
            else if (x < maze.cells.GetLength(0) - 2 &&
                !maze.cells[x + 1, y].WallLeft &&
                maze.cells[x + 1, y].DistanceFromStart < currentCell.DistanceFromStart)
            {
                x++;
            }
            else if (y < maze.cells.GetLength(1) - 2 &&
                !maze.cells[x, y + 1].WallBottom &&
                maze.cells[x, y + 1].DistanceFromStart < currentCell.DistanceFromStart)
            {
                y++;
            }
        }

        positions.Add(Vector3.zero);
        _componentLineRenderer.positionCount = positions.Count;
        _componentLineRenderer.SetPositions(positions.ToArray());

        PathIsDrawn = true; 
    }
}