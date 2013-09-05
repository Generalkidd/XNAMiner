using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using ParallelTasks;

namespace XNAMiner
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>

    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        static Pool _pool = null;
        static Work _work = null;
        static uint _nonce = 0;
        static long _maxAgeTicks = 20000 * TimeSpan.TicksPerMillisecond;
        static uint _batchSize = 100000;
        SpriteFont font1;
        string message1;
        string errorMessage;
        bool println = false;
        bool error = false;
        bool retry = false;
        bool select = false;
        bool mineContinue = false;
        bool consoleClear = false;
        int count = 0;

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
            graphics.PreferredBackBufferHeight = 720;
            graphics.PreferredBackBufferWidth = 1280;
            graphics.ApplyChanges();
            IsMouseVisible = true;
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
            font1 = Content.Load<SpriteFont>("SpriteFont1");
            // TODO: use this.Content to load your game content here
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
            {
                this.Exit();
            }

            if (Keyboard.GetState(PlayerIndex.One).IsKeyDown(Keys.Escape))
            {
                this.Exit();
            }

            //ThreadStart threadStarter = delegate 
            //{

            //}; 
            //Thread loadingThread = new Thread(threadStarter); 
            //loadingThread.Start();

            try
            {
                _pool = SelectPool();
                _work = GetWork();
                //while (true)
                //{
                    if (_work == null || _work.Age > _maxAgeTicks)
                        _work = GetWork();

                    if (_work.FindShare(ref _nonce, _batchSize))
                    {
                        SendShare(_work.Current);
                        _work = null;
                    }
                    else
                        PrintCurrentState();
                //}
            }
            catch (Exception e)
            {
                errorMessage = e.Message;
                error = true;
                retry = true;
                //Console.WriteLine();
                //Console.Write("ERROR: ");
                //Console.WriteLine(e.Message);
            }
            //Console.WriteLine();
            //Console.Write("Hit 'Enter' to try again...");
            //Console.ReadLine();

            // TODO: Add your update logic here

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            // TODO: Add your drawing code here
            if (consoleClear == true)
            {
                spriteBatch.Begin();
                spriteBatch.DrawString(font1, "*****************************", new Vector2(10, 20), Color.White);
                spriteBatch.DrawString(font1, "********* XNA Miner *********", new Vector2(10, 40), Color.White);
                spriteBatch.DrawString(font1, "*****************************", new Vector2(10, 60), Color.White);
                consoleClear = false;
                spriteBatch.End();
            }

            if (println == true)
            {
                spriteBatch.Begin();
                spriteBatch.DrawString(font1, "Data: " + Utils.ToString(_work.Data), new Vector2(10, 100), Color.White);
                spriteBatch.DrawString(font1, "Nonce: " + Utils.ToString(_nonce), new Vector2(10, 128), Color.White);
                spriteBatch.DrawString(font1, "Hash: " + Utils.ToString(_work.Hash), new Vector2(10, 148), Color.White);
                spriteBatch.DrawString(font1, message1, new Vector2(10, 168), Color.White);

                //println = false;
                spriteBatch.End();
            }

            if (select == true)
            {
                spriteBatch.Begin();
                //spriteBatch.DrawString(font1, "Select Pool: ", new Vector2(10, 128), Color.White);
                select = false;
                spriteBatch.End();
            }

            if (mineContinue == true)
            {
                spriteBatch.Begin();
                spriteBatch.DrawString(font1, "Hit 'Enter' to continue...", new Vector2(10, 200), Color.White);
                mineContinue = false; 
                spriteBatch.End();
            }

            if (error == true)
            {
                spriteBatch.Begin();
                spriteBatch.DrawString(font1, errorMessage, new Vector2(10, 148), Color.White);
                error = false;
                spriteBatch.End();
            }

            if (retry == true)
            {
                spriteBatch.Begin();
                spriteBatch.DrawString(font1, "Hit 'Enter' to try again...", new Vector2(10, 168), Color.White);
                retry = false;
                spriteBatch.End();
            }

            base.Draw(gameTime);
        }

        void ClearConsole()
        {
            consoleClear = true;
            //Console.Clear();
            //Console.WriteLine("*****************************");
            //Console.WriteLine("*** Minimal Bitcoin Miner ***");
            //Console.WriteLine("*****************************");
            //Console.WriteLine();
        }

        Pool SelectPool()
        {
            ClearConsole();
            Print("Chose a Mining Pool 'user:password@url:port' or leave empty to skip.");
            select = true;
            //Console.Write("Select Pool: ");
            string login = ReadLineDefault("lithander_2:foo@btcguild.com:8332"); //Pool must be hard coded until user input is implemented.
            return new Pool(login);
        }

        Work GetWork()
        {
            ClearConsole();
            Print("Requesting Work from Pool...");
            Print("Server URL: " + _pool.Url.ToString());
            Print("User: " + _pool.User);
            Print("Password: " + _pool.Password);
            return _pool.GetWork();
        }

        void SendShare(byte[] share)
        {
            ClearConsole();
            Print("*** Found Valid Share ***");
            Print("Share: " + Utils.ToString(_work.Current));
            Print("Nonce: " + Utils.ToString(_nonce));
            Print("Hash: " + Utils.ToString(_work.Hash));
            Print("Sending Share to Pool...");
            if (_pool.SendShare(share))
                Print("Server accepted the Share!");
            else
                Print("Server declined the Share!");
            mineContinue = true;
            //Console.Write("Hit 'Enter' to continue...");
            //Console.ReadLine();
        }

        DateTime _lastPrint = DateTime.Now;

        void PrintCurrentState()
        {
            ClearConsole();
            Print("Data: " + Utils.ToString(_work.Data));
            string current = Utils.ToString(_nonce);
            string max = Utils.ToString(uint.MaxValue);
            double progress = ((double)_nonce / uint.MaxValue) * 100;
            Print("Nonce: " + current + "/" + max + " " + progress.ToString("F2") + "%");
            Print("Hash: " + Utils.ToString(_work.Hash));
            TimeSpan span = DateTime.Now - _lastPrint;
            Print("Speed: " + (int)(((_batchSize) / 1000) / span.TotalSeconds) + "Kh/s");
            _lastPrint = DateTime.Now;
        }

        void Print(string msg)
        {
            message1 = msg;
            count++;
            println = true;
            //Console.WriteLine(msg);
            //Console.WriteLine();
        }

        private static string ReadLineDefault(string defaultValue)
        {
            //Allow Console.ReadLine with a default value
            string userInput = ""; //Need a way to get user input for their mining pool!
            //Console.WriteLine();
            if (userInput == "")
                return defaultValue;
            else
                return userInput;
        }

    }

    class Utils
    {
        public static byte[] ToBytes(string input)
        {
            byte[] bytes = new byte[input.Length / 2];
            for (int i = 0, j = 0; i < input.Length; j++, i += 2)
                bytes[j] = byte.Parse(input.Substring(i, 2), System.Globalization.NumberStyles.HexNumber);

            return bytes;
        }

        public static string ToString(byte[] input)
        {
            string result = "";
            foreach (byte b in input)
                result += b.ToString("x2");

            return result;
        }

        public static string ToString(uint value)
        {
            string result = "";
            foreach (byte b in BitConverter.GetBytes(value))
                result += b.ToString("x2");

            return result;
        }

        public static string EndianFlip32BitChunks(string input)
        {
            //32 bits = 4*4 bytes = 4*4*2 chars
            string result = "";
            for (int i = 0; i < input.Length; i += 8)
                for (int j = 0; j < 8; j += 2)
                {
                    //append byte (2 chars)
                    result += input[i - j + 6];
                    result += input[i - j + 7];
                }
            return result;
        }

        public static string RemovePadding(string input)
        {
            //payload length: final 64 bits in big-endian - 0x0000000000000280 = 640 bits = 80 bytes = 160 chars
            return input.Substring(0, 160);
        }

        public static string AddPadding(string input)
        {
            //add the padding to the payload. It never changes.
            return input + "000000800000000000000000000000000000000000000000000000000000000000000000000000000000000080020000";
        }
    }

    class Work
    {
        public Work(byte[] data)
        {
            Data = data;
            Current = (byte[])data.Clone();
            _nonceOffset = Data.Length - 4;
            _ticks = DateTime.Now.Ticks;
            _hasher = new SHA256Managed();

        }
        private SHA256Managed _hasher;
        private long _ticks;
        private long _nonceOffset;
        public byte[] Data;
        public byte[] Current;

        internal bool FindShare(ref uint nonce, uint batchSize)
        {
            for (; batchSize > 0; batchSize--)
            {
                BitConverter.GetBytes(nonce).CopyTo(Current, _nonceOffset);
                byte[] doubleHash = Sha256(Sha256(Current));

                //count trailing bytes that are zero
                int zeroBytes = 0;
                for (int i = 31; i >= 28; i--, zeroBytes++)
                    if (doubleHash[i] > 0)
                        break;

                //standard share difficulty matched! (target:ffffffffffffffffffffffffffffffffffffffffffffffffffffffff00000000)
                if (zeroBytes == 4)
                    return true;

                //increase
                if (++nonce == uint.MaxValue)
                    nonce = 0;
            }
            return false;
        }

        private byte[] Sha256(byte[] input)
        {
            byte[] crypto = _hasher.ComputeHash(input, 0, input.Length);
            return crypto;
        }

        public byte[] Hash
        {
            get { return Sha256(Sha256(Current)); }
        }

        public long Age
        {
            get { return DateTime.Now.Ticks - _ticks; }
        }
    }

    class Pool
    {
        public Uri Url;
        public string User;
        public string Password;

        public Pool(string login)
        {
            int urlStart = login.IndexOf('@');
            int passwordStart = login.IndexOf(':');
            string user = login.Substring(0, passwordStart);
            string password = login.Substring(passwordStart + 1, urlStart - passwordStart - 1);
            string url = "http://" + login.Substring(urlStart + 1);
            Url = new Uri(url);
            User = user;
            Password = password;
        }

        private string InvokeMethod(string method, string paramString = null)
        {
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(Url);
            webRequest.Credentials = new NetworkCredential(User, Password);
            webRequest.ContentType = "application/json-rpc";
            webRequest.Method = "POST";

            string jsonParam = (paramString != null) ? "\"" + paramString + "\"" : "";
            string request = "{\"id\": 0, \"method\": \"" + method + "\", \"params\": [" + jsonParam + "]}";

            // serialize json for the request
            byte[] byteArray = Encoding.UTF8.GetBytes(request);
            webRequest.ContentLength = byteArray.Length;
            using (Stream dataStream = webRequest.GetRequestStream())
                dataStream.Write(byteArray, 0, byteArray.Length);

            string reply = "";
            using (WebResponse webResponse = webRequest.GetResponse())
            using (Stream str = webResponse.GetResponseStream())
            using (StreamReader reader = new StreamReader(str))
                reply = reader.ReadToEnd();

            return reply;
        }

        public Work GetWork(bool silent = false)
        {
            return new Work(ParseData(InvokeMethod("getwork")));
        }

        private byte[] ParseData(string json)
        {
            Match match = Regex.Match(json, "\"data\": \"([A-Fa-f0-9]+)");
            if (match.Success)
            {
                string data = Utils.RemovePadding(match.Groups[1].Value);
                data = Utils.EndianFlip32BitChunks(data);
                return Utils.ToBytes(data);
            }
            throw new Exception("Didn't find valid 'data' in Server Response");
        }

        public bool SendShare(byte[] share)
        {
            string data = Utils.EndianFlip32BitChunks(Utils.ToString(share));
            string paddedData = Utils.AddPadding(data);
            string jsonReply = InvokeMethod("getwork", paddedData);
            Match match = Regex.Match(jsonReply, "\"result\": true");
            return match.Success;
        }
    }
}
