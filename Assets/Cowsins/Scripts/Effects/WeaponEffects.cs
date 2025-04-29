/// <summary>
/// This script belongs to cowsins� as a part of the cowsins� FPS Engine. All rights reserved. 
/// </summary>
using UnityEngine;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif
namespace cowsins
{
    public class WeaponEffects : MonoBehaviour
    {
        #region shared
        [SerializeField] private Transform gunsEffectsTransform, jumpMotionTransform;

        private PlayerMovement playerMovement;

        private WeaponController weaponController;

        private Rigidbody rb;

        #endregion
        
        #region bob
        [SerializeField] private Vector3 rotationMultiplier;

        [SerializeField] private float translationSpeed;

        [SerializeField] private float rotationSpeed;

        [SerializeField] private Vector3 movementLimit;

        [SerializeField] private Vector3 bobLimit;

        [SerializeField] private float aimingMultiplier;

        // New variables for inclination multipliers
        [SerializeField] private float horizontalInclineMultiplier = 1f;
        [SerializeField] private float forwardInclineMultiplier = 1f;

        private float bobSin { get => Mathf.Sin(bobSpeed); }
        private float bobCos { get => Mathf.Cos(bobSpeed); }

        private float bobSpeed;

        private Vector3 bobPos;

        private Vector3 bobRot;
        #endregion
        #region jumpMotion
        [SerializeField] private AnimationCurve jumpMotion, groundedMotion;

        [SerializeField] private float jumpMotionDistance, jumpMotionRotationAmount;

        [SerializeField, Min(1)] private float evaluationSpeed;

        private Coroutine jumpCoroutine;

        #endregion

        private void Start()
        {
            playerMovement = GetComponent<PlayerMovement>();
            weaponController = GetComponent<WeaponController>();
            rb = GetComponent<Rigidbody>();

            playerMovement.events.OnJump.AddListener(OnJump);
            playerMovement.events.OnLand.AddListener(OnLand);
        }

        private void OnJump()
        {
            if (jumpCoroutine != null) StopCoroutine(jumpCoroutine);
            jumpCoroutine = StartCoroutine(ApplyMotion(jumpMotion));
        }

        private void OnLand()
        {
            if (jumpCoroutine != null) StopCoroutine(jumpCoroutine);
            jumpCoroutine = StartCoroutine(ApplyMotion(groundedMotion));
        }

        private IEnumerator ApplyMotion(AnimationCurve motionCurve)
        {
            float motion = 0;

            while (motion < 1f)
            {
                motion += Time.deltaTime * evaluationSpeed;
                float evaluatedMotion = motionCurve.Evaluate(motion);

                jumpMotionTransform.localPosition = Vector3.Lerp(jumpMotionTransform.localPosition, new Vector3(0, evaluatedMotion, 0) * jumpMotionDistance, motion);
                jumpMotionTransform.localRotation = Quaternion.Lerp(jumpMotionTransform.localRotation, Quaternion.Euler(new Vector3(evaluatedMotion * jumpMotionRotationAmount, 0, 0)), motion);

                yield return null;
            }
        }

        private void Update()
        {
            if (!playerMovement.grounded && !playerMovement.wallRunning) return;
            DetailedBob();
        }

        private void DetailedBob()
        {
            bobSpeed += Time.deltaTime * (playerMovement.grounded ? rb.linearVelocity.magnitude / 2 : 1) + .01f;
            float mult = weaponController.isAiming ? aimingMultiplier : 1;

            bobPos.x = (bobCos * bobLimit.x * (playerMovement.grounded || playerMovement.wallRunning ? 1 : 0)) - (InputManager.x * movementLimit.x);
            bobPos.y = (bobSin * bobLimit.y) - (rb.linearVelocity.y * movementLimit.y);
            bobPos.z = -InputManager.y * movementLimit.z;

            gunsEffectsTransform.localPosition = Vector3.Lerp(gunsEffectsTransform.localPosition, bobPos * mult, Time.deltaTime * translationSpeed);

            bobRot.x = InputManager.x != 0 ? rotationMultiplier.x * Mathf.Sin(2 * bobSpeed) : rotationMultiplier.x * Mathf.Sin(2 * bobSpeed) / 2;
            bobRot.y = InputManager.x != 0 ? rotationMultiplier.y * bobCos : 0;
            bobRot.z = InputManager.x != 0 ? rotationMultiplier.z * bobCos * InputManager.x : 0;

            // New rotation adjustments for horizontal and forward/backward inclines
            if (InputManager.x != 0)
            {
                bobRot.z += horizontalInclineMultiplier * InputManager.x;  // Horizontal incline
            }
            if (InputManager.y != 0)
            {
                bobRot.x += forwardInclineMultiplier * InputManager.y;  // Forward/backward incline
            }

            gunsEffectsTransform.localRotation = Quaternion.Slerp(gunsEffectsTransform.localRotation, Quaternion.Euler(bobRot * mult), Time.deltaTime * rotationSpeed);
        }

    }
#if UNITY_EDITOR
    [CustomEditor(typeof(WeaponEffects))]
    public class WeaponEffectsEditor : Editor
    {
        override public void OnInspectorGUI()
        {
            serializedObject.Update();
            var myScript = target as WeaponEffects;

            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("WEAPON BOB SETTINGS", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("gunsEffectsTransform"));

            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("rotationMultiplier"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("translationSpeed"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("rotationSpeed"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("movementLimit"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("bobLimit"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("aimingMultiplier"));

            // Add fields for the new inclination multipliers
            EditorGUILayout.PropertyField(serializedObject.FindProperty("horizontalInclineMultiplier"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("forwardInclineMultiplier"));

            EditorGUI.indentLevel--;


            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("JUMP MOTION SETTINGS", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("jumpMotionTransform"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("jumpMotion"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("groundedMotion"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("jumpMotionDistance"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("jumpMotionRotationAmount"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("evaluationSpeed"));
            EditorGUILayout.Space(5f);

            EditorGUILayout.LabelField("WEAPON SWAY SETTINGS", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Weapon Sway can be modified in each Weapon Prefab ( Root of the Weapon Prefab )", EditorStyles.helpBox);
            EditorGUILayout.Space(5f);
            if (GUILayout.Button("Go to Weapon Prefabs Folder"))
            {
                string folderPath = "Assets/Cowsins/ScriptableObjects/Weapons";
                EditorUtility.FocusProjectWindow();
                Object folder = AssetDatabase.LoadAssetAtPath<Object>(folderPath);
                if (folder != null)
                {
                    EditorGUIUtility.PingObject(folder);
                    Selection.activeObject = folder;
                    EditorUtility.FocusProjectWindow();
                }
                else
                {
                    Debug.LogError($"Folder is empty or not found: {folderPath}. Did you remove or rename the Weapons Folder?");
                }
            }
            EditorGUILayout.Space(5f);
            serializedObject.ApplyModifiedProperties();

        }
    }
#endif
}
