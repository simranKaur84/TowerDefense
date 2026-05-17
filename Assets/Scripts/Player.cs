using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    private enum Mode
    {
        Build,
        Play
    }
    private Mode mode = Mode.Build;

    [Header("References")]
    public Transform trans;
    public Transform spawnPoint;
    public Transform leakPoint;

    [Tooltip("Reference to the sell button lock panel GameObject.")]
    public GameObject sellButtonLockPanel;

    public TMPro.TextMeshProUGUI livesText;
    public TMPro.TextMeshProUGUI levelText;

    [Header("X Bounds")]
    public float minimumX = -70;
    public float maximumX = 70;
    [Header("Y Bounds")]
    public float minimumY = 18;
    public float maximumY = 80;
    [Header("Z Bounds")]
    public float minimumZ = -130;
    public float maximumZ = 70;
    [Header("Movement")]
    public float arrowKeySpeed = 80;
    public float mouseDragSensitivity = 2.8f;
    [Range(0, .99f)]
    public float movementSmoothing = .75f;
    private Vector3 targetPosition;

    [Header("Scrolling")]
    public float scrollSensitivity = 1.6f;

    [Header("Build Mode")]
    public int gold = 50;
    public LayerMask stageLayerMask;
    public Transform highlighter;
    public RectTransform towerSellingPanel;
    public TextMeshProUGUI sellRefundText;
    public TextMeshProUGUI currentGoldText;
    public Color selectedBuildButtonColor = new Color(.2f, .8f, .2f);

    private Vector3 lastMousePosition;
    private int goldLastFrame;
    private bool cursorIsOverStage = false;
    private Tower towerPrefabToBuild = null;
    private Image selectedBuildButtonImage = null;
    private Tower selectedTower = null;
    private Dictionary<Vector3, Tower> towers = new Dictionary<Vector3, Tower>();

    [Header("Play Mode")]
    public GameObject buildButtonPanel;
    public GameObject gameLostPanel;
    public TMPro.TextMeshProUGUI gameLostPanelInfoText;
    public GameObject playButton;
    public Transform enemyHolder;
    public Enemy groundEnemyPrefab;
    public Enemy flyingEnemyPrefab;
    public float enemySpawnRate = .35f;
    public int flyingLevelInterval = 4;
    public int enemiesPerLevel = 15;
    public int goldRewardPerLevel = 12;

    [Header("Boss Level")]
    [Tooltip("Boss wave triggers every X levels (e.g. 5 = levels 5, 10, 15...)")]
    public int bossLevelInterval = 5;
    [Tooltip("Extra enemies spawned during boss wave (added on top of enemiesPerLevel)")]
    public int bossExtraEnemies = 10;
    public GameObject bossWarningPanel;
    public TMPro.TextMeshProUGUI bossWarningText;

    public static int level = 1;
    private int enemiesSpawnedThisLevel = 0;
    private int totalEnemiesToSpawnThisLevel = 0;
    public static int remainingLives = 40;
    private bool gameEnded = false;

    private bool IsBossLevel => level % bossLevelInterval == 0;

    void ArrowKeyMovement()
    {
        if (Input.GetKey(KeyCode.UpArrow))
            targetPosition.z += arrowKeySpeed * Time.deltaTime;
        else if (Input.GetKey(KeyCode.DownArrow))
            targetPosition.z -= arrowKeySpeed * Time.deltaTime;

        if (Input.GetKey(KeyCode.RightArrow))
            targetPosition.x += arrowKeySpeed * Time.deltaTime;
        else if (Input.GetKey(KeyCode.LeftArrow))
            targetPosition.x -= arrowKeySpeed * Time.deltaTime;
    }

    void MouseDragMovement()
    {
        if (Input.GetMouseButton(1))
        {
            Vector3 movement = new Vector3(-Input.GetAxis("Mouse X"), 0, -Input.GetAxis("Mouse Y")) * mouseDragSensitivity;
            if (movement != Vector3.zero)
                targetPosition += movement;
        }
    }

    void Zooming()
    {
        float scrollDelta = -Input.mouseScrollDelta.y;
        if (scrollDelta != 0)
            targetPosition.y += scrollDelta * scrollSensitivity;
    }

    void MoveTowardsTarget()
    {
        targetPosition.x = Mathf.Clamp(targetPosition.x, minimumX, maximumX);
        targetPosition.y = Mathf.Clamp(targetPosition.y, minimumY, maximumY);
        targetPosition.z = Mathf.Clamp(targetPosition.z, minimumZ, maximumZ);

        if (trans.position != targetPosition)
            trans.position = Vector3.Lerp(trans.position, targetPosition, 1 - movementSmoothing);
    }

    void PositionHighlighter()
    {
        if (Input.mousePosition != lastMousePosition)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, stageLayerMask.value))
            {
                Vector3 point = hit.point;
                point.x = Mathf.Round(hit.point.x * .1f) * 10;
                point.z = Mathf.Round(hit.point.z * .1f) * 10;
                point.z = Mathf.Clamp(point.z, -80, 80);
                point.y = .2f;
                highlighter.position = point;
                highlighter.gameObject.SetActive(true);
                cursorIsOverStage = true;
            }
            else
            {
                cursorIsOverStage = false;
                highlighter.gameObject.SetActive(false);
            }
        }
        lastMousePosition = Input.mousePosition;
    }

    void OnStageClicked()
    {
        if (towerPrefabToBuild != null)
        {
            if (!towers.ContainsKey(highlighter.position) && gold >= towerPrefabToBuild.goldCost)
                BuildTower(towerPrefabToBuild, highlighter.position);
        }
        else
        {
            if (towers.ContainsKey(highlighter.position))
            {
                selectedTower = towers[highlighter.position];
                sellRefundText.text = "for " + Mathf.CeilToInt(selectedTower.goldCost * selectedTower.refundFactor) + " gold";
                towerSellingPanel.gameObject.SetActive(true);
            }
        }
    }

    void BuildTower(Tower prefab, Vector3 position)
    {
        towers[position] = Instantiate<Tower>(prefab, position, Quaternion.identity);
        gold -= towerPrefabToBuild.goldCost;
        UpdateEnemyPath();
    }

    void SellTower(Tower tower)
    {
        DeselectTower();
        gold += Mathf.CeilToInt(tower.goldCost * tower.refundFactor);
        towers.Remove(tower.transform.position);
        Destroy(tower.gameObject);
        UpdateEnemyPath();
    }

    public void OnSellTowerButtonClicked()
    {
        if (selectedTower != null)
            SellTower(selectedTower);
    }

    void PositionSellPanel()
    {
        if (selectedTower != null)
        {
            var screenPosition = Camera.main.WorldToScreenPoint(selectedTower.transform.position + Vector3.forward * 8);
            towerSellingPanel.position = screenPosition;
        }
    }

    void UpdateCurrentGold()
    {
        if (gold != goldLastFrame)
            currentGoldText.text = gold + " gold";
        goldLastFrame = gold;
    }

    public void DeselectTower()
    {
        selectedTower = null;
        towerSellingPanel.gameObject.SetActive(false);
    }

    void DeselectBuildButton()
    {
        towerPrefabToBuild = null;
        if (selectedBuildButtonImage != null)
        {
            selectedBuildButtonImage.color = Color.white;
            selectedBuildButtonImage = null;
        }
    }

    void UpdateEnemyPath()
    {
        Invoke("PerformPathfinding", .1f);
    }

    void BuildModeLogic()
    {
        PositionHighlighter();
        PositionSellPanel();
        UpdateCurrentGold();

        if (cursorIsOverStage && Input.GetMouseButtonDown(0))
            OnStageClicked();

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            DeselectTower();
            DeselectBuildButton();
        }
    }

    public void OnBuildButtonClicked(Tower associatedTower)
    {
        towerPrefabToBuild = associatedTower;
        DeselectTower();
    }

    public void SetSelectedBuildButton(Image clickedButtonImage)
    {
        selectedBuildButtonImage = clickedButtonImage;
        clickedButtonImage.color = selectedBuildButtonColor;
    }

    void Start()
    {
        targetPosition = trans.position;
        GroundEnemy.path = new NavMeshPath();
        UpdateEnemyPath();

        if (bossWarningPanel != null)
            bossWarningPanel.SetActive(false);
    }

    void Update()
    {
        ArrowKeyMovement();
        MouseDragMovement();
        Zooming();
        MoveTowardsTarget();

        if (mode == Mode.Build)
            BuildModeLogic();
        else
            PlayModeLogic();

        UpdateLivesText();
        UpdateLevelText();
    }

    void PerformPathfinding()
    {
        NavMesh.CalculatePath(
        spawnPoint.position,
        leakPoint.position,
        NavMesh.AllAreas,
        GroundEnemy.path
        );

        if (GroundEnemy.path.status == NavMeshPathStatus.PathComplete)
            sellButtonLockPanel.SetActive(false);
        else
            sellButtonLockPanel.SetActive(true);
    }

    void SpawnEnemy()
    {
        Enemy enemy = null;

        if (IsBossLevel)
        {
            // Boss level: alternate between ground and flying
            // Ground spawns at normal position, flying spawns offset to the right
            if (enemiesSpawnedThisLevel % 2 == 0)
            {
                enemy = Instantiate(
                groundEnemyPrefab,
                spawnPoint.position,
                Quaternion.LookRotation(Vector3.back)
                );
            }
            else
            {
                enemy = Instantiate(
                flyingEnemyPrefab,
                spawnPoint.position + (Vector3.up * 18) + (Vector3.right * 15), // offset right so visible
                Quaternion.LookRotation(Vector3.back)
                );
            }
        }
        else if (level % flyingLevelInterval == 0)
        {
            enemy = Instantiate(
            flyingEnemyPrefab,
            spawnPoint.position + (Vector3.up * 18),
            Quaternion.LookRotation(Vector3.back)
            );
        }
        else
        {
            enemy = Instantiate(
            groundEnemyPrefab,
            spawnPoint.position,
            Quaternion.LookRotation(Vector3.back)
            );
        }

        enemy.trans.SetParent(enemyHolder);
        enemiesSpawnedThisLevel += 1;

        if (enemiesSpawnedThisLevel >= totalEnemiesToSpawnThisLevel)
            CancelInvoke("SpawnEnemy");
    }

    public void PlayModeLogic()
    {
        if (enemyHolder.childCount == 0 && enemiesSpawnedThisLevel >= totalEnemiesToSpawnThisLevel)
        {
            if (remainingLives > 0)
                GoToBuildMode();
            else
            {
                if (!gameEnded)
                {
                    gameEnded = true;

                    gameLostPanelInfoText.text =
                    "You survived until level " + level +
                    ". Remaining lives: " + remainingLives;

                    gameLostPanel.SetActive(true);

                    if (AudioManager.Instance != null)
                        AudioManager.Instance.PlayGameOverSound();

                    StartCoroutine(FreezeAfterSound());
                }
            }
        }
    }

    private IEnumerator FreezeAfterSound()
    {
        yield return new WaitForSecondsRealtime(2f);
        Time.timeScale = 0f;
    }

    void GoToPlayMode()
    {
        mode = Mode.Play;
        buildButtonPanel.SetActive(false);
        playButton.SetActive(false);
        highlighter.gameObject.SetActive(false);
    }

    void GoToBuildMode()
    {
        mode = Mode.Build;
        buildButtonPanel.SetActive(true);
        playButton.SetActive(true);
        enemiesSpawnedThisLevel = 0;
        level += 1;
        gold += goldRewardPerLevel;
    }

    public void StartLevel()
    {
        GoToPlayMode();

        // Boss levels spawn more enemies
        totalEnemiesToSpawnThisLevel = IsBossLevel
        ? enemiesPerLevel + bossExtraEnemies
        : enemiesPerLevel;

        if (IsBossLevel)
            StartCoroutine(ShowBossWarningThenSpawn());
        else
            InvokeRepeating("SpawnEnemy", .5f, enemySpawnRate);
    }

    private IEnumerator ShowBossWarningThenSpawn()
    {
        if (bossWarningPanel != null)
        {
            if (bossWarningText != null)
                bossWarningText.text = "!! BOSS WAVE !!";
            bossWarningPanel.SetActive(true);
            yield return new WaitForSeconds(2f);
            bossWarningPanel.SetActive(false);
        }
        else
        {
            yield return new WaitForSeconds(0f);
        }

        InvokeRepeating("SpawnEnemy", .5f, enemySpawnRate);
    }

    void UpdateLivesText()
    {
        livesText.text = "Lives: " + remainingLives;
    }

    void UpdateLevelText()
    {
        levelText.text = IsBossLevel ? "BOSS Level " + level : "Level: " + level;
    }
}