using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class PetBehaviorComponent : MonoBehaviour
{
    [SerializeField] private GameObject PetUI;

    private OrbContainerComponent _container;

    public bool _canPet = false;

    private GameObject petCreature;


    public void PetTheCreature()
    {
        _canPet = false;
        PetUI.SetActive(false);
        StartCoroutine(petCreature.GetComponent<NEW_IAController>().OnPet());
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Creature") && other.GetComponent<NEW_IAController>().isPacified == true && other.GetComponent<NEW_IAController>().canBePet == true)
        {
            PetUI.SetActive(true);
            petCreature = other.gameObject;
            _canPet = true;
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Creature") && other.GetComponent<NEW_IAController>().isPacified == true && other.GetComponent<NEW_IAController>().canBePet == true)
        {
            PetUI.SetActive(false);
            _canPet = false;
        }
    }
}