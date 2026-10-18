using System.Collections;
using UnityEngine;

namespace Depthbound
{
    public class DungeonDirector : MonoBehaviour
    {
        public static DungeonDirector Instance { get; private set; }

        private const int TotalActs = 3;
        private const int RoomsPerAct = 3;
        private const int TotalRooms = TotalActs * RoomsPerAct;
        // Visual direction: restrained anime-inspired dungeon.
        // Keep silhouettes and lighting readable without dense decorative clutter.
        private const bool SimpleAnimeVisuals = true;
        private readonly bool[] roomStarted = new bool[TotalRooms];
        private readonly bool[] roomCleared = new bool[TotalRooms];
        private readonly bool[] bossSpawned = new bool[TotalRooms];
        private readonly bool[] bossSpawning = new bool[TotalRooms];
        private readonly int[] enemiesRemaining = new int[TotalRooms];
        private readonly int[] activeEnemiesRemaining = new int[TotalRooms];
        private readonly int[] roomWaveIndex = new int[TotalRooms];
        private readonly int[] roomWaveCount = new int[TotalRooms];
        private readonly int[] nextWaveSpawnIndex = new int[TotalRooms];
        private readonly bool[] waveSpawning = new bool[TotalRooms];
        private readonly GameObject[] exitDoors = new GameObject[TotalRooms];
        private readonly GameObject[] entryDoors = new GameObject[TotalRooms];
        private readonly RuneHazard[] roomHazards = new RuneHazard[TotalRooms];

        private Transform worldRoot;
        private GameObject enemyPrefab;
        private FirstPersonController playerController;
        private PlayerCombat playerCombat;
        private PlayerHealth playerHealth;

        private Material floorMaterial;
        private Material wallMaterial;
        private Material trimMaterial;
        private Material accentMaterial;
        private Material metalMaterial;
        private Material runeMaterial;
        private Material warmRuneMaterial;
        private Material bossRuneMaterial;
        private Material woodMaterial;
        private Material boneMaterial;
        private Material rustMaterial;
        private Material boonFocusMaterial;
        private Light moonLight;

        private float runStartTime;

        public float ElapsedRunTime =>
            Mathf.Max(0f, Time.time - runStartTime);
        private int enemiesDefeated;
        private int boonsChosen;
        private int relicsCollected;
        private int evades;
        private int perfectBlocks;
        private int soulShardsCollected;
        private int maxMomentum;
        private bool deathTriggered;
        private Coroutine atmosphereRoutine;
        private GameObject activeBoonFocus;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Start()
        {
            runStartTime = Time.time;
            BuildMaterials();
            BuildWorld();
            CreatePlayer();
            enemyPrefab = Resources.Load<GameObject>("Enemy");

            if (HUDController.Instance != null)
            {
                HUDController.Instance.SetRoom(0, TotalRooms, 0);
                HUDController.Instance.SetObjective(
                    "ENTER THE ANTECHAMBER",
                    new Color(0.58f, 0.76f, 0.82f));
            }
        }

        public void StartRoom(int roomIndex)
        {
            if (roomIndex < 0 ||
                roomIndex >= TotalRooms ||
                roomStarted[roomIndex])
            {
                return;
            }

            roomStarted[roomIndex] = true;

            int act = roomIndex / RoomsPerAct;
            int localRoom = roomIndex % RoomsPerAct;

            if (atmosphereRoutine != null)
            {
                StopCoroutine(atmosphereRoutine);
            }

            atmosphereRoutine =
                StartCoroutine(BlendAtmosphere(roomIndex));

            if (roomHazards[roomIndex] != null)
            {
                roomHazards[roomIndex].SetArmed(true);
            }

            if (ProceduralAudio.Instance != null)
            {
                ProceduralAudio.Instance.SetDungeonStage(
                    roomIndex,
                    true);
            }

            if (entryDoors[roomIndex] != null)
            {
                DungeonDoor entrySeal =
                    entryDoors[roomIndex].GetComponent<DungeonDoor>();

                if (entrySeal != null)
                {
                    entrySeal.CloseFromAbove();
                }
                else
                {
                    entryDoors[roomIndex].SetActive(true);
                }
            }

            int count =
                EncounterCountForRoom(
                    roomIndex);

            enemiesRemaining[roomIndex] = count;

            int waves =
                count >= 4
                    ? 2
                    : 1;

            roomWaveCount[roomIndex] = waves;
            roomWaveIndex[roomIndex] = 1;
            waveSpawning[roomIndex] = false;

            int firstWaveCount =
                waves == 1
                    ? count
                    : Mathf.CeilToInt(
                        count * 0.5f);

            activeEnemiesRemaining[roomIndex] =
                firstWaveCount;

            nextWaveSpawnIndex[roomIndex] =
                firstWaveCount;

            SpawnWave(
                roomIndex,
                0,
                firstWaveCount,
                1);

            if (HUDController.Instance != null)
            {
                HUDController.Instance.SetRoom(
                    roomIndex,
                    TotalRooms,
                    activeEnemiesRemaining[roomIndex]);

                HUDController.Instance.ShowRoomBanner(
                    roomIndex,
                    IsBossRoom(roomIndex));

                HUDController.Instance.ShowEncounterRule(
                    EncounterRuleLabel(
                        roomIndex));

                if (localRoom == 0)
                {
                    HUDController.Instance.ShowActStyleShift(act);
                }

                SetWaveObjective(
                    roomIndex);

                if (localRoom == 1 &&
                    roomHazards[roomIndex] != null)
                {
                    HUDController.Instance.ShowHazardWarning(
                        act);
                }
            }
        }

        public void OnEnemyDefeated(int roomIndex)
        {
            if (roomIndex < 0 ||
                roomIndex >= TotalRooms ||
                roomCleared[roomIndex])
            {
                return;
            }

            enemiesRemaining[roomIndex] =
                Mathf.Max(
                    0,
                    enemiesRemaining[roomIndex] - 1);

            activeEnemiesRemaining[roomIndex] =
                Mathf.Max(
                    0,
                    activeEnemiesRemaining[roomIndex] - 1);

            enemiesDefeated++;

            if (HUDController.Instance != null)
            {
                HUDController.Instance.SetRoom(
                    roomIndex,
                    TotalRooms,
                    activeEnemiesRemaining[roomIndex]);

                if (activeEnemiesRemaining[roomIndex] > 0)
                {
                    SetWaveObjective(
                        roomIndex);
                }
            }

            if (activeEnemiesRemaining[roomIndex] == 0 &&
                enemiesRemaining[roomIndex] > 0)
            {
                if (!waveSpawning[roomIndex])
                {
                    StartCoroutine(
                        SpawnNextWaveRoutine(
                            roomIndex));
                }

                return;
            }

            if (enemiesRemaining[roomIndex] != 0)
            {
                return;
            }

            if (IsBossRoom(roomIndex) &&
                !bossSpawned[roomIndex] &&
                !bossSpawning[roomIndex])
            {
                StartCoroutine(BossArrivalRoutine(roomIndex));
                return;
            }

            roomCleared[roomIndex] = true;
            StartCoroutine(RoomClearRoutine(roomIndex));
        }

        private void SpawnWave(
            int roomIndex,
            int startIndex,
            int count,
            int waveNumber)
        {
            if (count <= 0)
            {
                return;
            }

            Vector3 center =
                RoomCenter(roomIndex);

            for (int i = 0;
                 i < count;
                 i++)
            {
                int spawnIndex =
                    startIndex + i;

                Vector3 position =
                    EncounterSpawnPosition(
                        roomIndex,
                        waveNumber,
                        i,
                        count,
                        center);

                int archetypeVariant =
                    ArchetypeVariantForRoom(
                        roomIndex,
                        spawnIndex,
                        waveNumber);

                SpawnEnemy(
                    position,
                    roomIndex,
                    archetypeVariant,
                    false);
            }
        }

        private IEnumerator SpawnNextWaveRoutine(
            int roomIndex)
        {
            waveSpawning[roomIndex] = true;

            int nextWave =
                Mathf.Min(
                    roomWaveCount[roomIndex],
                    roomWaveIndex[roomIndex] + 1);

            if (HUDController.Instance != null)
            {
                HUDController.Instance.SetRoom(
                    roomIndex,
                    TotalRooms,
                    0);

                HUDController.Instance.ShowWaveIncoming(
                    nextWave,
                    roomWaveCount[roomIndex]);

                HUDController.Instance.SetObjective(
                    IsBossRoom(roomIndex)
                        ? "MORE GUARDIANS APPROACH"
                        : "THE NEXT WAVE APPROACHES",
                    IsBossRoom(roomIndex)
                        ? new Color(
                            0.72f,
                            0.30f,
                            0.18f)
                        : new Color(
                            0.62f,
                            0.54f,
                            0.34f));
            }

            yield return new WaitForSeconds(
                WaveDelayForRoom(
                    roomIndex,
                    nextWave));

            if (deathTriggered ||
                roomCleared[roomIndex])
            {
                waveSpawning[roomIndex] = false;
                yield break;
            }

            int count =
                enemiesRemaining[roomIndex];

            roomWaveIndex[roomIndex] =
                nextWave;

            activeEnemiesRemaining[roomIndex] =
                count;

            int startIndex =
                nextWaveSpawnIndex[roomIndex];

            nextWaveSpawnIndex[roomIndex] +=
                count;

            SpawnWave(
                roomIndex,
                startIndex,
                count,
                nextWave);

            waveSpawning[roomIndex] = false;

            if (HUDController.Instance != null)
            {
                HUDController.Instance.SetRoom(
                    roomIndex,
                    TotalRooms,
                    activeEnemiesRemaining[roomIndex]);

                SetWaveObjective(
                    roomIndex);
            }
        }

        private void SetWaveObjective(
            int roomIndex)
        {
            if (HUDController.Instance == null)
            {
                return;
            }

            bool guardians =
                IsBossRoom(roomIndex) &&
                !bossSpawned[roomIndex];

            int wave =
                Mathf.Max(
                    1,
                    roomWaveIndex[roomIndex]);

            int waves =
                Mathf.Max(
                    1,
                    roomWaveCount[roomIndex]);

            int active =
                activeEnemiesRemaining[roomIndex];

            int localRoom =
                roomIndex % RoomsPerAct;

            string waveLabel =
                waves > 1
                    ? "WAVE " +
                      wave +
                      "/" +
                      waves +
                      " - "
                    : "";

            string objective;

            if (guardians)
            {
                objective =
                    waveLabel +
                    (active == 1
                        ? "1 GUARDIAN REMAINS"
                        : active +
                          " GUARDIANS REMAIN");
            }
            else if (localRoom == 1)
            {
                objective =
                    waveLabel +
                    "RESOLVE HOSTILES - RELIC ON ALTAR";
            }
            else
            {
                objective =
                    waveLabel +
                    (active == 1
                        ? "1 FOE REMAINS"
                        : active +
                          " FOES REMAIN");
            }

            HUDController.Instance.SetObjective(
                objective,
                guardians
                    ? new Color(
                        0.72f,
                        0.30f,
                        0.18f)
                    : localRoom == 1
                        ? new Color(
                            0.66f,
                            0.48f,
                            0.24f)
                        : new Color(
                            0.55f,
                            0.60f,
                            0.48f));
        }

        private IEnumerator BossArrivalRoutine(int roomIndex)
        {
            bossSpawning[roomIndex] = true;

            if (playerCombat != null)
            {
                playerCombat.BreakMomentum();
            }

            if (HUDController.Instance != null)
            {
                HUDController.Instance.SetRoom(
                    roomIndex,
                    TotalRooms,
                    0);

                HUDController.Instance.SetObjective(
                    "THE SEAL BREAKS",
                    new Color(0.98f, 0.30f, 0.18f));

                HUDController.Instance.ShowBossAwakens();
            }

            Vector3 center = RoomCenter(roomIndex);

            if (ProceduralAudio.Instance != null)
            {
                ProceduralAudio.Instance.BossAwaken(center);
            }

            CombatEffects.SpawnRuneBurst(
                center + Vector3.up * 0.08f,
                3.4f);

            CombatEffects.SpawnBossAwakening(center);

            if (playerController != null)
            {
                playerController.AddCameraImpact(
                    0.52f,
                    0.30f);

                Vector3 fromSeal =
                    playerController.transform.position - center;

                fromSeal.y = 0f;

                if (fromSeal.sqrMagnitude < 9.0f)
                {
                    if (fromSeal.sqrMagnitude < 0.01f)
                    {
                        fromSeal =
                            -playerController.transform.forward;
                        fromSeal.y = 0f;
                    }

                    Rigidbody playerBody =
                        playerController.GetComponent<Rigidbody>();

                    if (playerBody != null)
                    {
                        playerBody.AddForce(
                            fromSeal.normalized * 3.4f +
                            Vector3.up * 0.45f,
                            ForceMode.Impulse);
                    }
                }
            }

            yield return new WaitForSeconds(0.72f);

            bossSpawned[roomIndex] = true;
            bossSpawning[roomIndex] = false;
            enemiesRemaining[roomIndex] = 1;
            activeEnemiesRemaining[roomIndex] = 1;

            EnemyController boss = SpawnEnemy(
                center + Vector3.up * 0.05f,
                roomIndex,
                roomIndex / RoomsPerAct,
                true);

            SpawnBossReinforcements(
                center,
                boss);

            if (HUDController.Instance != null)
            {
                HUDController.Instance.SetRoom(
                    roomIndex,
                    TotalRooms,
                    1);

                HUDController.Instance.SetObjective(
                    "DEFEAT " + BossNameForRoom(roomIndex),
                    new Color(0.95f, 0.34f, 0.24f));
            }
        }

        private void SpawnBossReinforcements(
            Vector3 center,
            EnemyController boss)
        {
            if (RunMorality.Instance == null ||
                RunMorality.Instance.SparedArchetypes.Count == 0 ||
                boss == null)
            {
                return;
            }

            int allies = 0;
            int absent = 0;
            int betrayers = 0;
            int allySpawnIndex = 0;
            int betrayerSpawnIndex = 0;

            Vector3 playerDirection =
                playerController != null
                    ? playerController.transform.position -
                      center
                    : Vector3.back;

            playerDirection.y = 0f;

            if (playerDirection.sqrMagnitude <
                0.01f)
            {
                playerDirection =
                    Vector3.back;
            }

            playerDirection.Normalize();

            Vector3 tangent =
                new Vector3(
                    -playerDirection.z,
                    0f,
                    playerDirection.x);

            int count =
                RunMorality.Instance.SparedArchetypes.Count;

            for (int i = 0; i < count; i++)
            {
                EnemyArchetype archetype =
                    RunMorality.Instance.SparedArchetypes[i];

                float roll = Random.value;

                // 20% help, 30% stay away, 50% betray.
                if (roll >= 0.20f && roll < 0.50f)
                {
                    absent++;
                    continue;
                }

                bool friendly = roll < 0.20f;

                int sideIndex =
                    friendly
                        ? allySpawnIndex++
                        : betrayerSpawnIndex++;

                int laneStep =
                    sideIndex % 5;

                float lane =
                    laneStep == 0
                        ? 0f
                        : laneStep % 2 == 1
                            ? -((laneStep + 1) / 2) * 1.05f
                            : (laneStep / 2) * 1.05f;

                float depth =
                    sideIndex / 5 *
                    0.65f;

                Vector3 sideDirection =
                    friendly
                        ? playerDirection
                        : -playerDirection;

                Vector3 position =
                    center +
                    sideDirection *
                    (friendly
                        ? 4.15f + depth
                        : 4.45f + depth) +
                    tangent * lane +
                    Vector3.up * 0.08f;

                GameObject companionObject =
                    new GameObject(
                        friendly
                            ? "SparedAlly_" + archetype
                            : "SparedBetrayer_" + archetype);

                companionObject.transform.SetParent(worldRoot);
                companionObject.transform.position = position;

                MercyCompanion companion =
                    companionObject.AddComponent<MercyCompanion>();

                companion.Configure(
                    archetype,
                    friendly,
                    boss,
                    playerHealth);

                if (friendly)
                {
                    allies++;
                }
                else
                {
                    betrayers++;
                }
            }

            if (HUDController.Instance != null)
            {
                HUDController.Instance.ShowBossReinforcementOutcome(
                    allies,
                    absent,
                    betrayers);
            }
        }

        public void OnBossEnraged()
        {
            if (atmosphereRoutine != null)
            {
                StopCoroutine(atmosphereRoutine);
            }

            atmosphereRoutine = StartCoroutine(BlendBossEnrageAtmosphere());
        }

        private IEnumerator BlendBossEnrageAtmosphere()
        {
            Color startFog = RenderSettings.fogColor;
            Color startAmbient = RenderSettings.ambientLight;
            float startDensity = RenderSettings.fogDensity;

            Color targetFog = new Color(0.085f, 0.035f, 0.030f);
            Color targetAmbient = new Color(0.190f, 0.100f, 0.085f);
            const float targetDensity = 0.014f;
            const float duration = 0.85f;

            for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
            {
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);

                RenderSettings.fogColor = Color.Lerp(startFog, targetFog, eased);
                RenderSettings.ambientLight = Color.Lerp(startAmbient, targetAmbient, eased);
                RenderSettings.fogDensity = Mathf.Lerp(startDensity, targetDensity, eased);
                yield return null;
            }

            RenderSettings.fogColor = targetFog;
            RenderSettings.ambientLight = targetAmbient;
            RenderSettings.fogDensity = targetDensity;
            atmosphereRoutine = null;
        }

        public void RecordEvade()
        {
            evades++;
        }

        public void RecordPerfectBlock()
        {
            perfectBlocks++;
        }

        public void RecordRelic()
        {
            relicsCollected++;
        }

        public void RecordSoulShard()
        {
            soulShardsCollected++;
        }

        public void RecordMomentum(int count)
        {
            maxMomentum = Mathf.Max(maxMomentum, count);
        }

        public void OnPlayerDied()
        {
            if (deathTriggered) return;

            deathTriggered = true;
            StartCoroutine(PlayerDeathRoutine());
        }

        private IEnumerator PlayerDeathRoutine()
        {
            if (ProceduralAudio.Instance != null)
            {
                ProceduralAudio.Instance.SetMusicDucked(true);
            }

            if (playerCombat != null)
            {
                playerCombat.BreakMomentum();
                playerCombat.enabled = false;
            }

            if (playerController != null)
            {
                playerController.enabled = false;

                Rigidbody playerBody = playerController.GetComponent<Rigidbody>();
                if (playerBody != null)
                {
                    Vector3 velocity = playerBody.linearVelocity;
                    playerBody.linearVelocity = new Vector3(
                        velocity.x * 0.22f,
                        Mathf.Min(velocity.y, 0f),
                        velocity.z * 0.22f);
                }
            }

            Camera deathCamera = Camera.main;

            if (deathCamera != null)
            {
                Transform cameraTransform = deathCamera.transform;
                Vector3 startPosition = cameraTransform.localPosition;
                Quaternion startRotation = cameraTransform.localRotation;
                Vector3 endPosition =
                    startPosition +
                    new Vector3(0.10f, -0.34f, 0.06f);

                Quaternion endRotation =
                    startRotation *
                    Quaternion.Euler(13f, 0f, 10f);

                const float fallDuration = 0.45f;

                for (float elapsed = 0f;
                     elapsed < fallDuration;
                     elapsed += Time.unscaledDeltaTime)
                {
                    float t = Mathf.Clamp01(
                        elapsed / fallDuration);

                    float eased =
                        1f -
                        Mathf.Pow(1f - t, 3f);

                    cameraTransform.localPosition =
                        Vector3.Lerp(
                            startPosition,
                            endPosition,
                            eased);

                    cameraTransform.localRotation =
                        Quaternion.Slerp(
                            startRotation,
                            endRotation,
                            eased);

                    yield return null;
                }
            }
            else
            {
                yield return new WaitForSecondsRealtime(0.45f);
            }

            if (HUDController.Instance != null)
            {
                HUDController.Instance.ShowEnd(false, BuildRunSummary());
            }
        }

        private IEnumerator RoomClearRoutine(int roomIndex)
        {
            if (playerCombat != null)
            {
                playerCombat.BreakMomentum();
            }

            if (roomHazards[roomIndex] != null)
            {
                roomHazards[roomIndex].SetArmed(false);
            }

            if (ProceduralAudio.Instance != null)
            {
                ProceduralAudio.Instance.Clear();
                ProceduralAudio.Instance.SetDungeonStage(roomIndex, false);
            }

            if (HUDController.Instance != null)
            {
                HUDController.Instance.ShowRoomClear();
                HUDController.Instance.SetObjective(
                    roomIndex == TotalRooms - 1
                        ? "DESCENT COMPLETE"
                        : "CHAMBER SECURED",
                    new Color(0.42f, 0.86f, 0.78f));
            }

            OpenDoor(entryDoors[roomIndex], false);
            OpenDoor(exitDoors[roomIndex], true);

            if (IsBossRoom(roomIndex) &&
                roomIndex < TotalRooms - 1 &&
                RunMorality.Instance != null)
            {
                int completedAct =
                    roomIndex / RoomsPerAct + 1;

                RunMorality.Instance.CommitActBoundary(
                    completedAct);
            }

            yield return new WaitForSeconds(0.72f);

            if (roomIndex == TotalRooms - 1)
            {
                if (atmosphereRoutine != null)
                {
                    StopCoroutine(atmosphereRoutine);
                }

                atmosphereRoutine = StartCoroutine(BlendVictoryAtmosphere());

                yield return new WaitForSeconds(1.10f);
                if (HUDController.Instance != null)
                {
                    HUDController.Instance.ShowEnd(true, BuildRunSummary());
                }
            }
            else if (HUDController.Instance != null)
            {
                SpawnBoonFocus(roomIndex);

                HUDController.Instance.SetObjective(
                    "CHOOSE A BOON",
                    new Color(0.72f, 0.78f, 0.90f));
                HUDController.Instance.ShowUpgrade(ApplyUpgrade);
            }
        }

        private void ApplyUpgrade(UpgradeChoice choice)
        {
            boonsChosen++;

            if (HUDController.Instance != null)
            {
                HUDController.Instance.SetObjective(
                    "PROCEED TO THE NEXT CHAMBER",
                    new Color(0.58f, 0.76f, 0.82f));
            }

            switch (choice)
            {
                case UpgradeChoice.Vitality:
                    if (playerHealth != null) playerHealth.IncreaseMaxHealth(25f);
                    break;
                case UpgradeChoice.Brutality:
                    if (playerCombat != null) playerCombat.AddDamage(0.25f);
                    break;
                case UpgradeChoice.Mobility:
                    if (playerController != null) playerController.AddMobility(0.12f, 0.12f);
                    break;
            }

            if (activeBoonFocus != null)
            {
                Destroy(activeBoonFocus);
                activeBoonFocus = null;
            }

            if (playerController != null)
            {
                CombatEffects.SpawnBoon(playerController.transform.position, choice);
            }

            if (HUDController.Instance != null)
            {
                HUDController.Instance.ShowBoonAcquired(choice);
            }
        }

        private void SpawnBoonFocus(int roomIndex)
        {
            if (activeBoonFocus != null)
            {
                Destroy(activeBoonFocus);
            }

            Vector3 center = RoomCenter(roomIndex);

            GameObject root = new GameObject(
                "BoonFocus",
                typeof(BoonFocusVisual));

            root.transform.SetParent(worldRoot);
            root.transform.position = center + Vector3.up * 1.05f;

            GameObject core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            core.name = "BoonCore";
            core.transform.SetParent(root.transform, false);
            core.transform.localScale = Vector3.one * 0.42f;

            Collider coreCollider = core.GetComponent<Collider>();
            if (coreCollider != null)
            {
                Destroy(coreCollider);
            }

            Material coreMaterial = GetBoonFocusMaterial();
            core.GetComponent<Renderer>().sharedMaterial = coreMaterial;

            for (int i = 0; i < 3; i++)
            {
                float angle = i * Mathf.PI * 2f / 3f;

                GameObject mote = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                mote.name = "BoonMote_" + i;
                mote.transform.SetParent(root.transform, false);
                mote.transform.localPosition = new Vector3(
                    Mathf.Cos(angle) * 0.72f,
                    0.05f + i * 0.06f,
                    Mathf.Sin(angle) * 0.72f);
                mote.transform.localScale = Vector3.one * 0.11f;

                Collider moteCollider = mote.GetComponent<Collider>();
                if (moteCollider != null)
                {
                    Destroy(moteCollider);
                }

                mote.GetComponent<Renderer>().sharedMaterial = coreMaterial;
            }

            GameObject lightObject = new GameObject(
                "BoonLight",
                typeof(Light));
            lightObject.transform.SetParent(root.transform, false);

            Light light = lightObject.GetComponent<Light>();
            light.type = LightType.Point;
            light.renderMode = LightRenderMode.ForceVertex;
            light.range = 5.5f;
            light.intensity = 1.35f;
            light.color = new Color(0.18f, 0.72f, 0.88f);
            light.shadows = LightShadows.None;

            activeBoonFocus = root;
        }

        private string BuildRunSummary()
        {
            float elapsed = ElapsedRunTime;
            int minutes = Mathf.FloorToInt(elapsed / 60f);
            int seconds = Mathf.FloorToInt(elapsed % 60f);

            string morality =
                RunMorality.Instance != null
                    ? "\n" + RunMorality.Instance.BuildSummary()
                    : "";

            return
                "TIME  " + minutes.ToString("00") + ":" + seconds.ToString("00") +
                "        FOES RESOLVED  " + enemiesDefeated +
                "\nBOONS  " + boonsChosen +
                "        RELICS  " + relicsCollected +
                "        SOULS  " + soulShardsCollected +
                "\nEVADES  " + evades +
                "        PARRIES  " + perfectBlocks +
                "\nMAX CHAIN  x" + maxMomentum +
                morality;
        }

        private IEnumerator BlendVictoryAtmosphere()
        {
            Color startFog = RenderSettings.fogColor;
            Color startAmbient = RenderSettings.ambientLight;
            float startDensity = RenderSettings.fogDensity;

            Color targetFog = new Color(0.060f, 0.075f, 0.065f);
            Color targetAmbient = new Color(0.165f, 0.185f, 0.155f);
            const float targetDensity = 0.009f;
            const float duration = 1.25f;

            for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
            {
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = t * t * (3f - 2f * t);

                RenderSettings.fogColor = Color.Lerp(startFog, targetFog, eased);
                RenderSettings.ambientLight = Color.Lerp(startAmbient, targetAmbient, eased);
                RenderSettings.fogDensity = Mathf.Lerp(
                    startDensity,
                    targetDensity,
                    eased);

                yield return null;
            }

            RenderSettings.fogColor = targetFog;
            RenderSettings.ambientLight = targetAmbient;
            RenderSettings.fogDensity = targetDensity;
            atmosphereRoutine = null;
        }

        private IEnumerator BlendAtmosphere(int roomIndex)
        {
            int act = roomIndex / RoomsPerAct;

            Color targetFog;
            Color targetAmbient;
            Color targetFloor;
            Color targetWall;
            Color targetTrim;
            Color targetAccent;
            Color targetMetal;
            Color targetRune;
            Color targetMoon;
            float targetDensity;
            float targetMoonIntensity;

            if (act == 0)
            {
                // Mossbound Armory: the original olive / teal dungeon identity.
                targetFog = new Color(0.060f, 0.070f, 0.055f);
                targetAmbient = new Color(0.165f, 0.175f, 0.140f);
                targetFloor = new Color(0.135f, 0.145f, 0.120f);
                targetWall = new Color(0.185f, 0.195f, 0.160f);
                targetTrim = new Color(0.255f, 0.245f, 0.205f);
                targetAccent = new Color(0.185f, 0.325f, 0.300f);
                targetMetal = new Color(0.255f, 0.255f, 0.220f);
                targetRune = new Color(0.205f, 0.440f, 0.390f);
                targetMoon = new Color(0.64f, 0.68f, 0.58f);
                targetDensity = 0.009f;
                targetMoonIntensity = 0.46f;
            }
            else if (act == 1)
            {
                // Iron Vault: cold blue steel cut by brass and furnace amber.
                targetFog = new Color(0.040f, 0.060f, 0.072f);
                targetAmbient = new Color(0.105f, 0.145f, 0.175f);
                targetFloor = new Color(0.090f, 0.115f, 0.135f);
                targetWall = new Color(0.125f, 0.165f, 0.190f);
                targetTrim = new Color(0.340f, 0.265f, 0.135f);
                targetAccent = new Color(0.520f, 0.300f, 0.090f);
                targetMetal = new Color(0.305f, 0.335f, 0.350f);
                targetRune = new Color(0.700f, 0.390f, 0.095f);
                targetMoon = new Color(0.48f, 0.64f, 0.76f);
                targetDensity = 0.012f;
                targetMoonIntensity = 0.53f;
            }
            else
            {
                // Ashen Crypt: charcoal stone, bone highlights and ember red.
                targetFog = new Color(0.072f, 0.025f, 0.022f);
                targetAmbient = new Color(0.180f, 0.080f, 0.065f);
                targetFloor = new Color(0.125f, 0.060f, 0.052f);
                targetWall = new Color(0.185f, 0.085f, 0.072f);
                targetTrim = new Color(0.370f, 0.235f, 0.145f);
                targetAccent = new Color(0.580f, 0.105f, 0.070f);
                targetMetal = new Color(0.245f, 0.150f, 0.125f);
                targetRune = new Color(0.760f, 0.120f, 0.075f);
                targetMoon = new Color(0.84f, 0.34f, 0.22f);
                targetDensity = 0.015f;
                targetMoonIntensity = 0.60f;
            }

            Color startFog = RenderSettings.fogColor;
            Color startAmbient = RenderSettings.ambientLight;
            float startDensity = RenderSettings.fogDensity;
            Color startFloor = floorMaterial.color;
            Color startWall = wallMaterial.color;
            Color startTrim = trimMaterial.color;
            Color startAccent = accentMaterial.color;
            Color startMetal = metalMaterial.color;
            Color startRune = runeMaterial.color;
            Color startMoon =
                moonLight != null
                    ? moonLight.color
                    : targetMoon;
            float startMoonIntensity =
                moonLight != null
                    ? moonLight.intensity
                    : targetMoonIntensity;

            float duration = act == 0 ? 0.85f : 1.80f;

            for (float elapsed = 0f;
                 elapsed < duration;
                 elapsed += Time.deltaTime)
            {
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = t * t * (3f - 2f * t);

                RenderSettings.fogColor =
                    Color.Lerp(startFog, targetFog, eased);
                RenderSettings.ambientLight =
                    Color.Lerp(startAmbient, targetAmbient, eased);
                RenderSettings.fogDensity =
                    Mathf.Lerp(startDensity, targetDensity, eased);

                floorMaterial.color = Color.Lerp(startFloor, targetFloor, eased);
                wallMaterial.color = Color.Lerp(startWall, targetWall, eased);
                trimMaterial.color = Color.Lerp(startTrim, targetTrim, eased);
                accentMaterial.color = Color.Lerp(startAccent, targetAccent, eased);
                metalMaterial.color = Color.Lerp(startMetal, targetMetal, eased);
                runeMaterial.color = Color.Lerp(startRune, targetRune, eased);

                if (moonLight != null)
                {
                    moonLight.color = Color.Lerp(startMoon, targetMoon, eased);
                    moonLight.intensity = Mathf.Lerp(
                        startMoonIntensity,
                        targetMoonIntensity,
                        eased);
                }

                yield return null;
            }

            RenderSettings.fogColor = targetFog;
            RenderSettings.ambientLight = targetAmbient;
            RenderSettings.fogDensity = targetDensity;
            floorMaterial.color = targetFloor;
            wallMaterial.color = targetWall;
            trimMaterial.color = targetTrim;
            accentMaterial.color = targetAccent;
            metalMaterial.color = targetMetal;
            runeMaterial.color = targetRune;

            if (moonLight != null)
            {
                moonLight.color = targetMoon;
                moonLight.intensity = targetMoonIntensity;
            }

            atmosphereRoutine = null;
        }

        private void BuildWorld()
        {
            worldRoot = new GameObject("DungeonWorld").transform;
            worldRoot.SetParent(transform);

            RenderSettings.ambientLight =
                new Color(0.165f, 0.175f, 0.140f);

            RenderSettings.fog = true;
            RenderSettings.fogColor =
                new Color(0.060f, 0.070f, 0.055f);
            RenderSettings.fogDensity = 0.010f;

            GameObject moon = new GameObject("DungeonMoonlight", typeof(Light));
            moon.transform.SetParent(worldRoot);
            moon.transform.rotation = Quaternion.Euler(52f, -34f, 0f);
            moonLight = moon.GetComponent<Light>();
            moonLight.type = LightType.Directional;
            moonLight.intensity = 0.46f;
            moonLight.color =
                new Color(0.64f, 0.68f, 0.58f);
            moonLight.shadows = LightShadows.Hard;
            moonLight.shadowStrength = 0.56f;

            for (int i = 0; i < TotalRooms; i++)
            {
                BuildRoom(i);
                if (i < TotalRooms - 1) BuildCorridor(i);
            }
        }

        private void BuildRoom(int roomIndex)
        {
            Vector3 center = RoomCenter(roomIndex);
            Transform room = new GameObject("Chamber_" + (roomIndex + 1)).transform;
            room.SetParent(worldRoot);
            room.position = center;

            CreateCube(room, "Foundation", new Vector3(0f, -0.55f, 0f),
                new Vector3(18f, 1.1f, 18f), floorMaterial);

            CreateCube(room, "WestWall", new Vector3(-9f, 2.25f, 0f),
                new Vector3(0.72f, 5.6f, 18f), wallMaterial);
            CreateCube(room, "EastWall", new Vector3(9f, 2.25f, 0f),
                new Vector3(0.72f, 5.6f, 18f), wallMaterial);
            CreateCube(room, "Ceiling", new Vector3(0f, 5.25f, 0f),
                new Vector3(18f, 0.55f, 18f), wallMaterial);

            if (roomIndex == 0)
            {
                CreateCube(room, "SouthWall", new Vector3(0f, 2.25f, -9f),
                    new Vector3(18f, 5.6f, 0.72f), wallMaterial);
            }
            else
            {
                BuildSplitWall(room, -9f, "South");

                GameObject entryDoor = CreateDoor(
                    room,
                    "EntrySeal",
                    new Vector3(0f, 1.68f, -9f),
                    IsBossRoom(roomIndex) ? bossRuneMaterial : accentMaterial);

                entryDoor.SetActive(false);
                entryDoors[roomIndex] = entryDoor;
            }

            if (roomIndex < TotalRooms - 1)
            {
                BuildSplitWall(room, 9f, "North");

                GameObject exitDoor = CreateDoor(
                    room,
                    "ExitSeal",
                    new Vector3(0f, 1.68f, 9f),
                    accentMaterial);

                exitDoors[roomIndex] = exitDoor;
            }
            else
            {
                CreateCube(room, "NorthWall", new Vector3(0f, 2.25f, 9f),
                    new Vector3(18f, 5.6f, 0.72f), wallMaterial);
            }

            AddFloorInlays(room, roomIndex);
            AddRoomObstacles(room, roomIndex);
            AddRoomSetPiece(room, roomIndex);
            AddRoomCompositionBlocks(room, roomIndex);
            AddRoomProps(room, roomIndex);
            AddRoomRelic(room, roomIndex);

            // The older presentation stacked wall sigils, beams, rubble and ambient
            // dust in every chamber. The simplified pass intentionally leaves those
            // helpers unused so the room reads as a few strong shapes instead.
            if (!SimpleAnimeVisuals)
            {
                AddWallDetails(room);
                AddWallSigils(room, roomIndex);
                AddCeilingBeams(room, roomIndex);
                AddRubbleDetails(room, roomIndex);
                AddAmbientDust(room, roomIndex);
            }

            AddTorch(room, new Vector3(-8.45f, 2.55f, -5.2f), Quaternion.Euler(0f, 90f, 0f), roomIndex);
            AddTorch(room, new Vector3(8.45f, 2.55f, 5.2f), Quaternion.Euler(0f, -90f, 0f), roomIndex);

            GameObject trigger = new GameObject(
                "RoomTrigger_" + roomIndex,
                typeof(BoxCollider),
                typeof(RoomTrigger));

            trigger.transform.SetParent(room, false);
            trigger.transform.localPosition = new Vector3(0f, 1.5f, 0.7f);

            BoxCollider box = trigger.GetComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(15.2f, 3.0f, 13.8f);
            trigger.GetComponent<RoomTrigger>().roomIndex = roomIndex;
        }

        private void BuildSplitWall(Transform room, float z, string prefix)
        {
            CreateCube(room, prefix + "WallLeft", new Vector3(-5.25f, 2.25f, z),
                new Vector3(7.5f, 5.6f, 0.72f), wallMaterial);
            CreateCube(room, prefix + "WallRight", new Vector3(5.25f, 2.25f, z),
                new Vector3(7.5f, 5.6f, 0.72f), wallMaterial);
            CreateCube(room, prefix + "Lintel", new Vector3(0f, 4.20f, z),
                new Vector3(3.0f, 1.7f, 0.72f), wallMaterial);

            float facing = z > 0f ? -1f : 1f;
            CreateDecorationCube(room, prefix + "FrameLeft",
                new Vector3(-1.78f, 2.05f, z + facing * 0.39f),
                new Vector3(0.34f, 4.25f, 0.30f), trimMaterial);
            CreateDecorationCube(room, prefix + "FrameRight",
                new Vector3(1.78f, 2.05f, z + facing * 0.39f),
                new Vector3(0.34f, 4.25f, 0.30f), trimMaterial);
            CreateDecorationCube(room, prefix + "FrameTop",
                new Vector3(0f, 4.06f, z + facing * 0.39f),
                new Vector3(3.90f, 0.28f, 0.30f), trimMaterial);
        }

        private void BuildCorridor(int roomIndex)
        {
            float z =
                RoomCenter(roomIndex).z + 11f;

            int act =
                roomIndex / RoomsPerAct;

            CreateCube(
                worldRoot,
                "CorridorFloor_" + roomIndex,
                new Vector3(0f, -0.52f, z),
                new Vector3(3.5f, 1.05f, 4f),
                floorMaterial);

            CreateCube(
                worldRoot,
                "CorridorWallL_" + roomIndex,
                new Vector3(-2.0f, 2.1f, z),
                new Vector3(0.65f, 5.2f, 4f),
                wallMaterial);

            CreateCube(
                worldRoot,
                "CorridorWallR_" + roomIndex,
                new Vector3(2.0f, 2.1f, z),
                new Vector3(0.65f, 5.2f, 4f),
                wallMaterial);

            CreateCube(
                worldRoot,
                "CorridorCeiling_" + roomIndex,
                new Vector3(0f, 4.95f, z),
                new Vector3(4.7f, 0.50f, 4f),
                wallMaterial);

            for (int side = -1;
                 side <= 1;
                 side += 2)
            {
                CreateDecorationCube(
                    worldRoot,
                    "CorridorTrim_" +
                    roomIndex +
                    "_" +
                    side,
                    new Vector3(
                        side * 1.65f,
                        0.55f,
                        z),
                    new Vector3(
                        0.18f,
                        0.18f,
                        4f),
                    trimMaterial);
            }

            Material guideMaterial =
                act == 0
                    ? runeMaterial
                    : act == 1
                        ? warmRuneMaterial
                        : bossRuneMaterial;

            CreateDecorationCube(
                worldRoot,
                "CorridorGuideLeft_" + roomIndex,
                new Vector3(-0.78f, 0.025f, z),
                new Vector3(0.055f, 0.025f, 3.55f),
                guideMaterial);

            CreateDecorationCube(
                worldRoot,
                "CorridorGuideRight_" + roomIndex,
                new Vector3(0.78f, 0.025f, z),
                new Vector3(0.055f, 0.025f, 3.55f),
                guideMaterial);

            GameObject guideLightObject =
                new GameObject(
                    "CorridorGuideLight_" + roomIndex,
                    typeof(Light),
                    typeof(TorchFlicker));

            guideLightObject.transform.SetParent(
                worldRoot);

            guideLightObject.transform.position =
                new Vector3(
                    0f,
                    2.5f,
                    z);

            Light guideLight =
                guideLightObject.GetComponent<Light>();

            guideLight.type = LightType.Point;
            guideLight.renderMode =
                LightRenderMode.ForceVertex;

            guideLight.range = 5.5f;
            guideLight.intensity =
                1.05f + act * 0.12f;

            guideLight.color =
                act == 0
                    ? new Color(
                        0.16f,
                        0.52f,
                        0.50f)
                    : act == 1
                        ? new Color(
                            0.68f,
                            0.42f,
                            0.18f)
                        : new Color(
                            0.62f,
                            0.18f,
                            0.12f);

            guideLight.shadows =
                LightShadows.None;
        }

        private void AddFloorInlays(
            Transform room,
            int roomIndex)
        {
            int localRoom =
                roomIndex % RoomsPerAct;

            Material lineMaterial =
                IsBossRoom(roomIndex)
                    ? bossRuneMaterial
                    : localRoom == 1
                        ? warmRuneMaterial
                        : trimMaterial;

            CreateDecorationCube(
                room,
                "FloorAxisLong",
                new Vector3(0f, 0.020f, 0f),
                new Vector3(
                    0.070f,
                    0.024f,
                    15.4f),
                lineMaterial);

            CreateDecorationCube(
                room,
                "FloorAxisCross",
                new Vector3(0f, 0.022f, 0f),
                new Vector3(
                    15.4f,
                    0.024f,
                    0.070f),
                lineMaterial);
        }

        private void AddWallDetails(Transform room)
        {
            CreateDecorationCube(room, "WestBaseTrim",
                new Vector3(-8.58f, 0.55f, 0f),
                new Vector3(0.16f, 0.38f, 17.2f), trimMaterial);
            CreateDecorationCube(room, "EastBaseTrim",
                new Vector3(8.58f, 0.55f, 0f),
                new Vector3(0.16f, 0.38f, 17.2f), trimMaterial);

            CreateDecorationCube(room, "WestUpperTrim",
                new Vector3(-8.58f, 4.25f, 0f),
                new Vector3(0.16f, 0.22f, 17.2f), trimMaterial);
            CreateDecorationCube(room, "EastUpperTrim",
                new Vector3(8.58f, 4.25f, 0f),
                new Vector3(0.16f, 0.22f, 17.2f), trimMaterial);

            for (int i = -2; i <= 2; i++)
            {
                float z = i * 3.2f;
                CreateDecorationCube(room, "WestButtress_" + i,
                    new Vector3(-8.42f, 2.1f, z),
                    new Vector3(0.42f, 4.3f, 0.48f), trimMaterial);
                CreateDecorationCube(room, "EastButtress_" + i,
                    new Vector3(8.42f, 2.1f, z),
                    new Vector3(0.42f, 4.3f, 0.48f), trimMaterial);
            }
        }

        private void AddWallSigils(Transform room, int roomIndex)
        {
            Material sigilMaterial = roomIndex == 0
                ? runeMaterial
                : roomIndex == 1
                    ? warmRuneMaterial
                    : bossRuneMaterial;

            float[] zPositions = { -4.9f, 4.9f };

            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < zPositions.Length; i++)
                {
                    float x = side * 8.53f;
                    float z = zPositions[i];

                    Transform vertical = CreateDecorationCube(
                        room,
                        "WallSigilV_" + side + "_" + i,
                        new Vector3(x, 2.45f, z),
                        new Vector3(0.045f, 1.18f, 0.12f),
                        sigilMaterial).transform;

                    vertical.localRotation = Quaternion.Euler(
                        0f,
                        0f,
                        side * (i == 0 ? 8f : -8f));

                    CreateDecorationCube(
                        room,
                        "WallSigilH_" + side + "_" + i,
                        new Vector3(x, 2.45f, z),
                        new Vector3(0.048f, 0.12f, 0.78f),
                        sigilMaterial);

                    GameObject core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    core.name = "WallSigilCore_" + side + "_" + i;
                    core.transform.SetParent(room, false);
                    core.transform.localPosition = new Vector3(
                        x - side * 0.055f,
                        2.45f,
                        z);
                    core.transform.localScale = Vector3.one * 0.18f;

                    Collider collider = core.GetComponent<Collider>();
                    if (collider != null)
                    {
                        Destroy(collider);
                    }

                    core.GetComponent<Renderer>().sharedMaterial = sigilMaterial;
                }
            }
        }

        private void AddCeilingBeams(Transform room, int roomIndex)
        {
            Material beamMaterial = roomIndex == TotalRooms - 1 ? metalMaterial : trimMaterial;

            for (int i = -1; i <= 1; i++)
            {
                float z = i * 5.1f;

                CreateDecorationCube(
                    room,
                    "CeilingBeam_" + i,
                    new Vector3(0f, 4.88f, z),
                    new Vector3(17.1f, 0.24f, 0.42f),
                    beamMaterial);

                CreateDecorationCube(
                    room,
                    "BeamDropLeft_" + i,
                    new Vector3(-7.75f, 4.25f, z),
                    new Vector3(0.28f, 1.25f, 0.46f),
                    beamMaterial);

                CreateDecorationCube(
                    room,
                    "BeamDropRight_" + i,
                    new Vector3(7.75f, 4.25f, z),
                    new Vector3(0.28f, 1.25f, 0.46f),
                    beamMaterial);
            }
        }

        private void AddAmbientDust(Transform room, int roomIndex)
        {
            GameObject dustObject = new GameObject("AmbientDust");
            dustObject.transform.SetParent(room, false);
            dustObject.transform.localPosition = new Vector3(0f, 2.2f, 0f);

            ParticleSystem particles = dustObject.AddComponent<ParticleSystem>();

            Color dustColor = roomIndex == TotalRooms - 1
                ? new Color(0.70f, 0.20f, 0.12f, 0.25f)
                : roomIndex == 1
                    ? new Color(0.54f, 0.42f, 0.25f, 0.22f)
                    : new Color(0.30f, 0.48f, 0.58f, 0.20f);

            ParticleSystem.MainModule main = particles.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(5.0f, 9.0f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.015f, 0.075f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.012f, 0.038f);
            main.startColor = dustColor;
            main.maxParticles = 80;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 7f;

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(14.5f, 3.4f, 14.5f);

            ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.y = new ParticleSystem.MinMaxCurve(0.015f, 0.055f);

            ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.shadowCastingMode =
                UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            CombatEffects.ApplyParticleMaterial(
                particles,
                dustColor);
        }

        private void AddRoomObstacles(
            Transform room,
            int roomIndex)
        {
            int act =
                roomIndex / RoomsPerAct;

            int localRoom =
                roomIndex % RoomsPerAct;

            Vector3 left;
            Vector3 right;
            float scale;

            if (localRoom == 0)
            {
                left =
                    new Vector3(
                        -5.2f + act * 0.25f,
                        0f,
                        -2.9f + act * 0.55f);

                right =
                    new Vector3(
                        5.2f - act * 0.25f,
                        0f,
                        2.9f - act * 0.55f);

                scale =
                    0.96f + act * 0.05f;
            }
            else if (localRoom == 1)
            {
                left =
                    new Vector3(
                        -5.8f,
                        0f,
                        3.25f - act * 0.35f);

                right =
                    new Vector3(
                        5.8f,
                        0f,
                        -3.25f + act * 0.35f);

                scale =
                    0.86f + act * 0.04f;
            }
            else
            {
                left =
                    new Vector3(
                        -6.0f,
                        0f,
                        -4.25f + act * 0.25f);

                right =
                    new Vector3(
                        6.0f,
                        0f,
                        4.25f - act * 0.25f);

                scale =
                    0.78f + act * 0.03f;
            }

            BuildPillar(
                room,
                "Pillar_Left",
                left,
                scale);

            BuildPillar(
                room,
                "Pillar_Right",
                right,
                scale * 0.96f);
        }

        private void BuildPillar(Transform room, string name, Vector3 position, float scale)
        {
            Transform root = new GameObject(name).transform;
            root.SetParent(room, false);
            root.localPosition = position;

            CreateCube(
                root,
                "Monolith",
                new Vector3(0f, 1.12f, 0f),
                new Vector3(1.12f, 2.24f, 1.12f) * scale,
                wallMaterial);

            CreateCube(
                root,
                "Cap",
                new Vector3(0f, 2.27f, 0f),
                new Vector3(1.32f, 0.14f, 1.32f) * scale,
                trimMaterial);
        }

        private void AddRubbleDetails(Transform room, int roomIndex)
        {
            Vector3[] clusterCenters;

            if (roomIndex == 0)
            {
                clusterCenters = new[]
                {
                    new Vector3(-6.6f, 0f, 5.9f),
                    new Vector3(6.4f, 0f, -5.6f)
                };
            }
            else if (roomIndex == 1)
            {
                clusterCenters = new[]
                {
                    new Vector3(-6.3f, 0f, -5.7f),
                    new Vector3(6.2f, 0f, 5.8f),
                    new Vector3(5.9f, 0f, -5.9f)
                };
            }
            else
            {
                clusterCenters = new[]
                {
                    new Vector3(-6.5f, 0f, -5.7f),
                    new Vector3(6.3f, 0f, -5.8f),
                    new Vector3(-6.1f, 0f, 5.8f),
                    new Vector3(6.4f, 0f, 5.6f)
                };
            }

            for (int clusterIndex = 0; clusterIndex < clusterCenters.Length; clusterIndex++)
            {
                Vector3 center = clusterCenters[clusterIndex];

                for (int i = 0; i < 4; i++)
                {
                    float side = i % 2 == 0 ? -1f : 1f;
                    float forward = i < 2 ? -1f : 1f;

                    Vector3 offset = new Vector3(
                        side * (0.18f + i * 0.07f),
                        0.10f + i * 0.045f,
                        forward * (0.12f + i * 0.05f));

                    Transform rubble = CreateDecorationCube(
                        room,
                        "Rubble_" + clusterIndex + "_" + i,
                        center + offset,
                        new Vector3(
                            0.42f + i * 0.07f,
                            0.18f + (i % 2) * 0.12f,
                            0.36f + (3 - i) * 0.055f),
                        i % 3 == 0 ? trimMaterial : wallMaterial).transform;

                    rubble.localRotation = Quaternion.Euler(
                        (i * 9f) % 24f,
                        clusterIndex * 31f + i * 23f,
                        side * (6f + i * 3f));
                }
            }

            if (roomIndex == TotalRooms - 1)
            {
                CreateDecorationCube(
                    room,
                    "FallenBeamLeft",
                    new Vector3(-6.8f, 0.42f, 1.7f),
                    new Vector3(3.2f, 0.32f, 0.46f),
                    trimMaterial).transform.localRotation = Quaternion.Euler(0f, 23f, -7f);

                CreateDecorationCube(
                    room,
                    "FallenBeamRight",
                    new Vector3(6.6f, 0.36f, -1.8f),
                    new Vector3(2.8f, 0.28f, 0.42f),
                    trimMaterial).transform.localRotation = Quaternion.Euler(0f, -19f, 6f);
            }
        }

        private void AddRoomSetPiece(
            Transform room,
            int roomIndex)
        {
            int localRoom =
                roomIndex % RoomsPerAct;

            if (localRoom == 0)
            {
                CreateRune(
                    room,
                    new Vector3(0f, 0.045f, 1.0f),
                    2.15f,
                    runeMaterial,
                    false);
            }
            else if (localRoom == 1)
            {
                CreateRune(
                    room,
                    new Vector3(0f, 0.045f, 0f),
                    2.6f,
                    warmRuneMaterial,
                    false);

                int act =
                    roomIndex / RoomsPerAct;

                float hazardRadius =
                    act == 0
                        ? 2.45f
                        : act == 1
                            ? 2.25f
                            : 2.85f;

                roomHazards[roomIndex] =
                    CreateRuneHazard(
                        room,
                        new Vector3(0f, 0.06f, 0f),
                        hazardRadius,
                        16f + roomIndex,
                        act);

                CreateDecorationCube(
                    room,
                    "RelicPlinth",
                    new Vector3(0f, 0.18f, 4.9f),
                    new Vector3(2.8f, 0.34f, 1.8f),
                    trimMaterial);
            }
            else
            {
                CreateRune(
                    room,
                    new Vector3(0f, 0.05f, 0f),
                    4.0f,
                    bossRuneMaterial,
                    true);

                CreateDecorationCube(
                    room,
                    "BossDaisOuter",
                    new Vector3(0f, 0.045f, 0f),
                    new Vector3(6.3f, 0.08f, 6.3f),
                    trimMaterial);
            }
        }

        private void AddRoomCompositionBlocks(
            Transform room,
            int roomIndex)
        {
            int act =
                roomIndex / RoomsPerAct;

            int localRoom =
                roomIndex % RoomsPerAct;

            const float y = 0.034f;

            if (act == 0)
            {
                if (localRoom == 0)
                {
                    CreateDecorationCube(
                        room,
                        "ArmoryFloorPatchL",
                        new Vector3(-3.9f, y, 1.1f),
                        new Vector3(3.4f, 0.028f, 1.7f),
                        woodMaterial);

                    CreateDecorationCube(
                        room,
                        "ArmoryFloorPatchR",
                        new Vector3(3.9f, y, -1.4f),
                        new Vector3(3.4f, 0.028f, 1.7f),
                        woodMaterial);
                }
                else if (localRoom == 1)
                {
                    CreateDecorationCube(
                        room,
                        "CellarRunner",
                        new Vector3(0f, y, 3.8f),
                        new Vector3(5.8f, 0.028f, 1.55f),
                        woodMaterial);
                }
                else
                {
                    for (int side = -1;
                         side <= 1;
                         side += 2)
                    {
                        CreateDecorationCube(
                            room,
                            "BurrowSealPatch_" + side,
                            new Vector3(
                                side * 4.9f,
                                y,
                                0f),
                            new Vector3(
                                1.5f,
                                0.028f,
                                5.4f),
                            woodMaterial);
                    }
                }
            }
            else if (act == 1)
            {
                if (localRoom == 0)
                {
                    CreateDecorationCube(
                        room,
                        "GearLane",
                        new Vector3(0f, y, 0f),
                        new Vector3(1.05f, 0.030f, 12.4f),
                        rustMaterial);

                    for (int side = -1;
                         side <= 1;
                         side += 2)
                    {
                        CreateDecorationCube(
                            room,
                            "GearPlate_" + side,
                            new Vector3(
                                side * 4.5f,
                                y,
                                1.5f),
                            new Vector3(
                                2.4f,
                                0.030f,
                                2.4f),
                            metalMaterial);
                    }
                }
                else if (localRoom == 1)
                {
                    Vector3[] pads =
                    {
                        new Vector3(-4.2f, y, -3.9f),
                        new Vector3(4.2f, y, -3.9f),
                        new Vector3(-4.2f, y, 3.9f),
                        new Vector3(4.2f, y, 3.9f)
                    };

                    for (int i = 0;
                         i < pads.Length;
                         i++)
                    {
                        CreateDecorationCube(
                            room,
                            "MachinePad_" + i,
                            pads[i],
                            new Vector3(
                                1.7f,
                                0.030f,
                                1.7f),
                            rustMaterial);
                    }
                }
                else
                {
                    CreateDecorationCube(
                        room,
                        "IronSealBarX",
                        new Vector3(0f, y, 0f),
                        new Vector3(11.5f, 0.030f, 0.85f),
                        rustMaterial);

                    CreateDecorationCube(
                        room,
                        "IronSealBarZ",
                        new Vector3(0f, y + 0.002f, 0f),
                        new Vector3(0.85f, 0.030f, 11.5f),
                        rustMaterial);
                }
            }
            else
            {
                if (localRoom == 0)
                {
                    for (int i = -1;
                         i <= 1;
                         i++)
                    {
                        CreateDecorationCube(
                            room,
                            "CryptSlab_" + i,
                            new Vector3(
                                i * 3.25f,
                                y,
                                1.2f - Mathf.Abs(i) * 1.3f),
                            new Vector3(
                                1.45f,
                                0.030f,
                                3.1f),
                            boneMaterial);
                    }
                }
                else if (localRoom == 1)
                {
                    CreateDecorationCube(
                        room,
                        "AshPath",
                        new Vector3(0f, y, 3.7f),
                        new Vector3(5.2f, 0.030f, 1.25f),
                        trimMaterial);

                    CreateDecorationCube(
                        room,
                        "AshPatch",
                        new Vector3(-4.6f, y, -3.2f),
                        new Vector3(2.1f, 0.030f, 2.1f),
                        boneMaterial);
                }
                else
                {
                    Vector3[] sealCorners =
                    {
                        new Vector3(-4.8f, y, -4.8f),
                        new Vector3(4.8f, y, -4.8f),
                        new Vector3(-4.8f, y, 4.8f),
                        new Vector3(4.8f, y, 4.8f)
                    };

                    for (int i = 0;
                         i < sealCorners.Length;
                         i++)
                    {
                        CreateDecorationCube(
                            room,
                            "AshenSealCorner_" + i,
                            sealCorners[i],
                            new Vector3(
                                1.7f,
                                0.030f,
                                1.7f),
                            boneMaterial);
                    }
                }
            }
        }

        private void AddRoomProps(
            Transform room,
            int roomIndex)
        {
            int act =
                roomIndex / RoomsPerAct;

            int localRoom =
                roomIndex % RoomsPerAct;

            if (act == 0 &&
                localRoom == 0)
            {
                // Armory: large readable 2D prop silhouettes.
                for (int side = -1;
                     side <= 1;
                     side += 2)
                {
                    CreatePixelProp(
                        room,
                        "WeaponRack_" + side,
                        new Vector3(
                            side * 6.35f,
                            0.05f,
                            2.9f),
                        PixelPropKind.WeaponRack,
                        1.08f,
                        side < 0);

                    CreatePixelProp(
                        room,
                        "ArmoryCrates_" + side,
                        new Vector3(
                            side * 5.95f,
                            0.05f,
                            -3.35f),
                        PixelPropKind.CrateStack,
                        0.96f,
                        side > 0);
                }
            }
            else if (act == 0 &&
                     localRoom == 1)
            {
                // Storage cellar: barrel and crate clusters.
                for (int side = -1;
                     side <= 1;
                     side += 2)
                {
                    CreatePixelProp(
                        room,
                        "BarrelStack_" + side,
                        new Vector3(
                            side * 5.85f,
                            0.05f,
                            -2.95f),
                        PixelPropKind.BarrelStack,
                        0.92f,
                        side < 0);

                    CreatePixelProp(
                        room,
                        "CellarCrates_" + side,
                        new Vector3(
                            side * 6.05f,
                            0.05f,
                            3.75f),
                        PixelPropKind.CrateStack,
                        0.88f,
                        side > 0);
                }
            }
            else if (act == 0)
            {
                // Burrow seal: sparse broken stores around the arena edge.
                for (int side = -1;
                     side <= 1;
                     side += 2)
                {
                    CreatePixelProp(
                        room,
                        "BrokenStore_" + side,
                        new Vector3(
                            side * 6.25f,
                            0.05f,
                            -3.85f),
                        PixelPropKind.BrokenCrate,
                        0.92f,
                        side < 0);
                }
            }
            else if (act == 1 &&
                     localRoom == 0)
            {
                AddMachineProps(
                    room,
                    false);
            }
            else if (act == 1 &&
                     localRoom == 1)
            {
                AddMachineProps(
                    room,
                    true);

                for (int side = -1;
                     side <= 1;
                     side += 2)
                {
                    CreatePixelProp(
                        room,
                        "MachineChest_" + side,
                        new Vector3(
                            side * 5.95f,
                            0.05f,
                            4.45f),
                        PixelPropKind.MachineChest,
                        0.94f,
                        side < 0);
                }
            }
            else if (act == 1)
            {
                // Iron seal: leave the center open and keep machinery at the rim.
                AddMachineProps(
                    room,
                    false);
            }
            else if (localRoom == 0)
            {
                // Bone crypt.
                for (int side = -1;
                     side <= 1;
                     side += 2)
                {
                    CreatePixelProp(
                        room,
                        "Sarcophagus_" + side,
                        new Vector3(
                            side * 5.95f,
                            0.05f,
                            3.45f),
                        PixelPropKind.Sarcophagus,
                        1.02f,
                        side < 0);

                    AddBonePile(
                        room,
                        side,
                        -3.35f);
                }
            }
            else if (localRoom == 1)
            {
                // Ash cellar: urns and bones become broad sprite clusters.
                for (int side = -1;
                     side <= 1;
                     side += 2)
                {
                    CreatePixelProp(
                        room,
                        "UrnCluster_" + side,
                        new Vector3(
                            side * 5.85f,
                            0.05f,
                            -3.35f),
                        PixelPropKind.UrnCluster,
                        0.96f,
                        side > 0);

                    AddBonePile(
                        room,
                        side,
                        3.75f);
                }
            }
            else
            {
                // Final seal: only a few pale relic silhouettes.
                for (int side = -1;
                     side <= 1;
                     side += 2)
                {
                    CreatePixelProp(
                        room,
                        "ThroneRelic_" + side,
                        new Vector3(
                            side * 6.15f,
                            0.05f,
                            4.05f),
                        PixelPropKind.ThroneRelic,
                        0.98f,
                        side < 0);

                    AddBonePile(
                        room,
                        side,
                        -4.05f);
                }
            }
        }

        private void AddMachineProps(
            Transform room,
            bool dense)
        {
            for (int side = -1;
                 side <= 1;
                 side += 2)
            {
                float x =
                    side * 6.15f;

                CreatePixelProp(
                    room,
                    "WallGear_" + side,
                    new Vector3(
                        x,
                        0.12f,
                        3.55f),
                    PixelPropKind.Gear,
                    dense ? 1.12f : 0.96f,
                    side < 0);

                CreatePixelProp(
                    room,
                    "PipeBank_" + side,
                    new Vector3(
                        x,
                        0.05f,
                        -3.55f),
                    PixelPropKind.PipeBank,
                    dense ? 1.05f : 0.90f,
                    side > 0);

                if (dense)
                {
                    CreatePixelProp(
                        room,
                        "SecondaryGear_" + side,
                        new Vector3(
                            side * 5.15f,
                            0.05f,
                            0.55f),
                        PixelPropKind.Gear,
                        0.72f,
                        side > 0);
                }
            }
        }

        private void AddBonePile(
            Transform room,
            int side,
            float z)
        {
            CreatePixelProp(
                room,
                "BonePile_" +
                side +
                "_" +
                z,
                new Vector3(
                    side * 5.65f,
                    0.05f,
                    z),
                PixelPropKind.BonePile,
                0.82f,
                side < 0);
        }

        private GameObject CreatePixelProp(
            Transform parent,
            string name,
            Vector3 localPosition,
            PixelPropKind kind,
            float scale,
            bool flipX)
        {
            GameObject go =
                new GameObject(
                    name,
                    typeof(SpriteRenderer),
                    typeof(PixelBillboard));

            go.transform.SetParent(
                parent,
                false);

            go.transform.localPosition =
                localPosition;

            go.transform.localScale =
                Vector3.one *
                Mathf.Max(
                    0.10f,
                    scale);

            SpriteRenderer renderer =
                go.GetComponent<SpriteRenderer>();

            renderer.sprite =
                PixelPropFactory.Get(kind);

            renderer.flipX = flipX;
            renderer.sortingOrder = 1;

            return go;
        }

        private void AddRoomRelic(
            Transform room,
            int roomIndex)
        {
            if (roomIndex % RoomsPerAct != 1)
            {
                return;
            }

            int act =
                roomIndex / RoomsPerAct;

            GameObject relic =
                new GameObject(
                    "Act" +
                    (act + 1) +
                    "_Relic",
                    typeof(DungeonRelicPickup));

            relic.transform.SetParent(
                room,
                false);

            relic.transform.localPosition =
                new Vector3(
                    0f,
                    0.88f,
                    4.9f);

            relic.GetComponent<DungeonRelicPickup>()
                .Configure(act);
        }

        private GameObject CreateDecorationPrimitive(
            Transform parent,
            PrimitiveType type,
            string name,
            Vector3 localPosition,
            Vector3 localScale,
            Material material)
        {
            GameObject go =
                GameObject.CreatePrimitive(type);

            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;
            go.GetComponent<Renderer>().sharedMaterial = material;

            Collider collider =
                go.GetComponent<Collider>();

            if (collider != null)
            {
                Destroy(collider);
            }

            return go;
        }

        private RuneHazard CreateRuneHazard(
            Transform parent,
            Vector3 localPosition,
            float radius,
            float damage,
            int style)
        {
            GameObject root =
                new GameObject("RuneHazard");

            root.transform.SetParent(
                parent,
                false);

            root.transform.localPosition =
                localPosition;

            RuneHazard hazard =
                root.AddComponent<RuneHazard>();

            hazard.Configure(
                radius,
                damage,
                style);

            return hazard;
        }

        private void CreateRune(Transform parent, Vector3 localPosition, float size, Material material, bool boss)
        {
            GameObject rune = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rune.name = boss ? "BossRune" : "ChamberRune";
            rune.transform.SetParent(parent, false);
            rune.transform.localPosition = localPosition;
            rune.transform.localScale = new Vector3(size, 0.018f, size);

            Collider collider = rune.GetComponent<Collider>();
            if (collider != null) Destroy(collider);

            Renderer runeRenderer = rune.GetComponent<Renderer>();
            runeRenderer.sharedMaterial = material;
            runeRenderer.shadowCastingMode =
                UnityEngine.Rendering.ShadowCastingMode.Off;
            runeRenderer.receiveShadows = false;

            GameObject pulse = new GameObject("RuneLight", typeof(Light));
            pulse.transform.SetParent(rune.transform, false);
            pulse.transform.localPosition = new Vector3(0f, 2.2f, 0f);

            Light light = pulse.GetComponent<Light>();
            light.type = LightType.Point;
            light.renderMode = boss
                ? LightRenderMode.ForcePixel
                : LightRenderMode.ForceVertex;
            light.range = boss ? 7f : 4.5f;
            light.intensity = boss ? 1.2f : 0.65f;
            light.color = boss ? new Color(0.90f, 0.14f, 0.08f) : new Color(0.15f, 0.62f, 0.72f);

            rune.AddComponent<RunePulse>();
        }

        private void AddTorch(
            Transform room,
            Vector3 localPosition,
            Quaternion localRotation,
            int roomIndex)
        {
            Transform root =
                new GameObject(
                    "WallTorch")
                .transform;

            root.SetParent(
                room,
                false);

            root.localPosition =
                localPosition;

            root.localRotation =
                localRotation;

            CreateDecorationCube(
                root,
                "Bracket",
                new Vector3(
                    0f,
                    -0.10f,
                    0.22f),
                new Vector3(
                    0.18f,
                    0.18f,
                    0.55f),
                metalMaterial);

            CreateDecorationCube(
                root,
                "Stem",
                new Vector3(
                    0f,
                    0.12f,
                    -0.02f),
                new Vector3(
                    0.12f,
                    0.60f,
                    0.12f),
                metalMaterial);

            bool bossFlame =
                IsBossRoom(roomIndex);

            GameObject flame =
                new GameObject(
                    "PixelFlame",
                    typeof(SpriteRenderer),
                    typeof(PixelBillboard),
                    typeof(PixelFlameFlicker));

            flame.transform.SetParent(
                root,
                false);

            flame.transform.localPosition =
                new Vector3(
                    0f,
                    0.43f,
                    -0.04f);

            flame.transform.localScale =
                Vector3.one * 0.44f;

            SpriteRenderer flameRenderer =
                flame.GetComponent<SpriteRenderer>();

            flameRenderer.sprite =
                PixelPropFactory.Get(
                    PixelPropKind.TorchFlame);

            flameRenderer.sortingOrder = 3;

            flame.GetComponent<PixelFlameFlicker>()
                .Configure(bossFlame);

            Color flameColor =
                bossFlame
                    ? new Color(
                        0.72f,
                        0.20f,
                        0.08f)
                    : new Color(
                        0.82f,
                        0.48f,
                        0.16f);

            GameObject lightObject =
                new GameObject(
                    "TorchLight",
                    typeof(Light),
                    typeof(TorchFlicker));

            lightObject.transform.SetParent(
                root,
                false);

            lightObject.transform.localPosition =
                new Vector3(
                    0f,
                    0.55f,
                    -0.04f);

            Light light =
                lightObject.GetComponent<Light>();

            light.type =
                LightType.Point;

            light.range =
                bossFlame
                    ? 5.8f
                    : 5.2f;

            light.intensity =
                bossFlame
                    ? 1.28f
                    : 1.12f;

            light.color =
                flameColor;

            light.shadows =
                LightShadows.None;

            light.renderMode =
                LightRenderMode.ForceVertex;

            light.shadowStrength = 0f;
        }

        private Material GetBoonFocusMaterial()
        {
            if (boonFocusMaterial != null)
            {
                return boonFocusMaterial;
            }

            boonFocusMaterial = MakeMaterial(
                new Color(0.10f, 0.56f, 0.66f),
                0.10f,
                0.48f,
                new Color(0.16f, 0.72f, 0.88f) * 2.7f);

            boonFocusMaterial.name = "RuntimeBoonFocus";
            return boonFocusMaterial;
        }

        private GameObject CreateDoor(Transform parent, string name, Vector3 localPosition, Material material)
        {
            GameObject door = CreateCube(parent, name, localPosition,
                new Vector3(3.25f, 3.35f, 0.52f), material);

            door.AddComponent<DungeonDoor>();

            CreateDecorationCube(door.transform, "DoorBandTop",
                new Vector3(0f, 0.34f, -0.56f),
                new Vector3(0.86f, 0.08f, 0.12f), metalMaterial);

            CreateDecorationCube(door.transform, "DoorBandBottom",
                new Vector3(0f, -0.34f, -0.56f),
                new Vector3(0.86f, 0.08f, 0.12f), metalMaterial);

            GameObject unlockGuide = CreateDecorationCube(
                door.transform,
                "UnlockGuide",
                new Vector3(0f, 0f, -0.575f),
                new Vector3(0.10f, 0.78f, 0.08f),
                runeMaterial);

            Renderer guideRenderer =
                unlockGuide.GetComponent<Renderer>();
            guideRenderer.shadowCastingMode =
                UnityEngine.Rendering.ShadowCastingMode.Off;
            guideRenderer.receiveShadows = false;

            GameObject guideLightObject = new GameObject(
                "UnlockGuideLight",
                typeof(Light));

            guideLightObject.transform.SetParent(
                unlockGuide.transform,
                false);

            Light guideLight = guideLightObject.GetComponent<Light>();
            guideLight.type = LightType.Point;
            guideLight.renderMode = LightRenderMode.ForceVertex;
            guideLight.range = 2.4f;
            guideLight.intensity = 0.2f;
            guideLight.color = new Color(0.18f, 0.78f, 0.86f);
            guideLight.shadows = LightShadows.None;

            unlockGuide.SetActive(false);

            return door;
        }

        private void OpenDoor(GameObject door, bool highlightGuide)
        {
            if (door == null) return;

            DungeonDoor animatedDoor = door.GetComponent<DungeonDoor>();
            if (animatedDoor != null)
            {
                animatedDoor.Open(highlightGuide);
            }
            else
            {
                door.SetActive(false);
            }
        }

        private void CreatePlayer()
        {
            GameObject player = new GameObject("Player");
            player.tag = "Player";
            player.layer = 2;
            player.transform.SetParent(transform);
            player.transform.position = new Vector3(0f, 0.10f, -7.35f);

            CapsuleCollider capsule = player.AddComponent<CapsuleCollider>();
            capsule.height = 1.85f;
            capsule.radius = 0.42f;
            capsule.center = new Vector3(0f, 0.92f, 0f);

            Rigidbody body = player.AddComponent<Rigidbody>();
            body.mass = 1f;
            body.constraints = RigidbodyConstraints.FreezeRotation;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;

            GameObject cameraGo = new GameObject("PlayerCamera", typeof(Camera), typeof(AudioListener));
            cameraGo.tag = "MainCamera";
            cameraGo.layer = 2;
            cameraGo.transform.SetParent(player.transform, false);
            cameraGo.transform.localPosition = new Vector3(0f, 1.62f, 0f);

            Camera camera = cameraGo.GetComponent<Camera>();
            camera.nearClipPlane = 0.035f;
            camera.farClipPlane = 85f;
            camera.fieldOfView = 76f;
            camera.allowHDR = true;
            camera.allowMSAA = true;
            camera.backgroundColor = new Color(0.015f, 0.02f, 0.028f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.cullingMask &= ~(1 << 2);

            GameObject weaponCameraObject = new GameObject(
                "WeaponCamera",
                typeof(Camera));

            weaponCameraObject.layer = 2;
            weaponCameraObject.transform.SetParent(
                cameraGo.transform,
                false);

            Camera weaponCamera = weaponCameraObject.GetComponent<Camera>();
            weaponCamera.clearFlags = CameraClearFlags.Depth;
            weaponCamera.cullingMask = 1 << 2;
            weaponCamera.depth = camera.depth + 1f;
            weaponCamera.fieldOfView = 68f;
            weaponCamera.nearClipPlane = 0.01f;
            weaponCamera.farClipPlane = 5f;
            weaponCamera.allowHDR = false;
            weaponCamera.allowMSAA = true;

            playerController = player.AddComponent<FirstPersonController>();
            playerCombat = player.AddComponent<PlayerCombat>();
            playerHealth = player.AddComponent<PlayerHealth>();
        }

        private EnemyController SpawnEnemy(
            Vector3 position,
            int roomIndex,
            int variant,
            bool boss)
        {
            GameObject go;

            if (enemyPrefab != null)
            {
                go = Instantiate(
                    enemyPrefab,
                    position,
                    Quaternion.identity,
                    worldRoot);
            }
            else
            {
                go = new GameObject(
                    "EnemyRuntimeFallback");

                go.transform.SetParent(worldRoot);
                go.transform.position = position;
                go.AddComponent<EnemyController>();
            }

            go.name = boss
                ? "Act" +
                  (roomIndex / RoomsPerAct + 1) +
                  "_" +
                  BossNameForRoom(roomIndex)
                : "Room" +
                  (roomIndex + 1) +
                  "_Enemy_" +
                  variant;

            EnemyController enemy =
                go.GetComponent<EnemyController>();

            if (enemy == null)
            {
                enemy =
                    go.AddComponent<EnemyController>();
            }

            enemy.Configure(
                roomIndex,
                variant,
                boss);

            if (boss)
            {
                BossActMechanics mechanics =
                    go.GetComponent<BossActMechanics>();

                if (mechanics == null)
                {
                    mechanics =
                        go.AddComponent<BossActMechanics>();
                }

                mechanics.Configure(
                    roomIndex / RoomsPerAct,
                    enemy,
                    playerHealth);
            }

            return enemy;
        }

        private GameObject CreateCube(
            Transform parent,
            string name,
            Vector3 localPosition,
            Vector3 localScale,
            Material material)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = localPosition;
            cube.transform.localScale = localScale;
            cube.GetComponent<Renderer>().sharedMaterial = material;
            return cube;
        }

        private GameObject CreateDecorationCube(
            Transform parent,
            string name,
            Vector3 localPosition,
            Vector3 localScale,
            Material material)
        {
            GameObject cube = CreateCube(parent, name, localPosition, localScale, material);
            Collider collider = cube.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            return cube;
        }

        private void BuildMaterials()
        {
            // Flat, desaturated surfaces support the restrained pixel-dungeon look.
            // Specular response stays deliberately low so the room reads in broad blocks.
            floorMaterial = MakeMaterial(
                new Color(0.135f, 0.145f, 0.120f),
                0.01f,
                0.08f,
                Color.black);

            wallMaterial = MakeMaterial(
                new Color(0.185f, 0.195f, 0.160f),
                0.01f,
                0.06f,
                Color.black);

            trimMaterial = MakeMaterial(
                new Color(0.255f, 0.245f, 0.205f),
                0.04f,
                0.12f,
                Color.black);

            accentMaterial = MakeMaterial(
                new Color(0.185f, 0.325f, 0.300f),
                0.02f,
                0.12f,
                new Color(0.025f, 0.080f, 0.075f));

            metalMaterial = MakeMaterial(
                new Color(0.255f, 0.255f, 0.220f),
                0.18f,
                0.18f,
                Color.black);

            runeMaterial = MakeMaterial(
                new Color(0.205f, 0.440f, 0.390f),
                0.01f,
                0.10f,
                new Color(0.040f, 0.180f, 0.155f));

            warmRuneMaterial = MakeMaterial(
                new Color(0.555f, 0.355f, 0.155f),
                0.01f,
                0.10f,
                new Color(0.220f, 0.095f, 0.020f));

            bossRuneMaterial = MakeMaterial(
                new Color(0.505f, 0.155f, 0.110f),
                0.01f,
                0.10f,
                new Color(0.235f, 0.040f, 0.025f));

            woodMaterial = MakeMaterial(
                new Color(0.29f, 0.18f, 0.11f),
                0.01f,
                0.06f,
                Color.black);

            boneMaterial = MakeMaterial(
                new Color(0.62f, 0.59f, 0.50f),
                0.01f,
                0.05f,
                Color.black);

            rustMaterial = MakeMaterial(
                new Color(0.34f, 0.25f, 0.15f),
                0.10f,
                0.08f,
                Color.black);

            PixelDungeonTextureFactory.ApplyDungeonTextures(
                floorMaterial,
                wallMaterial,
                trimMaterial,
                woodMaterial,
                rustMaterial);
        }

        private static Material MakeMaterial(Color color, float metallic, float smoothness, Color emission)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Simple Lit");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            Material material = new Material(shader);
            material.color = color;
            material.enableInstancing = true;

            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", metallic);
            }

            if (material.HasProperty("_Glossiness"))
            {
                material.SetFloat("_Glossiness", smoothness);
            }

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", smoothness);
            }

            if (emission.maxColorComponent > 0.001f && material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission);
            }

            return material;
        }

        private static int EncounterCountForRoom(
            int roomIndex)
        {
            int[] encounterBudgets =
            {
                4, 5, 3,
                5, 6, 4,
                6, 6, 5
            };

            if (roomIndex < 0 ||
                roomIndex >=
                encounterBudgets.Length)
            {
                return 4;
            }

            return encounterBudgets[roomIndex];
        }

        private static bool IsBossRoom(int roomIndex)
        {
            return roomIndex % RoomsPerAct ==
                   RoomsPerAct - 1;
        }

        private static int ArchetypeVariantForRoom(
            int roomIndex,
            int spawnIndex,
            int waveNumber)
        {
            int slot =
                spawnIndex % 3;

            switch (roomIndex)
            {
                case 0:
                    // Old Armory / Pack Hunt:
                    // first wave stays grounded, second wave gets faster.
                    if (waveNumber <= 1)
                    {
                        return slot == 0
                            ? (int)EnemyArchetype.Mole
                            : slot == 1
                                ? (int)EnemyArchetype.Slime
                                : (int)EnemyArchetype.Mole;
                    }

                    return slot == 0
                        ? (int)EnemyArchetype.Bat
                        : slot == 1
                            ? (int)EnemyArchetype.Mole
                            : (int)EnemyArchetype.Bat;

                case 1:
                    // Storage Cellar / Relic Cellar.
                    return waveNumber <= 1
                        ? slot == 0
                            ? (int)EnemyArchetype.Slime
                            : slot == 1
                                ? (int)EnemyArchetype.Mole
                                : (int)EnemyArchetype.Slime
                        : slot == 0
                            ? (int)EnemyArchetype.Armored
                            : slot == 1
                                ? (int)EnemyArchetype.Mole
                                : (int)EnemyArchetype.Slime;

                case 2:
                    return slot == 0
                        ? (int)EnemyArchetype.Armored
                        : slot == 1
                            ? (int)EnemyArchetype.Skeleton
                            : (int)EnemyArchetype.Bat;

                case 3:
                    // Gear Passage / Iron Formation.
                    return waveNumber <= 1
                        ? slot == 0
                            ? (int)EnemyArchetype.Armored
                            : slot == 1
                                ? (int)EnemyArchetype.Mole
                                : (int)EnemyArchetype.Armored
                        : slot == 0
                            ? (int)EnemyArchetype.Mech
                            : slot == 1
                                ? (int)EnemyArchetype.Armored
                                : (int)EnemyArchetype.Mech;

                case 4:
                    // Machine Vault.
                    return waveNumber <= 1
                        ? slot == 0
                            ? (int)EnemyArchetype.Mech
                            : slot == 1
                                ? (int)EnemyArchetype.Slime
                                : (int)EnemyArchetype.Armored
                        : slot == 0
                            ? (int)EnemyArchetype.Bat
                            : slot == 1
                                ? (int)EnemyArchetype.Mech
                                : (int)EnemyArchetype.Slime;

                case 5:
                    return slot == 0
                        ? (int)EnemyArchetype.Mech
                        : slot == 1
                            ? (int)EnemyArchetype.Armored
                            : (int)EnemyArchetype.Skeleton;

                case 6:
                    // Bone Crypt / Crypt Swarm.
                    return waveNumber <= 1
                        ? slot == 0
                            ? (int)EnemyArchetype.Skeleton
                            : slot == 1
                                ? (int)EnemyArchetype.Slime
                                : (int)EnemyArchetype.Skeleton
                        : slot == 0
                            ? (int)EnemyArchetype.Bat
                            : slot == 1
                                ? (int)EnemyArchetype.Skeleton
                                : (int)EnemyArchetype.Bat;

                case 7:
                    // Ash Cellar.
                    return waveNumber <= 1
                        ? slot == 0
                            ? (int)EnemyArchetype.Slime
                            : slot == 1
                                ? (int)EnemyArchetype.Skeleton
                                : (int)EnemyArchetype.Slime
                        : slot == 0
                            ? (int)EnemyArchetype.Bat
                            : slot == 1
                                ? (int)EnemyArchetype.Skeleton
                                : (int)EnemyArchetype.Armored;

                default:
                    return waveNumber <= 1
                        ? slot == 0
                            ? (int)EnemyArchetype.Armored
                            : slot == 1
                                ? (int)EnemyArchetype.Skeleton
                                : (int)EnemyArchetype.Mech
                        : slot == 0
                            ? (int)EnemyArchetype.Mech
                            : slot == 1
                                ? (int)EnemyArchetype.Bat
                                : (int)EnemyArchetype.Armored;
            }
        }

        private static Vector3 EncounterSpawnPosition(
            int roomIndex,
            int waveNumber,
            int index,
            int count,
            Vector3 center)
        {
            float t =
                count <= 1
                    ? 0.5f
                    : index /
                      (float)(count - 1);

            switch (roomIndex)
            {
                case 0:
                {
                    // Pack Hunt: two shallow arcs that close from opposite sides.
                    float angle =
                        Mathf.Lerp(
                            -0.78f,
                            0.78f,
                            t);

                    float facing =
                        waveNumber <= 1
                            ? 0f
                            : Mathf.PI;

                    float radius =
                        waveNumber <= 1
                            ? 3.45f
                            : 4.55f;

                    return center +
                        new Vector3(
                            Mathf.Sin(
                                angle + facing) *
                            radius,
                            0.05f,
                            Mathf.Cos(
                                angle + facing) *
                            radius);
                }

                case 1:
                {
                    // Cellar: enemies emerge from the storage sides.
                    float side =
                        index % 2 == 0
                            ? -1f
                            : 1f;

                    float z =
                        Mathf.Lerp(
                            -3.2f,
                            3.2f,
                            t);

                    return center +
                        new Vector3(
                            side *
                            (waveNumber <= 1
                                ? 4.6f
                                : 5.4f),
                            0.05f,
                            z);
                }

                case 2:
                    return RingSpawn(
                        center,
                        index,
                        count,
                        4.25f,
                        0.55f);

                case 3:
                {
                    // Gear Passage: a readable opposing line formation.
                    float side =
                        waveNumber <= 1
                            ? -1f
                            : 1f;

                    return center +
                        new Vector3(
                            side * 4.85f,
                            0.05f,
                            Mathf.Lerp(
                                -3.6f,
                                3.6f,
                                t));
                }

                case 4:
                {
                    // Machine Vault: alternate corners around the central rune.
                    Vector3[] corners =
                    {
                        new Vector3(-4.7f, 0.05f, -4.2f),
                        new Vector3(4.7f, 0.05f, -4.2f),
                        new Vector3(-4.7f, 0.05f, 4.2f),
                        new Vector3(4.7f, 0.05f, 4.2f)
                    };

                    int offset =
                        waveNumber <= 1
                            ? 0
                            : 2;

                    return center +
                        corners[
                            (index + offset) %
                            corners.Length];
                }

                case 5:
                {
                    // Iron Seal: guardians occupy the four axes.
                    Vector3[] cross =
                    {
                        new Vector3(-5.0f, 0.05f, 0f),
                        new Vector3(5.0f, 0.05f, 0f),
                        new Vector3(0f, 0.05f, -5.0f),
                        new Vector3(0f, 0.05f, 5.0f)
                    };

                    return center +
                        cross[
                            index %
                            cross.Length];
                }

                case 6:
                {
                    // Crypt Swarm: tighter semicircles make the room feel crowded.
                    float angle =
                        Mathf.Lerp(
                            -1.05f,
                            1.05f,
                            t) +
                        (waveNumber <= 1
                            ? Mathf.PI * 0.25f
                            : Mathf.PI * 1.25f);

                    float radius =
                        waveNumber <= 1
                            ? 3.35f
                            : 4.15f;

                    return center +
                        new Vector3(
                            Mathf.Cos(angle) *
                            radius,
                            0.05f,
                            Mathf.Sin(angle) *
                            radius);
                }

                case 7:
                {
                    // Ash Cellar: asymmetrical corner pressure around the relic.
                    Vector3[] ashPoints =
                    {
                        new Vector3(-5.1f, 0.05f, -3.9f),
                        new Vector3(4.4f, 0.05f, -4.5f),
                        new Vector3(-4.2f, 0.05f, 4.6f),
                        new Vector3(5.0f, 0.05f, 3.4f),
                        new Vector3(0f, 0.05f, -5.1f)
                    };

                    int offset =
                        waveNumber <= 1
                            ? 0
                            : 2;

                    return center +
                        ashPoints[
                            (index + offset) %
                            ashPoints.Length];
                }

                default:
                {
                    // Final Seal: wide perimeter, never directly on the dais.
                    Vector3[] finalPoints =
                    {
                        new Vector3(-5.25f, 0.05f, -5.0f),
                        new Vector3(5.25f, 0.05f, -5.0f),
                        new Vector3(-5.25f, 0.05f, 5.0f),
                        new Vector3(5.25f, 0.05f, 5.0f),
                        new Vector3(0f, 0.05f, -5.5f),
                        new Vector3(0f, 0.05f, 5.5f)
                    };

                    int offset =
                        waveNumber <= 1
                            ? 0
                            : 3;

                    return center +
                        finalPoints[
                            (index + offset) %
                            finalPoints.Length];
                }
            }
        }

        private static Vector3 RingSpawn(
            Vector3 center,
            int index,
            int count,
            float radius,
            float phase)
        {
            float angle =
                index *
                Mathf.PI *
                2f /
                Mathf.Max(
                    1,
                    count) +
                phase;

            return center +
                new Vector3(
                    Mathf.Cos(angle) * radius,
                    0.05f,
                    Mathf.Sin(angle) * radius);
        }

        private static float WaveDelayForRoom(
            int roomIndex,
            int waveNumber)
        {
            switch (roomIndex)
            {
                case 0:
                    return 0.54f;
                case 3:
                    return 0.88f;
                case 4:
                    return 0.76f;
                case 6:
                    return 0.58f;
                case 7:
                    return 0.62f;
                case 8:
                    return 0.86f;
                default:
                    return 0.70f;
            }
        }

        private static string EncounterRuleLabel(
            int roomIndex)
        {
            switch (roomIndex)
            {
                case 0:
                    return "PACK HUNT";
                case 1:
                    return "RELIC CELLAR";
                case 2:
                    return "BURROW GUARD";
                case 3:
                    return "IRON FORMATION";
                case 4:
                    return "MACHINE VAULT";
                case 5:
                    return "FORGE GUARD";
                case 6:
                    return "CRYPT SWARM";
                case 7:
                    return "ASH RELIC";
                default:
                    return "FINAL SEAL";
            }
        }

        private static string BossNameForRoom(int roomIndex)
        {
            int act =
                roomIndex / RoomsPerAct;

            switch (act)
            {
                case 0:
                    return "BURROW WARDEN";
                case 1:
                    return "IRON WARDEN";
                default:
                    return "ASHEN WARDEN";
            }
        }

        private static Vector3 RoomCenter(int index)
        {
            return new Vector3(
                0f,
                0f,
                index * 22f);
        }
    }

    public class RoomTrigger : MonoBehaviour
    {
        public int roomIndex;
        private bool fired;

        private void OnTriggerEnter(Collider other)
        {
            if (fired || !other.CompareTag("Player")) return;

            fired = true;
            if (DungeonDirector.Instance != null)
            {
                DungeonDirector.Instance.StartRoom(roomIndex);
            }
        }
    }
}
