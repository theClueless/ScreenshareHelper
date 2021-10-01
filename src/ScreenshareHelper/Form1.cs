﻿using ScreenshareHelper.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ScreenshareHelper
{
    public partial class Form1 : Form
    {
        readonly Color transKey = Color.SaddleBrown;
        private bool isActive = true;

        public Form1()
        {
            InitializeComponent();
            FormBorderStyle = FormBorderStyle.None;
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            this.TransparencyKey = transKey;
            RestoreWindowPosition();

            this.MouseDown += Form1_MouseDown;
        }

        #region Drag/Move the form
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        private void Form1_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                Cursor.Current = Cursors.Cross;
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                if(isActive)
                    cp.Style |= 0x40000; //WS_SIZEBOX;  
                else
                    cp.Style &= ~0x40000; //WS_SIZEBOX;  
                return cp;
            }
        }
        #endregion
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            if (isActive)
                e.Graphics.Clear(transKey);
            else
                paint(e.Graphics);
        }

        #region Cursor
        [StructLayout(LayoutKind.Sequential)]
        struct CURSORINFO
        {
            public Int32 cbSize;
            public Int32 flags;
            public IntPtr hCursor;
            public POINTAPI ptScreenPos;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct POINTAPI
        {
            public int x;
            public int y;
        }

        [DllImport("user32.dll")]
        static extern bool GetCursorInfo(out CURSORINFO pci);

        [DllImport("user32.dll")]
        static extern bool DrawIcon(IntPtr hDC, int X, int Y, IntPtr hIcon);

        const Int32 CURSOR_SHOWING = 0x00000001;
        #endregion Cursor
        private void paint(Graphics graphics, bool withMouse = true)
        {
            //graphics.Clear(transKey);
            //graphics.FillRectangle(new SolidBrush(transKey), 0, 0, Settings.Default.CaptureSize.Width, Settings.Default.CaptureSize.Height);
            graphics.CopyFromScreen(Settings.Default.CaptureLocation.X, Settings.Default.CaptureLocation.Y, 0, 0, Settings.Default.CaptureSize);
            if (withMouse)
            {
                CURSORINFO pci;
                pci.cbSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(CURSORINFO));

                if (GetCursorInfo(out pci))
                {
                    if (pci.flags == CURSOR_SHOWING)
                    {
                        bool showMouse = this.Location != Settings.Default.CaptureLocation;
                        if (showMouse)
                        {
                            const int offset = 5;
                            DrawIcon(graphics.GetHdc(),
                                pci.ptScreenPos.x - Settings.Default.CaptureLocation.X - offset,
                                pci.ptScreenPos.y - Settings.Default.CaptureLocation.Y - offset,
                                pci.hCursor);
                            graphics.ReleaseHdc();
                        }
                    }
                }
            }

            //cut my own area
            //graphics.FillRectangle(Brushes.Black, 0, 0, Settings.Default.CaptureSize.Width, Settings.Default.CaptureSize.Height);
        }



        #region Window Events

        private void buttonSetCaptureArea_Click(object sender, EventArgs e)
        {
            Settings.Default.CaptureLocation = this.Location;
            Settings.Default.CaptureSize = this.Size;
        }
        
        private void RestoreWindowPosition()
        {
            if (Settings.Default.HasSetDefaults)
            {
                this.Location = Settings.Default.Location;
                this.Size = Settings.Default.Size;
            }
        }
        private void SaveWindowPosition()
        {
            if (this.WindowState == FormWindowState.Normal)
            {
                Settings.Default.Location = this.Location;
                Settings.Default.Size = this.Size;
            }
            else
            {
                Settings.Default.Location = this.RestoreBounds.Location;
                Settings.Default.Size = this.RestoreBounds.Size;
            }

            Settings.Default.HasSetDefaults = true;
            Settings.Default.Save();
        }

        private void Form1_Activated(object sender, EventArgs e)
        {
            isActive = true;
            FormBorderStyle = FormBorderStyle.None;//update CreateParams
            buttonSetCaptureArea.Visible = buttonCloseApp.Visible = isActive;
        }
        private void Form1_Deactivate(object sender, EventArgs e)
        {
            isActive = false;
            FormBorderStyle = FormBorderStyle.None; //update CreateParams
            this.Size = Settings.Default.CaptureSize;
            
            buttonSetCaptureArea.Visible = buttonCloseApp.Visible = isActive;
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveWindowPosition();
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            Thread t = new Thread(() =>
            {
                while (true)
                {
                    if (!isActive)
                        Invalidate();
                    Thread.Sleep(10);
                }
            });
            t.IsBackground = true;
            t.Start();
        }

        private void buttonCloseApp_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        #endregion
    }
}
