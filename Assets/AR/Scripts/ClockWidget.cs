using UnityEngine;
using TMPro;
using System;

public class ClockWidget : MonoBehaviour
{
    [SerializeField] private TextMeshPro timeDisplayText; // HH:MM AM/PM
    [SerializeField] private TextMeshPro secondsDisplayText; // SS

    void Update()
    {
        int hour = DateTime.Now.Hour;
        int minute = DateTime.Now.Minute;
        int second = DateTime.Now.Second;
        
        // 1. Determine AM or PM
        string amPm = (hour >= 12) ? "PM" : "AM";

        // 2. Adjust for 12-hour format
        int displayHour = hour;
        if (hour > 12)
        {
            displayHour = hour - 12;
        }
        else if (hour == 0)
        {
            displayHour = 12; // Midnight is 12 AM
        }

        // 3. Update the text
        // We use "D2" to ensure single digits have a leading zero (e.g., 01 instead of 1)
        timeDisplayText.text = $"{displayHour:D2}:{minute:D2} {amPm}";
        secondsDisplayText.text = second.ToString("D2");
    }
}