using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameUIManager : MonoBehaviour {

    [SerializeField] private Canvas canvas;
    [Space]
    [Space]
    [Header("Prefabs")]
    [SerializeField] private GameObject beatCounterPrefab;
    [SerializeField] private GameObject hollowNode;
    [SerializeField] private GameObject connector;
    [SerializeField] private GameObject tapIndicator;
    [SerializeField] private GameObject ghostTapIndicator;

    [Space]
    [Space]
    [Header("General")]
    [SerializeField] private RectTransform metronomePanel;
    [SerializeField] private RectTransform beatCounterPanel;
    [SerializeField] private RectTransform beatCountersHidingPanel;
    [SerializeField] private RectTransform musicNotationPanel;
    [SerializeField] private RectTransform showGuideButton;
    [SerializeField] private RectTransform metronomePendulum;
    [SerializeField] private RectTransform playStopButton;
    [SerializeField] private RectTransform exitLevelPanel;
    [SerializeField] private RectTransform removeGuidesButton;
    [Space]
    [SerializeField] private Vector3 metronomePanelPosition = new(0, -400, 0);
    [SerializeField] private Vector3 musicNotationPanelPosition = new(0, -1254, 0);
    [SerializeField] private Vector3 beatCounterPanelPosition = new(220, -150, 0);
    [SerializeField] private Vector3 showGuideButtonPosition = new(0, 235, 0);
    [SerializeField] private Vector3 guidePanelPosition = new(0, 220, 0);
    [Space]
    [SerializeField] private float counterTransformHeight = 75;
    [SerializeField] private float counterTransformSpacing = 0.15f;
    [SerializeField] private float counterTransformGroupSpacing = 0.5f;
    [SerializeField] private float counterTransformRowSpacing = 0.25f;
    [SerializeField] private Color defaultBeatCounterColor = new(0.7490196f, 0.7607843f, 0.9960784f, 1);
    [SerializeField] private Color highlightedBeatCounterColor = new(1, 0.6509804f, 0.1882353f, 1);
    [SerializeField] private float maximumMetronomeAngle = 60;

    [Space]
    [Space]
    [Header("Gameplay With Guides")]
    [SerializeField] private RectTransform guidePanel;
    [SerializeField] private RectTransform nodesContainer;
    [SerializeField] private RectTransform guideMusicNotationContainer;
    [SerializeField] private RectTransform tapIndicatorContainer;
    [Space]
    [SerializeField] private float maxNodeSeparationPerBeat = 350;
    [SerializeField] private float minNodeSeparationPerBeat = 200;
    [SerializeField] private float tapTimeLenienceInBeats = 0.1f; //The fact that lenience is in beats and not seconds makes it depend on the tempo
    [SerializeField] private float connectorBeamHeightAsFractionOfNodeDiameter = 0.3f;
    [SerializeField] private float guideRowSeparation = 45;
    [SerializeField] private float guideMusicNotationHoverDistance = 8;
    [SerializeField] private float guideMusicNotationSize = 0.5f;
    [SerializeField] private float tapIndicatorYSeparation = 2;
    [SerializeField] private float tapIndicatorSize = 0.75f;
    [SerializeField] private float removeGuidesButtonSeparation = 80;
    [SerializeField] private Color unreachedGuideColor = new(0.2235294f, 0.2588235f, 1, 1);
    [SerializeField] private Color reachedGuideColor = new(1, 0.6509804f, 0.1882353f, 1);
    [SerializeField] private Color unreachedGuideMusicNotationColor = new(0, 0.03529412f, 0.7490196f, 1);
    [SerializeField] private Color reachedGuideMusicNotationColor = new(0.7450981f, 0.427451f, 0, 1);
    [SerializeField] private Color correctTapIndicatorColor = new(0.2235294f, 0.4980392f, 1, 1);
    [SerializeField] private Color incorrectTapIndicatorColor = new(1, 0.3176471f, 0.2901961f, 1);
    [SerializeField] private Material guideMaterial;

    [Space]
    [Space]
    [Header("Gameplay Without Guides")]
    [SerializeField] private RectTransform tapAccuracyDiagramPanel;

    [Space]
    [Space]
    [Header("Results")]
    [SerializeField] private RectTransform resultsPanel;
    [SerializeField] private RectTransform tapAccuracyTextTransform;
    [SerializeField] private RectTransform retryButton;
    [SerializeField] private RectTransform retryWithoutGuidesButton;
    [SerializeField] private RectTransform nextLevelButton;
    [Space]
    [SerializeField] private float resultsPanelShowingDelay = 1.35f;
    [SerializeField] private Color defaultUIColor = new(0.2235294f, 0.2588235f, 1, 1);
    [SerializeField] private Color highlightedUIColor = new(1, 0.7484716f, 0.2f, 1);


    private GeneralUIManager generalUIManager;
    private SheetMusicUIManager sheetMusicUIManager;
    private LevelScriptableObject levelSO;
    private Measure[] measures;
    private int tempo;
    private float nodeSeparationPerBeat; //The separation of nodes for the current guide, in pixels per beat
    private float metronomeBeginTime;

    private bool noteTappingIsOn = false;
    private bool noteHasBeenTapped = false;
    private bool mouseIsDown = false;
    private int numAccurateTapIndicators;
    private int numTapIndicators;
    private int tapTimeWindowIndex = 0;
    private Vector3 colorBorderPosition;
    private Vector3 lastNodePosition;
    private List<TapTimeWindow> tapTimeWindows;

    private IEnumerator gameplaySequenceCoroutine = null;
    private IEnumerator metronomeCoroutine = null;
    private IEnumerator nodeGuideCoroutine = null;

    struct TapTimeWindow {

        public float begin;
        public float end;

        public TapTimeWindow(float begin, float end) {

            this.begin = begin;
            this.end = end;

        }

    }

    void Start() {
        
        generalUIManager = FindObjectOfType<GeneralUIManager>();
        sheetMusicUIManager = FindObjectOfType<SheetMusicUIManager>();

    }

    void Update() {

        if(noteTappingIsOn) {

            if(Input.GetMouseButtonDown(0) && !mouseIsDown) {

                mouseIsDown = true;
                numTapIndicators++;

                if(Time.time >= tapTimeWindows[tapTimeWindowIndex].begin && Time.time <= tapTimeWindows[tapTimeWindowIndex].end && !noteHasBeenTapped) {

                    numAccurateTapIndicators++;
                    noteHasBeenTapped = true;
                    RectTransform tapIndicator = Instantiate(this.tapIndicator, tapIndicatorContainer).GetComponent<RectTransform>();
                    tapIndicator.name = "Correct Tap Indicator";
                    tapIndicator.GetComponent<Image>().color = correctTapIndicatorColor;
                    tapIndicator.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, tapIndicatorSize * tapTimeLenienceInBeats * nodeSeparationPerBeat * 2);
                    tapIndicator.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, tapIndicatorSize * tapTimeLenienceInBeats * nodeSeparationPerBeat * 2);
                    tapIndicator.localPosition = colorBorderPosition + Vector3.down * (tapIndicatorYSeparation + tapTimeLenienceInBeats * nodeSeparationPerBeat + tapIndicator.rect.height / 2);

                } else {

                    RectTransform tapIndicator = Instantiate(this.tapIndicator, tapIndicatorContainer).GetComponent<RectTransform>();
                    tapIndicator.name = "Incorrect Tap Indicator";
                    tapIndicator.GetComponent<Image>().color = incorrectTapIndicatorColor;
                    tapIndicator.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, tapIndicatorSize * tapTimeLenienceInBeats * nodeSeparationPerBeat * 2);
                    tapIndicator.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, tapIndicatorSize * tapTimeLenienceInBeats * nodeSeparationPerBeat * 2);
                    tapIndicator.localPosition = colorBorderPosition + Vector3.down * (tapIndicatorYSeparation + tapTimeLenienceInBeats * nodeSeparationPerBeat + tapIndicator.rect.height / 2);

                }

            }

            if(Input.GetMouseButtonUp(0)) {

                mouseIsDown = false;

            }

            if(Time.time > tapTimeWindows[tapTimeWindowIndex].end) {

                tapTimeWindowIndex++;

                if(!noteHasBeenTapped) {

                    numTapIndicators++;
                    RectTransform forgotTapIndicator = Instantiate(ghostTapIndicator, tapIndicatorContainer).GetComponent<RectTransform>();
                    forgotTapIndicator.name = "Ghost Tap Indicator";
                    forgotTapIndicator.GetComponent<Image>().color = incorrectTapIndicatorColor;
                    forgotTapIndicator.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, tapIndicatorSize * tapTimeLenienceInBeats * nodeSeparationPerBeat * 2);
                    forgotTapIndicator.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, tapIndicatorSize * tapTimeLenienceInBeats * nodeSeparationPerBeat * 2);
                    forgotTapIndicator.localPosition = lastNodePosition + Vector3.down * (tapIndicatorYSeparation + tapTimeLenienceInBeats * nodeSeparationPerBeat + forgotTapIndicator.rect.height / 2);

                }

                noteHasBeenTapped = false;

            }

            if(tapTimeWindowIndex >= tapTimeWindows.Count) {

                StartCoroutine(EndLevelCoroutine());

            }

        }

    }

    public void SetupPregameScreen() {

        measures = Measure.ReadTextInput(levelSO.GetLevelContents());
        sheetMusicUIManager.CreateFullMusicNotationUI(measures, musicNotationPanel, size: 1.2f);
        StartCoroutine(generalUIManager.SlideObjectCoroutine(musicNotationPanel, musicNotationPanelPosition, movingTime: 0.5f));

        StartCoroutine(generalUIManager.SlideObjectCoroutine(showGuideButton, showGuideButtonPosition, movingTime: 0.5f));
        StartCoroutine(generalUIManager.SlideObjectCoroutine(metronomePanel, metronomePanelPosition, movingTime: 0.5f));

        beatCounterPanel.localPosition = beatCountersHidingPanel.localPosition;
        CreateBeatCounters(measures[0].GetTimeSignature(), beatCounterPanel, measures[0].GetBeatGrouping());
        StartCoroutine(generalUIManager.SlideObjectCoroutine(beatCounterPanel, beatCounterPanelPosition, movingTime: 0.5f));

        playStopButton.GetComponent<HoldAndReleaseButton>().EnableButton();

    }

    //This method is invoked when the user clicks the ORANGE X BUTTON on the top left of the screen.
    public void UnconfirmedExitLevelButtonAction() {

        //Directly exit the level if the button
        if(resultsPanel.gameObject.activeSelf) {

            ExitLevelButtonAction();
            return;

        }

        exitLevelPanel.gameObject.SetActive(true);
        if(levelSO is TutorialScriptableObject) {

            exitLevelPanel.GetChild(0).GetChild(1).gameObject.SetActive(true);
            exitLevelPanel.GetChild(0).GetChild(2).gameObject.SetActive(false);

        } else {

            exitLevelPanel.GetChild(0).GetChild(1).gameObject.SetActive(false);
            exitLevelPanel.GetChild(0).GetChild(2).gameObject.SetActive(true);

        }

        Time.timeScale = 0;

    }

    public void CancelExitLevelButtonAction() {

        exitLevelPanel.gameObject.SetActive(false);
        Time.timeScale = 1;

    }

    public void ExitLevelButtonAction() {

        //The exit level button has two functions: one to return to the level select screen, and another to return to the select guide option screen.
        //If the select guide option panel is out, then that means that this button should return to the level select screen.
        //Otherwise, it means that we're currently on the gameplay screen, which then means that this button should return to the select guide option screen.

        Time.timeScale = 1;

        //First stop the gameplay coroutines and reset the metronome and the play/stop button.
        if(gameplaySequenceCoroutine != null) {

            StopCoroutine(gameplaySequenceCoroutine);

        }
        if(metronomeCoroutine != null) {

            StopCoroutine(metronomeCoroutine);

        }
        if(nodeGuideCoroutine != null) {

            StopCoroutine(nodeGuideCoroutine);

        }
        metronomePendulum.localEulerAngles = Vector3.zero;
        playStopButton.GetChild(1).GetChild(0).gameObject.SetActive(true);
        playStopButton.GetChild(1).GetChild(1).gameObject.SetActive(false);
        noteTappingIsOn = false;
        tapTimeWindowIndex = 0;


        //Now remove this screen's UI.
        exitLevelPanel.gameObject.SetActive(false);
        resultsPanel.gameObject.SetActive(false);
        generalUIManager.MoveObjectOffScreen(metronomePanel, "vertical", true, callback: () => {

            for(int i = 0; i < beatCounterPanel.childCount; i++) {

                Destroy(beatCounterPanel.GetChild(i).gameObject);

            }

        });
        //Destroy the guide music notation immediately.
        for(int i = 0; i < guideMusicNotationContainer.childCount; i++) {

            Destroy(guideMusicNotationContainer.GetChild(i).gameObject);

        }
        generalUIManager.MoveObjectOffScreen(guidePanel, "vertical", false, callback: () => {

            for(int i = 0; i < nodesContainer.childCount; i++) {

                if(nodesContainer.GetChild(i) != tapIndicatorContainer) {

                    Destroy(nodesContainer.GetChild(i).gameObject);

                }

            }

            for(int i = 0; i < tapIndicatorContainer.childCount; i++) {

                Destroy(tapIndicatorContainer.GetChild(i).gameObject);

            }

        });
        generalUIManager.MoveObjectOffScreen(musicNotationPanel, "vertical", true, callback: () => {

            for(int i = 0; i < musicNotationPanel.childCount; i++) {

                Destroy(musicNotationPanel.GetChild(i).gameObject);

            }

        });
        if(showGuideButton.gameObject.activeSelf) {

            generalUIManager.MoveObjectOffScreen(showGuideButton, "vertical", false);

        }
        removeGuidesButton.gameObject.SetActive(false);

        generalUIManager.ResetupLevelSelectScreen(); //Bring out the level select screen

    }

    public void PlayPauseButtonAction() {

        if(playStopButton.GetChild(1).Find("Play Icon").gameObject.activeSelf) {

            ResetLevel();

            gameplaySequenceCoroutine = PlayLevelCoroutine();
            StartCoroutine(gameplaySequenceCoroutine);

            playStopButton.GetChild(1).GetChild(0).gameObject.SetActive(false);
            playStopButton.GetChild(1).GetChild(1).gameObject.SetActive(true);

            generalUIManager.MoveObjectOffScreen(showGuideButton, "vertical", false);

            removeGuidesButton.gameObject.SetActive(false);

        } else {

            StopLevel();

            playStopButton.GetChild(1).GetChild(0).gameObject.SetActive(true);
            playStopButton.GetChild(1).GetChild(1).gameObject.SetActive(false);

            if(guidePanel.gameObject.activeSelf) {

                removeGuidesButton.gameObject.SetActive(true);

            } else {

                StartCoroutine(generalUIManager.SlideObjectCoroutine(showGuideButton, showGuideButtonPosition));

            }

        }

    }

    public void ShowGuideButtonAction() {

        if(nodesContainer.childCount <= 0) {

            CreateGuideNodes(measures, nodesContainer, guideRowSeparation);
            sheetMusicUIManager.CreateNoteGuideMusicNotation(measures, guideMusicNotationContainer, nodesContainer, guideMusicNotationSize, guideMusicNotationHoverDistance, nodeSeparationPerBeat, unreachedGuideMusicNotationColor);

        }

        StartCoroutine(generalUIManager.SlideObjectCoroutine(guidePanel, guidePanelPosition, callback: () => {

            removeGuidesButton.gameObject.SetActive(true);

            RectTransform firstMusicNotationTransform = guideMusicNotationContainer.GetChild(0).GetComponent<RectTransform>();
            removeGuidesButton.localPosition = Vector3.up * (firstMusicNotationTransform.localPosition.y + removeGuidesButtonSeparation + removeGuidesButton.rect.height / 2);

        }));
        generalUIManager.MoveObjectOffScreen(showGuideButton, "vertical", false);

    }

    public void RemoveGuideButtonAction() {

        generalUIManager.MoveObjectOffScreen(guidePanel, "vertical", false);
        StartCoroutine(generalUIManager.SlideObjectCoroutine(showGuideButton, showGuideButtonPosition, callback: () => {

            ClearGuide();

        }));
        removeGuidesButton.gameObject.SetActive(false);

    }

    public void RetryLevelButtonAction() {

        resultsPanel.gameObject.SetActive(false);

        ResetLevel();

        if(guidePanel.gameObject.activeSelf) {

            removeGuidesButton.gameObject.SetActive(true);

        } else {

            StartCoroutine(generalUIManager.SlideObjectCoroutine(showGuideButton, showGuideButtonPosition));

        }

    }

    public void RetryLevelWithoutGuideButtonAction() {

        resultsPanel.gameObject.SetActive(false);

        ResetLevel();

        if(guidePanel.gameObject.activeSelf) {

            generalUIManager.MoveObjectOffScreen(guidePanel, "vertical", false);

        }

    }

    public void SetLevelSO(LevelScriptableObject levelSO) {

        this.levelSO = levelSO;
        tempo = levelSO.GetTempo();

    }

    private List<TapTimeWindow> SetTapTimeWindows(Measure[] measures, float timeOfFirstNote = -1) {

        float tapTimeLenienceInSeconds = tapTimeLenienceInBeats * 60 / tempo;
        if(timeOfFirstNote < 0) {

            timeOfFirstNote = Time.time + tapTimeLenienceInSeconds;

        }

        List<Element> elements = MeasureArrayAsList(measures);
        float noteWindowCenter = timeOfFirstNote;
        bool skipNextNote = false;
        List<TapTimeWindow> tapTimeWindows = new List<TapTimeWindow>();
        for(int i = 0; i < elements.Count; i++) {

            if(elements[i] is Note && !skipNextNote) {

                tapTimeWindows.Add(new TapTimeWindow(noteWindowCenter - tapTimeLenienceInSeconds, noteWindowCenter + tapTimeLenienceInSeconds));

                if(i < elements.Count - 1 && Note.IsTie(elements[i], elements[i + 1])) {

                    skipNextNote = true;

                }

            } else if(skipNextNote) {

                skipNextNote = false;

            }

            noteWindowCenter += elements[i].GetElementDuration(tempo);

        }

        return tapTimeWindows;

    }

    private IEnumerator PlayLevelCoroutine() {

        metronomeCoroutine = MetronomeCoroutine();
        StartCoroutine(metronomeCoroutine);

        float fullMeasureTimeInSeconds = (float) beatCounterPanel.childCount / tempo * 60f;
        tapTimeWindows = SetTapTimeWindows(measures, Time.time + fullMeasureTimeInSeconds);

        //Give a countoff by starting the node color switching a full measure length late.
        yield return new WaitForSeconds(fullMeasureTimeInSeconds - tapTimeLenienceInBeats / tempo * 60f);

        noteTappingIsOn = true;
        if(guidePanel.gameObject.activeSelf) {

            nodeGuideCoroutine = NodeGuideProgressCoroutine();
            StartCoroutine(nodeGuideCoroutine);

        }

    }

    private IEnumerator MetronomeCoroutine(bool doCountIn = true) {

        /* Below is the equation used to calculate the angle "theta" of the pendulum at a given time "t":
        theta = theta0 * sin(ct)   where "theta0" is the maximum pendulum angle, and where "c" is the period coefficient as shown below.
        c = 2*pi*f   where "f" is the frequency given in s^-1 (beats per second).
        f = (tempo / 60) / 2 = tempo / 120
        theta = theta0 * sin(pi*tempo*t / 60)
        */

        metronomeBeginTime = Time.time;
        float elapsedTimeSinceLastBeat = 0;
        int currentHighlightedBeatCounterIndex = 0;
        float pendulumPeriodCoefficient = Mathf.PI * tempo / 60f; //This is "c"
        beatCounterPanel.GetChild(0).GetComponent<Image>().color = highlightedBeatCounterColor;
        beatCounterPanel.GetChild(0).GetComponent<Image>().fillCenter = true;

        if(doCountIn) {

            int beatIndex = 1;
            while(beatIndex <= beatCounterPanel.childCount) {

                //In the while loop, we wait until the beat index goes one over the total number of beats.
                //This is only because we want to start normal beat counting at the beginning of the first beat of the first "normal" measure, and NOT the last beat of the count-in.

                metronomePendulum.localRotation = Quaternion.Euler(0, 0, maximumMetronomeAngle * Mathf.Sin(pendulumPeriodCoefficient * (Time.time - metronomeBeginTime)));
                elapsedTimeSinceLastBeat += Time.deltaTime;
                if(elapsedTimeSinceLastBeat >= 60f / tempo && beatIndex < beatCounterPanel.childCount) {

                    elapsedTimeSinceLastBeat = 0;

                    Image beatCounter = beatCounterPanel.GetChild(beatIndex).GetComponent<Image>();
                    beatCounter.color = highlightedBeatCounterColor;
                    beatCounter.fillCenter = true;
                    beatIndex++;

                } else if(elapsedTimeSinceLastBeat >= 60f / tempo && beatIndex >= beatCounterPanel.childCount) {

                    elapsedTimeSinceLastBeat = 0;
                    break;

                }

                yield return null;

            }

            //Reset the beat counters after the count-in.
            for(int i = 1; i < beatCounterPanel.childCount; i++) {

                beatCounterPanel.GetChild(i).GetComponent<Image>().color = defaultBeatCounterColor;
                beatCounterPanel.GetChild(i).GetComponent<Image>().fillCenter = false;

            }

        }

        //Start the normal beat counter color-changing.
        while(true) {

            metronomePendulum.localRotation = Quaternion.Euler(0, 0, maximumMetronomeAngle * Mathf.Sin(pendulumPeriodCoefficient * (Time.time - metronomeBeginTime)));

            elapsedTimeSinceLastBeat += Time.deltaTime;
            if(elapsedTimeSinceLastBeat >= 60f / tempo) {

                elapsedTimeSinceLastBeat = 0;
                Image oldBeatCounter = beatCounterPanel.GetChild(currentHighlightedBeatCounterIndex).GetComponent<Image>();
                oldBeatCounter.color = defaultBeatCounterColor;
                oldBeatCounter.fillCenter = false;
                
                currentHighlightedBeatCounterIndex = (currentHighlightedBeatCounterIndex + 1) % beatCounterPanel.childCount;
                Image newBeatCounter = beatCounterPanel.GetChild(currentHighlightedBeatCounterIndex).GetComponent<Image>();
                newBeatCounter.color = highlightedBeatCounterColor;
                newBeatCounter.fillCenter = true;

            }

            yield return null;

        }

    }

    private IEnumerator StopMetronomePendulumCoroutine() {

        //Please refer to the comments in MetronomeCoroutine() for explanation on the decently simple math behind this.

        if(metronomePendulum.localRotation.z == 0) {

            yield break;

        }

        float pendulumPeriodCoefficient = Mathf.PI * tempo / 60f;
        float beginRotation = metronomePendulum.localEulerAngles.z > 180 ? metronomePendulum.localEulerAngles.z - 360 : metronomePendulum.localEulerAngles.z;
        while(true) {

            float pendulumRotation = maximumMetronomeAngle * Mathf.Sin(pendulumPeriodCoefficient * (Time.time - metronomeBeginTime));
            metronomePendulum.localRotation = Quaternion.Euler(0, 0, pendulumRotation);
            if(beginRotation < 0 && pendulumRotation >= 0) {

                metronomePendulum.localRotation = Quaternion.Euler(Vector3.zero);
                yield break;

            } else if(beginRotation > 0 && pendulumRotation <= 0) {

                metronomePendulum.localRotation = Quaternion.Euler(Vector3.zero);
                yield break;

            } else {

                yield return null;

            }

        }

    }

    private IEnumerator NodeGuideProgressCoroutine() {

        void ApplyGuideMaterialToRow(Transform row, Material material) {

            for(int i = 0; i < row.childCount; i++) {

                Transform child = row.GetChild(i);
                if(child.name == "Connector Beam") {

                    child.GetComponent<Image>().material = guideMaterial;

                } else if(child.name == "Node") {

                    child.GetChild(1).GetComponent<Image>().material = guideMaterial;

                }

            }

        }
        void ApplyReachedColorToRow(Transform row) {

            for(int i = 0; i < row.childCount; i++) {

                Transform child = row.GetChild(i);
                if(child.name == "Connector Beam") {

                    child.GetComponent<Image>().material = null;
                    child.GetComponent<Image>().color = reachedGuideColor;

                } else if(child.name == "Node") {

                    child.GetChild(1).GetComponent<Image>().material = null;
                    child.GetChild(1).GetComponent<Image>().color = reachedGuideColor;

                }

            }

        }


        //These variables are for the color-changing music notation.
        int elementNotationIndex = 0;
        float elementNotationLocalXPosition = guideMusicNotationContainer.GetChild(0).localPosition.x;
        float musicNotationPanelPositionRelativeToCanvas = guideMusicNotationContainer.localPosition.x + guideMusicNotationContainer.parent.localPosition.x + guideMusicNotationContainer.parent.parent.localPosition.x;

        //Loop through each guide row (these are just the guide panel children).
        for(int row = 0; row < nodesContainer.childCount; row++) {

            RectTransform guideRow = (RectTransform) nodesContainer.GetChild(row);
            if(guideRow.CompareTag("Other Music Notation")) {

                continue;

            }

            ApplyGuideMaterialToRow(guideRow, guideMaterial);

            //Note: these are in coords relative to the UI canvas (the shader's color border property controls the position of the border in reference to the canvas for some reason).
            float guideRowPositionRelativeToCanvas = guideRow.localPosition.x + guideRow.parent.localPosition.x + guideRow.parent.parent.localPosition.x;
            float rowLeftEndXPosition = guideRowPositionRelativeToCanvas - guideRow.rect.width / 2;
            float rowRightEndXPosition = guideRowPositionRelativeToCanvas + guideRow.rect.width / 2;
            
            int nodeIndex = 1; //Note that the next node index starts at 1, not 0. This is because child #0 of guideRow is the connector beam, and the first NODE of guideRow is child #1
            float nodeLocalXPosition = guideRow.GetChild(1).localPosition.x;
            
            float colorBorderXPosition = rowLeftEndXPosition;
            float beginTime = Time.time;
            while(colorBorderXPosition <= rowRightEndXPosition) {

                colorBorderXPosition = rowLeftEndXPosition + (Time.time - beginTime) * nodeSeparationPerBeat * tempo / 60f;
                guideMaterial.SetFloat("_Color_Boundary", colorBorderXPosition);

                /*
                Change the color of the next element transform if the current x-position has past the element's x-position, AND if the color change border is on the element's row.
                We have to check the latter because when we're at the last element transform of a row, there is some time between the moment when the last element changes color
                and the moment when colorBorderXPosition is still on the same row. This can cause currXPosition >= [next element's x-position] to be erroneously true for all remaining elements,
                turning all remaining elements into the reached color early.
                 */
                if(colorBorderXPosition >= elementNotationLocalXPosition + musicNotationPanelPositionRelativeToCanvas && nodeIndex < guideRow.childCount) {

                    Transform elementNotation = guideMusicNotationContainer.GetChild(elementNotationIndex);
                    elementNotation.GetComponent<Image>().color = reachedGuideMusicNotationColor;
                    for(int i = 0; i < elementNotation.childCount; i++) {

                        elementNotation.GetChild(i).GetComponent<Image>().color = reachedGuideMusicNotationColor;

                    }

                    elementNotationIndex++;
                    elementNotationLocalXPosition = elementNotationIndex < guideMusicNotationContainer.childCount ? guideMusicNotationContainer.GetChild(elementNotationIndex).localPosition.x : float.NaN;

                }

                if(colorBorderXPosition >= nodeLocalXPosition + guideRowPositionRelativeToCanvas) {

                    guideRow.GetChild(nodeIndex).GetChild(0).GetComponent<Image>().color = reachedGuideColor; //Turn the node from hollow into filled
                    lastNodePosition = new Vector3(guideRow.GetChild(nodeIndex).localPosition.x, guideRow.localPosition.y, 0);
                    nodeIndex++;
                    nodeLocalXPosition = nodeIndex < guideRow.childCount ? guideRow.GetChild(nodeIndex).localPosition.x : float.MaxValue;

                }

                colorBorderPosition = new Vector3(colorBorderXPosition - guideRowPositionRelativeToCanvas, guideRow.localPosition.y, 0);

                yield return null;

            }

            ApplyReachedColorToRow(guideRow);

        }

    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="timeSignature"></param>
    /// <param name="parent"></param>
    /// <param name="beatGrouping">(optional) How beats should be grouped. <br></br>For example, in 6/8, beats are commonly grouped as 3-3, while in 12/8 it is 3-3-3-3, <br></br>and in 7/8 it can be 2-2-3, 2-3-2, or 3-2-2.</param>
    private void CreateBeatCounters(int[] timeSignature, RectTransform parent, int[] beatGrouping) {

        //Note: the time signatures 3/4 and 3/8 should have the same beat counters, and so should 7/4 and 7/8, and so on.
        //This is because we want to emphasize that time signatures sharing the same beat count are essentially the same

        //Calculate the counter grouping (DIFFERENT THAN THE BEAT GROUPING).
        int[] counterGrouping;
        bool actuallyHasGroups = false;
        for(int i = 0; i < beatGrouping.Length; i++) {

            if(beatGrouping[i] != 1) {

                actuallyHasGroups = true;
                break;

            }

        }
        if(actuallyHasGroups) {

            counterGrouping = beatGrouping;

        } else {

            counterGrouping = new int[] { beatGrouping.Length };

        }

        //We will use the row with the most beat counters as the standard for the width of beat counters
        FindLargestRowInBeatCounterGrouping(counterGrouping, out int maxNumTransformsPerRow, out int numGroupsInMaxRow, out int numRows);

        float counterTransformWidth = parent.rect.width / (maxNumTransformsPerRow + counterTransformSpacing * (maxNumTransformsPerRow - numGroupsInMaxRow) + counterTransformGroupSpacing * (numGroupsInMaxRow - 1));
        float counterTransformSpacingWidth = counterTransformWidth * counterTransformSpacing;
        float counterTransformGroupSpacingWidth = counterTransformWidth * counterTransformGroupSpacing;
        float counterTransformRowSpacingWidth = counterTransformWidth * counterTransformRowSpacing;

        //Create and place the beat counters
        float xPosition = -parent.rect.width / 2f + counterTransformWidth / 2f;
        float yPosition = (numRows - 1) * (counterTransformHeight + counterTransformRowSpacingWidth) / 2f;
        int numCounterTransformsInRow = 0;
        for(int group = 0; group < counterGrouping.Length; group++) {

            numCounterTransformsInRow += counterGrouping[group];

            for(int i = 0; i < counterGrouping[group]; i++) {

                RectTransform counterTransform = Instantiate(beatCounterPrefab, parent).GetComponent<RectTransform>();
                counterTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, counterTransformWidth);
                counterTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, counterTransformHeight);
                counterTransform.localPosition = new Vector3(xPosition, yPosition, 0);
                counterTransform.GetComponent<Image>().color = defaultBeatCounterColor;

                xPosition += counterTransformWidth + counterTransformSpacingWidth;

            }

            xPosition -= counterTransformSpacingWidth;
            xPosition += counterTransformGroupSpacingWidth;

            if(group < counterGrouping.Length - 1 && numCounterTransformsInRow + counterGrouping[group + 1] > 8) {

                yPosition -= beatCounterPrefab.GetComponent<RectTransform>().rect.height + counterTransformRowSpacingWidth;
                xPosition = -parent.rect.width / 2f + counterTransformWidth / 2f;
                numCounterTransformsInRow = 0;

            }

        }

    }

    private void CreateGuideNodes(Measure[] measures, RectTransform parent, float rowSeparation) {

        List<Element> elements = MeasureArrayAsList(measures); //Note: time signature doesn't matter for this because a beat has the length of a beat--regardless of what the beat REALLY is
        int lastAudibleElementIndex = 0;
        for(int i = elements.Count - 1; i >= 0; i--) {

            if(elements[i] is not Note || (i > 0 && Note.IsTie(elements[i - 1], elements[i]))) {

                continue;

            }

            lastAudibleElementIndex = i;
            break;

        }
        //We will first assume that the nodes need as much space as possible. Therefore, we will use minNodeSeparationPerBeat.
        List<float> minGuideRowWidths = CalculateWidthsOfGuideRows(elements, minNodeSeparationPerBeat, parent.rect.width, lastAudibleElementIndex);
        int numRows = minGuideRowWidths.Count;
        float longestRowWidth = -1;
        for(int i = 0; i < minGuideRowWidths.Count; i++) {

            if(minGuideRowWidths[i] > longestRowWidth) {

                longestRowWidth = minGuideRowWidths[i];

            }

        }


        //Scale up the nodeSeparationPerBeat so that it can cause the longest row width to be exactly the container panel width.
        nodeSeparationPerBeat = Math.Min(minNodeSeparationPerBeat * parent.rect.width / longestRowWidth, maxNodeSeparationPerBeat);
        float nodeRadius = nodeSeparationPerBeat * tapTimeLenienceInBeats;
        float beginXPosition = -parent.rect.width / 2f + nodeRadius;
        float xPosition = beginXPosition;
        float yPosition = (nodeRadius + rowSeparation / 2) * (numRows - 1);
        int numRowsCompleted = 0;
        for(int e = 0; e < elements.Count; e++) {

            Element element = elements[e];
            if(element is not Note) {

                xPosition += element.GetBeats() * nodeSeparationPerBeat;
                continue;

            }
            if(e > 0 && Note.IsTie(elements[e - 1], elements[e])) {

                //Skip if this note is at the receiving end of a tie
                xPosition += element.GetBeats() * nodeSeparationPerBeat;
                continue;

            }

            RectTransform newNode = Instantiate(hollowNode, parent).GetComponent<RectTransform>();
            newNode.name = "Node";
            float scalingFactor = nodeRadius * 2 / hollowNode.GetComponent<RectTransform>().rect.width;
            newNode.localScale = new Vector3(scalingFactor, scalingFactor, 1);

            //Place the node.
            newNode.localPosition = new Vector3(xPosition, yPosition, 0);
            newNode.GetChild(1).GetComponent<Image>().color = unreachedGuideColor;

            //Check if the next node would be on a new row.
            float nextNodeRightBound = xPosition + element.GetBeats() * nodeSeparationPerBeat + nodeRadius;
            if(nextNodeRightBound > parent.rect.width / 2f + 0.001f /*for floating-point inaccuracy*/) {

                //If the next node would end up outside of the container panel, then wrap this row up.
                ConnectNodesInRow(parent, numRowsCompleted + 1, beginXPosition, minGuideRowWidths[numRowsCompleted] * nodeSeparationPerBeat / minNodeSeparationPerBeat, yPosition, nodeRadius);

                //Lastly, update the x and y positions.
                beginXPosition = -parent.rect.width / 2;
                xPosition = beginXPosition + (element.GetBeats() - tapTimeLenienceInBeats) * nodeSeparationPerBeat; //Since the last node covers some of this 
                yPosition -= nodeRadius * 2 + rowSeparation; //Change the y-position

                numRowsCompleted++;
                continue;

            }

            //If the next node stays on the same row.
            xPosition += element.GetBeats() * nodeSeparationPerBeat; //Update the x-position

        }

        //If a connector beam still needs to be made, make it.
        if(numRowsCompleted < numRows) {

            ConnectNodesInRow(parent, numRowsCompleted + 1, beginXPosition, minGuideRowWidths[numRowsCompleted] * nodeSeparationPerBeat / minNodeSeparationPerBeat, yPosition, nodeRadius);

        }

    }

    private void ConnectNodesInRow(RectTransform nodesContainer, int rowNumber, float beginXPosition, float rowWidth, float yPosition, float nodeRadius) {

        //First, we need to make a connector line.
        RectTransform newConnector = Instantiate(connector, nodesContainer).GetComponent<RectTransform>();
        newConnector.name = "Connector Beam";
        newConnector.SetAsFirstSibling();
        newConnector.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, nodesContainer.GetChild(nodesContainer.childCount - 1).localPosition.x - beginXPosition);
        newConnector.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, connectorBeamHeightAsFractionOfNodeDiameter * nodeRadius * 2);
        newConnector.localPosition = new Vector3(beginXPosition + newConnector.rect.width / 2f, yPosition, 0);
        newConnector.GetComponent<Image>().color = unreachedGuideColor;

        //Second, we need to make a container GO for this row.
        GameObject guideRowGO = new GameObject($"Guide Row {rowNumber}");
        RectTransform guideRowRectTransform = guideRowGO.AddComponent<RectTransform>();
        guideRowRectTransform.SetParent(nodesContainer);
        guideRowRectTransform.localScale = Vector3.one;
        guideRowRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, rowWidth);
        guideRowRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, nodeRadius * 2);
        guideRowRectTransform.localPosition = new Vector3(-nodesContainer.rect.width / 2 + rowWidth / 2, yPosition, 0);
        //Parent all of the nodes in this row (this happens to be all nodes that are direct children of the parent guide panel).
        int i = 0;
        while(nodesContainer.childCount > rowNumber) {

            if(nodesContainer.GetChild(i).name == "Node" || nodesContainer.GetChild(i).name == "Connector Beam") {

                nodesContainer.GetChild(i).SetParent(guideRowRectTransform);

            } else {

                i++;

            }

        }
        guideRowRectTransform.localPosition = new Vector3(0, yPosition, 0); //Center the row

    }

    private IEnumerator EndLevelCoroutine() {

        noteTappingIsOn = false;
        mouseIsDown = false;
        noteHasBeenTapped = false;
        playStopButton.GetComponent<HoldAndReleaseButton>().DisableButton();

        //Stop the gameplay sequence and metronome.
        //Let the node guide coroutine finish on its own though.
        if(gameplaySequenceCoroutine != null) {

            StopCoroutine(gameplaySequenceCoroutine);

        }
        if(metronomeCoroutine != null) {

            StopCoroutine(metronomeCoroutine);

        }

        //Reset the metronome and beat counters.
        for(int i = 0; i < beatCounterPanel.childCount; i++) {

            beatCounterPanel.GetChild(i).GetComponent<Image>().fillCenter = false;
            beatCounterPanel.GetChild(i).GetComponent<Image>().color = defaultBeatCounterColor;

        }
        StartCoroutine(StopMetronomePendulumCoroutine());
        
        //Reset the play/pause button.
        playStopButton.GetChild(1).GetChild(0).gameObject.SetActive(true);
        playStopButton.GetChild(1).GetChild(1).gameObject.SetActive(false);

        yield return new WaitForSeconds(resultsPanelShowingDelay);

        resultsPanel.gameObject.SetActive(true);
        if(guidePanel.gameObject.activeSelf) {

            //Results panel when guides were used
            nextLevelButton.gameObject.SetActive(false);
            tapAccuracyTextTransform.gameObject.SetActive(true);
            retryWithoutGuidesButton.gameObject.SetActive(true);

            float accuracy = Mathf.RoundToInt((float) numAccurateTapIndicators / numTapIndicators * 100);
            TMP_Text accuracyText = tapAccuracyTextTransform.GetComponent<TMP_Text>();
            accuracyText.text = $"{accuracy}%";
            if(accuracy == 100) {

                accuracyText.color = highlightedUIColor;
                retryButton.GetComponent<HoldAndReleaseButton>().SetColor(defaultUIColor, 0.3f);
                retryWithoutGuidesButton.GetComponent<HoldAndReleaseButton>().SetColor(highlightedUIColor, 0.25f);

            } else {

                accuracyText.color = defaultUIColor;
                retryButton.GetComponent<HoldAndReleaseButton>().SetColor(highlightedUIColor, 0.25f);
                retryWithoutGuidesButton.GetComponent<HoldAndReleaseButton>().SetColor(defaultUIColor, 0.3f);

            }

        } else {

            //Results panel when guides were not used

        }

    }

    private void StopLevel() {

        noteTappingIsOn = false;
        mouseIsDown = false;
        noteHasBeenTapped = false;

        //Stop all of the coroutines.
        if(gameplaySequenceCoroutine != null) {

            StopCoroutine(gameplaySequenceCoroutine);

        }
        if(metronomeCoroutine != null) {

            StopCoroutine(metronomeCoroutine);

        }
        if(nodeGuideCoroutine != null) {

            StopCoroutine(nodeGuideCoroutine);

        }

        //Reset the beat counters.
        for(int i = 0; i < beatCounterPanel.childCount; i++) {

            beatCounterPanel.GetChild(i).GetComponent<Image>().fillCenter = false;
            beatCounterPanel.GetChild(i).GetComponent<Image>().color = defaultBeatCounterColor;

        }

        //Stop the metronome pendulum.
        StartCoroutine(StopMetronomePendulumCoroutine());

    }

    private void ResetLevel() {

        //Reset the gameplay mechanisms.
        tapTimeWindowIndex = 0;
        numAccurateTapIndicators = 0;
        numTapIndicators = 0;
        noteTappingIsOn = false;
        mouseIsDown = false;
        noteHasBeenTapped = false;

        ClearGuide();

        playStopButton.GetComponent<HoldAndReleaseButton>().EnableButton();

    }

    private void ClearGuide() {

        //Reset the node guides.
        foreach(Transform nodeRow in nodesContainer) {

            foreach(Transform child in nodeRow) {

                if(child.name == "Connector Beam") {

                    child.GetComponent<Image>().material = null;
                    child.GetComponent<Image>().color = unreachedGuideColor;

                } else if(child.name == "Node") {

                    child.GetChild(0).GetComponent<Image>().color = Color.white;
                    child.GetChild(1).GetComponent<Image>().material = null;
                    child.GetChild(1).GetComponent<Image>().color = unreachedGuideColor;

                }

            }

        }
        foreach(Transform elementTransform in guideMusicNotationContainer) {

            elementTransform.GetComponent<Image>().color = unreachedGuideMusicNotationColor;
            foreach(Transform elementComponent in elementTransform) {

                elementComponent.GetComponent<Image>().color = unreachedGuideMusicNotationColor;

            }

        }

        //Clear the tap indicators.
        for(int i = 0; i < tapIndicatorContainer.childCount; i++) {

            Destroy(tapIndicatorContainer.GetChild(i).gameObject);

        }

    }

    /// <summary>
    /// Given a list of elements, this finds how the element's guide nodes should be organized into rows. It returns the widths of guide rows, and outs the longestRowWidth
    /// </summary>
    /// <param name="elements"></param>
    /// <param name="nodeSeparationPerBeat"></param>
    /// <param name="containerWidth"></param>
    /// <param name="lastAudibleElementIndex"></param>
    /// <param name="longestRowWidth"></param>
    /// <param name="numRows"></param>
    /// <returns></returns>
    private List<float> CalculateWidthsOfGuideRows(List<Element> elements, float nodeSeparationPerBeat, float containerWidth, int lastAudibleElementIndex) {

        float nodeRadius = nodeSeparationPerBeat * tapTimeLenienceInBeats;
        float rowWidth = nodeRadius; //Set it equal to node radius because the first row always begins with a node
        //The following variable "nodeSeparation" will temporarily store the separation between one node and the next.
        //This is to address the problem encountered with rests and ties--or in other words--elements that are silent and won't have nodes.
        float nodeSeparation = 0; 
        bool noteIsSilent = false; //A boolean flag to also address the above problem (specifically for tied notes)

        List<float> rowWidths = new List<float>();
        for(int i = 0; i < lastAudibleElementIndex; i++) {

            nodeSeparation += elements[i].GetBeats() * nodeSeparationPerBeat;

            if(noteIsSilent || elements[i] is not Note) {

                //Skip this element if it's a rest or if it's at the receiving end of a tie
                noteIsSilent = false; //Remember to reset the noteIsSilent flag
                continue;

            }

            if(Note.IsTie(elements[i], elements[i + 1])) { //No need to worry that i+1 goes out of bounds, for i stops before lastAudibleElementIndex

                noteIsSilent = true; //Skip the next element if this current note is the beginning of a tie

            }

            //Check if the next node should be on a new row.
            if(rowWidth + nodeSeparation + nodeRadius > containerWidth + 0.001f /*for floating-point inaccuracy*/) {

                rowWidths.Add(rowWidth + nodeRadius); //Add node radius because the row ends with a node, and the connector beam ends at the center of the node
                //When starting a new row, there will NOT be a node at the start, and we will subtract the node radius because the last node's. Therefore we will not be adding nodeRadius
                rowWidth = nodeSeparation - nodeRadius;
                nodeSeparation = 0;
                continue;

            }

            //If code reaches here, that means that the next node will stay on the same row.
            rowWidth += nodeSeparation; //Do not add nodeRadius to the rowWidth variable (we only account for adding nodeRadius when we're thinking about ending a row)
            nodeSeparation = 0;

        }

        rowWidths.Add(rowWidth + nodeSeparation + nodeRadius);

        return rowWidths;

    }

    //NOTE TO SELF: THIS METHOD BELOW IS FOR THE METRONOME BEAT COUNTERS, *not* THE NODE GUIDES.
    private void FindLargestRowInBeatCounterGrouping(int[] counterGrouping, out int maxNumTransformsPerRow, out int numGroupsInMaxRow, out int numRows) {

        //Find how many transforms there will be in the first row. This number cannot be greater than 8
        int numTransformsPerRow = 0;
        maxNumTransformsPerRow = -1;
        int numGroupsInRow = 0;
        numGroupsInMaxRow = 0;
        numRows = 1;
        for(int i = 0; i < counterGrouping.Length; i++) {

            numGroupsInRow++;

            if(numTransformsPerRow + counterGrouping[i] > 8) {

                if(numTransformsPerRow > maxNumTransformsPerRow) {

                    maxNumTransformsPerRow = numTransformsPerRow;
                    numGroupsInMaxRow = numGroupsInRow;

                }
                numTransformsPerRow = counterGrouping[i];
                numGroupsInRow = 1;
                numRows++;

            } else {

                numTransformsPerRow += counterGrouping[i];
                if(numTransformsPerRow > maxNumTransformsPerRow) {

                    maxNumTransformsPerRow = numTransformsPerRow;
                    numGroupsInMaxRow = numGroupsInRow;

                }

            }

        }

    }

    private List<Element> MeasureArrayAsList(Measure[] measures) {

        List<Element> list = new List<Element>();
        for(int m = 0; m < measures.Length; m++) {

            for(int i = 0; i < measures[m].Count; i++) {

                list.Add(measures[m][i]);

            }

        }

        return list;

    }

}
