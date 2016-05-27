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
            MAX_RAYS = 100,
            GAME_HEIGHT = 6,
            GAME_WIDTH = 12,
            COLOR_MIN_VALUE = 63,
            COLOR_MAX_VALUE = 192;
        enum BLOCK_TYPE
        {
            NORMAL = 1,
            STATIC = 2,
            GLASS = 3,
            PORTAL = 4
        }
        static Random
            getRandom = new Random(DateTime.Now.Millisecond);
        static readonly Font
            Verdana13 = new Font("Verdana", 13);
        static readonly StringFormat
            TextFormatCenter = new StringFormat();
        static readonly Image
            iConsole = Properties.Resources.glassPanelConsole;
        static readonly Size 
            Resolution = Screen.PrimaryScreen.Bounds.Size;
        static readonly Rectangle
            GAME_RECTANGLE = new Rectangle((Resolution.Width - GAME_WIDTH * 100) / 2, (Resolution.Height - GAME_HEIGHT * 100) / 2, GAME_WIDTH * 100, GAME_HEIGHT * 100);
        static readonly HatchBrush 
            blockBrush = new HatchBrush(HatchStyle.Trellis, Color.DarkSlateBlue, Color.Black),
            blockBrushTransparent = new HatchBrush(HatchStyle.Trellis, Color.FromArgb(128, 72, 61, 139), Color.FromArgb(128, 0, 0, 0));
        static Timer updateTimer = new Timer();
        #endregion

        class ConsolePrototype
        {
            public Boolean Enabled;

            static private string consoleString;
            static private string consolePrevString;
            static private string consoleLog;
            static private Rectangle CONSOLE_REGION;

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
                if (consoleLog == "Unknown command.")
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
            if (!GAME_RECTANGLE.Contains(next))
                Ends = true;
            else
            {
                Ends = false;
                if (NOBLOCK.IsVisible(next))
                    goto Mark;
            }
            End = next;
            }
        }

        class BackGroundColor
        {
            public int R;
            public int G;
            public int B;
            public int RFactor = getRandom.Next(-1, 2);
            public int GFactor = getRandom.Next(-1, 2);
            public int BFactor = getRandom.Next(-1, 2);
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
            NOBLOCK = new Region(GAME_RECTANGLE);
        static string 
            QuartzFont = "Quartz MS";
        static Boolean
            ShowDI = false;
        static List<Rectangle>
            RectList = new List<Rectangle>();
        static List<Laser> 
            Lasers = new List<Laser>();
        static BackGroundColor
            BGColor;
        ConsolePrototype
            Console = new ConsolePrototype();
        int
            lastTick, lastFrameRate, frameRate,
            SelectedRectangle = -1;
        Point
            MoveStartPosition = new Point();

        int[,] map = new int[GAME_HEIGHT, GAME_WIDTH]
        {
            { 1, 1, 0, 0, 0, 1, 0, 0, 1, 0, 0, 1},
            { 1, 0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 1},
            { 0, 0, 0, 0, 1, 0, 0, 1, 0, 0, 0, 1},
            { 1, 0, 0, 1, 0, 1, 1, 0, 0, 0, 1, 1},
            { 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 1},
            { 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 1, 1},
        };
        #endregion

        public Form1()
        {
            InitializeComponent();
            TextFormatCenter.Alignment = StringAlignment.Center;

            for (int q = 0; q < GAME_HEIGHT; ++q)
                for (int w = 0; w < GAME_WIDTH; ++w)
                    switch (map[q, w])
                    {
                        case (int)BLOCK_TYPE.NORMAL:
                            RectList.Add(new Rectangle(GAME_RECTANGLE.X + 100 * w, GAME_RECTANGLE.Y + 100 * q, 100, 100));
                            break;
                    }
            BGColor = new BackGroundColor(240, 254, 254);
            Lasers.Add(new Laser(GAME_RECTANGLE.X + 100, GAME_RECTANGLE.Y + 150, 1, 1));
            Lasers[0].Ends = false;
            foreach (FontFamily Family in FontFamily.Families)
                if (Family.Name.ToUpper() == "QUARTZ" || Family.Name.ToUpper() == "QUARTZ MS")
                    QuartzFont = Family.Name;
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

        void pKeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.Tab)
                if (!Console.Enabled)
                    Console.Enabled = true;
                else
                {
                    Console.Enabled = false;
                    if (!String.IsNullOrEmpty(Console.getString()))
                    {
                        string temp = Console.getString();
                        temp = temp.Remove(0);
                        Console.setString(temp);
                    }
                }
        }

        void pKeyDown(object sender, KeyEventArgs e)
        {
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
                {
                    string temp = Console.getString();
                    temp += (char)e.KeyValue;
                    Console.setString(temp);
                }
                switch (e.KeyData)
                {
                    case Keys.Back:
                        if (Console.getLength() > 0)
                        {
                            string temp = Console.getString();
                            int tempint = Console.getLength() - 1;
                            temp = temp.Substring(0, tempint);
                            Console.setString(temp);
                        }
                        break;
                    case Keys.Enter:
                        if (!String.IsNullOrEmpty(Console.getString()))
                            Console.applyCommand();
                        break;
                }
            }
            #endregion
        }

        void pMouseDown(object sender, MouseEventArgs e)
        {
            for (int q = 0; q < RectList.Count; ++q)
                if (RectList[q].Contains(e.Location))
                {
                    SelectedRectangle = q;
                    MoveStartPosition = new Point(RectList[q].X, RectList[q].Y);
                }
        }

        void pMouseUp(object sender, MouseEventArgs e)
        {
            if (SelectedRectangle > -1)
                if (!GAME_RECTANGLE.Contains(e.Location))
                    RectList[SelectedRectangle] = new Rectangle(MoveStartPosition.X, MoveStartPosition.Y, 100, 100);
                else
                    for (int q = 0; q < RectList.Count; ++q)
                    {
                        Point TP = new Point(GAME_RECTANGLE.X + ((e.X - GAME_RECTANGLE.X) / 100) * 100, GAME_RECTANGLE.Y + ((e.Y - GAME_RECTANGLE.Y) / 100) * 100);
                        if (RectList[q].Contains(TP.X + 5, TP.Y + 5) && q != SelectedRectangle)
                        {
                            RectList[SelectedRectangle] = new Rectangle(MoveStartPosition.X, MoveStartPosition.Y, 100, 100);
                            break;
                        }
                        else
                            RectList[SelectedRectangle] = new Rectangle(TP.X, TP.Y, 100, 100);
                    }
            SelectedRectangle = -1;
        }

        void pMouseMove(object sender, MouseEventArgs e)
        {
            if (SelectedRectangle > -1)
            {
                RectList[SelectedRectangle] = new Rectangle(e.X - 50, e.Y - 50, 100, 100);
            }
        }

        void pUpdate(object sender, EventArgs e)
        {
            BGColor.Increase(true);
            this.BackColor = BGColor.Set();
            if (getRandom.Next(50) == 0)
                BGColor.randomFactors();
            NOBLOCK = new Region(GAME_RECTANGLE);
            foreach (Rectangle TR in RectList)
                NOBLOCK.Exclude(TR);
            Lasers.RemoveRange(1, Lasers.Count - 1);
            foreach (Laser TL in Lasers)
                TL.Refresh();
            if (Lasers.Count < MAX_RAYS)
                for (int q = 0; q < MAX_RAYS; ++q)
                {
                    if (!Lasers[q].Ends)
                        Lasers.Add(new Laser(Lasers[q].End, Direct(Lasers[q].Direction)));
                    else
                        break;
                }
            Invalidate();
        }

        void pDraw(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.FillRectangle(Brushes.SlateGray, GAME_RECTANGLE);
            for (int q = 0; q < RectList.Count; ++q)
            {
                g.FillRectangle(q != SelectedRectangle ? blockBrush : blockBrushTransparent, RectList[q]);
                if (q != SelectedRectangle) g.DrawRectangle(new Pen(Color.FromArgb(82, 96, 152)), RectList[q].X, RectList[q].Y, 100, 100);
            }
            Pen RayPen = new Pen(Color.Red, 6);
            RayPen.EndCap = RayPen.StartCap = LineCap.Flat;
            for (int q = 0; q < Lasers.Count; ++q)
            {
                g.DrawLine(RayPen, Lasers[q].Start, Lasers[q].End);
                //g.DrawString(q.ToString(), Verdana13, Brushes.Black, Lasers[q].Start);
            }
            if (ShowDI)
            #region Debug Information
            {
                string output = "FPS: " + CalculateFrameRate().ToString() + "\nActual Laser Count: " + Lasers.Count + "\n";
                //for (int q = 0; q < Lasers.Count; ++q )
                //    output += Lasers[q].Start.ToString() + Lasers[q].End.ToString() + Lasers[q].Direction.ToString() + Lasers[q].Ends.ToString() + "\n";
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
