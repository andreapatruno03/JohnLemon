using UnityEngine;

public class Node
{
    public bool walkable; // Indica se il nodo è camminabile
    public Vector3 worldPosition; // La posizione nel mondo del nodo
    public int gridX; // Coordinata X nella griglia
    public int gridY; // Coordinata Y nella griglia

    // Proprietà per A*
    public int gCost; // Costo dal nodo iniziale a questo nodo
    public int hCost; // Costo stimato dal nodo a quello finale
    public Node parent; // Nodo genitore per tracciare il percorso

    // Costo totale (fCost = gCost + hCost)
    public int fCost
    {
        get { return gCost + hCost; }
    }

    // Costruttore
    public Node(bool _walkable, Vector3 _worldPosition, int _gridX, int _gridY)
    {
        walkable = _walkable;
        worldPosition = _worldPosition;
        gridX = _gridX;
        gridY = _gridY;
    }
}