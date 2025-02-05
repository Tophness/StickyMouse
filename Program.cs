using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

static class Program {
  public
  const int WM_LBUTTONDOWN = 0x201,
    WM_LBUTTONUP = 0x202;
  public
  const int WM_RBUTTONDOWN = 0x204,
    WM_RBUTTONUP = 0x205;
  public
  const int WM_MBUTTONDOWN = 0x207,
    WM_MBUTTONUP = 0x208;
  public
  const int WH_MOUSE_LL = 14,
    WM_MOUSEMOVE = 0x200;
  public
  const int WH_KEYBOARD_LL = 13;
  public
  const int WM_KEYDOWN = 0x0100,
    WM_KEYUP = 0x0101;
  public
  const int WM_SYSKEYDOWN = 0x0104,
    WM_SYSKEYUP = 0x0105;

  public static double THRESHOLD = 3.0, THRESHOLD_FACTOR = 0.2;
  public static int MouseBindDown = WM_MBUTTONDOWN;
  public static int MouseBindUp = WM_MBUTTONUP;
  public static Keys KeyboardBindKey = Keys.None;
  public static bool UseKeyboardBind = false;
  public static bool LockInitialAxis = true;

  public static int ArcEditKey = (int) Keys.Tab;
  public static int ArcScrollUp = 0x20A;
  public static int ArcScrollDown = 0x20B;
  public static bool UseArcEditKeyboardBind = true;
  public static bool UseArcScrollUpKeyboardBind = true;
  public static bool UseArcScrollDownKeyboardBind = true;
  public static bool ToggleMode = false;

  static bool axisLocked;
  static double lockedDirX, lockedDirY;
  static IntPtr mouseHookID = IntPtr.Zero;
  static IntPtr keyboardHookID = IntPtr.Zero;
  static LowLevelMouseProc mouseProc = MouseHookCallback;
  static LowLevelKeyboardProc keyboardProc = KeyboardHookCallback;
  static bool active, computed;
  static POINT initial;
  static bool arcEditing, arcExecuting;
  public static POINT arcP1, arcP2, arcP3;
  static double bulgeDegrees;
  static PointF arcCenter;
  static double arcRadius, startAngle, sweepAngle;
  static ArcOverlay arcOverlay;
  static LinearOverlay linearOverlay;
  static bool linearKeyDown = false;
  static bool arcEditKeyDown = false;
  static string iniPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.ini");

  [STAThread]
  static void Main() {
    SetProcessDPIAware();
    LoadSettings();
    mouseHookID = SetMouseHook(mouseProc);
    keyboardHookID = SetKeyboardHook(keyboardProc);
    NotifyIcon notifyIcon = new NotifyIcon {
      Icon = SystemIcons.Application,
        Text = "Mouse Constraint App",
        Visible = true,
        ContextMenuStrip = new ContextMenuStrip()
    };
    notifyIcon.ContextMenuStrip.Items.Add("Settings...", null, (s, e) => ShowSettings());
    notifyIcon.ContextMenuStrip.Items.Add("Exit", null, (s, e) => Application.Exit());
    Application.Run();
    UnhookWindowsHookEx(mouseHookID);
    UnhookWindowsHookEx(keyboardHookID);
    notifyIcon.Dispose();
    arcOverlay?.Dispose();
    linearOverlay?.Dispose();
  }

  static void LoadSettings() {
    THRESHOLD = double.Parse(ReadIni("Settings", "Threshold", "3.0"));
    THRESHOLD_FACTOR = double.Parse(ReadIni("Settings", "ThresholdFactor", "0.2"));
    UseKeyboardBind = bool.Parse(ReadIni("Settings", "UseKeyboardBind", "False"));
    LockInitialAxis = bool.Parse(ReadIni("Settings", "LockInitialAxis", "True"));
    MouseBindDown = Convert.ToInt32(ReadIni("Settings", "MouseBindDown", WM_MBUTTONDOWN.ToString("X")), 16);
    MouseBindUp = Convert.ToInt32(ReadIni("Settings", "MouseBindUp", WM_MBUTTONUP.ToString("X")), 16);
    KeyboardBindKey = (Keys) int.Parse(ReadIni("Settings", "KeyboardBindKey", "0"));
    ArcEditKey = int.Parse(ReadIni("Settings", "ArcEditKey", ((int) Keys.Tab).ToString()));
    ArcScrollUp = Convert.ToInt32(ReadIni("Settings", "ArcScrollUp", ((int) 0x20A).ToString("X")), 16);
    ArcScrollDown = Convert.ToInt32(ReadIni("Settings", "ArcScrollDown", ((int) 0x20B).ToString("X")), 16);
    UseArcEditKeyboardBind = bool.Parse(ReadIni("Settings", "UseArcEditKeyboardBind", "true"));
    UseArcScrollUpKeyboardBind = bool.Parse(ReadIni("Settings", "UseArcScrollUpKeyboardBind", "true"));
    UseArcScrollDownKeyboardBind = bool.Parse(ReadIni("Settings", "UseArcScrollDownKeyboardBind", "true"));
    ToggleMode = bool.Parse(ReadIni("Settings", "ToggleMode", "False"));
  }

  static void SaveSettings() {
    WriteIni("Settings", "Threshold", THRESHOLD.ToString());
    WriteIni("Settings", "ThresholdFactor", THRESHOLD_FACTOR.ToString());
    WriteIni("Settings", "LockInitialAxis", LockInitialAxis.ToString());
    WriteIni("Settings", "UseKeyboardBind", UseKeyboardBind.ToString());
    WriteIni("Settings", "MouseBindDown", MouseBindDown.ToString("X"));
    WriteIni("Settings", "MouseBindUp", MouseBindUp.ToString("X"));
    WriteIni("Settings", "KeyboardBindKey", ((int) KeyboardBindKey).ToString());
    WriteIni("Settings", "ArcEditKey", ((int) ArcEditKey).ToString());
    WriteIni("Settings", "ArcScrollUp", ArcScrollUp.ToString("X"));
    WriteIni("Settings", "ArcScrollDown", ArcScrollDown.ToString("X"));
    WriteIni("Settings", "UseArcEditKeyboardBind", UseArcEditKeyboardBind.ToString());
    WriteIni("Settings", "UseArcScrollUpKeyboardBind", UseArcScrollUpKeyboardBind.ToString());
    WriteIni("Settings", "UseArcScrollDownKeyboardBind", UseArcScrollDownKeyboardBind.ToString());
    WriteIni("Settings", "ToggleMode", ToggleMode.ToString());
  }

  static string ReadIni(string section, string key, string defaultValue) {
    StringBuilder sb = new StringBuilder(255);
    GetPrivateProfileString(section, key, defaultValue, sb, (uint) sb.Capacity, iniPath);
    return sb.ToString();
  }

  static void WriteIni(string section, string key, string value) {
    WritePrivateProfileString(section, key, value, iniPath);
  }

  static void ShowSettings() {
    using(var settingsForm = new SettingsForm()) {
      if (settingsForm.ShowDialog() == DialogResult.OK)
        SaveSettings();
    }
  }

  static void ComputeArc() {
    float x1 = arcP1.x, y1 = arcP1.y, x2 = arcP2.x, y2 = arcP2.y;
    double dx = x2 - x1, dy = y2 - y1;
    double d = Math.Sqrt(dx * dx + dy * dy);
    double theta = Math.PI / 2 + bulgeDegrees * Math.PI / 180.0;
    double R = d / (2 * Math.Sin(theta / 2));
    arcRadius = R;
    double mx = (x1 + x2) / 2.0, my = (y1 + y2) / 2.0;
    double h = Math.Sqrt(R * R - (d / 2) * (d / 2));
    double ux = -dy / d, uy = dx / d;

    bool flipHoriz = (arcP2.x < arcP1.x);
    bool flipVert = (arcP2.y < arcP1.y);
    int flips = (flipHoriz ? 1 : 0) + (flipVert ? 1 : 0);
    double flipSign = (flips % 2 == 1) ? -1 : 1;

    double cx = mx + flipSign * h * ux;
    double cy = my + flipSign * h * uy;
    arcCenter = new PointF((float) cx, (float) cy);

    startAngle = Math.Atan2(y1 - cy, x1 - cx);
    sweepAngle = flipSign * theta;

    double endAngle = startAngle + sweepAngle;
    arcP2.x = (int) Math.Round(cx + R * Math.Cos(endAngle));
    arcP2.y = (int) Math.Round(cy + R * Math.Sin(endAngle));
  }

  static IntPtr SetMouseHook(LowLevelMouseProc proc) {
    using(Process curProcess = Process.GetCurrentProcess())
    using(ProcessModule curModule = curProcess.MainModule)
    return SetWindowsHookEx_Mouse(WH_MOUSE_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
  }

  static IntPtr SetKeyboardHook(LowLevelKeyboardProc proc) {
    using(Process curProcess = Process.GetCurrentProcess())
    using(ProcessModule curModule = curProcess.MainModule)
    return SetWindowsHookEx_Keyboard(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
  }

  static IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam) {
    if (nCode >= 0) {
      MSLLHOOKSTRUCT hs = Marshal.PtrToStructure < MSLLHOOKSTRUCT > (lParam);
      if (arcEditing) {
        if (wParam == (IntPtr) WM_MOUSEMOVE) {
          GetCursorPos(out arcP2);
          ComputeArc();
          arcOverlay.UpdateArc(arcP1, arcP2, arcCenter, arcRadius, startAngle, sweepAngle);
          return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
        }
        if (wParam == (IntPtr) 0x020A) {
          int delta = (short)((hs.mouseData >> 16) & 0xffff);
          bulgeDegrees += delta > 0 ? 1 : -1;
          ComputeArc();
          arcOverlay.UpdateArc(arcP1, arcP2, arcCenter, arcRadius, startAngle, sweepAngle);
          return (IntPtr) 1;
        }
      }
      if (!UseArcEditKeyboardBind) {
        if ((int) wParam == ArcEditKey) {
          if (!arcEditing && !arcExecuting) {
            arcEditing = true;
            bulgeDegrees = 0;
            GetCursorPos(out arcP1);
            arcP2 = arcP1;
            if (arcOverlay == null) arcOverlay = new ArcOverlay();
            arcOverlay.Show();
          } else if (arcExecuting) {
            arcExecuting = false;
            arcOverlay.Hide();
          }
          return (IntPtr) 1;
        }
        int expectedUp = ArcEditKey
        switch {
          WM_LBUTTONDOWN => WM_LBUTTONUP,
            WM_RBUTTONDOWN => WM_RBUTTONUP,
            WM_MBUTTONDOWN => WM_MBUTTONUP,
            _ => 0
        };
        if ((int) wParam == expectedUp && arcEditing) {
          arcEditing = false;
          GetCursorPos(out arcP2);
          ComputeArc();
          arcOverlay.UpdateArc(arcP1, arcP2, arcCenter, arcRadius, startAngle, sweepAngle);
          arcExecuting = true;
          return (IntPtr) 1;
        }
      }
      if (arcExecuting && wParam == (IntPtr) WM_MOUSEMOVE) {
        POINT cp = hs.pt;
        double dx = cp.x - arcCenter.X, dy = cp.y - arcCenter.Y;
        double currentAngle = Math.Atan2(dy, dx);
        double angleDiff = currentAngle - startAngle;
        if (sweepAngle > 0 && angleDiff < 0)
          angleDiff += 2 * Math.PI;
        else if (sweepAngle < 0 && angleDiff > 0)
          angleDiff -= 2 * Math.PI;
        double t = angleDiff / sweepAngle;
        int nx = (int) Math.Round(arcCenter.X + arcRadius * Math.Cos(startAngle + sweepAngle * t));
        int ny = (int) Math.Round(arcCenter.Y + arcRadius * Math.Sin(startAngle + sweepAngle * t));
        SetCursorPos(nx, ny);
        return (IntPtr) 1;
      }
      if (!UseKeyboardBind) {
        if (wParam == (IntPtr) MouseBindDown) {
          if (ToggleMode) {
            if (!linearKeyDown) {
              active = !active;
              if (active) {
                initial = hs.pt;
                computed = false;
                axisLocked = false;
              } else {
                axisLocked = false;
                linearOverlay?.Hide();
              }
            }
            linearKeyDown = true;
          } else {
            active = true;
            computed = false;
            initial = hs.pt;
          }
        } else if (wParam == (IntPtr) MouseBindUp) {
          linearKeyDown = false;
          if (!ToggleMode) {
            active = false;
            axisLocked = false;
            linearOverlay?.Hide();
          }
        }
      }
      if (wParam == (IntPtr) WM_MOUSEMOVE && active) {
        HandleMouseMove(hs.pt);
        return (IntPtr) 1;
      }
    }
    return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
  }

  static IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam) {
    if (nCode >= 0 && UseKeyboardBind) {
      int vkCode = Marshal.ReadInt32(lParam);
      Keys key = (Keys) vkCode;
      if (UseArcEditKeyboardBind && key == (Keys) ArcEditKey) {
        if (wParam == (IntPtr) WM_KEYDOWN) {
          if (ToggleMode) {
            if (!arcEditKeyDown) {
              if (arcExecuting) {
                arcExecuting = false;
                arcOverlay.Hide();
              } else if (arcEditing) {
                arcEditing = false;
                GetCursorPos(out arcP2);
                ComputeArc();
                arcOverlay.UpdateArc(arcP1, arcP2, arcCenter, arcRadius, startAngle, sweepAngle);
                arcExecuting = true;
              } else {
                arcEditing = true;
                bulgeDegrees = 0;
                GetCursorPos(out arcP1);
                arcP2 = arcP1;
                if (arcOverlay == null) arcOverlay = new ArcOverlay();
                arcOverlay.Show();
              }
            }
            arcEditKeyDown = true;
          } else {
            if (!arcEditing && !arcExecuting) {
              arcEditing = true;
              bulgeDegrees = 0;
              GetCursorPos(out arcP1);
              arcP2 = arcP1;
              if (arcOverlay == null) arcOverlay = new ArcOverlay();
              arcOverlay.Show();
            } else if (arcExecuting) {
              arcExecuting = false;
              arcOverlay.Hide();
            }
          }
          return (IntPtr) 1;
        } else if (wParam == (IntPtr) WM_KEYUP) {
          arcEditKeyDown = false;
          if (!ToggleMode && arcEditing) {
            arcEditing = false;
            GetCursorPos(out arcP2);
            ComputeArc();
            arcOverlay.UpdateArc(arcP1, arcP2, arcCenter, arcRadius, startAngle, sweepAngle);
            arcExecuting = true;
            return (IntPtr) 1;
          }
        }
      }

      if (arcEditing && (key == (Keys) ArcScrollUp || key == (Keys) ArcScrollDown)) {
        if (wParam == (IntPtr) WM_KEYDOWN) {
          bulgeDegrees += key == (Keys) ArcScrollUp ? 1 : -1;
          ComputeArc();
          arcOverlay.UpdateArc(arcP1, arcP2, arcCenter, arcRadius, startAngle, sweepAngle);
        }
        return (IntPtr) 1;
      }

      if (key == KeyboardBindKey) {
        if (wParam == (IntPtr) WM_KEYDOWN || wParam == (IntPtr) WM_SYSKEYDOWN) {
          if (ToggleMode) {
            if (!linearKeyDown) {
              active = !active;
              if (active) {
                GetCursorPos(out initial);
                computed = false;
                axisLocked = false;
              } else {
                axisLocked = false;
                linearOverlay?.Hide();
              }
            }
            linearKeyDown = true;
          } else {
            active = true;
            computed = false;
            GetCursorPos(out initial);
          }
        } else if (wParam == (IntPtr) WM_KEYUP || wParam == (IntPtr) WM_SYSKEYUP) {
          linearKeyDown = false;
          if (!ToggleMode) {
            active = false;
            axisLocked = false;
            linearOverlay?.Hide();
          }
        }
      }
    }
    return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
  }

  static void HandleMouseMove(POINT current) {
    int dx = current.x - initial.x, dy = current.y - initial.y;
    double dist = Math.Sqrt(dx * dx + dy * dy);
    if (!axisLocked) {
      if (dist >= THRESHOLD) {
        double angle = Math.Atan2(dy, dx);
        double quant = Math.Round(angle / (Math.PI / 4)) * (Math.PI / 4);
        lockedDirX = Math.Cos(quant);
        lockedDirY = Math.Sin(quant);
        axisLocked = true;
        if (linearOverlay == null)
          linearOverlay = new LinearOverlay();
        linearOverlay.UpdateLine(new PointF(initial.x, initial.y),
          new PointF((float) lockedDirX, (float) lockedDirY));
        linearOverlay.Show();
      }
    }
    if (axisLocked) {
      double proj = dx * lockedDirX + dy * lockedDirY;
      if (!LockInitialAxis) {
        double perpDist = Math.Abs(dx * lockedDirY - dy * lockedDirX);
        double dynamicThreshold = THRESHOLD_FACTOR * Math.Abs(proj);
        if (perpDist > dynamicThreshold) {
          axisLocked = false;
          linearOverlay?.Hide();
          return;
        }
      }
      int nx = initial.x + (int) Math.Round(proj * lockedDirX);
      int ny = initial.y + (int) Math.Round(proj * lockedDirY);
      SetCursorPos(nx, ny);
      linearOverlay?.UpdateLine(new PointF(initial.x, initial.y),
        new PointF((float) lockedDirX, (float) lockedDirY));
    }
  }

  [StructLayout(LayoutKind.Sequential)]
  public struct POINT {
    public int x;
    public int y;
  }
  [StructLayout(LayoutKind.Sequential)]
  struct MSLLHOOKSTRUCT {
    public POINT pt;
    public uint mouseData;
    public uint flags;
    public uint time;
    public IntPtr dwExtraInfo;
  }
  delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);
  delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
  [DllImport("user32.dll", SetLastError = true, EntryPoint = "SetWindowsHookEx")]
  static extern IntPtr SetWindowsHookEx_Mouse(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);
  [DllImport("user32.dll", SetLastError = true, EntryPoint = "SetWindowsHookEx")]
  static extern IntPtr SetWindowsHookEx_Keyboard(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);
  [DllImport("user32.dll", SetLastError = true)]
  static extern bool UnhookWindowsHookEx(IntPtr hhk);
  [DllImport("user32.dll")]
  static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
  [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
  static extern IntPtr GetModuleHandle(string lpModuleName);
  [DllImport("user32.dll")]
  static extern bool SetCursorPos(int X, int Y);
  [DllImport("user32.dll")]
  static extern bool GetCursorPos(out POINT lpPoint);
  [DllImport("user32.dll")]
  static extern bool SetProcessDPIAware();
  [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
  static extern uint GetPrivateProfileString(string lpAppName, string lpKeyName, string lpDefault, StringBuilder lpReturnedString, uint nSize, string lpFileName);
  [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
  static extern bool WritePrivateProfileString(string lpAppName, string lpKeyName, string lpString, string lpFileName);
}

class ArcOverlay: Form {
  PointF arcCenter;
  double arcRadius, startAngle, sweepAngle;

  public ArcOverlay() {
    FormBorderStyle = FormBorderStyle.None;
    ShowInTaskbar = false;
    TopMost = true;
    BackColor = Color.Black;
    TransparencyKey = Color.Black;
    Opacity = 0.25;
    WindowState = FormWindowState.Maximized;
  }

  protected override CreateParams CreateParams {
    get {
      CreateParams cp = base.CreateParams;
      cp.ExStyle |= 0x80000 | 0x20;
      return cp;
    }
  }

  public void UpdateArc(Program.POINT p1, Program.POINT p3, PointF center, double radius, double start, double sweep) {
    arcCenter = center;
    arcRadius = radius;
    startAngle = start * 180 / Math.PI;
    sweepAngle = sweep * 180 / Math.PI;
    Invalidate();
  }

  protected override void OnPaint(PaintEventArgs e) {
    if (arcRadius <= 0 || Math.Abs(sweepAngle) < 0.1) return;
    using(var pen = new Pen(Color.Red, 5)) {
      RectangleF rect = new RectangleF(arcCenter.X - (float) arcRadius, arcCenter.Y - (float) arcRadius, (float) arcRadius * 2, (float) arcRadius * 2);
      e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
      e.Graphics.DrawArc(pen, rect, (float) startAngle, (float) sweepAngle);
    }
    base.OnPaint(e);
  }
}

class LinearOverlay: Form {
  private PointF startPoint;
  private PointF direction;

  public LinearOverlay() {
    FormBorderStyle = FormBorderStyle.None;
    ShowInTaskbar = false;
    TopMost = true;
    BackColor = Color.Black;
    TransparencyKey = Color.Black;
    Opacity = 0.25;
    WindowState = FormWindowState.Maximized;
  }

  protected override CreateParams CreateParams {
    get {
      CreateParams cp = base.CreateParams;
      cp.ExStyle |= 0x80000 | 0x20;
      return cp;
    }
  }

  public void UpdateLine(PointF start, PointF dir) {
    startPoint = start;
    direction = dir;
    Invalidate();
  }

  protected override void OnPaint(PaintEventArgs e) {
    if (direction == PointF.Empty || (direction.X == 0 && direction.Y == 0)) return;

    using(var pen = new Pen(Color.Blue, 5)) {
      Rectangle screenBounds = Screen.PrimaryScreen.Bounds;
      PointF endPoint1 = CalculateEdgeIntersection(startPoint, direction, screenBounds);
      PointF endPoint2 = CalculateEdgeIntersection(startPoint, new PointF(-direction.X, -direction.Y), screenBounds);

      endPoint1 = ClampPointToScreen(endPoint1, screenBounds);
      endPoint2 = ClampPointToScreen(endPoint2, screenBounds);

      e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
      e.Graphics.DrawLine(pen, endPoint1, endPoint2);
    }

    base.OnPaint(e);
  }

  private PointF CalculateEdgeIntersection(PointF start, PointF dir, Rectangle bounds) {
    if (dir.X == 0 && dir.Y == 0)
      return start;

    float tX = (dir.X != 0) ? ((dir.X > 0) ? (bounds.Right - start.X) / dir.X : (bounds.Left - start.X) / dir.X) : float.MaxValue;
    float tY = (dir.Y != 0) ? ((dir.Y > 0) ? (bounds.Bottom - start.Y) / dir.Y : (bounds.Top - start.Y) / dir.Y) : float.MaxValue;
    float t = Math.Min(tX, tY);

    if (float.IsInfinity(t) || float.IsNaN(t))
      return start;

    return new PointF(start.X + dir.X * t, start.Y + dir.Y * t);
  }

  private PointF ClampPointToScreen(PointF point, Rectangle bounds) {
    point.X = Math.Clamp(point.X, bounds.Left, bounds.Right);
    point.Y = Math.Clamp(point.Y, bounds.Top, bounds.Bottom);
    return point;
  }
}

public partial class SettingsForm: Form {
  enum BindType {
    Linear,
    Arc,
    ArcScrollUp,
    ArcScrollDown
  }
  BindType currentBindType;
  [DllImport("user32.dll")] static extern short GetKeyState(int nVirtKey);
  public SettingsForm() {
    InitializeComponent();
    numericUpDownThreshold.Value = (decimal) Program.THRESHOLD;
    numericUpDownThresholdFactor.Value = (decimal) Program.THRESHOLD_FACTOR;
    lblLinearBind.Text = Program.UseKeyboardBind ? Program.KeyboardBindKey.ToString() :
      (Program.MouseBindDown == Program.WM_LBUTTONDOWN ? "Left Mouse Button" :
        Program.MouseBindDown == Program.WM_RBUTTONDOWN ? "Right Mouse Button" :
        Program.MouseBindDown == Program.WM_MBUTTONDOWN ? "Middle Mouse Button" : "Unknown");
    lblArcBind.Text = ((Keys) Program.ArcEditKey).ToString();
    lblArcScrollUpBind.Text = Program.ArcScrollUp.ToString("X");
    lblArcScrollDownBind.Text = Program.ArcScrollDown.ToString("X");
    lblGlobalStatus.Text = "Ready.";
    checkBoxLockAxis.Checked = Program.LockInitialAxis;
  }
  protected override bool ProcessCmdKey(ref Message msg, Keys keyData) {
    if (Capture && (currentBindType == BindType.Linear || currentBindType == BindType.Arc || currentBindType == BindType.ArcScrollUp || currentBindType == BindType.ArcScrollDown)) {
      KeyEventArgs e = new KeyEventArgs(keyData);
      CaptureBindKeyDown(this, e);
      return true;
    }
    return base.ProcessCmdKey(ref msg, keyData);
  }
  private Keys GetSpecificKey(KeyEventArgs e) {
    Keys finalKey = e.KeyCode;
    if (e.KeyCode == Keys.ShiftKey) {
      if (GetKeyState(0xA0) < 0)
        finalKey = Keys.LShiftKey;
      else if (GetKeyState(0xA1) < 0)
        finalKey = Keys.RShiftKey;
    } else if (e.KeyCode == Keys.ControlKey) {
      if (GetKeyState(0xA2) < 0)
        finalKey = Keys.LControlKey;
      else if (GetKeyState(0xA3) < 0)
        finalKey = Keys.RControlKey;
    } else if (e.KeyCode == Keys.Menu) {
      if (GetKeyState(0xA4) < 0)
        finalKey = Keys.LMenu;
      else if (GetKeyState(0xA5) < 0)
        finalKey = Keys.RMenu;
    }
    return finalKey;
  }
  private void btnSetLinearBind_Click(object sender, EventArgs e) {
    currentBindType = BindType.Linear;
    lblGlobalStatus.Text = "Press a mouse button or key...";
    this.KeyPreview = true;
    this.Capture = true;
    this.KeyDown += CaptureBindKeyDown;
    this.MouseDown += CaptureBindMouseDown;
  }
  private void btnSetArcBind_Click(object sender, EventArgs e) {
    currentBindType = BindType.Arc;
    lblGlobalStatus.Text = "Press a mouse button or key...";
    this.KeyPreview = true;
    this.Capture = true;
    this.KeyDown += CaptureBindKeyDown;
    this.MouseDown += CaptureBindMouseDown;
  }
  private void btnSetArcScrollUpBind_Click(object sender, EventArgs e) {
    currentBindType = BindType.ArcScrollUp;
    lblGlobalStatus.Text = "Scroll mouse wheel up...";
    this.KeyPreview = true;
    this.Capture = true;
    this.MouseWheel += CaptureBindMouseWheel;
  }
  private void btnSetArcScrollDownBind_Click(object sender, EventArgs e) {
    currentBindType = BindType.ArcScrollDown;
    lblGlobalStatus.Text = "Scroll mouse wheel down...";
    this.KeyPreview = true;
    this.Capture = true;
    this.MouseWheel += CaptureBindMouseWheel;
  }
  private void CaptureBindKeyDown(object sender, KeyEventArgs e) {
    this.Capture = false;
    this.KeyDown -= CaptureBindKeyDown;
    this.MouseDown -= CaptureBindMouseDown;
    this.MouseWheel -= CaptureBindMouseWheel;
    this.KeyPreview = false;
    Keys specificKey = GetSpecificKey(e);
    if (currentBindType == BindType.Linear) {
      Program.KeyboardBindKey = specificKey;
      Program.UseKeyboardBind = true;
      lblLinearBind.Text = specificKey.ToString();
    } else if (currentBindType == BindType.Arc) {
      Program.ArcEditKey = (int) specificKey;
      Program.UseArcEditKeyboardBind = true;
      lblArcBind.Text = specificKey.ToString();
    } else if (currentBindType == BindType.ArcScrollUp) {
      Program.ArcScrollUp = (int) specificKey;
      Program.UseArcScrollUpKeyboardBind = true;
      lblArcScrollUpBind.Text = specificKey.ToString();
    } else if (currentBindType == BindType.ArcScrollDown) {
      Program.ArcScrollDown = (int) specificKey;
      Program.UseArcScrollDownKeyboardBind = true;
      lblArcScrollDownBind.Text = specificKey.ToString();
    }
    lblGlobalStatus.Text = "Ready.";
    e.Handled = true;
  }
  private void CaptureBindMouseDown(object sender, MouseEventArgs e) {
    this.Capture = false;
    this.KeyDown -= CaptureBindKeyDown;
    this.MouseDown -= CaptureBindMouseDown;
    this.KeyPreview = false;
    (int down, int up) = e.Button
    switch {
      MouseButtons.Left => (Program.WM_LBUTTONDOWN, Program.WM_LBUTTONUP),
        MouseButtons.Right => (Program.WM_RBUTTONDOWN, Program.WM_RBUTTONUP),
        MouseButtons.Middle => (Program.WM_MBUTTONDOWN, Program.WM_MBUTTONUP),
        _ => (-1, -1)
    };
    if (down == -1) {
      MessageBox.Show("Unsupported mouse button");
      return;
    }
    if (currentBindType == BindType.Linear) {
      Program.MouseBindDown = down;
      Program.MouseBindUp = up;
      Program.UseKeyboardBind = false;
      lblLinearBind.Text = e.Button.ToString();
    } else if (currentBindType == BindType.Arc) {
      Program.ArcEditKey = down;
      Program.UseArcEditKeyboardBind = false;
      lblArcBind.Text = e.Button.ToString();
    }
    lblGlobalStatus.Text = "Ready.";
  }
  private void CaptureBindMouseWheel(object sender, MouseEventArgs e) {
    this.MouseWheel -= CaptureBindMouseWheel;
    this.Capture = false;
    this.KeyPreview = false;
    if (currentBindType == BindType.ArcScrollUp) {
      Program.ArcScrollUp = 0x20A;
      Program.UseArcScrollUpKeyboardBind = false;
      lblArcScrollUpBind.Text = "Mouse Wheel Up";
    } else if (currentBindType == BindType.ArcScrollDown) {
      Program.ArcScrollDown = 0x20B;
      Program.UseArcScrollDownKeyboardBind = false;
      lblArcScrollDownBind.Text = "Mouse Wheel Down";
    }
    lblGlobalStatus.Text = "Ready.";
  }
  private void btnOK_Click(object sender, EventArgs e) {
    Program.THRESHOLD = (double) numericUpDownThreshold.Value;
    Program.THRESHOLD_FACTOR = (double) numericUpDownThresholdFactor.Value;
    Program.LockInitialAxis = checkBoxLockAxis.Checked;
    Program.ToggleMode = checkBoxToggleMode.Checked;
    DialogResult = DialogResult.OK;
    Close();
  }
  private void btnCancel_Click(object sender, EventArgs e) {
    Close();
  }
  private GroupBox groupBoxLinear;
  private Button btnSetLinearBind;
  private Label lblLinearCurrent;
  private Label lblLinearBind;
  private GroupBox groupBoxArc;
  private Button btnSetArcBind;
  private Label lblArcCurrent;
  private Label lblArcBind;
  private GroupBox groupBoxArcScrollUp;
  private Button btnSetArcScrollUpBind;
  private Label lblArcScrollUpCurrent;
  private Label lblArcScrollUpBind;
  private GroupBox groupBoxArcScrollDown;
  private Button btnSetArcScrollDownBind;
  private Label lblArcScrollDownCurrent;
  private Label lblArcScrollDownBind;
  private Label lblGlobalStatus;
  private Label lblThreshold;
  private NumericUpDown numericUpDownThreshold;
  private Label lblThresholdFactor;
  private NumericUpDown numericUpDownThresholdFactor;
  private Button btnOK;
  private Button btnCancel;
  private CheckBox checkBoxLockAxis;
  private CheckBox checkBoxToggleMode;
  private void InitializeComponent() {
    this.checkBoxLockAxis = new CheckBox();
    this.groupBoxLinear = new GroupBox();
    this.btnSetLinearBind = new Button();
    this.lblLinearCurrent = new Label();
    this.lblLinearBind = new Label();
    this.groupBoxArc = new GroupBox();
    this.btnSetArcBind = new Button();
    this.lblArcCurrent = new Label();
    this.lblArcBind = new Label();
    this.groupBoxArcScrollUp = new GroupBox();
    this.btnSetArcScrollUpBind = new Button();
    this.lblArcScrollUpCurrent = new Label();
    this.lblArcScrollUpBind = new Label();
    this.groupBoxArcScrollDown = new GroupBox();
    this.btnSetArcScrollDownBind = new Button();
    this.lblArcScrollDownCurrent = new Label();
    this.lblArcScrollDownBind = new Label();
    this.lblGlobalStatus = new Label();
    this.lblThreshold = new Label();
    this.numericUpDownThreshold = new NumericUpDown();
    this.lblThresholdFactor = new Label();
    this.numericUpDownThresholdFactor = new NumericUpDown();
    this.btnOK = new Button();
    this.btnCancel = new Button();
    ((System.ComponentModel.ISupportInitialize)(this.numericUpDownThreshold)).BeginInit();
    ((System.ComponentModel.ISupportInitialize)(this.numericUpDownThresholdFactor)).BeginInit();
    this.SuspendLayout();
    this.groupBoxLinear.Text = "Linear Mode";
    this.groupBoxLinear.Location = new Point(15, 110);
    this.groupBoxLinear.Size = new Size(250, 80);
    this.btnSetLinearBind.Location = new Point(10, 20);
    this.btnSetLinearBind.Size = new Size(110, 23);
    this.btnSetLinearBind.Text = "Bind";
    this.btnSetLinearBind.Click += new EventHandler(this.btnSetLinearBind_Click);
    this.lblLinearCurrent.Location = new Point(10, 45);
    this.lblLinearCurrent.Size = new Size(100, 20);
    this.lblLinearCurrent.Text = "Current Bind Key:";
    this.lblLinearBind.Location = new Point(115, 45);
    this.lblLinearBind.Size = new Size(100, 20);
    this.lblLinearBind.Text = "None";
    this.groupBoxLinear.Controls.Add(this.btnSetLinearBind);
    this.groupBoxLinear.Controls.Add(this.lblLinearCurrent);
    this.groupBoxLinear.Controls.Add(this.lblLinearBind);
    this.groupBoxArc.Text = "Arc Mode";
    this.groupBoxArc.Location = new Point(15, 190);
    this.groupBoxArc.Size = new Size(250, 80);
    this.btnSetArcBind.Location = new Point(10, 20);
    this.btnSetArcBind.Size = new Size(110, 23);
    this.btnSetArcBind.Text = "Bind";
    this.btnSetArcBind.Click += new EventHandler(this.btnSetArcBind_Click);
    this.lblArcCurrent.Location = new Point(10, 45);
    this.lblArcCurrent.Size = new Size(100, 20);
    this.lblArcCurrent.Text = "Current Bind Key:";
    this.lblArcBind.Location = new Point(115, 45);
    this.lblArcBind.Size = new Size(100, 20);
    this.lblArcBind.Text = "None";
    this.groupBoxArc.Controls.Add(this.btnSetArcBind);
    this.groupBoxArc.Controls.Add(this.lblArcCurrent);
    this.groupBoxArc.Controls.Add(this.lblArcBind);
    this.groupBoxArcScrollUp.Text = "Arc Angle +";
    this.groupBoxArcScrollUp.Location = new Point(15, 270);
    this.groupBoxArcScrollUp.Size = new Size(250, 80);
    this.btnSetArcScrollUpBind.Location = new Point(10, 20);
    this.btnSetArcScrollUpBind.Size = new Size(110, 23);
    this.btnSetArcScrollUpBind.Text = "Bind";
    this.btnSetArcScrollUpBind.Click += new EventHandler(this.btnSetArcScrollUpBind_Click);
    this.lblArcScrollUpCurrent.Location = new Point(10, 45);
    this.lblArcScrollUpCurrent.Size = new Size(100, 20);
    this.lblArcScrollUpCurrent.Text = "Current Bind Key:";
    this.lblArcScrollUpBind.Location = new Point(115, 45);
    this.lblArcScrollUpBind.Size = new Size(100, 20);
    this.lblArcScrollUpBind.Text = "None";
    this.groupBoxArcScrollUp.Controls.Add(this.btnSetArcScrollUpBind);
    this.groupBoxArcScrollUp.Controls.Add(this.lblArcScrollUpCurrent);
    this.groupBoxArcScrollUp.Controls.Add(this.lblArcScrollUpBind);
    this.groupBoxArcScrollDown.Text = "Arc Angle -";
    this.groupBoxArcScrollDown.Location = new Point(15, 350);
    this.groupBoxArcScrollDown.Size = new Size(250, 80);
    this.btnSetArcScrollDownBind.Location = new Point(10, 20);
    this.btnSetArcScrollDownBind.Size = new Size(110, 23);
    this.btnSetArcScrollDownBind.Text = "Bind";
    this.btnSetArcScrollDownBind.Click += new EventHandler(this.btnSetArcScrollDownBind_Click);
    this.lblArcScrollDownCurrent.Location = new Point(10, 45);
    this.lblArcScrollDownCurrent.Size = new Size(100, 20);
    this.lblArcScrollDownCurrent.Text = "Current Bind Key:";
    this.lblArcScrollDownBind.Location = new Point(115, 45);
    this.lblArcScrollDownBind.Size = new Size(100, 20);
    this.lblArcScrollDownBind.Text = "None";
    this.groupBoxArcScrollDown.Controls.Add(this.btnSetArcScrollDownBind);
    this.groupBoxArcScrollDown.Controls.Add(this.lblArcScrollDownCurrent);
    this.groupBoxArcScrollDown.Controls.Add(this.lblArcScrollDownBind);
    this.lblThreshold.Location = new Point(15, 10);
    this.lblThreshold.Size = new Size(100, 20);
    this.lblThreshold.Text = "Threshold:";
    this.numericUpDownThreshold.DecimalPlaces = 1;
    this.numericUpDownThreshold.Increment = 0.1M;
    this.numericUpDownThreshold.Location = new Point(160, 8);
    this.numericUpDownThreshold.Size = new Size(80, 20);
    this.numericUpDownThreshold.Minimum = 0.1M;
    this.lblThresholdFactor.Location = new Point(15, 40);
    this.lblThresholdFactor.Size = new Size(140, 20);
    this.lblThresholdFactor.Text = "Threshold Factor:";
    this.numericUpDownThresholdFactor.DecimalPlaces = 2;
    this.numericUpDownThresholdFactor.Increment = 0.01M;
    this.numericUpDownThresholdFactor.Location = new Point(160, 33);
    this.numericUpDownThresholdFactor.Size = new Size(80, 20);
    this.numericUpDownThresholdFactor.Minimum = 0.01M;
    this.checkBoxLockAxis.Location = new Point(15, 60);
    this.checkBoxLockAxis.Size = new Size(200, 20);
    this.checkBoxLockAxis.Text = "Lock to initial axis";
    this.checkBoxLockAxis.Checked = Program.LockInitialAxis;
    this.checkBoxToggleMode = new CheckBox();
    this.checkBoxToggleMode.Location = new Point(15, 85);
    this.checkBoxToggleMode.Size = new Size(200, 20);
    this.checkBoxToggleMode.Text = "Toggle Mode";
    this.checkBoxToggleMode.Checked = Program.ToggleMode;
    this.lblGlobalStatus.Location = new Point(15, 430);
    this.lblGlobalStatus.Size = new Size(250, 20);
    this.lblGlobalStatus.Text = "Ready.";
    this.btnOK.Location = new Point(15, 450);
    this.btnOK.Size = new Size(75, 23);
    this.btnOK.Text = "Save";
    this.btnOK.Click += new EventHandler(this.btnOK_Click);
    this.btnCancel.Location = new Point(100, 450);
    this.btnCancel.Size = new Size(75, 23);
    this.btnCancel.Text = "Cancel";
    this.btnCancel.Click += new EventHandler(this.btnCancel_Click);
    this.ClientSize = new Size(280, 485);
    this.Controls.Add(this.groupBoxLinear);
    this.Controls.Add(this.groupBoxArc);
    this.Controls.Add(this.groupBoxArcScrollUp);
    this.Controls.Add(this.groupBoxArcScrollDown);
    this.Controls.Add(this.lblThreshold);
    this.Controls.Add(this.numericUpDownThreshold);
    this.Controls.Add(this.lblThresholdFactor);
    this.Controls.Add(this.numericUpDownThresholdFactor);
    this.Controls.Add(this.checkBoxLockAxis);
    this.Controls.Add(this.checkBoxToggleMode);
    this.Controls.Add(this.lblGlobalStatus);
    this.Controls.Add(this.btnOK);
    this.Controls.Add(this.btnCancel);
    this.FormBorderStyle = FormBorderStyle.FixedDialog;
    this.MaximizeBox = false;
    this.MinimizeBox = false;
    this.StartPosition = FormStartPosition.CenterScreen;
    this.Text = "Settings";
    ((System.ComponentModel.ISupportInitialize)(this.numericUpDownThreshold)).EndInit();
    ((System.ComponentModel.ISupportInitialize)(this.numericUpDownThresholdFactor)).EndInit();
    this.ResumeLayout(false);
    this.PerformLayout();
  }
}