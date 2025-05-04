using UnityEngine;
using UnityEngine.UI;

public class InGameUI : MonoBehaviour
{
    public int selectedWeapon = 0;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SelectWeapon();
    }

    // Update is called once per frame
    void Update()
    {
        int previousSelectedWeapon = selectedWeapon;
        if (Input.GetAxis("Mouse ScrollWheel") > 0f || Input.GetKeyDown(KeyCode.Keypad1)|| Input.GetKeyDown(KeyCode.Alpha1))
        {
            if (selectedWeapon >= transform.childCount - 1)
            {
                selectedWeapon = 0;
            }
            else
            {
                selectedWeapon++;
            }
            

        }

        if (Input.GetAxis("Mouse ScrollWheel") < 0f || Input.GetKeyDown(KeyCode.Keypad2) || Input.GetKeyDown(KeyCode.Alpha2))
        {
            if (selectedWeapon <= 0)
            {
                selectedWeapon = transform.childCount - 1;
            }
            else
            {
                selectedWeapon--;
            }
            
        }

        if (previousSelectedWeapon != selectedWeapon)
        {
            SelectWeapon();
        }

    }

    void SelectWeapon()
    {
        int i = 0;
        foreach (Transform weapon in transform)
        {
            if (i == selectedWeapon)
            {
                weapon.GetComponent<Image>().enabled = false;
                weapon.GetChild(i).GetComponent<Image>().enabled = true;
                Debug.Log("selected" + weapon.GetChild(i).gameObject.name);
            }
            else
            {
                weapon.GetComponent<Image>().enabled = true;
                weapon.GetChild(i).GetComponent<Image>().enabled = false;
                Debug.Log("not selected" + weapon.GetChild(i).gameObject.name );
                Debug.Log(i);
            }
            i++;
        }
    }
}
