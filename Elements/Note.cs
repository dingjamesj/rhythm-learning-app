

using System;
/// <summary>
/// An immutable class that represents a musical note.
/// </summary>
public class Note : Element {

    /// <summary> The note's pitch (letter) </summary>
    private readonly char pitch;
    /// <summary> The note's accidental (-1 = double flat, -0.5 = flat, 0 = natural, 0.5 = sharp, 1 = double sharp) </summary>
    private readonly float accidental;
    /// <summary> The note's octave (middle C is at octave 4) </summary>
    private readonly int octave;
    /// <summary> True if this note is the first note of a tie </summary>
    private readonly bool isTied;
    /// <summary> True if this note is forced to NOT beam with the next note </summary>
    private readonly bool forceUnbeam;

    public Note(float beats = 1f, char pitch = 'C', float accidental = 0, int octave = 4, bool isTied = false, bool forceUnbeam = false) {

        this.beats = beats;
        this.pitch = pitch;
        this.accidental = accidental;
        this.octave = octave;
        this.isTied = isTied;
        this.forceUnbeam = forceUnbeam;

    }

    public char GetPitch() { return pitch; }

    public float GetAccidental() { return accidental; }

    public int GetOctave() { return octave; }

    public bool IsTied() { return isTied; }

    public bool IsForceUnbeam() {  return forceUnbeam; }

    public static string GetAccidentalAsString(float accidental) {

        return (float) accidental switch {

            1 => "*",
            0.5f => "♯",
            0 => "",
            -0.5f => "♭",
            -1 => "𝄫",
            _ => $"{accidental}",

        };

    }

    public static float GetAccidentalAsFloat(string accidental) {

        return accidental switch {

            "𝄪" => 1,
            "*" => 1,
            "♯" => -0.5f,
            "#" => -0.5f,
            "♮" => 0,
            "" => 0,
            "♭" => -0.5f,
            "b" => -0.5f,
            "𝄫" => -1,
            "bb" => -1,
            "d" => -1,
            _ => throw new ArgumentException(),

        };

    }

    /// <summary>
    /// Determines if two elements can be in a tied pair
    /// </summary>
    /// <param name="firstElement"></param>
    /// <param name="secondElement"></param>
    /// <returns>True if the elements are 1. Notes; and 2. Are of the same pitch</returns>
    public static bool IsTie(Element firstElement, Element secondElement) {

        if(firstElement is Rest || secondElement is Rest) {

            return false;

        } else if (firstElement is Tuplet firstTuplet && firstTuplet[^1] is not Note) {

            return false;

        } else if(secondElement is Tuplet secondTuplet && secondTuplet[0] is not Note) {

            return false;

        }

        Note firstNote = (Note) (firstElement is Tuplet ? (firstElement as Tuplet)[^1] : firstElement);
        Note secondNote = (Note) (secondElement is Tuplet ? (secondElement as Tuplet)[0] : secondElement);

        if(firstNote.IsTied() && firstNote.Equals(secondNote) && !secondNote.IsTied()) {

            return true;

        } else {

            return false;

        }

    }

    public override bool Equals(object obj) {

        if(obj == null) {

            return false;

        }

        if(obj is not Note) {

            return false;

        }

        Note noteObj = (Note) obj;
        if(pitch == noteObj.pitch && accidental == noteObj.accidental && octave == noteObj.octave) {

            return true;
        
        } else {

            return false;

        }

    }

    public override int GetHashCode() {

        return 17*(pitch.GetHashCode() + accidental.GetHashCode() + octave.GetHashCode());

    }

    public override string ToString() {

        return $"{pitch}{GetAccidentalAsString(accidental)}{octave}";

    }

}
