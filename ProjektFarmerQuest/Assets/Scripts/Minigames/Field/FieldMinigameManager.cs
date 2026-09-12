using UnityEngine;
using TMPro;

public class FieldMinigameManager : MonoBehaviour
{
    public enum CutterType
    {
        None,
        WeedCutter,
        CropCutter
    }
    public TMP_Text cropPileText;
    public TMP_Text weedPileText;
    public GameObject resultPanel;
    public TMP_Text resultText;
    private bool minigameComplete = false;

    private CutterType selectedCutter = CutterType.None;

    private int cropsInCropPile = 0;
    private int weedsInCropPile = 0;

    private int weedsInWeedPile = 0;
    private int cropsInWeedPile = 0;

    private int plantsRemaining;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            TryCutPlant();
        }
    }

    public void SelectWeedCutter()
    {
        selectedCutter = CutterType.WeedCutter;

        Debug.Log("Weed Cutter selected");
    }

    public void SelectCropCutter()
    {
        selectedCutter = CutterType.CropCutter;

        Debug.Log("Crop Cutter selected");
    }

    void TryCutPlant()
    {
        if (minigameComplete)
        {
            return;
        }
        if (selectedCutter == CutterType.None)
        {
            return;
        }

        Ray ray =
            Camera.main.ScreenPointToRay(Input.mousePosition);

        if (!Physics.Raycast(ray, out RaycastHit hit))
        {
            return;
        }

        FieldPlant plant =
            hit.collider.GetComponent<FieldPlant>();

        if (plant == null)
        {
            return;
        }

        ProcessPlant(plant);

        plantsRemaining--;

        Destroy(plant.gameObject);

        Debug.Log($"Plants remaining: {plantsRemaining}");
        if (plantsRemaining <= 0)
        {
            EndMinigame();
        }
    }

    void ProcessPlant(FieldPlant plant)
    {
        string plantName = plant.specificName;

        if (selectedCutter == CutterType.CropCutter)
        {
            if (plant.plantType == FieldPlant.PlantType.Crop)
            {
                cropsInCropPile++;

                Debug.Log(
                    $"{plantName} correctly added to Crop pile."
                );
            }
            else
            {
                weedsInCropPile++;

                Debug.Log(
                    $"Mistake! {plantName} added to Crop pile."
                );
            }
        }
        else if (selectedCutter == CutterType.WeedCutter)
        {
            if (plant.plantType == FieldPlant.PlantType.Weed)
            {
                weedsInWeedPile++;

                Debug.Log(
                    $"{plantName} correctly added to Weed pile."
                );
            }
            else
            {
                cropsInWeedPile++;

                Debug.Log(
                    $"Mistake! {plantName} added to Weed pile."
                );
            }
        }

        UpdatePileText();

        Debug.Log(
            $"Crop Pile: {cropsInCropPile} crops, " +
            $"{weedsInCropPile} weeds"
        );

        Debug.Log(
            $"Weed Pile: {weedsInWeedPile} weeds, " +
            $"{cropsInWeedPile} crops"
        );
    }
    void Start()
    {
        plantsRemaining = FindObjectsByType<FieldPlant>(
            FindObjectsSortMode.None
        ).Length;

        resultPanel.SetActive(false);

        UpdatePileText();

        Debug.Log($"Plants remaining: {plantsRemaining}");
    }
    void UpdatePileText()
    {
        cropPileText.text =
            $"Crop Pile\n" +
            $"Vårhvede: {cropsInCropPile}\n" +
            $"Gåsefod contamination: {weedsInCropPile}";

        weedPileText.text =
            $"Weed Pile\n" +
            $"Gåsefod: {weedsInWeedPile}\n" +
            $"Vårhvede contamination: {cropsInWeedPile}";
    }
    void EndMinigame()
    {
        minigameComplete = true;
        int totalCorrect =
            cropsInCropPile + weedsInWeedPile;

        int totalMistakes =
            weedsInCropPile + cropsInWeedPile;

        int totalPlants =
            totalCorrect + totalMistakes;

        float purity = 0f;

        if (totalPlants > 0)
        {
            purity =
                ((float)totalCorrect / totalPlants) * 100f;
        }

        Debug.Log("FIELD MINIGAME COMPLETE!");
        Debug.Log($"Correctly sorted: {totalCorrect}");
        Debug.Log($"Mistakes: {totalMistakes}");
        Debug.Log($"Purity: {purity:F1}%");

        resultText.text =
            $"FIELD COMPLETE!\n\n" +
            $"Correctly Sorted: {totalCorrect} / {totalPlants}\n" +
            $"Mistakes: {totalMistakes}\n" +
            $"Purity: {purity:F1}%";

        resultPanel.SetActive(true);
    }
}