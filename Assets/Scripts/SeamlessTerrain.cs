using System;
using System.Collections.Generic;
using UnityEngine;
using Utils;

public class SeamlessTerrain : MonoBehaviour
{
    public class DrawingContext
    {
        public int[] Resolutions;
        public readonly int MaxResolution;
        public Material Material;
        public Matrix4x4 LocalToWorld;
        public Vector2 LocalViewPoint;
        public float Precision;

        private readonly int[] _resolutions;
        private readonly Dictionary<int, Mesh> _tiles = new Dictionary<int, Mesh>();
        private readonly Dictionary<int, Dictionary<int, Mesh>> _stitches = new Dictionary<int, Dictionary<int, Mesh>>();
        private readonly Dictionary<Mesh, Matrix4x4[]> _transforms = new Dictionary<Mesh, Matrix4x4[]>();
        private readonly Dictionary<Mesh, int> _counts = new Dictionary<Mesh, int>();

        private readonly Quaternion _rotationTop = Quaternion.Euler(0, -45f, 0);
        private readonly Quaternion _rotationLeft = Quaternion.Euler(0, -45f - 90f, 0);
        private readonly Quaternion _rotationRight = Quaternion.Euler(0, 45f, 0);
        private readonly Quaternion _rotationBottom = Quaternion.Euler(0, 45f + 90f, 0);

        private const float Sqrt2 = 1.41421356237309504880f;
        private const int MaxInstanceMeshes = 1023;
        
        public DrawingContext(int[] resolutions)
        {
            _resolutions = resolutions;
            
            foreach (var resolution in resolutions)
            {
                var k1 = (resolution - 1f) / resolution;
                var tileMesh = MeshUtils.TriangularTile(
                    Vector3.zero + 0.5f * (1 - k1) * new Vector3(1, 0, 1),
                    Vector3.right * k1,
                    Vector3.forward * k1,
                    resolution);
                _tiles.Add(resolution, tileMesh);
                _transforms.Add(tileMesh, new Matrix4x4[MaxInstanceMeshes]);
                _counts.Add(tileMesh, 0);

                if (resolution > MaxResolution)
                    MaxResolution = resolution;

                foreach (var otherSide in resolutions)
                {
                    var stitchMesh = MeshUtils.StitchingStrip(
                        Vector3.zero,
                        Vector3.forward,
                        Vector3.up,
                        resolution,
                        otherSide);
                    //_stitches.Add(new Tuple<int, int>(resolution, otherSide), stitchMesh);

                    if (!_stitches.ContainsKey(resolution))
                        _stitches.Add(resolution, new Dictionary<int, Mesh>());
                    
                    _stitches[resolution].Add(otherSide, stitchMesh);
                    
                    _transforms.Add(stitchMesh, new Matrix4x4[MaxInstanceMeshes]);
                    _counts.Add(stitchMesh, 0);
                }
            }
        }

        public int NearestResolution(float resolution)
        {
            var res = Mathf.RoundToInt(resolution);
            var nearest = _resolutions[0];
            for (var i = 1; i < _resolutions.Length; i++)
            {
                if (Mathf.Abs(_resolutions[i] - res) < Mathf.Abs(nearest - res))
                    nearest = _resolutions[i];
            }
            return nearest;
        }
        
        public void QueuePatch(Vector2 position, Vector2 size, float rL, float rR, float rT, float rB)
        {
            // Resolutions
            var l = NearestResolution(rL);
            var r = NearestResolution(rR);
            var t = NearestResolution(rT);
            var b = NearestResolution(rB);

            // Meshes
            var ml = _tiles[l];
            var mr = _tiles[r];
            var mt = _tiles[t];
            var mb = _tiles[b];

            // Stitches meshes
            var mlt = _stitches[l][t];
            var mtr = _stitches[t][r];
            var mrb = _stitches[r][b];
            var mbl = _stitches[b][l];
            
            var center = new Vector3(position.x + size.x * 0.5f, 0, position.y + size.y * 0.5f);
            var tileSize = new Vector3(size.x, 1, size.y) / Sqrt2;
            
            QueueMesh(ml, center, _rotationLeft, tileSize);
            QueueMesh(mt, center, _rotationTop, tileSize);
            QueueMesh(mr, center, _rotationRight, tileSize);
            QueueMesh(mb, center, _rotationBottom, tileSize);
            
            /*
            QueueMesh(mlt, center, _rotationTop, tileSize);
            QueueMesh(mtr, center, _rotationRight, tileSize);
            QueueMesh(mrb, center, _rotationBottom, tileSize);
            QueueMesh(mbl, center, _rotationLeft, tileSize);*/
        }

        public void QueuePatch(Vector2 position, Vector2 size, Vector4 desiredResolution)
        {
            QueuePatch(position, size, 
                desiredResolution.w, 
                desiredResolution.y, 
                desiredResolution.x,
                desiredResolution.z);
        }

        private void QueueMesh(Mesh m, Vector3 pos, Quaternion rot, Vector3 scale)
        {
            if (_counts[m] < MaxInstanceMeshes - 1)
                _transforms[m][_counts[m]++].SetTRS(pos, rot, scale);
        }

        public void DrawQueuedMeshes()
        {
            foreach (var kvp in _transforms)
            {
                var count = _counts[kvp.Key];
                if(count > 0)
                    Graphics.DrawMeshInstanced(kvp.Key, 0, Material, kvp.Value, count);
            }
        }

        public void Draw(Vector2 viewPoint, Vector2 position, Vector2 size, Vector4 eps, int depthRemaining)
        {
            if (depthRemaining <= 0 || Mathf.Max(eps.x, eps.y, eps.z, eps.w) <= 1)
            {
                QueuePatch(position, size, eps * MaxResolution);
                return;
            }
            
            /* Split:
             
             ---------
             | a | b |
             ---------
             | c | d |
             ---------             
             
            */


            var halfSize = size * 0.5f;
            var center = position + halfSize;
            
            var ab = Epsilon(viewPoint, center, center + new Vector2(0, halfSize.y), Precision);
            var dc = Epsilon(viewPoint, center, center - new Vector2(0, halfSize.y), Precision);
            var bd = Epsilon(viewPoint, center, center + new Vector2(halfSize.x, 0), Precision);
            var ca = Epsilon(viewPoint, center, center - new Vector2(halfSize.x, 0), Precision);
            
            var a = new Vector4(eps.x * 0.5f, ab, ca, eps.w * 0.5f);
            var b = new Vector4(eps.x * 0.5f, eps.y * 0.5f, bd, ab);
            var c = new Vector4(ca, dc, eps.z * 0.5f, eps.w * 0.5f);
            var d = new Vector4(bd, eps.y * 0.5f, eps.z * 0.5f, dc);

            if (eps.x > 1)
            {
                a.x *= 2f;
                b.x *= 2f;
            }
            
            if (eps.y > 1)
            {
                b.y *= 2f;
                d.y *= 2f;
            }
            
            if (eps.z > 1)
            {
                d.z *= 2f;
                c.z *= 2f;
            }
            
            if (eps.w > 1)
            {
                c.w *= 2f;
                a.w *= 2f;
            }
            
            Draw(viewPoint,position + new Vector2(0, halfSize.y), halfSize, a, depthRemaining - 1);
            Draw(viewPoint,center, halfSize, b, depthRemaining - 1);
            Draw(viewPoint, position, halfSize, c, depthRemaining - 1);
            Draw(viewPoint,position + new Vector2(halfSize.x, 0), halfSize, d, depthRemaining - 1);
        }

        public void Draw2(Vector2 size, int maxDepth)
        {
            var viewPoint = LocalViewPoint;

            var c1 = Vector2.zero;
            var c2 = Vector2.up * size.y;
            var c3 = size;
            var c4 = Vector2.right * size.x;
            
            var initialEps = new Vector4(
                Epsilon(viewPoint, c2, c3, Precision),
                Epsilon(viewPoint, c3, c4, Precision),
                Epsilon(viewPoint, c4, c1, Precision),
                Epsilon(viewPoint, c1, c2, Precision));
            
            Draw(viewPoint, Vector2.zero, size, initialEps, maxDepth);
        }

        public float Epsilon(Vector2 viewPoint, Vector2 start, Vector3 end, float precision)
        {
            var segmentLength = Vector3.Distance(end, start);
            return precision * segmentLength / SeamlessTerrain.PointToSegmentDistance(viewPoint, start, end);
        }

        public void Clear()
        {
            foreach (var k in _transforms.Keys)
            {
                _counts[k] = 0;
            }
        }
    }
    
    public class Patch
    {
        public Patch[] Children;
        public Vector2 Position;
        public Vector2 Size;

        public int[] Tiles;
        
        // AABB positions
        private readonly Vector2 _bl;
        private readonly Vector2 _br;
        private readonly Vector2 _tl;
        private readonly Vector2 _tr;

        public Patch(Vector2 position, Vector2 size, int maxDepth)
        {
            Position = position;
            Size = size;

            _bl = Position;
            _br = Position + new Vector2(size.x, 0);
            _tr = Position + new Vector2(size.x, size.y);
            _tl = Position + new Vector2(0, size.y);


            if (maxDepth > 0)
            {
                var halfSize = size * 0.5f;
                Children = new Patch[]
                {
                    new Patch(new Vector2(position.x, position.y + halfSize.y), halfSize, maxDepth - 1),
                    new Patch(new Vector2(position.x + halfSize.x, position.y), halfSize, maxDepth - 1),
                    new Patch(position + halfSize, halfSize, maxDepth - 1),
                    new Patch(position, halfSize, maxDepth - 1),
                };
            }
        }

        public void Draw(DrawingContext context)
        {
            var precision = context.Precision;
            var p = context.LocalViewPoint;
            
            var errB = precision * Size.x / SeamlessTerrain.PointToSegmentDistance(p, _bl, _br);
            var errT = precision * Size.x / SeamlessTerrain.PointToSegmentDistance(p, _tl, _tr);
            var errL = precision * Size.y / SeamlessTerrain.PointToSegmentDistance(p, _bl, _tl);
            var errR = precision * Size.y / SeamlessTerrain.PointToSegmentDistance(p, _br, _tr);
            
            // If has children, and error is to high - split the patch into children
            if (Children != null && Children.Length > 0 && Mathf.Max(errB, errT, errL, errR) > 1)
            {
                // Draw children
                for (var i = 0; i < Children.Length; i++)
                    Children[i].Draw(context);
            }
            else
            {
                context.QueuePatch(
                    Position, 
                    Size, 
                    errL * context.MaxResolution, 
                    errR * context.MaxResolution,
                    errT * context.MaxResolution, 
                    errB * context.MaxResolution);
            }
        }
       
        public Vector4 CalcEpsilon(Vector2 p, float precision)
        {
            return new Vector4(
                x: precision * Size.x / SeamlessTerrain.PointToSegmentDistance(p, _tl, _tr),
                y: precision * Size.y / SeamlessTerrain.PointToSegmentDistance(p, _br, _tr),
                z: precision * Size.x / SeamlessTerrain.PointToSegmentDistance(p, _bl, _br),
                w: precision * Size.y / SeamlessTerrain.PointToSegmentDistance(p, _bl, _tl));
        }
    }
    
    public Material Material;
    public float Precision = 1f;
    public Vector2 Size = new Vector2(1000f, 1000f);
    public int[] Resolutions = { 3, 8, 18};
    
    [Range(1, 5)]
    public int MaxDepth = 8;
    
    //private Patch _root;
    private DrawingContext _context;
    private Transform _cameraTransform;

    void Start()
    {
        //_root = new Patch(Vector2.zero, Size, MaxDepth);
        _context = new DrawingContext(Resolutions);
        _cameraTransform = Camera.main.transform;
    }

    private void Update()
    {
        var localViewPoint = transform.InverseTransformPoint(_cameraTransform.position);
        
        _context.Clear();
        _context.LocalToWorld = transform.localToWorldMatrix;
        _context.Precision = Precision;
        _context.Material = Material;
        _context.LocalViewPoint = new Vector2(localViewPoint.x, localViewPoint.z);
        //_root.Draw(_context);
        //_context.Draw();
        _context.Draw2(Size, MaxDepth);
        _context.DrawQueuedMeshes();
    }

    public static float PointToSegmentDistance(Vector2 point, Vector2 start, Vector2 end)
    {
        return Vector3.Distance(point, start + 0.5f * (end - start));
        //return Mathf.Min(Vector3.Distance(point, end), Vector3.Distance(point, start));
        //return DistancePointLine(point, start, end);
        
        var d = end - start;
        var l2 = d.x * d.x + d.y * d.y;
        
        if (Math.Abs(l2) < 1e-8)
            return Vector2.Distance(point, start);

        var t = Mathf.Clamp01(((point.x - start.x) * d.x + (point.y - start.x) * d.y) / l2);
        return Vector2.Distance(point, start + t * d);
    }

    public static float DistancePointLine(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
    {
        return Vector3.Distance(ProjectPointLine(point, lineStart, lineEnd), point);
    }
    public static Vector3 ProjectPointLine(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
    {
        var rhs = point - lineStart;
        var vector2 = lineEnd - lineStart;
        var magnitude = vector2.magnitude;
        var lhs = vector2;
        if (magnitude > 1E-06f)
        {
            lhs = (Vector3)(lhs / magnitude);
        }
        var num2 = Mathf.Clamp(Vector3.Dot(lhs, rhs), 0f, magnitude);
        return (lineStart + ((Vector3)(lhs * num2)));
    }


}
