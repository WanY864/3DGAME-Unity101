using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Depthbound
{
    public class BossActMechanics : MonoBehaviour
    {
        private EnemyController boss;
        private PlayerHealth playerHealth;
        private Transform player;
        private Rigidbody playerBody;
        private int actIndex;
        private bool configured;
        private Material warningMaterial;
        private Rigidbody bossBody;
        private Collider[] bossColliders;
        private bool[] bossColliderStates;
        private Renderer[] bossRenderers;
        private bool[] bossRendererStates;

        private readonly List<GameObject> activeWarnings =
            new List<GameObject>();

        public void Configure(
            int act,
            EnemyController bossController,
            PlayerHealth health)
        {
            actIndex = Mathf.Clamp(act, 0, 2);
            boss = bossController;
            playerHealth = health;

            if (health != null)
            {
                player = health.transform;
                playerBody = health.GetComponent<Rigidbody>();
            }

            if (boss != null)
            {
                bossBody = boss.GetComponent<Rigidbody>();
            }

            warningMaterial = BuildWarningMaterial(actIndex);
            configured = true;
            StartCoroutine(MechanicLoop());
        }

        private void OnDestroy()
        {
            RestoreBossPresentation();

            for (int i = 0;
                 i < activeWarnings.Count;
                 i++)
            {
                if (activeWarnings[i] != null)
                {
                    Destroy(activeWarnings[i]);
                }
            }

            activeWarnings.Clear();

            if (warningMaterial != null)
            {
                Destroy(warningMaterial);
            }
        }

        private IEnumerator MechanicLoop()
        {
            yield return new WaitForSeconds(2.8f);

            while (configured &&
                   boss != null &&
                   !boss.IsDead &&
                   playerHealth != null)
            {
                if (HUDController.Instance != null)
                {
                    HUDController.Instance.ShowBossMechanicWarning(
                        actIndex);
                }

                switch (actIndex)
                {
                    case 0:
                        yield return BurrowAmbush();
                        break;

                    case 1:
                        yield return IronLockdown();
                        break;

                    default:
                        yield return AshenPursuit();
                        break;
                }

                if (boss == null || boss.IsDead)
                {
                    yield break;
                }

                float delay;

                if (actIndex == 0)
                {
                    delay = boss.IsEnraged ? 2.75f : 3.85f;
                }
                else if (actIndex == 1)
                {
                    delay = boss.IsEnraged ? 5.60f : 7.20f;
                }
                else
                {
                    delay = boss.IsEnraged ? 1.65f : 2.45f;
                }

                yield return new WaitForSeconds(delay);
            }
        }

        private IEnumerator BurrowAmbush()
        {
            if (boss == null ||
                boss.IsDead ||
                player == null)
            {
                yield break;
            }

            SetBossControl(true);

            Vector3 origin = boss.transform.position;
            float groundY = origin.y;
            Vector3 initialDirection =
                player.position - origin;
            initialDirection.y = 0f;

            if (initialDirection.sqrMagnitude < 0.01f)
            {
                initialDirection = boss.transform.forward;
            }

            initialDirection.Normalize();

            GameObject departure =
                CreatePixelRing(
                    "BurrowDeparture",
                    new Vector3(
                        origin.x,
                        0.035f,
                        origin.z),
                    2.2f,
                    12);

            if (ProceduralAudio.Instance != null)
            {
                ProceduralAudio.Instance.RuneCharge(origin);
            }

            const float sinkTime = 0.30f;

            for (float elapsed = 0f;
                 elapsed < sinkTime;
                 elapsed += Time.deltaTime)
            {
                if (boss == null || boss.IsDead)
                {
                    RestoreBossPresentation();
                    yield break;
                }

                float t = Mathf.Clamp01(
                    elapsed / sinkTime);
                float stepped =
                    Mathf.Floor(t * 5f) / 5f;

                if (departure != null)
                {
                    SetPixelRingRadius(
                        departure.transform,
                        Mathf.Lerp(
                            2.2f,
                            0.35f,
                            stepped),
                        0.34f);
                }

                yield return null;
            }

            if (departure != null)
            {
                Destroy(departure);
            }

            CacheBossPresentation();
            SetBossRenderers(false);
            SetBossColliders(false);

            float travelTime =
                boss.IsEnraged
                    ? 0.46f
                    : 0.62f;

            GameObject arrival =
                CreatePixelRing(
                    "BurrowArrival",
                    new Vector3(
                        origin.x,
                        0.035f,
                        origin.z),
                    0.35f,
                    12);

            Vector3 destination = origin;

            for (float elapsed = 0f;
                 elapsed < travelTime;
                 elapsed += Time.deltaTime)
            {
                if (boss == null || boss.IsDead)
                {
                    RestoreBossPresentation();
                    if (arrival != null) Destroy(arrival);
                    yield break;
                }

                Vector3 towardPlayer =
                    player.position -
                    boss.transform.position;
                towardPlayer.y = 0f;

                if (towardPlayer.sqrMagnitude > 0.01f)
                {
                    initialDirection =
                        towardPlayer.normalized;
                }

                destination =
                    player.position -
                    initialDirection * 1.35f;
                destination.y = groundY;

                float t = Mathf.Clamp01(
                    elapsed / travelTime);
                float stepped =
                    Mathf.Floor(t * 7f) / 7f;
                Vector3 undergroundPosition =
                    Vector3.Lerp(
                        origin,
                        destination,
                        stepped);

                MoveBoss(undergroundPosition);

                if (arrival != null)
                {
                    arrival.transform.position =
                        new Vector3(
                            destination.x,
                            0.035f,
                            destination.z);

                    SetPixelRingRadius(
                        arrival.transform,
                        Mathf.Lerp(
                            0.35f,
                            2.25f,
                            stepped),
                        0.34f);
                }

                yield return null;
            }

            MoveBoss(destination);

            Vector3 face =
                player.position -
                destination;
            face.y = 0f;

            if (face.sqrMagnitude > 0.01f)
            {
                boss.transform.rotation =
                    Quaternion.LookRotation(
                        face.normalized,
                        Vector3.up);
            }

            SetBossRenderers(true);
            SetBossColliders(true);

            CombatEffects.SpawnRuneBurst(
                destination + Vector3.up * 0.05f,
                2.1f);

            ResolveRadialDamage(
                destination,
                boss.IsEnraged ? 2.45f : 2.15f,
                boss.IsEnraged ? 18f : 15f,
                5.2f);

            if (ProceduralAudio.Instance != null)
            {
                ProceduralAudio.Instance.RuneBurst(
                    destination);
            }

            if (arrival != null)
            {
                Destroy(arrival);
            }

            if (bossBody != null &&
                face.sqrMagnitude > 0.01f)
            {
                bossBody.AddForce(
                    face.normalized *
                    (boss.IsEnraged ? 4.8f : 4.1f),
                    ForceMode.Impulse);
            }

            yield return new WaitForSeconds(0.12f);
            SetBossControl(false);
        }

        private IEnumerator IronLockdown()
        {
            if (boss == null || boss.IsDead)
            {
                yield break;
            }

            SetBossControl(true);

            Vector3 center = boss.transform.position;
            center.y = 0.045f;

            const float length = 13.5f;
            const float width = 1.45f;

            GameObject horizontal =
                CreateBoxWarning(
                    "IronLockdownX",
                    center,
                    new Vector3(
                        0.25f,
                        0.035f,
                        width));

            GameObject vertical =
                CreateBoxWarning(
                    "IronLockdownZ",
                    center,
                    new Vector3(
                        width,
                        0.035f,
                        0.25f));

            if (ProceduralAudio.Instance != null)
            {
                ProceduralAudio.Instance.RuneCharge(center);
            }

            float warningTime =
                boss.IsEnraged
                    ? 0.78f
                    : 1.05f;

            for (float elapsed = 0f;
                 elapsed < warningTime;
                 elapsed += Time.deltaTime)
            {
                if (boss == null || boss.IsDead)
                {
                    if (horizontal != null) Destroy(horizontal);
                    if (vertical != null) Destroy(vertical);
                    RestoreBossPresentation();
                    yield break;
                }

                float t =
                    Mathf.Clamp01(
                        elapsed / warningTime);
                float stepped =
                    Mathf.Floor(t * 7f) / 7f;
                float laneLength =
                    Mathf.Lerp(
                        0.25f,
                        length,
                        stepped);

                if (horizontal != null)
                {
                    horizontal.transform.localScale =
                        new Vector3(
                            laneLength,
                            0.035f,
                            width);
                }

                if (vertical != null)
                {
                    vertical.transform.localScale =
                        new Vector3(
                            width,
                            0.035f,
                            laneLength);
                }

                yield return null;
            }

            if (ProceduralAudio.Instance != null)
            {
                ProceduralAudio.Instance.RuneBurst(center);
            }

            CombatEffects.SpawnRuneBurst(
                center + Vector3.up * 0.05f,
                3.2f);

            SetBossControl(false);

            float activeTime =
                boss.IsEnraged
                    ? 4.8f
                    : 4.2f;
            float nextDamageTime = 0f;

            for (float elapsed = 0f;
                 elapsed < activeTime;
                 elapsed += Time.deltaTime)
            {
                if (boss == null || boss.IsDead)
                {
                    break;
                }

                float steppedPulse =
                    Mathf.Floor(elapsed * 8f) % 2f < 1f
                        ? 0.88f
                        : 1f;

                if (horizontal != null)
                {
                    horizontal.transform.localScale =
                        new Vector3(
                            length,
                            0.035f,
                            width * steppedPulse);
                }

                if (vertical != null)
                {
                    vertical.transform.localScale =
                        new Vector3(
                            width * steppedPulse,
                            0.035f,
                            length);
                }

                if (player != null &&
                    Time.time >= nextDamageTime)
                {
                    Vector3 delta =
                        player.position -
                        center;
                    delta.y = 0f;

                    bool inHorizontal =
                        Mathf.Abs(delta.z) <=
                            width * 0.58f &&
                        Mathf.Abs(delta.x) <=
                            length * 0.55f;

                    bool inVertical =
                        Mathf.Abs(delta.x) <=
                            width * 0.58f &&
                        Mathf.Abs(delta.z) <=
                            length * 0.55f;

                    if (inHorizontal || inVertical)
                    {
                        nextDamageTime =
                            Time.time + 0.92f;

                        ApplyPlayerDamage(
                            boss.IsEnraged ? 12f : 10f,
                            center,
                            2.8f);
                    }
                }

                yield return null;
            }

            if (horizontal != null) Destroy(horizontal);
            if (vertical != null) Destroy(vertical);
        }

        private IEnumerator AshenPursuit()
        {
            if (boss == null ||
                boss.IsDead ||
                player == null)
            {
                yield break;
            }

            SetBossControl(false);

            int strikes =
                boss.IsEnraged
                    ? 7
                    : 5;

            for (int i = 0; i < strikes; i++)
            {
                if (boss == null ||
                    boss.IsDead ||
                    player == null)
                {
                    yield break;
                }

                Vector3 target = player.position;
                target.y = 0.04f;

                float radius =
                    boss.IsEnraged
                        ? 1.75f
                        : 1.55f;

                GameObject marker =
                    CreatePixelTarget(
                        "AshenPursuitMark_" + i,
                        target,
                        radius);

                if (ProceduralAudio.Instance != null)
                {
                    ProceduralAudio.Instance.RuneCharge(
                        target);
                }

                float trackingTime =
                    boss.IsEnraged
                        ? 0.34f
                        : 0.48f;

                for (float elapsed = 0f;
                     elapsed < trackingTime;
                     elapsed += Time.deltaTime)
                {
                    if (boss == null || boss.IsDead)
                    {
                        if (marker != null) Destroy(marker);
                        yield break;
                    }

                    target = player.position;
                    target.y = 0.04f;

                    if (marker != null)
                    {
                        marker.transform.position =
                            target;

                        float t = Mathf.Clamp01(
                            elapsed / trackingTime);
                        float stepped =
                            Mathf.Floor(t * 6f) / 6f;

                        marker.transform.localScale =
                            Vector3.one *
                            Mathf.Lerp(
                                0.72f,
                                1f,
                                stepped);
                    }

                    yield return null;
                }

                float lockTime =
                    boss.IsEnraged
                        ? 0.14f
                        : 0.20f;

                for (float elapsed = 0f;
                     elapsed < lockTime;
                     elapsed += Time.deltaTime)
                {
                    if (marker != null)
                    {
                        marker.transform.localScale =
                            Vector3.one *
                            (Mathf.Floor(
                                elapsed * 16f) %
                             2f < 1f
                                ? 0.88f
                                : 1f);
                    }

                    yield return null;
                }

                if (player != null)
                {
                    Vector3 flat =
                        player.position -
                        target;
                    flat.y = 0f;

                    if (flat.magnitude <= radius)
                    {
                        ApplyPlayerDamage(
                            boss.IsEnraged ? 11f : 9f,
                            target,
                            3.0f);
                    }
                }

                CombatEffects.SpawnRuneBurst(
                    target + Vector3.up * 0.05f,
                    radius);

                if (ProceduralAudio.Instance != null)
                {
                    ProceduralAudio.Instance.RuneBurst(
                        target);
                }

                if (marker != null)
                {
                    Destroy(marker);
                }

                yield return new WaitForSeconds(
                    boss.IsEnraged
                        ? 0.08f
                        : 0.14f);
            }
        }

        private void SetBossControl(bool controlled)
        {
            if (boss == null)
            {
                return;
            }

            boss.SetBossMechanicControl(controlled);

            if (controlled &&
                bossBody != null)
            {
                Vector3 velocity =
                    bossBody.linearVelocity;

                bossBody.linearVelocity =
                    new Vector3(
                        0f,
                        Mathf.Min(
                            velocity.y,
                            0f),
                        0f);
            }
        }

        private void MoveBoss(Vector3 position)
        {
            if (boss == null)
            {
                return;
            }

            if (bossBody != null)
            {
                bossBody.position = position;
            }
            else
            {
                boss.transform.position = position;
            }
        }

        private void CacheBossPresentation()
        {
            if (boss == null)
            {
                return;
            }

            bossColliders =
                boss.GetComponentsInChildren<Collider>(
                    true);
            bossColliderStates =
                new bool[bossColliders.Length];

            for (int i = 0;
                 i < bossColliders.Length;
                 i++)
            {
                bossColliderStates[i] =
                    bossColliders[i] != null &&
                    bossColliders[i].enabled;
            }

            bossRenderers =
                boss.GetComponentsInChildren<Renderer>(
                    true);
            bossRendererStates =
                new bool[bossRenderers.Length];

            for (int i = 0;
                 i < bossRenderers.Length;
                 i++)
            {
                bossRendererStates[i] =
                    bossRenderers[i] != null &&
                    bossRenderers[i].enabled;
            }
        }

        private void SetBossColliders(bool visible)
        {
            if (bossColliders == null)
            {
                CacheBossPresentation();
            }

            if (bossColliders == null)
            {
                return;
            }

            for (int i = 0;
                 i < bossColliders.Length;
                 i++)
            {
                if (bossColliders[i] != null)
                {
                    bossColliders[i].enabled =
                        visible &&
                        bossColliderStates[i];
                }
            }
        }

        private void SetBossRenderers(bool visible)
        {
            if (bossRenderers == null)
            {
                CacheBossPresentation();
            }

            if (bossRenderers == null)
            {
                return;
            }

            for (int i = 0;
                 i < bossRenderers.Length;
                 i++)
            {
                if (bossRenderers[i] != null)
                {
                    bossRenderers[i].enabled =
                        visible &&
                        bossRendererStates[i];
                }
            }
        }

        private void RestoreBossPresentation()
        {
            if (boss != null)
            {
                SetBossRenderers(true);
                SetBossColliders(true);
                boss.SetBossMechanicControl(false);
            }
        }

        private void ResolveRadialDamage(
            Vector3 center,
            float radius,
            float damage,
            float force)
        {
            if (player == null)
            {
                return;
            }

            Vector3 delta =
                player.position - center;

            delta.y = 0f;

            if (delta.magnitude <= radius)
            {
                ApplyPlayerDamage(
                    damage,
                    center,
                    force);
            }
        }

        private void ApplyPlayerDamage(
            float damage,
            Vector3 source,
            float knockback)
        {
            if (playerHealth == null)
            {
                return;
            }

            float before =
                playerHealth.CurrentHealth;

            playerHealth.TakeEnvironmentalDamage(
                damage,
                source);

            if (playerBody == null ||
                playerHealth.CurrentHealth >= before)
            {
                return;
            }

            Vector3 away =
                playerBody.position - source;

            away.y = 0f;

            if (away.sqrMagnitude < 0.01f)
            {
                away = Vector3.back;
            }

            playerBody.AddForce(
                away.normalized * knockback +
                Vector3.up * 0.45f,
                ForceMode.Impulse);
        }

        private GameObject CreatePixelRing(
            string name,
            Vector3 position,
            float radius,
            int segments)
        {
            GameObject root =
                new GameObject(name);

            root.transform.position = position;
            activeWarnings.Add(root);

            segments = Mathf.Max(8, segments);

            for (int i = 0; i < segments; i++)
            {
                float angle =
                    i / (float)segments *
                    Mathf.PI *
                    2f;

                Vector3 direction =
                    new Vector3(
                        Mathf.Cos(angle),
                        0f,
                        Mathf.Sin(angle));

                GameObject block =
                    GameObject.CreatePrimitive(
                        PrimitiveType.Cube);

                block.name =
                    "RingBlock_" + i;

                block.transform.SetParent(
                    root.transform,
                    false);

                block.transform.localPosition =
                    direction * radius;

                block.transform.localScale =
                    new Vector3(
                        0.34f,
                        0.025f,
                        0.34f);

                PrepareWarningRenderer(block);
            }

            return root;
        }

        private void SetPixelRingRadius(
            Transform root,
            float radius,
            float blockSize)
        {
            if (root == null)
            {
                return;
            }

            for (int i = 0;
                 i < root.childCount;
                 i++)
            {
                Transform child =
                    root.GetChild(i);

                Vector3 direction =
                    child.localPosition;

                direction.y = 0f;

                if (direction.sqrMagnitude <= 0.001f)
                {
                    continue;
                }

                direction.Normalize();

                child.localPosition =
                    direction * radius;

                child.localScale =
                    new Vector3(
                        blockSize,
                        0.025f,
                        blockSize);
            }
        }

        private GameObject CreatePixelTarget(
            string name,
            Vector3 position,
            float radius)
        {
            GameObject root =
                new GameObject(name);

            root.transform.position = position;
            activeWarnings.Add(root);

            Vector3[] offsets =
            {
                Vector3.zero,
                Vector3.left * radius * 0.62f,
                Vector3.right * radius * 0.62f,
                Vector3.forward * radius * 0.62f,
                Vector3.back * radius * 0.62f
            };

            for (int i = 0;
                 i < offsets.Length;
                 i++)
            {
                GameObject block =
                    GameObject.CreatePrimitive(
                        PrimitiveType.Cube);

                block.name =
                    "TargetBlock_" + i;

                block.transform.SetParent(
                    root.transform,
                    false);

                block.transform.localPosition =
                    offsets[i];

                float size =
                    i == 0
                        ? 0.46f
                        : 0.34f;

                block.transform.localScale =
                    new Vector3(
                        size,
                        0.025f,
                        size);

                PrepareWarningRenderer(block);
            }

            return root;
        }

        private void PrepareWarningRenderer(
            GameObject go)
        {
            Collider collider =
                go.GetComponent<Collider>();

            if (collider != null)
            {
                Destroy(collider);
            }

            Renderer renderer =
                go.GetComponent<Renderer>();

            if (renderer != null)
            {
                renderer.sharedMaterial =
                    warningMaterial;

                renderer.shadowCastingMode =
                    ShadowCastingMode.Off;

                renderer.receiveShadows = false;
            }
        }

        private GameObject CreateBoxWarning(
            string name,
            Vector3 position,
            Vector3 scale)
        {
            GameObject go =
                GameObject.CreatePrimitive(
                    PrimitiveType.Cube);

            go.name = name;
            go.transform.position = position;
            go.transform.localScale = scale;

            PrepareWarningObject(go);
            return go;
        }

        private void PrepareWarningObject(
            GameObject go)
        {
            activeWarnings.Add(go);
            PrepareWarningRenderer(go);
        }

        private static Material BuildWarningMaterial(
            int act)
        {
            Shader shader =
                Shader.Find(
                    "Universal Render Pipeline/Unlit");

            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            Color color =
                act == 0
                    ? new Color(
                        0.50f,
                        0.30f,
                        0.13f)
                    : act == 1
                        ? new Color(
                            0.58f,
                            0.40f,
                            0.16f)
                        : new Color(
                            0.55f,
                            0.14f,
                            0.09f);

            Material material =
                new Material(shader);

            material.name =
                "RuntimeBossMechanicWarning";

            material.color = color;

            if (material.HasProperty(
                "_BaseColor"))
            {
                material.SetColor(
                    "_BaseColor",
                    color);
            }

            if (material.HasProperty(
                "_EmissionColor"))
            {
                material.EnableKeyword(
                    "_EMISSION");

                material.SetColor(
                    "_EmissionColor",
                    color * 1.25f);
            }

            return material;
        }
    }
}
