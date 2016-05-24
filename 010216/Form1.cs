using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Drawing2D;

namespace _010216
{
    public partial class Form1 : Form
    {
        const int
            MAX_RAYS = 1000,
            GAME_HEIGHT = 6,
            GAME_WIDTH = 12;
        static readonly Size Resolution = Screen.PrimaryScreen.Bounds.Size;
        static readonly Rectangle
            GAME_RECTANGLE = new Rectangle((Resolution.Width - GAME_WIDTH * 100) / 2, (Resolution.Height - GAME_HEIGHT * 100) / 2, GAME_WIDTH * 100, GAME_HEIGHT * 100);
        static readonly HatchBrush blockBrush = new HatchBrush(HatchStyle.Trellis, Color.DarkSlateBlue);
        static Timer updateTimer = new Timer();
        List<Rectangle> RectList = new List<Rectangle>();
        int SelectedRectangle = -1;

        int[,] map = new int[GAME_HEIGHT, GAME_WIDTH]
        {
            { 1, 1, 0, 0, 0, 1, 0, 0, 1, 0, 0, 1},
            { 1, 0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0},
            { 0, 0, 1, 0, 1, 0, 0, 1, 0, 0, 0, 0},
            { 1, 0, 0, 0, 0, 1, 1, 0, 0, 0, 1, 0},
            { 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0},
            { 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1},
        };

        public Form1()
        {
            InitializeComponent();

            for (int q = 0; q < GAME_HEIGHT; ++q)
                for (int w = 0; w < GAME_WIDTH; ++w)
                    switch (map[q, w])
                    {
                        case 1:
                            RectList.Add(new Rectangle(GAME_RECTANGLE.X + 100 * w, GAME_RECTANGLE.Y + 100 * q, 100, 100));
                            break;
                    }

            this.Size = Resolution;
            this.Paint += new PaintEventHandler(pDraw);
            this.KeyDown += new KeyEventHandler(pKeyDown);
            //this.KeyUp += new KeyEventHandler(Program_KeyUp);
            this.MouseMove += new MouseEventHandler(pMouseMove);
            this.MouseUp += new MouseEventHandler(pMouseUp);
            this.MouseDown += new MouseEventHandler(pMouseDown);
            updateTimer.Interval = 1;
            updateTimer.Tick += new EventHandler(pUpdate);
            updateTimer.Start(); InitializeComponent();
        }

        void pKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyData)
            {
                case Keys.Escape:
                    Application.Exit();
                    break;
            }
        }

        void pMouseDown(object sender, MouseEventArgs e)
        {
            for (int q = 0; q < RectList.Count; ++q)
                if (RectList[q].Contains(e.Location))
                    SelectedRectangle = q;
        }

        void pMouseUp(object sender, MouseEventArgs e)
        {
            SelectedRectangle = -1;
        }

        void pMouseMove(object sender, MouseEventArgs e)
        {
            if (SelectedRectangle > 0)
            {
                RectList[SelectedRectangle].Offset(RectList[SelectedRectangle].X - e.X, RectList[SelectedRectangle].Y - e.Y);
            }
        }

        void pUpdate(object sender, EventArgs e)
        {
            Invalidate();
        }

        void pDraw(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            //if (Resolution.Width == 1920 && Resolution.Height == 1080)
            //{
            //    g.ScaleTransform(1.4f, 1.4f);
            //    g.TranslateTransform(-275, -155);
            //}
            g.FillRectangle(Brushes.SlateGray, GAME_RECTANGLE);
            foreach (Rectangle TR in RectList)
            {
                g.FillRectangle(blockBrush, TR);
                g.DrawRectangle(Pens.Aquamarine, TR.X, TR.Y, 100, 100);
            }
            g.ResetTransform();
            g.DrawString(SelectedRectangle.ToString(), new Font("Verdana", 13), Brushes.Black, 0, 0);
        }
    }
}
