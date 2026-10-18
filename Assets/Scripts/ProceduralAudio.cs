using UnityEngine;

namespace Depthbound
{
    public class ProceduralAudio : MonoBehaviour
    {
        public static ProceduralAudio Instance { get; private set; }

        private const int SpatialPoolSize = 8;

        private AudioSource sfxSource;
        private AudioSource musicSource;
        private AudioSource tensionSource;
        private AudioSource[] spatialSources;
        private int spatialSourceCursor;
        private float stageMusicVolume = 0.16f;
        private float stageTensionVolume;
        private float targetMusicPitch = 1f;
        private float targetTensionPitch = 1f;
        private bool musicDucked;
        private AudioClip daggerAttackClip;
        private AudioClip swordAttackClip;
        private AudioClip hitClip;
        private AudioClip daggerHitClip;
        private AudioClip swordHitClip;
        private AudioClip hurtClip;
        private AudioClip boostClip;
        private AudioClip clearClip;
        private AudioClip blockClip;
        private AudioClip switchClip;
        private AudioClip enemyAttackClip;
        private AudioClip footstepAClip;
        private AudioClip footstepBClip;
        private AudioClip gateOpenClip;
        private AudioClip gateCloseClip;
        private AudioClip enemyDeathClip;
        private AudioClip bossDeathClip;
        private AudioClip boonClip;
        private AudioClip guardBreakClip;
        private AudioClip soulCollectClip;
        private AudioClip bossEnrageClip;
        private AudioClip bossAwakenClip;
        private AudioClip bossSlamClip;
        private AudioClip runeChargeClip;
        private AudioClip runeBurstClip;
        private AudioClip parryClip;
        private AudioClip heartbeatClip;
        private AudioClip uiClickClip;
        private AudioClip landingClip;
        private AudioClip enemySpawnClip;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            RuntimeSettings.ApplyAudio();

            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.spatialBlend = 0f;

            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.spatialBlend = 0f;
            musicSource.volume = 0f;

            tensionSource = gameObject.AddComponent<AudioSource>();
            tensionSource.loop = true;
            tensionSource.spatialBlend = 0f;
            tensionSource.volume = 0f;

            BuildSpatialPool();

            daggerAttackClip = MakeSweep("DaggerSlash", 420f, 140f, 0.09f, 0.25f);
            swordAttackClip = MakeSweep("SwordSlash", 240f, 75f, 0.16f, 0.33f);
            hitClip = MakeTone("Hit", 90f, 0.11f, 0.55f, true);
            daggerHitClip = MakeTone("DaggerHit", 175f, 0.075f, 0.34f, true);
            swordHitClip = MakeTone("SwordHit", 68f, 0.135f, 0.60f, true);
            hurtClip = MakeTone("Hurt", 70f, 0.16f, 0.45f, true);
            boostClip = MakeSweep("Boost", 120f, 360f, 0.16f, 0.32f);
            clearClip = MakeSweep("Clear", 380f, 760f, 0.32f, 0.24f);
            blockClip = MakeTone("Block", 720f, 0.09f, 0.22f, true);
            switchClip = MakeSweep("Switch", 190f, 290f, 0.08f, 0.16f);
            enemyAttackClip = MakeSweep("EnemyAttack", 120f, 55f, 0.15f, 0.22f);
            footstepAClip = MakeTone("FootstepA", 82f, 0.07f, 0.16f, true);
            footstepBClip = MakeTone("FootstepB", 94f, 0.065f, 0.14f, true);
            gateOpenClip = MakeSweep("GateOpen", 88f, 42f, 0.30f, 0.23f);
            gateCloseClip = MakeSweep("GateClose", 44f, 96f, 0.24f, 0.22f);
            enemyDeathClip = MakeSweep("EnemyDeath", 115f, 44f, 0.19f, 0.24f);
            bossDeathClip = MakeSweep("BossDeath", 90f, 24f, 0.44f, 0.34f);
            boonClip = MakeSweep("Boon", 320f, 680f, 0.28f, 0.17f);
            guardBreakClip = MakeSweep("GuardBreak", 620f, 95f, 0.24f, 0.28f);
            soulCollectClip = MakeSweep("SoulCollect", 520f, 920f, 0.18f, 0.14f);
            bossEnrageClip = MakeSweep("BossEnrage", 140f, 42f, 0.52f, 0.32f);
            bossAwakenClip = MakeSweep("BossAwaken", 72f, 190f, 0.62f, 0.30f);
            bossSlamClip = MakeTone("BossSlam", 46f, 0.32f, 0.52f, true);
            runeChargeClip = MakeSweep("RuneCharge", 180f, 520f, 0.42f, 0.14f);
            runeBurstClip = MakeTone("RuneBurst", 58f, 0.22f, 0.42f, true);
            parryClip = MakeSweep("Parry", 980f, 420f, 0.11f, 0.24f);
            heartbeatClip = MakeTone("Heartbeat", 52f, 0.14f, 0.22f, false);
            uiClickClip = MakeSweep("UIClick", 460f, 620f, 0.055f, 0.12f);
            landingClip = MakeTone("Landing", 66f, 0.10f, 0.22f, true);
            enemySpawnClip = MakeSweep("EnemySpawn", 210f, 72f, 0.26f, 0.18f);

            musicSource.clip = MakeAmbient();
            tensionSource.clip = MakeTensionAmbient();

            musicSource.Play();
            tensionSource.Play();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            float blend = 1f - Mathf.Exp(-3.6f * dt);
            float pitchBlend = 1f - Mathf.Exp(-4.4f * dt);
            float duck = musicDucked ? 0.52f : 1f;

            if (musicSource != null)
            {
                musicSource.volume = Mathf.Lerp(
                    musicSource.volume,
                    stageMusicVolume * duck,
                    blend);

                musicSource.pitch = Mathf.Lerp(
                    musicSource.pitch,
                    targetMusicPitch,
                    pitchBlend);
            }

            if (tensionSource != null)
            {
                tensionSource.volume = Mathf.Lerp(
                    tensionSource.volume,
                    stageTensionVolume * duck,
                    blend);

                tensionSource.pitch = Mathf.Lerp(
                    tensionSource.pitch,
                    targetTensionPitch,
                    pitchBlend);
            }
        }

        public void Attack(WeaponMode mode) => Play(mode == WeaponMode.Dagger ? daggerAttackClip : swordAttackClip, 0.78f);
        public void Hit() => Play(hitClip, 0.9f);
        public void Hit(WeaponMode mode) =>
            Hit(mode, false);

        public void Hit(
            WeaponMode mode,
            bool emphasized)
        {
            Play(
                mode == WeaponMode.Dagger
                    ? daggerHitClip
                    : swordHitClip,
                emphasized ? 1.0f : 0.92f);

            if (emphasized)
            {
                Play(hitClip, 0.62f);
            }
        }
        public void Hurt() => Play(hurtClip, 0.8f);
        public void Boost() => Play(boostClip, 0.8f);
        public void Clear() => Play(clearClip, 1f);
        public void Block() => Play(blockClip, 0.95f);
        public void SwitchWeapon() => Play(switchClip, 0.6f);
        public void EnemyAttack() => Play(enemyAttackClip, 0.65f);
        public void EnemyAttack(
            Vector3 position,
            bool boss = false,
            bool fastVariant = false,
            bool heavyVariant = false) =>
            PlaySpatial(
                enemyAttackClip,
                position,
                boss
                    ? 0.84f
                    : heavyVariant ? 0.80f : fastVariant ? 0.58f : 0.66f,
                boss
                    ? 0.88f
                    : heavyVariant ? 0.80f : fastVariant ? 1.18f : 0.98f);
        public void Footstep(bool alternate) => Play(alternate ? footstepBClip : footstepAClip, 0.42f);
        public void GateOpen() => Play(gateOpenClip, 0.72f);
        public void GateOpen(Vector3 position) => PlaySpatial(gateOpenClip, position, 0.78f, 0.95f);
        public void GateClose() => Play(gateCloseClip, 0.68f);
        public void GateClose(Vector3 position) => PlaySpatial(gateCloseClip, position, 0.76f, 0.92f);
        public void EnemyDeath(bool boss) => Play(boss ? bossDeathClip : enemyDeathClip, boss ? 0.92f : 0.64f);
        public void EnemyDeath(Vector3 position, bool boss) =>
            PlaySpatial(
                boss ? bossDeathClip : enemyDeathClip,
                position,
                boss ? 1.0f : 0.68f,
                boss ? 0.86f : 1.0f);
        public void Boon() => Play(boonClip, 0.82f);
        public void GuardBreak() => Play(guardBreakClip, 0.90f);
        public void SoulCollect() => Play(soulCollectClip, 0.72f);
        public void BossEnrage() => Play(bossEnrageClip, 0.95f);
        public void BossAwaken(Vector3 position) => PlaySpatial(bossAwakenClip, position, 0.96f, 0.92f);
        public void BossSlam(Vector3 position) => PlaySpatial(bossSlamClip, position, 1.0f, 0.82f);
        public void RuneCharge() => Play(runeChargeClip, 0.70f);
        public void RuneCharge(Vector3 position) => PlaySpatial(runeChargeClip, position, 0.76f, 1.0f);
        public void RuneBurst() => Play(runeBurstClip, 0.86f);
        public void RuneBurst(Vector3 position) => PlaySpatial(runeBurstClip, position, 0.92f, 0.88f);
        public void Parry() => Play(parryClip, 0.92f);
        public void Heartbeat() => Play(heartbeatClip, 0.34f);
        public void UIClick() => Play(uiClickClip, 0.48f);
        public void Land(float strength) =>
            Play(
                landingClip,
                Mathf.Lerp(0.20f, 0.58f, Mathf.Clamp01(strength)));

        public void EnemySpawn(Vector3 position, bool boss, bool fastVariant) =>
            PlaySpatial(
                enemySpawnClip,
                position,
                boss ? 0.74f : 0.46f,
                boss ? 0.72f : fastVariant ? 1.18f : 0.96f);

        public void SetMenuMix()
        {
            stageMusicVolume = 0.105f;
            stageTensionVolume = 0f;
            targetMusicPitch = 0.94f;
            targetTensionPitch = 0.94f;
            musicDucked = false;
        }

        public void SetDungeonStage(int roomIndex, bool combat)
        {
            if (musicSource == null) return;

            float depth = Mathf.Clamp01(roomIndex / 2f);
            stageMusicVolume = combat
                ? Mathf.Lerp(0.17f, 0.235f, depth)
                : Mathf.Lerp(0.13f, 0.16f, depth);

            stageTensionVolume = combat
                ? Mathf.Lerp(0.045f, 0.090f, depth)
                : Mathf.Lerp(0.0f, 0.018f, depth);

            float targetPitch = combat
                ? Mathf.Lerp(1.0f, 1.075f, depth)
                : Mathf.Lerp(0.96f, 1.0f, depth);

            targetMusicPitch = targetPitch;
            targetTensionPitch = combat
                ? Mathf.Lerp(1.0f, 1.08f, depth)
                : 0.96f;
        }

        public void SetBossEnraged()
        {
            stageMusicVolume = Mathf.Max(stageMusicVolume, 0.255f);
            stageTensionVolume = Mathf.Max(stageTensionVolume, 0.12f);

            targetMusicPitch = 1.10f;
            targetTensionPitch = 1.12f;
        }

        public void SetMusicDucked(bool ducked)
        {
            musicDucked = ducked;
        }

        private void BuildSpatialPool()
        {
            spatialSources = new AudioSource[SpatialPoolSize];

            for (int i = 0; i < SpatialPoolSize; i++)
            {
                GameObject sourceObject = new GameObject(
                    "SpatialSFX_" + i);

                sourceObject.transform.SetParent(
                    transform,
                    false);

                AudioSource source = sourceObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0.88f;
                source.rolloffMode = AudioRolloffMode.Logarithmic;
                source.minDistance = 1.7f;
                source.maxDistance = 15f;
                source.dopplerLevel = 0f;

                spatialSources[i] = source;
            }
        }

        private void Play(AudioClip clip, float volume)
        {
            if (clip != null && sfxSource != null)
            {
                sfxSource.PlayOneShot(clip, volume);
            }
        }

        private void PlaySpatial(
            AudioClip clip,
            Vector3 position,
            float volume,
            float pitch)
        {
            if (clip == null ||
                spatialSources == null ||
                spatialSources.Length == 0)
            {
                return;
            }

            AudioSource source =
                spatialSources[spatialSourceCursor];

            spatialSourceCursor =
                (spatialSourceCursor + 1) %
                spatialSources.Length;

            if (source == null)
            {
                return;
            }

            source.Stop();
            source.transform.position = position;
            source.clip = clip;
            source.volume = volume;
            source.pitch = Mathf.Clamp(pitch, 0.5f, 2f);
            source.Play();
        }

        private static AudioClip MakeTone(string name, float frequency, float duration, float volume, bool noise)
        {
            const int sampleRate = 22050;
            int sampleCount = Mathf.CeilToInt(duration * sampleRate);
            float[] data = new float[sampleCount];
            System.Random rng = new System.Random(name.GetHashCode());

            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)sampleRate;
                float env = 1f - i / (float)sampleCount;
                float sample = Mathf.Sin(2f * Mathf.PI * frequency * t);
                if (noise) sample = sample * 0.55f + ((float)rng.NextDouble() * 2f - 1f) * 0.45f;
                data[i] = sample * env * volume;
            }

            AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip MakeSweep(string name, float startFrequency, float endFrequency, float duration, float volume)
        {
            const int sampleRate = 22050;
            int sampleCount = Mathf.CeilToInt(duration * sampleRate);
            float[] data = new float[sampleCount];
            float phase = 0f;

            for (int i = 0; i < sampleCount; i++)
            {
                float p = i / (float)sampleCount;
                float frequency = Mathf.Lerp(startFrequency, endFrequency, p);
                phase += 2f * Mathf.PI * frequency / sampleRate;
                float env = Mathf.Sin(Mathf.PI * p);
                data[i] = Mathf.Sin(phase) * env * volume;
            }

            AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip MakeTensionAmbient()
        {
            const int sampleRate = 22050;
            const float duration = 8f;
            int sampleCount = Mathf.CeilToInt(duration * sampleRate);
            float[] data = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)sampleRate;

                float beat = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 1.35f * t);
                beat = Mathf.Pow(beat, 7f);

                float sub =
                    Mathf.Sin(2f * Mathf.PI * 41.25f * t) *
                    (0.022f + beat * 0.055f);

                float metallic =
                    Mathf.Sin(2f * Mathf.PI * 165f * t) *
                    beat *
                    0.014f;

                float breath =
                    Mathf.Sin(2f * Mathf.PI * 0.17f * t) *
                    0.008f;

                data[i] = sub + metallic + breath;
            }

            AudioClip clip = AudioClip.Create(
                "DungeonTension",
                sampleCount,
                1,
                sampleRate,
                false);

            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip MakeAmbient()
        {
            const int sampleRate = 22050;
            const float duration = 8f;
            int sampleCount = Mathf.CeilToInt(duration * sampleRate);
            float[] data = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)sampleRate;
                float drone = Mathf.Sin(2f * Mathf.PI * 55f * t) * 0.045f;
                float upper = Mathf.Sin(2f * Mathf.PI * 82.5f * t) * 0.022f;
                float distant = Mathf.Sin(2f * Mathf.PI * 27.5f * t) * 0.018f;
                float pulse = 0.62f + 0.38f * Mathf.Sin(2f * Mathf.PI * 0.25f * t);
                data[i] = (drone + upper + distant) * pulse;
            }

            AudioClip clip = AudioClip.Create("DungeonAmbient", sampleCount, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
