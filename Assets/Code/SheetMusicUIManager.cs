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
    [SerializeField] private GameObject[] numberPrefabs;

    [Header("Preferences")]
    [SerializeField] private float noteSeparationFactor = 150;
    [SerializeField] private float betweenMeasuresGapLength = 22.5f;
    [SerializeField] private float notationRowSpacing = 45;
    [SerializeField] private float timeSignatureSpacing = 50;
    [Space]
    [SerializeField] private float noteStemHeight = 60;
    [SerializeField] private float noteBeamHeight = 13;
    [SerializeField] private float noteBeamSeparation = 3;
    [SerializeField] private float noteFlagSeparation = 25;
    [SerializeField] private float dotXDisplacement = 7.5f;
    [SerializeField] private float dotYDisplacement = 10f;
    [SerializeField] private float tieYDisplacementMultiplier = 1.25f;
    [Space]
    [SerializeField] private float tupletDivisionNumberSize = 0.5f;
    [SerializeField] private float tupletDivisionNumberYSeparation = 5f;
    [SerializeField] private float tupletBracketHeight = 12.5f;
    [SerializeField] private float tupletBracketNumberHoleGapSize = 12.5f;
    [SerializeField] private float tupletBracketThickness = 5;
    [Space]
    [SerializeField] private float cutOffNoteBeamLength = 10;

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
    /// <param name="autoAdjustSize">(optional) If true, will expand the size of the music notation to the maximum size that fits the parent bounds. <br></br>The <i>size</i> parameter will act as the minimum size.<br></br>This would also ensure that the music notation would fit inside the bounds, no matter what.</param>
    public void CreateFullMusicNotationUI(Measure[] measures, RectTransform parent, string alignment = "left-center", float size = 1, bool autoAdjustSize = true) {

        if(parent == null) {

            Debug.LogWarning($"{GetType().Name}: A null parent for the music notation was given.");
            parent = transform.GetComponent<RectTransform>();

        }

        float maxRowWidth = parent.rect.width;

        this.size = size <= 0 ? 1 : size;

        //Calculate how the measures will be placed
        int[] numMeasuresPerRow = CalculateMeasurePlacement(measures, parent, maxRowWidth, this.size, out float longestRowWidth);

        if(autoAdjustSize) {

            this.size = size * maxRowWidth / longestRowWidth;
            longestRowWidth = maxRowWidth;

        }

        //Use the calculated measure placements to actually place the measures in their correct spots
        PlaceMeasures(measures, parent, numMeasuresPerRow, longestRowWidth);

        ApplyTiesToSheetMusic(measures, parent);

        this.size = 1;

    }

    /// <summary>
    /// Calculates how the measures would be placed in rows, and returns the lengths of the rows in pixels. <br></br>
    /// Also outputs the measure transforms that were generated to find the row placements. These transforms can be used to display the music notation. <br></br>
    /// Measures are placed in rows such that rows are as even as possible <br></br>
    /// NOTE that this is imperfect. Chances are, some music notation rows will end up being slightly longer than the maxRowLength. 
    /// </summary>
    /// <param name="measures">The measures</param>
    /// <param name="parent">The parent transform to generate the measure transforms in</param>
    /// <param name="maxRowWidth">The maximum row width, in pixels</param>
    /// <param name="size">The music notation size that is used to calculate this</param>
    /// <param name="measureTransforms">The measure transforms that were generated</param>
    /// <returns>An array of ints that indicate how many measures are in each row</returns>
    private int[] CalculateMeasurePlacement(Measure[] measures, Transform parent, float maxRowWidth, float size, out float longestRowWidth) {

        //First calculate the measure widths, and find if there are any measures that are wider than maxRowWidth
        float[] measureWidths = new float[measures.Length];
        float largestMeasureWidth = -1;
        for(int i = 0; i < measures.Length; i++) {

            //Check if the measure needs a time signature
            bool needsTimeSignature = i == 0 || measures[i - 1].GetTimeSignature()[0] != measures[i].GetTimeSignature()[0] || measures[i - 1].GetTimeSignature()[1] != measures[i].GetTimeSignature()[1];
            measureWidths[i] = CalculateMeasureUIWidth(measures[i], size, needsTimeSignature);

            if(measureWidths[i] > largestMeasureWidth) {

                largestMeasureWidth = measureWidths[i];

            }

        }

        //If there are measures wider than maxRowWidth, then change the size so that the widest measure would fit the length of maxRowWidth
        if(maxRowWidth < largestMeasureWidth) {

            size *= maxRowWidth / largestMeasureWidth;

        }


        //Simulate measure placement that does not account for evenness. All it does is place measures until the row reaches maxRowLength, at which it then forms a new row.
        int requiredNumRows = 1; //This variable contains how many rows there MUST be in the final music notation, whether or not measures are evenly placed
        float combinedMeasuresWidth = 0; //The total length of all the measures
        float rowWidth = measureWidths[0];
        for(int i = 1; i < measureWidths.Length; i++) { //We start the for-loop on the second measure

            //Add this measure's width, along with the spacing from the previous measure
            rowWidth += betweenMeasuresGapLength * size + measureWidths[i];

            combinedMeasuresWidth += betweenMeasuresGapLength * size + measureWidths[i];

            //Check if we've gone over the maxRowWidth
            if(rowWidth > maxRowWidth) {

                //We have to put this measure into a new row
                requiredNumRows++;
                rowWidth = measureWidths[i];

                continue;

            }

        }


        //Now repeat the process, but instead of using maxRowWidth to find when rows end, use the average row width.
        //Also, instead of ending a row immediately when a measure goes over the threshold, we will TRY to keep that measure on the same row.
        //However, we will still check to see if that measure can stay on the row.
        int[] numMeasuresPerRows = new int[requiredNumRows];
        int rowIndex = 0;
        float averageRowWidth = combinedMeasuresWidth / requiredNumRows; //This calculation is going to be slightly wrong since the measure spacing was different when combinedMeasuresWidth was calculated.
        rowWidth = measureWidths[0];
        numMeasuresPerRows[0] = 1;
        longestRowWidth = measureWidths[0];
        for(int i = 1; i < measureWidths.Length; i++) {

            //Check if we will go over the average row width
            if(rowWidth + betweenMeasuresGapLength * size + measureWidths[i] > averageRowWidth) {

                //If we will go over, then see if we can still keep this measure on this row
                if(rowWidth + betweenMeasuresGapLength * size + measureWidths[i] <= maxRowWidth) {

                    //We can keep this measure
                    rowWidth += betweenMeasuresGapLength * size + measureWidths[i]; //We still add betweenMeasuresGapLength because it is the gap between this and the prev. measure
                    numMeasuresPerRows[rowIndex]++;
                    rowIndex++;

                    if(rowWidth > longestRowWidth) {

                        longestRowWidth = rowWidth;

                    }
                    rowWidth = 0;

                } else {

                    //We cannot keep this measure, and we have to make a new row
                    rowIndex++;
                    numMeasuresPerRows[rowIndex] = 1;

                    if(rowWidth > longestRowWidth) {

                        longestRowWidth = rowWidth;

                    }
                    rowWidth = measureWidths[i];

                }

            } else {

                //If we're not ending any rows, then add the measure width and measure separation width
                rowWidth += betweenMeasuresGapLength * size + measureWidths[i];
                numMeasuresPerRows[rowIndex]++;

            }

        }

        return numMeasuresPerRows;

    }

    /// <summary>
    /// Generates the measure UI and places them according to a pre-planned layout. The layout should be an int array with each index representing the number of measures in a row.
    /// </summary>
    /// <param name="measures">The measures</param>
    /// <param name="numMeasuresPerRow">The measure layout as an int[]. Each index is the number of measures in a row.</param>
    /// <param name="parent">The parent transform to put all the measure UI in</param>
    /// <param name="alignment">The alignment of the UI (left or left-center, left-center having the shape of left alignment but being centered in the parent transform)</param>
    /// <param name="musicNotationBoundsInset"></param>
    /// <param name="numRows"></param>
    /// <param name="longestRowWidth"></param>
    private void PlaceMeasures(Measure[] measures, RectTransform parent, int[] numMeasuresPerRow, float longestRowWidth, string alignment = "left-center", Color? color = null) {

        //First generate all of the measure transforms
        RectTransform[] measureTransforms = new RectTransform[measures.Length];
        for(int i = 0; i < measures.Length; i++) {

            //Check if the measure needs a time signature
            if(i == 0 || measures[i - 1].GetTimeSignature()[0] != measures[i].GetTimeSignature()[0] || measures[i - 1].GetTimeSignature()[1] != measures[i].GetTimeSignature()[1]) {

                measureTransforms[i] = CreateMeasureUI(measures[i], parent, true);

            } else {

                measureTransforms[i] = CreateMeasureUI(measures[i], parent, false);

            }

            measureTransforms[i].name = $"Measure {i + 1}";

        }

        //The following is calculated using math and geometry,
        //and the beginning measure's y-position should be (numRows - 1) * (rowHeight + rowSpacing).
        float measureHeight = size * (noteStemHeight + filledNoteHead.GetComponent<RectTransform>().rect.height / 2);
        float measureYPosition = (numMeasuresPerRow.Length - 1) * (measureHeight + size * notationRowSpacing) / 2f;
        int measureIndex = 0;
        for(int r = 0; r < numMeasuresPerRow.Length; r++) {

            float measureXPosition;

            //Find the left bound of this music notation row
            if(alignment == "left") {

                measureXPosition = -parent.rect.width / 2;

            } else if(alignment == "left-center") {

                measureXPosition = -longestRowWidth / 2;

            } else {

                throw new ArgumentException($"PlaceMeasures-- alignment {alignment} is not supported (\"left\" and \"left-center\" are supported)");

            }

            float endOfRowIndex = measureIndex + numMeasuresPerRow[r];
            while(measureIndex < endOfRowIndex) {

                //Place the measure
                measureTransforms[measureIndex].localPosition = new Vector3(measureXPosition + measureTransforms[measureIndex].rect.width / 2, measureYPosition);

                //Create a barline
                RectTransform barlineTransform;
                if(r == numMeasuresPerRow.Length - 1 && measureIndex == endOfRowIndex - 1) {

                    barlineTransform = Instantiate(doubleBarline, parent).GetComponent<RectTransform>();
                    barlineTransform.name = "Double Barline";

                } else {

                    barlineTransform = Instantiate(barline, parent).GetComponent<RectTransform>();
                    barlineTransform.name = "Barline";

                }
                barlineTransform.localScale = Vector3.one;
                barlineTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, barlineTransform.rect.width * size);
                barlineTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, barlineTransform.rect.height * size);
                barlineTransform.GetComponent<Image>().color = color ?? Color.black;
                barlineTransform.localPosition = new Vector3(measureXPosition + measureTransforms[measureIndex].rect.width, measureYPosition);

                //Update the new measureXPosition
                measureXPosition += measureTransforms[measureIndex].rect.width + betweenMeasuresGapLength * size;

                measureIndex++;

            }

            measureYPosition -= measureHeight + notationRowSpacing * size;

        }

    }

    /// <summary>
    /// Calculates what the width of the displayed UI would be for a measure
    /// </summary>
    /// <param name="measure"></param>
    /// <param name="size"></param>
    /// <param name="includeTimeSignature"></param>
    /// <returns></returns>
    private float CalculateMeasureUIWidth(Measure measure, float size, bool includeTimeSignature) {

        float GetElementPrefabWidth(Element element) {

            //The first element, in this case, would be the first element.
            if(element is Rest) {

                return size * GetRestPrefab(Element.ConvertBeatsToValue(measure[0].GetBaseAmountOfBeats(), measure.GetTimeSignature()[1])).GetComponent<RectTransform>().rect.width / 2;

            } else if(element is Note) {

                return size * GetNoteBodyPrefab(Element.ConvertBeatsToValue(measure[0].GetBaseAmountOfBeats(), measure.GetTimeSignature()[1])).GetComponent<RectTransform>().rect.width / 2;

            } else if(element is Tuplet tuplet) {

                if(tuplet[0] is Rest) {

                    return size * GetRestPrefab(Element.ConvertBeatsToValue(tuplet[0].GetBaseAmountOfBeats(), measure.GetTimeSignature()[1])).GetComponent<RectTransform>().rect.width / 2;

                } else if(tuplet[0] is Note) {

                    return size * GetNoteBodyPrefab(Element.ConvertBeatsToValue(tuplet[0].GetBaseAmountOfBeats(), measure.GetTimeSignature()[1])).GetComponent<RectTransform>().rect.width / 2;

                } else if(tuplet[0] is Tuplet) {

                    throw new ArgumentException("CalculateMeasureUIWidth-- nested tuplets have not been implemented");

                } else throw new ArgumentException($"CalculateMeasureUIWidth-- element type {tuplet[0].GetType().Name} is not recognized");

            } else throw new ArgumentException($"CalculateMeasureUIWidth-- element type {element.GetType().Name} is not recognized");

        }

        bool ElementNeedsFlag(IElementGroup elementGroup, int elementIndex, HashSet<float> strongBeats, float beatCount) {

            //Check if this note needs a flag (if so, then add the flag width to measureWidth)
            int prevFlagsBeamsNeeded = 0;
            if(elementIndex > 0) {

                prevFlagsBeamsNeeded = CalculateNumFlagsOrBeamsNeeded(elementGroup[elementIndex - 1], measure.GetTimeSignature()[1]);

            }
            int currFlagsBeamsNeeded = CalculateNumFlagsOrBeamsNeeded(elementGroup[elementIndex], measure.GetTimeSignature()[1]);
            int nextFlagsBeamsNeeded = 0;
            if(elementIndex < elementGroup.Count - 1) {

                nextFlagsBeamsNeeded = CalculateNumFlagsOrBeamsNeeded(elementGroup[elementIndex + 1], measure.GetTimeSignature()[1]);

            }

            if(currFlagsBeamsNeeded == 0) {

                return false;

            }

            bool prevElementForceUnbeam = elementIndex > 0 && elementGroup[elementIndex - 1] is Note && (elementGroup[elementIndex - 1] as Note).IsForceUnbeam();
            bool currElementForceUnbeam = elementGroup[elementIndex] is Note && (elementGroup[elementIndex] as Note).IsForceUnbeam();

            //Beaming left will occur if the current element is not on a strong beat and if the previous note has beams
            bool needsLeftBeam = prevFlagsBeamsNeeded != 0 && !strongBeats.Contains(beatCount) && !prevElementForceUnbeam;
            //Beaming right will occur if the NEXT element is not on a strong beat and if the next note needs beams
            bool needsRightBeam = nextFlagsBeamsNeeded != 0 && !strongBeats.Contains(beatCount + elementGroup[elementIndex].GetBeats()) && !currElementForceUnbeam;

            //Check if no beams are applicable, and a flag is required
            if(!needsLeftBeam && !needsRightBeam) {

                return true;

            } else {

                return false;

            }

        }


        float measureWidth = 0;

        //Note that element width is ignored. That is, element separation is solely based off of noteSeparationFactor.
        //However, even though element width is technically ignored, the first element's (and the last element's) half-width still need to be accounted for.
        //This stems from the fact that the localPosition of a transform is located at the transform's center.
        if(includeTimeSignature) {

            //The first element, in this case, would be the time signature.
            float noteStemHeightToTimeSigBaseHeightRatio = (noteStemHeight + filledNoteHead.GetComponent<RectTransform>().rect.height / 2) / numberPrefabs[0].GetComponent<RectTransform>().rect.height / 2;
            measureWidth = noteStemHeightToTimeSigBaseHeightRatio * size * Mathf.Max(numberPrefabs[measure.GetTimeSignature()[0]].GetComponent<RectTransform>().rect.width, numberPrefabs[measure.GetTimeSignature()[1]].GetComponent<RectTransform>().rect.width);
            measureWidth += timeSignatureSpacing * size;

        } else {

            //The first element, in this case, would be the first element.
            measureWidth += GetElementPrefabWidth(measure[0]);

        }

        //Mimic element placement
        float adjustedNoteSeparationFactor = noteSeparationFactor * 2 / Mathf.Sqrt(measure.GetTimeSignature()[1]); //We will shorten the note separation factor for higher time signature denominators
        float beatCount = 0;
        //Calculate where the strong beats are
        HashSet<float> strongBeats = new HashSet<float> { 0 };
        int cumulativeBeats = measure.GetBeatGrouping()[0];
        for(int i = 1; i < measure.GetBeatGrouping().Length; i++) {

            strongBeats.Add(cumulativeBeats);
            cumulativeBeats += measure.GetBeatGrouping()[i];

        }
        for(int e = 0; e < measure.Count - 1; e++) { //Do not add the note spacing after the last the element (we will do that separately)

            Element element = measure[e];

            if(element is not Tuplet) {

                measureWidth += adjustedNoteSeparationFactor * size * LogisticCurve(element.GetBeats(), logisticCurveWidth, logisticCurveBottom, logisticCurveXAdjustment);

                if(ElementNeedsFlag(measure, e, strongBeats, beatCount)) {

                    measureWidth += flag.GetComponent<RectTransform>().rect.width * size;

                }

                beatCount += element.GetBeats();

            } else {

                Tuplet tuplet = (Tuplet) element;
                float tupletBeatCount = 0;
                for(int t = 0; t < tuplet.Count; t++) {

                    measureWidth += adjustedNoteSeparationFactor * size * LogisticCurve(tuplet[t].GetBeats(), logisticCurveWidth, logisticCurveBottom, logisticCurveXAdjustment);

                    if(ElementNeedsFlag(tuplet, t, new HashSet<float> { 0 }, tupletBeatCount)) {

                        measureWidth += flag.GetComponent<RectTransform>().rect.width * size;

                    }

                    tupletBeatCount += tuplet[t].GetBeats();

                }

                beatCount += tuplet.GetBeats();

            }

        }

        //Now add the right extent of this measure

        IElementGroup elementGroupContainingLastElement; //The group that the last element is directly in (either the measure or a tuplet)
        if(measure[^1] is Tuplet lastTuplet) {

            elementGroupContainingLastElement = lastTuplet;
            //Add the width of the tuplet (up until the last element)
            for(int t = 0; t < lastTuplet.Count - 1; t++) {

                measureWidth += adjustedNoteSeparationFactor * size * LogisticCurve(elementGroupContainingLastElement[t].GetBeats(), logisticCurveWidth, logisticCurveBottom, logisticCurveXAdjustment);

            }

        } else {

            elementGroupContainingLastElement = measure;

        }

        //First set the lastElementRightExtent to be the note separation after the last note. This is the minimum it can be.
        float lastElementRightExtent = adjustedNoteSeparationFactor * LogisticCurve(elementGroupContainingLastElement[^1].GetBeats(), logisticCurveWidth, logisticCurveBottom, logisticCurveXAdjustment) * size;

        //We do not need to check if there is a dot because, when generating the measure UI, dots DON'T add to a note's separation

        //Check if there will be a flag because flags DO add to a note's separation (note: CalculateNumFlagsNeeded() auto checks if the element is a Note)
        bool isBeamedToPrevElement = elementGroupContainingLastElement.Count > 1 && CalculateNumFlagsOrBeamsNeeded(elementGroupContainingLastElement[^2], measure.GetTimeSignature()[1]) > 0;
        if(CalculateNumFlagsOrBeamsNeeded(elementGroupContainingLastElement[^1], measure.GetTimeSignature()[1]) > 0 && !isBeamedToPrevElement) {

            lastElementRightExtent += flag.GetComponent<RectTransform>().rect.width * size;

        }

        measureWidth += lastElementRightExtent;

        return measureWidth;

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
    private RectTransform CreateMeasureUI(Measure measure, Transform parent, bool needsTimeSignature = false) {

        //Create the game object that will contain the measure's music notation.
        GameObject measureGO = new GameObject("Unnamed Measure");
        RectTransform measureTransform = measureGO.AddComponent<RectTransform>();
        measureTransform.SetParent(parent);
        measureTransform.gameObject.tag = "Measure";
        measureTransform.localScale = Vector3.one;


        //Generate the UI
        float currXPosition = 0;
        
        if(needsTimeSignature) {

            RectTransform timeSignatureTransform = InstantiateTimeSignature(measure.GetTimeSignature(), measureTransform).GetComponent<RectTransform>();
            timeSignatureTransform.name = $"{measure.GetTimeSignature()[0]}/{measure.GetTimeSignature()[1]} Time Signature";
            timeSignatureTransform.localScale = Vector3.one;
            timeSignatureTransform.localPosition = new Vector3(0, 0);
            currXPosition = timeSignatureTransform.rect.width / 2 + timeSignatureSpacing * size;

        }

        //Generate the notes/rests
        float adjustedNoteSeparationFactor = noteSeparationFactor * 2 / Mathf.Sqrt(measure.GetTimeSignature()[1]); //We will shorten the note separation factor for higher time signature denominators
        for(int i = 0; i < measure.Count; i++) {

            Element element = measure[i];
            GameObject elementGO;

            if(element is Rest) {

                elementGO = InstantiateRest(element as Rest, measure.GetTimeSignature()[1], measureTransform);

            } else if(element is Note) {

                elementGO = InstantiateNoteBody(element as Note, measure.GetTimeSignature()[1], measureTransform);

            } else if(element is Tuplet) {

                elementGO = InstantiateTuplet(element as Tuplet, measure.GetTimeSignature()[1], measureTransform, adjustedNoteSeparationFactor);
                RectTransform elementTransform = elementGO.GetComponent<RectTransform>();
                //We will place the tuplet transform so that the first element in the tuplet has the x-position of currXPosition
                elementGO.transform.localPosition = new Vector3(currXPosition, 0) + Vector3.right * (elementTransform.rect.width / 2 - elementTransform.GetChild(0).GetComponent<RectTransform>().rect.width / 2);
                currXPosition += elementTransform.rect.width - elementTransform.GetChild(0).GetComponent<RectTransform>().rect.width / 2;
                continue;

            } else {

                throw new ArgumentException();

            }

            elementGO.transform.localPosition = new Vector3(currXPosition, 0);
            currXPosition += adjustedNoteSeparationFactor * size * LogisticCurve(element.GetBeats(), logisticCurveWidth, logisticCurveBottom, logisticCurveXAdjustment);

        }

        ApplyFlagsAndBeamsToElementGroup(measure, measureTransform, measure.GetTimeSignature()[1], needsTimeSignature);


        //Make the parent game object perfectly encompass the music notation.

        //First find how wide the parent game object should be
        RectTransform firstElementTransform = measureTransform.GetChild(0).GetComponent<RectTransform>();
        float measureMinimumX = firstElementTransform.localPosition.x - firstElementTransform.rect.width / 2f;

        IElementGroup elementGroupContainingLastElement = measure[^1] is Tuplet ? measure[^1] as Tuplet : measure;
        RectTransform lastElementTransform;
        if(measure[^1] is Tuplet) {

            Transform tupletTransform = measureTransform.GetChild(measureTransform.childCount - 1);
            //Get the 2nd last child because the last child of a tuplet is the tuplet numbering
            lastElementTransform = tupletTransform.GetChild(tupletTransform.childCount - 2).GetComponent<RectTransform>();

        } else {

            lastElementTransform = measureTransform.GetChild(measureTransform.childCount - 1).GetComponent<RectTransform>();

        }
        float measureMaximumX = lastElementTransform.localPosition.x + adjustedNoteSeparationFactor * size * LogisticCurve(elementGroupContainingLastElement[^1].GetBeats(), logisticCurveWidth, logisticCurveBottom, logisticCurveXAdjustment);
        if(elementGroupContainingLastElement is Tuplet) {

            measureMaximumX += lastElementTransform.parent.localPosition.x;

        }
        bool isBeamedToPrevElement = elementGroupContainingLastElement.Count > 1 && CalculateNumFlagsOrBeamsNeeded(elementGroupContainingLastElement[^2], measure.GetTimeSignature()[1]) > 0;
        if(CalculateNumFlagsOrBeamsNeeded(elementGroupContainingLastElement[^1], measure.GetTimeSignature()[1]) > 0 && !isBeamedToPrevElement) {

            measureMaximumX += flag.GetComponent<RectTransform>().rect.width * size;

        }

        //Redefine the measure container transform's dimensions (this won't automatically keep the element transforms centered).
        measureTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, measureMaximumX - measureMinimumX);
        measureTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size * (noteStemHeight + filledNoteHead.GetComponent<RectTransform>().rect.height / 2f));

        //Center the element transforms.
        Vector3 xDisplacementAdjustment = new Vector3(-(measureMaximumX - measureMinimumX) / 2f + firstElementTransform.rect.width / 2f - firstElementTransform.localPosition.x, 0, 0);
        Vector3 yDisplacementAdjustment = new Vector3(0, -measureTransform.rect.height / 2f + size * filledNoteHead.GetComponent<RectTransform>().rect.height / 2f, 0);
        //If there's a time signature, then manually add the x-adjustment to the time signature transform
        if(needsTimeSignature) { 

            measureTransform.GetChild(0).localPosition += xDisplacementAdjustment;

        }
        for(int i = needsTimeSignature ? 1 : 0; i < measureTransform.childCount; i++) {

            Transform elementTransform = measureTransform.GetChild(i);
            elementTransform.localPosition += xDisplacementAdjustment;

            if(measure[i - (needsTimeSignature ? 1 : 0)] is Note) {

                measureTransform.GetChild(i).localPosition += yDisplacementAdjustment;

            }

        }

        return measureTransform;

    }

    /// <param name="noteBaseDuration">The base duration of a note (ex: an eighth note in 4/4 is 0.5, and an eighth note in 6/8 is STILL 0.5</param>
    /// <returns>The note body prefab</returns>
    private GameObject GetNoteBodyPrefab(float noteBaseDuration) {

        return (float) noteBaseDuration switch {

            1f => wholeNoteHead,
            0.5f => hollowNoteHead,
            _ => filledNoteHead,

        };

    }

    private GameObject GetRestPrefab(float restBaseDuration) {

        return (float) restBaseDuration switch {

            0.0625f => sixteenthRest,
            0.125f => eighthRest,
            0.25f => quarterRest,
            0.5f => halfRest,
            1f => wholeRest,
            _ => throw new ArgumentException(),

        };

    }

    private GameObject InstantiateNoteBody(Note note, int timeSignatureBottom, Transform parent, Color? color = null) {

        float baseAmountOfBeats = note.GetBaseAmountOfBeats();
        float noteBaseDuration = Element.ConvertBeatsToValue(baseAmountOfBeats, timeSignatureBottom);

        GameObject noteHeadPrefab = GetNoteBodyPrefab(noteBaseDuration);

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
            dotGO.name = "Dot";
            float dotXPosition = noteHeadRectTransform.rect.width / 2f + dotGO.GetComponent<RectTransform>().rect.width / 2f + dotXDisplacement * size;
            float dotYPosition = -noteHeadRectTransform.rect.height / 2f + dotYDisplacement * size;
            dotGO.transform.localPosition = new Vector3(dotXPosition, dotYPosition);

        }

        return noteHeadGO;

    }

    private GameObject InstantiateRest(Rest rest, int timeSignatureBottom, Transform parent, Color? color = null) {

        float baseAmountOfBeats = rest.GetBaseAmountOfBeats();
        float restBaseDuration = Element.ConvertBeatsToValue(baseAmountOfBeats, timeSignatureBottom);

        GameObject restPrefab = GetRestPrefab(restBaseDuration);

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

    private GameObject InstantiateTuplet(Tuplet tuplet, int timeSignatureBottom, Transform parent, float noteSeparationFactor, Color? color = null) {

        GameObject tupletGO = new GameObject(tuplet.ToString());
        RectTransform tupletTransform = tupletGO.AddComponent<RectTransform>();
        tupletTransform.SetParent(parent);
        tupletTransform.localScale = Vector3.one;

        float currXPosition = 0;
        for(int i = 0; i < tuplet.Count; i++) {

            Element element = tuplet[i];
            GameObject elementGO;

            if(element is Rest) {

                elementGO = InstantiateRest(element as Rest, timeSignatureBottom, tupletTransform, color);

            } else if(element is Note) {

                elementGO = InstantiateNoteBody(element as Note, timeSignatureBottom, tupletTransform, color);

            } else if(element is Tuplet) {

                throw new ArgumentException("InstantiateTuplet-- Nested tuplets have not been implemented yet (an element in the Tuplet parameter is a tuplet)");

            } else throw new ArgumentException();

            elementGO.transform.localPosition = new Vector3(currXPosition, 0);
            currXPosition += noteSeparationFactor * size * LogisticCurve(tuplet[i].GetBeats(), logisticCurveWidth, logisticCurveBottom, logisticCurveXAdjustment);

        }


        //Apply beaming and flagging.
        ApplyFlagsAndBeamsToElementGroup(tuplet, tupletTransform, timeSignatureBottom, false);

        //Center the element transforms.
        RectTransform firstElementTransform = tupletTransform.GetChild(0).GetComponent<RectTransform>();
        float tupletMinX = -firstElementTransform.rect.width / 2;
        RectTransform lastElementTransform = tupletTransform.GetChild(tupletTransform.childCount - 1).GetComponent<RectTransform>();
        float spacingAfterLastNote = noteSeparationFactor * LogisticCurve(tuplet[^1].GetBeats(), logisticCurveWidth, logisticCurveBottom, logisticCurveXAdjustment) * size - lastElementTransform.rect.width / 2f;
        float tupletMaxX = lastElementTransform.localPosition.x + lastElementTransform.rect.width / 2 + spacingAfterLastNote;
        for(int i = 0; i < lastElementTransform.childCount; i++) {

            RectTransform elementComponentRectTransform = lastElementTransform.GetChild(i).GetComponent<RectTransform>();
            float rightExtentOfElement = lastElementTransform.localPosition.x + elementComponentRectTransform.localPosition.x + elementComponentRectTransform.rect.width / 2f + spacingAfterLastNote;
            if(rightExtentOfElement > tupletMaxX) {

                tupletMaxX = rightExtentOfElement;

            }

        }

        tupletTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, tupletMaxX - tupletMinX);
        tupletTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size * (noteStemHeight + filledNoteHead.GetComponent<RectTransform>().rect.height / 2f));

        float xDisplacementAdjustment = -(tupletMaxX - tupletMinX) / 2f + tupletTransform.GetChild(0).GetComponent<RectTransform>().rect.width / 2f;
        float yDisplacementAdjustment = -tupletTransform.rect.height / 2f + size * filledNoteHead.GetComponent<RectTransform>().rect.height / 2f;
        for(int i = 0; i < tupletTransform.childCount; i++) {

            Transform elementTransform = tupletTransform.GetChild(i);
            elementTransform.localPosition += Vector3.right * xDisplacementAdjustment;

            if(tuplet[i] is Note) {

                tupletTransform.GetChild(i).localPosition += Vector3.up * yDisplacementAdjustment;

            }

        }


        //Add the tuplet number/bracket

        //First check if we need a bracket or just a number (see if all notes are beamed)
        bool allNotesWereBeamed = true;
        for(int i = 0; i < tuplet.Count; i++) {

            //If there's a rest, then not all notes were beamed
            if(tuplet[i] is Rest) {

                allNotesWereBeamed = false;
                break;

            }

            //If there's a note that's a quarter note or longer, then not all notes were beamed (only eighth notes and shorter can have beams/flags)
            float baseValue = Element.ConvertBeatsToValue(tuplet[i].GetBaseAmountOfBeats(), timeSignatureBottom);
            if(baseValue >= 0.25f) { //If the note is a quarter note or longer

                allNotesWereBeamed = false;
                break;

            }

            //If there's a note that's forced to unbeam, then not all notes were beamed
            if(tuplet[i] is Note note && note.IsForceUnbeam()) {

                allNotesWereBeamed = false;
                break;

            }

        }

        //Actually create the numbering or bracket
        GameObject tupletNumberingContainer = new GameObject("Tuplet Numbering Container");
        tupletNumberingContainer.tag = "Other Music Notation";
        RectTransform tupletNumberingContainerTransform = tupletNumberingContainer.AddComponent<RectTransform>();
        tupletNumberingContainerTransform.SetParent(tupletTransform);
        tupletNumberingContainerTransform.localPosition = Vector3.zero;
        tupletNumberingContainerTransform.localScale = Vector3.one;
        RectTransform tupletNumberTransform = Instantiate(numberPrefabs[tuplet.GetNumDivisions()], tupletNumberingContainerTransform).GetComponent<RectTransform>();
        tupletNumberTransform.name = "Division Number";
        tupletNumberTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, tupletNumberTransform.rect.width * size * tupletDivisionNumberSize);
        tupletNumberTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, tupletNumberTransform.rect.height * size * tupletDivisionNumberSize);
        //Add the tuplet bracket if not all notes were beamed.
        if(!allNotesWereBeamed) {

            //Note: this below is NOT the same as tupletTransform.rect.width
            float leftTupletVisibleBound = -tupletTransform.rect.width / 2;
            float rightTupletVisibleBound = tupletMaxX - spacingAfterLastNote + xDisplacementAdjustment;
            float tupletNumberHeight = tupletTransform.rect.height / 2 + tupletDivisionNumberYSeparation * size + tupletNumberTransform.rect.height / 2;
            tupletNumberTransform.localPosition = new Vector3((leftTupletVisibleBound + rightTupletVisibleBound) / 2, tupletNumberHeight);

            RectTransform leftBracketHeightTransform = Instantiate(noteStem, tupletNumberingContainerTransform).GetComponent<RectTransform>();
            leftBracketHeightTransform.name = "Left Bracket Height";
            leftBracketHeightTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, tupletBracketThickness * size);
            leftBracketHeightTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, tupletBracketHeight * size);
            RectTransform rightBracketHeightTransform = Instantiate(leftBracketHeightTransform, tupletNumberingContainerTransform).GetComponent<RectTransform>();
            rightBracketHeightTransform.name = "Right Bracket Height";

            RectTransform leftBracketLengthTransform = Instantiate(noteStem, tupletNumberingContainerTransform).GetComponent<RectTransform>();
            leftBracketLengthTransform.name = "Left Bracket Length";
            leftBracketLengthTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, (rightTupletVisibleBound - leftTupletVisibleBound) / 2 - tupletBracketNumberHoleGapSize * size);
            leftBracketLengthTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, tupletBracketThickness * size);
            RectTransform rightBracketLengthTransform = Instantiate(leftBracketLengthTransform, tupletNumberingContainerTransform).GetComponent<RectTransform>();
            rightBracketLengthTransform.name = "Right Bracket Length";

            leftBracketHeightTransform.localPosition = new Vector3(leftTupletVisibleBound + leftBracketHeightTransform.rect.width / 2, tupletNumberHeight - leftBracketHeightTransform.rect.height / 2 + leftBracketLengthTransform.rect.height / 2);
            rightBracketHeightTransform.localPosition = new Vector3(rightTupletVisibleBound - rightBracketHeightTransform.rect.width / 2, tupletNumberHeight - rightBracketHeightTransform.rect.height / 2 + rightBracketLengthTransform.rect.height / 2);
            leftBracketLengthTransform.localPosition = new Vector3(leftTupletVisibleBound + leftBracketLengthTransform.rect.width / 2, tupletNumberHeight);
            rightBracketLengthTransform.localPosition = new Vector3(rightTupletVisibleBound - rightBracketLengthTransform.rect.width / 2, tupletNumberHeight);

        } else {

            //If all notes were beamed, then there will be a single beam running across the entire tuplet.
            //The number goes at the center of this single beam.
            tupletNumberTransform.localPosition = new Vector3((firstElementTransform.localPosition.x + firstElementTransform.rect.width / 2 - noteStem.GetComponent<RectTransform>().rect.width * size + lastElementTransform.localPosition.x + lastElementTransform.rect.width / 2) / 2, 
                tupletTransform.rect.height / 2 + tupletDivisionNumberYSeparation * size + tupletNumberTransform.rect.height / 2);

        }

        return tupletGO;

    }

    private GameObject InstantiateTimeSignature(int[] timeSignature, Transform parent, Color? color = null) {

        //Instantiate the top and bottom
        RectTransform topRectTransform = Instantiate(numberPrefabs[timeSignature[0]], parent).GetComponent<RectTransform>();
        RectTransform bottomRectTransform = Instantiate(numberPrefabs[timeSignature[1]], parent).GetComponent<RectTransform>();

        //Resize the top and bottom
        float noteStemHeightToTimeSigBaseHeightRatio = (noteStemHeight + filledNoteHead.GetComponent<RectTransform>().rect.height / 2) / numberPrefabs[0].GetComponent<RectTransform>().rect.height / 2;
        topRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, topRectTransform.rect.width * size * noteStemHeightToTimeSigBaseHeightRatio);
        topRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, topRectTransform.rect.height * size * noteStemHeightToTimeSigBaseHeightRatio);
        bottomRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, bottomRectTransform.rect.width * size * noteStemHeightToTimeSigBaseHeightRatio);
        bottomRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, bottomRectTransform.rect.height * size * noteStemHeightToTimeSigBaseHeightRatio);

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

    private void ApplyFlagsAndBeamsToElementGroup(IElementGroup elementGroup, Transform elementGroupTransform, int timeSignatureBottom, bool hasTimeSignature) {

        if(elementGroup.Count <= 0) {

            return;

        }

        float beatCount = 0;
        int[] beatGrouping;
        if(elementGroup is Measure) {

            beatGrouping = (elementGroup as Measure).GetBeatGrouping();

        } else {

            beatGrouping = new int[] { -1 };

        }

        //Calculate where the strong beats are
        HashSet<float> strongBeats = new HashSet<float> { 0 };
        int cumulativeBeats = beatGrouping[0];
        for(int i = 1; i < beatGrouping.Length; i++) {

            strongBeats.Add(cumulativeBeats);
            cumulativeBeats += beatGrouping[i];

        }

        float flagXDisplacement = 0; //Stores the x-position shifting caused by placing flags (placing beams do not cause such shifting)
        for(int i = 0; i < elementGroup.Count; i++) {

            Element currElement = elementGroup[i];
            bool prevElementForceUnbeam = false;
            bool currElementForceUnbeam = currElement is Note && (currElement as Note).IsForceUnbeam();
            //Note that we don't need a "nextElementForceUnbeam" because force unbeaming only applies forward--that is, a note with force unbeam will only force unbeam with the NEXT note
            int prevNumFlagsBeamsNeeded = 0;
            int currNumFlagsBeamsNeeded = CalculateNumFlagsOrBeamsNeeded(elementGroup[i], timeSignatureBottom);
            int nextNumFlagsBeamsNeeded = 0;
            Transform prevElementTransform = null;
            Transform currElementTransform = elementGroupTransform.GetChild(i + (hasTimeSignature ? 1 : 0));
            Transform nextElementTransform = null;
            if(i > 0) {

                Element prevElement = elementGroup[i - 1];
                prevElementForceUnbeam = prevElement is Note && (prevElement as Note).IsForceUnbeam();
                prevNumFlagsBeamsNeeded = CalculateNumFlagsOrBeamsNeeded(prevElement, timeSignatureBottom);
                prevElementTransform = elementGroupTransform.GetChild(i - 1 + (hasTimeSignature ? 1 : 0));

            }
            if(i < elementGroup.Count - 1) {

                Element nextElement = elementGroup[i + 1];
                nextNumFlagsBeamsNeeded = CalculateNumFlagsOrBeamsNeeded(nextElement, timeSignatureBottom);
                nextElementTransform = elementGroupTransform.GetChild(i + 1 + (hasTimeSignature ? 1 : 0));

            }

            currElementTransform.localPosition += Vector3.right * flagXDisplacement;

            //Skip this element if it needs no flags/beams
            if(currNumFlagsBeamsNeeded <= 0) {

                beatCount += currElement.GetBeats();
                continue;

            }

            //Count how many beams to the left and right are needed
            int numLeftBeamsNeeded = 0;
            int numRightBeamsNeeded = 0;
            bool isCurrentlyOnStrongBeat = strongBeats.Contains(beatCount);
            bool nextIsOnStrongBeat = strongBeats.Contains(beatCount + currElement.GetBeats());
            for(int beamCount = 1; beamCount <= currNumFlagsBeamsNeeded; beamCount++) {

                //We want to add a beam facing the previous note if it has the same or greater amount of beams as the current,
                //and we want to add a beam facing the next note if it has the same or greater amount of beams as the current
                bool needsLeftBeam = beamCount <= prevNumFlagsBeamsNeeded && !isCurrentlyOnStrongBeat && !prevElementForceUnbeam;
                bool needsRightBeam = beamCount <= nextNumFlagsBeamsNeeded && !nextIsOnStrongBeat && !currElementForceUnbeam;
                //If neither of those are the case, then we can still beam...
                // - For the left beam, IF we're currently NOT on a strong beat, and the prev. note needed a beam
                // - For the right beam, IF we're currently ON a strong beat, and the next note needs a beam
                if(!needsLeftBeam && !needsRightBeam) { 

                    needsLeftBeam = !isCurrentlyOnStrongBeat && prevNumFlagsBeamsNeeded != 0 && !prevElementForceUnbeam;
                    needsRightBeam = !nextIsOnStrongBeat && nextNumFlagsBeamsNeeded != 0 && !currElementForceUnbeam;

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

                FlagNote(currElementTransform, currNumFlagsBeamsNeeded);
                flagXDisplacement += flag.GetComponent<RectTransform>().rect.width * size;

            } else {

                //Beam backwards
                if(prevElementTransform != null && prevNumFlagsBeamsNeeded != 0) {

                    BeamNotes(0, prevElementTransform, numLeftBeamsNeeded, currElementTransform);

                }
                //Beam forwards
                if(nextElementTransform != null && nextNumFlagsBeamsNeeded != 0) {

                    BeamNotes(numRightBeamsNeeded, currElementTransform, 0, nextElementTransform);

                }

            }

            beatCount += currElement.GetBeats();

        }

    }

    private void ApplyTiesToSheetMusic(Measure[] measures, Transform musicNotationTransform) {

        //Loop through each measure and each element in each measure
        for(int m = 0; m < measures.Length; m++) { //"i" is the index of the music notation TRANSFORM

            RectTransform measureTransform = musicNotationTransform.GetChild(m).GetComponent<RectTransform>();
            if(measureTransform.CompareTag("Other Music Notation")) { //Check that this "measure" transform is ACTUALLY a measure

                //This transform does not represent a measure (probably a barline or something)
                continue;

            }

            //Check if the measure has a time signature
            bool hasTimeSignature = measureTransform.GetChild(0).CompareTag("Other Music Notation");
            for(int e = 0; e < measures[m].Count; e++) {

                Element element = measures[m][e];
                Element nextElement = null;
                Transform elementTransform = measureTransform.GetChild(e + (hasTimeSignature ? 1 : 0));
                Transform nextElementTransform = null;

                //Fill out any ties that are INSIDE the tuplet (so ignore the tuplet's last element)
                if(element is Tuplet tuplet) {

                    for(int t = 0; t < tuplet.Count - 1; t++) {

                        Element tupletElement = tuplet[t];
                        Element nextTupletElement = tuplet[t + 1];
                        Transform tupletElementTransform = elementTransform.GetChild(t);
                        Transform nextTupletElementTransform = elementTransform.GetChild(t + 1);

                        if(Note.IsTie(tupletElement, nextTupletElement)) {

                            TieNotes(tupletElementTransform, nextTupletElementTransform);

                        }

                    }

                } //If this element is a tuplet, the following code will take care if the tuplet's last note has a tie

                //Assign nextElement and nextTransform
                if(e < measures[m].Count - 1) { //Next element is in the same measure

                    nextElement = measures[m][e + 1];
                    nextElementTransform = measureTransform.GetChild(e + 1 + (hasTimeSignature ? 1 : 0));

                } else if(m < measures.Length - 1) { //Next element isn't in the same measure

                    nextElement = measures[m + 1][0];
                    if(musicNotationTransform.GetChild(m + 1).GetChild(0).CompareTag("Other Music Notation")) {

                        nextElementTransform = musicNotationTransform.GetChild(m + 1).GetChild(1);

                    } else {

                        nextElementTransform = musicNotationTransform.GetChild(m + 1).GetChild(0);

                    }

                }

                if(Note.IsTie(element, nextElement)) {

                    TieNotes(elementTransform, nextElementTransform);

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
        float noteStemWidth = noteStem.GetComponent<RectTransform>().rect.width * size;
        float distanceBetweenPositions;

        if(leftNoteTransform != null && rightNoteTransform != null) {

            distanceBetweenPositions = rightNoteTransform.localPosition.x - leftNoteTransform.localPosition.x - noteStemWidth;

        } else if(leftNoteTransform != null || rightNoteTransform != null) {

            distanceBetweenPositions = cutOffNoteBeamLength * 2;

        } else throw new ArgumentException("BeamNotes-- both leftNoteTransform and rightNoteTransform are null");

        float stemYMax = noteStemHeight * size;

        //Place all the left-side beams
        float currYPos = stemYMax;
        for(int i = 0; i < leftBeamCount; i++) {

            Rect leftNoteHeadRect = leftNoteTransform.GetComponent<RectTransform>().rect;
            GameObject beamGO = Instantiate(noteStem, leftNoteTransform);
            RectTransform beamRectTransform = beamGO.GetComponent<RectTransform>();
            beamGO.name = "Beam";
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
            beamGO.name = "Beam";
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
            flagGO.name = "Flag";
            RectTransform flagRectTransform = flagGO.GetComponent<RectTransform>();
            flagRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, flagRectTransform.rect.width * size);
            flagRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, flagRectTransform.rect.height * size);
            flagRectTransform.GetComponent<Image>().color = color ?? Color.black;
            flagGO.transform.localPosition = new Vector3(flagXPosition, currYPos - flagRectTransform.rect.height / 2f);
            currYPos -= noteFlagSeparation * size;

        }

    }

    private void TieNotes(Transform leftNoteTransform, Transform rightNoteTransform, Color? color = null) {

        //First detect if either left note transform or right note transform is a tuplet.
        //Note that a tuplet transform's  last child is always a numbering/bracket GO, of which is tagged as "Other Music Notation"
        Transform leftTupletTransform = null;
        Transform rightTupletTransform = null;
        if(leftNoteTransform.childCount > 0 && leftNoteTransform.GetChild(leftNoteTransform.childCount - 1).CompareTag("Other Music Notation")) {

            leftTupletTransform = leftNoteTransform;
            //Set the left note transform as the last element transform in the tuplet
            leftNoteTransform = leftNoteTransform.GetChild(leftNoteTransform.childCount - 2);
            //Also temporarily put this transform outside of the tuplet
            leftNoteTransform.SetParent(leftNoteTransform.parent.parent);

        }
        if(rightNoteTransform.childCount > 0 && rightNoteTransform.GetChild(rightNoteTransform.childCount - 1).CompareTag("Other Music Notation")) {

            rightTupletTransform = rightNoteTransform;
            //Set the right note transform as the first element transform in the tuplet
            rightNoteTransform = rightNoteTransform.GetChild(0);
            //Also temporarily put this transform outside of the tuplet
            rightNoteTransform.SetParent(rightNoteTransform.parent.parent);

        }

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
            leftTieHalfGO.name = "Left Tie Half";
            RectTransform leftTieHalfRectTransform = leftTieHalfGO.GetComponent<RectTransform>();
            leftTieHalfRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, leftTieHalfRectTransform.rect.width * size);
            leftTieHalfRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, leftTieHalfRectTransform.rect.height * size);
            leftTieHalfRectTransform.GetComponent<Image>().color = color ?? Color.black;
            leftTieHalfRectTransform.localPosition = new Vector3(leftTieHalfRectTransform.rect.width / 2f, (-leftNoteTransform.GetComponent<RectTransform>().rect.height / 2f - leftTieHalfRectTransform.rect.height / 2f) * tieYDisplacementMultiplier);

            GameObject rightTieHalfGO = Instantiate(tieHalf, rightNoteTransform);
            leftTieHalfGO.name = "Right Tie Half";
            RectTransform rightTieHalfRectTransform = rightTieHalfGO.GetComponent<RectTransform>();
            rightTieHalfRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, rightTieHalfRectTransform.rect.width * size);
            rightTieHalfRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, rightTieHalfRectTransform.rect.height * size);
            rightTieHalfRectTransform.GetComponent<Image>().color = color ?? Color.black;
            rightTieHalfRectTransform.localScale = new Vector3(-1, 1, 1);
            rightTieHalfRectTransform.localPosition = new Vector3(-rightTieHalfRectTransform.rect.width / 2f, (-rightNoteTransform.GetComponent<RectTransform>().rect.height / 2f - rightTieHalfRectTransform.rect.height / 2f) * tieYDisplacementMultiplier);

        }

        //Return the notes to their tuplet parents, if applicable
        if(leftTupletTransform != null) {

            leftNoteTransform.SetParent(leftTupletTransform);
            leftNoteTransform.SetSiblingIndex(leftTupletTransform.childCount - 2);

        }
        if(rightTupletTransform != null) {

            rightNoteTransform.SetParent(rightTupletTransform);
            rightNoteTransform.SetSiblingIndex(0);

        }

    }

    public void CreateNodeGuideMusicNotation(Measure[] measures, RectTransform parent, RectTransform nodesContainer, float noteHeadDiameterPixels, float elementHoverDistance, float nodeSeparationPerBeat, float noteStemHeightFactor, Color? color = null) {

        RectTransform sampleNode = nodesContainer.GetChild(0).GetChild(1).GetComponent<RectTransform>();
        float nodeRadius = sampleNode.rect.width / 2 * sampleNode.localScale.x;
        print($"NODERADIUS: {nodeRadius}");
        void CheckToUpdateRow(ref float xPosition, ref float yPosition, ref float rowRightBound, ref int rowIndex, float nodeSeparation) {

            //Check if the x-position will be going out of bounds of the current node row
            if(xPosition > rowRightBound) {

                rowIndex++;
                xPosition = nodesContainer.GetChild(rowIndex).localPosition.x - nodesContainer.GetChild(rowIndex).GetComponent<RectTransform>().rect.width / 2;
                xPosition += nodeSeparation - nodeRadius;
                yPosition = nodesContainer.GetChild(rowIndex).localPosition.y + elementHoverDistance + noteHeadDiameterPixels / 2;
                rowRightBound = nodesContainer.GetChild(rowIndex).localPosition.x + nodesContainer.GetChild(rowIndex).GetComponent<RectTransform>().rect.width / 2;

            }

        }

        List<Element> elements = Measure.MeasureArrayAsList(measures);

        //Set the scale of the music notation
        size = noteHeadDiameterPixels / filledNoteHead.GetComponent<RectTransform>().rect.width;
        float originalNoteStemHeight = noteStemHeight;
        noteStemHeight *= noteStemHeightFactor;
        elementHoverDistance *= size;

        //We will be taking each element, and we will keep track of each music notation's x-position using the note separation per beat
        int timeSignatureBottom = measures[0].GetTimeSignature()[1];
        float xPosition = nodesContainer.GetChild(0).localPosition.x - nodesContainer.GetChild(0).GetComponent<RectTransform>().rect.width / 2 + nodeRadius;
        float yPosition = nodesContainer.GetChild(0).localPosition.y + elementHoverDistance + noteHeadDiameterPixels / 2;
        float rowRightBound = nodesContainer.GetChild(0).localPosition.x + nodesContainer.GetChild(0).GetComponent<RectTransform>().rect.width / 2;
        float nodeSeparation = 0;
        int rowIndex = 0;
        for(int e = 0; e < elements.Count; e++) {

            //Check if the x-position will be going out of bounds of the current node row
            if(rowIndex + 1 < nodesContainer.childCount) {

                CheckToUpdateRow(ref xPosition, ref yPosition, ref rowRightBound, ref rowIndex, nodeSeparation);

            }

            if(elements[e] is Note note) {

                RectTransform noteTransform = InstantiateNoteBody(note, timeSignatureBottom, parent, color).GetComponent<RectTransform>();
                noteTransform.localPosition = new Vector3(xPosition, yPosition);
                nodeSeparation = elements[e].GetBeats() * nodeSeparationPerBeat;
                xPosition += nodeSeparation;

            } else if(elements[e] is Rest rest) {

                RectTransform restTransform = InstantiateRest(rest, timeSignatureBottom, parent, color).GetComponent<RectTransform>();
                restTransform.localPosition = new Vector3(xPosition, yPosition);
                nodeSeparation = elements[e].GetBeats() * nodeSeparationPerBeat;
                xPosition += nodeSeparation;

            } else if(elements[e] is Tuplet tuplet) {

                for(int t = 0; t < tuplet.Count; t++) {

                    //Check if the x-position will be going out of bounds of the current node row
                    if(rowIndex + 1 < nodesContainer.childCount - 1) {

                        CheckToUpdateRow(ref xPosition, ref yPosition, ref rowRightBound, ref rowIndex, nodeSeparation);

                    }

                    if(tuplet[t] is Note tupletNote) {

                        RectTransform noteTransform = InstantiateNoteBody(tupletNote, timeSignatureBottom, parent, color).GetComponent<RectTransform>();
                        noteTransform.localPosition = new Vector3(xPosition, yPosition);
                        nodeSeparation = tuplet.GetRealBeatsForElement(t) * nodeSeparationPerBeat;
                        xPosition += nodeSeparation;

                    } else if(tuplet[t] is Rest tupletRest) {

                        RectTransform restTransform = InstantiateRest(tupletRest, timeSignatureBottom, parent, color).GetComponent<RectTransform>();
                        restTransform.localPosition = new Vector3(xPosition, yPosition);
                        nodeSeparation = tuplet.GetRealBeatsForElement(t) * nodeSeparationPerBeat;
                        xPosition += nodeSeparation;

                    }

                }

            }

        }

        ////Loop through each node transform in nodesContainer
        //int e = 0; //The element count
        //for(int r = 0; r < nodesContainer.childCount; r++) {

        //    Transform nodeRow = nodesContainer.GetChild(r);
        //    for(int n = 1 /* skip the zeroth child because it's the connector beam */; n < nodeRow.childCount; n++) {

        //        RectTransform node = nodeRow.GetChild(n).GetComponent<RectTransform>();

        //        if(elements[e] is Rest rest) {

        //            RectTransform restTransform = InstantiateRest(rest, timeSignatureBottom, parent, color).GetComponent<RectTransform>();
        //            restTransform.position = node.position;
        //            restTransform.localPosition += Vector3.up * (elementHoverDistance + restTransform.rect.height / 2);

        //        } else if(elements[e] is Note note) {

        //            RectTransform noteTransform = InstantiateNoteBody(note, timeSignatureBottom, parent, color).GetComponent<RectTransform>();
        //            noteTransform.position = node.position;
        //            noteTransform.localPosition += Vector3.up * (elementHoverDistance + noteTransform.rect.height / 2);

        //        } else if(elements[e] is Tuplet tuplet) {

        //            for(int t = 0; t < tuplet.Count; t++) {

        //                node = nodeRow.GetChild(n).GetComponent<RectTransform>();

        //                if(tuplet[t] is Rest tupletRest) {

        //                    RectTransform restTransform = InstantiateRest(tupletRest, timeSignatureBottom, parent, color).GetComponent<RectTransform>();
        //                    restTransform.position = node.position;
        //                    restTransform.localPosition += Vector3.up * (elementHoverDistance + restTransform.rect.height / 2);

        //                } else if(tuplet[t] is Note tupletNote) {

        //                    RectTransform noteTransform = InstantiateNoteBody(tupletNote, timeSignatureBottom, parent, color).GetComponent<RectTransform>();
        //                    noteTransform.position = node.position;
        //                    noteTransform.localPosition += Vector3.up * (elementHoverDistance + noteTransform.rect.height / 2);

        //                } else throw new ArgumentException($"CreateNodeGuideMusicNotation-- element type {tuplet[t].GetType().Name} is not supported to be in a tuplet");

        //                if(t >= tuplet.Count - 1) {

        //                    continue;

        //                }

        //                n++;
        //                if(n >= nodeRow.childCount) {

        //                    n = 0;
        //                    r++;

        //                }

        //            }

        //        }

        //        e++;

            //}

        //}

        //Apply beaming and flagging
        

        //Tie notes that are supposed to be tied
        for(int e = 0; e < parent.childCount; e++) {

            if(e < elements.Count - 1 && Note.IsTie(elements[e], elements[e + 1])) {

                if(e < parent.childCount - 1) {

                    //Tie this note to the next note (since the next note exists in the guide music notation)
                    TieNotes(parent.GetChild(e), parent.GetChild(e + 1), color ?? Color.black);

                } else {

                    //Put a half-tie on this note since this note is the last audible element
                    GameObject tieHalfGO = Instantiate(this.tieHalf, parent.GetChild(e));
                    RectTransform tieHalf = tieHalfGO.GetComponent<RectTransform>(); //We will be making a tie with its end on the RIGHT
                    tieHalf.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, tieHalf.rect.width * size);
                    tieHalf.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, tieHalf.rect.height * size);
                    tieHalf.GetComponent<Image>().color = color ?? Color.black;
                    tieHalf.localPosition = new Vector3(tieHalf.rect.width / 2f, (-parent.GetChild(e).GetComponent<RectTransform>().rect.height / 2f - tieHalf.rect.height / 2f) * tieYDisplacementMultiplier);

                }

            }

        }

        //Reset the music notation scale
        size = 1;
        noteStemHeight = originalNoteStemHeight;

    }

    private int CalculateNumFlagsOrBeamsNeeded(Element element, int timeSignatureBottom) {

        if(element is not Note) {

            return 0;

        }

        float baseValue = Element.ConvertBeatsToValue(element.GetBaseAmountOfBeats(), timeSignatureBottom);

        float result = Mathf.Clamp(-(Mathf.Log(baseValue, 2) + 2f), 0, 1000);
        if(result == Mathf.Floor(result)) {

            return (int) result;

        } else {

            throw new ArgumentException($"CalculateNumFlagsOrBeamsNeeded-- invalid amount of beats {element.GetBeats()} with note value of {baseValue})");

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
