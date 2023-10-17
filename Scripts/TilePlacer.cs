using UnityEngine;
using UnityEngine.Tilemaps;

public class TilePlacer : MonoBehaviour
{
    public Cell matchingCell;

    public Tilemap floorMap;

    [SerializeField] private Tile floorTileVisited;
    [SerializeField] private Tile floorTileDone;

    public void DestorySelf()
    {
        Destroy(gameObject);
    }

    public void PlaceTile()
    {
        //Place the correct tile in the tilemap, then destroy this gameObject.
        Tile newTile = gameObject.name.Contains("Visited") ? floorTileVisited : floorTileDone;
        floorMap.SetTile(new Vector3Int(matchingCell.posX, matchingCell.posY, 0), newTile);

        DestorySelf();
    }
}