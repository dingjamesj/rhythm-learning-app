using System;
using System.Collections.Generic;

/// <summary>
/// An immutable class that represents a measure with a time signature.
/// </summary>
public class Measure : IElementGroup {

    private readonly List<Element> elements;
    private readonly int[] timeSignature;
    private readonly int[] beatGrouping;

    public Measure(List<Element> elements = null, int timeSignatureTop = 4, int timeSignatureBottom = 4, int[] beatGrouping = null) {

        timeSignature = new int[] { timeSignatureTop, timeSignatureBottom };
        this.elements = new List<Element>();
        
        //If there wasn't an element sequence given, then just fill the measure with constant notes.
        if(elements == null) {

            for(int i = 0; i < timeSignatureTop; i++) {

                this.elements.Add(new Note(beats: 1f / timeSignatureBottom));

            }

            return;

        }

        //Use the given element sequence.
        if(CanHoldElements(elements, timeSignature)) {

            for(int i = 0; i < elements.Count; i++) {

                this.elements.Add(elements[i]);

            }

        } else {

            throw new ArgumentException("Measure cannot hold that many elements.");

        }

        //Set the default beat grouping.
        if(beatGrouping == null && timeSignature[0] % 3 == 0 && timeSignature[1] == 8) {

            //If there was no beat grouping given, and the time signature can be expressed as 3n/8, then divide the beats into groups of 3.
            this.beatGrouping = new int[timeSignatureTop / 3];
            for(int i = 0; i < this.beatGrouping.Length; i++) {

                this.beatGrouping[i] = 3;

            }

        } else if(beatGrouping == null) {

            //If there was no beat grouping given, and it is not a 3n/8 time signature, then just put all the beats into one group.
            this.beatGrouping = new int[timeSignatureTop];
            for(int i = 0; i < this.beatGrouping.Length; i++) {

                this.beatGrouping[i] = 1;

            }

        } else {

            int beatSum = 0;
            this.beatGrouping = new int[beatGrouping.Length];
            for(int i = 0; i < beatGrouping.Length; i++) {

                beatSum += beatGrouping[i];
                this.beatGrouping[i] = beatGrouping[i];

            }

            if(beatSum != timeSignatureTop) {

                string str = "";
                for(int i = 0; i < beatGrouping.Length; i++) {

                    str += $"{beatGrouping[i]} ";

                }
                throw new ArgumentException($"The beat grouping of [{str.Substring(0, str.Length - 2)}] is incompatible with the time signature of {timeSignatureTop}/{timeSignatureBottom}");

            }

        }

    }
    
    public Element this[int i] {

        get {

            if(i < 0 || i >= elements.Count) {

                throw new ArgumentException($"Measure: Index {i} is out of range for measure of length {Count}.");

            } else {

                return elements[i];

            }

        }

    }

    public int Count { get { return elements.Count; } }

    public int[] GetTimeSignature() {

        return new int[] { timeSignature[0], timeSignature[1] };

    }

    public int[] GetBeatGrouping() {

        int[] temp = new int[beatGrouping.Length];
        for(int i = 0; i < beatGrouping.Length; i++) {

            temp[i] = beatGrouping[i];

        }

        return temp;

    }

    private bool CanHoldElements(List<Element> elements, int[] timeSignature) {

        double totalBeats = 0;
        for(int i = 0; i < elements.Count; i++) {

            totalBeats += elements[i].GetBeats();

        }

        return totalBeats <= timeSignature[0];

    }

    /// <summary>
    /// <u><b>Text input must be formatted in the following way:</b></u><br></br>
    /// 
    /// - Each measure is on its own line, and each line contains a measure<br></br>
    /// - Each line begins with the time signature, followed by the beat grouping, and ending with a colon and a space (ex. "4/4: " or "5/8 (2 + 3): ")<br></br>
    /// - Following the colon and space are the musical elements (notes and rests)<br></br>
    /// - Each musical element must be separated by a space<br></br>
    /// - Each musical element must describe, in order, the number of beats taken up, the musical pitch (or "R" if it is a rest), and then any extra arguments<br></br>
    /// - The properties of a musical element must be separated by a forwards slash "/"<br></br>
    /// - The musical pitch must be written as the letter name, the accidental (if applicable), and the octave (ex. "Cb4")<br></br>
    /// - Accidentals are typed according to the following: <br></br>
    /// ----- # = sharp <br></br>
    /// ----- b = flat <br></br>
    /// ----- * = double sharp <br></br>
    /// ----- d = double flat <br></br>
    /// - Extra args for notes: <br></br>
    /// ----- T = tie <br></br>
    /// ----- U = force unbeam <br></br>
    /// - Extra args must be separated by a comma, NO SPACE (ex. "T,U" NOT "T, U")<br></br>
    /// - Tuplets, however, should be notated <i>[number of divisions]</i>=(<i>[note]-[note]-[note] <b>etc.</b></i>)<br></br>
    /// - Ex. a quarter-note triplet in 4/4 would be "4/4: 3=(1-1-1)", an eighth-note duplet in 6/8 would be "6/8: 2=(1-1)", <br></br>- and an eighth-note triplet with a rest in the middle in 4/4 would be "4/4: 3=(0.5-0.5/R-0.5)"<br></br><br></br>
    /// <b>Finally, you can omit the musical pitch and/or extra arguments if they are unnecessary for your purpose. <br></br>
    /// <u>However, you may not omit the note duration, and the order MUST always be duration/pitch/extra arguments.</u></b>
    /// <br></br><br></br>
    /// <example>
    /// <u>An example text input would be: </u><br></br>
    /// 4/4: 0.5/C5 0.25/Eb4/T 0.25/Eb4 0.5/G5/U 0.5/C*3/T,U 0.5/C*3 1.5/F5 <br></br>
    /// 6/8: 1 1/C#4 1/T,U 3/R
    /// </example>
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    public static Measure[] ReadTextInput(string text) {

        text = text.Trim();
        string[] totalInput = text.Split("\n");
        Measure[] measures = new Measure[totalInput.Length];
        
        //Loop through each measure that was input.
        for(int m = 0; m < totalInput.Length; m++) {

            string inputedMeasure = totalInput[m];

            //Find the time signature and beat grouping. The measure intro is the "4/4:" or the "6/8:" or the "5/8 (2 + 3):"
            int colonIndex = inputedMeasure.IndexOf(':');
            string measureIntroStr = inputedMeasure.Substring(0, colonIndex);
            measureIntroStr.Trim().Replace(" ", "");
            string[] splitMeasureIntro = measureIntroStr.Split("("); //Splits the measure intro into the time signature part and the beat grouping part
            string[] timeSignatureData = splitMeasureIntro[0].Split("/"); //Split the time signature into the numerator and the denominator
            int[] timeSignature = new int[] { int.Parse(timeSignatureData[0]), int.Parse(timeSignatureData[1]) };
            int[] beatGrouping = null;
            if(splitMeasureIntro.Length == 2) {

                string beatGroupingStr = splitMeasureIntro[1].Substring(0, splitMeasureIntro[1].Length - 1);
                string[] beatGroupingData = beatGroupingStr.Split("+");
                beatGrouping = new int[beatGroupingData.Length];
                for(int i = 0; i < beatGroupingData.Length; i++) {

                    beatGrouping[i] = int.Parse(beatGroupingData[i]);

                }

            }

            //Decipher the measure contents.
            List<Element> musicalElements = new List<Element>();
            string[] measureContents = inputedMeasure.Substring(colonIndex + 2).Trim().Split(" "); //Split the measure contents into elements/tuplets
            for(int i = 0; i < measureContents.Length; i++) {

                //Check if this is an element or a tuplet.
                if(!measureContents[i].Contains("=")) {

                    musicalElements.Add(ReadElementTextInput(measureContents[i]));

                } else {

                    string[] tupletData = measureContents[i].Split("=");
                    int numDivisions = int.Parse(tupletData[0]);
                    string[] tupletElementData = tupletData[1].Substring(1, tupletData[1].Length - 2).Split("-");
                    Element[] tupletElements = new Element[tupletElementData.Length];
                    for(int t = 0; t < tupletElementData.Length; t++) {

                        tupletElements[t] = ReadElementTextInput(tupletElementData[t]);

                    }

                    musicalElements.Add(new Tuplet(numDivisions, tupletElements, timeSignature));

                }

            }

            measures[m] = new Measure(musicalElements, timeSignature[0], timeSignature[1], beatGrouping);

        }

        return measures;

    }

    private static Element ReadElementTextInput(string str) {

        //Find the duration, pitch, and extra arguments.
        string[] musicalElementData = str.Split("/");
        string durationData = musicalElementData[0];
        string pitchData = null;
        string extraArgumentsData = null;
        if(musicalElementData.Length >= 2) {

            if(int.TryParse(musicalElementData[1][^1] + "", out _) || musicalElementData[1].ToUpper().Equals("R")) {

                pitchData = musicalElementData[1];

            } else {

                extraArgumentsData = musicalElementData[1];

            }

        }
        if(musicalElementData.Length == 3) {

            if(pitchData == null) {

                pitchData = musicalElementData[2];

            } else {

                extraArgumentsData = musicalElementData[2];

            }

        }

        //Set the variables.
        float duration = float.Parse(durationData);
        char pitch = 'C';
        float accidental = 0;
        int octave = 4;
        if(pitchData != null) {

            pitch = pitchData[0];
            if(pitch == 'R') {

                return new Rest(duration);

            }

            if(!int.TryParse("" + pitchData[1], out octave)) {

                //The note has an accidental if the second character can't be parsed as an integer.
                accidental = Note.GetAccidentalAsFloat("" + pitchData[1]);
                octave = int.Parse("" + pitchData[2]);

            }

        }

        //Accept any extra arguments for this note.
        string[] extraArguments = (extraArgumentsData != null) ? extraArgumentsData.Split(",") : new string[0];
        bool isTied = false;
        bool forceUnbeam = false;
        for(int a = 0; a < extraArguments.Length; a++) {

            switch(extraArguments[a]) {

                case "T":
                    isTied = true;
                    break;
                case "U":
                    forceUnbeam = true;
                    break;

            }

        }

        return new Note(duration, pitch, accidental, octave, isTied, forceUnbeam);

    }

    /// <summary>
    /// 
    /// Please view this with proper HTML formatting. <br></br>
    /// 
    /// 
    /// <u><b>Text files must be formatted in the following way:</b></u><br></br>
    /// 
    /// - Measures are separated by a blank new line <br></br>
    /// - The first line of a measure contains its time signature <br></br>
    /// - All other lines contain its elements <br></br>
    /// - Rests are formatted like: "R [beats]" <br></br>
    /// - Notes are formatted like: "[tone][opt. accidental][octave] [beats] [extra arg 1] [extra arg 2] [extra arg 3] ..." <br></br>
    /// - Accidentals are typed according to the following: <br></br>
    ///    - * = double sharp <br></br>
    ///    - # = sharp <br></br>
    ///    - b = flat <br></br>
    ///    - d = double flat <br></br>
    /// - Extra args for notes: <br></br>
    ///    - T = tie <br></br>
    ///    - U = force unbeam <br></br>
    /// <br></br>
    /// <u>An example text file would be: </u><br></br>
    /// <i>[BEGINNING OF FILE]</i> <br></br>
    /// 4/4 <br></br>
    /// C5 0.5 <br></br>
    /// Eb4 0.25 T <br></br>
    /// Eb4 0.25 <br></br>
    /// G5 0.5 U <br></br>
    /// C*3 0.5 T U <br></br>
    /// C*3 0.5 <br></br>
    /// F5 1.5 <br></br>
    /// <br></br>
    /// 6/8 <br></br>
    /// C4 1 <br></br>
    /// C4 1 <br></br>
    /// C4 1 <br></br>
    /// C4 1 <br></br>
    /// C4 1 <br></br>
    /// C4 1 <br></br>
    /// <i>[END OF FILE]</i>
    /// 
    /// 
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    /// <exception cref="FormatException"></exception>
    [Obsolete]
    public static List<Measure> LegacyReadTextInput(string text) {

        ReadTextInput(text);

        text = text.ToUpper().Trim();

        List<Measure> measures = new List<Measure>();

        string[] strMeasures = text.Split("\n\n");
        UnityEngine.Debug.Log(strMeasures.Length);

        //Loop through each measure block
        foreach(string measureStr in strMeasures) {

            string[] currMeasureContentsArray = measureStr.Split("\n");

            //The first line in a measure block is the measure's time signature
            int[] timeSignature = new int[] { int.Parse("" + currMeasureContentsArray[0][0]), int.Parse("" + currMeasureContentsArray[0][2]) };
            
            //All other lines in a measure block represent an element
            List<Element> elements = new List<Element>();
            for(int elementTextCount = 1; elementTextCount < currMeasureContentsArray.Length; elementTextCount++) {

                string[] currElementArgs = currMeasureContentsArray[elementTextCount].Split(" ");
                string arg1 = currElementArgs[0];
                float beats;

                if(arg1 == "R") {

                    //This is a rest 
                    beats = float.Parse(currElementArgs[1]);
                    elements.Add(new Rest(beats));
                    continue;

                }

                char tone;
                float accidental;
                int octave;

                int beatArgumentIndex;

                if(float.TryParse(arg1, out beats)) {

                    //The user did not give a pitch, so assume the pitch is C4
                    tone = 'C';
                    accidental = 0;
                    octave = 4;

                    beats = float.Parse(currElementArgs[0]);

                    beatArgumentIndex = 0;

                } else {

                    //User gave a pitch
                    tone = arg1[0];

                    if(arg1.Length == 2) {

                        accidental = 0;
                        octave = int.Parse("" + arg1[1]);

                    } else if(arg1.Length == 3) {

                        accidental = Note.GetAccidentalAsFloat("" + arg1[1]);
                        octave = int.Parse("" + arg1[2]);

                    } else {

                        throw new FormatException($"Invalid pitch: {arg1}");

                    }

                    beats = float.Parse(currElementArgs[1]);

                    beatArgumentIndex = 1;

                }

                bool isTied = false;
                bool forceUnbeam = false;
                for(int i = beatArgumentIndex + 1; i < currElementArgs.Length; i++) {

                    switch(currElementArgs[i]) {

                        case "T":
                            isTied = true;
                            break;
                        case "U":
                            forceUnbeam = true;
                            break;

                    }

                }

                elements.Add(new Note(beats, tone, accidental, octave, isTied, forceUnbeam));

            }

            measures.Add(new Measure(elements, timeSignature[0], timeSignature[1]));

        }

        return measures;

    }

}
