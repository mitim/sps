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

        private const double ZOOM_DELTA = 0.05;

        public ImageEffector()
        {
            Mode = EffectMode.NOEFFECT;

        }

        public enum EffectMode { SCALE, SCROLL, ROTATE, NOEFFECT };

        public EffectMode Mode { get; protected set; }

        protected Point PrevPoint { get; set; } 

        public double Scale(double scale, double delta)
        {
            Mode = EffectMode.SCALE;
            double ns = scale + ZoomDelta(delta);
            if (ns > ZOOM_DELTA)
            {
                return ns;
            }
            else
            {
                return ZOOM_DELTA;
            }
        }

        public Point CorrectScalePoint(Point viewPos, Point mapPos, double mapScale)
        {
            if (Mode != EffectMode.SCALE) return viewPos;
            double x = mapPos.X * mapScale - viewPos.X;
            double y = mapPos.Y * mapScale - viewPos.Y;
            return new Point(x, y);
        }

        public double ScaleToFitSize(Photo p,double actualX, double actualY)
        {
            var xs = actualX / p.Image.PixelWidth;
            var ys = actualY / p.Image.PixelHeight;
            var ns = xs <= ys ? xs : ys;
            return ns <= 1.0 ? ns : 1.0;
        }

        public void PrevScroll(Point point)
        {
            Mode = EffectMode.SCROLL;
            PrevPoint = point;
        }

        public Point ScrollTo(Point offset, Point delta)
        {
            if (Mode != EffectMode.SCROLL) return new Point(0,0);
            var x = PrevPoint.X - delta.X;
            var y = PrevPoint.Y - delta.Y;
            var p = new Point(x, y);
            PrevPoint = delta;
            return new Point(x + offset.X, y + offset.Y);
        }

        public void PrevRotate(Point point)
        {
            Mode = EffectMode.ROTATE;
            PrevPoint = point;
        }

        public double RotateTo(double angle, Point size, Point delta)
        {
            if (Mode != EffectMode.ROTATE) return 0;
            var r1 = CalcRad(size, PrevPoint);
            var r2 = CalcRad(size, delta);
            return angle + (r2 - r1);
        }

        private double CalcRad(Point size, Point delta)
        {
            int x = (int)(delta.X - (size.X / 2));
            int y = (int)(delta.Y - (size.Y / 2));
            return Math.Atan2(y, x) % 2;
        }
 

        private double ZoomDelta(double delta)
        {
            return delta > 0 ? ZOOM_DELTA : -ZOOM_DELTA;
        }
    }
}
