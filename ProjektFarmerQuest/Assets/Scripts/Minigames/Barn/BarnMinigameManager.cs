using UnityEngine;
using TMPro;

public class BarnMinigameManager : MonoBehaviour
{
    public static BarnMinigameManager Instance;

    public TMP_Text selectedPigText;
    public TMP_Text pigStatsText;
    public TMP_Text timerText;
    public TMP_Text resultText;

    public GameObject resultPanel;

    // Animal locations
    public Transform outsidePoint;
    public Transform insidePoint;

    public GameObject selectionMarkerPrefab;

    private GameObject selectionMarker;

    public float gameDuration = 60f;

    private float timeRemaining;
    private bool gameEnded = false;

    public bool GameEnded
    {
        get { return gameEnded; }
    }

    private BarnAnimal selectedAnimal;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        timeRemaining = gameDuration;

        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }

        UpdateSelectedAnimalText();
        UpdateTimerText();
        UpdateAnimalStatsText();
    }

    void Update()
    {
        if (gameEnded)
        {
            return;
        }

        timeRemaining -= Time.deltaTime;

        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;

            UpdateTimerText();
            EndMinigame();

            return;
        }

        UpdateTimerText();
        UpdateAnimalStatsText();
        if (selectedAnimal != null && selectionMarker != null)
        {
            selectionMarker.transform.position =
                selectedAnimal.transform.position + new Vector3(0f, -0.5f, 0f);
        }
    }

    public void SelectAnimal(BarnAnimal animal)
    {
        if (gameEnded)
        {
            return;
        }

        selectedAnimal = animal;

        if (selectionMarker == null)
        {
            selectionMarker = Instantiate(selectionMarkerPrefab);
        }

        selectionMarker.transform.position =
            selectedAnimal.transform.position + new Vector3(0f, -0.5f, 0f);

        UpdateSelectedAnimalText();
        UpdateAnimalStatsText();
    }

    void UpdateSelectedAnimalText()
    {
        if (selectedPigText == null)
        {
            return;
        }

        if (selectedAnimal == null)
        {
            selectedPigText.text = "Selected Animal: None";
        }
        else
        {
            selectedPigText.text =
                $"Selected Animal: {selectedAnimal.gameObject.name}";
        }
    }


    void UpdateAnimalStatsText()
    {
        if (pigStatsText == null)
        {
            return;
        }

        if (selectedAnimal == null)
        {
            pigStatsText.text = "No Animal Selected";
            return;
        }

        string location =
            selectedAnimal.isOutside
                ? "Outside"
                : "Inside";

        pigStatsText.text =
            $"{selectedAnimal.animalType}\n\n" +
            $"Hunger: {selectedAnimal.hunger:F0}\n" +
            $"Thirst: {selectedAnimal.thirst:F0}\n" +
            $"Happiness: {selectedAnimal.happiness:F0}\n" +
            $"Location: {location}";
    }


    void UpdateTimerText()
    {
        if (timerText == null)
        {
            return;
        }

        timerText.text =
            $"Time: {Mathf.CeilToInt(timeRemaining)}";
    }


    public void FeedSelectedAnimal()
    {
        if (selectedAnimal == null || gameEnded)
        {
            return;
        }

        selectedAnimal.Feed();

        UpdateAnimalStatsText();
    }

    public void WaterSelectedAnimal()
    {
        if (selectedAnimal == null || gameEnded)
        {
            return;
        }

        selectedAnimal.GiveWater();

        UpdateAnimalStatsText();
    }

    public void PatSelectedAnimal()
    {
        if (selectedAnimal == null || gameEnded)
        {
            return;
        }

        selectedAnimal.Pat();

        UpdateAnimalStatsText();
    }

    public void MoveSelectedAnimalOutside()
    {
        if (selectedAnimal == null || gameEnded)
        {
            return;
        }

        if (outsidePoint == null)
        {
            Debug.LogWarning("OutsidePoint is not assigned.");
            return;
        }


        selectedAnimal.MoveOutside();

        // Moves the cow outside.
        selectedAnimal.transform.position =
            outsidePoint.position;

        UpdateAnimalStatsText();
    }

    public void MoveSelectedAnimalInside()
    {
        if (selectedAnimal == null || gameEnded)
        {
            return;
        }

        if (insidePoint == null)
        {
            Debug.LogWarning("InsidePoint is not assigned.");
            return;
        }

        selectedAnimal.MoveInside();

        // Moves the cow inside.
        selectedAnimal.transform.position =
            insidePoint.position;

        UpdateAnimalStatsText();
    }


    void EndMinigame()
    {
        gameEnded = true;

        BarnAnimal[] animals =
            FindObjectsByType<BarnAnimal>(
                FindObjectsSortMode.None
            );

        // Sort by name: Cow_1, Cow_2, Cow_3 etc
        // Add some orda
        System.Array.Sort(
            animals,
            (a, b) => string.Compare(
                a.gameObject.name,
                b.gameObject.name,
                System.StringComparison.Ordinal
            )
        );

        int herdedAnimals = 0;
        float totalHappiness = 0f;

        string animalResults = "";

        foreach (BarnAnimal animal in animals)
        {
            if (!animal.isOutside)
            {
                herdedAnimals++;
                totalHappiness += animal.happiness;

                animalResults +=
                    $"{animal.gameObject.name}\n" +
                    $"Herded\n" +
                    $"Happiness: {animal.happiness:F0}\n\n";
            }
            else
            {
                animalResults +=
                    $"{animal.gameObject.name}\n" +
                    $"Outside\n" +
                    $"Does not count\n\n";
            }
        }

        float averageHappiness = 0f;

        if (herdedAnimals > 0)
        {
            averageHappiness =
                totalHappiness / herdedAnimals;
        }

        int finalScore =
            Mathf.RoundToInt(averageHappiness);

        if (resultText != null)
        {
            resultText.text =
                $"BARN COMPLETE\n\n" +

                animalResults +

                $"Herded: {herdedAnimals} / {animals.Length}\n" +
                $"Average Happiness: {averageHappiness:F1}\n" +
                $"Final Score: {finalScore}";
        }

        if (resultPanel != null)
        {
            resultPanel.SetActive(true);
        }

        UpdateAnimalStatsText();
    }
}