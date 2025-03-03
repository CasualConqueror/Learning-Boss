using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Global enum definitions to resolve namespace issues
public enum BossStateType
{
    Idle,
    Chase,
    CirclePlayer,
    LightAttack,
    HeavyAttack,
    Retreat
}

/// <summary>
/// Handles boss attack detection, damage application, and environmental interactions like breaking columns.
/// </summary>
[RequireComponent(typeof(Collider))]
public class BossAttackSystem : MonoBehaviour
{
    public enum AttackState
    {
        None,
        LightAttack,
        HeavyAttack
    }

    [Header("Attack Settings")]
    public LayerMask playerLayer;
    public LayerMask environmentLayer;

    [Header("Light Attack")]
    public int lightAttackDamage = 10;
    public float lightAttackRadius = 5f;
    public float lightAttackAngle = 120f;
    public float lightAttackDuration = 1.2f;

    [Header("Heavy Attack")]
    public int heavyAttackDamage = 25;
    public float heavyAttackRadius = 7f;
    public float heavyAttackAngle = 150f;
    public float heavyAttackDuration = 2.0f;

    [Header("Environmental Interaction")]
    public bool canBreakColumns = true;
    public float columnBreakForce = 10f;

    [Header("Debug")]
    public bool showAttackDebug = true;

    private BossPersonalitySystem personalitySystem;
    private BossStateMachine stateMachine;
    private bool isAttacking = false;
    private float attackEndTime = 0f;
    private AttackState currentAttackState = AttackState.None;
    private BossStateType stateBeforeAttack;

    public delegate void AttackEvent(AttackState attackType, float duration);
    public event AttackEvent OnAttackStarted;
    public event AttackEvent OnAttackEnded;
    public delegate void EnvironmentInteractionEvent(GameObject interactedObject);
    public event EnvironmentInteractionEvent OnEnvironmentInteraction;

    private void Awake()
    {
        personalitySystem = GetComponent<BossPersonalitySystem>();
        stateMachine = GetComponent<BossStateMachine>();

        AutoConfigureLayer(ref playerLayer, "Player");
        AutoConfigureLayer(ref environmentLayer, "Environment");
    }

    private void Update()
    {
        if (isAttacking && Time.time >= attackEndTime)
        {
            EndAttack();
        }
    }

    public bool PerformLightAttack()
    {
        if (isAttacking) return false;

        BeginAttack(AttackState.LightAttack, lightAttackDuration);
        return PerformAttack(lightAttackDamage, lightAttackRadius, lightAttackAngle) ||
               CheckEnvironmentInteractions(lightAttackRadius, lightAttackAngle);
    }

    public bool PerformHeavyAttack()
    {
        if (isAttacking) return false;

        BeginAttack(AttackState.HeavyAttack, heavyAttackDuration);
        return PerformAttack(heavyAttackDamage, heavyAttackRadius, heavyAttackAngle) ||
               CheckEnvironmentInteractions(heavyAttackRadius, heavyAttackAngle);
    }

    private void BeginAttack(AttackState attackType, float duration)
    {
        isAttacking = true;
        currentAttackState = attackType;
        attackEndTime = Time.time + duration;
        OnAttackStarted?.Invoke(attackType, duration);
        Debug.Log($"[BossAttack] Starting {attackType} attack for {duration} seconds");
    }

    private void EndAttack()
    {
        isAttacking = false;
        OnAttackEnded?.Invoke(currentAttackState, 0f);
        currentAttackState = AttackState.None;
        Debug.Log("[BossAttack] Attack finished");
    }

    private bool PerformAttack(int damage, float radius, float angle)
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, radius, playerLayer);
        bool hitAnyPlayer = false;

        foreach (var collider in hitColliders)
        {
            if (IsTargetWithinAngle(collider.transform.position, angle))
            {
                PlayerStats playerStats = collider.GetComponent<PlayerStats>();
                if (playerStats != null)
                {
                    playerStats.TakeDamage(damage);
                    personalitySystem?.RegisterDamageDealt(damage);
                    hitAnyPlayer = true;
                }
            }
        }

        return hitAnyPlayer;
    }

    private bool CheckEnvironmentInteractions(float radius, float angle)
    {
        if (!canBreakColumns) return false;

        Collider[] environmentObjects = Physics.OverlapSphere(transform.position, radius, environmentLayer);
        bool interactedWithEnvironment = false;

        foreach (var collider in environmentObjects)
        {
            if (IsTargetWithinAngle(collider.transform.position, angle))
            {
                ColumnBreakScript columnBreak = collider.GetComponent<ColumnBreakScript>();
                if (columnBreak != null && !columnBreak.isBroken)
                {
                    columnBreak.BreakColumn();
                    ApplyForceToDebris(columnBreak.gameObject, (collider.transform.position - transform.position).normalized);
                    OnEnvironmentInteraction?.Invoke(columnBreak.gameObject);
                    interactedWithEnvironment = true;
                    Debug.Log($"[BossAttack] Boss broke a column at {columnBreak.transform.position}");
                }
            }
        }

        return interactedWithEnvironment;
    }

    private bool IsTargetWithinAngle(Vector3 targetPosition, float angle)
    {
        Vector3 directionToTarget = (targetPosition - transform.position).normalized;
        float targetAngle = Vector3.Angle(transform.forward, directionToTarget);
        return targetAngle <= angle / 2;
    }

    private void ApplyForceToDebris(GameObject brokenObject, Vector3 direction)
    {
        Rigidbody[] debrisRigidbodies = brokenObject.GetComponentsInChildren<Rigidbody>();

        foreach (var rb in debrisRigidbodies)
        {
            rb.AddForce(direction * columnBreakForce, ForceMode.Impulse);
            rb.AddTorque(UnityEngine.Random.insideUnitSphere * columnBreakForce, ForceMode.Impulse);
        }
    }

    private void AutoConfigureLayer(ref LayerMask layer, string layerName)
    {
        if (layer == 0)
        {
            int layerIndex = LayerMask.NameToLayer(layerName);
            if (layerIndex != -1)
            {
                layer = 1 << layerIndex;
                Debug.Log($"[BossAttack] Automatically set {layerName} layer to {layerIndex}");
            }
            else
            {
                Debug.LogWarning($"[BossAttack] {layerName} layer not found. Please set it manually.");
            }
        }
    }

    public bool IsAttacking() => isAttacking;
    public AttackState GetCurrentAttackState() => currentAttackState;
}
