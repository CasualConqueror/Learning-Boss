using System.Collections.Generic;
using UnityEngine;

public class BossDebugVisualizer : MonoBehaviour
{
    [Header("Settings")]
    public bool showDebugInfo = true;
    public bool showPersonalityStats = true;
    public bool showRLMetrics = true;
    public bool showPreferredDistanceCircle = true;
    public bool showAttackRanges = true;

    [Header("UI Settings")]
    public float displayWidth = 200f;
    public float displayOpacity = 0.8f;
    public Color backgroundColor = new Color(0, 0, 0, 0.8f);
    public Color textColor = Color.white;

    [Header("References")]
    public BossPersonalitySystem personalitySystem;
    public BossStateMachine stateMachine;
    public BossPerformanceTracker performanceTracker;

    private GUIStyle headerStyle;
    private GUIStyle labelStyle;
    private GUIStyle boxStyle;
    private GUIStyle buttonStyle;
    private bool stylesInitialized = false;

    private void OnEnable()
    {
        if (personalitySystem == null)
            personalitySystem = GetComponent<BossPersonalitySystem>();

        if (stateMachine == null)
            stateMachine = GetComponent<BossStateMachine>();

        if (performanceTracker == null)
            performanceTracker = GetComponent<BossPerformanceTracker>();
    }

    private void InitializeStyles()
    {
        // Header style
        headerStyle = new GUIStyle();
        headerStyle.fontSize = 14;
        headerStyle.fontStyle = FontStyle.Bold;
        headerStyle.normal.textColor = textColor;
        headerStyle.margin = new RectOffset(4, 4, 4, 4);

        // Label style
        labelStyle = new GUIStyle();
        labelStyle.fontSize = 12;
        labelStyle.normal.textColor = textColor;
        labelStyle.margin = new RectOffset(4, 4, 2, 2);

        // Box style (black background)
        boxStyle = new GUIStyle(GUI.skin.box);
        boxStyle.normal.background = MakeTexture(2, 2, backgroundColor);
        boxStyle.padding = new RectOffset(10, 10, 10, 10);

        // Button style
        buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 12;

        stylesInitialized = true;
    }

    private Texture2D MakeTexture(int width, int height, Color color)
    {
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = color;
        }

        Texture2D texture = new Texture2D(width, height);
        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }

    private void OnGUI()
    {
        if (!showDebugInfo) return;

        // Initialize styles on first OnGUI call
        if (!stylesInitialized)
        {
            InitializeStyles();
        }

        // Begin the main area with the black background
        GUILayout.BeginArea(new Rect(10, 10, displayWidth, Screen.height / 2f));
        GUILayout.BeginVertical(boxStyle);

        GUILayout.Label("Boss Debug Info", headerStyle);

        if (personalitySystem != null)
        {
            GUILayout.Label($"Personality: {personalitySystem.currentPersonalityName}", labelStyle);

            if (showPersonalityStats)
            {
                GUILayout.Label("Probabilities:", labelStyle);
                foreach (var personality in personalitySystem.personalities)
                {
                    string activeMarker = (personality == personalitySystem.currentPersonality) ? " ▶" : "";
                    GUILayout.Label($"• {personality.name}: {personality.selectionProbability:P1}{activeMarker}", labelStyle);
                }
            }

            if (showRLMetrics && personalitySystem.currentPersonality != null)
            {
                GUILayout.Label("Performance:", labelStyle);
                GUILayout.Label($"DMG Dealt: {personalitySystem.damageDealt:F1}", labelStyle);
                GUILayout.Label($"DMG Taken: {personalitySystem.damageTaken:F1}", labelStyle);
                GUILayout.Label($"Ratio: {personalitySystem.performanceRatio:F2}", labelStyle);
            }
        }

        if (stateMachine != null)
        {
            GUILayout.Label($"State: {stateMachine.currentStateType}", labelStyle);

            if (stateMachine.CurrentPersonality != null)
            {
                GUILayout.Label("Parameters:", labelStyle);
                GUILayout.Label($"Speed: {stateMachine.currentMovementSpeed:F1}", labelStyle);
                GUILayout.Label($"Distance: {stateMachine.currentPreferredDistance:F1}", labelStyle);
                GUILayout.Label($"Aggro: {stateMachine.aggressionFactor:F2}", labelStyle);
                GUILayout.Label($"Heavy %: {stateMachine.CurrentPersonality.heavyAttackProbability:F2}", labelStyle);
            }

            GUILayout.Label($"Light CD: {stateMachine.lightAttackTimer:F1}s", labelStyle);
            GUILayout.Label($"Heavy CD: {stateMachine.heavyAttackTimer:F1}s", labelStyle);
        }

        if (showDebugInfo)
        {
            if (GUILayout.Button("Clear All Data", buttonStyle))
            {
                RLDataPersistence persistence = GetComponent<RLDataPersistence>();
                if (persistence != null)
                {
                    persistence.DeleteSavedData();
                }

                if (performanceTracker != null)
                {
                    performanceTracker.ClearLogs();
                }
            }
        }

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    private void OnDrawGizmos()
    {
        if (!showDebugInfo) return;

        if (stateMachine != null && stateMachine.player != null)
        {
            // Draw preferred distance circle
            if (showPreferredDistanceCircle)
            {
                Gizmos.color = new Color(0, 1, 0, 0.2f);
                DrawCircle(stateMachine.player.position, stateMachine.currentPreferredDistance, 32);
            }

            // Draw attack ranges
            if (showAttackRanges)
            {
                // Light attack range
                Gizmos.color = new Color(1, 0.5f, 0, 0.2f);
                DrawCircle(transform.position, stateMachine.lightAttackRange, 24);

                // Heavy attack range
                Gizmos.color = new Color(1, 0, 0, 0.2f);
                DrawCircle(transform.position, stateMachine.heavyAttackRange, 24);
            }
        }
    }

    private void DrawCircle(Vector3 center, float radius, int segments)
    {
        // Adjustment to keep circle on the ground
        center.y = 0.1f;

        float angleStep = 360f / segments;

        for (int i = 0; i < segments; i++)
        {
            float angle1 = i * angleStep * Mathf.Deg2Rad;
            float angle2 = (i + 1) * angleStep * Mathf.Deg2Rad;

            Vector3 point1 = center + new Vector3(Mathf.Cos(angle1) * radius, 0, Mathf.Sin(angle1) * radius);
            Vector3 point2 = center + new Vector3(Mathf.Cos(angle2) * radius, 0, Mathf.Sin(angle2) * radius);

            Gizmos.DrawLine(point1, point2);
        }
    }
}