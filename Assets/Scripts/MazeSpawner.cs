using UnityEngine;
using Zenject;

public class MazeSpawner : MonoBehaviour
{
    [Inject(Id = GameInstaller.CELL_PREFAB_ID)] private readonly GameObject _cellPrefab;
    [HideInInspector] public Vector3 CellSize = new Vector3(1, 1, 0);
    public Maze maze;
    [SerializeField] private int _width = 10;
    [SerializeField] private int _height = 10;
    [Inject] private readonly MazeGenerator _generator;

    public int Left => 0;
    public int Right => _width - 1;
    public int Top => _height - 1;
    public int Bottom => 0;

    private void Start()
    {
        GenerateMaze();
    }

    public void GenerateMaze()
    {
        _generator.Height = _height;
        _generator.Width = _width;
        maze = _generator.GenerateMaze();

        for (int x = 0; x < maze.cells.GetLength(0); x++)
        {
            for (int y = 0; y < maze.cells.GetLength(1); y++)
            {
                GameObject cellObj = Instantiate(_cellPrefab, new Vector3(x * CellSize.x, y * CellSize.y, y * CellSize.z), Quaternion.identity, gameObject.transform);
                Cell cell = cellObj.GetComponent<Cell>();

                cell.WallLeft.SetActive(maze.cells[x, y].WallLeft);
                cell.WallBottom.SetActive(maze.cells[x, y].WallBottom);
            }
        }
    }
}