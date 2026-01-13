using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Map : MonoBehaviour
{
    //tiles shown as reachable to the player when move mode in on
    public List<Tile> tileRevealed;

    //spawner and exits to end the level
    [SerializeField]
    private Tile entree;
    [SerializeField]
    private List<Tile> sorties;

    private Dictionary<Vector2Int, Tile> _mapData = new Dictionary<Vector2Int, Tile>();

    void Start()
    {
        InitMap();
    }

    /// <summary>
    /// Initialize the data structure to manipulate the level design.
    /// </summary>
    public void InitMap()
    {
        tileRevealed = new List<Tile>();
        foreach (Tile t in this.gameObject.transform.GetComponentsInChildren<Tile>())
        {
            Vector2Int pos = new Vector2Int(
                                                Mathf.RoundToInt(t.transform.position.x), 
                                                Mathf.RoundToInt(t.transform.position.z)
                                            );

            if (t.GetComponent<Entrance>()) entree = t;
            if (t.GetComponent<Exit>()) sorties.Add(t);

            t.TileIndex = pos;
            t.gameObject.name = "Tile [" + pos.x + " , " + pos.y + "]";
            _mapData[pos] = t;
        }
    }

    /// <summary>
    /// Attempt to retrieve the tile by its key info
    /// </summary>
    /// <param name="pos">A vector 2 key reprensenting the X and Z position in world space</param>
    /// <returns>The tile object found or null if no key was found</returns>
    public Tile GetTileAt(Vector2Int pos)
    {
        return _mapData.TryGetValue(pos, out Tile t) ? t : null;
    }
    /// <summary>
    /// Attempt to find a tile with the Unit on it.
    /// </summary>
    /// <param name="u">The Unit object</param>
    /// <returns>The tile Object or null if no tile with the Unit on it was found</returns>
    public Tile FindTileWithUnit(Unit u)
    {
        Tile currentTile = null;
        foreach (Tile t in _mapData.Values)
        {
            if (t.GetComponent<Tile>().hasUnitOn && t.GetComponent<Tile>().unitOnTile == u)
            {
                currentTile = t.GetComponent<Tile>();
                break;
            }
        }
        return currentTile;
    }


    /// <summary>
    /// Calculate the possibility of movement for the Unit
    /// </summary>
    /// <param name="u">Unit object that is trying to move on the board. Reveal the tiles available.</param>
    /// <param name="maxPM">Availaible movement points</param>
    public void CalculateMovement(Unit u, int maxPM)
    {
        ClearRevealedTiles();
        Tile startTile = FindTileWithUnit(u);
        if (startTile == null) return;

        //explored tiles, memory to avoid double checking
        HashSet<Vector2Int> explored = new HashSet<Vector2Int>();
        //valid tiles to expands when looking for further neighbours
        Queue<(Vector2Int pos, int remainingPM)> queue = new Queue<(Vector2Int, int)>();

        //spread from unit tile
        queue.Enqueue((startTile.TileIndex, maxPM));
        explored.Add(startTile.TileIndex);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            //if pm left keeps going
            if (current.remainingPM > 0)
            {
                Vector2Int[] neighbors = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

                foreach (Vector2Int dir in neighbors)
                {
                    Vector2Int nextPos = current.pos + dir;

                    if (!explored.Contains(nextPos) && _mapData.TryGetValue(nextPos, out Tile nextTile))
                    {
                        if (nextTile.isWalkable && !nextTile.hasUnitOn)
                        {
                            explored.Add(nextPos);
                            tileRevealed.Add(nextTile);
                            queue.Enqueue((nextPos, current.remainingPM - 1));
                        }
                    }
                }
            }
        }
        RevealTiles();
    }
    /// <summary>
    /// Reveal tiles from the tileRevealed List
    /// </summary>
    public void RevealTiles()
    {
        if (tileRevealed.Count > 0)
        {
            foreach (Tile t in tileRevealed)
            {
                SpriteRenderer s = t.gameObject.GetComponentInChildren<SpriteRenderer>();
                s.color = new Color(.2f, 1, .2f, .6f);
            }
        }
    }

    //chnage colors back to initial state, HARDCODED color!
    /// <summary>
    /// Revert tiless visuals to their initial state and remove any Objects in the revealedTiles List
    /// </summary>
    public void ClearRevealedTiles()
    {

            if (tileRevealed.Count > 0)
            {
                foreach (Tile t in tileRevealed)
                {
                    SpriteRenderer s = t.gameObject.GetComponentInChildren<SpriteRenderer>();
                    s.color = new Color(1, 1, 1, .2f);
                }
            }
        tileRevealed.Clear();
    }

    /// <summary>
    /// Calculate the raw Manhattan distance between two Tiles positions.
    /// </summary>
    /// <param name="t1">First tile to compare</param>
    /// <param name="t2">Second tile to compoare</param>
    /// <returns>An integer representing the distance bewteen the two Tiles</returns>
    public int GetDistanceCost(Tile t1, Tile t2)
    {
        if (t1 == null || t2 == null) return 0;

        //manatthan dist
        int dx = Mathf.Abs(t2.TileIndex.x - t1.TileIndex.x);
        int dy = Mathf.Abs(t2.TileIndex.y - t1.TileIndex.y);

        return dx + dy;
    }

}
