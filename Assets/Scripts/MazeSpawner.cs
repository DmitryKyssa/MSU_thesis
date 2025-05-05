using UnityEngine;

public class MazeSpawner : Singleton<MazeSpawner>
{
    [SerializeField] private Cell _cellPrefab;
    [HideInInspector] public Vector3 CellSize = new Vector3(1, 1, 0);
    public Maze maze;
    [SerializeField] private int _width = 10;
    [SerializeField] private int _height = 10;

    public bool IsMazeGeneratedAtStart { get; set; }

    private void Start()
    {
        GenerateMaze();
        IsMazeGeneratedAtStart = true;
    }

    public void GenerateMaze()
    {
        MazeGenerator.Instance.Height = _height;
        MazeGenerator.Instance.Width = _width;
        maze = MazeGenerator.Instance.GenerateMaze();

        for (int x = 0; x < maze.cells.GetLength(0); x++)
        {
            for (int y = 0; y < maze.cells.GetLength(1); y++)
            {
                Cell cellObj = Instantiate(_cellPrefab, new Vector3(x * CellSize.x, y * CellSize.y, y * CellSize.z), Quaternion.identity, gameObject.transform);

                cellObj.WallLeft.SetActive(maze.cells[x, y].WallLeft);
                cellObj.WallBottom.SetActive(maze.cells[x, y].WallBottom);
            }
        }
    }
}