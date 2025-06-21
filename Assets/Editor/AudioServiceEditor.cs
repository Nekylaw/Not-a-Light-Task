#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using Game.Services.LightSources;
using Unity.VisualScripting;

[CustomEditor(typeof(AudioService))]
public class AudioServiceEditor : Editor
{
    private AudioService audioService;

    // Serialized Properties
    private SerializedProperty lightGroups;
    private SerializedProperty onWalkSound;
    private SerializedProperty onShootSound;
    private SerializedProperty onAimSound;
    private SerializedProperty onPickupSound;
    private SerializedProperty onLampToggleSound;
    private SerializedProperty onPacifyEndSound;
    private SerializedProperty customEventSounds;

    // Reorderable Lists
    private ReorderableList lightGroupsList;
    private ReorderableList customEventsList;

    // Colors
    private readonly Color headerColor = new Color(0.2f, 0.2f, 0.2f);
    private readonly Color activeColor = new Color(0.2f, 0.8f, 0.2f);
    private readonly Color inactiveColor = new Color(0.8f, 0.2f, 0.2f);

    private void OnEnable()
    {
        audioService = (AudioService)target;

        // Get properties
        lightGroups = serializedObject.FindProperty("_lightGroups");
        onWalkSound = serializedObject.FindProperty("_onWalkSound");
        onShootSound = serializedObject.FindProperty("_onShootSound");
        onAimSound = serializedObject.FindProperty("_onAimSound");
        onPickupSound = serializedObject.FindProperty("_onPickupSound");
        onLampToggleSound = serializedObject.FindProperty("_onLampToggleSound");
        onPacifyEndSound = serializedObject.FindProperty("_onPacifyEndSound");
        customEventSounds = serializedObject.FindProperty("_customEventSounds");

        // Setup light groups list
        SetupLightGroupsList();

        // Setup custom events list
        SetupCustomEventsList();
    }

    private void SetupLightGroupsList()
    {
        lightGroupsList = new ReorderableList(serializedObject, lightGroups, true, true, true, true);

        lightGroupsList.drawHeaderCallback = (Rect rect) => {
            EditorGUI.LabelField(rect, "Light Groups", EditorStyles.boldLabel);
        };

        lightGroupsList.elementHeightCallback = (int index) => {
            var element = lightGroups.GetArrayElementAtIndex(index);
            var height = EditorGUIUtility.singleLineHeight * 4 + 10;

            if (element.isExpanded)
            {
                var lights = element.FindPropertyRelative("lightSources");
                height += (EditorGUIUtility.singleLineHeight + 2) * Mathf.Max(1, lights.arraySize) + 40;
            }

            return height;
        };

        lightGroupsList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
            var element = lightGroups.GetArrayElementAtIndex(index);
            var groupName = element.FindPropertyRelative("groupName");
            var lights = element.FindPropertyRelative("lightSources");
            var ambientTrack = element.FindPropertyRelative("ambientTrack");
            var volume = element.FindPropertyRelative("volume");
            var requireAll = element.FindPropertyRelative("requireAllLights");

            rect.y += 2;

            // Header with foldout
            var headerRect = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);
            var foldoutRect = new Rect(rect.x, rect.y, rect.width - 100, EditorGUIUtility.singleLineHeight);
            var statusRect = new Rect(rect.x + rect.width - 90, rect.y, 90, EditorGUIUtility.singleLineHeight);

            element.isExpanded = EditorGUI.Foldout(foldoutRect, element.isExpanded, groupName.stringValue, true);

            // Show status in play mode
            if (Application.isPlaying)
            {
                var group = audioService.GetType()
                    .GetField("_lightGroups", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.GetValue(audioService) as System.Collections.Generic.List<AudioService.LightGroup>;

                if (group != null && index < group.Count)
                {
                    var lightGroup = group[index];
                    EditorGUI.LabelField(statusRect,
                        $"{lightGroup.ActiveCount}/{lightGroup.TotalCount} - {(lightGroup.IsActive ? "ACTIVE" : "INACTIVE")}",
                        lightGroup.IsActive ? EditorStyles.boldLabel : EditorStyles.miniLabel);
                }
            }

            rect.y += EditorGUIUtility.singleLineHeight + 2;

            // Basic info always visible
            EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight),
                groupName, new GUIContent("Group Name"));
            rect.y += EditorGUIUtility.singleLineHeight + 2;

            EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight),
                ambientTrack, new GUIContent("Ambient Track"));
            rect.y += EditorGUIUtility.singleLineHeight + 2;

            // Volume and require all on same line
            var halfWidth = rect.width / 2 - 5;
            EditorGUI.PropertyField(new Rect(rect.x, rect.y, halfWidth, EditorGUIUtility.singleLineHeight),
                volume, new GUIContent("Volume"));
            EditorGUI.PropertyField(new Rect(rect.x + halfWidth + 10, rect.y, halfWidth, EditorGUIUtility.singleLineHeight),
                requireAll, new GUIContent("Require All"));
            rect.y += EditorGUIUtility.singleLineHeight + 5;

            // Expanded content
            if (element.isExpanded)
            {
                EditorGUI.indentLevel++;

                // Light sources header
                EditorGUI.LabelField(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight),
                    $"Light Sources ({lights.arraySize})", EditorStyles.boldLabel);
                rect.y += EditorGUIUtility.singleLineHeight + 2;

                // Drop area for adding lights
                var dropArea = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight * Mathf.Max(1, lights.arraySize) + 20);
                GUI.Box(dropArea, "Drop Light Sources Here", EditorStyles.helpBox);

                // Handle drag and drop
                Event evt = Event.current;
                if (dropArea.Contains(evt.mousePosition))
                {
                    if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                        if (evt.type == EventType.DragPerform)
                        {
                            DragAndDrop.AcceptDrag();

                            foreach (UnityEngine.Object obj in DragAndDrop.objectReferences)
                            {
                                GameObject go = obj as GameObject;
                                if (go != null)
                                {
                                    var lightSource = go.GetComponent<LightSourceComponent>();
                                    if (lightSource != null)
                                    {
                                        lights.arraySize++;
                                        var newElement = lights.GetArrayElementAtIndex(lights.arraySize - 1);
                                        newElement.objectReferenceValue = lightSource;
                                    }
                                }
                            }

                            serializedObject.ApplyModifiedProperties();
                        }

                        evt.Use();
                    }
                }

                // Draw light sources
                for (int i = 0; i < lights.arraySize; i++)
                {
                    var lightRect = new Rect(rect.x + 10, rect.y + 5 + (EditorGUIUtility.singleLineHeight + 2) * i,
                        rect.width - 40, EditorGUIUtility.singleLineHeight);
                    var deleteRect = new Rect(lightRect.x + lightRect.width + 5, lightRect.y, 20, EditorGUIUtility.singleLineHeight);

                    EditorGUI.PropertyField(lightRect, lights.GetArrayElementAtIndex(i), GUIContent.none);

                    if (GUI.Button(deleteRect, "X"))
                    {
                        lights.DeleteArrayElementAtIndex(i);
                    }
                }

                EditorGUI.indentLevel--;
            }
        };

        lightGroupsList.onAddCallback = (ReorderableList list) => {
            lightGroups.arraySize++;
            var newElement = lightGroups.GetArrayElementAtIndex(lightGroups.arraySize - 1);
            newElement.FindPropertyRelative("groupName").stringValue = $"Light Group {lightGroups.arraySize}";
            newElement.FindPropertyRelative("volume").floatValue = 1f;
            newElement.FindPropertyRelative("requireAllLights").boolValue = true;
            newElement.isExpanded = true;
        };
    }

    private void SetupCustomEventsList()
    {
        customEventsList = new ReorderableList(serializedObject, customEventSounds, true, true, true, true);

        customEventsList.drawHeaderCallback = (Rect rect) => {
            EditorGUI.LabelField(rect, "Custom Events", EditorStyles.boldLabel);
        };

        customEventsList.elementHeightCallback = (int index) => {
            return EditorGUIUtility.singleLineHeight * 3 + 10;
        };

        customEventsList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
            var element = customEventSounds.GetArrayElementAtIndex(index);
            var eventName = element.FindPropertyRelative("eventName");
            var sound = element.FindPropertyRelative("soundToPlay");
            var volume = element.FindPropertyRelative("volume");

            rect.y += 2;

            EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight),
                eventName, new GUIContent("Event Name"));
            rect.y += EditorGUIUtility.singleLineHeight + 2;

            EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight),
                sound, new GUIContent("Sound"));
            rect.y += EditorGUIUtility.singleLineHeight + 2;

            EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight),
                volume, new GUIContent("Volume"));
        };
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Header
        EditorGUILayout.Space();
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.Label("Audio Service Configuration", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        EditorGUILayout.Space();

        // Toolbar
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Find All Lights", EditorStyles.toolbarButton))
        {
            audioService.SendMessage("FindAllLightSources");
        }
        if (GUILayout.Button("Validate Config", EditorStyles.toolbarButton))
        {
            audioService.SendMessage("ValidateConfiguration");
        }
        if (Application.isPlaying && GUILayout.Button("Test All Sounds", EditorStyles.toolbarButton))
        {
            audioService.SendMessage("TestAllSounds");
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // Light Groups Section
        audioService.ShowLightGroups = EditorGUILayout.BeginFoldoutHeaderGroup(audioService.ShowLightGroups, "Light Groups");
        if (audioService.ShowLightGroups)
        {
            EditorGUILayout.BeginVertical("box");
            lightGroupsList.DoLayoutList();
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        EditorGUILayout.Space();

        // Movement Events
        audioService.ShowMovementEvents = EditorGUILayout.BeginFoldoutHeaderGroup(audioService.ShowMovementEvents, "Movement Events");
        if (audioService.ShowMovementEvents)
        {
            EditorGUILayout.BeginVertical("box");
            DrawEventSound(onWalkSound, "_movement.OnWalk");
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        // Gun Events
        audioService.ShowGunEvents = EditorGUILayout.BeginFoldoutHeaderGroup(audioService.ShowGunEvents, "Gun Events");
        if (audioService.ShowGunEvents)
        {
            EditorGUILayout.BeginVertical("box");
            DrawEventSound(onShootSound, "_shoot.OnShoot");
            DrawEventSound(onAimSound, "_shoot.OnAim");
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        // Interaction Events
        audioService.ShowAbilityEvents = EditorGUILayout.BeginFoldoutHeaderGroup(audioService.ShowAbilityEvents, "Ability Events");
        if (audioService.ShowAbilityEvents)
        {
            EditorGUILayout.BeginVertical("box");
            DrawEventSound(onPickupSound, "_pickUp.OnPickup");
            DrawEventSound(onLampToggleSound, "_lightService.OnSwitchLight");
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        // Other Events
        audioService.ShowOtherEvents = EditorGUILayout.BeginFoldoutHeaderGroup(audioService.ShowOtherEvents, "Other Events");
        if (audioService.ShowOtherEvents)
        {
            EditorGUILayout.BeginVertical("box");
            DrawEventSound(onPacifyEndSound, "_pacify.OnPacifyEnd");
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        // Custom Events
        audioService.ShowCustomEvents = EditorGUILayout.BeginFoldoutHeaderGroup(audioService.ShowCustomEvents, "Custom Events");
        if (audioService.ShowCustomEvents)
        {
            EditorGUILayout.BeginVertical("box");
            customEventsList.DoLayoutList();
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawEventSound(SerializedProperty eventSound, string eventPath)
    {
        var expanded = eventSound.FindPropertyRelative("isExpanded");
        var eventName = eventSound.FindPropertyRelative("eventName");
        var sound = eventSound.FindPropertyRelative("soundToPlay");
        var volume = eventSound.FindPropertyRelative("volume");
        var playAtPos = eventSound.FindPropertyRelative("playAtPosition");
        var notes = eventSound.FindPropertyRelative("notes");

        EditorGUILayout.BeginVertical("box");

        // Header
        EditorGUILayout.BeginHorizontal();
        expanded.boolValue = EditorGUILayout.Foldout(expanded.boolValue, eventName.stringValue, true);

        // Show event path
        GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField(eventPath, EditorStyles.miniLabel, GUILayout.Width(150));

        // Test button in play mode
        if (Application.isPlaying && GUILayout.Button("Test", GUILayout.Width(40)))
        {
            Debug.Log($"Testing sound: {eventName.stringValue}");
            // Simple test play
            if (!sound.FindPropertyRelative("Guid").stringValue.Equals(""))
            {
                FMODUnity.RuntimeManager.PlayOneShot(sound.FindPropertyRelative("Path").stringValue);
            }
        }

        EditorGUILayout.EndHorizontal();

        // Expanded content
        if (expanded.boolValue)
        {
            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(sound, new GUIContent("Sound To Play"));

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(volume, new GUIContent("Volume"), GUILayout.Width(EditorGUIUtility.labelWidth + 100));
            GUILayout.FlexibleSpace();
            EditorGUILayout.PropertyField(playAtPos, new GUIContent("3D Position"));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(notes, new GUIContent("Notes"));

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(2);
    }
}

// Property Drawer for EventSound when shown in arrays
[CustomPropertyDrawer(typeof(AudioService.EventSound))]
public class EventSoundDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        var eventName = property.FindPropertyRelative("eventName");
        var sound = property.FindPropertyRelative("soundToPlay");

        // Draw event name as label
        position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive),
            new GUIContent(eventName.stringValue));

        // Draw sound field
        EditorGUI.PropertyField(position, sound, GUIContent.none);

        EditorGUI.EndProperty();
    }
}

// Helper window for debugging
public class AudioServiceDebugWindow : EditorWindow
{
    private AudioService audioService;
    private Vector2 scrollPos;

    [MenuItem("Window/Audio/Audio Service Debug")]
    public static void ShowWindow()
    {
        GetWindow<AudioServiceDebugWindow>("Audio Debug");
    }

    private void OnEnable()
    {
        audioService = FindFirstObjectByType<AudioService>();
    }

    private void OnGUI()
    {
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Debug window only works in Play Mode", MessageType.Info);
            return;
        }

        if (audioService == null)
        {
            EditorGUILayout.HelpBox("No AudioService found in scene", MessageType.Warning);
            if (GUILayout.Button("Create AudioService"))
            {
                audioService = AudioService.Instance;
            }
            return;
        }

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        EditorGUILayout.LabelField("Audio Service Status", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Show light groups status
        EditorGUILayout.LabelField("Light Groups:", EditorStyles.boldLabel);
        var lightGroups = audioService.GetType()
            .GetField("_lightGroups", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.GetValue(audioService) as System.Collections.Generic.List<AudioService.LightGroup>;

        if (lightGroups != null)
        {
            foreach (var group in lightGroups)
            {
                EditorGUILayout.BeginHorizontal("box");

                // Status indicator
                var statusColor = group.IsActive ? Color.green : Color.red;
                var oldColor = GUI.backgroundColor;
                GUI.backgroundColor = statusColor;
                GUILayout.Box("", GUILayout.Width(20), GUILayout.Height(20));
                GUI.backgroundColor = oldColor;

                EditorGUILayout.LabelField(group.groupName);
                EditorGUILayout.LabelField($"{group.ActiveCount}/{group.TotalCount}", GUILayout.Width(50));

                if (GUILayout.Button("Toggle All", GUILayout.Width(80)))
                {
                    foreach (var light in group.lightSources)
                    {
                        if (light != null)
                        {
                            // Toggle light logic here
                        }
                    }
                }

                EditorGUILayout.EndHorizontal();

                // Show individual lights
                if (group.showDebugInfo)
                {
                    EditorGUI.indentLevel++;
                    foreach (var light in group.lightSources)
                    {
                        if (light != null)
                        {
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField($"  - {light.name}", light.IsLightOn ? EditorStyles.boldLabel : EditorStyles.label);
                            EditorGUILayout.LabelField(light.IsLightOn ? "ON" : "OFF", GUILayout.Width(40));
                            EditorGUILayout.EndHorizontal();
                        }
                    }
                    EditorGUI.indentLevel--;
                }
            }
        }

        EditorGUILayout.Space();

        // Test buttons for events
        EditorGUILayout.LabelField("Test Events:", EditorStyles.boldLabel);

        if (GUILayout.Button("Trigger Walk Event"))
        {
            var movement = FindFirstObjectByType<MovementBehaviorComponent>();
            if (movement != null)
            {
                // Trigger walk event
                Debug.Log("Walk event triggered from debug window");
            }
        }

        if (GUILayout.Button("Trigger Shoot Event"))
        {
            var shoot = FindFirstObjectByType<ShootBehaviorComponent>();
            if (shoot != null)
            {
                // Trigger shoot event
                Debug.Log("Shoot event triggered from debug window");
            }
        }

        EditorGUILayout.EndScrollView();

        // Repaint in play mode
        if (Application.isPlaying)
        {
            Repaint();
        }
    }
}
#endif