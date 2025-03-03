using UnityEngine;
using Zenject;

public class UI : MonoBehaviour
{
    public const string CUBE = "Cube";

    [Inject] private readonly HintRenderer _hintRenderer;
    [Inject] private readonly MazeSpawner _mazeSpawner;
    [Inject] private readonly AgentBackHomeAlgorithm _mazeAgentBackHome;
    [Inject] private readonly AgentAStarAlgorithm _mazeAgentAStar;
    [Inject] private readonly MazeAgent _mazeAgent;

    public void ActivateAgentBackHome()
    {
        _mazeAgent.CircleColliderDisable = false;
        _mazeAgentBackHome.ActivateAgent();
    }

    public void ActivateAgentAStar()
    {
        _mazeAgent.CircleColliderDisable = false;
        _mazeAgentAStar.ActivateAgent();
    }

    public void ActivateAgent()
    {
        _mazeAgent.enabled = true;
    }

    public void RegenerateMaze()
    {
        _mazeAgentBackHome.ResetPosition();
        _hintRenderer.PathIsDrawn = false;
        _hintRenderer.ResetLineRendererPositions();
        Destroy(GameObject.Find(CUBE)); 
        foreach (Transform child in _mazeSpawner.transform) 
        {
            Destroy(child.gameObject);
        }
        _mazeSpawner.GenerateMaze();
    }

    public void DrawPath()
    {
        if (!_hintRenderer.PathIsDrawn)
        {
            _hintRenderer.DrawPath();
        }
        else
        {
            _hintRenderer.PathIsDrawn = false;
            _hintRenderer.ResetLineRendererPositions();
            Destroy(GameObject.Find(CUBE));
        }
    }
}