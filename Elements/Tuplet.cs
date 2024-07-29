﻿
/// <summary>
/// An immutable class that represents a tuplet (triplets, duplets, quintuplets, etc.) <br></br>
/// </summary>
public class Tuplet : Element {

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
            tupletToRealBeatsScale = 3 / numDivisions;

        } else {

            tupletToRealBeatsScale = UnityEngine.Mathf.Pow(2, (int) UnityEngine.Mathf.Log(numDivisions, 2)) / numDivisions;

        }

        beats = elementsBeatSum * tupletToRealBeatsScale;

    }

    public float GetBeatsForElement(int elementIndex) {

        return elements[elementIndex].GetBeats() * tupletToRealBeatsScale;

    }

    public int GetElementCount() {

        return elements.Length;

    }

    public float GetNumDivisions() {

        return numDivisions;

    }

}
