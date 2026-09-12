using UnityEngine;

public class BarnAreaTrigger : MonoBehaviour
{
    public string areaName;

    private void OnTriggerEnter(Collider other)
    {
        BarnAnimal animal = other.GetComponent<BarnAnimal>();

        if (animal == null)
        {
            return;
        }

        if (areaName == "Outside")
        {
            animal.isOutside = true;
            Debug.Log($"{other.name} entered Outside");
        }
        else if (areaName == "Inside")
        {
            animal.isOutside = false;
            Debug.Log($"{other.name} entered Inside");
        }
    }
}