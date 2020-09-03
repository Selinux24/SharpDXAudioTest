using SharpDX;
using System;
using System.Windows.Forms;

namespace SharpDXAudioTest
{
    using Color = System.Drawing.Color;
    using Graphics = System.Drawing.Graphics;
    using Pen = System.Drawing.Pen;
    using Point = System.Drawing.Point;
    using PointF = System.Drawing.PointF;
    using Rectangle = System.Drawing.Rectangle;
    using SolidBrush = System.Drawing.SolidBrush;

    class GridPanel : Panel
    {
        public IAgent SelectedAgent { get; set; }
        public IAgent Listener { get; set; }
        public IAgent Helicopter { get; set; }
        public IAgent Music { get; set; }

        private static Point GetItemPoint(Vector3 p, Rectangle clientRectangle)
        {
            float xSize = AudioConstants.XMAX - AudioConstants.XMIN;
            float zSize = AudioConstants.ZMAX - AudioConstants.ZMIN;

            var px = (p.X + AudioConstants.XMAX) * (clientRectangle.Width - xSize) / xSize;
            var pz = (p.Z + AudioConstants.ZMAX) * (clientRectangle.Height - zSize) / zSize;
            px += xSize * 0.5f;
            pz += zSize * 0.5f;

            return new Point((int)px, (int)pz);
        }
        private static Rectangle GetItemRectangle(Vector3 p, int size, Rectangle clientRectangle)
        {
            var point = GetItemPoint(p, clientRectangle);

            return new Rectangle((int)(point.X - (size * 0.5f)), (int)(point.Y - (size * 0.5f)), size, size);
        }
        private static float GetRelativeAngle(Vector3 dir, float coneAngle)
        {
            float dirAngle = MathUtil.RadiansToDegrees(AngleSigned(Vector2.UnitX, new Vector2(dir.X, dir.Z)));

            dirAngle -= coneAngle * 0.5f;
            if (coneAngle < 0) dirAngle += 360;

            return dirAngle;
        }
        private static float CrossSigned(Vector2 one, Vector2 two)
        {
            return one.X * two.Y - one.Y * two.X;
        }
        private static float AngleSigned(Vector2 one, Vector2 two)
        {
            return (float)Math.Atan2(CrossSigned(one, two), Vector2.Dot(one, two));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            DrawCanvas(e.Graphics, e.ClipRectangle);
        }

        void DrawCanvas(Graphics formGraphics, Rectangle bounds)
        {
            IAgent agent = SelectedAgent;
            Color agentColor = Color.Black;

            using (var myBrush = new SolidBrush(Color.Black))
            using (var myPen = new Pen(Color.Black))
            {
                formGraphics.Clear(Color.Black);

                int radius = (int)(bounds.Width * 1.22f);

                DrawGrid(formGraphics, myPen, bounds);

                if (agent == Listener) agentColor = Color.Blue;
                DrawListener(formGraphics, myPen, myBrush, bounds, Color.Blue);

                if (agent == Helicopter) agentColor = Color.Red;
                DrawHelicopter(formGraphics, myPen, myBrush, bounds, Color.Red, radius);

                if (agent == Music) agentColor = Color.DarkGray;
                DrawMusic(formGraphics, myPen, myBrush, bounds, Color.DarkGray, radius);

                if (agent == null)
                {
                    return;
                }

                DrawSelected(formGraphics, myPen, bounds, agent, agentColor);
            }
        }
        void DrawGrid(Graphics formGraphics, Pen myPen, Rectangle bounds)
        {
            float gridSize = 20;
            float xInc = (bounds.Width - 20) / gridSize;
            float yInc = (bounds.Height - 20) / gridSize;

            for (int i = 0; i < gridSize + 1; i++)
            {
                float x = i * xInc;
                float y = i * yInc;

                myPen.Color = i == gridSize / 2 ? Color.GreenYellow : Color.Green;

                var p1 = new PointF(0, x);
                var p2 = new PointF(bounds.Width, x);
                p1.X += 10; p1.Y += 10;
                p2.X -= 10; p2.Y += 10;
                formGraphics.DrawLines(myPen, new[] { p1, p2 });

                var p3 = new PointF(y, 0);
                var p4 = new PointF(y, bounds.Height);
                p3.X += 10; p3.Y += 10;
                p4.X += 10; p4.Y -= 10;
                formGraphics.DrawLines(myPen, new[] { p3, p4 });
            }
        }
        void DrawListener(Graphics formGraphics, Pen myPen, SolidBrush myBrush, Rectangle bounds, Color color)
        {
            if (Listener == null)
            {
                return;
            }

            var listenerOrientation = new Vector3(Listener.OrientFront.X, Listener.OrientFront.Y, -Listener.OrientFront.Z);
            var listenerPosition = new Vector3(Listener.Position.X, Listener.Position.Y, -Listener.Position.Z);

            if (Listener.UseCone)
            {
                float outerConeAngle = MathUtil.RadiansToDegrees(Listener.Cone.OuterAngle);
                float innerConeAngle = MathUtil.RadiansToDegrees(Listener.Cone.InnerAngle);

                myBrush.Color = Color.CornflowerBlue;
                formGraphics.FillPie(
                    myBrush,
                    GetItemRectangle(listenerPosition, 50, bounds),
                    GetRelativeAngle(listenerOrientation, outerConeAngle),
                    outerConeAngle);

                myBrush.Color = Color.CadetBlue;
                formGraphics.FillPie(
                    myBrush,
                    GetItemRectangle(listenerPosition, 50, bounds),
                    GetRelativeAngle(listenerOrientation, innerConeAngle),
                    innerConeAngle);

                myPen.Color = Color.Blue;
                formGraphics.DrawLine(
                    myPen,
                    GetItemPoint(listenerPosition, bounds),
                    GetItemPoint(listenerPosition + (listenerOrientation * 2), bounds));
            }
            myBrush.Color = color;
            formGraphics.FillEllipse(
                myBrush,
                GetItemRectangle(listenerPosition, 10, bounds));
        }
        void DrawHelicopter(Graphics formGraphics, Pen myPen, SolidBrush myBrush, Rectangle bounds, Color color, int radius)
        {
            if (Helicopter == null)
            {
                return;
            }

            var heliPosition = new Vector3(Helicopter.Position.X, Helicopter.Position.Y, -Helicopter.Position.Z);

            myBrush.Color = color;
            formGraphics.FillEllipse(
                myBrush,
                GetItemRectangle(heliPosition, 10, bounds));
            myPen.Color = color;
            formGraphics.DrawEllipse(
                myPen,
                GetItemRectangle(heliPosition, radius, bounds));
        }
        void DrawMusic(Graphics formGraphics, Pen myPen, SolidBrush myBrush, Rectangle bounds, Color color, int radius)
        {
            if (Music == null)
            {
                return;
            }

            var musicPosition = new Vector3(Music.Position.X, Music.Position.Y, -Music.Position.Z);

            myBrush.Color = color;
            formGraphics.FillEllipse(
                myBrush,
                GetItemRectangle(musicPosition, 10, bounds));
            myPen.Color = color;
            formGraphics.DrawEllipse(
                myPen,
                GetItemRectangle(musicPosition, radius, bounds));
        }
        void DrawSelected(Graphics formGraphics, Pen myPen, Rectangle bounds, IAgent agent, Color agentColor)
        {
            if (agent == null)
            {
                return;
            }

            var agentPosition = new Vector3(agent.Position.X, agent.Position.Y, -agent.Position.Z);
            myPen.Color = agentColor;
            formGraphics.DrawEllipse(
                myPen,
                GetItemRectangle(agentPosition, 15, bounds));
        }
    }
}
