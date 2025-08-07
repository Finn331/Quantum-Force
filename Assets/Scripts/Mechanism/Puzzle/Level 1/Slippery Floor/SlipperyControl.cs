using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SliiperyControl : MonoBehaviour
{
    public TextMeshProUGUI valueText;
    public Button increaseButton;
    public Button decreaseButton;

    private int currentValue = 0;

    void Start()
    {
        UpdateText();

        increaseButton.onClick.AddListener(IncreaseValue);
        decreaseButton.onClick.AddListener(DecreaseValue);
    }

    void IncreaseValue()
    {
        currentValue++;
        UpdateText();
    }

    void DecreaseValue()
    {
        currentValue--;
        UpdateText();
    }

    void UpdateText()
    {
        valueText.text = currentValue.ToString();
    }
}
