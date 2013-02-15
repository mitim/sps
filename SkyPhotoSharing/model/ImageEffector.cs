using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace SkyPhotoSharing
{
    class ImageEffector
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private const double ZOOM_DELTA = 0.05;
        private const double ORIGINAL_SCALE = 1.0;
        private const double ROTATE_VALUE = 0.3;
        public ImageEffector()
        {
            Mode = EffectMode.NOEFFECT;

        }

        public enum EffectMode { SCALE, SCROLL, ROTATE, NOEFFECT };

        public EffectMode Mode { get; protected set; }

        protected Point PrevMousePos { get; set; } 

        public void Scale(Photo photo, double delta)
        {
            Mode = EffectMode.SCALE;
            double os = photo.Scale;
            double ns = photo.Scale + ZoomDelta(delta);
            photo.Scale = (ns > ZOOM_DELTA) ? ns : ZOOM_DELTA;
            FitMapRectWithScale(photo, os);
        }

        public void CorrectScaledOffset(Photo photo, Point winPos, Point mapPos, Rect winSize)
        {
            if (Mode != EffectMode.SCALE) return;
            double x = (mapPos.X * photo.Scale) - winPos.X;
            double y = (mapPos.Y * photo.Scale) - winPos.Y;
            photo.WindowPosition = new Point(x, y);
            winSize.X = photo.WindowPosition.X;
            winSize.Y = photo.WindowPosition.Y;
            CorrectScrollStop(photo, winSize);
        }

        public void ScaleToFitSize(Photo photo, Rect winSize)
        {
            double os = photo.Scale;
            var xs = winSize.Width / photo.Image.PixelWidth;
            var ys = winSize.Height / photo.Image.PixelHeight;
            var ns = xs <= ys ? xs : ys;
            photo.Scale = (ns <= ORIGINAL_SCALE) ? ns : ORIGINAL_SCALE;
            FitMapRectWithScale(photo, os);
            CorrectScrollStop(photo, winSize);
        }
        public void ScaleToOriginalSize(Photo photo, Rect winSize)
        {
            double os = photo.Scale;
            photo.Scale = ORIGINAL_SCALE;
            FitMapRectWithScale(photo, os);
            CorrectScrollStop(photo, winSize);
        }

        public void PrevScroll(Point point)
        {
            Mode = EffectMode.SCROLL;
            PrevMousePos = point;
        }

        public void ScrollTo(Photo photo, Point outcome, Rect winSize)
        {
            if (Mode != EffectMode.SCROLL) return;
            var dx = PrevMousePos.X - outcome.X;
            var dy = PrevMousePos.Y - outcome.Y;
            PrevMousePos = outcome;
            var ns = Rect.Offset(winSize, new Vector(dx, dy));
            photo.WindowPosition = new Point(ns.X, ns.Y);
            CorrectScrollStop(photo, ns);
        }

        public void PrevRotate(Point point)
        {
            Mode = EffectMode.ROTATE;
            PrevMousePos = point;
        }

        public void RotateTo(Photo photo, Rect mapRect, Point outcome, Rect windowOffset)
        {
            if (Mode != EffectMode.ROTATE) return;
            var r1 = CalcRad(mapRect, PrevMousePos);
            var r2 = CalcRad(mapRect, outcome);
            photo.Rotate = (photo.Rotate + (ConvertRadToAngle(r2 - r1) * ROTATE_VALUE) + 360) % 360;
            PrevMousePos = outcome;
            photo.MapRect = RecalcShowRect(GetOriginalRect(photo), photo.Scale, ConvertAngleToRad(photo.Rotate));
            CorrectScrollStop(photo, windowOffset); 
        }

        public void RotateTo(Photo photo,double angle, Rect windowOffset)
        {
            photo.Rotate = angle;
            photo.MapRect = RecalcShowRect(GetOriginalRect(photo), photo.Scale, ConvertAngleToRad(photo.Rotate));
            CorrectScrollStop(photo, windowOffset);
        }

        public void InitializeViewPos(Photo photo, Vector windowSize, Vector mapSize)
        {
            if (!photo.WindowPosition.Equals(Photo.UNINITIALIZED_POINT)) return;
            var x = (mapSize.X - windowSize.X) / 2;
            var y = (mapSize.Y - photo.Image.PixelHeight) / 2;
            photo.WindowPosition = new Point(x, y);
            CorrectScrollStop(photo, new Rect(photo.WindowPosition, Point.Add(photo.WindowPosition, windowSize)));
        }

        public void CorrectScrollStop(Photo photo, Rect windowOffset)
        {
            var x = CorrectOffset(photo.MapRect.Left, photo.MapRect.Width, windowOffset.Left, windowOffset.Width);
            var y = CorrectOffset(photo.MapRect.Top, photo.MapRect.Height, windowOffset.Top, windowOffset.Height);
            photo.WindowPosition = new Point(x, y);
        }

        public void FlipVertical(Photo photo)
        {
            photo.FlipVertical *= -1;
        }

        public void FlipHorizontal(Photo photo)
        {
            photo.FlipHorizontal *= -1;
        }

        private Rect GetOriginalRect(Photo p)
        {
            return new Rect(
                p.MapLeft, 
                p.MapTop, 
                p.Image.PixelWidth, 
                p.Image.PixelHeight
            );
        }

        private double CalcRad(Rect size, Point delta)
        {
            var x = (delta.X - (size.Width  / 2));
            var y = (delta.Y - (size.Height / 2));
            return Math.Atan2(y, x);
        }
 

        private double ZoomDelta(double delta)
        {
            return delta > 0 ? ZOOM_DELTA : -ZOOM_DELTA;
        }

        private double CorrectOffset(double ptop, double plen, double wtop, double wlen)
        {
            if (plen < wlen) return ptop - ((wlen - plen) / 2);
            if (wtop < ptop) return ptop;
            var pb = ptop + plen;
            var wb = wtop + wlen;
            if (pb < wb) return wtop - (wb - pb);
            return wtop;
        }

        private void FitMapRectWithScale(Photo photo, double oldscale)
        {
            photo.MapRect = FitRectWithScale( photo.MapRect, oldscale, photo.Scale);
        }

        private Rect FitRectWithScale(Rect origin, double oldscale, double newscale)
        {
            var vec = newscale / oldscale;
            return new Rect(
                origin.X * vec,
                origin.Y * vec,
                origin.Width * vec,
                origin.Height * vec
            );
        }

        private Rect RecalcShowRect(Rect org, double scale, double spinrad)
        {
            var rec = FitRectWithScale(org, ORIGINAL_SCALE, scale);
            var ray = ((new Vector(rec.Width, rec.Height)).Length / 2);
            var piv = new Point(
                rec.Left + (rec.Width / 2),
                rec.Top + (rec.Height / 2)
            );
            var tl = Point.Add(CalcCoord(ray, CalcAtan(Point.Subtract(rec.TopLeft, piv)), spinrad), new Vector(piv.X, piv.Y));
            var tr = Point.Add(CalcCoord(ray, CalcAtan(Point.Subtract(rec.TopRight, piv)), spinrad), new Vector(piv.X, piv.Y));
            var bl = Point.Add(CalcCoord(ray, CalcAtan(Point.Subtract(rec.BottomLeft, piv)), spinrad), new Vector(piv.X, piv.Y));
            var br = Point.Add(CalcCoord(ray, CalcAtan(Point.Subtract(rec.BottomRight, piv)), spinrad), new Vector(piv.X, piv.Y));
            var l = new[] { tl.X, tr.X, bl.X, br.X }.Min();
            var t = new[] { tl.Y, tr.Y, bl.Y, br.Y }.Min();
            var r = new[] { tl.X, tr.X, bl.X, br.X }.Max();
            var b = new[] { tl.Y, tr.Y, bl.Y, br.Y }.Max();
            return new Rect(new Point(l, t), new Point(r, b));
        }

        private Point CalcCoord(double ray, double orgrad, double spinrad)
        {
            var nr = orgrad + spinrad;
            var x = Math.Cos(nr) * ray;
            var y = Math.Sin(nr) * ray;
            return new Point(x, y);
        }

        private double CalcAtan(Vector t)
        {
            return Math.Atan2(t.X, t.Y);
        }

        private double ConvertRadToAngle(double rad)
        {
            return rad * 180;
        }

        private double ConvertAngleToRad(double angle)
        {
            return angle / 180;
        }
    }
}
