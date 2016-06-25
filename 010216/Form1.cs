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
            MAX_RAYS = 32,
            GAME_HEIGHT = 6,
            GAME_WIDTH = 12,
            COLOR_MIN_VALUE = 63,
            COLOR_MAX_VALUE = 192;
        enum GAME_STATES
        {
            PAUSED = 0,
            ACTIVE = 1,
            MENU = 2,
            EDITOR = 3
        }
        enum BLOCK_TYPES
        {
            NORMAL = 1,
            SOLID = 2,
            GLASS = 3,
            DIAMOND = 4,
            NOBLOCK = 5,
            PORTAL = 6
        }
        enum CONTEXT_MENUS
        {
            NONE,
            REMOVE,
            CREATEorSETEMPTY,
            CREATEorFILL,
            BLOCKTYPES
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
            Rockwell16 = new Font("Rockwell", 16, FontStyle.Bold),
            Rockwell18 = new Font("Rockwell", 18, FontStyle.Bold),
            Verdana13 = new Font("Verdana", 13);
        static readonly StringFormat
            TextFormatCenterAll = new StringFormat(),
            TextFormatCenterHor = new StringFormat();
        static readonly Pen
            RayPen = new Pen(Color.Red, 6);
        static readonly SolidBrush
            BlueButtonBrush = new SolidBrush(Color.FromArgb(36, 156, 207)),
            GreenButtonBrush = new SolidBrush(Color.FromArgb(111, 195, 73));
        static readonly Image
            iBlockNormal = Properties.Resources.BlockMetal,
            iBlockSolid = Properties.Resources.BlockStone,
            iBlockGlass = Properties.Resources.BlockGlass,
            iEnd = Properties.Resources.End,
            iEndLaser = Properties.Resources.EndLaser,
            iGlassPanelCorners = Properties.Resources.glassPanelCorners,
            iGlassPanelCornersFill = Properties.Resources.glassPanelCornersFill,
            iPlaceForBlock = Properties.Resources.BlockCanPlace,
            iEmptySpace = Properties.Resources.BlockCantPlace,
            iMenuPanel = Properties.Resources.menuPanel,
            iGreenButton = Properties.Resources.GreenButtonFill,
            iBlueButton = Properties.Resources.BlueButtonFill,
            iTransGreenButton = Properties.Resources.GreenButtonEmpty,
            iTransBlueButton = Properties.Resources.BlueButtonEmpty,
            iConsole = Properties.Resources.glassPanelConsole;
        static readonly Size
            Resolution = Screen.PrimaryScreen.Bounds.Size;
        static readonly Rectangle
            NEXT_LEVEL_RECTANGLE = new Rectangle(Resolution.Width / 2 - 150, Resolution.Height / 2 - 50, 300, 100),
            YES_RECTANGLE = new Rectangle(NEXT_LEVEL_RECTANGLE.X + 90, NEXT_LEVEL_RECTANGLE.Y + 55, 120, 30),
            MENU_RECTANGLE = new Rectangle(Resolution.Width / 2 - 100, Resolution.Height / 2 - 150, 200, 300),
            CONTINUE_BUTTON_RECTANGLE = new Rectangle(MENU_RECTANGLE.X + 25, MENU_RECTANGLE.Y + 25, 150, 50),
            EDITOR_BUTTON_RECTANGLE = new Rectangle(MENU_RECTANGLE.X + 40, MENU_RECTANGLE.Y + 105, 120, 40),
            EXIT_BUTTON_RECTANGLE = new Rectangle(MENU_RECTANGLE.X + 55, MENU_RECTANGLE.Y + 250, 90, 30),
            GAME_RECTANGLE = new Rectangle((Resolution.Width - GAME_WIDTH * 100) / 2, 0 + (Resolution.Height - GAME_HEIGHT * 100) / 2, GAME_WIDTH * 100, GAME_HEIGHT * 100),
            RETURNFROMEDIT_BUTTON_RECTANGLE = new Rectangle(GAME_RECTANGLE.Right - 150, GAME_RECTANGLE.Y - 50, 150, 50);
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
            public void setType(BLOCK_TYPES _Type)
            {
                Type = _Type;
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
                            ShowDI = false;
                        else
                            ShowDI = true;
                        consoleLog = "Debug output is " + (ShowDI ? "enabled." : "disabled");
                        break;
                    case "QUIT":
                    case "EXIT":
                        Application.Exit();
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
                if (lEndPoint[CurrentLevel].Contains(next) && !ChangingLevel)
                {
                    ChangingLevel = true;
                    LevelPassageTime = (Time - StartupTime) / 100f;
                    GameState = GAME_STATES.PAUSED;
                }
                if (!GAME_RECTANGLE.Contains(next) || SolidBlocks.IsVisible(next))
                    Ends = true;
                else
                {
                    Ends = false;
                    if (EmptySpace.IsVisible(next))
                        goto Mark;
                }
                End = next;
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
            GameRegion = new Region(GAME_RECTANGLE),
            SolidBlocks = new Region();
        static string 
            QuartzFont = "Quartz MS";
        static Boolean[]
            ButtonsHover = new Boolean[3] { false, false, false };
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

        static int[, ,] Map = new int[LEVELS, GAME_HEIGHT, GAME_WIDTH]
        {
            {
                { 1, 0, 2, 0, 0, 1, 0, 0, 1, 0, 0, 1},
                { 0, 0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0},
                { 0, 0, 0, 0, 1, 0, 0, 1, 0, 0, 0, 0},
                { 1, 0, 0, 1, 0, 1, 1, 0, 0, 0, 1, 0},
                { 0, 0, 0, 5, 0, 0, 0, 0, 0, 1, 0, 1},
                { 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 1, 1}
            },
            {
                { 0, 0, 2, 0, 0, 1, 0, 0, 1, 0, 0, 0},
                { 1, 0, 0, 1, 0, 2, 0, 0, 0, 0, 0, 1},
                { 0, 0, 0, 0, 1, 0, 2, 1, 0, 0, 0, 0},
                { 0, 0, 0, 1, 0, 1, 1, 2, 0, 0, 5, 5},
                { 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0},
                { 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 1, 0}
            }
        };
        static int[,] EditorMap = new int[GAME_HEIGHT, GAME_WIDTH];
        Block ContextBlock;
        static Point
            ContextPosition = new Point(),
            EditorStartPoint = new Point(),
            EditorDirection = new Point();
        static Rectangle
            EditorEndPoint = new Rectangle();
        static CONTEXT_MENUS EditorContextMenu;
        #endregion



        public Form1()
        {
            InitializeComponent();
            GameState = GAME_STATES.ACTIVE;
            TextFormatCenterHor.Alignment = StringAlignment.Center;
            TextFormatCenterAll.LineAlignment = TextFormatCenterAll.Alignment = StringAlignment.Center;
            RayPen.EndCap = RayPen.StartCap = LineCap.Triangle;
            BGColor = new BackGroundColor(240, 253, 253);
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
            GameRegion = new Region(GAME_RECTANGLE);
            Lasers.Clear();
            Blocks.Clear();
            for (int q = 0; q < GAME_HEIGHT; ++q)
                for (int w = 0; w < GAME_WIDTH; ++w)
                    switch ((GameState != GAME_STATES.EDITOR ? Map[CurrentLevel, q, w] : EditorMap[q, w]))
                    {
                        case (int)BLOCK_TYPES.NORMAL:
                            Blocks.Add(new Block(new Rectangle(GAME_RECTANGLE.X + 100 * w, GAME_RECTANGLE.Y + 100 * q, 100, 100)));
                            break;
                        case (int)BLOCK_TYPES.SOLID:
                            Blocks.Add(new Block(new Rectangle(GAME_RECTANGLE.X + 100 * w, GAME_RECTANGLE.Y + 100 * q, 100, 100), BLOCK_TYPES.SOLID));
                            break;
                        case (int)BLOCK_TYPES.NOBLOCK:
                            Blocks.Add(new Block(new Rectangle(GAME_RECTANGLE.X + 100 * w, GAME_RECTANGLE.Y + 100 * q, 100, 100), BLOCK_TYPES.NOBLOCK));
                            break;
                    }
            foreach (Block TB in Blocks)
                if (TB.getType() == BLOCK_TYPES.NOBLOCK)
                    GameRegion.Exclude(TB.getRectangle());
            Lasers.Add(new Laser(lStartPoint[CurrentLevel], lDirection[CurrentLevel]));
            Lasers[0].Ends = false;
        }

        void pKeyUp(object sender, KeyEventArgs e)
        {
            if (GameState == GAME_STATES.EDITOR)
            {
                if (e.KeyData == Keys.L)
                    GameState = GAME_STATES.ACTIVE;
            }
            if (GameState == GAME_STATES.MENU)
                switch(e.KeyData)
                {
                    case Keys.C:
                        GameState = GAME_STATES.ACTIVE;
                        break;
                    case Keys.E:
                        GoToEditorState();
                        break;
                    case Keys.Q:
                        Application.Exit();
                        break;
                }
            else
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

        static void GoToEditorState()
        {
            GameState = GAME_STATES.EDITOR;
            EditorDirection = lDirection[CurrentLevel];
            EditorStartPoint = lStartPoint[CurrentLevel];
            EditorEndPoint = lEndPoint[CurrentLevel];
            for (int q = 0; q < GAME_HEIGHT; ++q)
                for (int w = 0; w < GAME_WIDTH; ++w)
                    if (Map[CurrentLevel, q, w] != 5)
                        EditorMap[q, w] = Map[CurrentLevel, q, w];
                    else
                        EditorMap[q, w] = -5;
            Setup();
        }

        void pKeyDown(object sender, KeyEventArgs e)
        {
            if (GameState != GAME_STATES.EDITOR)
            {
                if (e.KeyData == Keys.Enter)
                {
                    if (ChangingLevel)
                        NextLevelTransition();
                    if (GameState == GAME_STATES.MENU)
                        GameState = GAME_STATES.ACTIVE;
                }
                switch (e.KeyData)
                {
                    case Keys.Escape:
                        if (GameState != GAME_STATES.MENU)
                        {
                            Console.Enabled = false;
                            GameState = GAME_STATES.MENU;
                        }
                        else
                            GameState = GAME_STATES.ACTIVE;
                        break;
                }
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
        }

        static void NextLevelTransition()
        {
            if (ChangingLevel)
                if (CurrentLevel < LEVELS - 1)
                {
                    StartupTime = DateTime.Now.Ticks / 100000;
                    CurrentLevel++;
                    Setup();
                    ChangingLevel = false;
                    GameState = GAME_STATES.ACTIVE;
                }
                else
                    Application.Exit();
        }

        void pMouseDown(object sender, MouseEventArgs e)
        {
            if (GameState == GAME_STATES.EDITOR)
                if (e.Button == MouseButtons.Right && GAME_RECTANGLE.Contains(e.Location))
                {
                    Boolean BlockHited = false;
                    foreach (Block TB in Blocks)
                        if (TB.getRectangle().Contains(e.Location))
                        {
                            if (TB.getType() != BLOCK_TYPES.NOBLOCK)
                                EditorContextMenu = CONTEXT_MENUS.REMOVE;
                            else
                                EditorContextMenu = CONTEXT_MENUS.CREATEorFILL;
                            ContextBlock = TB;
                            BlockHited = true;
                            break;
                        }
                    if (!BlockHited)
                        if (EmptySpace.IsVisible(e.Location))
                            EditorContextMenu = CONTEXT_MENUS.CREATEorSETEMPTY;
                    if (EditorContextMenu != CONTEXT_MENUS.NONE)
                        ContextPosition = e.Location;
                }
                else
                {
                    switch (EditorContextMenu)
                    {
                        case CONTEXT_MENUS.REMOVE:
                            if (new Rectangle(ContextPosition, new Size(60, 15)).Contains(e.Location))
                            {
                                EmptySpace.Union(ContextBlock.getRectangle());
                                Blocks.Remove(ContextBlock);
                            }
                            EditorContextMenu = CONTEXT_MENUS.NONE;
                            break;
                        case CONTEXT_MENUS.CREATEorFILL:
                            if (new Rectangle(ContextPosition, new Size(60, 15)).Contains(e.Location))
                            {
                                EditorContextMenu = CONTEXT_MENUS.BLOCKTYPES;
                            }
                            if (new Rectangle(ContextPosition.X, ContextPosition.Y + 15, 60, 15).Contains(e.Location))
                            {
                                Blocks.Remove(ContextBlock);
                                EmptySpace.Union(ContextBlock.getRectangle());
                                EditorContextMenu = CONTEXT_MENUS.NONE;
                            }
                            break;
                        case CONTEXT_MENUS.CREATEorSETEMPTY:
                            if (new Rectangle(ContextPosition, new Size(60, 15)).Contains(e.Location))
                            {
                                EditorContextMenu = CONTEXT_MENUS.BLOCKTYPES;
                            }
                            if (new Rectangle(ContextPosition.X, ContextPosition.Y + 15, 60, 15).Contains(e.Location))
                            {
                                Blocks.Add(new Block(ContextBlock.getRectangle(), BLOCK_TYPES.NOBLOCK));
                                GameRegion.Exclude(ContextBlock.getRectangle());
                                EditorContextMenu = CONTEXT_MENUS.NONE;
                            }
                            break;
                        case CONTEXT_MENUS.BLOCKTYPES:

                            EditorContextMenu = CONTEXT_MENUS.NONE;
                            break;
                    }
                    if (EditorContextMenu != CONTEXT_MENUS.BLOCKTYPES)
                        EditorContextMenu = CONTEXT_MENUS.NONE;
                }
            StartMoveBlocks(e);
        }

        static void StartMoveBlocks(MouseEventArgs e)
        {
            if (GameState == GAME_STATES.ACTIVE || GameState == GAME_STATES.EDITOR)
                if (e.Button == MouseButtons.Left)
                    foreach (Block TB in Blocks)
                        if ((TB.getType() == BLOCK_TYPES.NORMAL || GameState == GAME_STATES.EDITOR) && TB.getRectangle().Contains(e.Location) && TB.getType() != BLOCK_TYPES.NOBLOCK)
                        {
                            SelectedBlock = Blocks.IndexOf(TB);
                            MoveStartPosition = new Point(TB.getRectangle().X, TB.getRectangle().Y);
                        }
        }

        void pMouseUp(object sender, MouseEventArgs e)
        {
            if (ChangingLevel && YES_RECTANGLE.Contains(e.Location))
                NextLevelTransition();
            if (GameState == GAME_STATES.MENU)
            {
                if (EXIT_BUTTON_RECTANGLE.Contains(e.Location))
                    Application.Exit();
                else
                    if (CONTINUE_BUTTON_RECTANGLE.Contains(e.Location))
                        GameState = GAME_STATES.ACTIVE;
                    else
                        if (EDITOR_BUTTON_RECTANGLE.Contains(e.Location))
                            GoToEditorState();
            }
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
        }

        void pMouseMove(object sender, MouseEventArgs e)
        {
            switch (GameState)
            {
                case GAME_STATES.MENU:
                    ButtonsHover[0] = CONTINUE_BUTTON_RECTANGLE.Contains(e.Location);
                    ButtonsHover[1] = EDITOR_BUTTON_RECTANGLE.Contains(e.Location);
                    ButtonsHover[2] = EXIT_BUTTON_RECTANGLE.Contains(e.Location);
                    break;
                case GAME_STATES.EDITOR:
                    goto case GAME_STATES.ACTIVE;
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
                            if (TB.getType() != BLOCK_TYPES.NOBLOCK)
                                EmptySpace.Exclude(TB.getRectangle());
                            if (TB.getType() == BLOCK_TYPES.SOLID)
                                SolidBlocks.Union(TB.getRectangle());
                        }
                    }
                    Lasers.RemoveRange(1, Lasers.Count - 1);
                    foreach (Laser TL in Lasers)
                        TL.Refresh();
                    if (Lasers.Count > 0)
                        for (int q = 0; q < Lasers.Count; ++q)
                        {
                            if (!Lasers[q].Ends && Lasers.Count < MAX_RAYS)
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
            g.SmoothingMode = SmoothingMode.AntiAlias;
            for (int q = 0; q < GAME_HEIGHT; ++q)
                for (int w = 0; w < GAME_WIDTH; ++w)
                    g.DrawImage((GameState != GAME_STATES.EDITOR ? Map[CurrentLevel, q, w] : EditorMap[q, w]) != 5 || EditorMap[q, w] != -5 ? iPlaceForBlock : iEmptySpace, GAME_RECTANGLE.X + 100 * w, GAME_RECTANGLE.Y + 100 * q);
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
            if (GameState != GAME_STATES.EDITOR)
            {
                foreach (Laser TL in Lasers)
                    g.DrawLine(RayPen, TL.Start, TL.End);
                if (SelectedBlock > -1)
                    g.DrawImage(Blocks[SelectedBlock].getType() == BLOCK_TYPES.NORMAL ? iBlockNormal : iBlockSolid, Blocks[SelectedBlock].getRectangle());
            }
            else
            {
                g.FillEllipse(Brushes.Red, lStartPoint[CurrentLevel].X - 10, lStartPoint[CurrentLevel].Y - 10, 20, 20);
                g.DrawImage(iGreenButton, RETURNFROMEDIT_BUTTON_RECTANGLE);
                g.DrawString("Back to game", Rockwell16, Brushes.Black, (RectangleF)RETURNFROMEDIT_BUTTON_RECTANGLE, TextFormatCenterAll);
            }
            g.DrawImage(ChangingLevel ? iEndLaser : iEnd, lEndPoint[CurrentLevel]);
            if (GameState == GAME_STATES.EDITOR && EditorContextMenu != CONTEXT_MENUS.NONE)
            {
                switch (EditorContextMenu)
                {
                    case CONTEXT_MENUS.REMOVE:
                        Rectangle Rect = new Rectangle(ContextPosition.X, ContextPosition.Y, 60, 15);
                        g.FillRectangle(new SolidBrush(Color.FromArgb(64, 64, 64)), Rect);
                        g.DrawRectangle(Pens.DarkGray, Rect);
                        g.DrawString("Remove", new Font("Tahoma", 9), Brushes.White, (RectangleF)Rect, TextFormatCenterAll);
                        break;
                    case CONTEXT_MENUS.CREATEorFILL:
                        Rect = new Rectangle(ContextPosition.X, ContextPosition.Y, 60, 15);
                        g.FillRectangle(new SolidBrush(Color.FromArgb(64, 64, 64)), Rect);
                        g.DrawRectangle(Pens.DarkGray, Rect);
                        g.DrawString("Create", new Font("Tahoma", 9), Brushes.White, (RectangleF)Rect, TextFormatCenterAll);
                        Rect = new Rectangle(ContextPosition.X, ContextPosition.Y + 15, 60, 15);
                        g.FillRectangle(new SolidBrush(Color.FromArgb(64, 64, 64)), Rect);
                        g.DrawRectangle(Pens.DarkGray, Rect);
                        g.DrawString("Fill", new Font("Tahoma", 9), Brushes.White, (RectangleF)Rect, TextFormatCenterAll);
                        break;
                    case CONTEXT_MENUS.CREATEorSETEMPTY:
                        Rect = new Rectangle(ContextPosition.X, ContextPosition.Y, 60, 15);
                        g.FillRectangle(new SolidBrush(Color.FromArgb(64, 64, 64)), Rect);
                        g.DrawRectangle(Pens.DarkGray, Rect);
                        g.DrawString("Create", new Font("Tahoma", 9), Brushes.White, (RectangleF)Rect, TextFormatCenterAll);
                        Rect = new Rectangle(ContextPosition.X, ContextPosition.Y + 15, 60, 15);
                        g.FillRectangle(new SolidBrush(Color.FromArgb(64, 64, 64)), Rect);
                        g.DrawRectangle(Pens.DarkGray, Rect);
                        g.DrawString("Set empty", new Font("Tahoma", 9), Brushes.White, (RectangleF)Rect, TextFormatCenterAll);
                        break;
                    case CONTEXT_MENUS.BLOCKTYPES:
                        Rect = new Rectangle(ContextPosition.X, ContextPosition.Y, 60, 15);
                        g.FillRectangle(new SolidBrush(Color.FromArgb(64, 64, 64)), Rect);
                        g.DrawRectangle(Pens.DarkGray, Rect);
                        g.DrawString("Normal", new Font("Tahoma", 9), Brushes.White, (RectangleF)Rect, TextFormatCenterAll);
                        Rect = new Rectangle(ContextPosition.X, ContextPosition.Y + 15, 60, 15);
                        g.FillRectangle(new SolidBrush(Color.FromArgb(64, 64, 64)), Rect);
                        g.DrawRectangle(Pens.DarkGray, Rect);
                        g.DrawString("Solid", new Font("Tahoma", 9), Brushes.White, (RectangleF)Rect, TextFormatCenterAll);
                        break;
                }
            }
            if (ChangingLevel)
            {
                g.DrawImage(iGlassPanelCorners, NEXT_LEVEL_RECTANGLE);
                g.DrawImage(iGlassPanelCornersFill, YES_RECTANGLE);
                g.DrawString("Congratulations, you pass a " + LevelNames[CurrentLevel] + " level over " + LevelPassageTime + " sec.", new Font("Kristen ITC", 13), Brushes.Black,
                    new Rectangle(NEXT_LEVEL_RECTANGLE.X + 5, NEXT_LEVEL_RECTANGLE.Y + 5, NEXT_LEVEL_RECTANGLE.Width - 10, NEXT_LEVEL_RECTANGLE.Height - 10), TextFormatCenterHor);
                g.DrawString(CurrentLevel < LEVELS - 1? "Next level" : "Exit", new Font("Kristen ITC", 15), Brushes.Black, 
                    new Rectangle(YES_RECTANGLE.X + 3, YES_RECTANGLE.Y + 2, YES_RECTANGLE.Width - 6, YES_RECTANGLE.Height - 5), TextFormatCenterHor);
            }
            if (GameState == GAME_STATES.MENU)
            {
                g.FillRectangle(new SolidBrush(Color.FromArgb(192, 0, 0, 0)), 0, 0, Resolution.Width, Resolution.Height);
                g.DrawImage(iMenuPanel, MENU_RECTANGLE);
                g.DrawImage(ButtonsHover[0] ? iGreenButton : iTransGreenButton, CONTINUE_BUTTON_RECTANGLE);
                g.DrawImage(ButtonsHover[1] ? iBlueButton : iTransBlueButton, EDITOR_BUTTON_RECTANGLE);
                g.DrawImage(ButtonsHover[2] ? iBlueButton : iTransBlueButton, EXIT_BUTTON_RECTANGLE);
                g.DrawString("Continue", Rockwell18, !ButtonsHover[0] ? GreenButtonBrush : Brushes.White, (RectangleF)CONTINUE_BUTTON_RECTANGLE, TextFormatCenterAll);
                //g.DrawString("C               ", Rockwell18, Brushes.Orange, (RectangleF)CONTINUE_BUTTON_RECTANGLE, TextFormatCenterAll);
                g.DrawString("Editor", Rockwell18, !ButtonsHover[1] ? BlueButtonBrush : Brushes.White, (RectangleF)EDITOR_BUTTON_RECTANGLE, TextFormatCenterAll);
                //g.DrawString("E          ", Rockwell18, Brushes.Orange, (RectangleF)(EDITOR_BUTTON_RECTANGLE), TextFormatCenterAll);
                g.DrawString("Quit", Rockwell16, !ButtonsHover[2] ? BlueButtonBrush : Brushes.White, (RectangleF)EXIT_BUTTON_RECTANGLE, TextFormatCenterAll);
                //g.DrawString("Q     ", Rockwell16, Brushes.Orange, (RectangleF)EXIT_BUTTON_RECTANGLE, TextFormatCenterAll);
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
                g.DrawString(Console.getPrevString(), Verdana13, Brushes.Black, new Rectangle(100, 0, 423, 20), TextFormatCenterHor);
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
