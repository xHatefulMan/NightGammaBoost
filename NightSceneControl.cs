using SkiaSharp;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;
using System;
using System.Collections.Generic;

namespace NightGammaBoost
{
    public class NightSceneControl : SKElement
    {
        private float _brightness = 0.08f;
        private Random _rng = new Random(42);
        private List<(float x, float y, float size)> _stars = new();
        private List<(float x, float baseY, float height, float width)> _trees = new();
        private List<float> _mountainPoints = new();

        public float Brightness
        {
            get => _brightness;
            set { _brightness = Math.Clamp(value, 0f, 1f); InvalidateVisual(); }
        }

        public NightSceneControl()
        {
            GenerateScene();
        }

        private void GenerateScene()
        {
            _stars.Clear();
            for (int i = 0; i < 80; i++)
                _stars.Add((
                    x: (float)_rng.NextDouble() * 680,
                    y: (float)_rng.NextDouble() * 60,
                    size: (float)(_rng.NextDouble() * 1.5 + 0.5)));

            _trees.Clear();
            for (int i = 0; i < 20; i++)
                _trees.Add(((float)(i * 35 + _rng.NextDouble() * 15), 110, (float)(35 + _rng.NextDouble() * 15), 14));
            for (int i = 0; i < 16; i++)
                _trees.Add(((float)(i * 44 + _rng.NextDouble() * 20), 130, (float)(45 + _rng.NextDouble() * 20), 18));
            for (int i = 0; i < 12; i++)
                _trees.Add(((float)(i * 58 + _rng.NextDouble() * 25), 155, (float)(55 + _rng.NextDouble() * 25), 22));

            _mountainPoints.Clear();
            float[] pts = { 0, 95, 80, 72, 160, 48, 250, 38, 340, 55, 430, 62, 520, 44, 610, 52, 680, 80 };
            foreach (var p in pts) _mountainPoints.Add(p);
        }

        protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
        {
            var canvas = e.Surface.Canvas;
            canvas.Clear();

            float W = e.Info.Width;
            float H = e.Info.Height;
            canvas.Scale(W / 680f, H / 170f);
            W = 680; H = 170;

            float b = _brightness;

            // Ciel
            using (var paint = new SKPaint { IsAntialias = true })
            {
                paint.Shader = SKShader.CreateLinearGradient(
                    new SKPoint(0, 0), new SKPoint(0, H),
                    new[] { Lerp(SKColor.Parse("#02040a"), SKColor.Parse("#0a1428"), b),
                            Lerp(SKColor.Parse("#04080f"), SKColor.Parse("#162040"), b) },
                    null, SKShaderTileMode.Clamp);
                canvas.DrawRect(0, 0, W, H, paint);
            }

            // Zone rad
            using (var paint = new SKPaint { IsAntialias = true })
            {
                byte ra = (byte)(30 + b * 60);
                paint.Shader = SKShader.CreateRadialGradient(
                    new SKPoint(520, 75), 120,
                    new[] { new SKColor(40, 120, 20, ra), new SKColor(20, 80, 10, 0) },
                    null, SKShaderTileMode.Clamp);
                canvas.DrawRect(0, 0, W, H, paint);
            }

            // Etoiles
            using (var paint = new SKPaint { IsAntialias = true })
            {
                float sa = Math.Max(0, 1f - b * 3f);
                foreach (var (sx, sy, size) in _stars)
                {
                    paint.Color = new SKColor(200, 210, 240, (byte)(sa * 200));
                    canvas.DrawCircle(sx, sy, size, paint);
                }
            }

            // Lune
            using (var paint = new SKPaint { IsAntialias = true })
            {
                float ma = Math.Max(0, 1f - b * 2f);
                paint.Color = new SKColor(220, 225, 240, (byte)(ma * 180));
                canvas.DrawCircle(580, 25, 14, paint);
                paint.Color = Lerp(SKColor.Parse("#04080f"), SKColor.Parse("#0a1428"), b);
                canvas.DrawCircle(572, 22, 12, paint);
            }

            // Montagne
            DrawMountain(canvas, b);

            // Colline
            using (var paint = new SKPaint { IsAntialias = true })
            {
                var path = new SKPath();
                path.MoveTo(0, H);
                path.LineTo(0, 120);
                path.QuadTo(170, 90, 340, 105);
                path.QuadTo(510, 120, 680, 100);
                path.LineTo(680, H);
                path.Close();
                paint.Color = Lerp(SKColor.Parse("#050a04"), SKColor.Parse("#0a1808"), b);
                canvas.DrawPath(path, paint);
            }

            DrawTrees(canvas, 0, 20, b, true);

            // Colline avant
            using (var paint = new SKPaint { IsAntialias = true })
            {
                var path = new SKPath();
                path.MoveTo(0, H);
                path.LineTo(0, 138);
                path.QuadTo(170, 125, 340, 132);
                path.QuadTo(510, 139, 680, 128);
                path.LineTo(680, H);
                path.Close();
                paint.Color = Lerp(SKColor.Parse("#040804"), SKColor.Parse("#081406"), b);
                canvas.DrawPath(path, paint);
            }

            DrawTrees(canvas, 20, 36, b, false);

            // Sol
            using (var paint = new SKPaint { IsAntialias = true })
            {
                var path = new SKPath();
                path.MoveTo(0, H);
                path.LineTo(0, 155);
                path.QuadTo(340, 148, 680, 152);
                path.LineTo(680, H);
                path.Close();
                paint.Color = Lerp(SKColor.Parse("#030604"), SKColor.Parse("#060e05"), b);
                canvas.DrawPath(path, paint);
            }

            DrawTrees(canvas, 36, 48, b, false);

            using (var paint = new SKPaint { IsAntialias = true })
            {
                paint.Color = Lerp(SKColor.Parse("#020403"), SKColor.Parse("#040a04"), b);
                canvas.DrawRect(0, 162, W, H - 162, paint);
            }
        }

        private void DrawMountain(SKCanvas canvas, float b)
        {
            using var paint = new SKPaint { IsAntialias = true };
            var pts = _mountainPoints;
            var path = new SKPath();
            path.MoveTo(0, 170);
            path.LineTo(pts[0], pts[1]);
            for (int i = 2; i < pts.Count - 2; i += 2)
            {
                float mx = (pts[i] + pts[i + 2]) / 2;
                float my = (pts[i + 1] + pts[i + 3]) / 2;
                path.QuadTo(pts[i], pts[i + 1], mx, my);
            }
            path.LineTo(pts[pts.Count - 2], pts[pts.Count - 1]);
            path.LineTo(680, 170);
            path.Close();
            paint.Color = Lerp(SKColor.Parse("#080c18"), SKColor.Parse("#101828"), b);
            canvas.DrawPath(path, paint);

            using var sp = new SKPaint { IsAntialias = true };
            sp.Color = new SKColor(220, 225, 240, (byte)(40 + b * 80));
            for (int i = 2; i < pts.Count - 2; i += 4)
            {
                if (pts[i + 1] < 60)
                {
                    var snow = new SKPath();
                    snow.MoveTo(pts[i] - 15, pts[i + 1] + 12);
                    snow.LineTo(pts[i], pts[i + 1]);
                    snow.LineTo(pts[i] + 15, pts[i + 1] + 12);
                    canvas.DrawPath(snow, sp);
                }
            }
        }

        private void DrawTrees(SKCanvas canvas, int from, int to, float b, bool far)
        {
            using var paint = new SKPaint { IsAntialias = true };
            for (int i = from; i < Math.Min(to, _trees.Count); i++)
            {
                var (tx, baseY, height, width) = _trees[i];
                SKColor treeColor = far
                    ? Lerp(SKColor.Parse("#060c05"), SKColor.Parse("#0c1a0a"), b)
                    : Lerp(SKColor.Parse("#040a03"), SKColor.Parse("#0a1608"), b);

                // Tronc
                paint.Color = Lerp(SKColor.Parse("#030503"), SKColor.Parse("#060a05"), b);
                canvas.DrawRect(tx + width / 2 - 1.5f, baseY - 6, 3, 8, paint);

                paint.Color = treeColor;
                float tip = baseY - height;
                float mid1 = tip + height * 0.35f;
                float mid2 = tip + height * 0.62f;

                var top = new SKPath();
                top.MoveTo(tx + width / 2, tip);
                top.LineTo(tx + width * 0.2f, mid1);
                top.LineTo(tx + width * 0.8f, mid1);
                top.Close();
                canvas.DrawPath(top, paint);

                var mid = new SKPath();
                mid.MoveTo(tx + width / 2, mid1 - height * 0.1f);
                mid.LineTo(tx, mid2);
                mid.LineTo(tx + width, mid2);
                mid.Close();
                canvas.DrawPath(mid, paint);

                var bot = new SKPath();
                bot.MoveTo(tx + width / 2, mid2 - height * 0.08f);
                bot.LineTo(tx - width * 0.1f, baseY);
                bot.LineTo(tx + width * 1.1f, baseY);
                bot.Close();
                canvas.DrawPath(bot, paint);
            }
        }

        private SKColor Lerp(SKColor a, SKColor b, float t) =>
            new SKColor(
                (byte)(a.Red + (b.Red - a.Red) * t),
                (byte)(a.Green + (b.Green - a.Green) * t),
                (byte)(a.Blue + (b.Blue - a.Blue) * t),
                255);
    }
}