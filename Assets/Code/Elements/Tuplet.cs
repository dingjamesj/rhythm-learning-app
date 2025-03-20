using System;

/// <summary>
/// An immutable class that represents a tuplet (triplets, duplets, quintuplets, etc.) <br></br>
/// </summary>
public class Tuplet : Element, IElementGroup {

    private readonly Element[] elements;
    private readonly int numDivisions;
    private readonly float tupletToRealBeatsScale;

    public Tuplet(int numDivisions, Element[] elements, int[] timeSignature) {

        this.numDivisions = numDivisions;

        float elementsBeatSum = 0;
        this.elements = new Element[elements.Length];
        for(int i = 0; i < elements.Length; i++) {

            this.elements[i] = elements[i];
            elementsBeatSum += elements[i].GetBeats();

        }

        //https://en.wikipedia.org/wiki/Tuplet
        if(timeSignature[1] == 8 && timeSignature[0] % 3 == 0 && numDivisions % 2 == 0) {

            //In the case of a even-numbered tuplet in a compound time signature:
            tupletToRealBeatsScale = 3f / numDivisions;

        } else {

            tupletToRealBeatsScale = UnityEngine.Mathf.Pow(2, (int) UnityEngine.Mathf.Log(numDivisions, 2)) / numDivisions;

        }

        beats = elementsBeatSum * tupletToRealBeatsScale;

    }

    public Element this[int i] {

        get {

            if(i < 0 || i >= elements.Length) {

                throw new ArgumentException($"Tuplet: Index {i} is out of range for measure of length {Count}.");

            } else {

                return elements[i];

            }


        }

    }

    public int Count { get { return elements.Length; } }

    public float GetRealBeatsForElement(int elementIndex) {

        return elements[elementIndex].GetBeats() * tupletToRealBeatsScale;

    }

    public float GetRealElementDuration(int elementIndex, float tempo) {

        return elements[elementIndex].GetBeats() * tupletToRealBeatsScale * 60 / tempo;

    }

    public int GetNumDivisions() {

        return numDivisions;

    }

    public override string ToString() {

        return numDivisions switch {

            2 => "Duplet",
            3 => "Triplet",
            4 => "Quadruplet",
            5 => "Quintuplet",
            _ => $"{numDivisions}-tuplet"

        };

    }

}