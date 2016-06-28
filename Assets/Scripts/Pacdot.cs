using UnityEngine;
using System.Collections;

public class Pacdot : MonoBehaviour {

    public TileManager.Tile tile;

	void OnTriggerEnter2D(Collider2D other)
	{
		if(other.tag == "pacman")
		{
			GameManager.score += 10;
		    GameObject[] pacdots = GameObject.FindGameObjectsWithTag("pacdot");
            tile.hasPacdot = false;
            Destroy(gameObject);

		    if (pacdots.Length == 1)
		    {
		        GameObject.FindObjectOfType<GameGUINavigation>().LoadLevel();
		    }
		}
	}
}
