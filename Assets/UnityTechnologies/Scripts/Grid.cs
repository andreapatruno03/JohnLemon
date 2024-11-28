using System.Collections.Generic;
using UnityEngine;

public class Grid : MonoBehaviour
{
    public Vector2 gridWorldSize; // Dimensioni del mondo in Unity (x, y)
    public float nodeRadius; // Raggio di ogni nodo
    public LayerMask unwalkableMask; // Layer che rappresenta i muri
    public List<Node> path; // Percorso calcolato (opzionale, per debug o visualizzazione)

    private Node[,] grid; // Griglia bidimensionale di nodi
    private float nodeDiameter;
    private int gridSizeX, gridSizeY;

    void Start()
    {
        if (gridWorldSize.x <= 0 || gridWorldSize.y <= 0)
        {
            Debug.LogError("gridWorldSize deve avere valori positivi!");
            return;
        }

        if (nodeRadius <= 0)
        {
            Debug.LogError("nodeRadius deve essere maggiore di 0!");
            return;
        }

        nodeDiameter = nodeRadius * 2;
        gridSizeX = Mathf.RoundToInt(gridWorldSize.x / nodeDiameter);
        gridSizeY = Mathf.RoundToInt(gridWorldSize.y / nodeDiameter);

        Debug.Log($"Creazione della griglia con dimensioni: {gridSizeX}x{gridSizeY}");
        CreateGrid();
    }


    void CreateGrid()
    {
        grid = new Node[gridSizeX, gridSizeY];
        Vector3 worldBottomLeft = transform.position - Vector3.right * gridWorldSize.x / 2 - Vector3.forward * gridWorldSize.y / 2;

        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                // Calcola la posizione del nodo
                Vector3 worldPoint = worldBottomLeft + Vector3.right * (x * nodeDiameter + nodeRadius) +
                                     Vector3.forward * (y * nodeDiameter + nodeRadius);
                // Controlla se il nodo è camminabile
                bool walkable = !Physics.CheckSphere(worldPoint, nodeRadius, unwalkableMask);

                // Crea il nodo e aggiungilo alla griglia
                grid[x, y] = new Node(walkable, worldPoint, x, y);

                // Log per debug
                Debug.Log($"Nodo {x},{y} walkable: {walkable}");
            }
        }
        Debug.Log($"Griglia creata con dimensioni: {gridSizeX}x{gridSizeY}");

        // Log nodi camminabili
        LogWalkableNodes();
    }




    public Node NodeFromWorldPoint(Vector3 worldPosition)
    {
        float percentX = (worldPosition.x + gridWorldSize.x / 2) / gridWorldSize.x;
        float percentY = (worldPosition.z + gridWorldSize.y / 2) / gridWorldSize.y;
        percentX = Mathf.Clamp01(percentX);
        percentY = Mathf.Clamp01(percentY);

        int x = Mathf.RoundToInt((gridSizeX - 1) * percentX);
        int y = Mathf.RoundToInt((gridSizeY - 1) * percentY);

        Node node = grid[x, y];
        Debug.Log($"Nodo trovato da posizione {worldPosition}: {node.gridX}, {node.gridY} - Camminabile: {node.walkable}");
        return node;
    }



    public List<Node> GetNeighbors(Node node)
    {
        List<Node> neighbors = new List<Node>();

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0)
                    continue;

                int checkX = node.gridX + dx;
                int checkY = node.gridY + dy;

                if (checkX >= 0 && checkX < gridSizeX && checkY >= 0 && checkY < gridSizeY)
                {
                    neighbors.Add(grid[checkX, checkY]);
                }
            }
        }

        return neighbors;
    }

    void LogWalkableNodes()
    {
        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                Node node = grid[x, y]; // Accedi direttamente al nodo dalla griglia
                Debug.Log($"Nodo ({x}, {y}): Camminabile = {node.walkable}, Posizione = {node.worldPosition}");
            }
        }
    }



    public void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(transform.position, new Vector3(gridWorldSize.x, 1, gridWorldSize.y));

        if (grid != null)
        {
            foreach (Node node in grid)
            {
                Gizmos.color = node.walkable ? Color.white : Color.red;
                Gizmos.DrawCube(node.worldPosition, Vector3.one * (nodeDiameter - 0.1f));
            }
        }
    }
}