using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using ParallelTasks;

namespace ImageProcessing
{
    enum Mode
    {
        Sequential,
        Parallel
    }

    enum Progress
    {
        None,
        GoingToProcess,
        Processing
    }

    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        KeyboardState previousKeyboard;
        GamePadState previousGamepad;
        SpriteFont font;
        Mode currentMode;
        Texture2D sourceTexture;
        Texture2D output;
        TimeSpan time;
        Progress progress;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            previousKeyboard = Keyboard.GetState();
            previousGamepad = GamePad.GetState(PlayerIndex.One);

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here
            font = Content.Load<SpriteFont>("Arial Outlined");
            sourceTexture = Content.Load<Texture2D>("Koala");
            output = sourceTexture;
            graphics.PreferredBackBufferWidth = sourceTexture.Width;
            graphics.PreferredBackBufferHeight = sourceTexture.Height;
            graphics.ApplyChanges();
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                this.Exit();

            Parallel.RunCallbacks();

            // TODO: Add your update logic here
            var keyboard = Keyboard.GetState();
            var gamepad = GamePad.GetState(PlayerIndex.One);

            if ((keyboard.IsKeyDown(Keys.P) && previousKeyboard.IsKeyUp(Keys.P))
                || (gamepad.IsButtonDown(Buttons.B) && previousGamepad.IsButtonUp(Buttons.B)))
            {
                if (currentMode == Mode.Parallel)
                    currentMode = Mode.Sequential;
                else
                    currentMode = Mode.Parallel;
            }

            if ((keyboard.IsKeyDown(Keys.Enter) && previousKeyboard.IsKeyUp(Keys.Enter))
                || (gamepad.IsButtonDown(Buttons.A) && previousGamepad.IsButtonUp(Buttons.A)))
            {
                progress = Progress.GoingToProcess;
            }

            previousKeyboard = keyboard;
            previousGamepad = gamepad;

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // TODO: Add your drawing code here
            spriteBatch.Begin();

            if (progress == Progress.Processing)
            {
                Window.Title = "ImageProcessing - Working..";
                System.Diagnostics.Debug.WriteLine(Window.Title);
                if (currentMode == Mode.Parallel)
                    output = ImageBlurer.BlurParallel(sourceTexture, 15, out time);
                else
                    output = ImageBlurer.BlurSequential(sourceTexture, 15, out time);
                Window.Title = "ImageProcessing - Complete " + currentMode;
                System.Diagnostics.Debug.WriteLine(Window.Title);
                progress = Progress.None;
            }

            if (progress == Progress.None)
            {
                if (Keyboard.GetState().IsKeyDown(Keys.Space) || GamePad.GetState(PlayerIndex.One).IsButtonDown(Buttons.X))
                    spriteBatch.Draw(sourceTexture, new Rectangle(0, 0, output.Width, output.Height), Color.White);
                else
                    spriteBatch.Draw(output, new Rectangle(0, 0, sourceTexture.Width, sourceTexture.Height), Color.White);
            }
            else if (progress == Progress.GoingToProcess)
            {
                spriteBatch.Draw(sourceTexture, new Rectangle(0, 0, output.Width, output.Height), Color.White);
                progress = Progress.Processing;
            }

            spriteBatch.DrawString(font,
                    string.Format(
#if XBOX
                    "Mode: {0} - Press B to toggle parallel/sequential\nHold X to see original image\nTime: {1} seconds. Press A to recalculate",
#else
                    "Mode: {0} - Press p to toggle parallel/sequential\nHold space to see original image\nTime: {1} seconds. Press enter to recalculate",
#endif
 currentMode,
                        time.TotalSeconds),
                    new Vector2(50, 50),
                    Color.White);

            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
