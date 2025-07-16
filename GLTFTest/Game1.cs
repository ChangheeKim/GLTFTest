using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SharpGLTF.Schema2;

namespace GLTFTest
{
    public class Game1 : Game
    {
        GraphicsDeviceManager _graphics;
        List<MeshData> _meshes = new();

        GltfModel _gltf;         // 필드 추가
        Matrix _world;
        float _rotationY;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this)
            {
                PreferredBackBufferWidth = 1024,
                PreferredBackBufferHeight = 768,
                GraphicsProfile = GraphicsProfile.Reach
            };
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            _world = Matrix.Identity;
            // view/proj 초기화 생략…
            base.Initialize();
        }

        protected override void LoadContent()
        {
            // 기존 BasicEffect 세팅은 GltfModel 내부로 이동했으니 제거
            _gltf = new GltfModel(
                GraphicsDevice,
                "glb/Duck.glb",
                view: Matrix.CreateLookAt(new Vector3(0, 2, 5), Vector3.Zero, Vector3.Up),
                projection: Matrix.CreatePerspectiveFieldOfView(
                    MathHelper.ToRadians(45f),
                    GraphicsDevice.Viewport.AspectRatio,
                    0.1f, 100f)
            );
        }

        protected override void Update(GameTime gameTime)
        {
            _rotationY += (float)gameTime.ElapsedGameTime.TotalSeconds * 0.5f;
            _world = Matrix.CreateRotationY(_rotationY);
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            _gltf.Draw(GraphicsDevice, _world);
            base.Draw(gameTime);
        }
    }
}
