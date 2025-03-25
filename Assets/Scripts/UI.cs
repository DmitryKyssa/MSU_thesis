using UnityEngine;
using Zenject;

public class UI : MonoBehaviour
{
    [Inject] private readonly HintRenderer _hintRenderer;
    [Inject] private readonly MazeSpawner _mazeSpawner;
    [Inject] private readonly AgentBackHomeAlgorithm _mazeAgentBackHome;
    [Inject] private readonly AgentAStarAlgorithm _mazeAgentAStar;
    [Inject] private readonly MazeAgent _mazeAgent;

    public void ActivateAgentBackHome()
    {
        _mazeAgent.SetCircleColliderEnableStatus(false);
        _hintRenderer.ComponentEdgeCollider.enabled = false;
        _mazeAgentBackHome.ActivateAgent();
    }

    public void ActivateAgentAStar()
    {
        _mazeAgent.SetCircleColliderEnableStatus(false);
        _hintRenderer.ComponentEdgeCollider.enabled = false;
        _mazeAgentAStar.ActivateAgent();
    }

    public void ActivateAgent()
    {
        _hintRenderer.ComponentEdgeCollider.enabled = true;
        _mazeAgent.enabled = true;
    }

    public void RegenerateMaze()
    {
        _mazeAgentBackHome.ResetPosition();
        _hintRenderer.PathIsDrawn = false;
        _hintRenderer.ResetLineRendererPositions();
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
        }
    }
}