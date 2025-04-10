using UnityEditor;
using UnityEditorInternal;

using UnityEngine;

public class ServicesOrdererWindow : EditorWindow
{
    #region Fields

    private const string WindowName = "Services Orderer";

    /// <summary>
    /// Serialized representation of <see cref="GameManager"/>
    /// </summary>
    private SerializedObject _serializedGameManager;

    /// <summary>
    /// Services order list serialized property 
    /// </summary>
    private SerializedProperty _servicesOrderProperty;

    /// <summary>
    /// Force init flag serialized property 
    /// </summary>
    private SerializedProperty _forceInitServicesProperty;
    
    private ReorderableList _serviceOrderReorderableList;

    #endregion


    #region Lifecycle

    private void OnEnable()
    {
        GameManager gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager == null)
        {
            Debug.LogError($"{nameof(GameManager)} component not found in scene!", this);
            return;
        }

        _serializedGameManager = new SerializedObject(gameManager);
        _servicesOrderProperty = _serializedGameManager.FindProperty("_servicesOrder");
        _forceInitServicesProperty = _serializedGameManager.FindProperty("_forceInitServices");

        if (_servicesOrderProperty == null || _forceInitServicesProperty == null)
        {
            Debug.LogError($"Serialized property/ies not found", this);
            return;
        }

        // Reordorable list
        _serviceOrderReorderableList = new ReorderableList(_serializedGameManager, _servicesOrderProperty, true, true, true, true)
        {
            drawHeaderCallback = rect => EditorGUI.LabelField(rect, ""),

            drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                SerializedProperty element = _servicesOrderProperty.GetArrayElementAtIndex(index);
                rect.y += 2;

                //Get index color
                Color indexColor = GetIndexColor(index);

                // Draw index rect
                Rect indexRect = new Rect(rect.x, rect.y, 20, EditorGUIUtility.singleLineHeight);
                // Style
                GUIStyle fieldStyle = new GUIStyle()
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = Color.black }
                };

                EditorGUI.DrawRect(indexRect, indexColor); // Color is background color
                EditorGUI.LabelField(indexRect, index.ToString(), fieldStyle);

                // field content
                Rect fieldRect = new Rect(rect.x + 25, rect.y, rect.width - 25, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(fieldRect, element, GUIContent.none);
            },

            onChangedCallback = list =>
            {
                _serializedGameManager.ApplyModifiedProperties();
                Debug.Log("Change services order !");
            }
        };
    }

    #endregion


    #region Public API

    [MenuItem("Window/Game/" + WindowName)]
    public static void ShowWindow()
    {
        ServicesOrdererWindow window = GetWindow<ServicesOrdererWindow>(WindowName);
        window.Show();
    }

    #endregion


    #region Private API

    /// <summary>
    /// Renders window content
    /// </summary>
    private void OnGUI()
    {
        if (_serializedGameManager == null)
        {
            EditorGUILayout.LabelField($"{nameof(GameManager)} component not found in scene!");
            return;
        }

        _serializedGameManager.Update();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Service Initialization Settings", EditorStyles.boldLabel);
        _forceInitServicesProperty.boolValue = EditorGUILayout.Toggle("Force Init Services", _forceInitServicesProperty.boolValue);

        _serviceOrderReorderableList.DoLayoutList();
        
        _serializedGameManager.ApplyModifiedProperties();
    }

    /// <summary>
    /// Generates a color depending on a list index.
    /// </summary>
    /// <param name="index"></param>
    /// <returns>Returns the index color.</returns>
    private Color GetIndexColor(int index)
    {
        Color[] colors = { Color.magenta, Color.yellow, Color.cyan, Color.green, Color.red, Color.blue, Color.gray };
        return colors[index % colors.Length];
    }

    #endregion

}
