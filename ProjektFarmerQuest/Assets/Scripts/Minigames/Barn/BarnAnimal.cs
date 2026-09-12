using UnityEngine;
using UnityEngine.EventSystems;


public class BarnAnimal : MonoBehaviour
{
    public enum AnimalType
    {
        Pig,
        Cow
    }

    public AnimalType animalType = AnimalType.Pig;


    public float hunger = 100f;
    public float thirst = 100f;
    public float happiness = 100f;

    public bool isOutside = false;

    public float hungerDecayInside = 2f;
    public float thirstDecayInside = 3f;
    public float happinessDecayInside = 1f;

    public float hungerDecayOutside = 1f;
    public float thirstDecayOutside = 1.5f;
    public float happinessDecayOutside = 0.5f;

    void Update()
    {
        if (BarnMinigameManager.Instance == null)
        {
            return;
        }

        if (BarnMinigameManager.Instance.GameEnded)
        {
            return;
        }

        UpdateStats();
    }

    void UpdateStats()
    {
        if (isOutside)
        {
            hunger -= hungerDecayOutside * Time.deltaTime;
            thirst -= thirstDecayOutside * Time.deltaTime;
            happiness -= happinessDecayOutside * Time.deltaTime;
        }
        else
        {
            hunger -= hungerDecayInside * Time.deltaTime;
            thirst -= thirstDecayInside * Time.deltaTime;
            happiness -= happinessDecayInside * Time.deltaTime;
        }

        hunger = Mathf.Clamp(hunger, 0f, 100f);
        thirst = Mathf.Clamp(thirst, 0f, 100f);
        happiness = Mathf.Clamp(happiness, 0f, 100f);
    }

    public void Feed()
    {
        hunger += 25f;
        hunger = Mathf.Clamp(hunger, 0f, 100f);
    }

    public void GiveWater()
    {
        thirst += 25f;
        thirst = Mathf.Clamp(thirst, 0f, 100f);
    }

    public void Pat()
    {
        happiness += 15f;
        happiness = Mathf.Clamp(happiness, 0f, 100f);
    }

    public void MoveOutside()
    {
        isOutside = true;
    }

    public void MoveInside()
    {
        isOutside = false;
    }
    void OnMouseDown()
    {
        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        if (BarnMinigameManager.Instance != null)
        {
            BarnMinigameManager.Instance.SelectAnimal(this);
        }
    }
}