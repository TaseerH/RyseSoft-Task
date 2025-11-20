// CHANGE LOG
// 
// CHANGES || version VERSION
//
// "Enable/Disable Headbob, Changed look rotations - should result in reduced camera jitters" || version 1.0.1

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Cinemachine;
using UnityEditor;

[RequireComponent(typeof(Rigidbody))]
public class FirstPersonController : MonoBehaviour
{
    private Rigidbody rb;

    #region Cinemachine
    [Header("=== CINEMACHINE ===")]
    public CinemachineVirtualCamera virtualCamera;
    private CinemachinePOV pov;
    #endregion

    #region Camera Movement Variables
    public float fov = 60f;
    public bool invertCamera = false;
    public bool cameraCanMove = true;
    public float mouseSensitivity = 2f;
    public float maxLookAngle = 50f;

    // Crosshair
    public bool lockCursor = true;
    public bool crosshair = true;
    public Sprite crosshairImage;
    public Color crosshairColor = Color.white;

    // Internal
    private Image crosshairObject;

    #region Camera Zoom Variables
    public bool enableZoom = true;
    public bool holdToZoom = false;
    public KeyCode zoomKey = KeyCode.Mouse1;
    public float zoomFOV = 30f;
    public float zoomStepTime = 5f;
    private bool isZoomed = false;
    private float defaultFOV;
    #endregion
    #endregion

    #region Movement Variables
    public bool playerCanMove = true;
    public float walkSpeed = 5f;
    public float maxVelocityChange = 10f;
    private bool isWalking = false;

    #region Sprint
    public bool enableSprint = true;
    public bool unlimitedSprint = false;
    public KeyCode sprintKey = KeyCode.LeftShift;
    public float sprintSpeed = 7f;
    public float sprintDuration = 5f;
    public float sprintCooldown = .5f;
    public float sprintFOV = 80f;
    public float sprintFOVStepTime = 10f;

    public bool useSprintBar = true;
    public bool hideBarWhenFull = true;
    public Image sprintBarBG;
    public Image sprintBar;
    public float sprintBarWidthPercent = .3f;
    public float sprintBarHeightPercent = .015f;

    private CanvasGroup sprintBarCG;
    private bool isSprinting = false;
    private float sprintRemaining;
    private float sprintBarWidth;
    private float sprintBarHeight;
    private bool isSprintCooldown = false;
    private float sprintCooldownReset;
    #endregion

    #region Jump
    public bool enableJump = true;
    public KeyCode jumpKey = KeyCode.Space;
    public float jumpPower = 5f;
    private bool isGrounded = false;
    #endregion

    #region Crouch
    public bool enableCrouch = true;
    public bool holdToCrouch = true;
    public KeyCode crouchKey = KeyCode.LeftControl;
    public float crouchHeight = .75f;
    public float speedReduction = .5f;
    private bool isCrouched = false;
    private Vector3 originalScale;
    #endregion
    #endregion

    #region Head Bob
    public bool enableHeadBob = true;
    public Transform joint; // This is now your "head" or camera holder for bobbing
    public float bobSpeed = 10f;
    public Vector3 bobAmount = new Vector3(.15f, .05f, 0f);
    private Vector3 jointOriginalPos;
    private float timer = 0;
    #endregion

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // Get Cinemachine POV component
        if (virtualCamera != null)
        {
            pov = virtualCamera.GetCinemachineComponent<CinemachinePOV>();
            if (pov == null)
            {
                Debug.LogError("Cinemachine Virtual Camera must have a POV component for first-person!");
            }
        }
        else
        {
            Debug.LogError("Please assign a Cinemachine Virtual Camera in the inspector!");
        }

        crosshairObject = GetComponentInChildren<Image>();
        originalScale = transform.localScale;
        jointOriginalPos = joint.localPosition;

        if (!unlimitedSprint)
        {
            sprintRemaining = sprintDuration;
            sprintCooldownReset = sprintCooldown;
        }

        defaultFOV = fov;
    }

    void Start()
    {
        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (crosshair)
        {
            crosshairObject.sprite = crosshairImage;
            crosshairObject.color = crosshairColor;
        }/*
        else
        {
            crosshairObject.gameObject.SetActive(false);
        }*/

        #region Sprint Bar Setup
        sprintBarCG = GetComponentInChildren<CanvasGroup>();
        if (useSprintBar)
        {
            //sprintBarBG.gameObject.SetActive(true);
            //sprintBar.gameObject.SetActive(true);

            float screenWidth = Screen.width;
            float screenHeight = Screen.height;
            sprintBarWidth = screenWidth * sprintBarWidthPercent;
            sprintBarHeight = screenHeight * sprintBarHeightPercent;

            sprintBarBG.rectTransform.sizeDelta = new Vector3(sprintBarWidth, sprintBarHeight, 0f);
            sprintBar.rectTransform.sizeDelta = new Vector3(sprintBarWidth - 2, sprintBarHeight - 2, 0f);

            if (hideBarWhenFull)
                sprintBarCG.alpha = 0;
        }
        else
        {
            //sprintBarBG.gameObject.SetActive(false);
            //sprintBar.gameObject.SetActive(false);
        }
        #endregion

        // Set initial FOV
        if (virtualCamera != null)
            virtualCamera.m_Lens.FieldOfView = fov;
    }

    private void Update()
    {
        if (virtualCamera == null || pov == null) return;

        #region Camera Sensitivity & Inversion
        if (cameraCanMove)
        {
            pov.m_HorizontalAxis.m_MaxSpeed = mouseSensitivity * 100f;
            pov.m_VerticalAxis.m_MaxSpeed = mouseSensitivity * 100f;
            pov.m_VerticalAxis.m_InvertInput = invertCamera;
            pov.m_VerticalAxis.m_MinValue = -maxLookAngle;
            pov.m_VerticalAxis.m_MaxValue = maxLookAngle;
            pov.m_VerticalAxis.m_Wrap = false;
        }
        else
        {
            pov.m_HorizontalAxis.m_MaxSpeed = 0;
            pov.m_VerticalAxis.m_MaxSpeed = 0;
        }
        #endregion

        #region Camera Zoom
        if (enableZoom && !isSprinting)
        {
            if (!holdToZoom && Input.GetKeyDown(zoomKey))
                isZoomed = !isZoomed;

            if (holdToZoom)
                isZoomed = Input.GetKey(zoomKey);

            float targetFOV = isZoomed ? zoomFOV : defaultFOV;
            virtualCamera.m_Lens.FieldOfView = Mathf.Lerp(
                virtualCamera.m_Lens.FieldOfView,
                targetFOV,
                zoomStepTime * Time.deltaTime
            );
        }
        #endregion

        #region Sprint FOV
        if (isSprinting)
        {
            isZoomed = false;
            virtualCamera.m_Lens.FieldOfView = Mathf.Lerp(
                virtualCamera.m_Lens.FieldOfView,
                sprintFOV,
                sprintFOVStepTime * Time.deltaTime
            );
        }
        else if (!isZoomed && !isSprinting)
        {
            virtualCamera.m_Lens.FieldOfView = Mathf.Lerp(
                virtualCamera.m_Lens.FieldOfView,
                defaultFOV,
                zoomStepTime * Time.deltaTime
            );
        }
        #endregion

        #region Sprint Logic
        if (enableSprint)
        {
            if (isSprinting && !unlimitedSprint)
            {
                sprintRemaining -= Time.deltaTime;
                if (sprintRemaining <= 0)
                {
                    isSprinting = false;
                    isSprintCooldown = true;
                }
            }
            else if (!isSprinting)
            {
                sprintRemaining = Mathf.Clamp(sprintRemaining + Time.deltaTime, 0, sprintDuration);
            }

            if (isSprintCooldown)
            {
                sprintCooldown -= Time.deltaTime;
                if (sprintCooldown <= 0)
                    isSprintCooldown = false;
            }
            else
            {
                sprintCooldown = sprintCooldownReset;
            }

            if (useSprintBar && !unlimitedSprint)
            {
                float percent = sprintRemaining / sprintDuration;
                sprintBar.transform.localScale = new Vector3(percent, 1f, 1f);
            }
        }
        #endregion

        #region Input Handling
        if (enableJump && Input.GetKeyDown(jumpKey) && isGrounded)
            Jump();

        if (enableCrouch)
        {
            if (!holdToCrouch && Input.GetKeyDown(crouchKey))
                Crouch();

            if (holdToCrouch)
            {
                if (Input.GetKeyDown(crouchKey)) { isCrouched = false; Crouch(); }
                if (Input.GetKeyUp(crouchKey)) { isCrouched = true; Crouch(); }
            }
        }
        #endregion

        CheckGround();
        if (enableHeadBob)
            HeadBob();
    }

    void FixedUpdate()
    {
        if (!playerCanMove) return;

        Vector3 input = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
        Vector3 targetVelocity = transform.TransformDirection(input.normalized);

        bool moving = input.magnitude > 0.1f;
        isWalking = moving && isGrounded;

        float currentSpeed = walkSpeed;

        if (enableSprint && Input.GetKey(sprintKey) && sprintRemaining > 0f && !isSprintCooldown && moving)
        {
            currentSpeed = sprintSpeed;
            isSprinting = true;

            if (isCrouched) Crouch();
            if (hideBarWhenFull && !unlimitedSprint)
                sprintBarCG.alpha = Mathf.MoveTowards(sprintBarCG.alpha, 1f, 5f * Time.deltaTime);
        }
        else
        {
            isSprinting = false;
            if (hideBarWhenFull && sprintRemaining >= sprintDuration)
                sprintBarCG.alpha = Mathf.MoveTowards(sprintBarCG.alpha, 0f, 3f * Time.deltaTime);
        }

        targetVelocity *= currentSpeed;

        Vector3 velocityChange = targetVelocity - rb.velocity;
        velocityChange.x = Mathf.Clamp(velocityChange.x, -maxVelocityChange, maxVelocityChange);
        velocityChange.z = Mathf.Clamp(velocityChange.z, -maxVelocityChange, maxVelocityChange);
        velocityChange.y = 0;

        rb.AddForce(velocityChange, ForceMode.VelocityChange);
    }

    private void CheckGround()
    {
        Vector3 origin = transform.position + Vector3.up * 0.1f;
        isGrounded = Physics.Raycast(origin, Vector3.down, 0.2f);
    }

    private void Jump()
    {
        if (isGrounded)
        {
            rb.AddForce(Vector3.up * jumpPower, ForceMode.Impulse);
            if (isCrouched && !holdToCrouch)
                Crouch();
        }
    }

    private void Crouch()
    {
        if (isCrouched)
        {
            transform.localScale = originalScale;
            walkSpeed /= speedReduction;
        }
        else
        {
            transform.localScale = new Vector3(originalScale.x, crouchHeight, originalScale.z);
            walkSpeed *= speedReduction;
        }
        isCrouched = !isCrouched;
    }

    private void HeadBob()
    {
        if (!isWalking)
        {
            timer = 0;
            joint.localPosition = Vector3.Lerp(joint.localPosition, jointOriginalPos, Time.deltaTime * bobSpeed);
            return;
        }

        float speedMultiplier = isSprinting ? (bobSpeed + sprintSpeed) :
                                isCrouched ? (bobSpeed * speedReduction) : bobSpeed;

        timer += Time.deltaTime * speedMultiplier;

        joint.localPosition = jointOriginalPos + new Vector3(
            Mathf.Sin(timer) * bobAmount.x,
            Mathf.Sin(timer * 2) * bobAmount.y, // Use *2 for vertical bounce feel
            0
        );
    }
}

#if UNITY_EDITOR

[CustomEditor(typeof(FirstPersonController))]
public class FirstPersonControllerEditor : Editor
{
    FirstPersonController fpc;
    SerializedObject serFPC;

    private void OnEnable()
    {
        fpc = (FirstPersonController)target;
        serFPC = new SerializedObject(fpc);
    }

    public override void OnInspectorGUI()
    {
        serFPC.Update();

        EditorGUILayout.Space();
        GUILayout.Label("Modular First Person Controller", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 16 });
        GUILayout.Label("By Jess Case • Cinemachine Edition", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Normal, fontSize = 12 });
        GUILayout.Label("version 2.0 (Cinemachine)", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Italic, fontSize = 11 });
        EditorGUILayout.Space();

        // ===================================================================
        // CINEMACHINE CAMERA SETUP
        // ===================================================================
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        GUILayout.Label("Cinemachine Camera Setup", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 13 });
        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(serFPC.FindProperty("virtualCamera"), new GUIContent("Cinemachine Virtual Camera", "The Cinemachine Virtual Camera that controls view. Must have a POV component."));

        // Warn if missing
        if (fpc.virtualCamera == null)
            EditorGUILayout.HelpBox("Assign a Cinemachine Virtual Camera with a POV aimer!", MessageType.Error);
        else if (fpc.virtualCamera.GetCinemachineComponent<CinemachinePOV>() == null)
            EditorGUILayout.HelpBox("Virtual Camera is missing a POV component! Add CinemachinePOV for first-person control.", MessageType.Warning);

        fpc.fov = EditorGUILayout.Slider(new GUIContent("Default Field of View", "Base FOV when not zooming or sprinting."), fpc.fov, 30f, 130f);
        fpc.cameraCanMove = EditorGUILayout.ToggleLeft(new GUIContent("Enable Camera Rotation", "Allows mouse look via Cinemachine POV."), fpc.cameraCanMove);

        GUI.enabled = fpc.cameraCanMove;
        fpc.invertCamera = EditorGUILayout.ToggleLeft(new GUIContent("Invert Y Axis", "Inverts vertical mouse look."), fpc.invertCamera);
        fpc.mouseSensitivity = EditorGUILayout.Slider(new GUIContent("Look Sensitivity", "Higher = faster camera rotation."), fpc.mouseSensitivity, 0.1f, 10f);
        fpc.maxLookAngle = EditorGUILayout.Slider(new GUIContent("Max Vertical Look Angle", "Clamps up/down look (0° = horizon)."), fpc.maxLookAngle, 30f, 89f);
        GUI.enabled = true;

        fpc.lockCursor = EditorGUILayout.ToggleLeft(new GUIContent("Lock & Hide Cursor", "Locks cursor to center in play mode."), fpc.lockCursor);

        // Crosshair
        fpc.crosshair = EditorGUILayout.ToggleLeft(new GUIContent("Auto Crosshair", "Shows a simple UI crosshair in the center."), fpc.crosshair);
        if (fpc.crosshair)
        {
            EditorGUI.indentLevel++;
            fpc.crosshairImage = (Sprite)EditorGUILayout.ObjectField(new GUIContent("Crosshair Sprite"), fpc.crosshairImage, typeof(Sprite), false);
            fpc.crosshairColor = EditorGUILayout.ColorField(new GUIContent("Crosshair Color"), fpc.crosshairColor);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();

        // Zoom Settings
        GUILayout.Label("Zoom", EditorStyles.boldLabel);
        fpc.enableZoom = EditorGUILayout.ToggleLeft(new GUIContent("Enable Zoom", "Allows aiming/zooming in."), fpc.enableZoom);

        GUI.enabled = fpc.enableZoom;
        fpc.holdToZoom = EditorGUILayout.ToggleLeft(new GUIContent("Hold to Zoom", "Hold key instead of toggle."), fpc.holdToZoom);
        fpc.zoomKey = (KeyCode)EditorGUILayout.EnumPopup(new GUIContent("Zoom Key"), fpc.zoomKey);
        fpc.zoomFOV = EditorGUILayout.Slider(new GUIContent("Zoom FOV", "Field of view when zoomed."), fpc.zoomFOV, 5f, fpc.fov);
        fpc.zoomStepTime = EditorGUILayout.Slider(new GUIContent("Zoom Smoothness", "How fast FOV transitions."), fpc.zoomStepTime, 1f, 20f);
        GUI.enabled = true;

        // ===================================================================
        // MOVEMENT
        // ===================================================================
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        GUILayout.Label("Movement Setup", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 13 });
        EditorGUILayout.Space();

        fpc.playerCanMove = EditorGUILayout.ToggleLeft(new GUIContent("Enable Player Movement"), fpc.playerCanMove);
        GUI.enabled = fpc.playerCanMove;
        fpc.walkSpeed = EditorGUILayout.Slider(new GUIContent("Walk Speed"), fpc.walkSpeed, 1f, fpc.sprintSpeed);
        GUI.enabled = true;

        // ===================================================================
        // SPRINT
        // ===================================================================
        EditorGUILayout.Space();
        GUILayout.Label("Sprint", EditorStyles.boldLabel);

        fpc.enableSprint = EditorGUILayout.ToggleLeft(new GUIContent("Enable Sprint"), fpc.enableSprint);
        GUI.enabled = fpc.enableSprint;

        fpc.unlimitedSprint = EditorGUILayout.ToggleLeft(new GUIContent("Unlimited Sprint"), fpc.unlimitedSprint);
        fpc.sprintKey = (KeyCode)EditorGUILayout.EnumPopup(new GUIContent("Sprint Key"), fpc.sprintKey);
        fpc.sprintSpeed = EditorGUILayout.Slider(new GUIContent("Sprint Speed"), fpc.sprintSpeed, fpc.walkSpeed, 25f);

        EditorGUILayout.PropertyField(serFPC.FindProperty("sprintDuration"), new GUIContent("Sprint Duration (sec)"));
        EditorGUILayout.PropertyField(serFPC.FindProperty("sprintCooldown"), new GUIContent("Sprint Cooldown (sec)"));

        fpc.sprintFOV = EditorGUILayout.Slider(new GUIContent("Sprint FOV"), fpc.sprintFOV, fpc.fov, 140f);
        fpc.sprintFOVStepTime = EditorGUILayout.Slider(new GUIContent("Sprint FOV Smoothness"), fpc.sprintFOVStepTime, 1f, 30f);

        fpc.useSprintBar = EditorGUILayout.ToggleLeft(new GUIContent("Show Sprint Bar"), fpc.useSprintBar);
        if (fpc.useSprintBar)
        {
            EditorGUI.indentLevel++;
            fpc.hideBarWhenFull = EditorGUILayout.ToggleLeft(new GUIContent("Hide When Full"), fpc.hideBarWhenFull);

            fpc.sprintBarBG = (Image)EditorGUILayout.ObjectField("Bar Background (Image)", fpc.sprintBarBG, typeof(Image), true);
            fpc.sprintBar = (Image)EditorGUILayout.ObjectField("Bar Fill (Image)", fpc.sprintBar, typeof(Image), true);

            fpc.sprintBarWidthPercent = EditorGUILayout.Slider("Bar Width (% of screen)", fpc.sprintBarWidthPercent, 0.1f, 0.6f);
            fpc.sprintBarHeightPercent = EditorGUILayout.Slider("Bar Height (% of screen)", fpc.sprintBarHeightPercent, 0.005f, 0.05f);
            EditorGUI.indentLevel--;
        }
        GUI.enabled = true;

        // ===================================================================
        // JUMP
        // ===================================================================
        EditorGUILayout.Space();
        GUILayout.Label("Jump", EditorStyles.boldLabel);

        fpc.enableJump = EditorGUILayout.ToggleLeft(new GUIContent("Enable Jump"), fpc.enableJump);
        GUI.enabled = fpc.enableJump;
        fpc.jumpKey = (KeyCode)EditorGUILayout.EnumPopup("Jump Key", fpc.jumpKey);
        fpc.jumpPower = EditorGUILayout.Slider("Jump Power", fpc.jumpPower, 1f, 20f);
        GUI.enabled = true;

        // ===================================================================
        // CROUCH
        // ===================================================================
        EditorGUILayout.Space();
        GUILayout.Label("Crouch", EditorStyles.boldLabel);

        fpc.enableCrouch = EditorGUILayout.ToggleLeft(new GUIContent("Enable Crouch"), fpc.enableCrouch);
        GUI.enabled = fpc.enableCrouch;
        fpc.holdToCrouch = EditorGUILayout.ToggleLeft(new GUIContent("Hold to Crouch"), fpc.holdToCrouch);
        fpc.crouchKey = (KeyCode)EditorGUILayout.EnumPopup("Crouch Key", fpc.crouchKey);
        fpc.crouchHeight = EditorGUILayout.Slider("Crouch Height Scale", fpc.crouchHeight, 0.3f, 1f);
        fpc.speedReduction = EditorGUILayout.Slider("Speed Reduction Multiplier", fpc.speedReduction, 0.1f, 1f);
        GUI.enabled = true;

        // ===================================================================
        // HEAD BOB
        // ===================================================================
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        GUILayout.Label("Head Bob Setup", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 13 });
        EditorGUILayout.Space();

        fpc.enableHeadBob = EditorGUILayout.ToggleLeft(new GUIContent("Enable Head Bob", "Bobs the camera/joint while walking."), fpc.enableHeadBob);

        GUI.enabled = fpc.enableHeadBob;
        fpc.joint = (Transform)EditorGUILayout.ObjectField(new GUIContent("Head Bob Joint", "Transform that will bob (usually a child of the camera or follow target)."), fpc.joint, typeof(Transform), true);
        fpc.bobSpeed = EditorGUILayout.Slider("Bob Speed", fpc.bobSpeed, 2f, 25f);
        fpc.bobAmount = EditorGUILayout.Vector3Field("Bob Amount (X/Y/Z)", fpc.bobAmount);
        GUI.enabled = true;

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Tip: For best results, set Cinemachine Virtual Camera → Follow & Look At → the same object as 'Head Bob Joint'.", MessageType.Info);

        // Apply changes
        if (GUI.changed)
        {
            Undo.RecordObject(fpc, "FPC Change");
            EditorUtility.SetDirty(fpc);
            serFPC.ApplyModifiedProperties();
        }
    }
}
#endif