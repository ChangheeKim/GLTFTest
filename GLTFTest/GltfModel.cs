using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SharpGLTF.Schema2;
using PrimitiveType = Microsoft.Xna.Framework.Graphics.PrimitiveType;

namespace GLTFTest
{
    public class GltfModel
    {
        private ModelRoot _gltfModel;
        private readonly List<MeshData> _meshes = new();
        private readonly BasicEffect _basicEffect;
        private readonly Matrix _view;
        private readonly Matrix _projection;
        private readonly GraphicsDevice _device;
        private readonly Dictionary<int, Texture2D> _textures = new();

        public GltfModel(GraphicsDevice device, string path, Matrix view, Matrix projection)
        {
            _device = device;
            _view = view;
            _projection = projection;

            _basicEffect = new BasicEffect(device)
            {
                LightingEnabled = true,
                View = _view,
                Projection = _projection,
                TextureEnabled = true  // 텍스처 활성화
            };
            _basicEffect.EnableDefaultLighting();

            _basicEffect.AmbientLightColor = new(0.4f, 0.4f, 0.4f);

            _basicEffect.DirectionalLight0.Enabled = true;
            _basicEffect.DirectionalLight0.DiffuseColor = new(0.3f, 0.3f, 0.3f);
            _basicEffect.DirectionalLight0.SpecularColor = new(0.1f, 0.1f, 0.1f);
            _basicEffect.DirectionalLight0.Direction = new(0f, -1f, 0f);

            _basicEffect.DirectionalLight1.Enabled = true;
            _basicEffect.DirectionalLight1.DiffuseColor = new(0.3f, 0.0f, 0.0f);
            _basicEffect.DirectionalLight1.Direction = new(-1.0f, 0f, 0f);

            _basicEffect.DirectionalLight2.Enabled = true;
            _basicEffect.DirectionalLight2.DiffuseColor = new(0.0f, 0.0f, 0.3f);
            _basicEffect.DirectionalLight2.Direction = new(1.0f, 0f, 0f);

            LoadGLTFModel(path);
        }

        public void LoadGLTFModel(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"GLB 파일을 찾을 수 없습니다: {path}");

            _gltfModel = ModelRoot.Load(path);
            LoadTextures();
            ExtractMeshData();
        }

        private void LoadTextures()
        {
            _textures.Clear();

            for (int i = 0; i < _gltfModel.LogicalTextures.Count; i++)
            {
                var gltfTexture = _gltfModel.LogicalTextures[i];
                var image = gltfTexture.PrimaryImage;

                if (image?.Content.IsValid == true)
                {
                    var imageData = image.Content.Content.ToArray();
                    using var stream = new MemoryStream(imageData);
                    var texture = Texture2D.FromStream(_device, stream);
                    _textures[i] = texture;
                }
            }
        }

        private void ExtractMeshData()
        {
            _meshes.Clear();
            foreach (var scene in _gltfModel.LogicalScenes)
                foreach (var node in scene.VisualChildren)
                    ExtractNodeMeshes(node, Matrix.Identity);
        }

        private void ExtractNodeMeshes(Node node, Matrix parentTransform)
        {
            var local = ToXna(node.LocalMatrix);
            var world = local * parentTransform;

            if (node.Mesh != null)
            {
                foreach (var prim in node.Mesh.Primitives)
                {
                    if (TryExtract(prim, world, out var md))
                        _meshes.Add(md);
                }
            }

            foreach (var child in node.VisualChildren)
                ExtractNodeMeshes(child, world);
        }

        private bool TryExtract(MeshPrimitive p, Matrix xform, out MeshData md)
        {
            md = null;
            var posAcc = p.GetVertexAccessor("POSITION")?.AsVector3Array();
            if (posAcc == null)
                return false;

            var nrmAcc = p.GetVertexAccessor("NORMAL")?.AsVector3Array();
            var uvAcc = p.GetVertexAccessor("TEXCOORD_0")?.AsVector2Array();

            // 버텍스 배열 생성
            var verts = new VertexPositionNormalTexture[posAcc.Count];
            for (int i = 0; i < posAcc.Count; i++)
            {
                var P = posAcc[i];
                var N = nrmAcc != null ? nrmAcc[i] : Vector3.UnitY;
                var U = uvAcc != null ? uvAcc[i] : Vector2.Zero;

                verts[i] = new VertexPositionNormalTexture(
                    new Vector3(P.X, P.Y, P.Z),
                    new Vector3(N.X, N.Y, N.Z),
                    new Vector2(U.X, U.Y)
                );
            }

            // 인덱스 배열 생성 및 뒤집기
            var idxList = p.GetIndices()?.Select(i => (short)i).ToArray();
            if (idxList != null)
            {
                for (int i = 0; i < idxList.Length; i += 3)
                {
                    // 삼각형의 두 번째와 세 번째 인덱스 교환
                    var tmp = idxList[i + 1];
                    idxList[i + 1] = idxList[i + 2];
                    idxList[i + 2] = tmp;
                }
            }

            // 머터리얼에서 텍스처 정보 가져오기
            Texture2D texture = null;
            if (p.Material != null)
            {
                var baseColorTexture = p.Material.FindChannel("BaseColor")?.Texture;
                if (baseColorTexture != null)
                {
                    var textureIndex = _gltfModel.LogicalTextures
                        .Select((tex, index) => new { Texture = tex, Index = index })
                        .FirstOrDefault(x => x.Texture == baseColorTexture)?.Index ?? -1;

                    if (textureIndex >= 0 && _textures.ContainsKey(textureIndex))
                    {
                        texture = _textures[textureIndex];
                    }
                }
            }

            md = new MeshData
            {
                Vertices = verts,
                Indices = idxList,
                Transform = xform,
                Texture = texture
            };
            return true;
        }

        public void Draw(GraphicsDevice device, Matrix world)
        {
            foreach (var mesh in _meshes)
            {
                _basicEffect.World = mesh.Transform * world;

                // 텍스처 설정
                if (mesh.Texture != null)
                {
                    _basicEffect.Texture = mesh.Texture;
                    _basicEffect.TextureEnabled = true;
                }
                else
                {
                    _basicEffect.TextureEnabled = false;
                }

                foreach (var pass in _basicEffect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    if (mesh.Indices != null)
                    {
                        device.DrawUserIndexedPrimitives(
                            PrimitiveType.TriangleList,
                            mesh.Vertices, 0, mesh.Vertices.Length,
                            mesh.Indices, 0, mesh.Indices.Length / 3
                        );
                    }
                    else
                    {
                        device.DrawUserPrimitives(
                            PrimitiveType.TriangleList,
                            mesh.Vertices, 0, mesh.Vertices.Length / 3
                        );
                    }
                }
            }
        }

        private static Matrix ToXna(System.Numerics.Matrix4x4 m)
            => new(
                m.M11, m.M12, m.M13, m.M14,
                m.M21, m.M22, m.M23, m.M24,
                m.M31, m.M32, m.M33, m.M34,
                m.M41, m.M42, m.M43, m.M44);

        public void Dispose()
        {
            foreach (var texture in _textures.Values)
                texture?.Dispose();
            _textures.Clear();
            _basicEffect?.Dispose();
        }
    }

    public class MeshData
    {
        public VertexPositionNormalTexture[] Vertices;
        public short[] Indices;
        public Matrix Transform;
        public Texture2D Texture;
    }
}