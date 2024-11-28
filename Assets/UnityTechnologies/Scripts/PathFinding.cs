using System.Collections.Generic;
using UnityEngine;

public class Pathfinding : MonoBehaviour
{
    public Transform seeker, target;
    private Grid grid;
    public List<Node> path;

    void Start()
    {
        grid = GetComponent<Grid>();
    }

    void Update()
    {
        // Calcola il percorso solo se il seeker e il target sono assegnati
        if (seeker != null && target != null)
        {
            FindPath(seeker.position, target.position);
        }
    }

    public void FindPath(Vector3 startPos, Vector3 targetPos)
    {
        Node startNode = grid.NodeFromWorldPoint(startPos);
        Node targetNode = grid.NodeFromWorldPoint(targetPos);

        if (!startNode.walkable)
        {
            Debug.LogWarning($"Nodo iniziale non camminabile: {startNode.gridX}, {startNode.gridY}. Cercando il nodo camminabile più vicino...");
            startNode = FindClosestWalkableNode(startNode);
            if (startNode == null)
            {
                Debug.LogError("Nessun nodo camminabile trovato vicino al nodo iniziale.");
                return;
            }
        }

        if (!targetNode.walkable)
        {
            Debug.LogWarning($"Nodo finale non camminabile: {targetNode.gridX}, {targetNode.gridY}. Cercando il nodo camminabile più vicino...");
            targetNode = FindClosestWalkableNode(targetNode);
            if (targetNode == null)
            {
                Debug.LogError("Nessun nodo camminabile trovato vicino al nodo finale.");
                return;
            }
        }

        List<Node> openSet = new List<Node>();
        HashSet<Node> closedSet = new HashSet<Node>();
        openSet.Add(startNode);

        while (openSet.Count > 0)
        {
            Node currentNode = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].fCost < currentNode.fCost ||
                    (openSet[i].fCost == currentNode.fCost && openSet[i].hCost < currentNode.hCost))
                {
                    currentNode = openSet[i];
                }
            }

            openSet.Remove(currentNode);
            closedSet.Add(currentNode);

            if (currentNode == targetNode)
            {
                RetracePath(startNode, targetNode);
                return;
            }

            foreach (Node neighbor in grid.GetNeighbors(currentNode))
            {
                if (!neighbor.walkable || closedSet.Contains(neighbor))
                    continue;

                int newCostToNeighbor = currentNode.gCost + GetDistance(currentNode, neighbor);
                if (newCostToNeighbor < neighbor.gCost || !openSet.Contains(neighbor))
                {
                    neighbor.gCost = newCostToNeighbor;
                    neighbor.hCost = GetDistance(neighbor, targetNode);
                    neighbor.parent = currentNode;

                    if (!openSet.Contains(neighbor))
                        openSet.Add(neighbor);
                }
            }
        }

        Debug.LogWarning("Percorso non trovato.");
        if (startNode == null || targetNode == null)
        {
            Debug.LogWarning("Nodo iniziale o finale nullo!");
            return;
        }

        if (!startNode.walkable)
        {
            Debug.LogWarning($"Nodo iniziale ({startNode.gridX}, {startNode.gridY}) non camminabile.");
            return;
        }

        if (!targetNode.walkable)
        {
            Debug.LogWarning($"Nodo finale ({targetNode.gridX}, {targetNode.gridY}) non camminabile.");
            return;
        }

    }

    void RetracePath(Node startNode, Node endNode)
    {
        List<Node> path = new List<Node>();
        Node currentNode = endNode;

        while (currentNode != startNode)
        {
            path.Add(currentNode);
            Debug.Log($"Nodo aggiunto al percorso: {currentNode.gridX}, {currentNode.gridY} Posizione: {currentNode.worldPosition}");
            currentNode = currentNode.parent;
        }
        path.Reverse(); // Importante: inverte la lista per avere il percorso corretto

        grid.path = path; // Salva il percorso per il debug
        this.path = path; // Assegna il percorso per l'utilizzo

        Debug.Log("Percorso finale calcolato:");
        foreach (Node node in path)
        {
            Debug.Log($"Nodo: {node.gridX}, {node.gridY} - Posizione: {node.worldPosition}");
        }
    }


    public int GetDistance(Node a, Node b)
    {
        int dstX = Mathf.Abs(a.gridX - b.gridX);
        int dstY = Mathf.Abs(a.gridY - b.gridY);

        if (dstX > dstY)
            return 14 * dstY + 10 * (dstX - dstY);
        return 14 * dstX + 10 * (dstY - dstX);
    }

    private Node FindClosestWalkableNode(Node node)
    {
        // Cerca il nodo camminabile più vicino
        List<Node> neighbors = grid.GetNeighbors(node);
        foreach (Node neighbor in neighbors)
        {
            if (neighbor.walkable)
            {
                return neighbor;
            }
        }

        Debug.LogWarning($"Nessun nodo camminabile vicino a {node.gridX}, {node.gridY}");
        return null;
    }

    void OnDrawGizmos()
    {
        if (path != null)
        {
            Gizmos.color = Color.blue; // Cambia colore del percorso

            for (int i = 0; i < path.Count - 1; i++)
            {
                // Disegna una linea tra i nodi del percorso
                Gizmos.DrawLine(path[i].worldPosition, path[i + 1].worldPosition);
            }

            // Disegna un punto per ogni nodo
            foreach (Node node in path)
            {
                Gizmos.DrawSphere(node.worldPosition, 0.3f); // Cambia il raggio della sfera
            }
        }
    }
}