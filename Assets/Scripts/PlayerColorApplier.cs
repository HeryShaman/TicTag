using System.Collections.Generic;
using UnityEngine;
using TMPro;


public class PlayerColorApplier : MonoBehaviour
{
    [SerializeField] private Renderer[] renderers;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private MaterialPropertyBlock _block;

    public Color CurrentColor { get; private set; } = Color.white;

    public void Apply(Color color)
    {
        CurrentColor = color;

        if (renderers == null || renderers.Length == 0)
            renderers = FindMeshRenderers();

        if (_block == null)
            _block = new MaterialPropertyBlock();

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            if (r == null) continue;

            r.GetPropertyBlock(_block);
            _block.SetColor(BaseColorId, color);
            _block.SetColor(ColorId, color);
            r.SetPropertyBlock(_block);
        }
    }

    private Renderer[] FindMeshRenderers()
    {
        var found = new List<Renderer>();

        foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
        {
            if (!(r is MeshRenderer) && !(r is SkinnedMeshRenderer)) continue;
            if (r.GetComponent<TMP_Text>() != null) continue;
            found.Add(r);
        }

        return found.ToArray();
    }
}