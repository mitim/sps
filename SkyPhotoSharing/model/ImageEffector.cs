using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SkyPhotoSharing
{
    class ImageEffector
    {
        public ImageEffector()
        {
            Mode = EffectMode.NOEFFECT;

        }

        public enum EffectMode { SCALE, SCROLL, ROTATE, NOEFFECT };

        public EffectMode Mode { get; protected set; }

        protected Point PrevPoint { get; set; } 

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
 
    }
}
