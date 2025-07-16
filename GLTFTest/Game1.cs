using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GLTFTest
{
    public class Game1 : Game
    {
        private readonly GraphicsDeviceManager _graphics;

        private GltfModel _gltf;         // 필드 추가
        private Matrix _world;
        private float _rotationY;

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
            _gltf = new GltfModel(
                GraphicsDevice,
                "Duck.glb",
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
