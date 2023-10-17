using System;
using System.Collections;
using UnityEngine;

public class PlayModeManager : MonoBehaviour
{
    public bool timing;

    public static PlayModeManager Instance { get; private set; }

    [SerializeField] private float startingTime;

    [SerializeField] private int totalMazes;

    private bool hasLost;

    private float timeLeft;

    private int mazesSolved;

    private TimeSpan timeSpan;

    private Transform player;
    private Transform start;
    private Transform goal;

    private Collider2D trigger;

    private PlayerController playerController;

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

        player = transform.Find("Player");
        start = transform.Find("Start");
        goal = transform.Find("Goal");

        trigger = goal.gameObject.GetComponent<Collider2D>();
        playerController = player.gameObject.GetComponent<PlayerController>();

        trigger.enabled = false;

        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (timing)
        {
            if (timeLeft > 0)
            {
                //Count down time
                timeLeft -= Time.deltaTime;
                timeSpan = TimeSpan.FromSeconds(timeLeft);

                UIManager.Instance.UpdateTimer(timeSpan);
            }
            else if (!hasLost)
            {
                //Out of time, has lost the game
                hasLost = true;
                UIManager.Instance.ToggleDoneScreen(false);
                Time.timeScale = 0;
            }
        }
    }

    public void GoToNextMaze()
    {
        mazesSolved++;

        if (mazesSolved < totalMazes) //Not yet solves all mazes, so load the next one
        {
            trigger.enabled = false;

            //Set new maze size (25% bigger than the last)
            MazeManager.Instance.numberOfColumns = Mathf.RoundToInt(MazeManager.Instance.numberOfColumns * 1.25f);
            MazeManager.Instance.numberOfRows = Mathf.RoundToInt(MazeManager.Instance.numberOfRows * 1.25f);

            //Clear the maze to start loading the new one
            MazeManager.Instance.ClearGrid();
        }
        else //Solved all mazes, so won the game!
        {
            UIManager.Instance.FadeBlackScreen(false);
            UIManager.Instance.ToggleDoneScreen(true);
            Time.timeScale = 0;
        }

        //Update the UI
        UIManager.Instance.UpdateMazesSolved(mazesSolved);
    }

    public void SetAllPositions(Vector2 goalPos)
    {
        //Set the player and start portal at the bottom left of the maze, and the goal at the top right.
        player.position = Vector2.zero;
        start.position = Vector2.zero;
        goal.position = goalPos;

        StartCoroutine(UsePortal());
    }

    public void StartPlayMode()
    {
        //Set all starting stats
        timing = false;
        trigger.enabled = false;
        hasLost = false;
        timeLeft = startingTime;
        mazesSolved = 0;

        UIManager.Instance.FadeBlackScreen(true);

        //Set starting maze size
        MazeManager.Instance.numberOfColumns = 8;
        MazeManager.Instance.numberOfRows = 5;

        UIManager.Instance.UpdateMazesSolved(mazesSolved);

        //Remove old grid to start generating a new maze
        MazeManager.Instance.ClearGrid();
    }

    public IEnumerator UsePortal()
    {
        if (timing)
        {
            //Stop timing so timer doesn't continue during the portal animation.
            timing = false;

            //Start the portal animation.
            player.position = goal.position;
            playerController.animator.SetBool("usingPortal", true);
            playerController.animator.SetTrigger("leave");

            //Wait, then fade in the black screen. After that, load the next maze.
            yield return new WaitForSeconds(.25f);
            UIManager.Instance.FadeBlackScreen(true);
            yield return new WaitForSeconds(1);
            GoToNextMaze();
        }
        else
        {
            //Fade out the black screen.
            UIManager.Instance.FadeBlackScreen(false);

            //Start the portal animation if it's not the first maze.
            if (mazesSolved > 0)
            {
                playerController.animator.SetTrigger("arrive");
            }

            //Wait untill the animation is done, then start timing and allow player to move.
            yield return new WaitForSeconds(.5f);
            playerController.animator.SetBool("usingPortal", false);
            timing = true;
            trigger.enabled = true;
        }
    }
}