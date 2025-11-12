using System.Collections.Generic;
using UnityEngine;

namespace TD.Gameplay.Towers
{
    /// <summary>
    /// Helper component that adjusts preview visuals during placement.
    /// </summary>
    public class TowerPlacementPreview : MonoBehaviour
    {
        [SerializeField] private Color validColor = new(0f, 1f, 0f, 0.45f);
        [SerializeField] private Color invalidColor = new(1f, 0f, 0f, 0.45f);

        private readonly List<SpriteRenderer> spriteRenderers = new();
        private readonly List<Renderer> meshRenderers = new();
        private readonly Dictionary<Renderer, MaterialPropertyBlock> propertyBlocks = new();

        private void Awake()
        {
            spriteRenderers.AddRange(GetComponentsInChildren<SpriteRenderer>(true));

            var renderers = GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                if (renderer is SpriteRenderer)
                {
                    continue;
                }

                meshRenderers.Add(renderer);
                propertyBlocks[renderer] = new MaterialPropertyBlock();
            }
        }

        public void SetVisibility(bool visible)
        {
            if (gameObject.activeSelf != visible)
            {
                gameObject.SetActive(visible);
            }
        }

        public void SetPlacementValidity(bool isValid)
        {
            var targetColor = isValid ? validColor : invalidColor;

            foreach (var spriteRenderer in spriteRenderers)
            {
                spriteRenderer.color = targetColor;
            }

            foreach (var meshRenderer in meshRenderers)
            {
                if (!propertyBlocks.TryGetValue(meshRenderer, out var block))
                {
                    block = new MaterialPropertyBlock();
                    propertyBlocks[meshRenderer] = block;
                }

                meshRenderer.GetPropertyBlock(block);
                block.SetColor("_Color", targetColor);
                meshRenderer.SetPropertyBlock(block);
            }
        }

        public void SetPosition(Vector3 worldPosition)
        {
            transform.position = worldPosition;
        }
    }
}
