using UnityEngine;

public class FieldPlant : MonoBehaviour
{
    public enum PlantType
    {
        Crop,
        Weed
    }
    public PlantType plantType;

    // The specific plant name.
    public string specificName;
}