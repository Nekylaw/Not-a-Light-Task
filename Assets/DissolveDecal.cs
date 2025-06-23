using System;
using Unity.VisualScripting;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class DissolveDecal : MonoBehaviour
{
    [SerializeField] private DecalProjector m_firstDecalProjector;
    
    [SerializeField] private DecalProjector m_secondDecalProjector;
    
    [SerializeField] private DecalProjector m_thirdDecalProjector;
    
    [SerializeField] private DecalProjector m_fourthDecalProjector;
    
    [SerializeField] private float m_dissolveFirstValue = 1f;
    [SerializeField] private float m_dissolveSecondValue = 1f;
    [SerializeField] private float m_dissolveThirdValue = 1f;
    [SerializeField] private float m_dissolveFourthValue = 1f;
    
    [SerializeField]
    private float m_time = -1f;
    
    public bool m_firstIsPlaying = false;
    public bool m_secondIsPlaying = false;
    public bool m_thirdIsPlaying = false;
    public bool m_fourthIsPlaying = false;
    
    private Material m_firstMaterial;
    private Material m_secondMaterial;
    private Material m_thirdMaterial;
    private Material m_fourthMaterial;

    private ActivatorsService activator;
    
    private void Awake()
    {
        activator = GameObject.FindFirstObjectByType<ActivatorsService>();
    }

    public void Start()
    {
        m_firstMaterial = m_firstDecalProjector.material;
        m_secondMaterial = m_secondDecalProjector.material;
        m_thirdMaterial = m_thirdDecalProjector.material;
        m_fourthMaterial = m_fourthDecalProjector.material;
        
        m_firstMaterial.SetFloat("_OpacityDissolve", m_dissolveFirstValue);
        m_secondMaterial.SetFloat("_OpacityDissolve", m_dissolveSecondValue);
        m_thirdMaterial.SetFloat("_OpacityDissolve", m_dissolveThirdValue);
        m_fourthMaterial.SetFloat("_OpacityDissolve", m_dissolveFourthValue);
    }

    public void Update()
    {

        
        if (m_firstIsPlaying)
        {
            if (m_dissolveFirstValue > m_time)
            {
                m_firstMaterial.SetFloat("_OpacityDissolve", m_dissolveFirstValue);
                m_dissolveFirstValue -= Time.deltaTime;
            }
            
        }

        if (m_secondIsPlaying)
        {
            if (m_dissolveSecondValue > m_time)
            {
                m_secondMaterial.SetFloat("_OpacityDissolve", m_dissolveSecondValue);
                m_dissolveSecondValue -= Time.deltaTime;
            }
        }

        if (m_thirdIsPlaying)
        {
            if (m_dissolveThirdValue > m_time)
            {
                m_thirdMaterial.SetFloat("_OpacityDissolve", m_dissolveSecondValue);
                m_dissolveThirdValue -= Time.deltaTime;
            }
        }

        if (m_fourthIsPlaying)
        {
            if (m_dissolveFourthValue > m_time)
            {
                m_fourthMaterial.SetFloat("_OpacityDissolve", m_dissolveFourthValue);
                m_dissolveFourthValue -= Time.deltaTime;
            }
        }
    }
    
    public void AnimateMat(int index)
    {
        switch (index)
        {
            case 1:
                m_firstIsPlaying = true;
                activator.HandleRevealStela(m_firstMaterial.GetComponent<EnhancedFocusComponent>().stelaIndex);
                break;
            case 2:
                m_secondIsPlaying = true;
                activator.HandleRevealStela(m_secondMaterial.GetComponent<EnhancedFocusComponent>().stelaIndex);
                break;
            case 3:
                m_thirdIsPlaying = true;
                activator.HandleRevealStela(m_thirdMaterial.GetComponent<EnhancedFocusComponent>().stelaIndex);
                break;
            case 4:
                m_fourthIsPlaying = true;
                activator.HandleRevealStela(m_fourthMaterial.GetComponent<EnhancedFocusComponent>().stelaIndex);
                break;
            
        }
        
    }
}
