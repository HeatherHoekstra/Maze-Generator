using System.Collections.Generic;

public class Cell
{
    public int posX;
    public int posY;

    public bool visited;

    public Dictionary<int, Cell> unvisitedNeighbours = new();

    //Constructor to set the position values on creation
    public Cell(int x, int y)
    {
        posX = x;
        posY = y;
    }
}