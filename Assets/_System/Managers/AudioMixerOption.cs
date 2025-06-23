using UnityEngine;
using FMOD;
using FMODUnity;
using Unity.VisualScripting;
using FMOD.Studio;

public class AudioMixerOption : MonoBehaviour
{
    FMOD.Studio.VCA MasterVCA;
    FMOD.Studio.VCA SFXVCA;
    FMOD.Studio.VCA MusicVCA;

    private void Awake()
    {
        MasterVCA = FMODUnity.RuntimeManager.GetVCA("vca:/Master");
        MusicVCA = FMODUnity.RuntimeManager.GetVCA("vca:/Music");
        SFXVCA = FMODUnity.RuntimeManager.GetVCA("vca:/SFX");

        if (!MasterVCA.isValid()) UnityEngine.Debug.LogWarning("Master VCA invalide !");
        if (!MusicVCA.isValid()) UnityEngine.Debug.LogWarning("Music VCA invalide !");
        if (!SFXVCA.isValid()) UnityEngine.Debug.LogWarning("SFX VCA invalide !");

    }
    public void MasterVolumeControl(float value)
    {
        MasterVCA.setVolume(value / 10);
    }
    public void SFXVolumeControl(float value)
    {
        SFXVCA.setVolume(value / 10);
    }
    public void MusicVolumeControl(float value)
    {
        MusicVCA.setVolume(value / 10);
    }
}
