using UnityEngine;

namespace Utils
{
    public static class MeshUtils
    {
        public static Mesh GeneratePlane(Vector3 origin, Vector3 axis0, Vector3 axis1, int axis0Vertices, int axis1Vertices, Vector2 uvStart, Vector2 uvEnd)
        {
            var vertices = new Vector3[axis0Vertices * axis1Vertices];
            var normals = new Vector3[vertices.Length];
            var uvs = new Vector2[vertices.Length];
            var triangles = new int[(axis0Vertices - 1) * (axis1Vertices - 1) * 2 * 3];
            var normal = Vector3.Cross(axis1, axis0).normalized;

            // Vertices
            for (var i = 0; i < vertices.Length; i++)
            {
                var i0 = i / axis1Vertices;
                var i1 = i % axis1Vertices;
                var localU = i0 / (axis0Vertices - 1f);
                var localV = i1 / (axis1Vertices - 1f);

                vertices[i] = origin + localU * axis0 + localV * axis1;
                normals[i] = normal;
                uvs[i].x = Mathf.Lerp(uvStart.x, uvEnd.x, localU);
                uvs[i].y = Mathf.Lerp(uvStart.y, uvEnd.y, localV);
            }

            // Triangles
            var vertexIndex = 0;
            for (var i0 = 0; i0 < axis0Vertices - 1; i0++)
            {
                for (var i1 = 0; i1 < axis1Vertices - 1; i1++)
                {
                    triangles[vertexIndex++] = (i0 + 0) * axis1Vertices + (i1 + 0);
                    triangles[vertexIndex++] = (i0 + 1) * axis1Vertices + (i1 + 1);
                    triangles[vertexIndex++] = (i0 + 1) * axis1Vertices + (i1 + 0);

                    triangles[vertexIndex++] = (i0 + 0) * axis1Vertices + (i1 + 0);
                    triangles[vertexIndex++] = (i0 + 0) * axis1Vertices + (i1 + 1);
                    triangles[vertexIndex++] = (i0 + 1) * axis1Vertices + (i1 + 1);
                }
            }

            var mesh = new Mesh()
            {
                name = $"Plane_{axis0Vertices}x{axis1Vertices}",
                vertices = vertices,
                normals = normals,
                uv = uvs,
                triangles = triangles
            };

            mesh.RecalculateBounds();
            return mesh;
        }

        public static Mesh TriangularTile(Vector3 origin, Vector3 side1, Vector3 side2, int verticesAlongAxis)
        {
            var vertices = new Vector3[verticesAlongAxis * (verticesAlongAxis + 1) / 2];
            var triangles = new int[(verticesAlongAxis - 1) * (verticesAlongAxis - 1) * 3];
            var normals = new Vector3[vertices.Length];
            var normal = Vector3.Cross(side1, side2);
            
            // Vertices
            var vi = 0;
            var tri = 0;
            for (var row = 0; row < verticesAlongAxis; row++)
            {
                var localV = 1f - (float)row / (verticesAlongAxis - 1);
                
                for (var col = 0; col < row + 1; col++)
                {
                    var localU = (float) col / (verticesAlongAxis - 1);  
                    
                    vertices[vi] = origin + localU * side1 + localV * side2;
                    normals[vi] = normal;

                    // Triangles
                    if (col >= 1 && row >= 1)
                    {
                        triangles[tri++] = vi;
                        triangles[tri++] = vi - 1;
                        triangles[tri++] = vi - row - 1;
                        
                        if (col < row)
                        {
                            triangles[tri++] = vi;
                            triangles[tri++] = vi - row - 1;
                            triangles[tri++] = vi - row;
                        }
                    }

                    vi++;
                }
            }
            
            var bounds = new Bounds();
            bounds.Encapsulate(origin);
            bounds.Encapsulate(origin + side1);
            bounds.Encapsulate(origin + side2);
            
            var mesh = new Mesh()
            {
                name = $"Triangle_{verticesAlongAxis}",
                vertices = vertices,
                triangles = triangles,
                bounds = bounds
            };
            return mesh;
        }

        public static Mesh StitchingStrip(Vector3 origin, Vector3 target, Vector3 normal, int numLeft, int numRight)
        {
            var stripLength = Vector3.Distance(target, origin);
            var fAxis = (target - origin).normalized;
            var rAxis = Vector3.Cross(normal, fAxis).normalized;
            
            var leftStepScale = stripLength / numLeft;
            var rightStepScale = stripLength / numRight;
            
            var leftOffset = leftStepScale * 0.5f;
            var rightOffset = rightStepScale * 0.5f;
            
            var vertices = new Vector3[numLeft + numRight + 2];
            var normals = new Vector3[vertices.Length];
            var triangles = new int[(numLeft + numRight) * 3];

            var leftLeg = 0;
            var rightLeg = 0;
            var tri = 0;
            var vi = 0;

            for (var i = 0; i < numLeft; i++)
                vertices[vi++] = origin + -leftOffset * rAxis + (leftOffset + i * leftStepScale) * fAxis;

            for (var i = 0; i < numRight; i++)
                vertices[vi++] = origin + rightOffset * rAxis + (rightOffset + i * rightStepScale) * fAxis;

            vertices[vi++] = origin;
            vertices[vi++] = target;
            
            while (leftLeg + rightLeg < numLeft + numRight - 2)
            {
                var l = ((leftLeg + 1) * leftStepScale + leftOffset) - (rightLeg * rightStepScale + rightOffset);
                var r = ((rightLeg + 1) * rightStepScale + rightOffset) - (leftLeg * leftStepScale + leftOffset);
                
                if (l < r)
                {
                    var nextLeftLeg = leftLeg + 1;
                    
                    triangles[tri++] = leftLeg;
                    triangles[tri++] = nextLeftLeg;
                    triangles[tri++] = numLeft + rightLeg;

                    leftLeg = nextLeftLeg;
                }
                else
                {
                    var nextRightLeg = rightLeg + 1;
                    
                    triangles[tri++] = leftLeg;
                    triangles[tri++] = numLeft + nextRightLeg;
                    triangles[tri++] = numLeft + rightLeg;

                    rightLeg = nextRightLeg;
                }
            }
            
            var originIndex = vertices.Length - 2;
            var targetIndex = vertices.Length - 1;

            triangles[tri++] = originIndex;
            triangles[tri++] = 0;
            triangles[tri++] = numLeft;
            
            triangles[tri++] = targetIndex;
            triangles[tri++] = numLeft + numRight - 1;
            triangles[tri++] = numLeft - 1;
            
            var mesh = new Mesh()
            {
                name = $"StitchingStrip_{numLeft}->{numRight}",
                vertices = vertices,
                normals = normals,
                triangles = triangles
            };

            mesh.RecalculateBounds();
            return mesh;
        }
    }
}