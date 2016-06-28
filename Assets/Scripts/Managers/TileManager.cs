using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class TileManager : MonoBehaviour {

	public class Tile
	{
		public int x { get; set; }
		public int y { get; set; }
		public bool occupied {get; set;}
		public int adjacentCount {get; set;}
		public bool isIntersection {get; set;}
        public bool hasPacdot { get; set; }
        public bool isDangerous { get; set; }
		
		public Tile left,right,up,down;
		
		public Tile(int x_in, int y_in)
		{
			x = x_in; y = y_in;
			occupied = hasPacdot = isDangerous = false;
			left = right = up = down = null;
		}


	};
	
	public List<Tile> tiles = new List<Tile>();
    public GameObject PacdotPrefab;
	
	// Use this for initialization
	void Start () 
	{
        ReadTiles();

        // Instantiation des pacdots
        foreach(Tile t in tiles)
        {
            if (!t.occupied)
            {
                GameObject dot = GameObject.Instantiate(PacdotPrefab, new Vector3(t.x, t.y), Quaternion.identity) as GameObject;
                t.hasPacdot = true;
                dot.GetComponent<Pacdot>().tile = t;
            }
        }

	}

    // Update is called once per frame
	void Update () 
	{
		//DrawNeighbors();
	}
	
	//-----------------------------------------------------------------------
	// hardcoded tile data: 1 = free tile, 0 = wall
    void ReadTiles()
    {
        // hardwired data instead of reading from file (not feasible on web player)
        string data = @"0000000000000000000000000000
0111111111111001111111111110
0100001000001001000001000010
0100001000001111000001000010
0100001000001001000001000010
0111111111111001111111111110
0100001001000000001001000010
0100001001000000001001000010
0111111001111001111001111110
0001001000001001000001001000
0001001000001001000001001000
0111001111111111111111001110
0100001001000000001001000010
0100001001000000001001000010
0111111001000000001001111110
0100001001000000001001000010
0100001001000000001001000010
0111001001111111111001001110
0001001001000000001001001000
0001001001000000001001001000
0111111111111111111111111110
0100001000001001000001000010
0100001000001001000001000010
0111001111111001111111001110
0001001001000000001001001000
0001001001000000001001001000
0111111001111001111001111110
0100001000001001000001000010
0100001000001001000001000010
0111111111111111111111111110
0000000000000000000000000000";

        int X = 1, Y = 31;
        using (StringReader reader = new StringReader(data))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {

                X = 1; // for every line
                for (int i = 0; i < line.Length; ++i)
                {
                    Tile newTile = new Tile(X, Y);

                    // if the tile we read is a valid tile (movable)
                    if (line[i] == '1')
                    {
                        // check for left-right neighbor
                        if (i != 0 && line[i - 1] == '1')
                        {
                            // assign each tile to the corresponding side of other tile
                            newTile.left = tiles[tiles.Count - 1];
                            tiles[tiles.Count - 1].right = newTile;

                            // adjust adjcent tile counts of each tile
                            newTile.adjacentCount++;
                            tiles[tiles.Count - 1].adjacentCount++;
                        }
                    }

                    // if the current tile is not movable
                    else newTile.occupied = true;

                    // check for up-down neighbor, starting from second row (Y<30)
                    int upNeighbor = tiles.Count - line.Length; // up neighbor index
                    if (Y < 30 && !newTile.occupied && !tiles[upNeighbor].occupied)
                    {
                        tiles[upNeighbor].down = newTile;
                        newTile.up = tiles[upNeighbor];

                        // adjust adjcent tile counts of each tile
                        newTile.adjacentCount++;
                        tiles[upNeighbor].adjacentCount++;
                    }

                    tiles.Add(newTile);
                    X++;
                }

                Y--;
            }
        }

        // after reading all tiles, determine the intersection tiles
        foreach (Tile tile in tiles)
        {
            if (tile.adjacentCount > 2)
                tile.isIntersection = true;
        }

    }

	//-----------------------------------------------------------------------
	// Draw lines between neighbor tiles (debug)
	void DrawNeighbors()
	{
		foreach(Tile tile in tiles)
		{
			Vector3 pos = new Vector3(tile.x, tile.y, 0);
			Vector3 up = new Vector3(tile.x+0.1f, tile.y+1, 0);
			Vector3 down = new Vector3(tile.x-0.1f, tile.y-1, 0);
			Vector3 left = new Vector3(tile.x-1, tile.y+0.1f, 0);
			Vector3 right = new Vector3(tile.x+1, tile.y-0.1f, 0);
			
			if(tile.up != null)		Debug.DrawLine(pos, up);
			if(tile.down != null)	Debug.DrawLine(pos, down);
			if(tile.left != null)	Debug.DrawLine(pos, left);
			if(tile.right != null)	Debug.DrawLine(pos, right);
		}
		
	}

    public Tile GetTile(int x, int y)
    {
        return tiles[Index(x, y)];
    }

	//----------------------------------------------------------------------
	// returns the index in the tiles list of a given tile's coordinates
	public int Index(int X, int Y)
	{
		// if the requsted index is in bounds
		//Debug.Log ("Index called for X: " + X + ", Y: " + Y);
		if(X>=1 && X<=28 && Y<=31 && Y>=1)
			return (31-Y)*28 + X-1;

		// else, if the requested index is out of bounds
		// return closest in-bounds tile's index 
	    if(X<1)		X = 1;
	    if(X>28) 	X = 28;
	    if(Y<1)		Y = 1;
	    if(Y>31)	Y = 31;

	    return (31-Y)*28 + X-1;
	}
	
	public int Index(Tile tile)
	{
		return (31-tile.y)*28 + tile.x-1;
	}

	//----------------------------------------------------------------------
	// returns the distance between two tiles
	public float distance(Tile tile1, Tile tile2)
	{
		return Mathf.Sqrt( Mathf.Pow(tile1.x - tile2.x, 2) + Mathf.Pow(tile1.y - tile2.y, 2));
	}

    public enum TILEDIRECTION
    {
        UP,
        DOWN,
        LEFT,
        RIGHT
    }

    /// <summary>
    /// Retourne le tile ayant un pacdot le plus proche
    /// </summary>
    /// <param name="myTile">Tile de départ</param>
    /// <returns></returns>
    public Tile GetClosestTileWithPacdot(Tile myTile)
    {
        float min = 1000;
        Tile minT = myTile;

        foreach(Tile t in tiles)
        {
            if (t.hasPacdot && distance(myTile, t) < min)
            {
                min = distance(myTile, t);
                minT = t;
            }
        }

        return minT;
    }

    /// <summary>
    /// Retourne une file contenant le chemin le plus court pour aller de start à goal
    /// </summary>
    /// <param name="start">Tile de départ</param>
    /// <param name="goal">Tile d'arrivée</param>
    /// <returns>Queue de tous les tiles à parcourir pour atteindre le tile final</returns>
    public static Queue<Tile> GetPathTo(Tile start, Tile goal)
    {
        // Frontière de recherche
        Queue<Tile> frontier = new Queue<Tile>();
        frontier.Enqueue(start);

        // Dictionnaire qui pour chaque tile associe celui duquel on vient
        Dictionary<Tile, Tile> cameFrom = new Dictionary<Tile, Tile>();
        cameFrom[start] = null;

        // Tant qu'on a encore des tiles à explorer
        while (frontier.Count > 0)
        {
            Tile current = frontier.Dequeue();

            // Si le tile a un ghost dessus, on l'ignore (pour l'éviter)
            if (!current.isDangerous)
            {
                // Pour chaque voisin du tile actuel, l'ajouter à la liste des frontières
                // et populate son cameFrom
                if (current.up != null)
                {
                    if (!cameFrom.ContainsKey(current.up))
                    {
                        frontier.Enqueue(current.up);
                        cameFrom[current.up] = current;
                    }
                }

                if (current.down != null)
                {
                    if (!cameFrom.ContainsKey(current.down))
                    {
                        frontier.Enqueue(current.down);
                        cameFrom[current.down] = current;
                    }
                }

                if (current.left != null)
                {
                    if (!cameFrom.ContainsKey(current.left))
                    {
                        frontier.Enqueue(current.left);
                        cameFrom[current.left] = current;
                    }
                }

                if (current.right != null)
                {
                    if (!cameFrom.ContainsKey(current.right))
                    {
                        frontier.Enqueue(current.right);
                        cameFrom[current.right] = current;
                    }
                }
            }
            
        }

        // Parcourir la liste cameFrom à l'envers pour déterminer le chemin
        Tile c = goal;
        List<Tile> path = new List<Tile>();

        path.Add(c);
        while (c != start)
        {
            c = cameFrom[c];
            path.Add(c);
        }

        // Reverse la liste
        path.Reverse();

        // Tant pis pour les perf on s'en fiche pour l'instant
        return new Queue<Tile>(path);
    }

    /// <summary>
    /// Retourne une liste de tiles depuis le tile startingTile jusqu'à la prochaine intersection
    /// </summary>
    /// <param name="startingTile">Le tile de départ</param>
    /// <param name="direction">La direction</param>
    /// <param name="maxDepth">Profondeur maximale de recherche</param>
    /// <returns>Liste de tiles jusqu'à la prochaine intersection</returns>
    public static List<Tile> GetCorridor(Tile startingTile, TILEDIRECTION direction, int maxDepth = 1000)
    {
        if (maxDepth <= 0) maxDepth = 2;

        int curDepth = 1;
        Tile t = startingTile;
        Tile previousT = t;
        List<Tile> corridor = new List<Tile>();

        corridor.Add(t);

        switch (direction)
        {
            case TILEDIRECTION.UP:
                t = t.up;
                break;
            case TILEDIRECTION.DOWN:
                t = t.down;
                break;
            case TILEDIRECTION.LEFT:
                t = t.left;
                break;
            case TILEDIRECTION.RIGHT:
                t = t.right;
                break;
        }

        // Tant qu'on a pas atteint une intersection ou qu'on a pas dépassé maxDepth on avance
        while (!t.isIntersection && curDepth < maxDepth - 1)
        {
            corridor.Add(t);
            curDepth++;

            if (t.up != null && t.up != previousT)
            {
                previousT = t;
                t = t.up;
            }
            else if (t.right != null && t.right != previousT)
            {
                previousT = t;
                t = t.right;
            }
            else if (t.down != null && t.down != previousT)
            {
                previousT = t;
                t = t.down;
            }
            else if (t.left != null && t.left != previousT)
            {
                previousT = t;
                t = t.left;
            }
        }

        // On ajoute la dernière intersection pour plus de "visibilité" de l'IA
        corridor.Add(t); 

        return corridor;
    }
}
