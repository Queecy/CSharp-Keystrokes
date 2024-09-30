using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Windows.Forms;

namespace KeyStrokes
{
    public partial class Main : Form
    {
        private List<Particle> particles = new List<Particle>();
        private Random random = new Random();
        private System.Windows.Forms.Timer timerParticles = new System.Windows.Forms.Timer();
        private Point previousFormLocation;
        private bool isFirstUpdate = true;

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern IntPtr LoadCursorFromFile(string lpFileName);

        private const int WH_KEYBOARD_LL = 13;
        private const int WH_MOUSE_LL = 14;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_LBUTTONUP = 0x0202;
        private const int WM_RBUTTONDOWN = 0x0204;
        private const int WM_RBUTTONUP = 0x0205;

        private LowLevelKeyboardProc _keyboardProc;
        private LowLevelMouseProc _mouseProc;
        private IntPtr _keyboardHookID = IntPtr.Zero;
        private IntPtr _mouseHookID = IntPtr.Zero;

        public Main()
        {
            InitializeComponent();

            DoubleBuffered = true;
            InitializeParticles();
            timerParticles.Interval = 1;
            timerParticles.Tick += timerparticleseffect_Tick;
            timerParticles.Start();
            DoubleBuffered = true;

            // .cur .ani
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string cursorFilePath = Path.Combine(appDataPath, "Cursor.ani");
            string cursorUrl = "https://raw.githubusercontent.com/MrQueecy/CSharp-Keystrokes/main/Cursor.ani";
            if (!File.Exists(cursorFilePath))
            {
                try
                {
                    using (WebClient webClient = new WebClient())
                    {
                        webClient.DownloadFile(cursorUrl, cursorFilePath);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error:" + ex.Message);
                    return;
                }
            }

            IntPtr hCursor = LoadCursorFromFile(cursorFilePath);
            if (hCursor == IntPtr.Zero)
            {
                MessageBox.Show("Error loading cursor");
            }
            else
            {
                this.Cursor = new Cursor(hCursor);
            }

            _keyboardProc = KeyboardHookCallback;

            _keyboardHookID = SetHook(_keyboardProc, WH_KEYBOARD_LL);
            _mouseHookID = SetHook(_mouseProc, WH_MOUSE_LL);

            this.KeyPreview = true;
            this.FormClosing += Form1_FormClosing;
        }

        private void Main_Load(object sender, EventArgs e)
        {

        }

        private void DeveloperMode_CheckedChanged(object sender, EventArgs e)
        {
            if (DeveloperMode.Checked)
            {
                this.Size = new Size(1005, 415);
            }
            else
            {
                this.Size = new Size(1005, 355);
            }
        }

        private void BlurEffect_CheckedChanged(object sender, EventArgs e)
        {
            if (BlurEffect.Checked)
            {
                EnableBlur();
            }
            else 
            {
                DisableBlur();
            }
        }

        private bool particlesEnabled = false; 
        private void ParticlesEffect_CheckedChanged(object sender, EventArgs e)
        {
            if (ParticlesEffect.Checked)
            {
                particlesEnabled = true; 
                InitializeParticles(); 
                this.Invalidate(); 
            }
            else
            {
                particlesEnabled = false; 
                particles.Clear(); 
                this.Invalidate(); 
            }
        }

        private void ForceTop_CheckedChanged(object sender, EventArgs e)
        {
            if (ForceTop.Checked)
            {
                this.TopMost = true;
            }
            else
            {
                this.TopMost = false;
            }
        }

        private void EnableBlur()
        {
            var accent = new AccentPolicy
            {
                AccentState = AccentState.ACCENT_ENABLE_BLURBEHIND
            };

            var accentStructSize = Marshal.SizeOf(accent);

            var accentPtr = Marshal.AllocHGlobal(accentStructSize);
            Marshal.StructureToPtr(accent, accentPtr, false);

            var data = new WindowCompositionAttributeData
            {
                Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
                SizeOfData = accentStructSize,
                Data = accentPtr
            };

            SetWindowCompositionAttribute(this.Handle, ref data);

            Marshal.FreeHGlobal(accentPtr);
        }

        private void DisableBlur()
        {
            var accent = new AccentPolicy
            {
                AccentState = AccentState.ACCENT_DISABLED 
            };

            var accentStructSize = Marshal.SizeOf(accent);
            var accentPtr = Marshal.AllocHGlobal(accentStructSize);
            Marshal.StructureToPtr(accent, accentPtr, false);

            var data = new WindowCompositionAttributeData
            {
                Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
                SizeOfData = accentStructSize,
                Data = accentPtr
            };

            SetWindowCompositionAttribute(this.Handle, ref data);

            Marshal.FreeHGlobal(accentPtr);
        }

        [DllImport("user32.dll")]
        private static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

        private enum WindowCompositionAttribute
        {
            WCA_ACCENT_POLICY = 19
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WindowCompositionAttributeData
        {
            public WindowCompositionAttribute Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }

        private enum AccentState
        {
            ACCENT_DISABLED = 0,
            ACCENT_ENABLE_GRADIENT = 1,
            ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
            ACCENT_ENABLE_BLURBEHIND = 3,
            ACCENT_ENABLE_ACRYLICBLURBEHIND = 4,
            ACCENT_ENABLE_HOSTBACKDROP = 5,
            ACCENT_ENABLE_ACRYLICBACKGROUND = 6
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct AccentPolicy
        {
            public AccentState AccentState;
            public int AccentFlags;
            public int GradientColor;
            public int AnimationId;
        }

        private void timerparticleseffect_Tick(object sender, EventArgs e)
        {
            UpdateParticles();
            Invalidate();
        }

        public class Particle
        {
            public PointF Position { get; set; }
            public PointF Velocity { get; set; }
            public int Radius { get; set; }
            public Color Color { get; set; }

        }

        private void InitializeParticles()
        {
            int numParticles = 100;
            for (int i = 0; i < numParticles; i++)
            {
                double angle = random.NextDouble() * 2 * Math.PI;
                double speed = random.Next(1, 3);
                particles.Add(new Particle()
                {
                    Position = new PointF(random.Next(0, ClientSize.Width), random.Next(0, ClientSize.Height)),
                    Velocity = new PointF((float)(Math.Cos(angle) * speed), (float)(Math.Sin(angle) * speed)),
                    Radius = random.Next(2, 5),
                    Color = Color.Purple 
                });
            }
        }

        private void UpdateParticles()
        {
            if (!particlesEnabled) return;

            foreach (var particle in particles)
            {
                particle.Position = new PointF(particle.Position.X + particle.Velocity.X * 0.5f, particle.Position.Y + particle.Velocity.Y * 0.5f);
                if (particle.Position.X < 0 || particle.Position.X > ClientSize.Width)
                {
                    particle.Velocity = new PointF(-particle.Velocity.X, particle.Velocity.Y);
                    particle.Position = new PointF(particle.Position.X + particle.Velocity.X * 0.5f, particle.Position.Y);
                }
                if (particle.Position.Y < 0 || particle.Position.Y > ClientSize.Height)
                {
                    particle.Velocity = new PointF(particle.Velocity.X, -particle.Velocity.Y);
                    particle.Position = new PointF(particle.Position.X, particle.Position.Y + particle.Velocity.Y * 0.5f);
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (!particlesEnabled) return;

            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            foreach (var particle in particles)
            {
                int transparency = (int)((1.0f - (particle.Position.Y / (float)ClientSize.Height)) * 255);
                if (transparency > 255) transparency = 255;
                if (transparency < 0) transparency = 0;

                Color particleColor = Color.FromArgb(transparency, Color.Purple);

                int reducedRadius = particle.Radius / 2;
                e.Graphics.FillEllipse(new SolidBrush(particleColor),
                    particle.Position.X - reducedRadius,
                    particle.Position.Y - reducedRadius,
                    reducedRadius * 2, reducedRadius * 2);
            }

            foreach (var particle in particles)
            {
                foreach (var otherParticle in particles)
                {
                    if (particle != otherParticle)
                    {
                        float dx = particle.Position.X - otherParticle.Position.X;
                        float dy = particle.Position.Y - otherParticle.Position.Y;
                        float distance = (float)Math.Sqrt(dx * dx + dy * dy);

                        if (distance < 50)
                        {
                            int alpha = (int)((1.0f - (distance / 50.0f)) * 255.0f);
                            Color lineColor = Color.FromArgb(alpha, 128, 0, 128);
                            e.Graphics.DrawLine(new Pen(lineColor, 1),
                                particle.Position, otherParticle.Position);
                        }
                    }
                }
            }
        }

        private void AppExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void AppHide_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void GameMode_Click(object sender, EventArgs e)
        {
            // this.Hide();
            GameMode gameModeForm = new GameMode();
            gameModeForm.Show();
        }

        // KEY 
        private IntPtr SetHook(Delegate proc, int hookType)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(hookType, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                HandleKeyDown((Keys)vkCode);
            }
            else if (nCode >= 0 && wParam == (IntPtr)WM_KEYUP)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                HandleKeyUp((Keys)vkCode);
            }
            return CallNextHookEx(_keyboardHookID, nCode, wParam, lParam);
        }

        private void HandleKeyDown(Keys key)
        {
            switch (key)
            {
                case Keys.Q:
                    Qkey.BackColor = Color.FromArgb(30, 30, 30);
                    break;
                case Keys.W:
                    Wkey.BackColor = Color.FromArgb(30, 30, 30);
                    break;
                case Keys.E:
                    Ekey.BackColor = Color.FromArgb(30, 30, 30);
                    break;
                case Keys.R:
                    Rkey.BackColor = Color.FromArgb(30, 30, 30);
                    break;
                case Keys.T:
                    Tkey.BackColor = Color.FromArgb(30, 30, 30);
                    break;
                case Keys.Y:
                    Ykey.BackColor = Color.FromArgb(30, 30, 30);
                    break;
                case Keys.U:
                    Ukey.BackColor = Color.FromArgb(30, 30, 30);
                    break;
                case Keys.I:
                    Ikey.BackColor = Color.FromArgb(30, 30, 30);
                    break;
                case Keys.O:
                    Okey.BackColor = Color.FromArgb(30, 30, 30);
                    break;
                case Keys.P:
                    Pkey.BackColor = Color.FromArgb(30, 30, 30);
                    break;
                case Keys.A:
                    Akey.BackColor = Color.FromArgb(30, 30, 30);
                    break;
                case Keys.S:
                    Skey.BackColor = Color.FromArgb(30, 30, 30);
                    break;
                case Keys.D:
                    Dkey.BackColor = Color.FromArgb(30, 30, 30);
                    break;
                case Keys.F:
                    Fkey.BackColor = Color.FromArgb(30, 30, 30);
                    break;
                case Keys.G:
                    Gkey.BackColor = Color.FromArgb(30, 30, 30);
                    break;
                case Keys.H:
                    Hkey.BackColor = Color.FromArgb(30, 30, 30);
                    break;
                case Keys.J:
                    Jkey.BackColor = Color.FromArgb(30, 30, 30);
                    break;
                case Keys.K:
                    Kkey.BackColor = Color.FromArgb(30, 30, 30);
                    break;
                case Keys.L:
                    Lkey.BackColor = Color.FromArgb(30, 30, 30);
                    break;
                case Keys.Z:
                    Zkey.BackColor = Color.FromArgb(30, 30, 30);
                    break;
                case Keys.X:
                    Xkey.BackColor = Color.FromArgb(30, 30, 30);
                    break;
                case Keys.C:
                    Ckey.BackColor = Color.FromArgb(30, 30, 30);
                    break;
                case Keys.V:
                    Vkey.BackColor = Color.FromArgb(30, 30, 30);
                    break;
                case Keys.B:
                    Bkey.BackColor = Color.FromArgb(30, 30, 30);
                    break;
                case Keys.N:
                    Nkey.BackColor = Color.FromArgb(30, 30, 30);
                    break;
                case Keys.M:
                    Mkey.BackColor = Color.FromArgb(30, 30, 30);
                    break;
                case Keys.LShiftKey:
                    Shiftkey.BackColor = Color.FromArgb(30, 30, 30);
                    break;
                case Keys.RShiftKey:
                    Shiftkey0.BackColor = Color.FromArgb(30, 30, 30);
                    break;
                case Keys.LControlKey:
                    CTRLkey.BackColor = Color.FromArgb(30, 30, 30);
                    break;
                case Keys.RControlKey:
                    CTRLkey0.BackColor = Color.FromArgb(30, 30, 30);
                    break;
                case Keys.Space:
                    Spacekey.BackColor = Color.FromArgb(30, 30, 30);
                    break;
                case Keys.Enter:
                    Enterkey.BackColor = Color.FromArgb(30, 30, 30);
                    break;
                case Keys.Back:
                    Backspacekey.BackColor = Color.FromArgb(30, 30, 30);
                    break;
                case Keys.Tab:
                    Tabkey.BackColor = Color.FromArgb(30, 30, 30);
                    break;
                case Keys.Escape:
                    ESCkey.BackColor = Color.FromArgb(30, 30, 30);
                    break;
                case Keys.Up:
                    arrow0.BackColor = Color.FromArgb(30, 30, 30);
                    break;
                case Keys.Down:
                    arrow2.BackColor = Color.FromArgb(30, 30, 30);
                    break;
                case Keys.Left:
                    arrow1.BackColor = Color.FromArgb(30, 30, 30);
                    break;
                case Keys.Right:
                    arrow0.BackColor = Color.FromArgb(30, 30, 30);
                    break;
                case Keys.Delete:
                    DELkey.BackColor = Color.FromArgb(30, 30, 30);
                    break;
                case Keys.Insert:
                    INSkey.BackColor = Color.FromArgb(30, 30, 30);
                    break;
                case Keys.Home:
                    HOMEkey.BackColor = Color.FromArgb(30, 30, 30);
                    break;
                case Keys.End:
                    ENDkey.BackColor = Color.FromArgb(30, 30, 30);
                    break;
                case Keys.PageUp:
                    PageUPkey.BackColor = Color.FromArgb(30, 30, 30);
                    break;
                case Keys.PageDown:
                    PageDownkey.BackColor = Color.FromArgb(30, 30, 30);
                    break;
                case Keys.F1:
                    F1key.BackColor = Color.FromArgb(30, 30, 30);
                    break;
                case Keys.F2:
                    F2key.BackColor = Color.FromArgb(30, 30, 30);
                    break;
                case Keys.F3:
                    F3key.BackColor = Color.FromArgb(30, 30, 30);
                    break;
                case Keys.F4:
                    F4key.BackColor = Color.FromArgb(30, 30, 30);
                    break;
                case Keys.F5:
                    F5key.BackColor = Color.FromArgb(30, 30, 30);
                    break;
                case Keys.F6:
                    F6key.BackColor = Color.FromArgb(30, 30, 30);
                    break;
                case Keys.F7:
                    F7key.BackColor = Color.FromArgb(30, 30, 30);
                    break;
                case Keys.F8:
                    F8key.BackColor = Color.FromArgb(30, 30, 30);
                    break;
                case Keys.F9:
                    F9key.BackColor = Color.FromArgb(30, 30, 30);
                    break;
                case Keys.F10:
                    F10key.BackColor = Color.FromArgb(30, 30, 30);
                    break;
                case Keys.F11:
                    F11key.BackColor = Color.FromArgb(30, 30, 30);
                    break;
                case Keys.F12:
                    F12key.BackColor = Color.FromArgb(30, 30, 30);
                    break;
                case Keys.Alt:
                    ALTkey.BackColor = Color.FromArgb(30, 30, 30);
                    break;
                case Keys.CapsLock:
                    CapsLockkey.BackColor = Color.FromArgb(30, 30, 30);
                    break;
                case Keys.LWin:
                    Winkey.BackColor = Color.FromArgb(30, 30, 30);
                    break;
                case Keys.RWin:
                    Winkey0.BackColor = Color.FromArgb(30, 30, 30);
                    break;
            }
        }

        private void HandleKeyUp(Keys key)
        {
            switch (key)
            {
                case Keys.Q:
                    Qkey.BackColor = Color.FromArgb(17, 17, 17);
                    break;
                case Keys.W:
                    Wkey.BackColor = Color.FromArgb(17, 17, 17);
                    break;
                case Keys.E:
                    Ekey.BackColor = Color.FromArgb(17, 17, 17);
                    break;
                case Keys.R:
                    Rkey.BackColor = Color.FromArgb(17, 17, 17);
                    break;
                case Keys.T:
                    Tkey.BackColor = Color.FromArgb(17, 17, 17);
                    break;
                case Keys.Y:
                    Ykey.BackColor = Color.FromArgb(17, 17, 17);
                    break;
                case Keys.U:
                    Ukey.BackColor = Color.FromArgb(17, 17, 17);
                    break;
                case Keys.I:
                    Ikey.BackColor = Color.FromArgb(17, 17, 17);
                    break;
                case Keys.O:
                    Okey.BackColor = Color.FromArgb(17, 17, 17);
                    break;
                case Keys.P:
                    Pkey.BackColor = Color.FromArgb(17, 17, 17);
                    break;
                case Keys.A:
                    Akey.BackColor = Color.FromArgb(17, 17, 17);
                    break;
                case Keys.S:
                    Skey.BackColor = Color.FromArgb(17, 17, 17);
                    break;
                case Keys.D:
                    Dkey.BackColor = Color.FromArgb(17, 17, 17);
                    break;
                case Keys.F:
                    Fkey.BackColor = Color.FromArgb(17, 17, 17);
                    break;
                case Keys.G:
                    Gkey.BackColor = Color.FromArgb(17, 17, 17);
                    break;
                case Keys.H:
                    Hkey.BackColor = Color.FromArgb(17, 17, 17);
                    break;
                case Keys.J:
                    Jkey.BackColor = Color.FromArgb(17, 17, 17);
                    break;
                case Keys.K:
                    Kkey.BackColor = Color.FromArgb(17, 17, 17);
                    break;
                case Keys.L:
                    Lkey.BackColor = Color.FromArgb(17, 17, 17);
                    break;
                case Keys.Z:
                    Zkey.BackColor = Color.FromArgb(17, 17, 17);
                    break;
                case Keys.X:
                    Xkey.BackColor = Color.FromArgb(17, 17, 17);
                    break;
                case Keys.C:
                    Ckey.BackColor = Color.FromArgb(17, 17, 17);
                    break;
                case Keys.V:
                    Vkey.BackColor = Color.FromArgb(17, 17, 17);
                    break;
                case Keys.B:
                    Bkey.BackColor = Color.FromArgb(17, 17, 17);
                    break;
                case Keys.N:
                    Nkey.BackColor = Color.FromArgb(17, 17, 17);
                    break;
                case Keys.M:
                    Mkey.BackColor = Color.FromArgb(17, 17, 17);
                    break;
                case Keys.LShiftKey:
                    Shiftkey.BackColor = Color.FromArgb(17, 17, 17);
                    break;
                case Keys.RShiftKey:
                    Shiftkey0.BackColor = Color.FromArgb(17, 17, 17);
                    break;
                case Keys.LControlKey:
                    CTRLkey.BackColor = Color.FromArgb(17, 17, 17);
                    break;
                case Keys.RControlKey:
                    CTRLkey0.BackColor = Color.FromArgb(17, 17, 17);
                    break;
                case Keys.Space:
                    Spacekey.BackColor = Color.FromArgb(17, 17, 17);
                    break;
                case Keys.Enter:
                    Enterkey.BackColor = Color.FromArgb(17, 17, 17);
                    break;
                case Keys.Back:
                    Backspacekey.BackColor = Color.FromArgb(17, 17, 17);
                    break;
                case Keys.Tab:
                    Tabkey.BackColor = Color.FromArgb(17, 17, 17);
                    break;
                case Keys.Escape:
                    ESCkey.BackColor = Color.FromArgb(17, 17, 17);
                    break;
                case Keys.Up:
                    arrow0.BackColor = Color.FromArgb(17, 17, 17);
                    break;
                case Keys.Down:
                    arrow2.BackColor = Color.FromArgb(17, 17, 17);
                    break;
                case Keys.Left:
                    arrow1.BackColor = Color.FromArgb(17, 17, 17);
                    break;
                case Keys.Right:
                    arrow0.BackColor = Color.FromArgb(17, 17, 17);
                    break;
                case Keys.Delete:
                    DELkey.BackColor = Color.FromArgb(17, 17, 17);
                    break;
                case Keys.Insert:
                    INSkey.BackColor = Color.FromArgb(17, 17, 17);
                    break;
                case Keys.Home:
                    HOMEkey.BackColor = Color.FromArgb(17, 17, 17);
                    break;
                case Keys.End:
                    ENDkey.BackColor = Color.FromArgb(17, 17, 17);
                    break;
                case Keys.PageUp:
                    PageUPkey.BackColor = Color.FromArgb(17, 17, 17);
                    break;
                case Keys.PageDown:
                    PageDownkey.BackColor = Color.FromArgb(17, 17, 17);
                    break;
                case Keys.F1:
                    F1key.BackColor = Color.FromArgb(17, 17, 17);
                    break;
                case Keys.F2:
                    F2key.BackColor = Color.FromArgb(17, 17, 17);
                    break;
                case Keys.F3:
                    F3key.BackColor = Color.FromArgb(17, 17, 17);
                    break;
                case Keys.F4:
                    F4key.BackColor = Color.FromArgb(17, 17, 17);
                    break;
                case Keys.F5:
                    F5key.BackColor = Color.FromArgb(17, 17, 17);
                    break;
                case Keys.F6:
                    F6key.BackColor = Color.FromArgb(17, 17, 17);
                    break;
                case Keys.F7:
                    F7key.BackColor = Color.FromArgb(17, 17, 17);
                    break;
                case Keys.F8:
                    F8key.BackColor = Color.FromArgb(17, 17, 17);
                    break;
                case Keys.F9:
                    F9key.BackColor = Color.FromArgb(17, 17, 17);
                    break;
                case Keys.F10:
                    F10key.BackColor = Color.FromArgb(17, 17, 17);
                    break;
                case Keys.F11:
                    F11key.BackColor = Color.FromArgb(17, 17, 17);
                    break;
                case Keys.F12:
                    F12key.BackColor = Color.FromArgb(17, 17, 17);
                    break;
                case Keys.Alt:
                    ALTkey.BackColor = Color.FromArgb(17, 17, 17);
                    break;
                case Keys.CapsLock:
                    CapsLockkey.BackColor = Color.FromArgb(17, 17, 17);
                    break;
                case Keys.LWin:
                    Winkey.BackColor = Color.FromArgb(17, 17, 17);
                    break;
                case Keys.RWin:
                    Winkey0.BackColor = Color.FromArgb(17, 17, 17);
                    break;
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            UnhookWindowsHookEx(_keyboardHookID);
            UnhookWindowsHookEx(_mouseHookID);
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, Delegate lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }
}