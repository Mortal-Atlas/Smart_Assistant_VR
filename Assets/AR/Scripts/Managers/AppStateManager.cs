using UnityEngine;

public class AppStateManager : MonoBehaviour
{
    [Header("App Panels (Assign your UI GameObjects here)")]
    public GameObject standbyPanel;
    public GameObject spotifyPanel;
    public GameObject scannerPanel;
    public GameObject combatPanel;
    public GameObject tomodatchiPanel;

    [Header("Future App Panels")]
    public GameObject forwardRightPanel;
    public GameObject forwardLeftPanel;
    public GameObject backLeftPanel;
    public GameObject backRightPanel;

    /// <summary>
    /// Disables all panels, then enables the requested one.
    /// </summary>
    public void SwitchApp(string appName)
    {
        DisableAllPanels();

        switch (appName)
        {
            case "Standby":
                if (standbyPanel) standbyPanel.SetActive(true);
                break;
            case "Spotify":
                if (spotifyPanel) spotifyPanel.SetActive(true);
                break;
            case "Scanner":
                if (scannerPanel) scannerPanel.SetActive(true);
                break;
            case "Combat":
                if (combatPanel) combatPanel.SetActive(true);
                break;
            case "Tomodatchi":
                if (tomodatchiPanel) tomodatchiPanel.SetActive(true);
                break;
            case "App_ForwardRight":
                if (forwardRightPanel) forwardRightPanel.SetActive(true);
                break;
            case "App_ForwardLeft":
                if (forwardLeftPanel) forwardLeftPanel.SetActive(true);
                break;
            case "App_BackLeft":
                if (backLeftPanel) backLeftPanel.SetActive(true);
                break;
            case "App_BackRight":
                if (backRightPanel) backRightPanel.SetActive(true);
                break;
            default:
                Debug.LogWarning("Unknown app state received: " + appName);
                if (standbyPanel) standbyPanel.SetActive(true); // Default fallback
                break;
        }
    }

    private void DisableAllPanels()
    {
        if (standbyPanel) standbyPanel.SetActive(false);
        if (spotifyPanel) spotifyPanel.SetActive(false);
        if (scannerPanel) scannerPanel.SetActive(false);
        if (combatPanel) combatPanel.SetActive(false);
        if (tomodatchiPanel) tomodatchiPanel.SetActive(false);
        if (forwardRightPanel) forwardRightPanel.SetActive(false);
        if (forwardLeftPanel) forwardLeftPanel.SetActive(false);
        if (backLeftPanel) backLeftPanel.SetActive(false);
        if (backRightPanel) backRightPanel.SetActive(false);
    }
}