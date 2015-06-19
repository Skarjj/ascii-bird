using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System.Text;

namespace ConsoleApplication1
{
    //class StateChangeException : Exception
    //{
    //    public StateChangeException(string message)
    //    {

    //    }
    //}

    class Bird
    {
        public Bird()
        {
            this._pos = 5;
        }

        public void Flap()
        {
            this._flaps = 2;
        }

        public void ResetBird()
        {
            this._flaps = 0;
            this._pos = 5;
        }

        public void UpdatePos(long currentTick, long lastTickAction)
        {
            if (_flaps > 0 && this._pos - 1 >= 0)
            {
                this._pos -= 1;
                this._flaps -=1;
                return;
            }

            if (this._pos == 0)
            {
                this._flaps = 0;
            }

            if (currentTick - lastTickAction < 3)
            {
                return;
            }

            if (currentTick - lastTickAction > 6)
            {
                if (!(this._pos + 1 > 24))
                {
                    this._pos += 1;
                }
            }
            else
            {
                if (!(this._pos + 1 > 24))
                {
                    if (currentTick % 2 == 0)
                    {
                        this._pos += 1;
                    }
                }
            }

        }

        public int Position { get { return _pos; } }
        public int _flaps = 2;
        private int _pos;
    }

    static class Score
    {
        static public void ResetScore()
        {
            CurrentScore = 0;
        }

        static public void SetHighScore(int score)
        {
            if (score > HighScore)
            {
                HighScore = score;
            }
        }

        static public int HighScore { get; private set; }
        static public int CurrentScore { get; set; }
    }

    class Pipes
    {
        public Pipes()
        {
            this._pipeGap = rand.Next(4, 20);
            this._pipeLoc = 1;
            _pipes.Add(this);
        }

        public static void CreatePipe()
        {
            new Pipes();
        }

        public static void ShiftPipes()
        {
            foreach (var item in _pipes)
            {
                item._pipeLoc += 1;
                if (item._pipeLoc == 65)
                {
                    Score.CurrentScore += 1;
                }
            }
            _pipes.RemoveAll(p => p._pipeLoc == 79);
        }

        public static void PrintPipes()
        {
            foreach (var item in _pipes)
            {
                Console.Write("{0}, ", item.PipeLoc);
            }
        }

        public static void ResetPipes()
        {
            _pipes.Clear();
        }

        public int PipeGap { get { return _pipeGap; } }
        public int PipeLoc { get { return _pipeLoc; } }
        private int _pipeGap;
        private int _pipeLoc;

        public static List<Pipes> _pipes = new List<Pipes>();
        protected static Random rand = new Random();
    }

    class Tick
    {
        public Tick() { }

        public void IncrementTick() { currentTick += 1; }
        public long GetCurrentTick { get { return currentTick; } }
        private long currentTick = 0;
    }

    class FiniteStateMachine
    {
        public enum States { PreGame, GameLoop, GameOver, Paused, Quit };
        public States State { get; set; }

        public enum Events { Start, Pause, Resume, Die, Exit };

        private Action[,] fsm;

        public FiniteStateMachine()
        {
            this.fsm = new Action[5, 5] { 
                //Start,        Pause,             Resume,             Die         Exit
                {this.Start,    this.Pause,        null,               this.Died,  this.Exit},  //PreGame
                {null,          this.Pause,        null,               this.Died,  this.Exit},  //GameLoop
                {this.Start,    null,              null,               null,       this.Exit},  //GameOver
                {this.Start,    null,              this.Resume,        null,       this.Exit},  //Paused
                {null,          null,              null,               null,       this.Exit}}; //Quit
        }

        public void ProcessEvent(Events theEvent)
        {
            int state = (int)this.State;
            int theEventOrdinal = (int)theEvent;

            var actionToInvoke = this.fsm[state, theEventOrdinal];

            //if (actionToInvoke == null)
            //{
            //    throw new StateChangeException("That operation can not be performed in this state");
            //}

            actionToInvoke.Invoke();
        }

        private void Start() { this.State = States.GameLoop; }
        private void Pause() { this.State = States.Paused; }
        private void Resume() { this.State = States.GameLoop; }
        private void Died() { this.State = States.GameOver; }
        private void Exit() { this.State = States.Quit; }
    }

    class InputHandler
    {
        internal void HandleInput(ConsoleKeyInfo key, ref FiniteStateMachine fsm, Bird bird, ref long currentTick, ref long lastActionTick)
        {
            if (fsm.State == FiniteStateMachine.States.GameLoop)
            {
                if (key.Key == ConsoleKey.Spacebar)
                {
                    bird.Flap();
                    lastActionTick = currentTick;
                }

                if (key.Key == ConsoleKey.P)
                {
                    fsm.ProcessEvent(FiniteStateMachine.Events.Pause);                    
                }
            }

            if (fsm.State == FiniteStateMachine.States.Paused && key.Key == ConsoleKey.R)
            {
                fsm.ProcessEvent(FiniteStateMachine.Events.Resume);
            }

            if (fsm.State == FiniteStateMachine.States.GameOver && key.Key == ConsoleKey.Spacebar)
            {
                fsm.ProcessEvent(FiniteStateMachine.Events.Start);
                bird.ResetBird();
                Pipes.ResetPipes();
                Score.SetHighScore(Score.CurrentScore);
                Score.ResetScore();
            }

            if (key.Key == ConsoleKey.Q)
            {
                fsm.ProcessEvent(FiniteStateMachine.Events.Exit);
            }
        }
    }

    class Program
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteConsoleOutput(
          SafeFileHandle hConsoleOutput,
          CharInfo[] lpBuffer,
          Coord dwBufferSize,
          Coord dwBufferCoord,
          ref SmallRect lpWriteRegion);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool SetConsoleCursorInfo(
            SafeFileHandle hConsoleOutput,
            ref ConsoleCursorInfo lpConsoleCursorInfo);

        [StructLayout(LayoutKind.Sequential)]
        public struct ConsoleCursorInfo
        {
            public int dwSize;
            public bool bVisible;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Coord
        {
            public short X;
            public short Y;

            public Coord(short X, short Y)
            {
                this.X = X;
                this.Y = Y;
            }
        };

        [StructLayout(LayoutKind.Explicit)]
        public struct CharUnion
        {
            [FieldOffset(0)]
            public char UnicodeChar;
            [FieldOffset(0)]
            public byte AsciiChar;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct CharInfo
        {
            [FieldOffset(0)]
            public CharUnion Char;
            [FieldOffset(2)]
            public short Attributes;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SmallRect
        {
            public short Left;
            public short Top;
            public short Right;
            public short Bottom;
        }

        //Returning a new empty array is quicker that looping through the existing one and filling it with blanks.
        static CharInfo[] ClearBuffer()
        {        
            return new CharInfo[80*25];
        }

        static CharInfo[] FillBuffer(List<Pipes> pipes, Bird bird, CharInfo[] buf, CharInfo[] scorebuf)
        {
            foreach (var item in pipes)
            {
                for (int i = 0; i < buf.Length - 80; i++)
                {
                    if ((i + item.PipeLoc) % 80 == 0 || (i + item.PipeLoc + 1) % 80 == 0)
                    {
                        if (i / 80 == item.PipeGap || i / 80 == item.PipeGap + 1 || i / 80 == item.PipeGap - 1)
                        {
                            buf[i].Attributes = 0;
                            buf[i].Char.UnicodeChar = (char)32;
                       }
                        else
                        {
                            buf[i].Attributes = 10;
                            buf[i].Char.UnicodeChar = (char)35;
                        }
                    }
                }
            }

            for (int i = buf.Length - 80, j = 0; i < buf.Length; i++, j++)
            {
                buf[i] = scorebuf[j];
            }

            buf[15 + (bird.Position * 80)].Attributes = 15;
            buf[15 + (bird.Position * 80)].Char.UnicodeChar = (char)64;

            return buf;
        }

        static CharInfo[] ScoreBuffer(CharInfo[] buf)
        {
            StringBuilder scoreString = new StringBuilder();
            scoreString.Append("Score: ");
            scoreString.AppendFormat("{0}", Score.CurrentScore);
            if (Score.CurrentScore.ToString().Length < 2)
            {
                scoreString.Append("       ");
            }
            else
            {
                scoreString.Append("      ");
            }  
            scoreString.Append("High Score: ");
            scoreString.AppendFormat("{0}", Score.HighScore);
            scoreString.ToString().ToCharArray();

            for (int i = 0; i < scoreString.Length; i++)
            {
                buf[i].Attributes = 11;
                buf[i].Char.UnicodeChar = scoreString[i];
            }
           
            return buf;
        }

        static bool CheckCollision(Bird bird, CharInfo[] buf)
        {
            //Checks hitting a pipe square on
            if ((buf[16 + (bird.Position * 80)].Char.UnicodeChar) == (char)35)
            {
                return true;
            }
              
            if (bird.Position > 0 && bird.Position < 23)
            {
                //Checks rising up to bottom of pipe
                if (bird._flaps > 0 && (buf[16 + ((bird.Position - 1) * 80)].Char.UnicodeChar) == (char)35)
                {
                    return true;
                }
                //Checks falling through top of pipe
                if (bird._flaps > 0 && (buf[16 + ((bird.Position + 1) * 80)].Char.UnicodeChar) == (char)35)
                {
                    return true;
                }
            }
            return false;
        }

        [STAThread]
        static void Main(string[] args)
        {
            SafeFileHandle safeFileHandle = new SafeFileHandle(GetStdHandle(-11), true);

            if (!safeFileHandle.IsInvalid)
            {
                CharInfo[] buf = new CharInfo[80 * 25];
                CharInfo[] scorebuf = new CharInfo[80];
                SmallRect rect = new SmallRect() { Left = 0, Top = 0, Right = 80, Bottom = 25 };
                SmallRect scorerect = new SmallRect() { Left = 0, Top = 24, Right = 80, Bottom = 25 };
                Bird bird = new Bird();
                Tick tickrate = new Tick();
                long lastActionTick = 0;
                FiniteStateMachine fsm = new FiniteStateMachine();
                fsm.State = FiniteStateMachine.States.GameLoop;
                InputHandler handleInput = new InputHandler();
                ConsoleCursorInfo cci = new ConsoleCursorInfo() { dwSize = 1, bVisible = false };
                bool c = SetConsoleCursorInfo(safeFileHandle, ref cci);

                while (fsm.State != FiniteStateMachine.States.Quit)
                {
                    long currentTick = tickrate.GetCurrentTick;

                    if (currentTick % 20 == 0)
                    {
                        Pipes.CreatePipe();
                    }

                    if (Console.KeyAvailable)
                    {
                        ConsoleKeyInfo key = Console.ReadKey(true);
                        handleInput.HandleInput(key, ref fsm, bird, ref currentTick, ref lastActionTick);
                    }

                    if (fsm.State == FiniteStateMachine.States.Paused)
                    {
                        Console.Clear();
                        Console.WriteLine("Paused");
                        while (fsm.State != FiniteStateMachine.States.GameLoop)
                        {
                            ConsoleKeyInfo key = Console.ReadKey(true);
                            handleInput.HandleInput(key, ref fsm, bird, ref currentTick, ref lastActionTick);
                        }
                    }

                    if (CheckCollision(bird, buf))
                    {
                        fsm.ProcessEvent(FiniteStateMachine.Events.Die);

                        while (fsm.State == FiniteStateMachine.States.GameOver)
                        {
                            ConsoleKeyInfo key = Console.ReadKey(true);
                            handleInput.HandleInput(key, ref fsm, bird, ref currentTick, ref lastActionTick);
                        }
                    }

                    buf = ClearBuffer();
                    Pipes.ShiftPipes();
                    bird.UpdatePos(currentTick, lastActionTick);
                    scorebuf = ScoreBuffer(scorebuf);
                    buf = FillBuffer(Pipes._pipes, bird, buf, scorebuf);

                    bool b = WriteConsoleOutput(safeFileHandle, buf,
                        new Coord() { X = 80, Y = 25 },
                        new Coord() { X = 0, Y = 0 },
                        ref rect);

                    tickrate.IncrementTick();
                    Thread.Sleep(85);
                }

                if (fsm.State == FiniteStateMachine.States.Quit)
                {
                    Console.Clear();
                    Console.WriteLine("Exiting! Goodbye");
                }
            }
            Console.ReadKey();
        }
    }
}