using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public Slider shapeSlider;

    public static UIManager Instance { get; private set; }

    [SerializeField] private Animator blackScreenAnimator;
    [SerializeField] private Animator playmodeInstructionsAnimator;

    [SerializeField] private Button generateButton;

    [SerializeField] private GameObject settingsMenu;
    [SerializeField] private GameObject endScreenFireworks;
    [SerializeField] private GameObject generatingUI;
    [SerializeField] private GameObject playmodeUI;
    [SerializeField] private GameObject generateEnd;
    [SerializeField] private GameObject playmodeEnd;

    [SerializeField] private Slider columnsSlider;
    [SerializeField] private Slider rowsSlider;
    [SerializeField] private Slider speedSlider;

    [SerializeField] private Sprite play;
    [SerializeField] private Sprite replay;

    [SerializeField] [TextArea] private string enterPlaymode;
    [SerializeField] [TextArea] private string enterGenerationMode;
    [SerializeField] private string playmodeWin;
    [SerializeField] private string playmodeLose;

    [SerializeField] private TextMeshProUGUI columnsNumber;
    [SerializeField] private TextMeshProUGUI rowsNumber;
    [SerializeField] private TextMeshProUGUI speedNumber;
    [SerializeField] private TextMeshProUGUI ShapeText;
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private TextMeshProUGUI mazesSolvedText;
    [SerializeField] private TextMeshProUGUI playmodeInstructionsText;
    [SerializeField] private TextMeshProUGUI modeText;

    private Animator settingsAnimator;
    private Animator endScreenAnimator;

    private bool endScreenActive;
    private bool restarting;
    private bool settingsActive;

    private TextMeshProUGUI playmodeEndText;

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

        settingsAnimator = settingsMenu.GetComponent<Animator>();
        endScreenAnimator = endScreenFireworks.transform.parent.GetComponent<Animator>();
        playmodeEndText = playmodeEnd.GetComponent<TextMeshProUGUI>();

        //Make sure the settings menu and end screen are active
        settingsMenu.SetActive(true);
        endScreenAnimator.gameObject.SetActive(true);
    }

    public void ChangeColumns()
    {
        //Change the number of columns in the maze manager and UI.
        MazeManager.Instance.numberOfColumns = (int)columnsSlider.value;
        columnsNumber.text = columnsSlider.value.ToString();
    }

    public void ChangeRows()
    {
        //Change the number of rows in the maze manager and UI.
        MazeManager.Instance.numberOfRows = (int)rowsSlider.value;
        rowsNumber.text = rowsSlider.value.ToString();
    }

    public void ChangeShape()
    {
        //Check if the slider is on rectangle (0) or hexagon(1), and change the UI accordingly.
        bool isHexagon = shapeSlider.value == 1;
        ShapeText.text = isHexagon ? "Hexagon" : "Rectangle";

        //Change the slider max values. A hexagon has a smaller maximum size than a rectangle.
        columnsSlider.maxValue = isHexagon ? 100 : 250;
        rowsSlider.maxValue = isHexagon ? 100 : 250;

        //If the maze is a hexagon, and the slider values were higher than the new max sizes, set them to the new max size.
        if (isHexagon)
        {
            if (columnsSlider.value > columnsSlider.maxValue)
            {
                columnsSlider.value = columnsSlider.maxValue;
                ChangeColumns();
            }
            if (rowsSlider.value > rowsSlider.maxValue)
            {
                rowsSlider.value = rowsSlider.maxValue;
                ChangeRows();
            }
        }

        MazeManager.Instance.ChangeMazeShape(isHexagon);
    }

    public void ChangeSpeed()
    {
        //Change the speed (amount of time between each loop) of the generation in the maze manager en UI.
        MazeManager.Instance.speed = speedSlider.value;
        speedNumber.text = speedSlider.value.ToString();

        //Calculate how many times the slider value fits in the base speed (0.2).
        float decimalPointMultiplier = speedSlider.value < .2f ? 1 : 10;

        //Round it to one decimal place when it's smaller (speeds up) than the base speed, and two when it's bigger (slows down).
        float newSpeed = Mathf.Round((0.2f / speedSlider.value) * (10 * decimalPointMultiplier)) * (.1f / decimalPointMultiplier);

        speedNumber.text = newSpeed.ToString() + "x";
    }

    public void FadeBlackScreen(bool fadeIn)
    {
        string fade = fadeIn ? "fadeIn" : "fadeOut";
        blackScreenAnimator.SetTrigger(fade);
    }

    public void RestartAfterEnding()
    {
        restarting = true;

        //Keep the text on the screen the same while it's fading out.
        ToggleDoneScreen(playmodeEndText.text == playmodeWin);

        //Depending on the mode, start generating a new maze, or start the play mode again.
        if (!MazeManager.Instance.playMode)
        {
            StartGeneratingMaze();
        }
        else
        {
            PlayModeManager.Instance.StartPlayMode();
        }

        restarting = false;
    }

    public void StartGeneratingMaze()
    {
        //If not currently generating a maze, start generating. Otherwise, get ready to restart generation.
        if (!MazeManager.Instance.isGeneratingMaze)
        {
            generateButton.GetComponent<Image>().sprite = replay;
            MazeManager.Instance.StartGeneratingMaze();
        }
        else
        {
            MazeManager.Instance.wantsToRestartMaze = true;
            MazeManager.Instance.StopGeneratingMaze();
        }
    }

    public void SwitchMode()
    {
        //If you are in the settings menu, fade out the play mode pop-up.
        if (settingsActive)
        {
            playmodeInstructionsAnimator.SetTrigger("fade");
        }

        //Change the mode of the maze manager
        MazeManager.Instance.playMode = !MazeManager.Instance.playMode;

        bool playmode = MazeManager.Instance.playMode;

        //Change the UI to match the new mode
        modeText.text = playmode ? "Exit play mode" : "Enter play mode";
        generatingUI.SetActive(!playmode);
        playmodeUI.SetActive(playmode);
        settingsMenu.transform.GetChild(0).gameObject.SetActive(!playmode);

        //Stop the time scale when in play mode, and start it when in generation mode.
        Time.timeScale = playmode ? 0 : 1;

        //In in play mode, leave the settings menu.
        if (MazeManager.Instance.playMode)
        {
            ToggleSettingsMenu();
        }

        MazeManager.Instance.ChangePlaymode();
    }

    public void ToggleDoneScreen(bool won)
    {
        //If the fireworks are off and you have won/completed a maze, turn them on. When you have lost or the screen is closing, turn them off.
        if (!endScreenFireworks.activeInHierarchy && won)
        {
            endScreenFireworks.SetActive(true);
        }
        else
        {
            endScreenFireworks.SetActive(false);
        }

        endScreenActive = !endScreenActive;

        //If the end screen is closing, start the time.
        if (!endScreenActive)
        {
            Time.timeScale = 1;

            //If you are not restarting after finishing play mode, switch back to generation mode.
            if (MazeManager.Instance.playMode && !restarting)
            {
                SwitchMode();
            }
        }

        bool playmode = MazeManager.Instance.playMode;

        //Set the UI to match the mode
        generateEnd.SetActive(!playmode);
        playmodeEnd.SetActive(playmode);

        if (playmode)
        {
            playmodeEndText.text = won ? playmodeWin : playmodeLose;
        }

        endScreenAnimator.SetTrigger("fade");
    }

    public void TogglePlaymodeInstructions()
    {
        //Change the text in the play mode pop-up according to the new mode.
        string newText = MazeManager.Instance.playMode ? enterGenerationMode : enterPlaymode;
        playmodeInstructionsText.text = newText;

        playmodeInstructionsAnimator.SetTrigger("fade");
    }

    public void ToggleSettingsMenu()
    {
        settingsAnimator.SetTrigger("fade");

        if (!MazeManager.Instance.playMode)
        {
            //When activating the settings menu, stop generating a maze. When deactivating it, create a grid with the new settings.
            if (MazeManager.Instance.isGeneratingMaze)
            {
                MazeManager.Instance.StopGeneratingMaze();
            }
            else
            {
                MazeManager.Instance.ClearGrid();
                MazeManager.Instance.CreateGrid();
                generateButton.GetComponent<Image>().sprite = play;
            }
        }
        else //When in playmode, toggle between stopping and starting time.
        {
            float timeScale = Time.timeScale == 0 ? 1 : 0;
            Time.timeScale = timeScale;
        }

        settingsActive = !settingsActive;
    }

    public void UpdateMazesSolved(int numberSolved)
    {
        mazesSolvedText.text = numberSolved.ToString();
    }

    public void UpdateTimer(TimeSpan time)
    {
        //Convert the timespan to a string in the minutes:seconds format.
        timeText.text = string.Format("{0:00}:{1:00}", time.Minutes, time.Seconds);
    }
}