using UnityEngine;
using System.Collections;

public class Energizer : MonoBehaviour {

    private GameManager gm;
    public TileManager.Tile tile;

    // Use this for initialization
    void Start ()
	{
	    gm = GameObject.Find("Game Manager").GetComponent<GameManager>();
        if( gm == null )    Debug.Log("Energizer did not find Game Manager!");
	}

    void OnTriggerEnter2D(Collider2D col)
    {
        if(col.tag == "pacman")
        {
            tile.hasPacgum = false;
            gm.ScareGhosts();
            Destroy(gameObject);
        }
    }
}
