using UnityEditor;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    private Camera cam;

    private float ratio;
    private float screenSizeX = 1920;
    private float screenSizeY = 1080;

    private int x;
    private int y;

    //These are half the size of a cell.
    private Vector2 hexagonCorrection = new(.45f, .375f);

    private Vector2 rectangleCorrection = new(.5f, .5f);

    private void Awake()
    {
        cam = Camera.main;
        ratio = screenSizeX / screenSizeY;
    }

    //Only relevant in Unity editor
    private void Update()
    {
        //Check to see if the resolution in the game window has changed.
        //if (Handles.GetMainGameViewSize().x != screenSizeX)
        //{
        //    //since the maze size hasn't changed, give the old sizes.
        //    AdjustCameraSettings(x, y);
        //}
    }

    public void AdjustCameraSettings(int newX, int newY)
    {
        x = newX;
        y = newY;

        Vector2 mazeShapeCorrection = MazeManager.Instance.isHexagonMaze ? hexagonCorrection : rectangleCorrection;

        //Only relevant in Unity editor

            //Get the resolutions of the game window, and calculate the ratio
            //screenSizeX = Handles.GetMainGameViewSize().x;
            //screenSizeY = Handles.GetMainGameViewSize().y;
            //ratio = screenSizeX / screenSizeY;

        //Set to true when the maze is so wide that it won't fit the screen when adjusting the camera size for the hight of the maze.
        //For example: when the (rectangle) maze is 200 by 100, and the screen is 1920x1080, you will get 200 /1920 (=0.104) and 100 /1080(=0.092),
        //so the camera size needs to be adjusted to the width.
        //In case of a hexagon maze, the "2* mazeShapeCorrection" helps factor in the irregular shape of the hexagon cells.
        bool fitScreenToWidth = x * 2 * mazeShapeCorrection.x / screenSizeX > y * 2 * mazeShapeCorrection.y / screenSizeY;

        //Calculate edge so it's always 3% or 5% of the maze size the camera size is set to.
        float edge = fitScreenToWidth ? (float)x / 33 : (float)y / 20;

        //When the cam size needs to be set to the hight, multiply this by half the cell height.
        //When the cam size needs to be set to the width, calculate the size by dividing the width by the screen ratio, and then multiply by half the cell width.
        //For example: 200 / 1.7 = 117.5, multiplied by .5 = 58.75.
        //Then add some room at the edge of the screen.
        cam.orthographicSize = fitScreenToWidth ? ((x / ratio) * mazeShapeCorrection.x) + edge : (y * mazeShapeCorrection.y) + edge;

        if (!MazeManager.Instance.playMode)
        {
            cam.transform.position = new Vector3(x * mazeShapeCorrection.x, y * mazeShapeCorrection.y, -10);
        }
        else //If in playmode, add a little bit of extra room at the top for the timer
        {
            cam.transform.position = new Vector3(x * mazeShapeCorrection.x, y * mazeShapeCorrection.y + (edge / 2), -10);
        }
    }
}