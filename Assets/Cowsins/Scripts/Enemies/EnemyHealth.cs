/// <summary>
/// This script belongs to cowsins™ as a part of the cowsins´ FPS Engine. All rights reserved. 
/// </summary>
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace cowsins
{
    /// <summary>
    /// Super simple enemy script that allows any object with this component attached to receive damage,aim towards the player and shoot at it.
    /// This is not definitive and it will 100% be modified and re structured in future updates
    /// </summary>
    public partial class EnemyHealth : Identifiable, IDamageable
    {
        [System.Serializable]
        public class Events
        {
            public UnityEvent OnSpawn, OnDamaged, OnDeath;
        }

        [Tooltip("Name of the enemy. This will appear on the killfeed"), SerializeField]
        protected string _name;

        [ReadOnly] public float health;

        [ReadOnly] public float shield;

        [Tooltip("initial enemy health "), SerializeField]
        protected float maxHealth;

        [Tooltip("initial enemy shield"), SerializeField]
        protected float maxShield;

        [Tooltip("When the object dies, decide if it should be destroyed or not."), SerializeField] private bool destroyOnDie;

        [SerializeField] private GameObject deathEffect;

        [Tooltip("display enemy status via UI"), SerializeField]
        public Slider healthSlider, shieldSlider;

        [Tooltip("If true, it will display the UI with the shield and health sliders."), SerializeField]
        private bool showUI;

        public bool showDamagePopUps;

        [Tooltip("If true, it will display the KillFeed UI."), SerializeField]
        protected bool showKillFeed;

        [SerializeField]
        protected AudioClip dieSFX;

        public Events events;

        protected bool isDead;

        public bool DestroyOnDie => destroyOnDie;

        public bool ShowUI => showUI;

        public Slider HealthSlider => healthSlider;

        public Slider ShieldSlider => shieldSlider;

        public virtual void Start()
        {
            // Status initial settings
            health = maxHealth;
            shield = maxShield;

            // Spawn
            events.OnSpawn.Invoke();

            // UI 
            // Determine max values
            if (healthSlider != null) healthSlider.maxValue = maxHealth;
            if (shieldSlider != null) shieldSlider.maxValue = maxShield;
            if (!showUI) // Destroy the enemy UI if we do not want to display it
            {
                Destroy(healthSlider);
                Destroy(shieldSlider);
            }
        }

        // Update is called once per frame
        public virtual void Update()
        {
            //Handle UI 
            if (healthSlider != null) healthSlider.value = Mathf.Lerp(healthSlider.value, health, Time.deltaTime * 6);
            if (shieldSlider != null) shieldSlider.value = Mathf.Lerp(shieldSlider.value, shield, Time.deltaTime * 4);
        }
        /// <summary>
        /// Since it is IDamageable, it can take damage, if a shot is landed, damage the enemy
        /// </summary>
        public virtual void Damage(float _damage, bool isHeadshot)
        {
            float damage = Mathf.Abs(_damage);
            float oldDmg = damage;
            if (damage <= shield) // Shield will be damaged
            {
                shield -= damage;
            }
            else
            {
                damage = damage - shield;
                shield = 0;
                health -= damage;
            }

            // Custom event on damaged
            events.OnDamaged.Invoke();
            UIEvents.onEnemyHit?.Invoke(isHeadshot);
            if(showDamagePopUps) UIEvents.showDamagePopUp?.Invoke(transform.position, oldDmg);
#if SAVE_LOAD_ADD_ON
            SaveInteraction();
#endif
            if (health <= 0 && !isDead) Die();
        }
        public virtual void Die()
        {
            isDead = true;
            // Custom event on damaged
            events.OnDeath.Invoke();

            SoundManager.Instance.PlaySound(dieSFX, 0, 1, false, 0);
            // Does it display killfeed on death? 
            if (showKillFeed)
            {
                UIEvents.onEnemyKilled.Invoke(_name);
            }
            if (deathEffect) Instantiate(deathEffect, transform.position, Quaternion.identity);
            if (destroyOnDie) Destroy(this.gameObject);
        }
    }
#if UNITY_EDITOR
    [System.Serializable]
    [CustomEditor(typeof(EnemyHealth))]
    public class EnemyEditor : Editor
    {
        private bool showIdentity = false;
        private bool showStats = false;
        private bool showUIFoldout = false;
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EnemyHealth myScript = (EnemyHealth)target;

            Texture2D myTexture = Resources.Load<Texture2D>("CustomEditor/enemyHealth_CustomEditor") as Texture2D;
            GUILayout.Label(myTexture);
            EditorGUI.indentLevel++;
            EditorGUILayout.BeginVertical(GUI.skin.GetStyle("HelpBox"));
            {
                // Identity foldout
                showIdentity = EditorGUILayout.Foldout(showIdentity, "IDENTITY", true);
                if (showIdentity)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("_name"));
                    EditorGUILayout.Space(6);
                    EditorGUI.indentLevel--;
                }

            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(6);
            EditorGUILayout.BeginVertical(GUI.skin.GetStyle("HelpBox"));
            {
                // Stats foldout
                showStats = EditorGUILayout.Foldout(showStats, "STATISTICS", true);
                if (showStats)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("maxHealth"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("maxShield"));

                    if (!myScript.DestroyOnDie)
                    {
                        EditorGUILayout.HelpBox("DestroyOnDie is set to false, your object won’t be destroyed once you kill it.", MessageType.Warning);
                    }

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("destroyOnDie"));
                    if (myScript.DestroyOnDie)
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("deathEffect"));
                        EditorGUI.indentLevel--;
                    }
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("dieSFX"));
                    EditorGUILayout.Space(6);
                    EditorGUI.indentLevel--;
                }

            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(6);
            EditorGUILayout.BeginVertical(GUI.skin.GetStyle("HelpBox"));
            {
                showUIFoldout = EditorGUILayout.Foldout(showUIFoldout, "UI", true);
                if (showUIFoldout)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("showUI"));
                    if (myScript.ShowUI)
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("healthSlider"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("shieldSlider"));
                        if (!myScript.HealthSlider && !myScript.ShieldSlider)
                        {
                            EditorGUILayout.Space(10);
                            EditorGUILayout.HelpBox("Your Enemy UI is null, create a new custom Canvas or Create the Default UI", MessageType.Error);
                            EditorGUILayout.Space(5);
                            if (GUILayout.Button("Create Default UI"))
                            {
                                CreateDefaultUI(myScript.transform);
                            }
                            EditorGUILayout.Space(10);
                        }
                        EditorGUI.indentLevel--;
                    }

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("showDamagePopUps"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("showKillFeed"));
                    EditorGUILayout.Space(6);
                    EditorGUI.indentLevel--;
                }


            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(6);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("events"));
            EditorGUI.indentLevel--;


            serializedObject.ApplyModifiedProperties();
        }



        private void CreateDefaultUI(Transform myTransform)
        {
            // Path to EnemyStatusSlider prefab
            string prefabPath = "Assets/Cowsins/Prefabs/Others/UI/EnemyStatusSliders.prefab";

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (prefab != null)
            {
                // Instantiate the prefab 
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, myTransform);
                instance.transform.localPosition += new Vector3(0, 5, 0);
                Undo.RegisterCreatedObjectUndo(instance, "Create Default UI");

                SerializedProperty healthSliderProperty = serializedObject.FindProperty("healthSlider");
                SerializedProperty shieldSliderProperty = serializedObject.FindProperty("shieldSlider");

                healthSliderProperty.objectReferenceValue = instance.transform.Find("HealthSlider").GetComponent<Slider>();
                shieldSliderProperty.objectReferenceValue = instance.transform.Find("ShieldSlider").GetComponent<Slider>();
                serializedObject.ApplyModifiedProperties();

                EditorUtility.SetDirty(target);
            }
            else
            {
                Debug.LogError("<color=red>[COWSINS]</color> Prefab at path <b><color=cyan>{prefabPath}</color></b> " +
                               "could not be found. Did you move, rename or delete EnemyStatusSlider.prefab?");
            }
        }
    }
#endif
}