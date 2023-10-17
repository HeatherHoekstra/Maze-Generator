using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MazeManager : MonoBehaviour
{
    public bool isGeneratingMaze;
    public bool isHexagonMaze;
    public bool playMode;
    public bool wantsToRestartMaze;

    public float speed;

    public int numberOfColumns;
    public int numberOfRows;

    public GameObject animatedTiles;

    public static MazeManager Instance { get; private set; }

    [SerializeField] private GameObject mazeRectangle;
    [SerializeField] private GameObject mazeHexagon;

    [SerializeField] private GameObject floorVisitedFadePrefab;
    [SerializeField] private GameObject floorDoneFadePrefab;
    [SerializeField] private GameObject wallFadePrefab;
    [SerializeField] private GameObject floorVisitedFadePrefabH;
    [SerializeField] private GameObject floorDoneFadePrefabH;
    [SerializeField] private GameObject wallFadePrefabH;

    [SerializeField] private Tilemap floorMap;

    [SerializeField] private Tile floorTileUnvisited;
    [SerializeField] private Tile floorTileVisited;
    [SerializeField] private Tile floorTileDone;
    [SerializeField] private Tile wallTile;

    [SerializeField] private Tile floorTileUnvisitedH;
    [SerializeField] private Tile floorTileVisitedH;
    [SerializeField] private Tile floorTileDoneH;
    [SerializeField] private Tile wallTileH;

    private CameraManager cameraManager;

    private Cell currentCell;
    private Cell unvisitedNeighbour;

    private Coroutine currentCoroutine;

    private GameObject activeMaze;
    private GameObject floorVisitedFade;
    private GameObject floorDoneFade;

    private int direction; // Rectangle: 0 = Top, 1 = Right, 2 = Bottom, 3 = Left. Hexagon: 0 = top left, 1 = top right, 3 = right, etc.
    private int neighbourDirection;

    private List<Tilemap> wallMaps = new();
    private List<Matrix4x4> wallTileMatrixes = new();
    private List<Cell> cells = new();
    private List<Cell> visitedCells = new();

    private Vector2 hexagonOffset = new(.86f, .75f);

    private void Awake()
    {
        // If there is an instance, and it's not me, delete myself.
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
    }

    private void Start()
    {
        cameraManager = Camera.main.gameObject.GetComponent<CameraManager>();

        //Start as a rectangle maze
        ChangeMazeShape(false);

        CreateGrid();
    }

    public void StartGeneratingMaze()
    {
        currentCoroutine = StartCoroutine(GenerateMaze());
    }

    public void StopGeneratingMaze()
    {
        //If there is an active coroutine, stop and remove it.
        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
            currentCoroutine = null;
            isGeneratingMaze = false;
        }

        //Destroy all fading tiles
        for (int i = 0; i < animatedTiles.transform.childCount; i++)
        {
            GameObject tile = animatedTiles.transform.GetChild(i).gameObject;
            Destroy(tile);
        }

        ClearGrid();
    }

    public void ChangeMazeShape(bool isHexagon)
    {
        isHexagonMaze = isHexagon;

        //Change the active maze, tilemap, and prefabs for the floor and wall animations.
        mazeHexagon.SetActive(isHexagonMaze);
        mazeRectangle.SetActive(!isHexagonMaze);

        activeMaze = isHexagonMaze ? mazeHexagon : mazeRectangle;
        floorMap = activeMaze.transform.GetChild(0).gameObject.GetComponent<Tilemap>();

        floorVisitedFade = isHexagonMaze ? floorVisitedFadePrefabH : floorVisitedFadePrefab;
        floorDoneFade = isHexagonMaze ? floorDoneFadePrefabH : floorDoneFadePrefab;

        //Create a new list of wall tilemaps
        wallMaps.Clear();
        foreach (Transform child in activeMaze.transform)
        {
            if (child.gameObject.name.Contains("Wall"))
            {
                wallMaps.Add(child.gameObject.GetComponent<Tilemap>());
            }
        }

        CreateWallTileData();

        //Correct the slider value in case you switch to play mode and the maze was a hexagon
        if (UIManager.Instance.shapeSlider.value == 1 && !isHexagon)
        {
            UIManager.Instance.shapeSlider.value = 0;
        }
    }

    public void ClearGrid()
    {
        //Clear all the stored data
        cells.Clear();
        visitedCells.Clear();

        floorMap.ClearAllTiles();
        foreach (Tilemap map in wallMaps)
        {
            map.ClearAllTiles();
        }

        if (wantsToRestartMaze || playMode)
        {
            CreateGrid();
        }
    }

    public void CreateGrid()
    {
        cameraManager.AdjustCameraSettings(numberOfColumns, numberOfRows);

        int index;

        for (int i = 0; i < numberOfColumns; i++)
        {
            for (int j = 0; j < numberOfRows; j++)
            {
                Cell cell = new(i, j);
                cells.Add(cell);

                CreateTiles(i, j);

                index = cells.Count - 1;

                int currentDirection = wallMaps.Count - 1;

                //Check if cell has neighbour on left.
                if (cell.posX > 0)
                {
                    //Subtract the number of rows to find the left neighbour.
                    cell.unvisitedNeighbours.Add(currentDirection, cells[index - numberOfRows]);

                    //Set the current cell as the right neighbour of the left neighbour.
                    cell.unvisitedNeighbours[currentDirection].unvisitedNeighbours.Add(currentDirection - wallMaps.Count / 2, cell);

                    //When the maze is a hexagon, add a top left neighbour if the cell isn't on uneven rows or the top cell of a row.
                    if (isHexagonMaze && cell.posY % 2 == 0 && cell.posY < numberOfRows - 1)
                    {
                        currentDirection = 0;
                        cell.unvisitedNeighbours.Add(currentDirection, cells[index - numberOfRows + 1]);
                        cell.unvisitedNeighbours[currentDirection].unvisitedNeighbours.Add(currentDirection + wallMaps.Count / 2, cell); //0, 3
                    }
                }
                //Check if cell has a neighbour below.
                if (cell.posY > 0)
                {
                    //If the maze is a reactangle or row is uneven.
                    if (!isHexagonMaze || isHexagonMaze && cell.posY % 2 == 1)
                    {
                        currentDirection = wallMaps.Count - 2;
                    }
                    else
                    {
                        //check if the row is even, to see what side the neighbour below the cell is on.
                        if (cell.posY % 2 == 0)
                        {
                            currentDirection = 3;
                        }
                    }

                    //Subtract one to find the bottom neighbour.
                    cell.unvisitedNeighbours.Add(currentDirection, cells[index - 1]);

                    //Set the current cell as the top neighbour for the bottom neighbour.
                    cell.unvisitedNeighbours[currentDirection].unvisitedNeighbours.Add(currentDirection - wallMaps.Count / 2, cell);
                }
            }
        }

        if (wantsToRestartMaze || playMode)
        {
            StartGeneratingMaze();
            wantsToRestartMaze = false;
        }
    }

    public void ChangePlaymode()
    {
        PlayModeManager.Instance.gameObject.SetActive(playMode);

        if (!playMode)
        {
            //When switching to generation mode, set the columns and rows to their old sizes, and create a new grid.
            UIManager.Instance.ChangeColumns();
            UIManager.Instance.ChangeRows();

            isGeneratingMaze = false;
            ClearGrid();
            CreateGrid();
        }
        else
        {
            //When switching to play mode and the active maze is a hexagon, change it to a rectangle
            if (isHexagonMaze)
            {
                ChangeMazeShape(false);
            }

            PlayModeManager.Instance.StartPlayMode();
        }

        //Activate or deactivate the colliders on the wall tilemaps.
        foreach (Tilemap wall in wallMaps)
        {
            wall.gameObject.GetComponent<Collider2D>().enabled = playMode;
        }
    }

    private void CreateTiles(int x, int y)
    {
        Vector3Int tilePosition = new(x, y, 0);

        int index = 0;

        //Pick the correct floor and wall prefab based on the maze shape.
        Tile floorToPlace = isHexagonMaze ? floorTileUnvisitedH : floorTileUnvisited;
        Tile wallToPlace = isHexagonMaze ? wallTileH : wallTile;

        //Change the floor to a done floor tile when in playmode.
        floorToPlace = playMode ? floorTileDone : floorToPlace;

        //Create floor
        floorMap.SetTile(tilePosition, floorToPlace);

        //Create walls on each wall map, picking the correct rotation for the map
        foreach (Tilemap map in wallMaps)
        {
            map.SetTile(tilePosition, wallToPlace);
            map.SetTransformMatrix(tilePosition, wallTileMatrixes[index]);

            index++;
        }
    }

    private void CreateWallTileData()
    {
        wallTileMatrixes.Clear();

        int angle = 0;

        //Pick the correct amount the angle should change for the cell shape.
        int angleDifference = isHexagonMaze ? 60 : 90;

        //For each wall tilemap, create an decreasing rotation and store this as an matrix in list.
        for (int i = 0; i < wallMaps.Count; i++)
        {
            Quaternion tileRotation = Quaternion.Euler(0, 0, angle);
            Matrix4x4 matrix = Matrix4x4.TRS(Vector3.zero, tileRotation, Vector3.one);

            wallTileMatrixes.Add(matrix);

            angle -= angleDifference;
        }
    }

    private void RemoveWalls()
    {
        //Don't animate the removed walls when in playmode.
        if (!playMode)
        {
            //Calculate the postion of the animated wall.
            Vector2 position = new Vector2(currentCell.posX + .5f, currentCell.posY + .5f);

            //Change the position if the maze is a hexagon
            if (isHexagonMaze)
            {
                //Check of the row is even or uneven and adjust the position. An uneven row needs to be moved more to the right.
                if (currentCell.posY % 2 == 0)
                {
                    position = new Vector2((currentCell.posX * hexagonOffset.x) + (hexagonOffset.x / 2), (currentCell.posY * hexagonOffset.y) + (hexagonOffset.y / 2));
                }
                else
                {
                    position = new Vector2((currentCell.posX * hexagonOffset.x) + (hexagonOffset.x), (currentCell.posY * hexagonOffset.y) + (hexagonOffset.y / 2));
                }
            }

            //Calculate the rotation of the animated wall, based on the direction.
            Quaternion rotation = isHexagonMaze ? Quaternion.Euler(0, 0, direction * -60) : Quaternion.Euler(0, 0, direction * -90);

            //Create the animated wall, picking the prefab based on the maze shape.
            GameObject newWallFade = Instantiate(isHexagonMaze ? wallFadePrefabH : wallFadePrefab, position, rotation);

            newWallFade.transform.parent = animatedTiles.transform;
        }

        //Remove wall from wallmap with index of direction, and of neighbour direction for the neighbour
        wallMaps[direction].SetTile(new Vector3Int(currentCell.posX, currentCell.posY, 0), null);
        wallMaps[neighbourDirection].SetTile(new Vector3Int(unvisitedNeighbour.posX, unvisitedNeighbour.posY, 0), null);

        //Remove the neighbour from the unvisited neighbours list.
        currentCell.unvisitedNeighbours.Remove(direction);
    }

    private void VisitCell()
    {
        //Don't animate the generation when in play mode.
        if (!playMode)
        {
            //Check if the cell has been visited before, set the matching prefab.
            GameObject newFade = !currentCell.visited ? floorVisitedFade : floorDoneFade;

            //Create a new gameobject with an animation of the new floortile fading in.
            GameObject newTile = Instantiate(newFade, new Vector2(currentCell.posX, currentCell.posY), Quaternion.identity);

            //Correct the position if the maze is hexagon
            if (isHexagonMaze)
            {
                //The even rows need to move one cell width, the uneven ones half a cell width.
                if (currentCell.posY % 2 == 0)
                {
                    newTile.transform.position = new Vector2(currentCell.posX * hexagonOffset.x, currentCell.posY * hexagonOffset.y);
                }
                else
                {
                    newTile.transform.position = new Vector2((currentCell.posX * hexagonOffset.x) + (hexagonOffset.x / 2), currentCell.posY * hexagonOffset.y);
                }
            }

            //Pass the current cell and tilemap on to the animated object, so it knows where to place the tile and on what tilemap when it's done.
            TilePlacer tilePlacer = newTile.GetComponent<TilePlacer>();
            newTile.transform.parent = animatedTiles.transform;
            tilePlacer.matchingCell = currentCell;
            tilePlacer.floorMap = floorMap;
        }

        //If the cell is not visited, mark it as such and add it to the visited cells list.
        if (!currentCell.visited)
        {
            currentCell.visited = true;
            visitedCells.Add(currentCell);
        }
    }

    private IEnumerator GenerateMaze()
    {
        isGeneratingMaze = true;

        //Pick a starting cell
        currentCell = cells[Random.Range(0, cells.Count)];

        VisitCell();

        //While there are visited cells, chose a new cell to go to.
        while (visitedCells.Count > 0)
        {
            //If there is a unvisited neighbour
            if (currentCell.unvisitedNeighbours.Count > 0)
            {
                //Pick a neighbour from the unvisited neigbours
                int randomIndex = Random.Range(0, currentCell.unvisitedNeighbours.Count);

                unvisitedNeighbour = currentCell.unvisitedNeighbours.Values.ElementAt(randomIndex);

                if (unvisitedNeighbour.visited)
                {
                    //If the chosen neighbour is marked as visited, remove it from the dictionary and go back the top of the loop to pick another one.
                    currentCell.unvisitedNeighbours.Remove(currentCell.unvisitedNeighbours.Keys.ElementAt(randomIndex));
                }
                else
                {
                    //Set the direction as the key of the unvisited neighbour.
                    direction = currentCell.unvisitedNeighbours.Keys.ElementAt(randomIndex);

                    //Set the neighbour direction to the opposite of the current cell direction.
                    if (direction < wallMaps.Count / 2)
                    {
                        neighbourDirection = direction + wallMaps.Count / 2;
                    }
                    else
                    {
                        neighbourDirection = direction - wallMaps.Count / 2;
                    }

                    RemoveWalls();

                    currentCell = unvisitedNeighbour;

                    VisitCell();
                }
            }
            //If there are no visited cells in the list, backtrack untill you find one with an unvisited neighbour.
            else
            {
                //Remove the current cell from the visited cells. If there are any left, move to the most recently added visited cell in the list.
                visitedCells.Remove(currentCell);
                if (visitedCells.Count > 0)
                {
                    VisitCell();
                    currentCell = visitedCells[visitedCells.Count - 1];
                }
                VisitCell();
            }

            if (!playMode)
            {
                //Wait to start the next round of the loop, to be able to see the generation of the maze.
                yield return new WaitForSeconds(speed);
            }
        }

        //The maze is done, wait till animation tiles are done.
        yield return new WaitForSeconds(.2f);

        if (!playMode)
        {
            UIManager.Instance.ToggleDoneScreen(true);
        }
        else
        {
            //When in play mode, pass the maze size on so the goal can be set at the correct place.
            PlayModeManager.Instance.SetAllPositions(new Vector2(numberOfColumns - 1, numberOfRows - 1));
        }
    }
}