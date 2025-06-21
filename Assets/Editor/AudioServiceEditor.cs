#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using Game.Services.LightSources;
using System.ComponentModel;
using UnityEngine.Rendering.Universal;

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

        lightGroupsList.drawHeaderCallback = (Rect rect) =>
        {
            EditorGUI.LabelField(rect, "Light Groups", EditorStyles.boldLabel);
        };

        lightGroupsList.elementHeightCallback = (int index) =>
        {
            var element = lightGroups.GetArrayElementAtIndex(index);
            float height = EditorGUIUtility.singleLineHeight * 5 + 20; // Base height

            if (element.isExpanded)
            {
                // Add height for progressive settings
                height += EditorGUIUtility.singleLineHeight * 4 + 20;

                // Add height for light sources
                var lights = element.FindPropertyRelative("lightSources");
                int lightCount = Mathf.Max(3, lights.arraySize); // Minimum 3 slots for drop area
                height += (EditorGUIUtility.singleLineHeight + 4) * lightCount;

                var ambiantTrack = element.FindPropertyRelative("ambientTrack");
                height += ambiantTrack.isExpanded ?
                    EditorGUIUtility.singleLineHeight * 8 :
                    EditorGUIUtility.singleLineHeight;
            }

            return height + 10;
        };

        lightGroupsList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            var element = lightGroups.GetArrayElementAtIndex(index);
            var groupName = element.FindPropertyRelative("groupName");
            var lights = element.FindPropertyRelative("lightSources");
            var ambientTrack = element.FindPropertyRelative("ambientTrack");
            var targetVolume = element.FindPropertyRelative("targetVolume");
            var fadeInTime = element.FindPropertyRelative("fadeInTime");
            var fadeOutTime = element.FindPropertyRelative("fadeOutTime");
            var requireAll = element.FindPropertyRelative("requireAllLights");
            var startOnAwake = element.FindPropertyRelative("startOnAwake");
            var muteWhenInactive = element.FindPropertyRelative("muteWhenInactive");

            rect.y += 2;

            // Background box for the entire element
            GUI.Box(new Rect(rect.x - 2, rect.y - 2, rect.width + 4, rect.height - 4), GUIContent.none);

            // Header with foldout
            var foldoutRect = new Rect(rect.x + 10, rect.y, rect.width - 120, EditorGUIUtility.singleLineHeight);
            var statusRect = new Rect(rect.x + rect.width - 100, rect.y, 100, EditorGUIUtility.singleLineHeight);

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
                    var volumeText = lightGroup.IsInitialized ? $"Vol: {lightGroup.CurrentVolume:F2}" : "Not Init";
                    EditorGUI.LabelField(statusRect,
                        $"{lightGroup.ActiveCount}/{lightGroup.TotalCount} | {volumeText}",
                        lightGroup.IsActive ? EditorStyles.boldLabel : EditorStyles.miniLabel);
                }
            }

            rect.y += EditorGUIUtility.singleLineHeight + 5;

            // Indent content
            rect.x += 15;
            rect.width -= 15;

            // Basic info always visible
            EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width - 10, EditorGUIUtility.singleLineHeight),
                groupName, new GUIContent("Group Name"));
            rect.y += EditorGUIUtility.singleLineHeight + 3;

            EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width - 10, EditorGUIUtility.singleLineHeight),
                ambientTrack, new GUIContent("Ambient Track"));

            float yOffset = ambientTrack.isExpanded ? EditorGUIUtility.singleLineHeight * 8 : EditorGUIUtility.singleLineHeight;
            rect.y += EditorGUIUtility.singleLineHeight + yOffset;

            // Progressive settings section
            EditorGUI.LabelField(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight),
                "Progressive Settings", EditorStyles.boldLabel);

            rect.y += EditorGUIUtility.singleLineHeight + 3;

            var halfWidth = (rect.width - 20) / 2;
            EditorGUI.PropertyField(new Rect(rect.x, rect.y, halfWidth, EditorGUIUtility.singleLineHeight),
                targetVolume, new GUIContent("Target Volume"));
            EditorGUI.PropertyField(new Rect(rect.x + halfWidth + 10, rect.y, halfWidth, EditorGUIUtility.singleLineHeight),
                requireAll, new GUIContent("Require All"));
            rect.y += EditorGUIUtility.singleLineHeight + 8;

            // Expanded content
            if (element.isExpanded)
            {
                EditorGUI.PropertyField(new Rect(rect.x, rect.y, halfWidth, EditorGUIUtility.singleLineHeight),
                    fadeInTime, new GUIContent("Fade In Time"));
                EditorGUI.PropertyField(new Rect(rect.x + halfWidth + 10, rect.y, halfWidth, EditorGUIUtility.singleLineHeight),
                    fadeOutTime, new GUIContent("Fade Out Time"));
                rect.y += EditorGUIUtility.singleLineHeight + 3;

                EditorGUI.PropertyField(new Rect(rect.x, rect.y, halfWidth, EditorGUIUtility.singleLineHeight),
                    startOnAwake, new GUIContent("Start On Awake"));
                EditorGUI.PropertyField(new Rect(rect.x + halfWidth + 10, rect.y, halfWidth, EditorGUIUtility.singleLineHeight),
                    muteWhenInactive, new GUIContent("Mute When Inactive"));
                rect.y += EditorGUIUtility.singleLineHeight + 8;

                // Light sources section
                EditorGUI.LabelField(new Rect(rect.x, rect.y, rect.width - 100, EditorGUIUtility.singleLineHeight),
                    $"Light Sources ({lights.arraySize})", EditorStyles.boldLabel);

                // Add button
                if (GUI.Button(new Rect(rect.x + rect.width - 90, rect.y, 80, EditorGUIUtility.singleLineHeight), "+ Add Slot"))
                {
                    lights.arraySize++;
                }
                rect.y += EditorGUIUtility.singleLineHeight + 5;

                // Light sources list with drop area
                int displayCount = Mathf.Max(3, lights.arraySize);
                var dropAreaHeight = (EditorGUIUtility.singleLineHeight + 4) * displayCount;
                var dropArea = new Rect(rect.x, rect.y, rect.width - 10, dropAreaHeight);

                // Draw drop area background
                GUI.Box(dropArea, lights.arraySize == 0 ? "Drop Light Sources Here" : "", EditorStyles.helpBox);

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
                                        // Check if already in list
                                        bool alreadyExists = false;
                                        for (int i = 0; i < lights.arraySize; i++)
                                        {
                                            if (lights.GetArrayElementAtIndex(i).objectReferenceValue == lightSource)
                                            {
                                                alreadyExists = true;
                                                break;
                                            }
                                        }

                                        if (!alreadyExists)
                                        {
                                            lights.arraySize++;
                                            var newElement = lights.GetArrayElementAtIndex(lights.arraySize - 1);
                                            newElement.objectReferenceValue = lightSource;
                                        }
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
                    var lightRect = new Rect(rect.x + 5, rect.y + 5 + (EditorGUIUtility.singleLineHeight + 4) * i,
                        rect.width - 50, EditorGUIUtility.singleLineHeight);
                    var deleteRect = new Rect(lightRect.x + lightRect.width + 5, lightRect.y, 30, EditorGUIUtility.singleLineHeight);

                    EditorGUI.PropertyField(lightRect, lights.GetArrayElementAtIndex(i), GUIContent.none);

                    if (GUI.Button(deleteRect, "X", EditorStyles.miniButton))
                    {
                        lights.DeleteArrayElementAtIndex(i);
                        // If the element was a reference, we might need to delete twice
                        if (i < lights.arraySize && lights.GetArrayElementAtIndex(i).objectReferenceValue == null)
                        {
                            lights.DeleteArrayElementAtIndex(i);
                        }
                    }
                }
            }
        };

        lightGroupsList.onAddCallback = (ReorderableList list) =>
        {
            lightGroups.arraySize++;
            var newElement = lightGroups.GetArrayElementAtIndex(lightGroups.arraySize - 1);
            newElement.FindPropertyRelative("groupName").stringValue = $"Light Group {lightGroups.arraySize}";
            newElement.FindPropertyRelative("targetVolume").floatValue = 1f;
            newElement.FindPropertyRelative("fadeInTime").floatValue = 2f;
            newElement.FindPropertyRelative("fadeOutTime").floatValue = 1f;
            newElement.FindPropertyRelative("requireAllLights").boolValue = true;
            newElement.FindPropertyRelative("startOnAwake").boolValue = true;
            newElement.FindPropertyRelative("muteWhenInactive").boolValue = true;
            newElement.isExpanded = true;
        };
    }

    private void SetupCustomEventsList()
    {
        customEventsList = new ReorderableList(serializedObject, customEventSounds, true, true, true, true);

        customEventsList.drawHeaderCallback = (Rect rect) =>
        {
            EditorGUI.LabelField(rect, "Custom Events", EditorStyles.boldLabel);
        };

        customEventsList.elementHeightCallback = (int index) =>
        {
            return EditorGUIUtility.singleLineHeight * 3 + 10;
        };

        customEventsList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            var element = customEventSounds.GetArrayElementAtIndex(index);
            var eventName = element.FindPropertyRelative("eventName");
            var sound = element.FindPropertyRelative("soundToPlay");
            var volume = element.FindPropertyRelative("targetVolume");

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
        GUILayout.Label("🔊 Audio Service Configuration", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        EditorGUILayout.Space();

        // Toolbar
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        if (GUILayout.Button("Find All Lights", EditorStyles.toolbarButton))
        {
            audioService.SendMessage("FindAllLightSources");
        }
        if (GUILayout.Button("Validate Config", EditorStyles.toolbarButton))
        {
            audioService.SendMessage("ValidateConfiguration");
        }
        if (Application.isPlaying)
        {
            if (GUILayout.Button("Test All Sounds", EditorStyles.toolbarButton))
            {
                audioService.SendMessage("TestAllSounds");
            }
            if (GUILayout.Button("Initialize Groups", EditorStyles.toolbarButton))
            {
                audioService.SendMessage("InitializeAllGroupsEditor");
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // Light Groups Section
        audioService.showLightGroups = EditorGUILayout.BeginFoldoutHeaderGroup(audioService.showLightGroups, "💡 Light Groups");
        if (audioService.showLightGroups)
        {
            EditorGUILayout.BeginVertical("box");
            lightGroupsList.DoLayoutList();
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        EditorGUILayout.Space();

        // Movement Events
        audioService.showMovementEvents = EditorGUILayout.BeginFoldoutHeaderGroup(audioService.showMovementEvents, "🚶 Movement Events");
        if (audioService.showMovementEvents)
        {
            EditorGUILayout.BeginVertical("box");
            DrawEventSound(onWalkSound, "_movement.OnWalk");
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        // Combat Events
        audioService.showCombatEvents = EditorGUILayout.BeginFoldoutHeaderGroup(audioService.showCombatEvents, "🔫 Gun Events");
        if (audioService.showCombatEvents)
        {
            EditorGUILayout.BeginVertical("box");
            DrawEventSound(onShootSound, "_shoot.OnShoot");
            DrawEventSound(onAimSound, "_shoot.OnAim");
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        // Interaction Events
        audioService.showInteractionEvents = EditorGUILayout.BeginFoldoutHeaderGroup(audioService.showInteractionEvents, "✋ Interaction Events");
        if (audioService.showInteractionEvents)
        {
            EditorGUILayout.BeginVertical("box");
            DrawEventSound(onPickupSound, "_pickUp.OnPickup");
            DrawEventSound(onLampToggleSound, "_lightService.OnSwitchLight");
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        // Ability Events
        audioService.showOtherEvents = EditorGUILayout.BeginFoldoutHeaderGroup(audioService.showOtherEvents, "🎮 Ability Events");
        if (audioService.showOtherEvents)
        {
            EditorGUILayout.BeginVertical("box");
            DrawEventSound(onPacifyEndSound, "_pacify.OnPacifyEnd");
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        // Custom Events
        audioService.showCustomEvents = EditorGUILayout.BeginFoldoutHeaderGroup(audioService.showCustomEvents, "⚡ Custom Events");
        if (audioService.showCustomEvents)
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
        var isLooped = eventSound.FindPropertyRelative("isLooped");
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
            // Try to play the sound
            audioService.PlaySound((FMODUnity.EventReference)sound.boxedValue);
        }

        EditorGUILayout.EndHorizontal();

        // Expanded content
        if (expanded.boolValue)
        {
            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(sound, new GUIContent("Sound To Play"));
            EditorGUILayout.PropertyField(isLooped, new GUIContent("Is looped"), GUILayout.Width(EditorGUIUtility.labelWidth + 100));

            EditorGUILayout.PropertyField(volume, new GUIContent("Volume"), GUILayout.Width(EditorGUIUtility.labelWidth + 100));
            EditorGUILayout.PropertyField(playAtPos, new GUIContent("3D Position"));

            EditorGUILayout.PropertyField(notes, new GUIContent("Notes"));

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(2);
    }
}
#endif