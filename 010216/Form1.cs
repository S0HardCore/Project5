using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace _010216
{
    public partial class Form1 : Form
    {
        #region Constants
        const int
            LEVELS = 2,
            MAX_RAYS = 100,
            GAME_HEIGHT = 6,
            GAME_WIDTH = 12,
            COLOR_MIN_VALUE = 63,
            COLOR_MAX_VALUE = 192;
        enum GAME_STATES
        {
            PAUSED = 0,
            ACTIVE = 1,
            MENU = 2
        }
        enum BLOCK_TYPES
        {
            NORMAL = 1,
            SOLID = 2,
            GLASS = 3,
            PORTAL = 4,
            NOBLOCK = 5,
            DIAMOND = 6
        }
        static readonly string[]
            LevelNames = new string[LEVELS]
            {
                "first",
                "last"
            };
        static readonly Random
            getRandom = new Random(DateTime.Now.Millisecond);
        static readonly Font
            Verdana13 = new Font("Verdana", 13);
        static readonly StringFormat
            TextFormatCenter = new StringFormat();
        static readonly Image
            iBlockNormal = Properties.Resources.BlockMetal,
            iBlockSolid = Properties.Resources.BlockStone,
            iBlockGlass = Properties.Resources.BlockGlass,
            iEnd = Properties.Resources.End,
            iEndLaser = Properties.Resources.EndLaser,
            iGlassPanelCorners = Properties.Resources.glassPanelCorners,
            iConsole = Properties.Resources.glassPanelConsole;
        static readonly Size
            Resolution = Screen.PrimaryScreen.Bounds.Size;
        static readonly Rectangle
            NEXT_LEVEL_RECTANGLE = new Rectangle(Resolution.Width / 2 - 150, Resolution.Height / 2 - 50, 300, 100),
            YES_RECTANGLE = new Rectangle(NEXT_LEVEL_RECTANGLE.X + 90, NEXT_LEVEL_RECTANGLE.Y + 55, 120, 30),
            GAME_RECTANGLE = new Rectangle((Resolution.Width - GAME_WIDTH * 100) / 2, 0 + (Resolution.Height - GAME_HEIGHT * 100) / 2, GAME_WIDTH * 100, GAME_HEIGHT * 100);
        static readonly Point[]
            lStartPoint = new Point[LEVELS]
            {
                new Point(GAME_RECTANGLE.X + 200, GAME_RECTANGLE.Y + 50),
                new Point(GAME_RECTANGLE.X + 300, GAME_RECTANGLE.Y + 50)
            },
            lDirection = new Point[LEVELS]
            {
                new Point(-1, 1),
                new Point(1, 1)
            };
        static readonly Rectangle[]
            lEndPoint = new Rectangle[LEVELS]
            {
                new Rectangle(GAME_RECTANGLE.X + 290, GAME_RECTANGLE.Y + 40, 20, 20),
                new Rectangle(GAME_RECTANGLE.X + 590, GAME_RECTANGLE.Y + 140, 20, 20)
            };
        static readonly HatchBrush 
            blockBrush = new HatchBrush(HatchStyle.Trellis, Color.DarkSlateBlue, Color.Black),
            blockBrushTransparent = new HatchBrush(HatchStyle.Trellis, Color.FromArgb(128, 72, 61, 139), Color.FromArgb(128, 0, 0, 0));
        static Timer updateTimer = new Timer();
        #endregion

        class Block
        {
            private Rectangle Rectangle;
            private BLOCK_TYPES Type = BLOCK_TYPES.NORMAL;
            public Block(Rectangle _Rectangle)
            {
                Rectangle = _Rectangle;
            }
            public Block(Rectangle _Rectangle, BLOCK_TYPES _Type):this(_Rectangle)
            {
                Type = _Type;
            }
            public BLOCK_TYPES getType()
            {
                return Type;
            }
            public void setRectangle(Rectangle _Rectangle)
            {
                Rectangle = _Rectangle;
            }
            public Rectangle getRectangle()
            {
                return Rectangle;
            }
        }

        class ConsolePrototype
        {
            public Boolean Enabled;

            private string consoleString;
            private string consolePrevString;
            private string consoleLog;
            private Rectangle CONSOLE_REGION;

            public string getString() { return consoleString; }
            public string getPrevString() { return consolePrevString; }
            public string getLog() { return consoleLog; }
            public int getLength() { return consoleString.Length; }
            public void setString(string String) { consoleString = String; }
            public void setPrevString(string String) { consolePrevString = String; }
            public void setLog(string String) { consoleLog = String; }
            public Rectangle getRegion()
            {
                CONSOLE_REGION = new Rectangle(0, 0, 520, 50);
                return CONSOLE_REGION;
            }
            public void applyCommand()
            {
                consoleString = consoleString.Trim();
                if (consoleLog == "Unknown command." || consoleLog == "Still unknown command.")
                    consoleLog = "Still unknown command.";
                else
                    consoleLog = "Unknown command.";
                switch (consoleString)
                {
                    case "OUTPUT":
                    case "DEBUG":
                    case "INFORMATION":
                    case "INFO":
                    case "DEBUGINFO":
                    case "DEBUGINFORMATION":
                        if (ShowDI)
                        {
                            ShowDI = false;
                            consoleLog = "Debug output is disabled.";
                        }
                        else
                        {
                            ShowDI = true;
                            consoleLog = "Debug output is enabled.";
                        }
                        break;
                }
                consolePrevString = consoleString;
                if (consoleString.Length > 0)
                    consoleString = consoleString.Remove(0);
            }
        }

        class Laser
        {
            public Point Start;
            public Point End;
            public Point Direction;
            public Boolean Ends = false;
            public Laser(Point _Start, Point _Direction)
            {
                Start = _Start;
                Direction = _Direction;
                this.Refresh();
            }
            public Laser(int x, int y, int ax, int ay)
            {
                Start = new Point(x, y);
                Direction = new Point(ax, ay);
                this.Refresh();
            }
            public void Refresh()
            {
                Point next = Start;
            Mark:
                next.Offset(Direction);
                if (!GAME_RECTANGLE.Contains(next) || SolidBlocks.IsVisible(next))
                    Ends = true;
                else
                {
                    Ends = false;
                    if (EmptySpace.IsVisible(next))
                        goto Mark;
                }
                End = next;
                    if (lEndPoint[CurrentLevel].Contains(End) && !ChangingLevel)
                    {
                        ChangingLevel = true;
                        LevelPassageTime = (Time - StartupTime) / 100f;
                        GameState = GAME_STATES.PAUSED;
                    }
                    else
                        ChangingLevel = false;
            }
        }

        class BackGroundColor
        {
            private int R;
            private int G;
            private int B;
            private int RFactor = getRandom.Next(-1, 2);
            private int GFactor = getRandom.Next(-1, 2);
            private int BFactor = getRandom.Next(-1, 2);
            public BackGroundColor(int _R, int _G, int _B)
            {
                R = _R;
                G = _G;
                B = _B;
            }
            public void Increase(int r, int g, int b)
            {
                R += r;
                G += g;
                B += b;
                this.Limits();
            }
            public void Increase(Boolean Factor)
            {
                R += RFactor;
                G += GFactor;
                B += BFactor;
                this.Limits();
            }
            public void Limits()
            {
                if (R > COLOR_MAX_VALUE)
                    RFactor = -1;
                else
                    if (R < COLOR_MIN_VALUE)
                        RFactor = 1;
                if (G > COLOR_MAX_VALUE)
                    GFactor = -1;
                else
                    if (G < COLOR_MIN_VALUE)
                        GFactor = 1;
                if (B > COLOR_MAX_VALUE)
                    BFactor = -1;
                else
                    if (B < COLOR_MIN_VALUE)
                        BFactor = 1;
            }
            public void randomFactors()
            {
            Mark:
                RFactor = getRandom.Next(-1, 2);
                GFactor = getRandom.Next(-1, 2);
                BFactor = getRandom.Next(-1, 2);
                if (RFactor == 0 && GFactor == 0 && BFactor == 0)
                    goto Mark;
            }
            public Color Set()
            {
                return Color.FromArgb(R, G, B);
            }
            public void setRandomColor()
            {
                R = getRandom.Next(COLOR_MIN_VALUE, COLOR_MAX_VALUE);
                G = getRandom.Next(COLOR_MIN_VALUE, COLOR_MAX_VALUE);
                B = getRandom.Next(COLOR_MIN_VALUE, COLOR_MAX_VALUE);
            }
        }

        #region Variables
        static Region
            EmptySpace = new Region(GAME_RECTANGLE),
            SolidBlocks = new Region();
        static string 
            QuartzFont = "Quartz MS";
        static Boolean
            ChangingLevel = false,
            ShowDI = false;
        static List<Block>
            Blocks = new List<Block>();
        static List<Laser> 
            Lasers = new List<Laser>();
        static BackGroundColor
            BGColor;
        static ConsolePrototype
            Console = new ConsolePrototype();
        static int
            lastTick, lastFrameRate, frameRate,
            CurrentLevel = 0, SelectedBlock = -1;
        static Point
            MoveStartPosition = new Point();
        static long 
            StartupTime = DateTime.Now.Ticks / 100000,
            Time = DateTime.Now.Ticks / 100000;
        static double
            LevelPassageTime = 0f;
        static GAME_STATES
            GameState;
        #endregion

        static int[, ,] map = new int[LEVELS, GAME_HEIGHT, GAME_WIDTH]
        {
            {
                { 1, 0, 2, 0, 0, 1, 0, 0, 1, 0, 0, 1},
                { 0, 0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0},
                { 0, 0, 0, 0, 1, 0, 0, 1, 0, 0, 0, 0},
                { 1, 0, 0, 1, 0, 1, 1, 0, 0, 0, 1, 0},
                { 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 1},
                { 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 1, 1}
            },
            {
                { 0, 0, 2, 0, 0, 1, 0, 0, 1, 0, 0, 0},
                { 1, 0, 0, 1, 0, 2, 0, 0, 0, 0, 0, 1},
                { 0, 0, 0, 0, 1, 0, 2, 1, 0, 0, 0, 0},
                { 0, 0, 0, 1, 0, 1, 1, 2, 0, 0, 1, 1},
                { 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0},
                { 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 1, 0}
            }
        };

        public Form1()
        {
            InitializeComponent();
            GameState = GAME_STATES.ACTIVE;
            TextFormatCenter.Alignment = StringAlignment.Center;
            BGColor = new BackGroundColor(240, 254, 254);
            foreach (FontFamily Family in FontFamily.Families)
                if (Family.Name.ToUpper() == "QUARTZ" || Family.Name.ToUpper() == "QUARTZ MS")
                    QuartzFont = Family.Name;
            Setup();
            this.Size = Resolution;
            this.Paint += new PaintEventHandler(pDraw);
            this.KeyDown += new KeyEventHandler(pKeyDown);
            this.KeyUp += new KeyEventHandler(pKeyUp);
            this.MouseMove += new MouseEventHandler(pMouseMove);
            this.MouseUp += new MouseEventHandler(pMouseUp);
            this.MouseDown += new MouseEventHandler(pMouseDown);
            updateTimer.Interval = 1;
            updateTimer.Tick += new EventHandler(pUpdate);
            updateTimer.Start();
        }

        static void Setup()
        {
            Lasers.Clear();
            Blocks.Clear();
            for (int q = 0; q < GAME_HEIGHT; ++q)
                for (int w = 0; w < GAME_WIDTH; ++w)
                    switch (map[CurrentLevel, q, w])
                    {
                        case (int)BLOCK_TYPES.NORMAL:
                            Blocks.Add(new Block(new Rectangle(GAME_RECTANGLE.X + 100 * w, GAME_RECTANGLE.Y + 100 * q, 100, 100)));
                            break;
                        case (int)BLOCK_TYPES.SOLID:
                            Blocks.Add(new Block(new Rectangle(GAME_RECTANGLE.X + 100 * w, GAME_RECTANGLE.Y + 100 * q, 100, 100), BLOCK_TYPES.SOLID));
                            break;
                    }
            Lasers.Add(new Laser(lStartPoint[CurrentLevel], lDirection[CurrentLevel]));
            Lasers[0].Ends = false;
        }

        void pKeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.Tab)
                if (!Console.Enabled)
                    Console.Enabled = true;
                else
                {
                    Console.Enabled = false;
                    if (!String.IsNullOrEmpty(Console.getString()))
                        Console.setString("");
                }
        }

        void pKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.Enter)
                NextLevelTransition();
            switch (e.KeyData)
            {
                case Keys.Escape:
                    Application.Exit();
                    break;
            }
            if (Console.Enabled)
                #region Console
            {
                if ((e.KeyData >= Keys.A && e.KeyData <= Keys.Z) ||
                    (e.KeyData >= Keys.D0 && e.KeyData <= Keys.D9))
                        Console.setString(Console.getString() + (char)e.KeyValue);
                switch (e.KeyData)
                {
                    case Keys.Back:
                        if (Console.getLength() > 0)
                            Console.setString(Console.getString().Remove(Console.getLength() - 1, 1));
                        break;
                    case Keys.Enter:
                        if (!String.IsNullOrEmpty(Console.getString()))
                            Console.applyCommand();
                        break;
                }
            }
                #endregion
            else
                switch (e.KeyData)
                {
                    case Keys.P:
                        if (!ChangingLevel)
                            if (GameState == GAME_STATES.PAUSED)
                                GameState = GAME_STATES.ACTIVE;
                            else
                                GameState = GAME_STATES.PAUSED;
                        break;
                }
        }

        static void NextLevelTransition()
        {
            if (ChangingLevel)
                if (CurrentLevel < LEVELS - 1)
                {
                    StartupTime = DateTime.Now.Ticks / 100000;
                    CurrentLevel++;
                    Setup();
                    GameState = GAME_STATES.ACTIVE;
                }
                else
                    Application.Exit();
        }

        void pMouseDown(object sender, MouseEventArgs e)
        {
            switch (GameState)
            {
                case GAME_STATES.ACTIVE:
                    foreach (Block TB in Blocks)
                        if (TB.getType() != BLOCK_TYPES.SOLID && TB.getRectangle().Contains(e.Location))
                        {
                            SelectedBlock = Blocks.IndexOf(TB);
                            MoveStartPosition = new Point(TB.getRectangle().X, TB.getRectangle().Y);
                        }
                    break;
            }
        }

        void pMouseUp(object sender, MouseEventArgs e)
        {
            if (YES_RECTANGLE.Contains(e.Location))
                NextLevelTransition();
            switch (GameState)
            {
                case GAME_STATES.ACTIVE:
                    if (SelectedBlock > -1)
                        if (!GAME_RECTANGLE.Contains(e.Location))
                            Blocks[SelectedBlock].setRectangle(new Rectangle(MoveStartPosition.X, MoveStartPosition.Y, 100, 100));
                        else
                            foreach (Block TB in Blocks)
                            {
                                Point TP = new Point(GAME_RECTANGLE.X + ((e.X - GAME_RECTANGLE.X) / 100) * 100, GAME_RECTANGLE.Y + ((e.Y - GAME_RECTANGLE.Y) / 100) * 100);
                                if (TB.getRectangle().Contains(TP.X + 5, TP.Y + 5) && Blocks.IndexOf(TB) != SelectedBlock)
                                {
                                    Blocks[SelectedBlock].setRectangle(new Rectangle(MoveStartPosition.X, MoveStartPosition.Y, 100, 100));
                                    break;
                                }
                                else
                                    Blocks[SelectedBlock].setRectangle(new Rectangle(TP.X, TP.Y, 100, 100));
                            }
                    SelectedBlock = -1;
                    break;
            }
        }

        void pMouseMove(object sender, MouseEventArgs e)
        {
            switch (GameState)
            {
                case GAME_STATES.ACTIVE:
                    if (SelectedBlock > -1)
                    {
                        Blocks[SelectedBlock].setRectangle(new Rectangle(e.X - 50, e.Y - 50, 100, 100));
                    }
                    break;
            }
        }

        void pUpdate(object sender, EventArgs e)
        {
            Time = DateTime.Now.Ticks / 100000;
            BGColor.Increase(true);
            this.BackColor = BGColor.Set();
            if (getRandom.Next(50) == 0)
                BGColor.randomFactors();
            switch (GameState)
            {
                case GAME_STATES.ACTIVE:
                    EmptySpace = new Region(GAME_RECTANGLE);
                    SolidBlocks.MakeEmpty();
                    foreach (Block TB in Blocks)
                    {
                        if (Blocks.IndexOf(TB) != SelectedBlock)
                        {
                            EmptySpace.Exclude(TB.getRectangle());
                            if (TB.getType() == BLOCK_TYPES.SOLID)
                                SolidBlocks.Union(TB.getRectangle());
                        }
                    }
                    Lasers.RemoveRange(1, Lasers.Count - 1);
                    foreach (Laser TL in Lasers)
                        TL.Refresh();
                    if (Lasers.Count < MAX_RAYS && Lasers.Count > 0)
                        for (int q = 0; q < Lasers.Count; ++q)
                        {
                            if (!Lasers[q].Ends)
                                Lasers.Add(new Laser(Lasers[q].End, Direct(Lasers[q].Direction)));
                            else
                                break;
                        }
                    break;
            }
                    Invalidate();
        }

        void pDraw(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.FillRectangle(Brushes.SlateGray, GAME_RECTANGLE);
            Pen RayPen = new Pen(Color.Red, 6);
            RayPen.EndCap = RayPen.StartCap = LineCap.Triangle;
            foreach (Block TB in Blocks)
                switch (TB.getType())
                {
                    case BLOCK_TYPES.NORMAL:
                        g.DrawImage(iBlockNormal, TB.getRectangle());
                        break;
                    case BLOCK_TYPES.SOLID:
                        g.DrawImage(iBlockSolid, TB.getRectangle());
                        break;
                }
            if (GameState != GAME_STATES.MENU)
            {
                foreach (Laser TL in Lasers)
                    g.DrawLine(RayPen, TL.Start, TL.End);
                if (SelectedBlock > -1)
                    g.DrawImage(Blocks[SelectedBlock].getType() == BLOCK_TYPES.NORMAL ? iBlockNormal : iBlockSolid, Blocks[SelectedBlock].getRectangle());
            }
            g.DrawImage(ChangingLevel ? iEndLaser : iEnd, lEndPoint[CurrentLevel]);
            if (ChangingLevel)
            {
                g.DrawImage(iGlassPanelCorners, NEXT_LEVEL_RECTANGLE);
                g.DrawImage(iGlassPanelCorners, YES_RECTANGLE);
                g.DrawString("Congratulations, you pass a " + LevelNames[CurrentLevel] + " level over " + LevelPassageTime + " sec.", new Font("Kristen ITC", 13), Brushes.Black,
                    new Rectangle(NEXT_LEVEL_RECTANGLE.X + 5, NEXT_LEVEL_RECTANGLE.Y + 5, NEXT_LEVEL_RECTANGLE.Width - 10, NEXT_LEVEL_RECTANGLE.Height - 10), TextFormatCenter);
                g.DrawString(CurrentLevel < LEVELS - 1? "Next level" : "Exit", new Font("Kristen ITC", 15), Brushes.Black, 
                    new Rectangle(YES_RECTANGLE.X + 3, YES_RECTANGLE.Y + 3, YES_RECTANGLE.Width - 6, YES_RECTANGLE.Height - 6), TextFormatCenter);
            }
            if (ShowDI)
                #region Debug Information
            {
                string output = "FPS: " + CalculateFrameRate().ToString() +
                    "\nGame State: " + GameState.ToString() + 
                    "\nLevel: " + (CurrentLevel + 1).ToString() + 
                    "\nChanging: " + ChangingLevel.ToString() +
                    "\nActual Laser Count: " + Lasers.Count +
                    "\nStartup: " + StartupTime.ToString() + 
                    "\nTime: " + Time.ToString();
                g.DrawString(output, new Font(QuartzFont, 14), Brushes.Black, 0, Console.Enabled ? 50 : 0);
            }
                #endregion
            if (Console.Enabled)
                #region Console
            {
                g.DrawString("Console: ", Verdana13, Brushes.Black, 3, 25);
                g.DrawString(Console.getLog(), Verdana13, Brushes.Black, Console.getRegion().X + 3, Console.getRegion().Y);
                g.DrawString(Console.getPrevString(), Verdana13, Brushes.Black, new Rectangle(100, 0, 423, 20), TextFormatCenter);
                g.DrawString(Console.getString(), Verdana13, Brushes.Black, new Rectangle(81, 25, 460, 20));
                g.DrawImage(iConsole, Console.getRegion());
            }
                #endregion
        }

        static Point Direct(Point Init)
        {
            Point P = Init;
            if (P.X == 1 && P.Y == 1)
                P = new Point(1, -1);
            else
                if (P.X == 1 && P.Y == -1)
                    P = new Point(-1, -1);
                else
                    if (P.X == -1 && P.Y == -1)
                        P = new Point(-1, 1);
                    else
                        P = new Point(1, 1);
           return P;
        }

        #region Secondary Functions

        int CalculateFrameRate()
        {
            if (System.Environment.TickCount - lastTick >= 1000)
            {
                lastFrameRate = frameRate;
                frameRate = 0;
                lastTick = System.Environment.TickCount;
            }
            frameRate++;
            return lastFrameRate;
        }
        /*
        static float Cos(int _Direction)
        {
            return (float)Math.Cos(Math.PI * _Direction / 180);
        }
        static float Sin(int _Direction)
        {
            return (float)Math.Sin(Math.PI * _Direction / 180);
        }
        static float AngleBetween(Point One, Point Two)
        {
            double x1 = One.X,
                   x2 = Two.X,
                   y1 = One.Y,
                   y2 = Two.Y;
            double Angle = Math.Atan2(y1 - y2, x1 - x2) / Math.PI * 180;
            Angle = (Angle < 0) ? Angle + 360 : Angle;
            return (float)Angle;
        }*/
        #endregion
    }
}
