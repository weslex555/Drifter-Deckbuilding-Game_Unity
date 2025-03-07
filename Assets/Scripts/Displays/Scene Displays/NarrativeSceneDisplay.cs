using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NarrativeSceneDisplay : MonoBehaviour
{
    [SerializeField] private GameObject background, narrativeText,
        clipCounter, continueButton;

    private Narrative narrative;
    private int currentClip;
    private TextMeshProUGUI clipCounterText;

    public Narrative CurrentNarrative
    {
        get => narrative;
        set
        {
            narrative = value;
            currentClip = 0;
            DisplayCurrentClip();
        }
    }

    private void Awake()
    {
        clipCounterText = clipCounter.GetComponent<TextMeshProUGUI>();
        continueButton.SetActive(false);
    }

    private void DisplayCurrentClip()
    {
        clipCounterText.SetText(currentClip + 1 + "/" +
            narrative.NarrativeText.Length);
        background.GetComponent<Image>().sprite = CurrentNarrative.NarrativeBackground;
        Managers.D_MAN.TimedText(narrative.NarrativeText[currentClip],
            narrativeText.GetComponent<TextMeshProUGUI>());
    }

    public void NextButton_OnClick()
    {
        if (SceneLoader.SceneIsLoading) return;
        GetComponent<SoundPlayer>().PlaySound(0);

        if (Managers.D_MAN.CurrentTextRoutine != null)
        {
            Managers.D_MAN.StopTimedText(true);
            return;
        }
        int lastClip = narrative.NarrativeText.Length - 1;
        if (++currentClip < lastClip)
            DisplayCurrentClip();
        else if (currentClip == lastClip)
        {
            DisplayCurrentClip();
            continueButton.SetActive(true);
        }
        else currentClip--;
    }
    public void PreviousButton_OnClick()
    {
        if (SceneLoader.SceneIsLoading) return;
        GetComponent<SoundPlayer>().PlaySound(0);

        if (--currentClip < 0) currentClip = 0;
        else DisplayCurrentClip();
        continueButton.SetActive(false);
    }
    public void ContinueButton_OnClick()
    {
        if (SceneLoader.SceneIsLoading) return;
        Managers.G_MAN.EndNarrativeScene();
    }
}
