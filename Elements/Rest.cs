
/// <summary>An immutable class that represents a rest</summary>
public class Rest : Element {
    
    public Rest(float beats) {

        this.beats = beats;

    }

    public override bool Equals(object obj) {
        
        if(obj == null) {

            return false;

        }

        if(obj is not Rest) {

            return false;

        }

        Rest restObj = (Rest) obj;
        if(beats == restObj.beats) {

            return true;

        } else {

            return false;

        }

    }

    public override int GetHashCode() {

        return beats.GetHashCode();

    }

    public override string ToString() {

        return $"Rest {beats}";

    }

}
