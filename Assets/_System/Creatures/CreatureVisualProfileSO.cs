using UnityEngine;

/// <summary>
/// Represents a visual profile for creatures. Colors and other visual properties can be defined for different states of the creature.
/// </summary>
[CreateAssetMenu(fileName = "CreatureVisualProfile", menuName = "Game/Creature/Creature Visual Profile")]
public class CreatureVisualProfileSO : ScriptableObject
{
    [Header("Global Settings")]
    public float baseColorMultiplier = 3f;
    public float bodyAlpha = 0.8f;

    [Header("State Profiles")]
    public StateProfile NormalProfile = new StateProfile
    {
        baseColor = new Color(0.529f, 0.808f, 0.922f),
        rimColor = Color.white,
        rimIntensity = 2f,
        eyesColor = new Color(1f, 1f, 0.3f),
        eyesIntensity = 3f,
        heartColor = new Color(1f, 0.8f, 0.2f),
        heartIntensity = 8f,
        heartBeatBaseSpeed = 1.5f,
        heartBeatExcitedSpeed = 4f
    };

    public StateProfile ExcitedProfile = new StateProfile
    {
        baseColor = new Color(1f, 0.4f, 0.3f),
        rimColor = new Color(1f, 0.5f, 0f),
        rimIntensity = 3f,
        eyesColor = new Color(1f, 0.5f, 0f),
        eyesIntensity = 5f,
        heartColor = new Color(1f, 0.3f, 0.1f),
        heartIntensity = 12f,
        heartBeatBaseSpeed = 2f,
        heartBeatExcitedSpeed = 6f
    };

    public StateProfile EatingProfile = new StateProfile
    {
        baseColor = new Color(1f, 0.6f, 0.8f),
        rimColor = new Color(1f, 0.8f, 0.9f),
        rimIntensity = 2.5f,
        eyesColor = new Color(1f, 0.9f, 0.5f),
        eyesIntensity = 4f,
        heartColor = new Color(1f, 1f, 0.5f),
        heartIntensity = 10f,
        heartBeatBaseSpeed = 3f,
        heartBeatExcitedSpeed = 8f
    };

    public StateProfile DrainingProfile = new StateProfile
    {
        baseColor = new Color(0.8f, 0.3f, 1f),
        rimColor = new Color(0.8f, 0.4f, 1f),
        rimIntensity = 3.5f,
        eyesColor = new Color(0.9f, 0.3f, 1f),
        eyesIntensity = 6f,
        heartColor = new Color(1f, 0f, 1f),
        heartIntensity = 14f,
        heartBeatBaseSpeed = 4f,
        heartBeatExcitedSpeed = 10f
};

    public StateProfile PacifiedProfile = new StateProfile
    {
        baseColor = new Color(0.5f, 1f, 0.5f),
        rimColor = new Color(0.7f, 1f, 0.7f),
        rimIntensity = 2f,
        eyesColor = new Color(0.5f, 1f, 0.7f),
        eyesIntensity = 3f,
        heartColor = new Color(0.4f, 1f, 0.6f),
        heartIntensity = 6f,
        heartBeatBaseSpeed = 1f,
        heartBeatExcitedSpeed = 2f
    };

    [System.Serializable]
    public struct StateProfile
    {
        [Header("Body")]
        public Color baseColor;

        [Header("Rim Lighting")]
        public Color rimColor;
        [Range(0f, 5f)]
        public float rimIntensity;

        [Header("Eyes")]
        public Color eyesColor;
        [Range(0f, 10f)]
        public float eyesIntensity;

        [Header("Heart")]
        public Color heartColor;
        [Range(0f, 15f)]
        public float heartIntensity;
        [Range(0.5f, 10f)]
        public float heartBeatBaseSpeed;
        [Range(1f, 15f)]
        public float heartBeatExcitedSpeed;

        /// <summary>
        /// Lerps between profiles
        /// </summary>
        public static StateProfile Lerp(StateProfile a, StateProfile b, float t)
        {
            return new StateProfile
            {
                baseColor = Color.Lerp(a.baseColor, b.baseColor, t),
                rimColor = Color.Lerp(a.rimColor, b.rimColor, t),
                rimIntensity = Mathf.Lerp(a.rimIntensity, b.rimIntensity, t),
                eyesColor = Color.Lerp(a.eyesColor, b.eyesColor, t),
                eyesIntensity = Mathf.Lerp(a.eyesIntensity, b.eyesIntensity, t),
                heartColor = Color.Lerp(a.heartColor, b.heartColor, t),
                heartIntensity = Mathf.Lerp(a.heartIntensity, b.heartIntensity, t),
                heartBeatBaseSpeed = Mathf.Lerp(a.heartBeatBaseSpeed, b.heartBeatBaseSpeed, t),
                heartBeatExcitedSpeed = Mathf.Lerp(a.heartBeatExcitedSpeed, b.heartBeatExcitedSpeed, t)
            };
        }
    }

    private void OnValidate()
    {
        baseColorMultiplier = Mathf.Max(0.1f, baseColorMultiplier);
        bodyAlpha = Mathf.Clamp01(bodyAlpha);
    }
}