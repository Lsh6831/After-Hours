using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 플레이어 화면에 현재 미션 목표와 완료 안내를 표시합니다.
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("목표 UI")]
    [SerializeField] private GameObject objectivePanel;
    [SerializeField] private Text objectiveTitleText;
    [SerializeField] private Text objectiveDescriptionText;
    [SerializeField] private Text objectiveStatusText;

    [Header("표시 설정")]
    [SerializeField] private string completedStatusText = "완료";
    [SerializeField] private string activeStatusText = "현재 목표";

    public void ShowObjective(string title, string description)
    {
        if (objectivePanel != null)
        {
            objectivePanel.SetActive(true);
        }

        if (objectiveTitleText != null)
        {
            objectiveTitleText.text = title;
        }

        if (objectiveDescriptionText != null)
        {
            objectiveDescriptionText.text = description;
        }

        if (objectiveStatusText != null)
        {
            objectiveStatusText.text = activeStatusText;
        }
    }

    public void ShowCompleted(string message)
    {
        if (objectiveStatusText != null)
        {
            objectiveStatusText.text = completedStatusText;
        }

        if (objectiveDescriptionText != null && !string.IsNullOrWhiteSpace(message))
        {
            objectiveDescriptionText.text = message;
        }
    }

    public void HideObjective()
    {
        if (objectivePanel != null)
        {
            objectivePanel.SetActive(false);
        }
    }
}
