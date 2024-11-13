using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HoldAndReleaseButton : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler {

    [SerializeField] private RectTransform buttonTransform;
    [SerializeField] private RectTransform shadowTransform;
    [SerializeField] private Color pressedColor;
    [SerializeField] private UnityEvent buttonAction;

    private Vector2 originalSize;
    private Vector3 originalPosition;
    private Color originalColor;
    private bool buttonIsEnabled = true;

    void Awake() {

        originalSize = new Vector2(buttonTransform.rect.width, buttonTransform.rect.height);
        originalPosition = buttonTransform.localPosition;
        originalColor = buttonTransform.GetComponent<Image>().color;

    }

    public void OnPointerClick(PointerEventData data) {

        if(buttonIsEnabled) {

            buttonAction.Invoke();

        }

    }

    public void OnPointerDown(PointerEventData data) {

        if(buttonIsEnabled) {

            float sizeChangeFactor = shadowTransform.rect.width / buttonTransform.rect.width;
            buttonTransform.localScale = new Vector3(sizeChangeFactor, sizeChangeFactor, 1);
            buttonTransform.localPosition = shadowTransform.localPosition + Vector3.down * (shadowTransform.rect.height / 2f - buttonTransform.rect.height / 2f * sizeChangeFactor);
            Image image = buttonTransform.GetComponent<Image>();
            Color.RGBToHSV(image.color, out float h, out float s, out float v);
            originalColor = image.color;
            image.color = pressedColor;

        }

    }

    public void OnPointerUp(PointerEventData data) {

        if(buttonIsEnabled) {

            //buttonTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, originalSize.x);
            //buttonTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, originalSize.y);
            buttonTransform.localScale = Vector3.one;
            buttonTransform.localPosition = originalPosition;
            buttonTransform.GetComponent<Image>().color = originalColor;

        }

    }

    /// <summary>
    /// Button can no longer be pressed, but still has a visible shadow (hence it is as if the button is "locked")
    /// </summary>
    public void LockButton() {

        buttonIsEnabled = false;

    }

    /// <summary>
    /// Button can no longer be pressed, and the shadow is hidden
    /// </summary>
    public void DisableButton() {

        shadowTransform.gameObject.SetActive(false);
        buttonIsEnabled = false;

    }

    /// <summary>
    /// Allows the button to be pressed, and unhides the shadow
    /// </summary>
    public void EnableButton() {

        shadowTransform.gameObject.SetActive(true);
        buttonIsEnabled = true;

    }

    public Transform GetButtonTransform() {

        return buttonTransform;

    }

    public Transform GetShadowTransform() {

        return shadowTransform;

    }

    public Color GetColor() {

        return buttonTransform.GetComponent<Image>().color;

    }

    public Color GetShadowColor() {

        return shadowTransform.GetComponent<Image>().color;

    }

    public Color GetPressedColor() {

        return pressedColor;

    }

    public void SetColor(Color color, float shadowDesaturation = 0.2f) {

        Color.RGBToHSV(color, out float hue, out float sat, out float val);

        buttonTransform.GetComponent<Image>().color = color;
        shadowTransform.GetComponent<Image>().color = Color.HSVToRGB(hue, sat - shadowDesaturation, val);
        pressedColor = Color.HSVToRGB(hue, sat, val - 0.3f);

    }

    public void SetColor(Color normalColor, Color shadowColor, Color pressedColor) {

        buttonTransform.GetComponent<Image>().color = normalColor;
        shadowTransform.GetComponent<Image>().color = shadowColor;
        this.pressedColor = pressedColor;

    }

    public void ShrinkButton() {

        float sizeChangeFactor = shadowTransform.rect.width / buttonTransform.rect.width;
        buttonTransform.localScale = new Vector3(sizeChangeFactor, sizeChangeFactor, 1);
        buttonTransform.localPosition = shadowTransform.localPosition + Vector3.down * (shadowTransform.rect.height / 2f - buttonTransform.rect.height / 2f * sizeChangeFactor);

    }

    public void ExpandButton() {

        buttonTransform.localScale = Vector3.one;
        buttonTransform.localPosition = originalPosition;

    }

}
