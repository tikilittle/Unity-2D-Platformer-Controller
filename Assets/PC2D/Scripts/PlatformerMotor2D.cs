﻿using UnityEngine;
using System.Collections;
using PC2D;

[RequireComponent(typeof(Rigidbody2D))]
public class PlatformerMotor2D : MonoBehaviour
{

    #region Public

    /// <summary>
    /// The maximum speed the motor will move on the ground, only effects horizontal speed.
    /// </summary>
    public float maxGroundSpeed = 3f;
    
    /// <summary>
    /// How much time does it take for the motor to get from zero speed to max speed. This value
    /// is used to calculate the acceleration.
    /// </summary>
    public float timeToMaxGroundSpeed;

    /// <summary>
    /// The distance the motor will slide to a stop from full speed while on the ground.
    /// </summary>
    public float groundStopDistance;

    /// <summary>
    /// The maximum horizontal speed of the motor in the air.
    /// </summary>
    public float maxAirSpeed = 2f;

    /// <summary>
    /// The time it takes to move from zero horizontal speed to the maximum speed. This value is
    /// used to calculate the acceleration.
    /// </summary>
    public float timeToMaxAirSpeed;

    /// <summary>
    /// The distance the motor will 'slide' to a stop while in the air. Only effects horizontal
    /// movement.
    /// </summary>
    public float airStopDistance;

    /// <summary>
    /// The maximum speed that the motor will fall. Only effects vertical speed when falling.
    /// </summary>
    public float maxFallSpeed = 5f;

    /// <summary>
    /// The maximum speed that the motor will fall during 'fast fall'.
    /// </summary>
    public float maxFastFallSpeed = 5f;

    /// <summary>
    /// If the motor is in 'fast fall' then the gravityScale is multiplied by the value. Higher number means
    /// faster acceleration while falling.
    /// </summary>
    public float fastFallGravityMultiplier = 1f;

    /// <summary>
    /// On a collision with the ground the physics engine will slow down any horizontal momentum. If this is set to true
    /// then that horizontal speed is maintained.
    /// </summary>
    public bool preserveHorizontalMomentumOnLanding = true;
    
    /// <summary>
    /// The height the motor will jump when a jump command is issued.
    /// </summary>
    public float baseJumpHeight = 0.5f;

    /// <summary>
    /// The extra height the motor will jump if jump is 'held' down.
    /// </summary>
    public float extraJumpHeight = 0.5f;

    /// <summary>
    /// If double jumps are allowed.
    /// </summary>
    public bool allowDoubleJump = false;

    /// <summary>
    /// If wall jumps are allowed.
    /// </summary>
    public bool allowWallJump = false;

    /// <summary>
    /// The jump speed multiplier when wall jumping. This is useful to force bigger jumps off of the wall.
    /// </summary>
    public float wallJumpMultiplier = 1f;

    /// <summary>
    /// If wall clinging is allowed. A wall cling is when a motor will 'stick' to a wall.
    /// </summary>
    public bool allowWallCling = false;

    /// <summary>
    /// The duration of the wall cling in seconds. Set to a very large number to effectively allow permenent clings.
    /// </summary>
    public float wallClingDuration = 0.5f;

    /// <summary>
    /// If wall slides are allowed. A wall slide is when a motor slides down a wall. This will only take in effect
    /// once the cling is over.
    /// </summary>
    public bool allowWallSlide = false;

    /// <summary>
    /// The speed that the motor will slide down the wall.
    /// </summary>
    public float wallSlideSpeed = 1;

    /// <summary>
    /// Are corner grabs allowed? A corner grab is when the motor sticks to a corner.
    /// </summary>
    public bool allowCornerGrab = false;

    /// <summary>
    /// The jump speed multiplier when jumping from a corner grab. Useful to forcing bigger jumps.
    /// </summary>
    public float cornerJumpMultiplier = 1f;

    /// <summary>
    /// The duraction, in seconds, that the motor will stick to a corner.
    /// </summary>
    public float cornerGrabDuration = 0.5f;

    /// <summary>
    /// This is the size of the corner check. This can be tweaked with if corner grabs are not working correctly.
    /// </summary>
    public float cornerDistanceCheck = 0.1f;

    /// <summary>
    /// Is dashing allowed?
    /// </summary>
    public bool allowDash = false;

    /// <summary>
    /// When the motor will be allowed to dash again after dashing. The cooldown begins at the end of a dash.
    /// </summary>
    public float dashCooldown = 1;

    /// <summary>
    /// How far the motor will dash.
    /// </summary>
    public float dashDistance = 1;

    /// <summary>
    /// How long the dash lasts in seconds.
    /// </summary>
    public float dashDuration = 0.2f;

    /// <summary>
    /// The easing function used during the dash. Pick 'Linear' for just a set speed.
    /// </summary>
    public EasingFunctions.Functions dashEasingFunction = EasingFunctions.Functions.EaseOutQuad;

    /// <summary>
    /// The inputThreshold for the motor. The motor will absolute values less than this for movement.
    /// </summary>
    [Range(0f, 1f)]
    public float inputThreshold = 0.2f;

    /// <summary>
    /// The heavyInputThreshold for the motor. Works similarly as inputThreshold but used for wall clings, wall slides, wall jumps,
    /// and corner grabs.
    /// </summary>
    [Range(0f, 1f)]
    public float heavyInputThreshold = 0.5f;

    /// <summary>
    /// Delegate to attach to when the motor dashes.
    /// </summary>
    public System.Action onDash;

    /// <summary>
    /// Delegate to attach to when the motor's dash ends.
    /// </summary>
    public System.Action onDashEnd;

    /// <summary>
    /// Delegate to attach to when the motor jumps.
    /// </summary>
    public System.Action onJump;

    /// <summary>
    /// How far out the motor will check for the environment mask. This value can be tweaked if jump checks are not firing when wanted.
    /// </summary>
    public float checkDistance = 0.025f;

    /// <summary>
    /// The environment check mask. The motor doesn't know what to consider is an environment so this mask tells it.
    /// </summary>
    public LayerMask checkMask;

    /// <summary>
    /// The states the motor can be in.
    /// </summary>
    public enum MotorState
    {
        OnGround,
        InAir,
        FallingFast,
        Sliding,
        OnCorner,
        Clinging,
        Dashing,
        Frozen
    }

    /// <summary>
    /// Set the x movement direction. This is multiplied by the max speed. -1 is full left, 1 is full right. Higher numbers will result in
    /// faster speed.
    /// </summary>
    public float normalizedXMovement { get; set; }

    /// <summary>
    /// Call this to get state information about the motor. This will be information such as if the object is in the air or on the 
    /// ground. This can be used to set the appropriate animations.
    /// </summary>
    public MotorState motorState { get; private set; }

    /// <summary>
    /// Since the motor needs to know the facing of the object, this information is made available to anyone else who might need it.
    /// </summary>
    public bool facingLeft { get; private set; }

    /// <summary>
    /// Set this true to have the motor fall faster. Set to false to fall at normal speeds.
    /// </summary>
    public bool fallFast { get; set; }

    /// <summary>
    /// If jumpingHeld is set to true then the motor will jump further. Set to false if jumping isn't 'held'.
    /// </summary>
    public bool jumpingHeld
    {
        get
        {
            return _jumping.held;
        }

        set
        {
            // Since we set held to true on pressed, we only set to false here. This prevent held from being set after a release.
            if (!value)
            {
                _jumping.held = value;
            }

        }
    }

    /// <summary>
    /// Setting frozen to true will put the motor in a 'frozen' state. All information will be saved and set once unfrozen (the motor also
    /// reduce gravity to 0).
    /// 
    /// Note: This isn't a way to turn off the motor. To turn off the motor, simply set the script to disabled.
    /// </summary>
    public bool frozen
    {
        get
        {
            return _frozen;
        }
        set
        {
            if (!enabled)
            {
                Debug.LogWarning("PlatformerMotor2D.frozen set to true when motor is disabled, ignoring.");
                return;
            }

            if (_frozen != value)
            {
                _frozen = value;

                if (_frozen)
                {
                    _frozenTime = Time.time;
                    _frozenVelocity = rigidbody2D.velocity;
                    _frozenGravity = rigidbody2D.gravityScale;

                    rigidbody2D.velocity = Vector2.zero;
                    rigidbody2D.gravityScale = 0;
                    motorState = MotorState.Frozen;
                }
                else
                {
                    // Any cooldown or time events happen here.
                    float delta = Time.time - _frozenTime;
                    _dashing.canDashAgain += delta;

                    rigidbody2D.velocity = _frozenVelocity;
                    rigidbody2D.gravityScale = _frozenGravity;
                }
            }
        }
    }

    /// <summary>
    /// Set this to use a specific collider for checks instead of grabbing the collider from gameObject.collider2D.
    /// </summary>
    public Collider2D colliderToUse { get; set; }

    /// <summary>
    /// Call this to have the GameObject try to jump, once called it will be handled in the FixedUpdate tick. The y axis is considered jump.
    /// </summary>
    /// <param name="extraSpeed">Added speed to the default calculated speed.</param>
    public void Jump(float extraSpeed = 0)
    {
        _jumping.pressed = true;
        _jumping.timePressed = Time.time;
        _jumping.isJumping = false;
        _jumping.extraSpeed = extraSpeed;

        // Consider jumping held in case there are multiple fixed ticks before the next update tick.
        // This is useful as jumpingHeld may not be set to true with a GetButton() call.
        _jumping.held = true;
    }

    /// <summary>
    /// This will force a jump to occur even if the motor doesn't think a jump is valid. This function will not work if the motor
    /// is dashing.
    /// </summary>
    /// <param name="extraSpeed">Added speed to the default calculated speed</param>
    public void ForceJump(float extraSpeed = 0)
    {
        Jump(extraSpeed);
        _jumping.force = true;
    }

    /// <summary>
    /// Call to end a jump. Causes the motor to stop calculated held speed for a jump.
    /// </summary>
    public void EndJump()
    {
        _jumping.pressed = false;
        _jumping.isJumping = false;
        _jumping.timePressed = 0;
    }

    /// <summary>
    /// Resets the state for the a double jump allowing another jump. This doesn't do anything if double jumps aren't allowed.
    /// </summary>
    public void ResetDoubleJump()
    {
        _jumping.doubleJumped = false;
    }

    /// <summary>
    /// Call this to have the GameObject try to dash, once called it will be handled in the FixedUpdate tick.
    /// This casues the object to dash along their facing (if left or right for side scrollers).
    /// </summary>
    public void Dash()
    {
        _dashing.pressed = true;
        _dashing.dashWithDirection = false;
    }

    /// <summary>
    /// Send a direction vector to dash allow dashing in a specific direction.
    /// </summary>
    /// <param name="dir">The normalized direction of the dash.</param>
    public void Dash(Vector2 dir)
    {
        _dashing.pressed = true;
        _dashing.dashWithDirection = true;
        _dashing.dashDir = dir;
    }

    /// <summary>
    /// Call to end dash immediately.
    /// </summary>
    public void EndDash()
    {
        // If dashing then end now.
        if (_dashing.isDashing)
        {
            _dashing.isDashing = false;
            _dashing.canDashAgain = Time.time + dashCooldown;
            rigidbody2D.gravityScale = _originalGravity;

            float normalizedTime = _dashing.timeDashed / dashDuration;

            Vector2 vel = rigidbody2D.velocity;

            vel.x = _dashDerivativeFunction(_dashing.start.x, _dashing.end.x, normalizedTime);

            if (_dashing.dashWithDirection)
            {
                vel.y = _dashDerivativeFunction(_dashing.start.y, _dashing.end.y, normalizedTime);
            }

            rigidbody2D.velocity = vel;

            if (onDashEnd != null)
            {
                onDashEnd();
            }
        }
    }

    #endregion

    #region Private

    private Vector2 _upRight;
    private Vector2 _upLeft;
    private float _originalDrag;
    private float _originalGravity;
    private float _ignoreMovementUntil = 0;
    private Vector2 _velocityBeforeTick;
    private bool _frozen;
    private float _frozenTime;
    private Vector2 _frozenVelocity;
    private float _frozenGravity;

    private Surface _stuckTo = Surface.None;
    private enum Surface
    {
        None,
        Ground,
        LeftWall,
        RightWall,
    }

    // The function is cached to avoid unnecessary memory allocation.
    private EasingFunctions.EasingFunc _dashFunction;
    private EasingFunctions.EasingFunc _dashDerivativeFunction;

    // This is stored to notice if the public field changes during runtime.
    private EasingFunctions.Functions _currentDashEasingFunction;

    // Contains the various jump variables, this is for organization.
    private class JumpState
    {
        public bool isJumping = false;
        public bool pressed = false;
        public bool held = false;
        public bool doubleJumped = false;

        public float timePressed = 0;
        public float timeBuffer = 0.2f; // Amount of time that a jump can be triggered, same as the default unity controller script.
        public float allowExtraDuration;

        public bool force = false;
        public float extraSpeed;
    }
    private JumpState _jumping = new JumpState();

    // Contains the various dash variables.
    private class DashState
    {
        public bool isDashing = false;
        public bool pressed = false;
        public float canDashAgain;
        public float timeDashed;
        public bool dashWithDirection;
        public Vector2 dashDir = Vector2.zero;
        public Vector2 start = Vector2.zero;
        public Vector2 end = Vector2.zero;
    }
    private DashState _dashing = new DashState();

    // Contains information for wall clings, slides, and corner grabs.
    private class WallState
    {
        public bool onCorner = false;
        public float cornerHangTime = 0;

        public bool sliding = false;

        public bool clinging = false;
        public float clingTime = 0;

        public bool canHangAgain = true;
    }
    private WallState _wallInfo = new WallState();

    // This seems to be the magic number where you won't collide with the "ground" by being on the wall and not be able to sit on a corner
    // without colliding with the ground.
    private const float TRIM_STUCKTO_NUM = 0.01f;

    // When jumping off of a wall, this is the amount of time that movement input is ignored.
    private const float IGNORE_INPUT_TIME = 0.2f;

    void Start()
    {
        _upRight = Vector2.up + Vector2.right;
        _upRight.Normalize();
        _upLeft = new Vector2(-_upRight.x, _upRight.y);

        _originalDrag = rigidbody2D.drag;
        _originalGravity = rigidbody2D.gravityScale;

        SetDashFunctions();
    }

    void OnDestroy()
    {
        onDash = null;
        onDashEnd = null;
        onJump = null;
    }

    void OnEnable()
    {
        // The motor does not use drag.
        rigidbody2D.drag = 0;
    }

    void OnDisable()
    {
        rigidbody2D.gravityScale = _originalGravity;
        rigidbody2D.drag = _originalDrag;
    }

    void OnCollisionEnter2D(Collision2D other)
    {
        if (preserveHorizontalMomentumOnLanding && false)
        {
            if (other.contacts[0].point.y < transform.position.y)
            {
                Vector2 vel = rigidbody2D.velocity;
                vel.x = _velocityBeforeTick.x;
                rigidbody2D.velocity = vel;
            }
        }
    }

    void FixedUpdate()
    {
        // Frozen?
        if (frozen)
        {
            return;
        }

        // First, are we trying to dash?
        if (allowDash && _dashing.pressed && Time.time >= _dashing.canDashAgain)
        {
            StartDash();
        }

        _dashing.pressed = false;

        if (_dashing.isDashing)
        {
            // We are dashing.
            HandleDash();
        }
        else
        {
            // If we have standard control then facing can change any frame.
            SetFacing();

            // Are we grounded?
            SetStuckTo();

            if (_stuckTo == Surface.Ground)
            {
                motorState = MotorState.OnGround;

                // Turn off gravity, this prevents losing some velocity on every tick.
                rigidbody2D.gravityScale = 0;
            }
            else
            {
                motorState = MotorState.InAir;
                rigidbody2D.gravityScale = _originalGravity;
            }

            // Apply movement if we're not ignoring it.
            if (Time.time >= _ignoreMovementUntil)
            {
                ApplyMovement();
            }

            // Handle jumping.
            HandleJumping();

            // Finally, any wall interactions.
            HandleWallInteraction();

            // Check speeds.
            ClampVelocity();

            // If we are falling fast then multiply the gravityScale.
            if (motorState == MotorState.InAir)
            {
                if (fallFast)
                {
                    rigidbody2D.gravityScale = _originalGravity * fastFallGravityMultiplier;
                    motorState = MotorState.FallingFast;
                }
                else
                {
                    rigidbody2D.gravityScale = _originalGravity;
                }
            }
        }

        if (Physics2D.gravity != Vector2.zero && rigidbody2D.gravityScale != 0 && motorState == MotorState.InAir)
        {
            // The rigidbody might go to sleep if clinged onto a wall.
            rigidbody2D.WakeUp();
        }

        _velocityBeforeTick = rigidbody2D.velocity;
    }

    private void SetDashFunctions()
    {
        _dashFunction = EasingFunctions.GetEasingFunction(dashEasingFunction);
        _dashDerivativeFunction = EasingFunctions.GetEasingFunctionDerivative(dashEasingFunction);
        _currentDashEasingFunction = dashEasingFunction;
    }

    private void HandleJumping()
    {
        if (_stuckTo != Surface.None && rigidbody2D.velocity.y < 0)
        {
            // If we're grounded then we are not jumping.
            _jumping.isJumping = false;
        }

        // This is something that the default Unity Controller script does, allows the player to press jump button
        // earlier than would normally be allowed. They say it leads to a more pleasant experience for the
        // user. I'll assume they're on to something.
        if (Time.time > _jumping.timePressed + _jumping.timeBuffer)
        {
            _jumping.pressed = false;
        }

        // If we're currently jumping and the jump button is still held down ignore gravity to allow us to acheive the extra height.
        if (_jumping.isJumping && _jumping.held)
        {
            if (_jumping.allowExtraDuration > 0)
            {
                float gravityIgnoredTime = Mathf.Clamp(_jumping.allowExtraDuration, 0f, Time.fixedDeltaTime);
                _jumping.allowExtraDuration -= Time.fixedDeltaTime;

                // TODO: This might want to be based off of the jump direction (in case of wall jumps) which might give a better
                //       feel.
                rigidbody2D.velocity += new Vector2(0, -1 * _originalGravity * Physics2D.gravity.y * gravityIgnoredTime);
            }
        }

        // If our state is not in the air then open up the possibility of double jump (we need to be able to double jump if
        // we walk off an edge so it can't be based of when a jump occured).
        if (motorState != MotorState.InAir)
        {
            _jumping.doubleJumped = false;
        }

        // Jump?
        if (_jumping.pressed)
        {
            bool jumped = true;

            // Jump might mean different things depending on the state.
            if (_stuckTo == Surface.Ground || _jumping.force)
            {
                // Normal jump.
                rigidbody2D.velocity = new Vector2(rigidbody2D.velocity.x, CalculateJumpSpeed() + _jumping.extraSpeed);
            }
            else if (_wallInfo.onCorner)
            {
                // If we are on a corner then jump up.
                rigidbody2D.velocity = new Vector2(rigidbody2D.velocity.x, CalculateJumpSpeed() * cornerJumpMultiplier);
                _ignoreMovementUntil = Time.time + IGNORE_INPUT_TIME;
            }
            else if (allowWallJump && _stuckTo == Surface.LeftWall)
            {
                // If jump was pressed as we or before we entered the wall then just jump away.
                rigidbody2D.velocity = _upRight * CalculateJumpSpeed() * wallJumpMultiplier;

                // It's likely the player is still pressing into the wall, ignore movement for a little amount of time.
                // TODO: Only ignore left movement?
                _ignoreMovementUntil = Time.time + IGNORE_INPUT_TIME;

                // If wall jump is allowed but not wall slide then double jump will not be allowed earlier, allow it now.
                _jumping.doubleJumped = false;
            }
            else if (allowWallJump && _stuckTo == Surface.RightWall)
            {
                rigidbody2D.velocity = _upLeft * CalculateJumpSpeed() * wallJumpMultiplier;
                _ignoreMovementUntil = Time.time + IGNORE_INPUT_TIME;
                _jumping.doubleJumped = false;
            }
            else if (allowDoubleJump && _stuckTo == Surface.None && !_jumping.doubleJumped)
            {
                rigidbody2D.velocity = new Vector2(rigidbody2D.velocity.x, CalculateJumpSpeed());
                _jumping.doubleJumped = true;
            }
            else
            {
                // Guess we aren't jumping!
                jumped = false;
            }

            if (jumped)
            {
                _jumping.isJumping = true;
                _jumping.pressed = false;
                _wallInfo.onCorner = false;
                _wallInfo.sliding = false;
                _wallInfo.clinging = false;
                _jumping.force = false;
                _jumping.allowExtraDuration = extraJumpHeight / CalculateJumpSpeed();
                rigidbody2D.gravityScale = _originalGravity;

                if (onJump != null)
                {
                    onJump();
                }
            }
        }
    }

    private void HandleWallInteraction()
    {
        // We can grab corners or walls again.
        if (_stuckTo != Surface.RightWall && _stuckTo != Surface.LeftWall)
        {
            _wallInfo.canHangAgain = true;
        }

        if (fallFast || _jumping.isJumping)
        {
            // If we're being told to fall fast then we won't allow any wall interactions. Ditto on if we're jumping.
            return;
        }

        // Corner grab?
        if (allowCornerGrab)
        {
            if (rigidbody2D.velocity.y < 0 || _wallInfo.onCorner)
            {
                if ((_stuckTo == Surface.LeftWall && normalizedXMovement < -heavyInputThreshold ||
                    _stuckTo == Surface.RightWall && normalizedXMovement > heavyInputThreshold) &&
                    CheckIfAtCorner())
                {
                    if (!_wallInfo.onCorner && _wallInfo.canHangAgain)
                    {
                        _wallInfo.onCorner = true;
                        _wallInfo.canHangAgain = false;
                        _wallInfo.cornerHangTime = Time.time + cornerGrabDuration;
                    }

                    if (_wallInfo.onCorner && Time.time >= _wallInfo.cornerHangTime)
                    {
                        _wallInfo.onCorner = false;
                    }

                    if (_wallInfo.onCorner)
                    {
                        // We're stuck!
                        rigidbody2D.gravityScale = 0;
                        rigidbody2D.velocity = Vector2.zero;

                        motorState = MotorState.OnCorner;

                        return;
                    }
                }
                else
                {
                    _wallInfo.onCorner = false;
                }
            }
        }

        // Wall Cling
        if (allowWallCling)
        {
            if (rigidbody2D.velocity.y < 0 || _wallInfo.clinging)
            {
                if (_stuckTo == Surface.LeftWall && normalizedXMovement < -heavyInputThreshold ||
                _stuckTo == Surface.RightWall && normalizedXMovement > heavyInputThreshold)
                {
                    if (!_wallInfo.clinging && _wallInfo.canHangAgain)
                    {
                        _wallInfo.clinging = true;
                        _wallInfo.canHangAgain = false;
                        _wallInfo.clingTime = Time.time + wallClingDuration;
                    }

                    if (_wallInfo.clinging && Time.time >= _wallInfo.clingTime)
                    {
                        _wallInfo.clinging = false;
                    }

                    if (_wallInfo.clinging)
                    {
                        // Sticky!
                        rigidbody2D.gravityScale = 0;
                        rigidbody2D.velocity = Vector2.zero;

                        motorState = MotorState.Clinging;

                        return;
                    }
                }
                else
                {
                    _wallInfo.clinging = false;
                }
            }
        }

        // Wall slide?
        if (allowWallSlide)
        {
            if (rigidbody2D.velocity.y < 0 || _wallInfo.sliding)
            {
                _wallInfo.sliding = false;

                // Only if we're currently falling.
                if (_stuckTo == Surface.LeftWall && normalizedXMovement < -heavyInputThreshold ||
                    _stuckTo == Surface.RightWall && normalizedXMovement > heavyInputThreshold)
                {
                    rigidbody2D.gravityScale = 0;
                    rigidbody2D.velocity = -Vector2.up  * wallSlideSpeed;
                    motorState = MotorState.Sliding;

                    _wallInfo.sliding = true;

                    return;
                }
            }
        }
    }

    private void ApplyMovement()
    {
        if (Mathf.Abs(normalizedXMovement) >= inputThreshold)
        {
            if (_stuckTo == Surface.Ground)
            {
                // On ground movement.
                if (timeToMaxGroundSpeed > 0)
                {
                     rigidbody2D.velocity += Vector2.right * normalizedXMovement * (maxGroundSpeed / timeToMaxGroundSpeed) * 
                         Time.fixedDeltaTime;
                }
                else
                {
                    // We can overwrite y if we're on the ground, it's fine.
                    rigidbody2D.velocity = Vector2.right * normalizedXMovement * maxGroundSpeed;
                }
            }
            else
            {
                // Don't apply the force if we're already on the wall.
                if (normalizedXMovement > 0 && _stuckTo == Surface.LeftWall ||
                    normalizedXMovement < 0 && _stuckTo == Surface.RightWall ||
                    _stuckTo == Surface.None)
                {
                    if (timeToMaxAirSpeed > 0)
                    {
                        rigidbody2D.velocity += Vector2.right * normalizedXMovement * (maxAirSpeed / timeToMaxAirSpeed) * 
                            Time.fixedDeltaTime;
                    }
                    else
                    {
                        Vector2 vel = rigidbody2D.velocity;
                        vel.x = normalizedXMovement * maxAirSpeed;
                        rigidbody2D.velocity = vel;
                    }
                }
            }
        }
        else if (rigidbody2D.velocity.x != 0)
        {
            if (_stuckTo == Surface.Ground)
            {
                if (groundStopDistance > 0)
                {
                    Decelerate((maxGroundSpeed * maxGroundSpeed) / (2 * groundStopDistance));
                }
                else
                {
                    rigidbody2D.velocity = Vector2.zero;
                }
            }
            else
            {
                if (airStopDistance > 0)
                {
                    Decelerate((maxAirSpeed * maxAirSpeed) / (2 * airStopDistance));
                }
                else
                {
                    Vector2 vel = rigidbody2D.velocity;
                    vel.x = 0;
                    rigidbody2D.velocity = vel;
                }
            }
        }
    }

    private void Decelerate(float deceleration)
    {
        // Decelerate, do not pass 0.
        Vector3 vel = rigidbody2D.velocity;

        if (vel.x < 0)
        {
            vel.x += deceleration * Time.fixedDeltaTime;

            if (vel.x > 0)
            {
                vel.x = 0;
            }
        }
        else if (vel.x > 0)
        {
            vel.x -= deceleration * Time.fixedDeltaTime;

            if (vel.x < 0)
            {
                vel.x = 0;
            }
        }

        rigidbody2D.velocity = vel;
    }

    private void StartDash()
    {
        _dashing.isDashing = true;

        // Set facing now and it won't be set again during dash.
        SetFacing();

        if (!_dashing.dashWithDirection)
        {
            // We dash depending on our direction.
            _dashing.dashDir = facingLeft ? -Vector2.right : Vector2.right;
        }

        _dashing.start = transform.position;
        _dashing.end = _dashing.start + _dashing.dashDir * dashDistance;

        // This will begin the dash this frame.
        _dashing.timeDashed = Time.fixedDeltaTime;

        rigidbody2D.gravityScale = 0;
        rigidbody2D.velocity = Vector2.zero;
        motorState = MotorState.Dashing;

        if (onDash != null)
        {
            onDash();
        }
    }

    private void HandleDash()
    {
        // Move to the appropriate position.
        Vector2 newPos = transform.position;

        _dashing.timeDashed = Mathf.Clamp(_dashing.timeDashed + Time.fixedDeltaTime, 0f, dashDuration);
        float normalizedTime = _dashing.timeDashed / dashDuration;

        if (_currentDashEasingFunction != dashEasingFunction)
        {
            // This allows the easing function to change during runtime and cut down on unnecessary allocations.
            SetDashFunctions();
        }

        newPos.x = _dashFunction(_dashing.start.x, _dashing.end.x, normalizedTime);

        if (_dashing.dashWithDirection)
        {
            // We only set the y if we are dashing with direction.
            newPos.y = _dashFunction(_dashing.start.y, _dashing.end.y, normalizedTime);
        }

        rigidbody2D.MovePosition(newPos);

        if (_dashing.timeDashed >= dashDuration)
        {
            EndDash();
        }
    }

    private void SetFacing()
    {
        if (normalizedXMovement < -inputThreshold)
        {
            facingLeft = true;
        }
        else if (normalizedXMovement > inputThreshold)
        {
            facingLeft = false;
        }
    }

    private bool CheckIfAtCorner()
    {
        Bounds box;

        if (colliderToUse != null)
        {
            box = colliderToUse.bounds;
        }
        else
        {
            box = collider2D.bounds;
        }

        Vector2 min = box.min;
        Vector2 max = box.max;

        // New min y is always at the current max y.
        min.y = max.y;
        max.y += cornerDistanceCheck;

        if (_stuckTo == Surface.LeftWall)
        {
            max.x = min.x;
            min.x -= cornerDistanceCheck;
        }
        else if (_stuckTo == Surface.RightWall)
        {
            min.x = max.x;
            max.x += cornerDistanceCheck;
        }

        Collider2D col = Physics2D.OverlapArea(min, max, checkMask);

        return col == null;
    }

    private void ClampVelocity()
    {
        Vector2 checkedSpeed = rigidbody2D.velocity;

        if (_stuckTo == Surface.Ground)
        {
            checkedSpeed.x = Mathf.Clamp(checkedSpeed.x, -maxGroundSpeed, maxGroundSpeed);
        }
        else
        {
            // Check both horizontal air speed and fall speed.
            checkedSpeed.x = Mathf.Clamp(checkedSpeed.x, -maxAirSpeed, maxAirSpeed);

            // For y we set the checked speed one tick above gravity to prevent it from making the rigidbody move over our cap.
            float cappedFallSpeed;

            if (fallFast)
            {
                cappedFallSpeed = -maxFastFallSpeed + -Physics2D.gravity.y * rigidbody2D.gravityScale * Time.fixedDeltaTime;

                if (checkedSpeed.y < cappedFallSpeed)
                {
                    checkedSpeed.y = cappedFallSpeed;
                }
            }
            else
            {
                cappedFallSpeed = -maxFallSpeed + -Physics2D.gravity.y * rigidbody2D.gravityScale * Time.fixedDeltaTime;

                if (checkedSpeed.y < cappedFallSpeed)
                {
                    checkedSpeed.y = cappedFallSpeed;
                }
            }
        }

        rigidbody2D.velocity = checkedSpeed;
    }

    private void SetStuckTo()
    {
        Bounds box;

        if (colliderToUse != null)
        {
            box = colliderToUse.bounds;
        }
        else
        {
            box = collider2D.bounds;
        }

        Vector2 min = box.min;
        Vector2 max = box.max;

        min.x += TRIM_STUCKTO_NUM;
        max.x -= TRIM_STUCKTO_NUM;

        min.y -= checkDistance;
        max.y = transform.position.y; // Go ahead and bring the maximum y down.

        // TODO: This requires that a ground layer is set up to work. Consider moving to a set up that will consider all
        //       collisions but ignore the player's collider.

        Collider2D col = Physics2D.OverlapArea(min, max, checkMask);

        _stuckTo = col != null ? Surface.Ground : Surface.None;

        if (_stuckTo == Surface.None)
        {
            // Consider possible stuck to left wall if we're pressing into it.
            if (normalizedXMovement < -inputThreshold)
            {
                // How about on the walls for wall jump? Left wall first.
                min = box.min;
                max = box.max;

                min.y += TRIM_STUCKTO_NUM;
                max.y -= TRIM_STUCKTO_NUM;

                min.x -= checkDistance;
                max.x = transform.position.x;

                col = Physics2D.OverlapArea(min, max, checkMask);

                if (col != null)
                {
                    _stuckTo = Surface.LeftWall;
                }
            }
            else if (normalizedXMovement > inputThreshold)
            {
                // Now right wall.
                min = box.min;
                max = box.max;

                min.y += TRIM_STUCKTO_NUM;
                max.y -= TRIM_STUCKTO_NUM;

                min.x = transform.position.x;
                max.x += checkDistance;

                col = Physics2D.OverlapArea(min, max, checkMask);

                if (col != null)
                {
                    _stuckTo = Surface.RightWall;
                }
            }
        }
    }

    private float CalculateJumpSpeed()
    {
        return Mathf.Sqrt(-2 * baseJumpHeight * _originalGravity * Physics2D.gravity.y);
    }

    void OnDrawGizmosSelected()
    {
        // Ground check.
        Bounds box;
        Vector2 min;
        Vector2 max;

        if (colliderToUse != null)
        {
            box = colliderToUse.bounds;
        }
        else
        {
            box = collider2D.bounds;
        }

        // Ground check box
        min = box.min;
        max = box.max;
        min.x += TRIM_STUCKTO_NUM;
        max.x -= TRIM_STUCKTO_NUM;
        min.y -= checkDistance;
        max.y = transform.position.y;
        Gizmos.DrawWireCube(new Vector2((min.x + max.x) / 2, (min.y + max.y) / 2), new Vector2(max.x - min.x, min.y - max.y));

        if (allowCornerGrab || allowWallJump || allowWallSlide)
        {
            // Left check box
            min = box.min;
            max = box.max;
            min.y += TRIM_STUCKTO_NUM;
            max.y -= TRIM_STUCKTO_NUM;
            min.x -= checkDistance;
            max.x = transform.position.x;
            Gizmos.DrawWireCube(new Vector2((min.x + max.x) / 2, (min.y + max.y) / 2), new Vector2(max.x - min.x, min.y - max.y));

            // Right check box
            min = box.min;
            max = box.max;
            min.y += TRIM_STUCKTO_NUM;
            max.y -= TRIM_STUCKTO_NUM;
            min.x = transform.position.x;
            max.x += checkDistance;
            Gizmos.DrawWireCube(new Vector2((min.x + max.x) / 2, (min.y + max.y) / 2), new Vector2(max.x - min.x, min.y - max.y));
        }

        if (allowCornerGrab)
        {
            min = box.min;
            max = box.max;
            min.y = max.y;
            max.y += cornerDistanceCheck;
            max.x = min.x;
            min.x -= cornerDistanceCheck;
            Gizmos.DrawWireCube(new Vector2((min.x + max.x) / 2, (min.y + max.y) / 2), new Vector2(max.x - min.x, min.y - max.y));

            min = box.min;
            max = box.max;
            min.y = max.y;
            max.y += cornerDistanceCheck;
            min.x = max.x;
            max.x += cornerDistanceCheck;
            Gizmos.DrawWireCube(new Vector2((min.x + max.x) / 2, (min.y + max.y) / 2), new Vector2(max.x - min.x, min.y - max.y));
        }

        // Show the distance that it will take for the motor to stop on the ground and air.
        Vector2 from = new Vector2(box.max.x, box.min.y);
        Gizmos.color = Color.green;
        Gizmos.DrawLine(from, from + Vector2.right * groundStopDistance);

        from = box.max;
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(from, from + Vector2.right * airStopDistance);
    }

    #endregion
}