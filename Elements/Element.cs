
/// <summary>
/// The superclass of all elements for music notation. <br></br>
/// Also contains static methods for elements.
/// </summary>
public abstract class Element {

    protected float beats;

    public float GetBeats() {

        return beats;

    }

    /// <summary>
    /// Returns the amount of beats this element holds without the dot
    /// </summary>
    /// <returns> If the element has a value that can only happen with a dot, the duration this element has without the dot. </returns>
    public float GetBaseAmountOfBeats() {

        //This whole function just says "if beats divided by 1.5 results in a power of 2, then this note is dotted."
        //This is because dotted notes are just the base amount of beats multiplied by 1.5

        float quotient = beats / 1.5f;

        if(UnityEngine.Mathf.Log(quotient, 2) % 1f == 0) {

            return quotient;

        } else {

            return beats;

        }

    }

    /// <summary>
    /// Calculates the duration of an element in seconds.
    /// </summary>
    /// <param name="element"></param>
    /// <param name="timeSignature"></param>
    /// <param name="tempo"></param>
    /// <returns>The duration of an element in seconds</returns>
    public float GetElementDuration(float tempo) {

        if(tempo <= 0) {

            UnityEngine.Debug.LogWarning($"Element: a tempo of {tempo} was given (it is less than or equal to 0).");

        }

        return beats / tempo * 60;

    }

    /// <summary>
    /// Converts an amount of beats to a value for a given time signature. For example, one beat in 4/4 has a value of 0.25 (a quarter note/rest)
    /// </summary>
    /// <param name="beats"></param>
    /// <param name="timeSignatureBottom"></param>
    /// <returns>The value of the given amount of beats in the given time signature</returns>
    public static float ConvertBeatsToValue(float beats, int timeSignatureBottom) {

        return beats / timeSignatureBottom;

    }

}
