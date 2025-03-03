using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// Note: BossStateType is already defined globally in BossAttackSystem.cs
// We'll use that enum instead of redefining it here

public abstract class BossBaseState
{
    protected BossStateMachine stateMachine;
    protected PersonalityProfile personality;

    public BossBaseState(BossStateMachine stateMachine)
    {
        this.stateMachine = stateMachine;
        this.personality = stateMachine.CurrentPersonality;
    }

    public virtual void Enter() { }
    public virtual void Exit() { }
    public virtual void Update() { }
    public virtual void FixedUpdate() { }

    public virtual void OnPersonalityChanged(PersonalityProfile newPersonality)
    {
        this.personality = newPersonality;
    }
}

public class BossStateMachine : MonoBehaviour
{
    public Transform player;
    public NavMeshAgent navAgent;
    public Animator animator;

    private BossStateType stateBeforeAttack;
    private bool isInAttackAnimation = false;

    public BossStateType currentStateType;
    private Dictionary<BossStateType, BossBaseState> states = new Dictionary<BossStateType, BossBaseState>();
    private BossBaseState currentState;

    public int lightAttackDamage = 10;
    public float lightAttackRange = 4f;
    public float lightAttackCooldown = 1f;

    public int heavyAttackDamage = 25;
    public float heavyAttackRange = 5.5f;
    public float heavyAttackCooldown = 3f;
    public float heavyAttackWindup = 1.5f;

    public float baseMovementSpeed = 3.5f;
    public float currentMovementSpeed;

    public float currentPreferredDistance;
    public float aggressionFactor;
    public float attackRangeBuffer = 0.5f;

    public float lightAttackTimer = 0f;
    public float heavyAttackTimer = 0f;

    public PersonalityProfile CurrentPersonality { get; private set; }

    private BossPersonalitySystem personalitySystem;
    private BossAttackSystem attackSystem;

    private void Awake()
    {
        // Get required components
        personalitySystem = GetComponent<BossPersonalitySystem>();
        attackSystem = GetComponent<BossAttackSystem>();

        // Setup components if not assigned
        if (navAgent == null)
        {
            navAgent = GetComponent<NavMeshAgent>();
            if (navAgent == null)
            {
                Debug.LogError("[Boss] NavMeshAgent component is required but missing!");
                enabled = false;
                return;
            }
        }

        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                Debug.LogWarning("[Boss] Animator component is missing. Animation transitions will not work.");
            }
        }

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
                Debug.Log("[Boss] Found player automatically using 'Player' tag.");
            }
            else
            {
                Debug.LogWarning("[Boss] Player reference is missing and cannot be found with 'Player' tag. Boss behavior will be limited.");
            }
        }

        // Verify attack system
        if (attackSystem == null)
        {
            Debug.LogWarning("[Boss] BossAttackSystem component is missing. Adding one automatically.");
            attackSystem = gameObject.AddComponent<BossAttackSystem>();

            // Initialize attack system with our damage values
            attackSystem.lightAttackDamage = lightAttackDamage;
            attackSystem.heavyAttackDamage = heavyAttackDamage;
        }
        if (attackSystem != null)
        {
            attackSystem.OnAttackStarted += HandleAttackStarted;
            attackSystem.OnAttackEnded += HandleAttackEnded;
        }

        // Initialize states dictionary
        if (states == null)
        {
            states = new Dictionary<BossStateType, BossBaseState>();
        }

        // Initialize states
        states[BossStateType.Idle] = new BossIdleState(this);
        states[BossStateType.Chase] = new BossChaseState(this);
        states[BossStateType.CirclePlayer] = new BossCirclePlayerState(this);
        states[BossStateType.LightAttack] = new BossLightAttackState(this);
        states[BossStateType.HeavyAttack] = new BossHeavyAttackState(this);
        states[BossStateType.Retreat] = new BossRetreatState(this);

        // Start in idle state if component is enabled
        if (enabled)
        {
            ChangeState(BossStateType.Idle);
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks
        if (attackSystem != null)
        {
            attackSystem.OnAttackStarted -= HandleAttackStarted;
            attackSystem.OnAttackEnded -= HandleAttackEnded;
        }
    }

    private void HandleAttackStarted(BossAttackSystem.AttackState attackType, float duration)
    {
        // Store current state to return to after attack
        stateBeforeAttack = currentStateType;

        // Enter attack state
        isInAttackAnimation = true;

        // Stop movement during attack
        if (navAgent != null)
        {
            navAgent.isStopped = true;
        }

        // Change to appropriate attack state based on the attack type
        if (attackType == BossAttackSystem.AttackState.LightAttack)
        {
            ChangeState(BossStateType.LightAttack);
        }
        else if (attackType == BossAttackSystem.AttackState.HeavyAttack)
        {
            ChangeState(BossStateType.HeavyAttack);
        }

        Debug.Log($"[BossStateMachine] Entered attack state: {attackType}, stopped movement for {duration} seconds");
    }

    private void HandleAttackEnded(BossAttackSystem.AttackState attackType, float duration)
    {
        isInAttackAnimation = false;

        // Re-enable movement
        if (navAgent != null)
        {
            navAgent.isStopped = false;
        }

        // Return to previous state
        ChangeState(stateBeforeAttack);

        Debug.Log($"[BossStateMachine] Attack {attackType} completed, returning to {stateBeforeAttack} state");
    }

    public bool CanMove()
    {
        // Can't move if in attack animation
        return !isInAttackAnimation;
    }

    public void MoveToTarget(Vector3 position)
    {
        if (!CanMove())
        {
            return;
        }

        if (navAgent != null && navAgent.isActiveAndEnabled)
        {
            navAgent.SetDestination(position);
        }
    }

    public void StopMoving()
    {
        if (navAgent != null && navAgent.isActiveAndEnabled)
        {
            navAgent.isStopped = true;
        }
    }

    public void ResumeMoving()
    {
        if (!CanMove())
        {
            return;
        }

        if (navAgent != null && navAgent.isActiveAndEnabled)
        {
            navAgent.isStopped = false;
        }
    }

private void UpdateAnimator()
{
    if (animator == null) return;

    // Update movement animation
    animator.SetBool("Moving",
        currentStateType == BossStateType.Chase ||
        currentStateType == BossStateType.CirclePlayer ||
        currentStateType == BossStateType.Retreat);

    // Set attack speed multiplier based on personality
    if (CurrentPersonality != null)
    {
        animator.SetFloat("AttackSpeedMultiplier", CurrentPersonality.attackSpeedMultiplier);
    }
}

    private void Start()
    {
        // Apply initial personality
        if (personalitySystem && personalitySystem.currentPersonality != null)
        {
            UpdatePersonalityParameters(personalitySystem.currentPersonality);
        }

        // Sync attack ranges with attack system
        if (attackSystem != null)
        {
            attackSystem.lightAttackRadius = lightAttackRange;
            attackSystem.heavyAttackRadius = heavyAttackRange;
            attackSystem.lightAttackDamage = lightAttackDamage;
            attackSystem.heavyAttackDamage = heavyAttackDamage;
        }
    }

    private void Update()
    {
        // Update attack cooldowns
        if (lightAttackTimer > 0)
            lightAttackTimer -= Time.deltaTime;

        if (heavyAttackTimer > 0)
            heavyAttackTimer -= Time.deltaTime;

    // Add this line to update animations every frame
    UpdateAnimator();

    // Update current state
    if (currentState != null)
        currentState.Update();
}

    private void FixedUpdate()
    {
        if (currentState != null)
            currentState.FixedUpdate();
    }

    public void ChangeState(BossStateType newState)
    {
        // Validate state change
        if (states == null)
        {
            Debug.LogError("[Boss] Cannot change state: states dictionary is null");
            return;
        }

        if (!states.ContainsKey(newState))
        {
            Debug.LogError($"[Boss] Cannot change to {newState} state: state does not exist in the dictionary");
            return;
        }

        // Exit current state
        if (currentState != null)
        {
            currentState.Exit();
        }

        // Change state
        currentStateType = newState;
        currentState = states[newState];

        // Enter new state
        if (currentState != null)
        {
            currentState.Enter();
            Debug.Log($"[Boss] Changed to {newState} state");
        }
        else
        {
            Debug.LogError($"[Boss] State {newState} exists in dictionary but is null");
        }
    }

    public void UpdatePersonalityParameters(PersonalityProfile personality)
    {
        if (personality == null)
        {
            Debug.LogError("[Boss] Cannot update parameters: personality is null");
            return;
        }

        CurrentPersonality = personality;

        // Update movement parameters
        currentMovementSpeed = baseMovementSpeed * personality.movementSpeedMultiplier;
        if (navAgent != null)
        {
            navAgent.speed = currentMovementSpeed;
        }

        // Update combat parameters
        currentPreferredDistance = personality.preferredDistance;
        aggressionFactor = personality.aggressionLevel;

        // Update attack speeds (via animation speed)
        if (animator != null)
        {
            animator.SetFloat("AttackSpeedMultiplier", personality.attackSpeedMultiplier);
        }

        // Notify current state about personality change
        if (currentState != null)
        {
            currentState.OnPersonalityChanged(personality);
        }

        Debug.Log($"[Boss] Applied personality parameters: Speed={currentMovementSpeed:F1}, " +
                  $"Preferred Distance={currentPreferredDistance:F1}, Aggression={aggressionFactor:F2}");
    }

    public float GetDistanceToPlayer()
    {
        if (player == null)
            return Mathf.Infinity;

        return Vector3.Distance(transform.position, player.position);
    }

    public void DealDamageToPlayer(int amount)
    {
        // Don't apply direct damage here - use the attack system instead
        Debug.Log($"[Boss] Attempting to deal {amount} damage to player");

        // For backward compatibility - register damage dealt for reinforcement learning
        if (personalitySystem != null)
        {
            personalitySystem.RegisterDamageDealt(amount);
        }
    }

    public void TakeDamage(float amount)
    {
        // Handle boss taking damage here
        Debug.Log($"[Boss] Taking {amount} damage");

        // Track damage taken for reinforcement learning
        if (personalitySystem != null)
        {
            personalitySystem.RegisterDamageTaken(amount);
        }
        else
        {
            Debug.LogWarning("[Boss] Cannot register damage taken: BossPersonalitySystem is missing");
        }
    }

    public bool IsInAttackRange(float range)
    {
        return GetDistanceToPlayer() <= range + attackRangeBuffer;
    }

    public bool ShouldUseHeavyAttack()
    {
        if (CurrentPersonality == null) return false;
        return Random.value <= CurrentPersonality.heavyAttackProbability;
    }

    public Vector3 GetDirectionToPlayer()
    {
        if (player == null)
            return Vector3.forward;

        return (player.position - transform.position).normalized;
    }

    // Attack Methods - Call these from states

    public bool PerformLightAttack()
    {
        if (attackSystem == null)
        {
            Debug.LogError("[Boss] Cannot perform light attack: Attack System is missing");
            return false;
        }

        bool hit = attackSystem.PerformLightAttack();

        // Trigger animation
        if (animator != null)
        {
            animator.SetTrigger("LightAttack");
        }

        return hit;
    }

    public bool PerformHeavyAttack()
    {
        if (attackSystem == null)
        {
            Debug.LogError("[Boss] Cannot perform heavy attack: Attack System is missing");
            return false;
        }

        bool hit = attackSystem.PerformHeavyAttack();

        // We don't need to trigger a separate animation here anymore
        // since we're using a single animation with events

        return hit;
    }
    public void OnWindupComplete()
    {
        // This will be called when the windup phase of the animation completes
        if (currentStateType == BossStateType.HeavyAttack)
        {
            BossHeavyAttackState heavyState = currentState as BossHeavyAttackState;
            if (heavyState != null)
            {
                heavyState.CompleteWindup();
            }
        }
    }



    // Animation Event callbacks

    public void OnLightAttackPoint()
    {
        // This is called from animation event at the point of attack
        if (currentStateType == BossStateType.LightAttack)
        {
            PerformLightAttack();
        }
    }

    public void OnHeavyAttackPoint()
    {
        // This is called from animation event at the point of attack
        if (currentStateType == BossStateType.HeavyAttack)
        {
            PerformHeavyAttack();
        }
    }
}

// Movement States
public class BossIdleState : BossBaseState
{
    private float idleDuration = 1f;
    private float timer = 0f;
    private float attackCheckInterval = 0.2f;
    private float attackCheckTimer = 0f;

    public BossIdleState(BossStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        stateMachine.navAgent.isStopped = true;
        stateMachine.animator?.SetTrigger("Idle");

        timer = 0f;
        attackCheckTimer = 0f;
        idleDuration = Random.Range(0.5f, 1.5f);
    }

    public override void Update()
    {
        timer += Time.deltaTime;
        attackCheckTimer += Time.deltaTime;

        // Check for attack opportunities frequently
        if (attackCheckTimer >= attackCheckInterval)
        {
            attackCheckTimer = 0f;

            // Check if player is in attack range while idle
            if (stateMachine.IsInAttackRange(stateMachine.heavyAttackRange) &&
                stateMachine.heavyAttackTimer <= 0 &&
                stateMachine.ShouldUseHeavyAttack() &&
                Random.value < stateMachine.aggressionFactor)
            {
                stateMachine.ChangeState(BossStateType.HeavyAttack);
                return;
            }
            else if (stateMachine.IsInAttackRange(stateMachine.lightAttackRange) &&
                     stateMachine.lightAttackTimer <= 0)
            {
                stateMachine.ChangeState(BossStateType.LightAttack);
                return;
            }
        }

        if (timer >= idleDuration)
        {
            // Transition based on distance and aggression
            float distanceToPlayer = stateMachine.GetDistanceToPlayer();

            if (distanceToPlayer < stateMachine.currentPreferredDistance * 0.8f)
            {
                // Too close, retreat or attack based on aggression
                if (Random.value < stateMachine.aggressionFactor &&
                    (stateMachine.lightAttackTimer <= 0 || stateMachine.heavyAttackTimer <= 0))
                {
                    // Choose attack type
                    if (stateMachine.ShouldUseHeavyAttack() && stateMachine.heavyAttackTimer <= 0)
                    {
                        stateMachine.ChangeState(BossStateType.HeavyAttack);
                    }
                    else if (stateMachine.lightAttackTimer <= 0)
                    {
                        stateMachine.ChangeState(BossStateType.LightAttack);
                    }
                    else
                    {
                        stateMachine.ChangeState(BossStateType.Retreat);
                    }
                }
                else
                {
                    stateMachine.ChangeState(BossStateType.Retreat);
                }
            }
            else if (distanceToPlayer > stateMachine.currentPreferredDistance * 1.2f)
            {
                // Too far, chase player
                stateMachine.ChangeState(BossStateType.Chase);
            }
            else
            {
                // At good distance, circle or attack
                if (Random.value < stateMachine.aggressionFactor &&
                    (stateMachine.lightAttackTimer <= 0 || stateMachine.heavyAttackTimer <= 0))
                {
                    // Choose attack type
                    if (stateMachine.ShouldUseHeavyAttack() && stateMachine.heavyAttackTimer <= 0)
                    {
                        stateMachine.ChangeState(BossStateType.HeavyAttack);
                    }
                    else if (stateMachine.lightAttackTimer <= 0)
                    {
                        stateMachine.ChangeState(BossStateType.LightAttack);
                    }
                    else
                    {
                        stateMachine.ChangeState(BossStateType.CirclePlayer);
                    }
                }
                else
                {
                    stateMachine.ChangeState(BossStateType.CirclePlayer);
                }
            }
        }
    }

    public override void Exit()
    {
        stateMachine.navAgent.isStopped = false;
    }
}

public class BossChaseState : BossBaseState
{
    private float updateDestinationInterval = 0.2f;
    private float timer = 0f;
    private float attackCheckInterval = 0.1f;
    private float attackCheckTimer = 0f;

    public BossChaseState(BossStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        stateMachine.animator?.SetBool("Moving", true);
        timer = updateDestinationInterval; // Force immediate update
        attackCheckTimer = 0f;
    }

    public override void Update()
    {
        timer += Time.deltaTime;
        attackCheckTimer += Time.deltaTime;

        // Check for attack opportunities frequently during chase
        if (attackCheckTimer >= attackCheckInterval)
        {
            attackCheckTimer = 0f;

            // Check if we suddenly came into attack range while chasing
            if (stateMachine.IsInAttackRange(stateMachine.heavyAttackRange) &&
                stateMachine.heavyAttackTimer <= 0 &&
                stateMachine.ShouldUseHeavyAttack() &&
                Random.value < stateMachine.aggressionFactor * 1.5f) // More aggressive while chasing
            {
                stateMachine.ChangeState(BossStateType.HeavyAttack);
                return;
            }
            else if (stateMachine.IsInAttackRange(stateMachine.lightAttackRange) &&
                     stateMachine.lightAttackTimer <= 0)
            {
                stateMachine.ChangeState(BossStateType.LightAttack);
                return;
            }
        }

        if (timer >= updateDestinationInterval)
        {
            timer = 0f;
            UpdateDestination();
        }

        float distanceToPlayer = stateMachine.GetDistanceToPlayer();

        // Check if we're getting close to preferred distance
        if (distanceToPlayer <= stateMachine.currentPreferredDistance)
        {
            // At good distance, decide what to do based on aggression
            if (Random.value < stateMachine.aggressionFactor &&
                stateMachine.IsInAttackRange(stateMachine.heavyAttackRange) &&
                stateMachine.heavyAttackTimer <= 0 &&
                stateMachine.ShouldUseHeavyAttack())
            {
                stateMachine.ChangeState(BossStateType.HeavyAttack);
            }
            else if (Random.value < stateMachine.aggressionFactor &&
                     stateMachine.IsInAttackRange(stateMachine.lightAttackRange) &&
                     stateMachine.lightAttackTimer <= 0)
            {
                stateMachine.ChangeState(BossStateType.LightAttack);
            }
            else
            {
                stateMachine.ChangeState(BossStateType.CirclePlayer);
            }
        }
    }

    private void UpdateDestination()
    {
        if (stateMachine.player != null)
        {
            // Calculate a point in the direction of the player but at the preferred distance
            Vector3 directionToPlayer = stateMachine.GetDirectionToPlayer();
            Vector3 targetPosition = stateMachine.player.position - directionToPlayer * stateMachine.currentPreferredDistance;

            stateMachine.navAgent.SetDestination(targetPosition);
        }
    }

    public override void Exit()
    {
        stateMachine.animator?.SetBool("Moving", false);
    }
}

public class BossCirclePlayerState : BossBaseState
{
    private float circlingDuration;
    private float timer = 0f;
    private float circleDirection = 1f; // 1 for clockwise, -1 for counter-clockwise
    private float updateDestinationInterval = 0.2f;
    private float destinationTimer = 0f;

    public BossCirclePlayerState(BossStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        stateMachine.animator?.SetBool("Moving", true);
        timer = 0f;
        destinationTimer = updateDestinationInterval; // Force immediate update

        // Randomize circling duration and direction
        circlingDuration = Random.Range(3f, 5f);
        circleDirection = Random.value > 0.5f ? 1f : -1f;
    }

    public override void Update()
    {
        timer += Time.deltaTime;
        destinationTimer += Time.deltaTime;

        if (destinationTimer >= updateDestinationInterval)
        {
            destinationTimer = 0f;
            UpdateDestination();
        }

        float distanceToPlayer = stateMachine.GetDistanceToPlayer();

        // Check for attack opportunities
        if (Random.value < (stateMachine.aggressionFactor * Time.deltaTime * 2f))
        {
            if (stateMachine.IsInAttackRange(stateMachine.heavyAttackRange) &&
                stateMachine.heavyAttackTimer <= 0 &&
                stateMachine.ShouldUseHeavyAttack())
            {
                stateMachine.ChangeState(BossStateType.HeavyAttack);
                return;
            }
            else if (stateMachine.IsInAttackRange(stateMachine.lightAttackRange) &&
                     stateMachine.lightAttackTimer <= 0)
            {
                stateMachine.ChangeState(BossStateType.LightAttack);
                return;
            }
        }

        // Check if we're too far or too close
        if (distanceToPlayer > stateMachine.currentPreferredDistance * 1.3f)
        {
            stateMachine.ChangeState(BossStateType.Chase);
            return;
        }
        else if (distanceToPlayer < stateMachine.currentPreferredDistance * 0.7f)
        {
            stateMachine.ChangeState(BossStateType.Retreat);
            return;
        }

        // Change state after circling duration
        if (timer >= circlingDuration)
        {
            stateMachine.ChangeState(BossStateType.Idle);
        }
    }

    private void UpdateDestination()
    {
        if (stateMachine.player != null)
        {
            // Calculate a point around the player to circle to
            Vector3 directionToPlayer = stateMachine.GetDirectionToPlayer();
            Vector3 perpendicularDirection = new Vector3(-directionToPlayer.z * circleDirection, 0, directionToPlayer.x * circleDirection);

            Vector3 targetPosition = stateMachine.player.position -
                                     directionToPlayer * stateMachine.currentPreferredDistance +
                                     perpendicularDirection * 2f;

            stateMachine.navAgent.SetDestination(targetPosition);
        }
    }

    public override void Exit()
    {
        stateMachine.animator?.SetBool("Moving", false);
    }
}

public class BossRetreatState : BossBaseState
{
    private float retreatDuration = 2f;
    private float timer = 0f;
    private float updateDestinationInterval = 0.2f;
    private float destinationTimer = 0f;

    public BossRetreatState(BossStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        stateMachine.animator?.SetBool("Moving", true);
        timer = 0f;
        destinationTimer = updateDestinationInterval; // Force immediate update

        // Personality affects retreat duration
        if (personality != null)
        {
            retreatDuration = 2f - personality.aggressionLevel;
        }
    }

    public override void Update()
    {
        timer += Time.deltaTime;
        destinationTimer += Time.deltaTime;

        if (destinationTimer >= updateDestinationInterval)
        {
            destinationTimer = 0f;
            UpdateRetreatDestination();
        }

        float distanceToPlayer = stateMachine.GetDistanceToPlayer();

        // If we've reached a good distance, stop retreating
        if (distanceToPlayer >= stateMachine.currentPreferredDistance || timer >= retreatDuration)
        {
            stateMachine.ChangeState(BossStateType.Idle);
        }
    }

    private void UpdateRetreatDestination()
    {
        if (stateMachine.player != null)
        {
            // Calculate a point away from the player
            Vector3 directionFromPlayer = -stateMachine.GetDirectionToPlayer();
            Vector3 targetPosition = stateMachine.transform.position + directionFromPlayer * 5f;

            // Find a valid NavMesh position
            NavMeshHit hit;
            if (NavMesh.SamplePosition(targetPosition, out hit, 10f, NavMesh.AllAreas))
            {
                stateMachine.navAgent.SetDestination(hit.position);
            }
        }
    }

    public override void Exit()
    {
        stateMachine.animator?.SetBool("Moving", false);
    }
}



public class BossLightAttackState : BossBaseState
{
    private float attackDelay = 0.3f; // Time between entering state and dealing damage
    private float attackDuration = 0.8f; // Total duration of attack animation
    private float timer = 0f;
    private bool damageDealt = false;

    public BossLightAttackState(BossStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        stateMachine.navAgent.isStopped = true;
        stateMachine.animator?.SetTrigger("LightAttack");

        // Look at player
        if (stateMachine.player != null)
        {
            Vector3 lookDirection = stateMachine.player.position - stateMachine.transform.position;
            lookDirection.y = 0;
            if (lookDirection != Vector3.zero)
            {
                stateMachine.transform.rotation = Quaternion.LookRotation(lookDirection);
            }
        }

        timer = 0f;
        damageDealt = false;

        // Apply personality-specific timing adjustments
        if (personality != null)
        {
            attackDelay /= personality.attackSpeedMultiplier;
            attackDuration /= personality.attackSpeedMultiplier;
        }
    }

    public override void Update()
    {
        timer += Time.deltaTime;

        // Deal damage at the right moment in the animation
        if (!damageDealt && timer >= attackDelay && stateMachine.IsInAttackRange(stateMachine.lightAttackRange))
        {
            damageDealt = stateMachine.PerformLightAttack();

            // If using animation events instead, this will be handled by the OnLightAttackPoint method
            // in which case we shouldn't call PerformLightAttack here
        }

        // End attack after duration
        if (timer >= attackDuration)
        {
            // Set cooldown
            float cooldownModifier = personality != null ? personality.attackSpeedMultiplier : 1f;
            stateMachine.lightAttackTimer = stateMachine.lightAttackCooldown / cooldownModifier;

            // Transition to next state based on distance and aggression
            float distanceToPlayer = stateMachine.GetDistanceToPlayer();

            if (distanceToPlayer < stateMachine.currentPreferredDistance * 0.7f)
            {
                stateMachine.ChangeState(BossStateType.Retreat);
            }
            else if (distanceToPlayer > stateMachine.currentPreferredDistance * 1.3f)
            {
                stateMachine.ChangeState(BossStateType.Chase);
            }
            else if (stateMachine.heavyAttackTimer <= 0 &&
                      Random.value < stateMachine.aggressionFactor &&
                      stateMachine.ShouldUseHeavyAttack())
            {
                stateMachine.ChangeState(BossStateType.HeavyAttack);
            }
            else
            {
                stateMachine.ChangeState(BossStateType.CirclePlayer);
            }
        }
    }

    public override void Exit()
    {
        stateMachine.navAgent.isStopped = false;
    }
}

public class BossHeavyAttackState : BossBaseState
{
    private float windupDuration;
    private float attackDuration = 1.5f;
    private float timer = 0f;
    private bool damageDealt = false;
    private bool inWindupPhase = true;

    public BossHeavyAttackState(BossStateMachine stateMachine) : base(stateMachine) { }

    // Add this new method to handle the animation event
    public void CompleteWindup()
    {
        inWindupPhase = false;
        timer = 0f;
    }

    public override void Enter()
    {
        stateMachine.navAgent.isStopped = true;

        // Instead of triggering HeavyAttackWindup, use CrossPunch
        stateMachine.animator?.SetTrigger("CrossPunch");

        timer = 0f;
        damageDealt = false;
        inWindupPhase = true;

        // Apply personality-specific timing adjustments
        float speedMultiplier = personality != null ? personality.attackSpeedMultiplier : 1f;
        windupDuration = stateMachine.heavyAttackWindup / speedMultiplier;
        attackDuration /= speedMultiplier;

        // Look directly at player
        if (stateMachine.player != null)
        {
            Vector3 lookDirection = stateMachine.player.position - stateMachine.transform.position;
            lookDirection.y = 0;
            if (lookDirection != Vector3.zero)
            {
                stateMachine.transform.rotation = Quaternion.LookRotation(lookDirection);
            }
        }
    }

    public override void Update()
    {
        timer += Time.deltaTime;

        if (inWindupPhase)
        {
            // Keep facing the player during windup
            if (stateMachine.player != null)
            {
                Vector3 lookDirection = stateMachine.player.position - stateMachine.transform.position;
                lookDirection.y = 0;
                if (lookDirection != Vector3.zero)
                {
                    stateMachine.transform.rotation = Quaternion.Slerp(
                        stateMachine.transform.rotation,
                        Quaternion.LookRotation(lookDirection),
                        Time.deltaTime * 5f
                    );
                }
            }

            // Manual fallback if animation event doesn't trigger
            if (timer >= windupDuration)
            {
                CompleteWindup();
            }
        }
        else
        {
            // Deal damage is now primarily handled by animation event
            // but keep this as a fallback
            if (!damageDealt && timer >= 0.3f && stateMachine.IsInAttackRange(stateMachine.heavyAttackRange))
            {
                damageDealt = stateMachine.PerformHeavyAttack();
            }

            // End attack after duration
            if (timer >= attackDuration)
            {
                // Set cooldown
                float speedMultiplier = personality != null ? personality.attackSpeedMultiplier : 1f;
                stateMachine.heavyAttackTimer = stateMachine.heavyAttackCooldown / speedMultiplier;

                // Heavy attacks leave the boss vulnerable for a moment
                stateMachine.ChangeState(BossStateType.Idle);
            }
        }
    }

    public override void Exit()
    {
        stateMachine.navAgent.isStopped = false;
    }
}