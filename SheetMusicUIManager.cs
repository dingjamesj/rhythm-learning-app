using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SheetMusicUIManager : MonoBehaviour {

    [Header("Prefabs")]
    [SerializeField] private GameObject filledNoteHead;
    [SerializeField] private GameObject hollowNoteHead;
    [SerializeField] private GameObject wholeNoteHead;
    [SerializeField] private GameObject noteStem;
    [SerializeField] private GameObject flag;
    [SerializeField] private GameObject sixteenthRest;
    [SerializeField] private GameObject eighthRest;
    [SerializeField] private GameObject quarterRest;
    [SerializeField] private GameObject halfRest;
    [SerializeField] private GameObject wholeRest;
    [SerializeField] private GameObject dot;
    [SerializeField] private GameObject tie;
    [SerializeField] private GameObject tieHalf;
    [SerializeField] private GameObject barline;
    [SerializeField] private GameObject doubleBarline;
    [SerializeField] private GameObject[] timeSignatureDigitPrefabs;

    [Header("Preferences")]
    [SerializeField] private float noteSeparationFactor = 150;
    [SerializeField] private float betweenMeasuresGapLength = 22.5f;
    [SerializeField] private float noteStemHeight = 60;
    [SerializeField] private float noteBeamHeight = 13;
    [SerializeField] private float noteBeamSeparation = 3;
    [SerializeField] private float noteFlagSeparation = 0.55f;
    [SerializeField] private float notationRowSpacing = 45;
    [SerializeField] private float dotXDisplacement = 7.5f;
    [SerializeField] private float dotYDisplacement = 10f;
    [SerializeField] private float tieYDisplacementMultiplier = 1.25f;

    /// <summary>
    /// Music notation size, as a ratio between the desired size to the original sprite size
    /// </summary>
    private float size = 1;
    private readonly float logisticCurveWidth = 10;
    private readonly float logisticCurveBottom = 1.2f;
    private readonly float logisticCurveXAdjustment = 1.25f;


    /// <summary>
    /// Creates the UI for a series of measures, WITH extra arguments like ties and forced unbeaming <br></br>
    /// </summary>
    /// <param name="measures"></param>
    /// <param name="parent">The parent that measure game objects should generate under</param>
    /// <param name="size">(optional) The size of the music notation, as a ratio between desired size and the original size of the sprites</param>
    /// <param name="alignment">(optional) The alignment of the music notation ("left," "center," or "left-center" (left-center means that it is left-aligned, but the music notation as a whole is centered)</param>
    /// <param name="musicNotationBoundsInset">(optional) The inset, relative to the left and right edges of the parent rect, of the bounds at which music notation can be displayed</param>
    public void CreateFullMusicNotationUI(Measure[] measures, RectTransform parent, string alignment = "left-center", float size = 1, float musicNotationBoundsInset = 0) {

        if(parent == null) {

            Debug.LogWarning("SheetMusicUIManager: A null parent for the music notation was given.");
            parent = transform.GetComponent<RectTransform>();

        }

        float notationWrappingWidth = parent.rect.width - musicNotationBoundsInset * 2f;
        if(notationWrappingWidth <= 0) {

            Debug.LogWarning($"SheetMusicUIManager: The music notation bounds inset of {musicNotationBoundsInset} causes a non-positve notation wrapping length of {notationWrappingWidth}.");

        }

        this.size = size <= 0 ? 1 : size;

        //Calculate how the measures will be placed (which measures are in which row, based on notationWrappingLength)
        RectTransform[,] measureRectTransforms = new RectTransform[measures.Length * 2, measures.Length * 2]; //(this array won't be completely filled)
        float[] notationRowLengths = new float[measures.Length]; //(this array ALSO won't be completely filled)
        int rowCount = 0;
        int colCount = 0;
        for(int i = 0; i < measures.Length; i++) {

            RectTransform measureRectTransform = CreateMeasureUI(measures[i], parent).GetComponent<RectTransform>();
            measureRectTransform.name = $"Measure {i}";
            float measureWidth = measureRectTransform.rect.width + betweenMeasuresGapLength * size;
            if(measureWidth >= notationWrappingWidth) { //The width of this measure is larger than the notation wrapping length, so display a warning message to the console

                Debug.LogWarning($"SheetMusicUIManager: The music notation bounds inset of {musicNotationBoundsInset} causes a notation wrapping length of {notationWrappingWidth}, " +
                    $"which is less than the width of some measures given the size {size} (measure {i + 1} has a width of {measureWidth}).");

            }

            //If we need a time signature for this measure, then include that.
            //Note that the time signature notation is NOT part of the measure GO--it is NOT a child of the measure GO.
            RectTransform timeSignatureRectTransform = null;
            if(i == 0 || (measures[i - 1].GetTimeSignature()[0] != measures[i].GetTimeSignature()[0] || measures[i - 1].GetTimeSignature()[1] != measures[i].GetTimeSignature()[1])) {

                timeSignatureRectTransform = InstantiateTimeSignature(measures[i].GetTimeSignature(), parent).GetComponent<RectTransform>();
                measureWidth += timeSignatureRectTransform.rect.width + betweenMeasuresGapLength * size;

            }

            if(notationRowLengths[rowCount] + measureWidth <= notationWrappingWidth) {

                //There is still room in the current row in our music notation
                if(timeSignatureRectTransform != null) {

                    //Make sure to include the time signature notation if applicable
                    measureRectTransforms[rowCount, colCount] = timeSignatureRectTransform;
                    colCount++;

                }
                measureRectTransforms[rowCount, colCount] = measureRectTransform;

                notationRowLengths[rowCount] += measureWidth;
                colCount++;

            } else {

                //We need to make a new row in our music notation
                rowCount++;
                colCount = 0;
                if(timeSignatureRectTransform != null) {

                    measureRectTransforms[rowCount, colCount] = timeSignatureRectTransform;
                    colCount++;

                }
                measureRectTransforms[rowCount, colCount] = measureRectTransform;

                notationRowLengths[rowCount] += measureWidth;
                colCount++;

                //Subtract the extra measure gap space at the end of the previous row
                notationRowLengths[rowCount - 1] -= size * (betweenMeasuresGapLength - barline.GetComponent<RectTransform>().rect.width / 2f);

            }

        }
        notationRowLengths[notationRowLengths.Length - 1] -= size * (betweenMeasuresGapLength - doubleBarline.GetComponent<RectTransform>().rect.width / 2f);

        //Use the calculated measure placements to actually place the measures in their correct spots
        PlaceMeasures(measureRectTransforms, parent, alignment, musicNotationBoundsInset, rowCount + 1, notationRowLengths);

        ApplyTiesToSheetMusic(measures, parent);

        this.size = 1;

    }

    public void CreateNoteGuideMusicNotation(Measure[] measures, RectTransform parent, Transform nodesContainer, float sizeAsFractionOfNodeWidth, float elementHoverDistance, float nodeSeparationPerBeat, Color? color = null) {

        //Find the last audible element
        int lastAudibleMeasureIndex = 0;
        int lastAudibleElementIndex = 0;
        bool canExitLoop = false;
        for(int m = measures.Length - 1; m >= 0; m--) {

            for(int i = measures[m].GetElementCount() - 1; i >= 0; i--) {

                if(measures[m][i] is Note && !(i > 0 && Note.IsTie(measures[m][i - 1], measures[m][i])) && !(i == 0 && m > 0 && Note.IsTie(measures[m - 1][measures[m - 1].GetElementCount() - 1], measures[m][i]))) {

                    lastAudibleMeasureIndex = m;
                    lastAudibleElementIndex = i;
                    canExitLoop = true;
                    break;

                }

            }

            if(canExitLoop) {

                break;

            }

        }

        Transform sampleNodeTransform = nodesContainer.GetChild(0).Find("Node");
        float nodeRadius = sampleNodeTransform.localScale.x * sampleNodeTransform.GetComponent<RectTransform>().rect.width / 2;
        float sizeInPixels = sizeAsFractionOfNodeWidth <= 0 ? nodeRadius * 2 : sizeAsFractionOfNodeWidth * nodeRadius * 2;
        size = sizeInPixels / filledNoteHead.GetComponent<RectTransform>().rect.width;
        int rowIndex = 0;
        float xPosition = nodesContainer.GetChild(rowIndex).localPosition.x - (nodesContainer.GetChild(rowIndex) as RectTransform).rect.width / 2 + nodeRadius;
        float yPosition = nodesContainer.GetChild(rowIndex).localPosition.y + (nodesContainer.GetChild(rowIndex) as RectTransform).rect.height / 2 + elementHoverDistance;
        for(int m = 0; m <= lastAudibleMeasureIndex; m++) {

            //int i will loop from 0 to lastAudibleElementIndex when m is equal to lastAudibleMeasureIndex. Otherwise, i will loop from 0 to the end of the measure.
            for(int i = 0; i < (m < lastAudibleMeasureIndex ? measures[m].GetElementCount() : lastAudibleElementIndex + 1); i++) {

                Element element = measures[m][i];

                if(element is Rest) {

                    GameObject restGO = InstantiateRest(element as Rest, measures[m].GetTimeSignature()[1], parent, color);
                    restGO.transform.localPosition = new Vector3(xPosition, yPosition + restGO.GetComponent<RectTransform>().rect.height / 2, 0);

                } else if(element is Note) {

                    GameObject noteGO = InstantiateNoteBody(element as Note, measures[m].GetTimeSignature()[1], parent, color);
                    noteGO.transform.localPosition = new Vector3(xPosition, yPosition + noteGO.GetComponent<RectTransform>().rect.height / 2, 0);
                    FlagNote(noteGO.transform, CalculateNumFlagsNeeded(element, measures[m].GetTimeSignature()[1]), color);

                }

                //Update the position for the next element.
                if(xPosition + element.GetBeats() * nodeSeparationPerBeat > parent.rect.width / 2 && rowIndex + 1 < nodesContainer.childCount) {

                    rowIndex++;
                    yPosition = nodesContainer.GetChild(rowIndex).localPosition.y + (nodesContainer.GetChild(rowIndex) as RectTransform).rect.height / 2 + elementHoverDistance;
                    xPosition = nodesContainer.GetChild(rowIndex).localPosition.x - (nodesContainer.GetChild(rowIndex) as RectTransform).rect.width / 2 + element.GetBeats() * nodeSeparationPerBeat - nodeRadius;

                } else {

                    xPosition += element.GetBeats() * nodeSeparationPerBeat;

                }

            }

        }

        //Tie notes that are supposed to be tied.
        int noteIndex = 0;
        for(int m = 0; m <= lastAudibleMeasureIndex; m++) {

            for(int i = 0; i < (m < lastAudibleMeasureIndex ? measures[m].GetElementCount() : lastAudibleElementIndex + 1); i++) {

                //If the note and the next note are in a tie, then tie them.
                if((i < measures[m].GetElementCount() - 1 && Note.IsTie(measures[m][i], measures[m][i + 1])) || (i == measures[m].GetElementCount() - 1 && m < lastAudibleMeasureIndex && Note.IsTie(measures[m][i], measures[m + 1][0]))) {

                    //Note that the guide music notation may not encompass all elements in measures[]
                    //because the guide music notation only contains music elements up until the last audible one.
                    //However, regardless if the next element is displayed in the guide or not, we need to show that this note is tied.
                    if(noteIndex < parent.childCount - 1) {

                        //Show that this note is tied to the next note (since the next note actually exists).
                        TieNotes(parent.GetChild(noteIndex), parent.GetChild(noteIndex + 1), color ?? Color.black);

                    } else {

                        //Show that this note is tied, but since the next note doesn't exist, only instantiate a tie half.
                        GameObject tieHalfGO = Instantiate(this.tieHalf, parent.GetChild(noteIndex));
                        RectTransform tieHalf = tieHalfGO.GetComponent<RectTransform>(); //We will be making a tie with its end on the RIGHT
                        tieHalf.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, tieHalf.rect.width * sizeAsFractionOfNodeWidth);
                        tieHalf.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, tieHalf.rect.height * sizeAsFractionOfNodeWidth);
                        tieHalf.GetComponent<Image>().color = color ?? Color.black;
                        tieHalf.localPosition = new Vector3(tieHalf.rect.width / 2f, (-parent.GetChild(noteIndex).GetComponent<RectTransform>().rect.height / 2f - tieHalf.rect.height / 2f) * tieYDisplacementMultiplier);

                    }

                }

                noteIndex++;

            }

        }

        size = 1;

    }

    private void PlaceMeasures(RectTransform[,] measureRectTransforms, RectTransform parent, string alignment, float musicNotationBoundsInset, int numRows, float[] notationRowLengths) {

        float individualRowHeight = measureRectTransforms[0, 0].rect.height;
        float measureYPosition = (numRows - 1) * (individualRowHeight + notationRowSpacing * size) / 2f;

        float maxNotationRowLength = -1;
        for(int i = 0; i < numRows; i++) {

            if(notationRowLengths[i] > maxNotationRowLength) {

                maxNotationRowLength = notationRowLengths[i];

            }

        }

        for(int r = 0; r < numRows; r++) {

            int measureIndex = 0;
            float measureXPosition; //The x position of the first measure in each row depends on the alignment. THIS IS THE ONLY THING THAT ALIGNMENT AFFECTS.
            if(alignment.Equals("center")) {

                measureXPosition = -notationRowLengths[r] / 2f + measureRectTransforms[r, 0].rect.width / 2f;

            } else if(alignment.Equals("left")) {

                measureXPosition = -parent.rect.width / 2f + measureRectTransforms[r, 0].rect.width / 2f + musicNotationBoundsInset;

            } else if(alignment.Equals("left-center")) {

                measureXPosition = -maxNotationRowLength / 2f + measureRectTransforms[r, 0].rect.width / 2f;

            } else {

                Debug.LogWarning($"SheetMusicUIManager: Alignment \"{alignment}\" is not a recognized alignment type.");
                measureXPosition = -maxNotationRowLength / 2f + measureRectTransforms[r, 0].rect.width / 2f;

            }

            bool rowHasMoreMeasures = true;
            while(rowHasMoreMeasures) {

                RectTransform measureRectTransform = measureRectTransforms[r, measureIndex];
                measureRectTransform.localPosition = new Vector3(measureXPosition, measureYPosition);

                if(measureRectTransform.name.Contains("Time Signature")) {

                    measureXPosition += measureRectTransform.rect.width / 2f + measureRectTransforms[r, measureIndex + 1].rect.width / 2f + betweenMeasuresGapLength * size;
                    rowHasMoreMeasures = measureIndex != measureRectTransforms.GetLength(1) - 1 && measureRectTransforms[r, measureIndex + 1] != null;
                    measureIndex++;
                    continue;

                }

                rowHasMoreMeasures = measureIndex != measureRectTransforms.GetLength(1) - 1 && measureRectTransforms[r, measureIndex + 1] != null;
                //Use the double barline if this measure is the LAST MEASURE OF THE LAST ROW. Otherwise use the regular barline
                RectTransform barlineRectTransform = (r == numRows - 1 && !rowHasMoreMeasures) ? Instantiate(doubleBarline).GetComponent<RectTransform>() : Instantiate(barline).GetComponent<RectTransform>();
                barlineRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, barlineRectTransform.rect.width * size);
                barlineRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, barlineRectTransform.rect.height * size);
                barlineRectTransform.GetComponent<Image>().color = Color.black;
                barlineRectTransform.SetParent(parent);
                barlineRectTransform.localScale = Vector3.one;
                barlineRectTransform.localPosition = new Vector3(measureRectTransform.localPosition.x + measureRectTransform.rect.width / 2f, measureYPosition);

                if(rowHasMoreMeasures) {

                    measureXPosition += measureRectTransform.rect.width / 2f + measureRectTransforms[r, measureIndex + 1].rect.width / 2f + betweenMeasuresGapLength * size;

                }

                measureIndex++;

            }

            measureYPosition -= individualRowHeight + notationRowSpacing * size;

        }

    }

    /// <summary>
    /// Generates the music notation for a measure. NOTE: This does not generate ties <br></br>
    /// All music notation gets parented under one game object.
    /// </summary>
    /// <param name="measure">The measure object to generate</param>
    /// <param name="parent">The parent of the game object that all music notation will be generated under</param>
    /// <param name="size">(optional) The size of the music notation, as a ratio between desired size and the original size of the sprites</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    private RectTransform CreateMeasureUI(Measure measure, Transform parent) {

        //Create the game object that will contain the measure's music notation
        GameObject measureGO = new GameObject("Unnamed Measure");
        RectTransform measureRectTransform = measureGO.AddComponent<RectTransform>();
        measureRectTransform.SetParent(parent);
        measureRectTransform.gameObject.tag = "Measure";
        measureRectTransform.localScale = Vector3.one;


        //Generate the UI
        float currXPosition = 0;
        //We will shorten the note separation factor for higher time signature denominators.
        float adjustedNoteSeparationFactor = noteSeparationFactor * 2 / Mathf.Sqrt(measure.GetTimeSignature()[1]);
        for(int i = 0; i < measure.GetElementCount(); i++) {

            Element element = measure[i];
            GameObject elementGO;

            if(element is Rest) {

                elementGO = InstantiateRest(element as Rest, measure.GetTimeSignature()[1], measureRectTransform);

            } else if(element is Note) {

                elementGO = InstantiateNoteBody(element as Note, measure.GetTimeSignature()[1], measureRectTransform);

            } else if(element is Tuplet) {

                elementGO = InstantiateTuplet(element as Tuplet, measureRectTransform);

            } else {

                throw new ArgumentException();

            }

            elementGO.transform.localPosition = new Vector3(currXPosition, 0);
            currXPosition += adjustedNoteSeparationFactor * LogisticCurve(element.GetBeats(), logisticCurveWidth, logisticCurveBottom, logisticCurveXAdjustment) * size;

        }

        ApplyFlagsAndBeamsToMeasure(measure, measureRectTransform);


        //Make the parent game object perfectly encompass the music notation

        RectTransform firstElementRectTransform = measureRectTransform.GetChild(0).GetComponent<RectTransform>();
        float measureMinimumX = firstElementRectTransform.localPosition.x - firstElementRectTransform.rect.width / 2f;
        RectTransform lastElementTransform = measureRectTransform.GetChild(measureRectTransform.childCount - 1).GetComponent<RectTransform>();
        //The measure bar should be placed closer to the last note if it has a shorter duration (ex: a measure bar is placed closer to a final eighth note than a final quarter note)
        float spacingAfterLastNote = adjustedNoteSeparationFactor * LogisticCurve(measure[measure.GetElementCount() - 1].GetBeats(), logisticCurveWidth, logisticCurveBottom, logisticCurveXAdjustment) * size;
        float measureMaximumX = lastElementTransform.localPosition.x + lastElementTransform.rect.width / 2f + spacingAfterLastNote;
        foreach(Transform elementComponent in measureRectTransform.GetChild(measureRectTransform.childCount - 1)) {

            RectTransform elementComponentRectTransform = elementComponent.GetComponent<RectTransform>();
            float rightExtentOfElement = lastElementTransform.localPosition.x + elementComponentRectTransform.localPosition.x + elementComponentRectTransform.rect.width / 2f;
            if(rightExtentOfElement > measureMaximumX) {

                measureMaximumX = rightExtentOfElement + spacingAfterLastNote;

            }

        }

        measureRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, measureMaximumX - measureMinimumX);
        measureRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size * (noteStemHeight + filledNoteHead.GetComponent<RectTransform>().rect.height / 2f));
        Vector3 xDisplacementAdjustment = new Vector3(-(measureMaximumX - measureMinimumX) / 2f + measureRectTransform.GetChild(0).GetComponent<RectTransform>().rect.width / 2f, 0, 0);
        Vector3 yDisplacementAdjustment = new Vector3(0, -measureRectTransform.rect.height / 2f + size * filledNoteHead.GetComponent<RectTransform>().rect.height / 2f, 0);
        for(int i = 0; i < measureRectTransform.childCount; i++) {

            Transform elementTransform = measureRectTransform.GetChild(i);
            elementTransform.localPosition += xDisplacementAdjustment;

            if(measure[i] is Note) {

                measureRectTransform.GetChild(i).localPosition += yDisplacementAdjustment;

            }

        }

        return measureRectTransform;

    }

    private GameObject InstantiateNoteBody(Note note, int timeSignatureBottom, Transform parent, Color? color = null) {

        float baseAmountOfBeats = note.GetBaseAmountOfBeats();
        float noteBaseDuration = Element.ConvertBeatsToValue(baseAmountOfBeats, timeSignatureBottom);

        GameObject noteHeadPrefab = (float) noteBaseDuration switch {

            1f => wholeNoteHead, 
            0.5f => hollowNoteHead,
            _ => filledNoteHead,

        };

        GameObject noteHeadGO = Instantiate(noteHeadPrefab, parent);
        RectTransform noteHeadRectTransform = noteHeadGO.GetComponent<RectTransform>();
        noteHeadGO.name = $"Note Body ({note}, {note.GetBeats()} beats)";
        noteHeadRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, noteHeadRectTransform.rect.width * size);
        noteHeadRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, noteHeadRectTransform.rect.height * size);
        noteHeadRectTransform.GetComponent<Image>().color = color ?? Color.black;

        if(noteBaseDuration == 1) {

            return noteHeadGO;

        }

        GameObject noteStemGO = Instantiate(noteStem, noteHeadGO.transform);
        RectTransform noteStemRectTransform = noteStemGO.GetComponent<RectTransform>();
        noteStemGO.name = "Note Stem";
        noteStemRectTransform.GetComponent<Image>().color = color ?? Color.black;

        noteStemRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, noteStemRectTransform.rect.width * size);
        noteStemRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, noteStemHeight * size);
        float noteStemXPosition = noteHeadRectTransform.rect.width / 2f - noteStemRectTransform.rect.width / 2f;
        float noteStemYPosition = noteStemHeight * size / 2f;
        noteStemGO.transform.localPosition = new Vector3(noteStemXPosition, noteStemYPosition);

        // Find out if we need a dot or not by seeing if the base duration equals the actual duration
        if(baseAmountOfBeats != note.GetBeats()) {

            GameObject dotGO = Instantiate(dot, noteHeadGO.transform);
            RectTransform dotRectTransform = dotGO.GetComponent<RectTransform>();
            dotRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, dotRectTransform.rect.width * size);
            dotRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, dotRectTransform.rect.height * size);
            dotRectTransform.GetComponent<Image>().color = color ?? Color.black;
            float dotXPosition = noteHeadRectTransform.rect.width / 2f + dotGO.GetComponent<RectTransform>().rect.width / 2f + dotXDisplacement * size;
            float dotYPosition = -noteHeadRectTransform.rect.height / 2f + dotYDisplacement * size;
            dotGO.transform.localPosition = new Vector3(dotXPosition, dotYPosition);

        }

        return noteHeadGO;

    }

    private GameObject InstantiateRest(Rest rest, int timeSignatureBottom, Transform parent, Color? color = null) {

        float baseAmountOfBeats = rest.GetBaseAmountOfBeats();
        float restBaseDuration = Element.ConvertBeatsToValue(baseAmountOfBeats, timeSignatureBottom);

        GameObject restPrefab = (float) restBaseDuration switch {

            0.0625f => sixteenthRest,
            0.125f => eighthRest,
            0.25f => quarterRest,
            0.5f => halfRest,
            1f => wholeRest,
            _ => throw new ArgumentException(),

        };

        GameObject restGO = Instantiate(restPrefab, parent);
        RectTransform restRectTransform = restGO.GetComponent<RectTransform>();
        restRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, restRectTransform.rect.width * size);
        restRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, restRectTransform.rect.height * size);
        restGO.name = (float) restBaseDuration switch {

            0.0625f => "Sixteenth Rest",
            0.125f => "Eighth Rest",
            0.25f => "Quarter Rest",
            0.5f => "Half Rest",
            1f => "Whole Rest",
            _ => throw new ArgumentException(),

        };
        restRectTransform.GetComponent<Image>().color = color ?? Color.black;

        // Find out if we need a dot or not by seeing if the base duration equals the actual duration
        if(baseAmountOfBeats != rest.GetBeats()) {

            GameObject dotGO = Instantiate(dot, restGO.transform);
            RectTransform dotRectTransform = dotGO.GetComponent<RectTransform>();
            dotRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, dotRectTransform.rect.width * size);
            dotRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, dotRectTransform.rect.height * size);
            dotRectTransform.GetComponent<Image>().color = color ?? Color.black;
            float dotXPosition = restGO.GetComponent<RectTransform>().rect.width / 2f + dotGO.GetComponent<RectTransform>().rect.width / 2f + dotXDisplacement * size;
            float dotYPosition = -restGO.GetComponent<RectTransform>().rect.height / 2f + dotYDisplacement * size;
            dotGO.transform.localPosition = new Vector3(dotXPosition, dotYPosition);

        }

        return restGO;

    }

    private GameObject InstantiateTimeSignature(int[] timeSignature, Transform parent, Color? color = null) {

        //Instantiate the top and bottom
        RectTransform topRectTransform = Instantiate(timeSignatureDigitPrefabs[timeSignature[0]], parent).GetComponent<RectTransform>();
        RectTransform bottomRectTransform = Instantiate(timeSignatureDigitPrefabs[timeSignature[1]], parent).GetComponent<RectTransform>();

        //Resize the top and bottom
        topRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, topRectTransform.rect.width * size);
        topRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, topRectTransform.rect.height * size);
        bottomRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, bottomRectTransform.rect.width * size);
        bottomRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, bottomRectTransform.rect.height * size);

        //Color the top and bottom
        topRectTransform.GetComponent<Image>().color = color ?? Color.black;
        bottomRectTransform.GetComponent<Image>().color = color ?? Color.black;

        //Create the parent GO (the calculations are made given the fact that the top and bottom sprites have the same dimensions)
        GameObject timeSignatureGO = new GameObject($"{timeSignature[0]}/{timeSignature[1]} Time Signature");
        timeSignatureGO.tag = "Other Music Notation";
        RectTransform timeSignatureRectTransform = timeSignatureGO.AddComponent<RectTransform>();
        timeSignatureRectTransform.SetParent(parent);
        timeSignatureRectTransform.localScale = Vector3.one;
        timeSignatureRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, topRectTransform.rect.width);
        timeSignatureRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, topRectTransform.rect.height * 2f);
        topRectTransform.SetParent(timeSignatureRectTransform);
        bottomRectTransform.SetParent(timeSignatureRectTransform);
        topRectTransform.localPosition = new Vector3(0, topRectTransform.rect.height / 2f);
        bottomRectTransform.localPosition = new Vector3(0, -bottomRectTransform.rect.height / 2f);

        return timeSignatureGO;

    }

    private GameObject InstantiateTuplet(Tuplet tuplet, Transform parent, Color? color = null) {

        return null;

    }

    private void ApplyFlagsAndBeamsToMeasure(Measure measure, Transform measureTransform) {

        if(measure.GetElementCount() <= 0) {

            return;

        }

        float beatCount = 0;
        int[] beatGrouping = measure.GetBeatGrouping();
        HashSet<float> strongBeats = new HashSet<float>();
        if(beatGrouping.Length == 1) {

            //If the beat grouping is just one number, then that means that it is a simple meter.
            //In simple meters, strong beats occur on every integer beat (in 4/4, the beat grouping is {4} and the strong beats are 0, 1, 2, and 3)
            for(int i = 0; i < beatGrouping[0]; i++) {

                strongBeats.Add(i);

            }

        } else {

            //If the beat grouping is more than one number, then that means it is either compound or odd.
            strongBeats.Add(0);
            int cumulativeBeats = beatGrouping[0];
            for(int i = 1; i < beatGrouping.Length; i++) {

                strongBeats.Add(cumulativeBeats);
                cumulativeBeats += beatGrouping[i];

            }

        }

        float flagXDisplacement = 0; //Stores the x-position shifting caused by placing flags (placing beams do not cause such shifting)
        for(int i = 0; i < measure.GetElementCount(); i++) {

            Element currElement = measure[i];
            bool prevElementForceUnbeam = false;
            bool currElementForceUnbeam = currElement is Note && (currElement as Note).IsForceUnbeam();
            bool nextElementForceUnbeam = false;
            int prevNumFlagsNeeded = 0;
            int currNumFlagsNeeded = CalculateNumFlagsNeeded(measure[i], measure.GetTimeSignature()[1]);
            int nextNumFlagsNeeded = 0;
            Transform prevElementTransform = null;
            Transform currElementTransform = measureTransform.GetChild(i);
            Transform nextElementTransform = null;
            if(i > 0) {

                Element prevElement = measure[i - 1];
                prevElementForceUnbeam = prevElement is Note && (prevElement as Note).IsForceUnbeam();
                prevNumFlagsNeeded = CalculateNumFlagsNeeded(prevElement, measure.GetTimeSignature()[1]);
                prevElementTransform = measureTransform.GetChild(i - 1);

            }
            if(i < measure.GetElementCount() - 1) {

                Element nextElement = measure[i + 1];
                nextElementForceUnbeam = nextElement is Note && (nextElement as Note).IsForceUnbeam();
                nextNumFlagsNeeded = CalculateNumFlagsNeeded(nextElement, measure.GetTimeSignature()[1]);
                nextElementTransform = measureTransform.GetChild(i + 1);

            }

            currElementTransform.localPosition += Vector3.right * flagXDisplacement;

            //Skip this element if it needs no flags/beams
            if(currNumFlagsNeeded <= 0 || currElement is not Note) {

                beatCount += currElement.GetBeats();
                continue;

            }

            //Count how many beams to the left and right are needed
            int numLeftBeamsNeeded = 0;
            int numRightBeamsNeeded = 0;
            bool isCurrentlyOnStrongBeat = strongBeats.Contains(beatCount);
            bool nextIsOnStrongBeat = strongBeats.Contains(beatCount + currElement.GetBeats());
            for(int beamCount = 1; beamCount <= currNumFlagsNeeded; beamCount++) {

                if(currElementForceUnbeam) {

                    break;

                }

                //We want to add a beam facing the previous note if it has the same or greater amount of beams as the current,
                //and we want to add a beam facing the next note if it has the same or greater amount of beams as the current
                bool needsLeftBeam = beamCount <= prevNumFlagsNeeded && !isCurrentlyOnStrongBeat && !prevElementForceUnbeam;
                bool needsRightBeam = beamCount <= nextNumFlagsNeeded && !nextIsOnStrongBeat && !nextElementForceUnbeam;
                if(!needsLeftBeam && !needsRightBeam) {

                    needsLeftBeam = !isCurrentlyOnStrongBeat && prevNumFlagsNeeded != 0 && !prevElementForceUnbeam;
                    needsRightBeam = !nextIsOnStrongBeat && nextNumFlagsNeeded != 0 && !nextElementForceUnbeam;

                }

                if(needsLeftBeam) {

                    numLeftBeamsNeeded++;

                }

                if(needsRightBeam) {

                    numRightBeamsNeeded++;

                }

            }

            //Do the actual beaming/flagging
            if(numLeftBeamsNeeded == 0 && numRightBeamsNeeded == 0) {

                FlagNote(currElementTransform, currNumFlagsNeeded);
                flagXDisplacement += flag.GetComponent<RectTransform>().rect.width;

            } else {

                /*
                if(nextElementTransform != null) {

                    print(nextElementTransform.name + " " + nextNumFlagsNeeded);

                }*/

                //Beam backwards
                if(prevElementTransform != null && prevNumFlagsNeeded != 0) {

                    BeamNotes(0, prevElementTransform, numLeftBeamsNeeded, currElementTransform);

                }
                //Beam forwards
                if(nextElementTransform != null && nextNumFlagsNeeded != 0) {

                    BeamNotes(numRightBeamsNeeded, currElementTransform, 0, nextElementTransform);

                }

            }

            beatCount += currElement.GetBeats();

        }

    }

    private void ApplyTiesToSheetMusic(Measure[] measures, Transform sheetMusicTransform) {

        int measureCount = 0;
        for(int i = 0; i < sheetMusicTransform.childCount; i++) {

            if(sheetMusicTransform.GetChild(i).CompareTag("Measure") == false) {

                continue;

            }

            for(int elementCount = 0; elementCount < sheetMusicTransform.GetChild(i).childCount; elementCount++) {

                Element currElement = measures[measureCount][elementCount];
                Element nextElement = null;
                Transform currTransform = sheetMusicTransform.GetChild(i).GetChild(elementCount);
                Transform nextTransform = null;
                if(elementCount < sheetMusicTransform.GetChild(i).childCount - 1) {

                    //If the next element would still be in the same measure
                    nextElement = measures[measureCount][elementCount + 1];
                    nextTransform = sheetMusicTransform.GetChild(i).GetChild(elementCount + 1);

                } else if(measureCount < measures.Length - 1) {

                    //If the next element would not be in the same measure

                    nextElement = measures[measureCount + 1][0];
                    for(int k = i + 1; k < sheetMusicTransform.childCount; k++) {

                        if(sheetMusicTransform.GetChild(k).tag == "Measure") {

                            nextTransform = sheetMusicTransform.GetChild(k).GetChild(0);
                            break;

                        }

                    }

                    measureCount++;

                }

                if(Note.IsTie(currElement, nextElement)) {

                    TieNotes(currTransform, nextTransform);

                } else if(currElement is Note && (currElement as Note).IsTied()) {

                    Debug.LogWarning($"SheetMusicUIManager: Invalid tie detected between notes {currElement} and {nextElement}");

                }

            }

        }

    }

    /// <summary>
    /// Beams notes by placing "half-beams" on the inner sides of the two. For example, a beamed pair of eighth notes will have two half-beams, one on the left and one on the right.
    /// A dotted eighth note beamed to a sixteenth note will have three half-beams--one on the right, two on the left.
    /// </summary>
    /// <param name="leftBeamCount"></param>
    /// <param name="leftNoteGO"></param>
    /// <param name="rightBeamCount"></param>
    /// <param name="rightNoteGO"></param>
    private void BeamNotes(int leftBeamCount, Transform leftNoteTransform, int rightBeamCount, Transform rightNoteTransform, Color? color = null) {

        if(leftBeamCount <= 0 && rightBeamCount <= 0) {

            return;
        
        }

        //Calculate how long the half-beams need to be
        float noteStemWidth = rightNoteTransform.Find("Note Stem").GetComponent<RectTransform>().rect.width;
        float distanceBetweenPositions = rightNoteTransform.localPosition.x - leftNoteTransform.localPosition.x - noteStemWidth;

        RectTransform noteStemRectTransform = leftNoteTransform.Find("Note Stem").GetComponent<RectTransform>();
        float stemYMax = noteStemRectTransform.rect.height;

        //Place all the left-side beams
        float currYPos = stemYMax;
        for(int i = 0; i < leftBeamCount; i++) {

            Rect leftNoteHeadRect = leftNoteTransform.GetComponent<RectTransform>().rect;
            GameObject beamGO = Instantiate(noteStem, leftNoteTransform);
            RectTransform beamRectTransform = beamGO.GetComponent<RectTransform>();
            beamGO.name = $"Beam {i}";
            beamRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, distanceBetweenPositions / 2f);
            beamRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, noteBeamHeight * size);
            beamRectTransform.GetComponent<Image>().color = color ?? Color.black;
            beamGO.transform.localPosition = new Vector3(leftNoteHeadRect.width / 2f + distanceBetweenPositions / 4f, currYPos - beamRectTransform.rect.height / 2f);
            currYPos -= beamRectTransform.rect.height + noteBeamSeparation * size;

        }

        //Place all the right-side beams
        currYPos = stemYMax;
        for(int i = 0; i < rightBeamCount; i++) {

            Rect rightNoteHeadRect = rightNoteTransform.GetComponent<RectTransform>().rect;
            GameObject beamGO = Instantiate(noteStem, rightNoteTransform);
            RectTransform beamRectTransform = beamGO.GetComponent<RectTransform>();
            beamGO.name = $"Beam {i}";
            beamRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, distanceBetweenPositions / 2f);
            beamRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, noteBeamHeight * size);
            beamRectTransform.GetComponent<Image>().color = color ?? Color.black;
            beamGO.transform.localPosition = new Vector3(rightNoteHeadRect.xMax - distanceBetweenPositions / 4f - noteStemWidth, currYPos - beamRectTransform.rect.height / 2f);
            currYPos -= beamRectTransform.rect.height + noteBeamSeparation * size;

        }

    }

    /// <summary>
    /// Places flags on a note
    /// </summary>
    /// <param name="noteGO"></param>
    /// <param name="numFlags"></param>
    private void FlagNote(Transform noteTransform, int numFlags, Color? color = null) {

        if(numFlags <= 0) {

            return;

        }

        RectTransform noteStemRectTransform = noteTransform.Find("Note Stem").GetComponent<RectTransform>();
        float stemYMax = noteStemRectTransform.localPosition.y + noteStemRectTransform.rect.height / 2f;
        float flagXPosition = noteStemRectTransform.localPosition.x + noteStemRectTransform.rect.width / 2f + flag.GetComponent<RectTransform>().rect.width / 2f * size;

        float currYPos = stemYMax;
        for(int i = 0; i < numFlags; i++) {

            GameObject flagGO = Instantiate(flag, noteTransform);
            RectTransform flagRectTransform = flagGO.GetComponent<RectTransform>();
            flagRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, flagRectTransform.rect.width * size);
            flagRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, flagRectTransform.rect.height * size);
            flagRectTransform.GetComponent<Image>().color = color ?? Color.black;
            flagGO.transform.localPosition = new Vector3(flagXPosition, currYPos - flagRectTransform.rect.height / 2f);
            currYPos -= noteFlagSeparation * size;

        }

    }

    private void TieNotes(Transform leftNoteTransform, Transform rightNoteTransform, Color? color = null) {

        if(leftNoteTransform.position.y == rightNoteTransform.position.y) {

            GameObject tieGO = Instantiate(tie, leftNoteTransform);
            tieGO.name = "Tie";
            RectTransform tieRectTransform = tieGO.GetComponent<RectTransform>();
            float distanceBetweenTransforms = rightNoteTransform.localPosition.x - leftNoteTransform.localPosition.x - (leftNoteTransform.parent.localPosition.x - rightNoteTransform.parent.localPosition.x);
            tieRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, distanceBetweenTransforms);
            tieRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, tieRectTransform.rect.height * size);
            tieRectTransform.GetComponent<Image>().color = color ?? Color.black;
            tieRectTransform.localPosition = new Vector3(tieRectTransform.rect.width / 2f, (-leftNoteTransform.GetComponent<RectTransform>().rect.height / 2f - tieRectTransform.rect.height / 2f) * tieYDisplacementMultiplier);

        } else {
            
            GameObject leftTieHalfGO = Instantiate(tieHalf, leftNoteTransform);
            RectTransform leftTieHalfRectTransform = leftTieHalfGO.GetComponent<RectTransform>();
            leftTieHalfRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, leftTieHalfRectTransform.rect.width * size);
            leftTieHalfRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, leftTieHalfRectTransform.rect.height * size);
            leftTieHalfRectTransform.GetComponent<Image>().color = color ?? Color.black;
            leftTieHalfRectTransform.localPosition = new Vector3(leftTieHalfRectTransform.rect.width / 2f, (-leftNoteTransform.GetComponent<RectTransform>().rect.height / 2f - leftTieHalfRectTransform.rect.height / 2f) * tieYDisplacementMultiplier);

            GameObject rightTieHalfGO = Instantiate(tieHalf, rightNoteTransform);
            RectTransform rightTieHalfRectTransform = rightTieHalfGO.GetComponent<RectTransform>();
            rightTieHalfRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, rightTieHalfRectTransform.rect.width * size);
            rightTieHalfRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, rightTieHalfRectTransform.rect.height * size);
            rightTieHalfRectTransform.GetComponent<Image>().color = color ?? Color.black;
            rightTieHalfRectTransform.localScale = new Vector3(-1, 1, 1);
            rightTieHalfRectTransform.localPosition = new Vector3(-rightTieHalfRectTransform.rect.width / 2f, (-rightNoteTransform.GetComponent<RectTransform>().rect.height / 2f - rightTieHalfRectTransform.rect.height / 2f) * tieYDisplacementMultiplier);

        }

    }

    private int CalculateNumFlagsNeeded(Element element, int timeSignatureBottom) {

        if(element is not Note) {

            return 0;

        }

        float baseValue = Element.ConvertBeatsToValue(element.GetBaseAmountOfBeats(), timeSignatureBottom);

        float result = Mathf.Clamp(-(Mathf.Log(baseValue, 2) + 2f), 0, 1000);
        if(result == Mathf.Floor(result)) {

            return (int) result;

        } else {

            throw new ArgumentException($"Tuplets have not been implemented yet (a note value of {baseValue})");

        }

    }

    /// <summary>
    /// Calculates the y-value in a logistic curve, given an x-value. <br></br>
    /// The equation is 1 - 1 / (bottom + width^(x - xAdjustment))
    /// </summary>
    /// <param name="x">The x-value</param>
    /// <param name="width">The width coefficient of the curve (a larger width coeffcient resutls in a smaller actual width)</param>
    /// <param name="bottom">The coefficient that affects the bottom of the curve (a larger coefficient means a higher bottom)</param>
    /// <param name="xAdjustment">Shifts the graph left and right</param>
    /// <returns></returns>
    private float LogisticCurve(float x, float width, float bottom, float xAdjustment) {

        return 1f - 1f / (bottom + Mathf.Pow(width, x - xAdjustment));
        
    }

}
