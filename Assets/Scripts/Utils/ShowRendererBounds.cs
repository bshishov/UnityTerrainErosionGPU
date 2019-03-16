using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Utils
{
    [RequireComponent(typeof(Renderer))]
    public class ShowRendererBounds : MonoBehaviour
    {
        public Color BoundsColor = Color.yellow;

        private Renderer _renderer;

        void OnDrawGizmos()
        {
            if (_renderer == null)
                _renderer = GetComponent<Renderer>();

            Gizmos.color = BoundsColor;
            var bounds = _renderer.bounds;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
    }
}
