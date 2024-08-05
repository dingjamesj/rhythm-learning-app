using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GeneralUIManager : MonoBehaviour {

    [SerializeField] private Canvas canvas;

    [Space]
    [Space]
    [Header("Main Menu Screen")]
    [SerializeField] private RectTransform learnButton;
    [SerializeField] private RectTransform practiceButton;
    [SerializeField] private RectTransform titlePanel;
    [SerializeField] private RectTransform titleTextTransform;
    [SerializeField] private RectTransform statsButton;
    [SerializeField] private RectTransform settingsButton;
    [SerializeField] private RectTransform[] difficultyButtons;
    [Space]
    [SerializeField] private Vector3 titlePanelDefaultPosition = new(0, 540, 0);
    [SerializeField] private Vector3 titleTextPosition = new(0, 469, 0);
    [SerializeField] private Vector3 titlePanelLevelSelectPosition = new(0, 850, 0);
    [SerializeField] private Vector3 learnButtonPosition = new(0, 840, 0);
    [SerializeField] private Vector3 practiceButtonPosition = new(0, 490, 0);
    [SerializeField] private Vector3 levelTypeHeaderPosition = new(0, -100, 0);
    [SerializeField] private Vector3[] difficultyButtonPositions = new Vector3[] { new(0, 850, 0), new(0, 550, 0), new(0, 250, 0) };
    [SerializeField] private Vector3 difficultyHeaderPosition = new(0, -375, 0);
    [SerializeField] private Vector3 statsButtonPosition = new(-175, 154, 0);
    [SerializeField] private Vector3 settingsButtonPosition = new(175, 165, 0);

    [Space]
    [Space]
    [Header("Level Select Screen")]
    [SerializeField] private RectTransform levelButtons;
    [SerializeField] private RectTransform levelPreviewPanel;
    [SerializeField] private RectTransform levelPreviewBottomPanel;
    [Space]
    [SerializeField] private Vector3 levelButtonsPosition = new(0, 685, 0);
    [SerializeField] private Vector3 levelPreviewPanelPosition = new(0, 680, 0);
    [SerializeField] private Color selectedLevelButtonColor = new(1, 0.7411765f, 0.2196078f, 1);
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
    [SerializeField] private float settingsButtonSpinTime = 1/3f;

    private enum LevelType {Tutorial, Practice}
    private SheetMusicUIManager sheetMusicUIManager;
    private GameUIManager gameUIManager;
    private LevelType currentSelectedLevelType;
    private int currentSelectedDifficulty = -1;
    private RectTransform currentSelectedLevelButtonTransform = null;
    private LevelScriptableObject currentLevelSO;

    void Start() {

        sheetMusicUIManager = FindObjectOfType<SheetMusicUIManager>(); //Get SheetMusicUIManager
        gameUIManager = FindObjectOfType<GameUIManager>(); //Get GameUIManager

        //Put level numbers on all of the level buttons
        levelButtons.gameObject.SetActive(true);
        for(int i = 0; i < levelButtons.childCount; i++) {

            levelButtons.GetChild(i).GetComponent<HoldAndReleaseButton>().GetButtonTransform().GetChild(0).GetComponent<TMP_Text>().text = $"{i + 1}";

        }
        levelButtons.gameObject.SetActive(false);

        MoveObjectOffScreen(settingsPanel, "horizontal", false, movingTime: 0); //Set the settings panel right off of the screen

    }

    /// <summary>
    /// Shifts the UI into the difficulty select screen
    /// </summary>
    /// <param name="buttonTransform"></param>
    public void LevelTypeButtonAction(RectTransform buttonTransform) {

        MoveObjectOffScreen(titleTextTransform, "vertical", true);
        MoveObjectOffScreen(settingsButton, "vertical", false);
        MoveObjectOffScreen(statsButton, "vertical", false);

        //Move off-screen the level type button that wasn't clicked on
        if(buttonTransform == practiceButton) {

            MoveObjectOffScreen(learnButton, "vertical", false);
            currentSelectedLevelType = LevelType.Practice;

        } else {

            MoveObjectOffScreen(practiceButton, "vertical", false);
            currentSelectedLevelType = LevelType.Tutorial;

        }

        //Turn the selected button into a header and move it up
        StartCoroutine(SlideObjectCoroutine(buttonTransform, levelTypeHeaderPosition + titlePanelLevelSelectPosition, useAnchoredPosition: false, callback: () => {

            buttonTransform.Find("Back Button").gameObject.SetActive(true);

        }));
        buttonTransform.GetComponent<HoldAndReleaseButton>().DisableButton();

        //Move the title panel up and remove the white bars
        StartCoroutine(SlideObjectCoroutine(titlePanel, titlePanelLevelSelectPosition));
        //Remove the white bars
        titlePanel.GetChild(0).gameObject.SetActive(false);

        //Bring out the difficulty buttons
        for(int i = 0; i < 3; i++) {

            StartCoroutine(SlideObjectCoroutine(difficultyButtons[i], difficultyButtonPositions[i]));

        }
        /*
        //Set the level preview panel color (the level preview panel's color is dependent on the level type header color)
        Color previewPanelColor = currentSelectedLevelType == LevelType.Tutorial ? tutorialsLevelPreviewColor : practiceLevelPreviewColor;
        SetLevelPreviewPanelColor(leftLevelPreviewPanel, previewPanelColor);
        SetLevelPreviewPanelColor(rightLevelPreviewPanel, previewPanelColor);
        */

    }

    /// <summary>
    /// Returns the UI to the welcome screen
    /// </summary>
    /// <param name="levelTypeButtonTransform"></param>
    public void LevelTypeBackButtonAction(RectTransform levelTypeButtonTransform) {

        for(int i = 0; i < 3; i++) {

            //Move off-screen all the difficulty buttons and set all of them to their button state (as oppposed to their header state)
            MoveObjectOffScreen(difficultyButtons[i], "vertical", false);
            difficultyButtons[i].Find("Back Button").gameObject.SetActive(false);
            difficultyButtons[i].GetComponent<HoldAndReleaseButton>().EnableButton();

        }

        //Move the level buttons off of the screen
        if(levelButtons.gameObject.activeSelf) {

            MoveObjectOffScreen(levelButtons, "vertical", false);

        }

        //Slide the title panel back into place
        StartCoroutine(SlideObjectCoroutine(titlePanel, titlePanelDefaultPosition));
        //Re-activate the white bars
        titlePanel.GetChild(0).gameObject.SetActive(true);

        StartCoroutine(SlideObjectCoroutine(titleTextTransform, titleTextPosition));
        StartCoroutine(SlideObjectCoroutine(learnButton, learnButtonPosition));
        StartCoroutine(SlideObjectCoroutine(practiceButton, practiceButtonPosition));
        
        //Turn the header back into a button
        levelTypeButtonTransform.GetComponent<HoldAndReleaseButton>().EnableButton();
        levelTypeButtonTransform.Find("Back Button").gameObject.SetActive(false);

        StartCoroutine(SlideObjectCoroutine(statsButton, statsButtonPosition));
        StartCoroutine(SlideObjectCoroutine(settingsButton, settingsButtonPosition));

        if(levelPreviewPanel.gameObject.activeSelf) {

            LevelPreviewBackButtonAction();

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
                MoveObjectOffScreen(difficultyButton, "vertical", false);

            } else {

                //Set the selected difficulty.
                currentSelectedDifficulty = i;

            }

        }

        //Slide the difficulty header up and then display its back button.
        StartCoroutine(SlideObjectCoroutine(buttonTransform, difficultyHeaderPosition + titlePanel.localPosition, useAnchoredPosition: false, callback: () => {

            buttonTransform.Find("Back Button").gameObject.SetActive(true);

        }));
        buttonTransform.GetComponent<HoldAndReleaseButton>().DisableButton(); //Disable the difficulty header's button functionality

        //Find the number of available levels.
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

        //Hide all unavailable levels and set the color.
        Color normalColor = GetCurrentDifficultyColor();
        Color levelButtonColor = DesaturateColor(normalColor, 0.3f);
        for(int i = 0; i < levelButtons.childCount; i++) {

            Transform levelButtonTransform = levelButtons.GetChild(i);

            if(i < numLevelsAvailable) {

                levelButtonTransform.gameObject.SetActive(true);
                levelButtonTransform.GetComponent<HoldAndReleaseButton>().SetColor(levelButtonColor);

            } else {

                levelButtonTransform.gameObject.SetActive(false);

            }

        }

        //Set the color of the level preview panel.
        Color desaturatedColor = DesaturateColor(normalColor, 0.15f);
        levelPreviewPanel.GetComponent<Image>().color = normalColor;
        levelPreviewBottomPanel.GetComponent<Image>().color = levelButtonColor;
        levelPreviewBottomPanel.Find("Bar").GetComponent<Image>().color = normalColor;

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
            difficultyButtons[i].Find("Back Button").gameObject.SetActive(false);
            difficultyButtons[i].GetComponent<HoldAndReleaseButton>().EnableButton();

        }

        MoveObjectOffScreen(levelButtons, "vertical", false); //Move the level buttons off of the screen

        if(levelPreviewPanel.gameObject.activeSelf) {

            LevelPreviewBackButtonAction();

        }

    }

    public void LevelButtonAction(int levelNumber) {

        RectTransform levelButtonTransform = (RectTransform) levelButtons.GetChild(levelNumber - 1);

        //Highlight the button
        levelButtonTransform.GetComponent<HoldAndReleaseButton>().SetColor(selectedLevelButtonColor);

        //De-highlight the previously selected button
        if(currentSelectedLevelButtonTransform != null) {

            currentSelectedLevelButtonTransform.GetComponent<HoldAndReleaseButton>().SetColor(GetCurrentDifficultyColor());

        }
        currentSelectedLevelButtonTransform = levelButtonTransform; //Update the selected button

        //Bring in the level preview panel
        StartCoroutine(SlideObjectCoroutine(levelPreviewPanel, levelPreviewPanelPosition));

        //Set the level number text
        levelPreviewBottomPanel.Find("Level Number Text").GetComponent<TMP_Text>().text = $"{levelNumber}";

        //Display the level-specific details
        currentLevelSO = GetLevelScriptableObject(currentSelectedLevelType == LevelType.Tutorial, currentSelectedDifficulty, levelNumber); //Get the SO

        //Display the level description
        levelPreviewBottomPanel.Find("Level Title Text").GetComponent<TMP_Text>().text = currentLevelSO.GetLevelDescription();

        //Display the music notation
        Transform musicNotationContainer = levelPreviewPanel.Find("Music Notation Container");
        musicNotationContainer.gameObject.SetActive(true);
        while(musicNotationContainer.childCount != 0) { //Clear the current music notation (if any)

            DestroyImmediate(musicNotationContainer.GetChild(0).gameObject);

        }
        sheetMusicUIManager.CreateFullMusicNotationUI(Measure.ReadTextInput(currentLevelSO.GetLevelContents()), (RectTransform) musicNotationContainer, size: 1f, musicNotationBoundsInset: 10);

    }

    public void LevelPreviewBackButtonAction() {

        currentSelectedLevelButtonTransform.GetComponent<HoldAndReleaseButton>().SetColor(DesaturateColor(GetCurrentDifficultyColor(), 0.3f)); //De-highlighting the button
        currentSelectedLevelButtonTransform = null;
        MoveObjectOffScreen(levelPreviewPanel, "vertical", false);

        Transform musicNotationContainer = levelPreviewPanel.Find("Music Notation Container");
        for(int i = 0; i < musicNotationContainer.childCount; i++) {

            Destroy(musicNotationContainer.GetChild(i).gameObject);

        }

    }

    public void PlayButtonAction() {

        currentSelectedLevelButtonTransform = null;
        gameUIManager.SetLevelSO(currentLevelSO);

        MoveObjectOffScreen(currentSelectedLevelType == LevelType.Tutorial ? learnButton : practiceButton, "vertical", true, movingTime: 0.5f);
        MoveObjectOffScreen(difficultyButtons[currentSelectedDifficulty], "vertical", true, movingTime: 0.5f);
        MoveObjectOffScreen(levelButtons, "vertical", false, movingTime: 0.5f);
        MoveObjectOffScreen(levelPreviewPanel, "vertical", false, movingTime: 0.5f);
        MoveObjectOffScreen(titlePanel, "vertical", true, movingTime: 0.5f, callback: () => {

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

        RectTransform levelTypeHeader = currentSelectedLevelType == LevelType.Tutorial ? learnButton : practiceButton;
        RectTransform difficultyHeader = difficultyButtons[currentSelectedDifficulty];

        StartCoroutine(SlideObjectCoroutine(titlePanel, titlePanelLevelSelectPosition));
        StartCoroutine(SlideObjectCoroutine(levelTypeHeader, levelTypeHeaderPosition + titlePanelLevelSelectPosition, useAnchoredPosition: false));
        StartCoroutine(SlideObjectCoroutine(difficultyHeader, difficultyHeaderPosition + titlePanelLevelSelectPosition, useAnchoredPosition: false));
        StartCoroutine(SlideObjectCoroutine(levelButtons, levelButtonsPosition));
        LevelButtonAction(currentLevelSO.GetLevelNumber());

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

        return difficultyButtons[currentSelectedDifficulty].GetComponent<HoldAndReleaseButton>().GetButtonTransform().GetComponent<Image>().color;

    }

    private Color DesaturateColor(Color color, float desaturation) {

        Color.RGBToHSV(color, out float hue, out float sat, out float val);
        return Color.HSVToRGB(hue, sat - desaturation, val);

    }

}
