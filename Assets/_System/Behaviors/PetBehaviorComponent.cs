using Unity.VisualScripting;
using UnityEngine;

public class PetBehaviorComponent : MonoBehaviour
{
    [SerializeField] private GameObject PetUI;

    private OrbContainerComponent _container;

    public bool _canPet = false;

    private GameObject PetCreature;


    public void PetTheCreature()
    {
        _canPet = false;
        PetUI.SetActive(false);
        PetCreature.gameObject.GetComponent<NEW_IAController>().canWander = false;
        PetCreature.gameObject.GetComponent<NEW_IAController>().CanBePet = false;
        StartCoroutine(PetCreature.GetComponent<NEW_IAController>().OnPet());
        PetManager.Instance.ChoosePet(false);

        //faire apparaitre une orbe

        //jouer l'anim
        //petit effet de particules (style capture pokemon ?)
        //changer la couleur de la boule de la créature ? 

    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Creature") && other.GetComponent<NEW_IAController>().isPacified == true && other.GetComponent<NEW_IAController>().CanBePet == true)
        {
            PetUI.SetActive(true);
            PetCreature = other.gameObject;
            _canPet = true;
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Creature") && other.GetComponent<NEW_IAController>().isPacified == true && other.GetComponent<NEW_IAController>().CanBePet == true)
        {
            PetUI.SetActive(false);
            _canPet = false;
        }
    }
}