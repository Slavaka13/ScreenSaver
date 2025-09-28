using System;
using System.Drawing;

namespace ScreenSaver
{
    internal class Snowflake
    {
        public PointF Position;
        public float PixelSize;   // размер в пикселях
        public float SpeedY;
        public float DriftAmp;
        public float DriftSpeed;
        public float Phase;

        private float baseX;

        public Snowflake(PointF pos, float pixelSize, float speedY,
                         float driftAmp, float driftSpeed, float phase)
        {
            Position = pos;
            baseX = pos.X;
            PixelSize = pixelSize;
            SpeedY = speedY;
            DriftAmp = driftAmp;
            DriftSpeed = driftSpeed;
            Phase = phase;
        }

        public void Update(float dt, int screenW, int screenH, Random rnd)
        {
            // ровный вертикальный ход
            Position = new PointF(Position.X, Position.Y + SpeedY * dt);

            // плавный синус по X
            Phase += DriftSpeed * dt;
            float x = baseX + (float)Math.Sin(Phase) * DriftAmp;
            Position = new PointF(x, Position.Y);

            // респавн сверху
            if (Position.Y > screenH + 10)
            {
                baseX = rnd.Next(0, screenW);
                Position = new PointF(baseX, -rnd.Next(20, 150));

                float t = (float)rnd.NextDouble();     // 0..1
                PixelSize = Lerp(12f, 44f, t);        // умеренные размеры
                SpeedY = Lerp(70f, 200f, t);       // крупнее — быстрее
                DriftAmp = Lerp(3f, 14f, 1f - t);    // мелкие гуляют больше
                DriftSpeed = Lerp(0.6f, 1.2f, (float)rnd.NextDouble());
                Phase = (float)(rnd.NextDouble() * Math.PI * 2);
            }
        }

        private static float Lerp(float a, float b, float t) => a + (b - a) * t;
    }
}
