using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GeneralUIManager : MonoBehaviour {

    [SerializeField] private Canvas canvas;
    
    [Header("Main Menu Screen")]
    [SerializeField] private RectTransform tutorialsButton;
    [SerializeField] private RectTransform practiceButton;
    [SerializeField] private RectTransform titlePanel;
    [SerializeField] private RectTransform statsButton;
    [SerializeField] private RectTransform settingsButton;
    [SerializeField] private RectTransform[] difficultyButtons;
    [SerializeField] private RectTransform background;
    [Space]
    [SerializeField] private float backgroundScrollSpeed = 5f;
    [SerializeField] private Vector3 titlePanelPosition;
    [SerializeField] private Vector3 tutorialsButtonPosition;
    [SerializeField] private Vector3 practiceButtonPosition;
    [SerializeField] private Vector3 levelTypeHeaderPosition1;
    [SerializeField] private Vector3[] difficultyButtonPositions;
    [SerializeField] private Vector3 difficultyHeaderPosition;
    [SerializeField] private Vector3 difficultyButtonHidingPosition;
    [SerializeField] private Vector3 levelTypeHeaderPosition2;
    [SerializeField] private Vector3 statsButtonPosition;

    [Space]
    [Space]
    [Header("Level Select Screen")]
    [SerializeField] private RectTransform levelButtons;
    [SerializeField] private RectTransform leftLevelPreviewPanel;
    [SerializeField] private RectTransform rightLevelPreviewPanel;
    [Space]
    [SerializeField] private Vector3 levelButtonsPosition;
    [SerializeField] private Color tutorialsLevelPreviewColor;
    [SerializeField] private Color practiceLevelPreviewColor;
    [SerializeField] private Color selectedTutorialsLevelButtonColor;
    [SerializeField] private Color selectedPracticeLevelButtonColor;
    [SerializeField] private LevelScriptableObject[] easyLevels;
    [SerializeField] private LevelScriptableObject[] mediumLevels;
    [SerializeField] private LevelScriptableObject[] hardLevels;
    [SerializeField] private LevelScriptableObject[] easyTutorials;
    [SerializeField] private LevelScriptableObject[] mediumTutorials;
    [SerializeField] private LevelScriptableObject[] hardTutorials;

    [Space]
    [Space]
    [Header("Settings/Stats Screens")]
    [SerializeField] private RectTransform settingsPanel;
    [Space]
    [SerializeField] private float settingsButtonSpinTime = 0.2f;

    private enum LevelType {Tutorial, Practice}
    private SheetMusicUIManager sheetMusicUIManager;
    private GameUIManager gameUIManager;
    private bool shouldMainMenuBackgroundBeScrolling = true;
    private LevelType currentSelectedLevelType;
    private int currentSelectedDifficulty = -1;
    private RectTransform currentSelectedLevelButtonTransform = null;
    private RectTransform levelPreviewPanel = null;
    private LevelScriptableObject currentLevelSO;
    private IEnumerator backgroundScrollCoroutine;

    void Start() {

        backgroundScrollCoroutine = MainMenuBackgroundScrollCoroutine(background);
        StartCoroutine(backgroundScrollCoroutine); //Scrolling background

        sheetMusicUIManager = FindObjectOfType<SheetMusicUIManager>(); //Get SheetMusicUIManager
        gameUIManager = FindObjectOfType<GameUIManager>(); //Get GameUIManager

        //Put level numbers on all of the level buttons
        levelButtons.gameObject.SetActive(true);
        for(int i = 0; i < levelButtons.childCount; i++) {

            levelButtons.GetChild(i).GetComponent<HoldAndReleaseButton>().GetButtonTransform().GetChild(0).GetComponent<TMP_Text>().text = $"{i + 1}";

        }
        levelButtons.gameObject.SetActive(false);

        //Set the preview panels right off of the screen
        MoveObjectOffScreen(leftLevelPreviewPanel, "horizontal", false, movingTime: 0);
        MoveObjectOffScreen(rightLevelPreviewPanel, "horizontal", true, movingTime: 0);

        MoveObjectOffScreen(settingsPanel, "horizontal", false, movingTime: 0); //Set the settings panel right off of the screen

    }

    /// <summary>
    /// Shifts the UI into the difficulty select screen
    /// </summary>
    /// <param name="buttonTransform"></param>
    public void LevelTypeButtonAction(RectTransform buttonTransform) {

        MoveObjectOffScreen(titlePanel, "horizontal", false);

        //Move off-scren the level type button that wasn't clicked on
        if(buttonTransform == practiceButton) {

            MoveObjectOffScreen(tutorialsButton, "horizontal", true);
            currentSelectedLevelType = LevelType.Practice;

        } else {

            MoveObjectOffScreen(practiceButton, "horizontal", true);
            currentSelectedLevelType = LevelType.Tutorial;

        }

        //Turn the selected button into a header and move it up
        StartCoroutine(SlideObjectCoroutine(buttonTransform, levelTypeHeaderPosition1, callback: () => {

            buttonTransform.Find("X Button").gameObject.SetActive(true);

        }));
        buttonTransform.GetComponent<HoldAndReleaseButton>().DisableButton();

        //Bring out the difficulty buttons
        for(int i = 0; i < 3; i++) {

            StartCoroutine(SlideObjectCoroutine(difficultyButtons[i], difficultyButtonPositions[i]));

        }

        //Set the level preview panel color (the level preview panel's color is dependent on the level type header color)
        Color previewPanelColor = currentSelectedLevelType == LevelType.Tutorial ? tutorialsLevelPreviewColor : practiceLevelPreviewColor;
        SetLevelPreviewPanelColor(leftLevelPreviewPanel, previewPanelColor);
        SetLevelPreviewPanelColor(rightLevelPreviewPanel, previewPanelColor);

    }

    /// <summary>
    /// Returns the UI to the welcome screen
    /// </summary>
    /// <param name="levelTypeButtonTransform"></param>
    public void LevelTypeBackButtonAction(RectTransform levelTypeButtonTransform) {

        for(int i = 0; i < 3; i++) {

            //Move off-screen all the difficulty buttons and set all of them to their button state (as oppposed to their header state)
            MoveObjectOffScreen(difficultyButtons[i], "vertical", false);
            difficultyButtons[i].Find("X Button").gameObject.SetActive(false);
            difficultyButtons[i].GetComponent<HoldAndReleaseButton>().EnableButton();

        }
        //Move the level buttons off of the screen
        MoveObjectOffScreen(levelButtons, "vertical", false);

        //Slide the title panel and the two level type buttons back into their places
        StartCoroutine(SlideObjectCoroutine(titlePanel, titlePanelPosition));
        StartCoroutine(SlideObjectCoroutine(tutorialsButton, tutorialsButtonPosition));
        StartCoroutine(SlideObjectCoroutine(practiceButton, practiceButtonPosition));
        
        //Re-enable the previously disabled button and remove its back button
        levelTypeButtonTransform.GetComponent<HoldAndReleaseButton>().EnableButton();
        levelTypeButtonTransform.Find("X Button").gameObject.SetActive(false);

        //Set the preview panel right off screen
        if(levelPreviewPanel == leftLevelPreviewPanel) {

            MoveObjectOffScreen(levelPreviewPanel, "horizontal", false);

        } else if(levelPreviewPanel == rightLevelPreviewPanel) {

            MoveObjectOffScreen(levelPreviewPanel, "horizontal", true);

        }
        
    }

    /// <summary>
    /// Shifts the UI into the level select screen
    /// </summary>
    /// <param name="buttonTransform"></param>
    public void DifficultyButtonAction(RectTransform buttonTransform) {

        for(int i = 0; i < 3; i++) {

            RectTransform difficultyButton = difficultyButtons[i];
            if(difficultyButton != buttonTransform) {

                //Slide off-screen the difficulty buttons that weren't selected by the user.
                MoveObjectOffScreen(difficultyButton, "horizontal", false, callback: () => {

                    difficultyButton.anchoredPosition3D = difficultyButtonHidingPosition;

                });

            } else {

                //Set the selected difficulty.
                currentSelectedDifficulty = i;

            }

        }

        RectTransform levelTypeHeader = currentSelectedLevelType == LevelType.Tutorial ? tutorialsButton : practiceButton;
        StartCoroutine(SlideObjectCoroutine(levelTypeHeader, levelTypeHeaderPosition2)); //Slide the level type header to the right
        //Slide the difficulty header up and then display its back button.
        StartCoroutine(SlideObjectCoroutine(buttonTransform, difficultyHeaderPosition, callback: () => {

            buttonTransform.Find("X Button").gameObject.SetActive(true);

        }));
        buttonTransform.GetComponent<HoldAndReleaseButton>().DisableButton(); //Disable the difficulty header's button functionality

        //Set the level buttons' colors and hide all unavailable levels.
        Color difficultyColor = GetCurrentDifficultyColor();
        int numLevelsAvailable = 0;
        if(currentSelectedLevelType == LevelType.Tutorial) {

            switch(currentSelectedDifficulty) {

            case 0:
                numLevelsAvailable = easyTutorials.Length;
                break;
            case 1:
                numLevelsAvailable = mediumTutorials.Length;
                break;
            case 2:
                numLevelsAvailable = hardTutorials.Length;
                break;

            }

        } else {

            switch(currentSelectedDifficulty) {

            case 0:
                numLevelsAvailable = easyLevels.Length;
                break;
            case 1:
                numLevelsAvailable = mediumLevels.Length;
                break;
            case 2:
                numLevelsAvailable = hardLevels.Length;
                break;

            }

        }
        for(int i = 0; i < levelButtons.childCount; i++) {

            Transform levelButtonTransform = levelButtons.GetChild(i);

            if(i < numLevelsAvailable) {

                levelButtonTransform.gameObject.SetActive(true);
                levelButtonTransform.GetComponent<HoldAndReleaseButton>().SetColor(difficultyColor);

            } else {

                levelButtonTransform.gameObject.SetActive(false);

            }

        }
        //Slide the level buttons into the UI.
        StartCoroutine(SlideObjectCoroutine(levelButtons, levelButtonsPosition));

    }

    /// <summary>
    /// The action of the back button on the difficulty header
    /// Returns the UI to the difficulty selection screen
    /// </summary>
    public void DifficultyBackButtonAction() {

        for(int i = 0; i < 3; i++) {

            //Slide the difficulty headers back in place, hide their back buttons, and re-enable their button functionality
            StartCoroutine(SlideObjectCoroutine(difficultyButtons[i], difficultyButtonPositions[i]));
            difficultyButtons[i].Find("X Button").gameObject.SetActive(false);
            difficultyButtons[i].GetComponent<HoldAndReleaseButton>().EnableButton();

        }

        MoveObjectOffScreen(levelButtons, "vertical", false); //Move the level buttons off of the screen

        //Move the level type header left, back into its original position
        if(currentSelectedLevelType == LevelType.Tutorial) {

            StartCoroutine(SlideObjectCoroutine(tutorialsButton, levelTypeHeaderPosition1));

        } else {

            StartCoroutine(SlideObjectCoroutine(practiceButton, levelTypeHeaderPosition1));

        }

        //Set the preview panel right off screen
        if(levelPreviewPanel == leftLevelPreviewPanel) {

            MoveObjectOffScreen(levelPreviewPanel, "horizontal", false);

        } else if(levelPreviewPanel == rightLevelPreviewPanel) {

            MoveObjectOffScreen(levelPreviewPanel, "horizontal", true);

        }

    }

    public void LevelButtonAction(int levelNumber) {

        RectTransform levelButtonTransform = (RectTransform) levelButtons.GetChild(levelNumber - 1);
        
        if(levelButtonTransform == currentSelectedLevelButtonTransform) {

            LevelPreviewBackButtonAction();
            return;

        }

        //If the level button is on the right side of the screen, then bring in the left preview panel, and vice versa
        if(levelNumber % 9 > 4 || levelNumber % 9 == 0) {

            levelPreviewPanel = leftLevelPreviewPanel;

            if(rightLevelPreviewPanel.gameObject.activeSelf) {

                MoveObjectOffScreen(rightLevelPreviewPanel, "horizontal", true);

            }

        } else {

            levelPreviewPanel = rightLevelPreviewPanel;

            if(leftLevelPreviewPanel.gameObject.activeSelf) {

                MoveObjectOffScreen(leftLevelPreviewPanel, "horizontal", false);

            }

        }

        //Highlight the button
        levelButtonTransform.GetComponent<HoldAndReleaseButton>().SetColor(currentSelectedLevelType == LevelType.Tutorial ? selectedTutorialsLevelButtonColor : selectedPracticeLevelButtonColor);

        //De-highlight the previously selected button
        if(currentSelectedLevelButtonTransform != null) {

            currentSelectedLevelButtonTransform.GetComponent<HoldAndReleaseButton>().SetColor(GetCurrentDifficultyColor());

        }
        currentSelectedLevelButtonTransform = levelButtonTransform; //Update the selected button

        //Bring out the preview panel
        Vector3 levelPreviewPanelPosition;
        if(levelPreviewPanel == leftLevelPreviewPanel) {

            levelPreviewPanelPosition = new Vector3(0, 0, 0);

        } else {

            levelPreviewPanelPosition = new Vector3(0, 0, 0);

        }
        StartCoroutine(SlideObjectCoroutine(levelPreviewPanel, levelPreviewPanelPosition));

        //Set the level number text
        levelPreviewPanel.Find("Level Number Text").GetComponent<TMP_Text>().text = $"{levelNumber}";


        //Display the level-specific details
        currentLevelSO = GetLevelScriptableObject(currentSelectedLevelType == LevelType.Tutorial, currentSelectedDifficulty, levelNumber); //Get the SO

        //Display the level description
        string difficultyStr = currentSelectedDifficulty switch {

            0 => "Easy",
            1 => "Medium",
            2 => "Hard",
            _ => ""

        };
        string description = currentLevelSO == null ? $"{difficultyStr} {(currentSelectedLevelType == LevelType.Tutorial ? "tutorial" : "practice")} level {levelNumber} is coming soon." : currentLevelSO.GetLevelDescription();
        levelPreviewPanel.Find("Text Panel").GetChild(0).GetComponent<TMP_Text>().text = description;

        //Display the music notation
        if(currentLevelSO != null) {

            Transform musicNotationPanel = levelPreviewPanel.Find("Music Notation Panel");
            musicNotationPanel.gameObject.SetActive(true);
            while(musicNotationPanel.childCount != 0) { //Clear the current music notation (if any)

                DestroyImmediate(musicNotationPanel.GetChild(0).gameObject);

            }
            sheetMusicUIManager.CreateFullMusicNotationUI(Measure.ReadTextInput(currentLevelSO.GetLevelContents()), (RectTransform) musicNotationPanel, size: 0.65f, musicNotationBoundsInset: 10);

            levelPreviewPanel.Find("Cancel & Play Buttons Panel").Find("Play Button").GetComponent<HoldAndReleaseButton>().EnableButton();

        } else {

            levelPreviewPanel.Find("Music Notation Panel").gameObject.SetActive(false);
            levelPreviewPanel.Find("Cancel & Play Buttons Panel").Find("Play Button").GetComponent<HoldAndReleaseButton>().LockButton();

        }

    }

    public void LevelPreviewBackButtonAction() {

        currentSelectedLevelButtonTransform.GetComponent<HoldAndReleaseButton>().SetColor(GetCurrentDifficultyColor()); //De-highlighting the button
        currentSelectedLevelButtonTransform = null;
        //Set the preview panel right off screen
        if(levelPreviewPanel == leftLevelPreviewPanel) {

            MoveObjectOffScreen(levelPreviewPanel, "horizontal", false);

        } else if(levelPreviewPanel == rightLevelPreviewPanel) {

            MoveObjectOffScreen(levelPreviewPanel, "horizontal", true);

        }

        Transform musicNotationPanel = levelPreviewPanel.Find("Music Notation Panel");
        for(int i = 0; i < musicNotationPanel.childCount; i++) {

            Destroy(musicNotationPanel.GetChild(i).gameObject);

        }

    }

    public void PlayButtonAction() {

        currentSelectedLevelButtonTransform = null;
        gameUIManager.SetLevelSO(currentLevelSO);

        MoveObjectOffScreen(currentSelectedLevelType == LevelType.Tutorial ? tutorialsButton : practiceButton, "horizontal", false);
        MoveObjectOffScreen(difficultyButtons[currentSelectedDifficulty], "horizontal", false);
        MoveObjectOffScreen(levelButtons, "vertical", false);
        if(levelPreviewPanel == leftLevelPreviewPanel) {

            MoveObjectOffScreen(levelPreviewPanel, "horizontal", false);

        } else {

            MoveObjectOffScreen(levelPreviewPanel, "horizontal", true);

        }
        MoveObjectOffScreen(statsButton, "vertical", true);
        StopCoroutine(backgroundScrollCoroutine);
        MoveObjectOffScreen(background, "vertical", false, movingTime: 1, callback: () => {

            gameUIManager.SetupPregameScreen();

        });

    }

    public void SettingsButtonAction() {

        if(settingsPanel.gameObject.activeSelf) {

            MoveObjectOffScreen(settingsPanel, "horizontal", false, movingTime: settingsButtonSpinTime, useScaledTime: false);
            StartCoroutine(RotateObjectCoroutine(settingsButton, new Vector3(0, 0, 90), movingTime: settingsButtonSpinTime, useScaledTime: false, callback: () => {

                settingsButton.localEulerAngles = Vector3.zero;
                Time.timeScale = 1;

            }));

        } else {

            Time.timeScale = 0;
            StartCoroutine(SlideObjectCoroutine(settingsPanel, Vector3.zero, movingTime: settingsButtonSpinTime, useScaledTime: false));
            StartCoroutine(RotateObjectCoroutine(settingsButton, new Vector3(0, 0, -90), movingTime: settingsButtonSpinTime, useScaledTime: false, callback: () => {

                settingsButton.localEulerAngles = Vector3.zero;

            }));

        }

    }

    public void ResetupLevelSelectScreen() {

        RectTransform levelTypeHeader = currentSelectedLevelType == LevelType.Tutorial ? tutorialsButton : practiceButton;
        RectTransform difficultyHeader = difficultyButtons[currentSelectedDifficulty];

        StartCoroutine(SlideObjectCoroutine(levelTypeHeader, levelTypeHeaderPosition2));
        StartCoroutine(SlideObjectCoroutine(difficultyHeader, difficultyHeaderPosition));
        StartCoroutine(SlideObjectCoroutine(levelButtons, levelButtonsPosition));
        StartCoroutine(SlideObjectCoroutine(statsButton, statsButtonPosition));
        LevelButtonAction(currentLevelSO.GetLevelNumber());
        background.gameObject.SetActive(true);
        StartCoroutine(backgroundScrollCoroutine);

    }

    public void MoveObjectOffScreen(RectTransform transform, string axis, bool moveDirectionIsPositive, float movingTime = 0.2f, bool useScaledTime = true, Action callback = null) {

        Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(canvas.transform, transform);
        Vector3 extents = bounds.extents;
        Rect canvasRect = canvas.GetComponent<RectTransform>().rect;

        Vector3 finalPosition;
        if(axis == "vertical") {

            if(moveDirectionIsPositive) {

                finalPosition = new Vector3(transform.localPosition.x, canvasRect.height / 2f + extents.y, transform.localPosition.z);

            } else {

                finalPosition = new Vector3(transform.localPosition.x, -canvasRect.height / 2f - extents.y, transform.localPosition.z);

            }

        } else if(axis == "horizontal") {

            if(moveDirectionIsPositive) {

                finalPosition = new Vector3(canvasRect.width / 2f + extents.x, transform.localPosition.y, transform.localPosition.z);

            } else {

                finalPosition = new Vector3(-canvasRect.width / 2f - extents.x, transform.localPosition.y, transform.localPosition.z);

            }

        } else {

            throw new ArgumentException($"GeneralUIManager: Axis \"{axis}\" is not valid");

        }

        StartCoroutine(SlideObjectCoroutine(transform, finalPosition, movingTime: movingTime, endActiveState: false, useAnchoredPosition: false, useScaledTime: useScaledTime, callback: callback));

    }

    public IEnumerator SlideObjectCoroutine(RectTransform transform, Vector3 finalPosition, float movingTime = 0.2f, bool endActiveState = true, bool useAnchoredPosition = true, bool useScaledTime = true, Action callback = null) {

        if(movingTime < 0) {

            throw new ArgumentException($"GeneralUIManager: Moving time {movingTime} is not valid (must be non-negative)");

        }

        transform.gameObject.SetActive(true);

        if(movingTime == 0) {

            if(useAnchoredPosition) {

                transform.anchoredPosition3D = finalPosition;

            } else {

                transform.localPosition = finalPosition;

            }
            transform.gameObject.SetActive(endActiveState);
            
            yield break; //Exit the coroutine

        }

        Vector3 beginPosition = useAnchoredPosition ? transform.anchoredPosition3D : transform.localPosition;

        if(useAnchoredPosition && useScaledTime) {

            //Use anchoredPosition3D and Time.time
            float beginTime = Time.time;
            while(Time.time - beginTime <= movingTime) {

                transform.anchoredPosition3D = Vector3.Lerp(beginPosition, finalPosition, (Time.time - beginTime) / movingTime);
                yield return null;

            }

            transform.anchoredPosition3D = finalPosition;

        } else if(useAnchoredPosition && !useScaledTime) {

            //Use anchoredPosition3D and Time.unscaledTime
            float beginTime = Time.unscaledTime;
            while(Time.unscaledTime - beginTime <= movingTime) {

                transform.anchoredPosition3D = Vector3.Lerp(beginPosition, finalPosition, (Time.unscaledTime - beginTime) / movingTime);
                yield return null;

            }

            transform.anchoredPosition3D = finalPosition;

        } else if(!useAnchoredPosition && useScaledTime) {

            //Use localPosition and Time.time
            float beginTime = Time.time;
            while(Time.time - beginTime <= movingTime) {

                transform.localPosition = Vector3.Lerp(beginPosition, finalPosition, (Time.time - beginTime) / movingTime);
                yield return null;

            }

            transform.localPosition = finalPosition;

        } else if(!useAnchoredPosition && !useScaledTime) {

            //Use localPosition and Time.unscaledTime
            float beginTime = Time.unscaledTime;
            while(Time.unscaledTime - beginTime <= movingTime) {

                transform.localPosition = Vector3.Lerp(beginPosition, finalPosition, (Time.unscaledTime - beginTime) / movingTime);
                yield return null;

            }

            transform.localPosition = finalPosition;

        }

        transform.gameObject.SetActive(endActiveState);

        callback?.Invoke();

    }

    public IEnumerator RotateObjectCoroutine(Transform transform, Vector3 finalRotation, float movingTime = 0.2f, bool useScaledTime = true, Action callback = null) {

        if(movingTime < 0) {

            throw new ArgumentException($"GeneralUIManager: Rotation time {movingTime} is not valid (must be non-negative)");

        }

        transform.gameObject.SetActive(true);

        if(movingTime == 0) {

            transform.localEulerAngles = finalRotation;
            yield break;

        }

        Vector3 beginRotation = transform.localEulerAngles;
        //Make all angles in beginRotation between the values of -180 and 180
        beginRotation = new Vector3(beginRotation.x > 180 ? beginRotation.x - 360 : beginRotation.x, beginRotation.y > 180 ? beginRotation.y - 360 : beginRotation.y, beginRotation.z > 180 ? beginRotation.z - 360 : beginRotation.z);
        if(useScaledTime) {

            float beginTime = Time.time;
            while(Time.time - beginTime <= movingTime) {

                transform.localEulerAngles = Vector3.Lerp(beginRotation, finalRotation, (Time.time - beginTime) / movingTime);
                
                yield return null;

            }

        } else {

            float beginTime = Time.unscaledTime;
            while(Time.unscaledTime - beginTime <= movingTime) {

                transform.localEulerAngles = Vector3.Lerp(beginRotation, finalRotation, (Time.unscaledTime - beginTime) / movingTime);
                yield return null;

            }

        }

        transform.localEulerAngles = finalRotation;

        callback?.Invoke();

    }

    private IEnumerator MainMenuBackgroundScrollCoroutine(Transform mainMenuBackground) {

        float mainMenuBackgroundHeight = mainMenuBackground.GetComponent<RectTransform>().rect.height;
        while(shouldMainMenuBackgroundBeScrolling) {

            mainMenuBackground.Translate(0, backgroundScrollSpeed * mainMenuBackgroundHeight / 100f * Time.deltaTime, 0);
            if(mainMenuBackground.localPosition.y > mainMenuBackgroundHeight / 4f) {

                mainMenuBackground.localPosition = Vector3.up * -mainMenuBackgroundHeight / 4f;

            }
            yield return null;

        }

    }

    private LevelScriptableObject GetLevelScriptableObject(bool isTutorial, int difficulty, int number) {

        try {

            switch(difficulty) {

                case 0:
                    if(isTutorial) {

                        return easyTutorials[number - 1];

                    } else {

                        return easyLevels[number - 1];

                    }
                case 1:
                    if(isTutorial) {

                        return mediumTutorials[number - 1];

                    } else {

                        return mediumLevels[number - 1];

                    }
                case 2:
                    if(isTutorial) {

                        return hardTutorials[number - 1];

                    } else {

                        return hardLevels[number - 1];

                    }
                default: throw new ArgumentException();

            }

        } catch(IndexOutOfRangeException) {

            return null;

        }

    }

    private Color GetCurrentDifficultyColor() {

        Color difficultyColor = difficultyButtons[currentSelectedDifficulty].GetComponent<HoldAndReleaseButton>().GetButtonTransform().GetComponent<Image>().color;
        Color.RGBToHSV(difficultyColor, out float hue, out float sat, out float val);
        return Color.HSVToRGB(hue, sat - 0.45f, val);

    }

    private void SetLevelPreviewPanelColor(RectTransform levelPreviewPanelParam, Color previewPanelColor) {

        Color.RGBToHSV(previewPanelColor, out float hue, out float sat, out float val);
        Transform topBarShadow = levelPreviewPanelParam.Find("Top Bar Shadow");
        Transform topBar = levelPreviewPanelParam.Find("Top Bar");
        Transform textPanelShadow = levelPreviewPanelParam.Find("Text Panel Shadow");
        Transform textPanel = levelPreviewPanelParam.Find("Text Panel");
        Transform cancelAndPlayButtonsPanel = levelPreviewPanelParam.Find("Cancel & Play Buttons Panel");
        Transform playTriangle = cancelAndPlayButtonsPanel.Find("Play Button").GetComponent<HoldAndReleaseButton>().GetButtonTransform().GetChild(0);
        Transform cancelButton = cancelAndPlayButtonsPanel.Find("Cancel Button");
        topBar.GetComponent<Image>().color = previewPanelColor;
        textPanel.GetComponent<Image>().color = previewPanelColor;
        topBarShadow.GetComponent<Image>().color = Color.HSVToRGB(hue, sat - 0.3f, val);
        textPanelShadow.GetComponent<Image>().color = Color.HSVToRGB(hue, sat - 0.3f, val);
        cancelAndPlayButtonsPanel.GetComponent<Image>().color = previewPanelColor;
        playTriangle.GetComponent<Image>().color = previewPanelColor;
        cancelButton.GetComponent<HoldAndReleaseButton>().SetColor(Color.HSVToRGB(hue, sat - 0.3f, val));

    }

}
