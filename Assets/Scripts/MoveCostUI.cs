using TMPro;
using UnityEngine;

public class MoveCostUI : MonoBehaviour
{
    public TMP_Text costText;
    public ClickManager clickManager;

    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
        HideCost();
    }

    void Update()
    {
        if (clickManager == null || costText == null)
            return;

        if (clickManager.actionMode != ClickManager.ActionMode.Move)
        {
            HideCost();
            return;
        }

        costText.transform.rotation = mainCamera.transform.rotation;
    }

    public void ShowCost(int cost, int remainingPM, Vector3 worldPosition)
    {
        if (clickManager.actionMode != ClickManager.ActionMode.Move)
            return;

        costText.gameObject.SetActive(true);

        costText.text = "-" + cost + " PM";

        costText.transform.position = worldPosition + new Vector3(0, 0.55f, 0);
    }

    public void HideCost()
    {
        if (costText != null)
            costText.gameObject.SetActive(false);
    }
}