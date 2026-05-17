using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using System.Windows.Forms;

namespace VibeBetterCube
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "--self-test")
            {
                Environment.Exit(SelfTest.Run() ? 0 : 1);
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    internal static class SelfTest
    {
        public static bool Run()
        {
            Face[] faces = new Face[] { Face.Right, Face.Left, Face.Up, Face.Down, Face.Front, Face.Back };

            try
            {
                for (int size = 2; size <= 7; size++)
                {
                    CubeModel model = new CubeModel(size);
                    string solved = Signature(model);
                    if (!model.IsSolved()) return false;

                    for (int f = 0; f < faces.Length; f++)
                    {
                        for (int layer = 1; layer <= size; layer++)
                        {
                            model.Reset(size);
                            model.Turn(faces[f], layer, true);
                            model.Turn(faces[f], layer, false);
                            if (Signature(model) != solved) return false;

                            model.Reset(size);
                            for (int i = 0; i < 4; i++)
                            {
                                model.Turn(faces[f], layer, true);
                            }

                            if (Signature(model) != solved) return false;

                            if (layer < size)
                            {
                                model.Reset(size);
                                model.TurnWide(faces[f], layer, 2, true);
                                model.TurnWide(faces[f], layer, 2, false);
                                if (Signature(model) != solved) return false;
                            }
                        }

                        model.Reset(size);
                        model.Turn(faces[f], 1, true);
                        if (model.IsSolved()) return false;

                        model.Reset(size);
                        model.TurnWide(faces[f], 1, size, true);
                        if (!model.IsSolved()) return false;
                    }
                }
            }
            catch
            {
                return false;
            }

            return true;
        }

        private static string Signature(CubeModel model)
        {
            StringBuilder sb = new StringBuilder();
            foreach (Cubie cubie in model.Cubies)
            {
                sb.Append(cubie.X).Append(',').Append(cubie.Y).Append(',').Append(cubie.Z).Append(':');
                for (int i = 0; i < cubie.Stickers.Length; i++)
                {
                    if (cubie.Stickers[i].HasValue)
                    {
                        sb.Append(cubie.Stickers[i].Value.ToArgb().ToString("X8"));
                    }
                    else
                    {
                        sb.Append('.');
                    }

                    sb.Append(';');
                }
            }

            return sb.ToString();
        }
    }

    internal enum Face
    {
        Right = 0,
        Left = 1,
        Up = 2,
        Down = 3,
        Front = 4,
        Back = 5
    }

    internal enum Axis
    {
        X,
        Y,
        Z
    }

    internal struct Vec3
    {
        public float X;
        public float Y;
        public float Z;

        public Vec3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static Vec3 operator +(Vec3 a, Vec3 b)
        {
            return new Vec3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        }

        public static Vec3 operator -(Vec3 a, Vec3 b)
        {
            return new Vec3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        }

        public static Vec3 operator *(Vec3 a, float scale)
        {
            return new Vec3(a.X * scale, a.Y * scale, a.Z * scale);
        }
    }

    internal sealed class Cubie
    {
        public int X;
        public int Y;
        public int Z;
        public Color?[] Stickers;

        public Cubie(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
            Stickers = new Color?[6];
        }
    }

    internal sealed class CubeModel
    {
        private Cubie[,,] cubies;

        public int Size { get; private set; }

        public CubeModel(int size)
        {
            Reset(size);
        }

        public IEnumerable<Cubie> Cubies
        {
            get
            {
                for (int x = 0; x < Size; x++)
                {
                    for (int y = 0; y < Size; y++)
                    {
                        for (int z = 0; z < Size; z++)
                        {
                            yield return cubies[x, y, z];
                        }
                    }
                }
            }
        }

        public void Reset(int size)
        {
            Size = Math.Max(2, Math.Min(7, size));
            cubies = new Cubie[Size, Size, Size];

            for (int x = 0; x < Size; x++)
            {
                for (int y = 0; y < Size; y++)
                {
                    for (int z = 0; z < Size; z++)
                    {
                        Cubie cubie = new Cubie(x, y, z);
                        if (x == Size - 1) cubie.Stickers[(int)Face.Right] = ExpectedColor(Face.Right);
                        if (x == 0) cubie.Stickers[(int)Face.Left] = ExpectedColor(Face.Left);
                        if (y == Size - 1) cubie.Stickers[(int)Face.Up] = ExpectedColor(Face.Up);
                        if (y == 0) cubie.Stickers[(int)Face.Down] = ExpectedColor(Face.Down);
                        if (z == Size - 1) cubie.Stickers[(int)Face.Front] = ExpectedColor(Face.Front);
                        if (z == 0) cubie.Stickers[(int)Face.Back] = ExpectedColor(Face.Back);
                        cubies[x, y, z] = cubie;
                    }
                }
            }
        }

        public void Turn(Face face, int layerFromFace, bool clockwise)
        {
            layerFromFace = Math.Max(1, Math.Min(Size, layerFromFace));

            Axis axis;
            int faceSign;
            int layerCoordinate;
            GetTurnData(face, layerFromFace, Size, out axis, out faceSign, out layerCoordinate);

            int direction = clockwise ? -faceSign : faceSign;
            Cubie[,,] next = new Cubie[Size, Size, Size];

            for (int x = 0; x < Size; x++)
            {
                for (int y = 0; y < Size; y++)
                {
                    for (int z = 0; z < Size; z++)
                    {
                        Cubie cubie = cubies[x, y, z];
                        if (!IsInLayer(cubie, axis, layerCoordinate))
                        {
                            next[x, y, z] = cubie;
                            continue;
                        }

                        int nx = x;
                        int ny = y;
                        int nz = z;
                        RotatePosition(ref nx, ref ny, ref nz, axis, direction, Size);
                        cubie.X = nx;
                        cubie.Y = ny;
                        cubie.Z = nz;
                        cubie.Stickers = RotateStickers(cubie.Stickers, axis, direction);
                        next[nx, ny, nz] = cubie;
                    }
                }
            }

            cubies = next;
        }

        public void TurnWide(Face face, int layerFromFace, int width, bool clockwise)
        {
            layerFromFace = Math.Max(1, Math.Min(Size, layerFromFace));
            width = Math.Max(1, Math.Min(width, Size - layerFromFace + 1));

            for (int i = 0; i < width; i++)
            {
                Turn(face, layerFromFace + i, clockwise);
            }
        }

        public bool IsSolved()
        {
            Color?[] faceColors = new Color?[6];
            Face[] faces = new Face[] { Face.Right, Face.Left, Face.Up, Face.Down, Face.Front, Face.Back };

            for (int i = 0; i < faces.Length; i++)
            {
                Face face = faces[i];
                Color? faceColor = null;

                foreach (Cubie cubie in Cubies)
                {
                    if (!IsOnFace(cubie, face, Size)) continue;

                    Color? sticker = cubie.Stickers[(int)face];
                    if (!sticker.HasValue || !IsCubeColor(sticker.Value)) return false;

                    if (!faceColor.HasValue)
                    {
                        faceColor = sticker.Value;
                    }
                    else if (faceColor.Value.ToArgb() != sticker.Value.ToArgb())
                    {
                        return false;
                    }
                }

                if (!faceColor.HasValue) return false;
                faceColors[i] = faceColor;
            }

            return HasEachCubeColorOnce(faceColors);
        }

        private static bool IsOnFace(Cubie cubie, Face face, int size)
        {
            if (face == Face.Right) return cubie.X == size - 1;
            if (face == Face.Left) return cubie.X == 0;
            if (face == Face.Up) return cubie.Y == size - 1;
            if (face == Face.Down) return cubie.Y == 0;
            if (face == Face.Front) return cubie.Z == size - 1;
            return cubie.Z == 0;
        }

        private static bool HasEachCubeColorOnce(Color?[] colors)
        {
            bool[] seen = new bool[6];
            for (int i = 0; i < colors.Length; i++)
            {
                if (!colors[i].HasValue) return false;

                int colorIndex = CubeColorIndex(colors[i].Value);
                if (colorIndex < 0 || seen[colorIndex]) return false;
                seen[colorIndex] = true;
            }

            for (int i = 0; i < seen.Length; i++)
            {
                if (!seen[i]) return false;
            }

            return true;
        }

        private static bool IsCubeColor(Color color)
        {
            return CubeColorIndex(color) >= 0;
        }

        private static int CubeColorIndex(Color color)
        {
            Face[] faces = new Face[] { Face.Right, Face.Left, Face.Up, Face.Down, Face.Front, Face.Back };
            for (int i = 0; i < faces.Length; i++)
            {
                if (color.ToArgb() == ExpectedColor(faces[i]).ToArgb()) return i;
            }

            return -1;
        }

        private static Color ExpectedColor(Face face)
        {
            if (face == Face.Right) return Color.FromArgb(239, 91, 91);
            if (face == Face.Left) return Color.FromArgb(255, 168, 76);
            if (face == Face.Up) return Color.FromArgb(246, 248, 252);
            if (face == Face.Down) return Color.FromArgb(255, 226, 89);
            if (face == Face.Front) return Color.FromArgb(84, 198, 132);
            return Color.FromArgb(93, 156, 236);
        }

        public static void GetTurnData(Face face, int layerFromFace, int size, out Axis axis, out int faceSign, out int layerCoordinate)
        {
            switch (face)
            {
                case Face.Right:
                    axis = Axis.X;
                    faceSign = 1;
                    layerCoordinate = size - layerFromFace;
                    break;
                case Face.Left:
                    axis = Axis.X;
                    faceSign = -1;
                    layerCoordinate = layerFromFace - 1;
                    break;
                case Face.Up:
                    axis = Axis.Y;
                    faceSign = 1;
                    layerCoordinate = size - layerFromFace;
                    break;
                case Face.Down:
                    axis = Axis.Y;
                    faceSign = -1;
                    layerCoordinate = layerFromFace - 1;
                    break;
                case Face.Front:
                    axis = Axis.Z;
                    faceSign = 1;
                    layerCoordinate = size - layerFromFace;
                    break;
                default:
                    axis = Axis.Z;
                    faceSign = -1;
                    layerCoordinate = layerFromFace - 1;
                    break;
            }
        }

        public static bool IsInLayer(Cubie cubie, Axis axis, int coordinate)
        {
            if (axis == Axis.X) return cubie.X == coordinate;
            if (axis == Axis.Y) return cubie.Y == coordinate;
            return cubie.Z == coordinate;
        }

        private static void RotatePosition(ref int x, ref int y, ref int z, Axis axis, int direction, int size)
        {
            int oldX = x;
            int oldY = y;
            int oldZ = z;

            if (axis == Axis.X)
            {
                if (direction > 0)
                {
                    y = size - 1 - oldZ;
                    z = oldY;
                }
                else
                {
                    y = oldZ;
                    z = size - 1 - oldY;
                }
            }
            else if (axis == Axis.Y)
            {
                if (direction > 0)
                {
                    x = oldZ;
                    z = size - 1 - oldX;
                }
                else
                {
                    x = size - 1 - oldZ;
                    z = oldX;
                }
            }
            else
            {
                if (direction > 0)
                {
                    x = size - 1 - oldY;
                    y = oldX;
                }
                else
                {
                    x = oldY;
                    y = size - 1 - oldX;
                }
            }
        }

        private static Color?[] RotateStickers(Color?[] stickers, Axis axis, int direction)
        {
            Color?[] rotated = new Color?[6];
            for (int i = 0; i < stickers.Length; i++)
            {
                if (!stickers[i].HasValue) continue;
                Vec3 normal = FaceNormal((Face)i);
                Vec3 newNormal = RotateNormal(normal, axis, direction);
                Face newFace = NormalToFace(newNormal);
                rotated[(int)newFace] = stickers[i];
            }

            return rotated;
        }

        public static Vec3 FaceNormal(Face face)
        {
            switch (face)
            {
                case Face.Right: return new Vec3(1, 0, 0);
                case Face.Left: return new Vec3(-1, 0, 0);
                case Face.Up: return new Vec3(0, 1, 0);
                case Face.Down: return new Vec3(0, -1, 0);
                case Face.Front: return new Vec3(0, 0, 1);
                default: return new Vec3(0, 0, -1);
            }
        }

        private static Vec3 RotateNormal(Vec3 n, Axis axis, int direction)
        {
            if (axis == Axis.X)
            {
                return direction > 0 ? new Vec3(n.X, -n.Z, n.Y) : new Vec3(n.X, n.Z, -n.Y);
            }

            if (axis == Axis.Y)
            {
                return direction > 0 ? new Vec3(n.Z, n.Y, -n.X) : new Vec3(-n.Z, n.Y, n.X);
            }

            return direction > 0 ? new Vec3(-n.Y, n.X, n.Z) : new Vec3(n.Y, -n.X, n.Z);
        }

        private static Face NormalToFace(Vec3 n)
        {
            if (n.X > 0.5f) return Face.Right;
            if (n.X < -0.5f) return Face.Left;
            if (n.Y > 0.5f) return Face.Up;
            if (n.Y < -0.5f) return Face.Down;
            if (n.Z > 0.5f) return Face.Front;
            return Face.Back;
        }
    }

    internal sealed class FacePoly
    {
        public PointF[] Outer;
        public PointF[] Inner;
        public float Depth;
        public Color StickerColor;
        public float Light;
        public Cubie Cubie;
        public Face Face;
        public Vec3 Center;
        public bool HasSticker;
    }

    internal sealed class HitInfo
    {
        public Cubie Cubie;
        public Face Face;
        public Vec3 Center;
    }

    internal struct TurnMove
    {
        public bool IsValid;
        public Face Face;
        public int Layer;
        public bool Clockwise;
        public int Width;
        public int Turns;
        public Axis Axis;
        public int LayerCoordinate;
        public int Direction;
    }

    internal sealed class CubeView : Control
    {
        private const float Spacing = 1.0f;
        private const float Half = 0.5f;
        private const int DragThreshold = 14;
        private const int AnimationMs = 185;
        private readonly ToolTip tooltip;
        private readonly Timer animationTimer;
        private Point lastMouse;
        private Point turnStart;
        private Point turnCurrent;
        private bool orbitDragging;
        private bool turnDragging;
        private int dragTurnCount;
        private HitInfo turnHit;
        private TurnMove previewMove;
        private bool animating;
        private int animationStartTick;
        private float animationProgress;
        private Face animationFace;
        private int animationLayer;
        private int animationWidth;
        private int animationTurns;
        private bool animationClockwise;
        private Axis animationAxis;
        private int animationLayerCoordinate;
        private int animationDirection;
        private string centerMessage;
        private float yaw;
        private float pitch;
        private float zoom;

        public CubeModel Model { get; set; }
        public event Action<Face, int, int, int, bool> TurnCompleted;

        public CubeView()
        {
            yaw = -0.58f;
            pitch = -0.44f;
            zoom = 0.95f;
            BackColor = Color.FromArgb(12, 14, 20);
            ForeColor = Color.White;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            tooltip = new ToolTip();
            tooltip.SetToolTip(this, "Drag a sticker to turn. Drag empty space to orbit. Mouse wheel zooms.");
            animationTimer = new Timer();
            animationTimer.Interval = 15;
            animationTimer.Tick += delegate { UpdateAnimation(); };
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left && e.Button != MouseButtons.Right) return;
            if (animating) return;

            if (!string.IsNullOrEmpty(centerMessage))
            {
                if (!GetCenterMessageRect().Contains(e.Location))
                {
                    ClearCenterMessage();
                }

                return;
            }

            if (e.Button == MouseButtons.Left || e.Button == MouseButtons.Right)
            {
                HitInfo hit = HitTest(e.Location);
                if (hit != null)
                {
                    turnDragging = true;
                    dragTurnCount = e.Button == MouseButtons.Right ? 2 : 1;
                    turnHit = hit;
                    turnStart = e.Location;
                    turnCurrent = e.Location;
                    previewMove = new TurnMove();
                    Cursor = Cursors.Hand;
                    Capture = true;
                    return;
                }
            }

            orbitDragging = true;
            lastMouse = e.Location;
            Cursor = Cursors.SizeAll;
            Capture = true;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (turnDragging)
            {
                turnCurrent = e.Location;
                previewMove = BuildPreviewMove();
                Invalidate();
                return;
            }

            if (!orbitDragging) return;
            int dx = e.X - lastMouse.X;
            int dy = e.Y - lastMouse.Y;
            yaw += dx * 0.01f;
            pitch += dy * 0.01f;
            pitch = Math.Max(-1.35f, Math.Min(1.35f, pitch));
            lastMouse = e.Location;
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (turnDragging)
            {
                turnCurrent = e.Location;
                previewMove = BuildPreviewMove();
                TurnMove move = previewMove;
                turnDragging = false;
                dragTurnCount = 1;
                turnHit = null;
                previewMove = new TurnMove();
                Capture = false;
                Cursor = Cursors.Default;

                if (move.IsValid)
                {
                    AnimateTurn(move.Face, move.Layer, move.Width, move.Turns, move.Clockwise);
                }

                Invalidate();
                return;
            }

            orbitDragging = false;
            Capture = false;
            Cursor = Cursors.Default;
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            zoom += e.Delta > 0 ? 0.08f : -0.08f;
            zoom = Math.Max(0.28f, Math.Min(1.8f, zoom));
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(BackColor);

            using (LinearGradientBrush bg = new LinearGradientBrush(ClientRectangle, Color.FromArgb(15, 18, 26), Color.FromArgb(23, 27, 38), 55f))
            {
                g.FillRectangle(bg, ClientRectangle);
            }

            if (Model == null) return;

            List<FacePoly> polygons = BuildPolygons();
            polygons.Sort(delegate(FacePoly a, FacePoly b) { return a.Depth.CompareTo(b.Depth); });

            DrawShadow(g);

            using (Pen edgePen = new Pen(Color.FromArgb(165, 5, 7, 11), 1.1f))
            {
                for (int i = 0; i < polygons.Count; i++)
                {
                    FacePoly poly = polygons[i];
                    Color body = ApplyLight(Color.FromArgb(42, 47, 59), poly.Light);
                    using (SolidBrush plastic = new SolidBrush(body))
                    {
                        g.FillPolygon(plastic, poly.Outer);
                    }

                    g.DrawPolygon(edgePen, poly.Outer);

                    if (poly.HasSticker)
                    {
                        Color lit = ApplyLight(poly.StickerColor, poly.Light);
                        using (SolidBrush sticker = new SolidBrush(lit))
                        {
                            g.FillPolygon(sticker, poly.Inner);
                        }

                        using (Pen stickerEdge = new Pen(Color.FromArgb(115, 0, 0, 0), 1.0f))
                        {
                            g.DrawPolygon(stickerEdge, poly.Inner);
                        }
                    }
                }
            }

            DrawTurnPreview(g);
            DrawCenterMessage(g);
        }

        public bool AnimateTurn(Face face, int layer, bool clockwise)
        {
            return AnimateTurn(face, layer, 1, 1, clockwise);
        }

        public bool AnimateTurn(Face face, int layer, int width, bool clockwise)
        {
            return AnimateTurn(face, layer, width, 1, clockwise);
        }

        public bool AnimateTurn(Face face, int layer, int width, int turns, bool clockwise)
        {
            if (Model == null || animating) return false;

            layer = Math.Max(1, Math.Min(Model.Size, layer));
            width = Math.Max(1, Math.Min(width, Model.Size - layer + 1));
            turns = Math.Max(1, Math.Min(2, turns));
            int faceSign;
            CubeModel.GetTurnData(face, layer, Model.Size, out animationAxis, out faceSign, out animationLayerCoordinate);
            animationDirection = clockwise ? -faceSign : faceSign;
            animationFace = face;
            animationLayer = layer;
            animationWidth = width;
            animationTurns = turns;
            animationClockwise = clockwise;
            animationProgress = 0f;
            animationStartTick = Environment.TickCount;
            animating = true;
            centerMessage = null;
            animationTimer.Start();
            Invalidate();
            return true;
        }

        public void CancelAnimation()
        {
            animationTimer.Stop();
            animating = false;
            animationProgress = 0f;
            turnDragging = false;
            orbitDragging = false;
            dragTurnCount = 1;
            previewMove = new TurnMove();
            Invalidate();
        }

        public void ShowCenterMessage(string message)
        {
            centerMessage = message;
            Invalidate();
        }

        public void ClearCenterMessage()
        {
            centerMessage = null;
            Invalidate();
        }

        private void DrawShadow(Graphics g)
        {
            Rectangle shadow = new Rectangle(ClientSize.Width / 2 - 155, ClientSize.Height / 2 + 130, 310, 48);
            if (shadow.Width <= 0 || shadow.Height <= 0) return;
            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddEllipse(shadow);
                using (PathGradientBrush brush = new PathGradientBrush(path))
                {
                    brush.CenterColor = Color.FromArgb(80, 0, 0, 0);
                    brush.SurroundColors = new Color[] { Color.FromArgb(0, 0, 0, 0) };
                    g.FillPath(brush, path);
                }
            }
        }

        private List<FacePoly> BuildPolygons()
        {
            List<FacePoly> result = new List<FacePoly>();
            float scale = CurrentScale();

            foreach (Cubie cubie in Model.Cubies)
            {
                AddFaceIfVisible(result, cubie, Face.Right, cubie.X == Model.Size - 1, scale);
                AddFaceIfVisible(result, cubie, Face.Left, cubie.X == 0, scale);
                AddFaceIfVisible(result, cubie, Face.Up, cubie.Y == Model.Size - 1, scale);
                AddFaceIfVisible(result, cubie, Face.Down, cubie.Y == 0, scale);
                AddFaceIfVisible(result, cubie, Face.Front, cubie.Z == Model.Size - 1, scale);
                AddFaceIfVisible(result, cubie, Face.Back, cubie.Z == 0, scale);
            }

            return result;
        }

        private void AddFaceIfVisible(List<FacePoly> result, Cubie cubie, Face face, bool onBoundary, float scale)
        {
            Vec3[] corners = GetFaceCorners(cubie, face);
            Vec3 normal = CubeModel.FaceNormal(face);
            if (IsAnimatingCubie(cubie))
            {
                float angle = CurrentAnimationAngle();
                for (int i = 0; i < corners.Length; i++)
                {
                    corners[i] = RotateAroundAxis(corners[i], animationAxis, angle);
                }

                normal = RotateAroundAxis(normal, animationAxis, angle);
            }

            Vec3 viewNormal = RotateView(normal);
            if (viewNormal.Z <= 0.02f) return;

            Vec3 center = Average(corners);
            Vec3[] inner = new Vec3[4];
            for (int i = 0; i < corners.Length; i++)
            {
                inner[i] = center + (corners[i] - center) * 0.78f;
            }

            FacePoly poly = new FacePoly();
            poly.Outer = Project(corners, scale);
            poly.Inner = Project(inner, scale);
            poly.Depth = AverageZ(corners);
            poly.Light = 0.68f + viewNormal.Z * 0.32f;
            Color? stickerColor = onBoundary ? cubie.Stickers[(int)face] : null;
            poly.StickerColor = stickerColor.HasValue ? stickerColor.Value : Color.FromArgb(28, 32, 41);
            poly.Cubie = cubie;
            poly.Face = face;
            poly.Center = center;
            poly.HasSticker = stickerColor.HasValue;
            result.Add(poly);
        }

        private Vec3[] GetFaceCorners(Cubie cubie, Face face)
        {
            float offset = (Model.Size - 1) * 0.5f;
            float x = (cubie.X - offset) * Spacing;
            float y = (cubie.Y - offset) * Spacing;
            float z = (cubie.Z - offset) * Spacing;

            if (face == Face.Right)
            {
                return new Vec3[] {
                    new Vec3(x + Half, y - Half, z - Half),
                    new Vec3(x + Half, y + Half, z - Half),
                    new Vec3(x + Half, y + Half, z + Half),
                    new Vec3(x + Half, y - Half, z + Half)
                };
            }

            if (face == Face.Left)
            {
                return new Vec3[] {
                    new Vec3(x - Half, y - Half, z + Half),
                    new Vec3(x - Half, y + Half, z + Half),
                    new Vec3(x - Half, y + Half, z - Half),
                    new Vec3(x - Half, y - Half, z - Half)
                };
            }

            if (face == Face.Up)
            {
                return new Vec3[] {
                    new Vec3(x - Half, y + Half, z - Half),
                    new Vec3(x - Half, y + Half, z + Half),
                    new Vec3(x + Half, y + Half, z + Half),
                    new Vec3(x + Half, y + Half, z - Half)
                };
            }

            if (face == Face.Down)
            {
                return new Vec3[] {
                    new Vec3(x - Half, y - Half, z + Half),
                    new Vec3(x - Half, y - Half, z - Half),
                    new Vec3(x + Half, y - Half, z - Half),
                    new Vec3(x + Half, y - Half, z + Half)
                };
            }

            if (face == Face.Front)
            {
                return new Vec3[] {
                    new Vec3(x - Half, y - Half, z + Half),
                    new Vec3(x + Half, y - Half, z + Half),
                    new Vec3(x + Half, y + Half, z + Half),
                    new Vec3(x - Half, y + Half, z + Half)
                };
            }

            return new Vec3[] {
                new Vec3(x + Half, y - Half, z - Half),
                new Vec3(x - Half, y - Half, z - Half),
                new Vec3(x - Half, y + Half, z - Half),
                new Vec3(x + Half, y + Half, z - Half)
            };
        }

        private PointF[] Project(Vec3[] points, float scale)
        {
            PointF[] projected = new PointF[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                projected[i] = ProjectPoint(points[i], scale);
            }

            return projected;
        }

        private PointF ProjectPoint(Vec3 point, float scale)
        {
            Vec3 v = RotateView(point);
            float cameraDistance = 7.4f + Model.Size * 1.15f;
            float depth = cameraDistance - v.Z;
            if (depth < 1f) depth = 1f;
            float perspective = cameraDistance / depth;
            return new PointF(ClientSize.Width * 0.5f + v.X * scale * perspective, ClientSize.Height * 0.5f - v.Y * scale * perspective);
        }

        private float AverageZ(Vec3[] points)
        {
            float sum = 0f;
            for (int i = 0; i < points.Length; i++)
            {
                sum += RotateView(points[i]).Z;
            }

            return sum / points.Length;
        }

        private Vec3 RotateView(Vec3 v)
        {
            float cy = (float)Math.Cos(yaw);
            float sy = (float)Math.Sin(yaw);
            float cp = (float)Math.Cos(pitch);
            float sp = (float)Math.Sin(pitch);

            float x1 = v.X * cy + v.Z * sy;
            float z1 = -v.X * sy + v.Z * cy;
            float y2 = v.Y * cp - z1 * sp;
            float z2 = v.Y * sp + z1 * cp;
            return new Vec3(x1, y2, z2);
        }

        private float CurrentScale()
        {
            float scale = Math.Min(ClientSize.Width, ClientSize.Height) / (Model.Size * 1.38f) * zoom;
            if (scale < 1f) scale = 1f;
            return scale;
        }

        private void UpdateAnimation()
        {
            int elapsed = Environment.TickCount - animationStartTick;
            float t = elapsed / (float)AnimationMs;
            if (t >= 1f)
            {
                animationTimer.Stop();
                animating = false;
                animationProgress = 0f;
                for (int i = 0; i < animationTurns; i++)
                {
                    Model.TurnWide(animationFace, animationLayer, animationWidth, animationClockwise);
                }

                Invalidate();

                if (TurnCompleted != null)
                {
                    TurnCompleted(animationFace, animationLayer, animationWidth, animationTurns, animationClockwise);
                }

                return;
            }

            animationProgress = Ease(t);
            Invalidate();
        }

        private float CurrentAnimationAngle()
        {
            return animationDirection * animationProgress * (float)(Math.PI * 0.5 * animationTurns);
        }

        private bool IsAnimatingCubie(Cubie cubie)
        {
            if (!animating) return false;

            for (int i = 0; i < animationWidth; i++)
            {
                Axis axis;
                int faceSign;
                int layerCoordinate;
                CubeModel.GetTurnData(animationFace, animationLayer + i, Model.Size, out axis, out faceSign, out layerCoordinate);
                if (CubeModel.IsInLayer(cubie, animationAxis, layerCoordinate)) return true;
            }

            return false;
        }

        private HitInfo HitTest(Point point)
        {
            if (Model == null || animating) return null;

            List<FacePoly> polygons = BuildPolygons();
            polygons.Sort(delegate(FacePoly a, FacePoly b) { return a.Depth.CompareTo(b.Depth); });

            for (int i = polygons.Count - 1; i >= 0; i--)
            {
                FacePoly poly = polygons[i];
                if (!poly.HasSticker) continue;
                if (!PointInPolygon(point, poly.Outer)) continue;

                HitInfo hit = new HitInfo();
                hit.Cubie = poly.Cubie;
                hit.Face = poly.Face;
                hit.Center = poly.Center;
                return hit;
            }

            return null;
        }

        private TurnMove BuildPreviewMove()
        {
            TurnMove move = new TurnMove();
            if (turnHit == null || Model == null) return move;

            PointF drag = new PointF(turnCurrent.X - turnStart.X, turnCurrent.Y - turnStart.Y);
            float dragLength = Length(drag);
            if (dragLength < DragThreshold) return move;

            PointF dragNormal = new PointF(drag.X / dragLength, drag.Y / dragLength);
            Vec3 faceNormal = CubeModel.FaceNormal(turnHit.Face);
            TurnMove faceTwist = TryBuildFaceTwistMove(faceNormal, drag, dragLength);
            if (faceTwist.IsValid) return faceTwist;

            Axis faceAxis = AxisFromVector(faceNormal);
            Axis bestTangentAxis = Axis.X;
            int bestTangentSign = 1;
            float bestScore = -1f;
            float scale = CurrentScale();
            PointF center = ProjectPoint(turnHit.Center, scale);

            Axis[] axes = new Axis[] { Axis.X, Axis.Y, Axis.Z };
            for (int i = 0; i < axes.Length; i++)
            {
                Axis axis = axes[i];
                if (axis == faceAxis) continue;

                Vec3 tangent = AxisVector(axis, 1);
                PointF projected = ProjectPoint(turnHit.Center + tangent * 0.8f, scale);
                PointF screen = new PointF(projected.X - center.X, projected.Y - center.Y);
                float screenLength = Length(screen);
                if (screenLength < 0.1f) continue;

                PointF screenNormal = new PointF(screen.X / screenLength, screen.Y / screenLength);
                float dot = Dot(screenNormal, dragNormal);
                float score = Math.Abs(dot);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestTangentAxis = axis;
                    bestTangentSign = dot >= 0f ? 1 : -1;
                }
            }

            if (bestScore < 0.35f) return move;

            Vec3 signedTangent = AxisVector(bestTangentAxis, bestTangentSign);
            Vec3 signedTurnAxis = Cross(faceNormal, signedTangent);
            Axis turnAxis = AxisFromVector(signedTurnAxis);
            int direction = SignOnAxis(signedTurnAxis, turnAxis);
            int coordinate = CoordinateFor(turnHit.Cubie, turnAxis);

            return MoveFromAxis(turnAxis, coordinate, direction);
        }

        private TurnMove MoveFromAxis(Axis axis, int coordinate, int direction)
        {
            int faceSign = coordinate >= Model.Size / 2 ? 1 : -1;
            Face face = FaceForAxis(axis, faceSign);
            int layer = faceSign > 0 ? Model.Size - coordinate : coordinate + 1;
            int width = WideWidthFor(layer);

            TurnMove move = new TurnMove();
            move.IsValid = true;
            move.Face = face;
            move.Layer = layer;
            move.Clockwise = direction == -faceSign;
            move.Width = width;
            move.Turns = dragTurnCount == 2 ? 2 : 1;
            move.Axis = axis;
            move.LayerCoordinate = coordinate;
            move.Direction = direction;
            return move;
        }

        private TurnMove TryBuildFaceTwistMove(Vec3 faceNormal, PointF drag, float dragLength)
        {
            TurnMove move = new TurnMove();
            if (turnHit.Face != Face.Front && turnHit.Face != Face.Back && turnHit.Face != Face.Left && turnHit.Face != Face.Right) return move;

            float ax = Math.Abs(drag.X);
            float ay = Math.Abs(drag.Y);
            if (ax < DragThreshold * 0.8f || ay < DragThreshold * 0.8f) return move;

            float ratio = ax > ay ? ax / ay : ay / ax;
            if (ratio > 2.15f) return move;

            float scale = CurrentScale();
            Axis faceAxis = AxisFromVector(faceNormal);
            int faceSign = SignOnAxis(faceNormal, faceAxis);
            float offset = (Model.Size - 1) * 0.5f * Spacing + Half;
            Vec3 faceCenter = AxisVector(faceAxis, faceSign) * offset;
            PointF faceCenterScreen = ProjectPoint(faceCenter, scale);
            PointF radial = new PointF(turnStart.X - faceCenterScreen.X, turnStart.Y - faceCenterScreen.Y);
            float cross = radial.X * drag.Y - radial.Y * drag.X;

            if (Length(radial) < 12f)
            {
                cross = drag.X * drag.Y;
            }

            bool clockwise = cross > 0f;
            move.IsValid = true;
            move.Face = turnHit.Face;
            move.Layer = 1;
            move.Width = WideWidthFor(1);
            move.Turns = dragTurnCount == 2 ? 2 : 1;
            move.Clockwise = clockwise;
            move.Axis = faceAxis;
            move.LayerCoordinate = faceSign > 0 ? Model.Size - 1 : 0;
            move.Direction = clockwise ? -faceSign : faceSign;
            return move;
        }

        private int WideWidthFor(int layer)
        {
            bool wide = (ModifierKeys & Keys.Shift) == Keys.Shift;
            if (!wide) return 1;
            return Math.Max(1, Math.Min(2, Model.Size - layer + 1));
        }

        private void DrawTurnPreview(Graphics g)
        {
            if (!turnDragging || !previewMove.IsValid) return;

            PointF start = new PointF(turnStart.X, turnStart.Y);
            PointF end = new PointF(turnCurrent.X, turnCurrent.Y);
            if (Length(new PointF(end.X - start.X, end.Y - start.Y)) < DragThreshold) return;

            using (Pen arrow = new Pen(Color.White, 4.5f))
            {
                arrow.StartCap = LineCap.Round;
                arrow.EndCap = LineCap.Custom;
                arrow.CustomEndCap = new AdjustableArrowCap(5.5f, 7.5f, true);
                g.DrawLine(arrow, start, end);
            }

            string text = "Release: " + MoveText(previewMove) + (previewMove.Width > 1 ? " wide turn" : " turn");
            using (Font font = new Font("Segoe UI Semibold", 11f, FontStyle.Bold, GraphicsUnit.Point))
            {
                SizeF size = g.MeasureString(text, font);
                float x = Math.Max(10f, Math.Min(ClientSize.Width - size.Width - 22f, end.X + 12f));
                float y = Math.Max(10f, Math.Min(ClientSize.Height - size.Height - 16f, end.Y - size.Height - 10f));
                RectangleF rect = new RectangleF(x - 7f, y - 4f, size.Width + 14f, size.Height + 8f);

                using (SolidBrush bg = new SolidBrush(Color.FromArgb(175, 10, 13, 20)))
                using (SolidBrush fg = new SolidBrush(Color.White))
                {
                    g.FillRectangle(bg, rect);
                    g.DrawString(text, font, fg, x, y);
                }
            }
        }

        private void DrawCenterMessage(Graphics g)
        {
            if (string.IsNullOrEmpty(centerMessage)) return;

            using (Font titleFont = new Font("Segoe UI Semibold", 28f, FontStyle.Bold, GraphicsUnit.Point))
            using (Font subFont = new Font("Segoe UI", 11f, FontStyle.Regular, GraphicsUnit.Point))
            {
                string title = "Solved";
                SizeF titleSize = g.MeasureString(title, titleFont);
                SizeF subSize = g.MeasureString(centerMessage, subFont);
                RectangleF rect = GetCenterMessageRect();

                using (SolidBrush bg = new SolidBrush(Color.FromArgb(205, 9, 12, 18)))
                using (Pen border = new Pen(Color.FromArgb(145, 255, 255, 255), 1f))
                using (SolidBrush titleBrush = new SolidBrush(Color.White))
                using (SolidBrush subBrush = new SolidBrush(Color.FromArgb(198, 208, 225)))
                {
                    g.FillRectangle(bg, rect);
                    g.DrawRectangle(border, rect.X, rect.Y, rect.Width, rect.Height);
                    g.DrawString(title, titleFont, titleBrush, rect.X + (rect.Width - titleSize.Width) * 0.5f, rect.Y + 10f);
                    g.DrawString(centerMessage, subFont, subBrush, rect.X + (rect.Width - subSize.Width) * 0.5f, rect.Y + titleSize.Height + 12f);
                }
            }
        }

        private RectangleF GetCenterMessageRect()
        {
            if (string.IsNullOrEmpty(centerMessage)) return RectangleF.Empty;

            using (Font titleFont = new Font("Segoe UI Semibold", 28f, FontStyle.Bold, GraphicsUnit.Point))
            using (Font subFont = new Font("Segoe UI", 11f, FontStyle.Regular, GraphicsUnit.Point))
            {
                Size titleSize = TextRenderer.MeasureText("Solved", titleFont, Size.Empty, TextFormatFlags.NoPadding);
                Size subSize = TextRenderer.MeasureText(centerMessage, subFont, Size.Empty, TextFormatFlags.NoPadding);
                float width = Math.Max(titleSize.Width, subSize.Width) + 48f;
                float height = titleSize.Height + subSize.Height + 28f;
                return new RectangleF((ClientSize.Width - width) * 0.5f, (ClientSize.Height - height) * 0.5f, width, height);
            }
        }

        private static string MoveText(TurnMove move)
        {
            string text = FaceName(move.Face);
            if (move.Width > 1) text += "w";
            if (move.Turns == 2) text += "2";
            else if (!move.Clockwise) text += "'";
            if (move.Layer > 1) text += " layer " + move.Layer;
            return text;
        }

        private static string FaceName(Face face)
        {
            if (face == Face.Right) return "R";
            if (face == Face.Left) return "L";
            if (face == Face.Down) return "D";
            if (face == Face.Front) return "F";
            if (face == Face.Back) return "B";
            return "U";
        }

        private static bool PointInPolygon(Point point, PointF[] polygon)
        {
            bool inside = false;
            int j = polygon.Length - 1;
            for (int i = 0; i < polygon.Length; i++)
            {
                bool crosses = ((polygon[i].Y > point.Y) != (polygon[j].Y > point.Y)) &&
                    (point.X < (polygon[j].X - polygon[i].X) * (point.Y - polygon[i].Y) / (polygon[j].Y - polygon[i].Y) + polygon[i].X);
                if (crosses) inside = !inside;
                j = i;
            }

            return inside;
        }

        private static Vec3 RotateAroundAxis(Vec3 v, Axis axis, float angle)
        {
            float c = (float)Math.Cos(angle);
            float s = (float)Math.Sin(angle);

            if (axis == Axis.X)
            {
                return new Vec3(v.X, v.Y * c - v.Z * s, v.Y * s + v.Z * c);
            }

            if (axis == Axis.Y)
            {
                return new Vec3(v.X * c + v.Z * s, v.Y, -v.X * s + v.Z * c);
            }

            return new Vec3(v.X * c - v.Y * s, v.X * s + v.Y * c, v.Z);
        }

        private static Axis AxisFromVector(Vec3 v)
        {
            float ax = Math.Abs(v.X);
            float ay = Math.Abs(v.Y);
            float az = Math.Abs(v.Z);
            if (ax >= ay && ax >= az) return Axis.X;
            if (ay >= ax && ay >= az) return Axis.Y;
            return Axis.Z;
        }

        private static int SignOnAxis(Vec3 v, Axis axis)
        {
            float value = axis == Axis.X ? v.X : axis == Axis.Y ? v.Y : v.Z;
            return value >= 0f ? 1 : -1;
        }

        private static Vec3 AxisVector(Axis axis, int sign)
        {
            if (axis == Axis.X) return new Vec3(sign, 0, 0);
            if (axis == Axis.Y) return new Vec3(0, sign, 0);
            return new Vec3(0, 0, sign);
        }

        private static Vec3 Cross(Vec3 a, Vec3 b)
        {
            return new Vec3(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X);
        }

        private static int CoordinateFor(Cubie cubie, Axis axis)
        {
            if (axis == Axis.X) return cubie.X;
            if (axis == Axis.Y) return cubie.Y;
            return cubie.Z;
        }

        private static Face FaceForAxis(Axis axis, int sign)
        {
            if (axis == Axis.X) return sign > 0 ? Face.Right : Face.Left;
            if (axis == Axis.Y) return sign > 0 ? Face.Up : Face.Down;
            return sign > 0 ? Face.Front : Face.Back;
        }

        private static float Length(PointF p)
        {
            return (float)Math.Sqrt(p.X * p.X + p.Y * p.Y);
        }

        private static float Dot(PointF a, PointF b)
        {
            return a.X * b.X + a.Y * b.Y;
        }

        private static float Ease(float t)
        {
            if (t < 0f) t = 0f;
            if (t > 1f) t = 1f;
            return t * t * (3f - 2f * t);
        }

        private static Vec3 Average(Vec3[] points)
        {
            Vec3 sum = new Vec3(0, 0, 0);
            for (int i = 0; i < points.Length; i++)
            {
                sum += points[i];
            }

            return sum * (1f / points.Length);
        }

        private static Color ApplyLight(Color color, float light)
        {
            light = Math.Max(0.45f, Math.Min(1.15f, light));
            int r = Math.Max(0, Math.Min(255, (int)(color.R * light)));
            int g = Math.Max(0, Math.Min(255, (int)(color.G * light)));
            int b = Math.Max(0, Math.Min(255, (int)(color.B * light)));
            return Color.FromArgb(r, g, b);
        }
    }

    internal sealed class MainForm : Form
    {
        private readonly CubeModel model;
        private readonly CubeView view;
        private readonly ComboBox sizeBox;
        private readonly ComboBox faceBox;
        private readonly NumericUpDown layerInput;
        private readonly Label statusLabel;
        private readonly Random random;
        private int userTurnCount;
        private int lastScrambleTurns;

        public MainForm()
        {
            model = new CubeModel(3);
            random = new Random();

            Text = "VibeBetterCube";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(980, 660);
            Size = new Size(1180, 760);
            BackColor = Color.FromArgb(14, 16, 22);
            Font = new Font("Segoe UI", 10f, FontStyle.Regular, GraphicsUnit.Point);

            Panel shell = new Panel();
            shell.Dock = DockStyle.Fill;
            shell.BackColor = BackColor;
            Controls.Add(shell);

            Panel sidebar = new Panel();
            sidebar.Width = 292;
            sidebar.Dock = DockStyle.Left;
            sidebar.Padding = new Padding(22, 24, 22, 18);
            sidebar.BackColor = Color.FromArgb(20, 24, 33);
            shell.Controls.Add(sidebar);

            view = new CubeView();
            view.Model = model;
            view.Dock = DockStyle.Fill;
            shell.Controls.Add(view);
            view.BringToFront();

            FlowLayoutPanel controls = new FlowLayoutPanel();
            controls.Dock = DockStyle.Fill;
            controls.FlowDirection = FlowDirection.TopDown;
            controls.WrapContents = false;
            controls.AutoScroll = true;
            controls.BackColor = sidebar.BackColor;
            sidebar.Controls.Add(controls);

            Label title = new Label();
            title.Text = "VibeBetterCube";
            title.Font = new Font("Segoe UI Semibold", 18f, FontStyle.Bold, GraphicsUnit.Point);
            title.ForeColor = Color.FromArgb(238, 243, 255);
            title.Width = 236;
            title.Height = 36;
            title.Margin = new Padding(0, 0, 0, 4);
            controls.Controls.Add(title);

            Label subtitle = new Label();
            subtitle.Text = "NxNxN simulator";
            subtitle.ForeColor = Color.FromArgb(142, 151, 171);
            subtitle.Width = 236;
            subtitle.Height = 24;
            subtitle.Margin = new Padding(0, 0, 0, 22);
            controls.Controls.Add(subtitle);

            controls.Controls.Add(MakeLabel("Size"));
            sizeBox = new ComboBox();
            StyleCombo(sizeBox);
            for (int i = 2; i <= 7; i++) sizeBox.Items.Add(i);
            sizeBox.SelectedItem = 3;
            sizeBox.SelectedIndexChanged += delegate { ChangeSize(); };
            controls.Controls.Add(sizeBox);

            controls.Controls.Add(MakeSpacer(8));
            controls.Controls.Add(MakeLabel("Face"));
            faceBox = new ComboBox();
            StyleCombo(faceBox);
            faceBox.Items.AddRange(new object[] { "U", "D", "L", "R", "F", "B" });
            faceBox.SelectedIndex = 0;
            controls.Controls.Add(faceBox);

            controls.Controls.Add(MakeLabel("Layer"));
            layerInput = new NumericUpDown();
            layerInput.Minimum = 1;
            layerInput.Maximum = 3;
            layerInput.Value = 1;
            layerInput.Width = 236;
            layerInput.Height = 36;
            layerInput.BackColor = Color.FromArgb(29, 34, 46);
            layerInput.ForeColor = Color.FromArgb(238, 243, 255);
            layerInput.BorderStyle = BorderStyle.FixedSingle;
            layerInput.Margin = new Padding(0, 0, 0, 14);
            controls.Controls.Add(layerInput);

            TableLayoutPanel moveGrid = new TableLayoutPanel();
            moveGrid.Width = 236;
            moveGrid.Height = 46;
            moveGrid.ColumnCount = 2;
            moveGrid.RowCount = 1;
            moveGrid.Margin = new Padding(0, 0, 0, 14);
            moveGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            moveGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            Button cw = MakeButton("CW", Color.FromArgb(95, 214, 201));
            Button ccw = MakeButton("CCW", Color.FromArgb(123, 156, 255));
            cw.Click += delegate { ApplyMove(true); };
            ccw.Click += delegate { ApplyMove(false); };
            moveGrid.Controls.Add(cw, 0, 0);
            moveGrid.Controls.Add(ccw, 1, 0);
            controls.Controls.Add(moveGrid);

            Button scramble = MakeWideButton("Scramble", Color.FromArgb(255, 184, 108));
            scramble.Click += delegate { Scramble(); };
            controls.Controls.Add(scramble);

            Button reset = MakeWideButton("Reset", Color.FromArgb(47, 53, 67));
            reset.Click += delegate { ResetCube(); };
            controls.Controls.Add(reset);

            controls.Controls.Add(MakeSpacer(12));
            statusLabel = new Label();
            statusLabel.Text = "Ready";
            statusLabel.ForeColor = Color.FromArgb(142, 151, 171);
            statusLabel.Width = 236;
            statusLabel.Height = 76;
            statusLabel.Margin = new Padding(0, 0, 0, 0);
            controls.Controls.Add(statusLabel);

            view.TurnCompleted += delegate(Face face, int layer, int width, int turns, bool clockwise)
            {
                userTurnCount++;
                string moveText = MoveText(face, layer, width, turns, clockwise);
                SetStatus("Last: " + moveText);

                if (model.IsSolved())
                {
                    view.ShowCenterMessage(string.Format("Completed in {0} user turns", userTurnCount));
                }
            };
        }

        private void ChangeSize()
        {
            int size = (int)sizeBox.SelectedItem;
            view.CancelAnimation();
            view.ClearCenterMessage();
            model.Reset(size);
            userTurnCount = 0;
            lastScrambleTurns = 0;
            layerInput.Maximum = size;
            layerInput.Value = 1;
            SetStatus(string.Format("Last: {0}x{0}x{0} reset", size));
            view.Invalidate();
        }

        private void ApplyMove(bool clockwise)
        {
            Face face = SelectedFace();
            int layer = (int)layerInput.Value;
            int width = ((ModifierKeys & Keys.Shift) == Keys.Shift) ? Math.Max(1, Math.Min(2, model.Size - layer + 1)) : 1;
            if (view.AnimateTurn(face, layer, width, clockwise))
            {
                SetStatus("Turning: " + MoveText(face, layer, width, 1, clockwise));
            }
        }

        private void Scramble()
        {
            view.CancelAnimation();
            view.ClearCenterMessage();
            int turns = 18 + model.Size * 6;
            for (int i = 0; i < turns; i++)
            {
                Face face = (Face)random.Next(0, 6);
                int layer = random.Next(1, model.Size + 1);
                bool clockwise = random.Next(0, 2) == 0;
                model.Turn(face, layer, clockwise);
            }

            userTurnCount = 0;
            lastScrambleTurns = turns;
            SetStatus(string.Format("Last: scramble ({0} setup turns)", turns));
            view.Invalidate();
        }

        private void ResetCube()
        {
            view.CancelAnimation();
            view.ClearCenterMessage();
            model.Reset(model.Size);
            userTurnCount = 0;
            lastScrambleTurns = 0;
            layerInput.Value = 1;
            SetStatus("Last: reset");
            view.Invalidate();
        }

        private void SetStatus(string firstLine)
        {
            if (lastScrambleTurns > 0)
            {
                statusLabel.Text = string.Format("{0}\r\nUser turns: {1}\r\nScramble: {2} turns", firstLine, userTurnCount, lastScrambleTurns);
            }
            else
            {
                statusLabel.Text = string.Format("{0}\r\nUser turns: {1}", firstLine, userTurnCount);
            }
        }

        private static string MoveText(Face face, int layer, int width, int turns, bool clockwise)
        {
            string text = FaceName(face);
            if (width > 1) text += "w";
            if (turns == 2) text += "2";
            else if (!clockwise) text += "'";
            if (layer > 1) text += " layer " + layer;
            return text;
        }

        private Face SelectedFace()
        {
            string text = faceBox.SelectedItem == null ? "U" : faceBox.SelectedItem.ToString();
            if (text == "R") return Face.Right;
            if (text == "L") return Face.Left;
            if (text == "D") return Face.Down;
            if (text == "F") return Face.Front;
            if (text == "B") return Face.Back;
            return Face.Up;
        }

        private static string FaceName(Face face)
        {
            if (face == Face.Right) return "R";
            if (face == Face.Left) return "L";
            if (face == Face.Down) return "D";
            if (face == Face.Front) return "F";
            if (face == Face.Back) return "B";
            return "U";
        }

        private static Label MakeLabel(string text)
        {
            Label label = new Label();
            label.Text = text;
            label.ForeColor = Color.FromArgb(183, 191, 210);
            label.Width = 236;
            label.Height = 22;
            label.Margin = new Padding(0, 0, 0, 4);
            return label;
        }

        private static Control MakeSpacer(int height)
        {
            Panel spacer = new Panel();
            spacer.Width = 236;
            spacer.Height = height;
            spacer.BackColor = Color.Transparent;
            spacer.Margin = new Padding(0);
            return spacer;
        }

        private static void StyleCombo(ComboBox combo)
        {
            combo.Width = 236;
            combo.Height = 36;
            combo.DropDownStyle = ComboBoxStyle.DropDownList;
            combo.BackColor = Color.FromArgb(29, 34, 46);
            combo.ForeColor = Color.FromArgb(238, 243, 255);
            combo.FlatStyle = FlatStyle.Flat;
            combo.Margin = new Padding(0, 0, 0, 14);
        }

        private static Button MakeWideButton(string text, Color color)
        {
            Button button = MakeButton(text, color);
            button.Width = 236;
            button.Height = 42;
            button.Margin = new Padding(0, 0, 0, 10);
            return button;
        }

        private static Button MakeButton(string text, Color color)
        {
            Button button = new Button();
            button.Text = text;
            button.Dock = DockStyle.Fill;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.BackColor = color;
            button.ForeColor = Color.FromArgb(12, 14, 20);
            button.Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold, GraphicsUnit.Point);
            button.Margin = new Padding(3);
            button.Cursor = Cursors.Hand;
            return button;
        }
    }
}
