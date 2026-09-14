using System.Collections.Generic;
using UnityEngine;

// The imported character is visual only. The existing root Rigidbody and
// capsule remain authoritative for movement, possession and collisions.
public class FutsalPlayerVisual : MonoBehaviour
{
    private Rigidbody body;
    private Animation animationPlayer;
    private readonly List<Material> ownedMaterials = new List<Material>();
    private string activeClip;
    private float kickUntil;

    public void Initialize(Material shaderSource, FutsalTeam team, FutsalRole role, int number)
    {
        GameObject asset = Resources.Load<GameObject>("Players/Athlete");
        if (asset == null)
        {
            Debug.LogError("Missing Players/Athlete character model.", this);
            return;
        }
        body = GetComponent<Rigidbody>();
        GameObject model = Instantiate(asset, transform, false);
        model.name = "Player Model";
        model.transform.localPosition = Vector3.down;
        foreach (Collider collider in model.GetComponentsInChildren<Collider>()) collider.enabled = false;
        GetComponent<Renderer>().enabled = false;
        foreach (Transform child in transform)
            if (child.name == "Facing Marker" || child.name == "Direction Patch") child.gameObject.SetActive(false);

        bool home = team == FutsalTeam.Home;
        bool keeper = role == FutsalRole.Goalkeeper;
        Color kit = keeper ? (home ? new Color(0.06f, 0.65f, 0.4f) : new Color(1f, 0.6f, 0.06f))
            : (home ? new Color(0.08f, 0.3f, 0.85f) : new Color(0.85f, 0.08f, 0.12f));
        foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>())
        {
            Material[] materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                Material original = materials[i];
                if (original == null) continue;
                Texture texture = original.HasProperty("_BaseMap") ? original.GetTexture("_BaseMap") : original.mainTexture;
                Color tint = original.HasProperty("_BaseColor") ? original.GetColor("_BaseColor") : original.color;
                string name = original.name.ToLowerInvariant();
                if (name.Contains("jersey") || name.Contains("socks")) tint = kit;
                if (name.Contains("shorts")) tint = home ? new Color(0.06f, 0.09f, 0.18f) : new Color(0.92f, 0.93f, 0.95f);
                Material material = new Material(shaderSource) { name = original.name + " " + team };
                if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
                if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
                if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", tint);
                if (material.HasProperty("_Color")) material.SetColor("_Color", tint);
                if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.2f);
                if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0.2f);
                if (name.Contains("hair"))
                {
                    if (material.HasProperty("_AlphaClip")) material.SetFloat("_AlphaClip", 1f);
                    if (material.HasProperty("_Cutoff")) material.SetFloat("_Cutoff", 0.4f);
                    if (material.HasProperty("_Cull")) material.SetFloat("_Cull", 0f);
                    material.EnableKeyword("_ALPHATEST_ON");
                    material.renderQueue = 2450;
                }
                ownedMaterials.Add(material);
                materials[i] = material;
            }
            renderer.sharedMaterials = materials;
        }
        animationPlayer = model.GetComponent<Animation>();
        if (animationPlayer == null) animationPlayer = model.AddComponent<Animation>();
        foreach (AnimationClip clip in Resources.LoadAll<AnimationClip>("Players/Athlete"))
        {
            if (!clip.legacy || clip.name.StartsWith("__preview__")) continue;
            string name = clip.name.Contains("|") ? clip.name.Substring(clip.name.LastIndexOf('|') + 1) : clip.name;
            animationPlayer.AddClip(clip, name);
            animationPlayer[name].wrapMode = name == "Kick" ? WrapMode.Once : WrapMode.Loop;
        }
        animationPlayer.cullingType = AnimationCullingType.BasedOnRenderers;
        Play("Idle", 0f);
        animationPlayer.Sample();
    }

    public void PlayKick(float strength)
    {
        if (animationPlayer == null || animationPlayer["Kick"] == null) return;
        animationPlayer["Kick"].speed = Mathf.Lerp(1.25f, 0.9f, Mathf.Clamp01(strength));
        kickUntil = Time.time + animationPlayer["Kick"].length / animationPlayer["Kick"].speed;
        animationPlayer["Kick"].time = 0f;
        activeClip = null;
        Play("Kick", 0.05f);
    }

    public void ResetPose()
    {
        if (animationPlayer == null) return;
        kickUntil = 0f;
        activeClip = null;
        animationPlayer.Stop();
        Play("Idle", 0f);
        animationPlayer.Sample();
    }

    private void LateUpdate()
    {
        if (animationPlayer == null || Time.deltaTime <= 0f || Time.time < kickUntil) return;
        Vector3 velocity = body.linearVelocity;
        velocity.y = 0f;
        float speed = velocity.magnitude;
        string clip = speed < 0.15f ? "Idle" : speed < 7.7f ? "Jog" : "Sprint";
        Play(clip, 0.14f);
        if (animationPlayer[clip] != null)
            animationPlayer[clip].speed = clip == "Idle" ? 1f : Mathf.Clamp(speed / (clip == "Jog" ? 5f : 9.5f), 0.35f, 1.5f);
    }

    private void Play(string name, float fade)
    {
        if (activeClip == name || animationPlayer[name] == null) return;
        animationPlayer.CrossFade(name, fade);
        activeClip = name;
    }

    private void OnDestroy()
    {
        foreach (Material material in ownedMaterials) if (material != null) Destroy(material);
    }
}
