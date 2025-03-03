using System;
using System.Collections.Generic;
using UnityEngine;

// Simplified PersonalityProfile with basic serialization
[Serializable]
public class PersonalityProfile
{
    // Default values for all fields to prevent serialization issues
    public string name = "Default";
    [SerializeField] private float _aggressionLevel = 0.5f;
    [SerializeField] private float _movementSpeedMultiplier = 1.0f;
    [SerializeField] private float _attackSpeedMultiplier = 1.0f;
    [SerializeField] private float _preferredDistance = 4.0f;
    [SerializeField] private float _heavyAttackProbability = 0.5f;
    [SerializeField] private float _selectionProbability = 0.25f;

    // Properties with validation
    public float aggressionLevel {
        get { return _aggressionLevel; }
        set { _aggressionLevel = Mathf.Clamp01(value); }
    }

    public float movementSpeedMultiplier {
        get { return _movementSpeedMultiplier; }
        set { _movementSpeedMultiplier = Mathf.Clamp(value, 0.5f, 2f); }
    }

    public float attackSpeedMultiplier {
        get { return _attackSpeedMultiplier; }
        set { _attackSpeedMultiplier = Mathf.Clamp(value, 0.5f, 1.5f); }
    }

    public float preferredDistance {
        get { return _preferredDistance; }
        set { _preferredDistance = Mathf.Clamp(value, 1f, 10f); }
    }

    public float heavyAttackProbability {
        get { return _heavyAttackProbability; }
        set { _heavyAttackProbability = Mathf.Clamp01(value); }
    }

    public float selectionProbability {
        get { return _selectionProbability; }
        set { _selectionProbability = Mathf.Clamp(value, 0.05f, 1f); }
    }
}

public class BossPersonalitySystem : MonoBehaviour
{
    [Header("Personality Profiles")]
    [SerializeField]
    private List<PersonalityProfile> _personalities = new List<PersonalityProfile>();
    public List<PersonalityProfile> personalities {
        get { return _personalities; }
    }

    [Header("Personality Selection")]
    [SerializeField] private float _personalityChangeCooldown = 10f;
    public float personalityChangeCooldown {
        get { return _personalityChangeCooldown; }
        set { _personalityChangeCooldown = Mathf.Max(1f, value); }
    }

    [HideInInspector] public float timeSinceLastChange = 0f;

    [Header("Current State")]
    [SerializeField] private PersonalityProfile _currentPersonality;
    public PersonalityProfile currentPersonality {
        get { return _currentPersonality; }
        private set { _currentPersonality = value; }
    }

    public string currentPersonalityName {
        get { return currentPersonality != null ? currentPersonality.name : "None"; }
    }

    [Header("Performance Tracking")]
    [HideInInspector] public float damageDealt = 0f;
    [HideInInspector] public float damageTaken = 0f;
    public float performanceRatio {
        get { return damageTaken > 0 ? damageDealt / damageTaken : damageDealt; }
    }

    [Header("Learning Parameters")]
    [SerializeField, Range(0.01f, 0.5f)] private float _learningRate = 0.1f;
    public float learningRate {
        get { return _learningRate; }
        set { _learningRate = Mathf.Clamp(value, 0.01f, 0.5f); }
    }

    [SerializeField, Range(0.05f, 0.2f)] private float _minProbability = 0.05f;
    public float minProbability {
        get { return _minProbability; }
        set { _minProbability = Mathf.Clamp(value, 0.05f, 0.2f); }
    }

    [Header("Genetic Algorithm Parameters")]
    [SerializeField, Range(0f, 0.5f)] private float _mutationChance = 0.1f;
    [SerializeField, Range(0.01f, 0.5f)] private float _mutationStrength = 0.2f;
    [SerializeField, Range(1, 20)] private int _maxPersonalities = 10;

    public float mutationChance {
        get { return _mutationChance; }
        set { _mutationChance = Mathf.Clamp(value, 0f, 0.5f); }
    }

    public float mutationStrength {
        get { return _mutationStrength; }
        set { _mutationStrength = Mathf.Clamp(value, 0.01f, 0.5f); }
    }

    public int maxPersonalities {
        get { return _maxPersonalities; }
        set { _maxPersonalities = Mathf.Clamp(value, 1, 20); }
    }

    [HideInInspector] public BossStateMachine stateMachine;
    [HideInInspector] public BossPerformanceTracker performanceTracker;
    [HideInInspector] public RLDataPersistence dataPersistence;

    private void Awake()
    {
        // Get required components
        stateMachine = GetComponent<BossStateMachine>();
        performanceTracker = GetComponent<BossPerformanceTracker>();
        dataPersistence = GetComponent<RLDataPersistence>();

        // Initialize personalities list if null
        if (_personalities == null)
        {
            _personalities = new List<PersonalityProfile>();
        }

        // Create default personalities if none exist
        if (_personalities.Count == 0)
        {
            Debug.Log("[Boss] No personalities defined. Creating defaults.");
            CreateDefaultPersonalities();
        }

        // Load saved probabilities (if data persistence component exists)
        if (dataPersistence != null)
        {
            LoadPersonalityProbabilities();
        }
        else
        {
            Debug.LogWarning("[Boss] RLDataPersistence component is missing. Data will not be saved between sessions.");
        }

        // Log available personalities
        LogPersonalityInfo();
    }

    // Safe method to create a mutation
    private PersonalityProfile CreateMutatedPersonality(PersonalityProfile parent)
    {
        // Guard clause
        if (parent == null)
        {
            Debug.LogError("[Boss] Cannot create mutation from null parent!");
            return null;
        }

        // Create a new personality rather than clone to avoid reference issues
        PersonalityProfile mutation = new PersonalityProfile();

        // Set basic properties
        mutation.name = parent.name + "-Mut";
        mutation.aggressionLevel = parent.aggressionLevel;
        mutation.movementSpeedMultiplier = parent.movementSpeedMultiplier;
        mutation.attackSpeedMultiplier = parent.attackSpeedMultiplier;
        mutation.preferredDistance = parent.preferredDistance;
        mutation.heavyAttackProbability = parent.heavyAttackProbability;
        mutation.selectionProbability = parent.selectionProbability;

        // Select one trait to mutate
        int traitToMutate = UnityEngine.Random.Range(0, 5);

        // Apply mutation to the selected trait
        switch (traitToMutate)
        {
            case 0: // Mutate aggressionLevel
                mutation.aggressionLevel = Mathf.Clamp01(
                    mutation.aggressionLevel + UnityEngine.Random.Range(-_mutationStrength, _mutationStrength));
                mutation.name = parent.name + "-Aggro";
                break;

            case 1: // Mutate movementSpeedMultiplier
                mutation.movementSpeedMultiplier = Mathf.Clamp(
                    mutation.movementSpeedMultiplier * (1 + UnityEngine.Random.Range(-_mutationStrength, _mutationStrength)),
                    0.5f, 2f);
                mutation.name = parent.name + "-Speed";
                break;

            case 2: // Mutate attackSpeedMultiplier
                mutation.attackSpeedMultiplier = Mathf.Clamp(
                    mutation.attackSpeedMultiplier * (1 + UnityEngine.Random.Range(-_mutationStrength, _mutationStrength)),
                    0.5f, 1.5f);
                mutation.name = parent.name + "-Attack";
                break;

            case 3: // Mutate preferredDistance
                mutation.preferredDistance = Mathf.Clamp(
                    mutation.preferredDistance * (1 + UnityEngine.Random.Range(-_mutationStrength, _mutationStrength)),
                    1f, 10f);
                mutation.name = parent.name + "-Range";
                break;

            case 4: // Mutate heavyAttackProbability
                mutation.heavyAttackProbability = Mathf.Clamp01(
                    mutation.heavyAttackProbability + UnityEngine.Random.Range(-_mutationStrength, _mutationStrength));
                mutation.name = parent.name + "-Heavy";
                break;
        }

        return mutation;
    }

    // Cleanup underperforming personalities to prevent list bloat
    public void CleanupUnderperformingPersonalities()
    {
        // Don't cleanup if we have fewer than 5 personalities
        if (_personalities.Count <= 5)
            return;

        // Find lowest probability personality that isn't the current one
        float lowestProb = float.MaxValue;
        PersonalityProfile worstPerformer = null;

        foreach (var p in _personalities)
        {
            if (p != _currentPersonality && p != null && p.selectionProbability < lowestProb)
            {
                lowestProb = p.selectionProbability;
                worstPerformer = p;
            }
        }

        // Remove it if probability is very low
        if (worstPerformer != null && worstPerformer.selectionProbability < _minProbability * 1.5f)
        {
            Debug.Log($"[Boss] Removing underperforming personality: {worstPerformer.name} with probability {worstPerformer.selectionProbability:F3}");
            _personalities.Remove(worstPerformer);
            NormalizeProbabilities(null, 0);
        }
    }

    // Try to create a mutation if conditions are right
    private bool TryCreateMutation(PersonalityProfile parent)
    {
        // Guard clauses
        if (_personalities.Count >= _maxPersonalities)
            return false;
        if (parent == null)
            return false;
        if (UnityEngine.Random.value > _mutationChance)
            return false;

        // Create a mutation of the selected personality
        PersonalityProfile mutation = CreateMutatedPersonality(parent);

        // Check mutation was created successfully
        if (mutation == null)
            return false;

        // Add the mutation to the list of personalities
        _personalities.Add(mutation);

        // Split probability between parent and child
        float sharedProbability = parent.selectionProbability / 2f;
        parent.selectionProbability = sharedProbability;
        mutation.selectionProbability = sharedProbability;

        // Log mutation creation
        Debug.Log($"[Boss] Created mutated personality from {parent.name}: {mutation.name} with probability {mutation.selectionProbability:F3}");

        return true;
    }

    private void Start()
    {
        // Make sure we have at least one personality before selecting
        if (_personalities == null || _personalities.Count == 0)
        {
            Debug.LogError("[Boss] No personalities available in Start(). Creating default personalities.");
            CreateDefaultPersonalities();
        }

        // Initialize current personality without updating performance metrics
        if (_personalities.Count > 0 && _currentPersonality == null)
        {
            // For the first selection, just pick the first personality without any RL calculation
            SetActivePersonality(_personalities[0]);
            Debug.Log($"[Boss] Initial personality set to {currentPersonalityName}");
        }
        else
        {
            // Select initial personality using standard method (will update performance if existing personality)
            SelectNewPersonality();
        }
    }

    private void Update()
    {
        timeSinceLastChange += Time.deltaTime;

        // Check if it's time to potentially change personality
        if (timeSinceLastChange >= _personalityChangeCooldown)
        {
            SelectNewPersonality();
            timeSinceLastChange = 0f;
        }
    }

    // Primary method to select a new personality based on probabilities
    public void SelectNewPersonality()
    {
        // Validate personalities list
        if (_personalities == null || _personalities.Count == 0)
        {
            Debug.LogError("[Boss] Cannot select personality: No personalities available!");
            return;
        }

        // Store performance data for current personality before changing
        if (_currentPersonality != null)
        {
            try
            {
                UpdatePersonalityPerformance();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Boss] Error in UpdatePersonalityPerformance: {e.Message}");
            }
            ResetPerformanceMetrics();
        }

        // Periodically clean up underperforming personalities to prevent list from growing too large
        if (UnityEngine.Random.value < 0.3f)
        {
            CleanupUnderperformingPersonalities();
        }

        // Select a new personality based on probabilities
        float random = UnityEngine.Random.value;
        float cumulativeProbability = 0f;
        bool personalitySelected = false;
        PersonalityProfile selectedPersonality = null;

        foreach (PersonalityProfile personality in _personalities)
        {
            if (personality == null) continue;

            cumulativeProbability += personality.selectionProbability;

            if (random <= cumulativeProbability)
            {
                selectedPersonality = personality;
                personalitySelected = true;
                break;
            }
        }

        // If somehow no personality was selected (due to rounding errors), pick the first one
        if (!personalitySelected && _personalities.Count > 0)
        {
            // Find first non-null personality
            foreach (var p in _personalities)
            {
                if (p != null)
                {
                    selectedPersonality = p;
                    break;
                }
            }

            if (selectedPersonality == null)
            {
                Debug.LogError("[Boss] All personalities are null! Creating a default personality.");
                CreateDefaultPersonalities();
                selectedPersonality = _personalities[0];
            }
        }

        // Try to create a mutation from the selected personality
        if (selectedPersonality != null)
        {
            bool mutationCreated = TryCreateMutation(selectedPersonality);

            // Set the active personality (either original or mutated)
            SetActivePersonality(selectedPersonality);

            if (mutationCreated)
            {
                Debug.Log($"[Boss] Changed to {currentPersonalityName} personality and created mutation. Probabilities: {GetProbabilitiesString()}");
            }
            else
            {
                Debug.Log($"[Boss] Changed to {currentPersonalityName} personality. Probabilities: {GetProbabilitiesString()}");
            }
        }
        else
        {
            Debug.LogError("[Boss] Failed to select a personality!");
        }
    }

    // Set a personality as active and update the state machine
    private void SetActivePersonality(PersonalityProfile personality)
    {
        if (personality == null)
        {
            Debug.LogError("[Boss] Cannot set null personality as active!");
            return;
        }

        _currentPersonality = personality;

        // Update state machine with new personality parameters
        if (stateMachine != null)
        {
            stateMachine.UpdatePersonalityParameters(_currentPersonality);
        }
        else
        {
            Debug.LogWarning("[Boss] StateMachine component is missing. Cannot update personality parameters.");
        }
    }

    // Register damage dealt by the boss
    public void RegisterDamageDealt(float amount)
    {
        damageDealt += amount;

        if (performanceTracker != null)
        {
            performanceTracker.LogPerformance(currentPersonalityName, "damage_dealt", amount);
        }
    }

    // Register damage taken by the boss
    public void RegisterDamageTaken(float amount)
    {
        damageTaken += amount;

        if (performanceTracker != null)
        {
            performanceTracker.LogPerformance(currentPersonalityName, "damage_taken", amount);
        }
    }

    // Update the probability of a personality based on performance
    private void UpdatePersonalityPerformance()
    {
        // Validate current personality
        if (_currentPersonality == null)
        {
            Debug.LogError("[Boss] Cannot update performance: currentPersonality is null!");
            return;
        }

        // Prevent division by zero and ensure no change if no combat occurs
        if (damageTaken == 0 && damageDealt == 0)
        {
            return; // Do nothing, keeping probability the same
        }

        float adjustment = 0.0f;

        if (damageTaken == 0)
        {
            adjustment = damageDealt * _learningRate;
        }
        else if (damageDealt == 0)
        {
            adjustment = -1.0f * _learningRate; // Negative adjustment when taking damage but dealing none
        }
        else
        {
            adjustment = (damageDealt / damageTaken - 1.0f) * _learningRate;
        }

        // Boost adjustment for mutated personalities to give them a chance to prove themselves
        bool isMutation = _currentPersonality.name.Contains("-Mut") ||
                         _currentPersonality.name.Contains("-Aggro") ||
                         _currentPersonality.name.Contains("-Speed") ||
                         _currentPersonality.name.Contains("-Attack") ||
                         _currentPersonality.name.Contains("-Range") ||
                         _currentPersonality.name.Contains("-Heavy");

        if (isMutation && adjustment > 0)
        {
            // Give successful mutations a boost to help them establish
            adjustment *= 1.2f;
        }

        // Calculate performance ratio for logging
        float performanceRatio = (damageTaken > 0) ? (damageDealt / damageTaken) : damageDealt;

        // Apply reinforcement learning: increase probability for good performance, decrease for poor performance
        float newProbability = Mathf.Clamp(_currentPersonality.selectionProbability + adjustment, _minProbability, 1.0f);
        float difference = newProbability - _currentPersonality.selectionProbability;
        _currentPersonality.selectionProbability = newProbability;

        try
        {
            // Adjust other personalities to ensure probabilities sum to 1
            NormalizeProbabilities(_currentPersonality, difference);

            // Save updated probabilities if data persistence exists
            if (dataPersistence != null)
            {
                dataPersistence.SavePersonalityData(_personalities);
            }

            Debug.Log($"[Boss] Updated {currentPersonalityName} performance: " +
                    $"Dealt {damageDealt}, Taken {damageTaken}, Ratio {performanceRatio:F2}, " +
                    $"Adjustment {adjustment:F3}, New probability {newProbability:F3}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Boss] Error in normalization or saving: {e.Message}");
        }
    }

    // Normalize probabilities to ensure they sum to 1
    private void NormalizeProbabilities(PersonalityProfile excludedPersonality, float difference)
    {
        // Validate personalities list
        if (_personalities == null || _personalities.Count == 0)
        {
            Debug.LogError("[Boss] Cannot normalize: personalities list is null or empty!");
            return;
        }

        // Don't normalize if there's only one personality
        if (_personalities.Count <= 1) return;

        try
        {
            // If the adjustment was positive, reduce other probabilities
            if (difference > 0 && excludedPersonality != null)
            {
                float totalOtherProbabilities = 0f;
                foreach (var p in _personalities)
                {
                    if (p != null && p != excludedPersonality)
                    {
                        totalOtherProbabilities += p.selectionProbability;
                    }
                }

                // Calculate scaling factor to distribute the difference
                if (totalOtherProbabilities > 0) // Avoid division by zero
                {
                    float scalingFactor = (totalOtherProbabilities - difference) / totalOtherProbabilities;

                    // Apply scaling to other personalities
                    foreach (var p in _personalities)
                    {
                        if (p != null && p != excludedPersonality)
                        {
                            p.selectionProbability = Mathf.Max(p.selectionProbability * scalingFactor, _minProbability);
                        }
                    }
                }
            }

            // Ensure sum of probabilities is exactly 1.0
            float sum = 0f;
            int validPersonalityCount = 0;

            foreach (var p in _personalities)
            {
                if (p != null)
                {
                    sum += p.selectionProbability;
                    validPersonalityCount++;
                }
            }

            // Apply final normalization
            if (sum > 0 && validPersonalityCount > 0)
            {
                float normalizationFactor = 1.0f / sum;
                foreach (var p in _personalities)
                {
                    if (p != null)
                    {
                        p.selectionProbability *= normalizationFactor;
                    }
                }
            }
            else if (validPersonalityCount > 0)
            {
                // If sum is zero (unlikely but possible), distribute evenly
                float equalProbability = 1.0f / validPersonalityCount;
                foreach (var p in _personalities)
                {
                    if (p != null)
                    {
                        p.selectionProbability = equalProbability;
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Boss] Error during probability normalization: {e.Message}");
        }
    }

    // Reset performance metrics after updating personality probabilities
    private void ResetPerformanceMetrics()
    {
        damageDealt = 0f;
        damageTaken = 0f;
    }

    // Create default personalities for the boss
    private void CreateDefaultPersonalities()
    {
        Debug.Log("[Boss] Creating default personalities...");

        // Clear any existing personalities to avoid duplicates
        _personalities.Clear();

        // Create personalities with explicit construction to avoid serialization issues
        PersonalityProfile aggressive = new PersonalityProfile();
        aggressive.name = "Aggressive";
        aggressive.aggressionLevel = 0.9f;
        aggressive.movementSpeedMultiplier = 1.3f;
        aggressive.attackSpeedMultiplier = 1.5f;
        aggressive.preferredDistance = 2f;
        aggressive.heavyAttackProbability = 0.7f;
        aggressive.selectionProbability = 0.2f;
        _personalities.Add(aggressive);

        PersonalityProfile defensive = new PersonalityProfile();
        defensive.name = "Defensive";
        defensive.aggressionLevel = 0.3f;
        defensive.movementSpeedMultiplier = 0.8f;
        defensive.attackSpeedMultiplier = 0.7f;
        defensive.preferredDistance = 7f;
        defensive.heavyAttackProbability = 0.3f;
        defensive.selectionProbability = 0.2f;
        _personalities.Add(defensive);

        PersonalityProfile balanced = new PersonalityProfile();
        balanced.name = "Balanced";
        balanced.aggressionLevel = 0.6f;
        balanced.movementSpeedMultiplier = 1.0f;
        balanced.attackSpeedMultiplier = 1.0f;
        balanced.preferredDistance = 4f;
        balanced.heavyAttackProbability = 0.5f;
        balanced.selectionProbability = 0.2f;
        _personalities.Add(balanced);

        PersonalityProfile berserker = new PersonalityProfile();
        berserker.name = "Berserker";
        berserker.aggressionLevel = 1.0f;
        berserker.movementSpeedMultiplier = 1.5f;
        berserker.attackSpeedMultiplier = 1.5f;
        berserker.preferredDistance = 1.5f;
        berserker.heavyAttackProbability = 0.8f;
        berserker.selectionProbability = 0.2f;
        _personalities.Add(berserker);

        PersonalityProfile tactical = new PersonalityProfile();
        tactical.name = "Tactical";
        tactical.aggressionLevel = 0.5f;
        tactical.movementSpeedMultiplier = 1.2f;
        tactical.attackSpeedMultiplier = 0.9f;
        tactical.preferredDistance = 5f;
        tactical.heavyAttackProbability = 0.4f;
        tactical.selectionProbability = 0.2f;
        _personalities.Add(tactical);

        Debug.Log($"[Boss] Created {_personalities.Count} default personalities");

        // Make sure probabilities sum to 1
        NormalizeProbabilities(null, 0);
    }

    // Load personality probabilities from saved data
    private void LoadPersonalityProbabilities()
    {
        // Check if dataPersistence component exists
        if (dataPersistence == null)
        {
            Debug.LogWarning("[Boss] RLDataPersistence component is missing. Skipping data loading.");
            return;
        }

        List<PersonalityProfile> loadedProfiles = dataPersistence.LoadPersonalityData();

        // Apply loaded probabilities to matching personalities
        if (loadedProfiles != null && loadedProfiles.Count > 0)
        {
            foreach (var loadedProfile in loadedProfiles)
            {
                if (loadedProfile == null || string.IsNullOrEmpty(loadedProfile.name))
                {
                    continue;
                }

                PersonalityProfile existingProfile = _personalities.Find(p => p.name == loadedProfile.name);
                if (existingProfile != null)
                {
                    existingProfile.selectionProbability = loadedProfile.selectionProbability;
                }
            }

            Debug.Log($"[Boss] Loaded personality probabilities: {GetProbabilitiesString()}");
        }
        else
        {
            Debug.Log("[Boss] No saved personality data found or data was empty. Using default probabilities.");
        }
    }

    // Get formatted string of all personality probabilities
    private string GetProbabilitiesString()
    {
        string result = "";

        if (_personalities == null) return "No personalities defined";

        foreach (var p in _personalities)
        {
            if (p != null)
            {
                result += $"{p.name}={p.selectionProbability:F2}, ";
            }
        }
        return result.TrimEnd(' ', ',');
    }

    // Log information about all available personalities
    private void LogPersonalityInfo()
    {
        Debug.Log($"[Boss] Available personalities ({_personalities.Count}):");
        foreach (var p in _personalities)
        {
            if (p != null)
            {
                Debug.Log($"  - {p.name}: Aggression={p.aggressionLevel:F2}, " +
                          $"MoveSpeed={p.movementSpeedMultiplier:F2}, " +
                          $"AttackSpeed={p.attackSpeedMultiplier:F2}, " +
                          $"Probability={p.selectionProbability:F2}");
            }
            else
            {
                Debug.LogWarning($"  - NULL personality entry found!");
            }
        }
    }

    // Public methods for editor
    public void ClearPersonalities()
    {
        if (_personalities == null)
        {
            _personalities = new List<PersonalityProfile>();
        }
        else
        {
            _personalities.Clear();
        }

        _currentPersonality = null;
    }

    public void InitDefaultPersonalities()
    {
        CreateDefaultPersonalities();
    }

    public void EqualizePersonalityProbabilities()
    {
        if (_personalities == null || _personalities.Count == 0) return;

        int validCount = 0;
        foreach (var p in _personalities)
        {
            if (p != null) validCount++;
        }

        if (validCount == 0) return;

        float equalProbability = 1.0f / validCount;

        foreach (var p in _personalities)
        {
            if (p != null)
            {
                p.selectionProbability = equalProbability;
            }
        }

        Debug.Log($"[Boss] Equalized all personality probabilities to {equalProbability:F3}");
    }
}