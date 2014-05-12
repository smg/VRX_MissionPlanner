using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Xml;

namespace LNMultiPilot.Library
{
    public class MPMotor
    {
        public enum MPMotorGraphicStyle  
        { 
            mgVerticalSlider = 1,
            mgHorizontalSlider = 2,
            mgHorizontalLeftSlider = 3,
            mgCircle = 4,
            mgEllipse = 5,
            mgHorizontalBidirectional = 6,
            mgHorizontalBidirectionalLeft = 7,
            mgVerticalBidirectional = 8
        }


        public int Index;
        public double Min;
        public double Max;
        public double Value;
        public MPMotorGraphicStyle Style;

        //stili disegno
        Pen m_penBorder = System.Drawing.Pens.Black;
        Brush m_brushfontValue = System.Drawing.Brushes.Black;
        Font m_fontValue = new Font("Arial", 10);

        public MPMotor()
        {
            Index = -1;
            Min = 1000;
            Max = 2000;
            Value = 1000;
            Style = MPMotorGraphicStyle.mgCircle;
        }

        public MPMotor(XmlNode nd)
        {
            Min = 1000;
            Max = 2000;
            Value = 1000;

            Index = Convert.ToInt32(nd.Attributes["index"].Value);
            float x = (float) Utility.Str2Double(nd.Attributes["x"].Value);
            float y = (float)Utility.Str2Double(nd.Attributes["y"].Value);
            float w = (float)Utility.Str2Double(nd.Attributes["width"].Value);
            float h = (float)Utility.Str2Double(nd.Attributes["height"].Value);
            m_RectF = new RectangleF(x, y, w, h);
            Style = (MPMotorGraphicStyle) Enum.Parse(typeof(MPMotorGraphicStyle), nd.Attributes["style"].Value);
        }

        public void AppendXml(XmlDocument doc, XmlNode nd)
        {
            XmlNode ch = doc.CreateElement("motor");
            XmlAttribute a = doc.CreateAttribute("index");
            a.Value = Index.ToString();
            ch.Attributes.Append(a);
            a = doc.CreateAttribute("style");
            a.Value = Style.ToString();
            ch.Attributes.Append(a);
            a = doc.CreateAttribute("x");
            a.Value = Utility.Double2Str((double) m_RectF.X);
            ch.Attributes.Append(a);
            a = doc.CreateAttribute("y");
            a.Value = Utility.Double2Str((double)m_RectF.Y);
            ch.Attributes.Append(a);
            a = doc.CreateAttribute("width");
            a.Value = Utility.Double2Str((double)m_RectF.Width);
            ch.Attributes.Append(a);
            a = doc.CreateAttribute("height");
            a.Value = Utility.Double2Str((double)m_RectF.Height);
            ch.Attributes.Append(a);
            nd.AppendChild(ch);
        }

        protected RectangleF m_RectF;
        public RectangleF RectPerc
        {
            get { return m_RectF; }
            set { m_RectF = value; }
        }

        public PointF PositionPerc
        {
            get
            {
                PointF pt = new PointF(m_RectF.Location.X, m_RectF.Location.Y);
                pt.X += m_RectF.Width / 2;
                pt.Y += m_RectF.Height / 2;
                return pt;
            }
            set {
                PointF pt = value;
                pt.X -= m_RectF.Width / 2;
                pt.Y -= m_RectF.Height / 2;
                m_RectF.Location = pt;
            }
        }

        protected Rectangle m_Rect;
        public Rectangle Rect
        {
            get { return m_Rect; }
        }

        public Point Position
        {
            get {
                Point pt = new Point(m_Rect.Location.X, m_Rect.Location.Y);
                pt.Offset(m_Rect.Width / 2, m_Rect.Height / 2);
                return pt;
            }
        }

        public void Reposition(Rectangle containerRect)
        {
            m_Rect.X = (int)(containerRect.Width * m_RectF.X) + containerRect.X;
            m_Rect.Width = (int)(containerRect.Width * m_RectF.Width);
            m_Rect.Y = (int)(containerRect.Height * m_RectF.Y) + containerRect.Y;
            m_Rect.Height = (int)(containerRect.Height * m_RectF.Height);
        }

        protected double Interpolate(double minout, double maxout, double minin, double maxin, double val)
        {
            if (maxin - minin == 0)
                return 0;
            return minout + (maxout - minout) * (val - minin) / (maxin - minin);
        }

        public void Draw(Graphics surf)
        {
            switch (Style)
            { 
                case MPMotorGraphicStyle.mgVerticalSlider:
                    DrawVerticalRect(surf);
                    break;
                case MPMotorGraphicStyle.mgHorizontalSlider:
                    DrawHorizontalRect(surf, false);
                    break;
                case MPMotorGraphicStyle.mgHorizontalLeftSlider:
                    DrawHorizontalRect(surf, true);
                    break;
                case MPMotorGraphicStyle.mgCircle:
                    DrawCircle(surf);
                    break;
                case MPMotorGraphicStyle.mgEllipse:
                    DrawEllipse(surf);
                    break;
                case MPMotorGraphicStyle.mgHorizontalBidirectional:
                    DrawHorizontalBidirectionalRect(surf, false);
                    break;
                case MPMotorGraphicStyle.mgHorizontalBidirectionalLeft:
                    DrawHorizontalBidirectionalRect(surf, true);
                    break;
                case MPMotorGraphicStyle.mgVerticalBidirectional:
                    DrawVerticalBidirectional(surf);
                    break;
            }
        }

        protected Color FillColor
        {
            get 
            {
                Color col = Color.Transparent;
                double Mean = (Max + Min) / 2;
                

                if ((Style == MPMotorGraphicStyle.mgVerticalBidirectional) ||
                    (Style == MPMotorGraphicStyle.mgHorizontalBidirectional) ||
                    (Style == MPMotorGraphicStyle.mgHorizontalBidirectionalLeft))
                {

                    double Mean1 = (Mean + Min) / 2;
                    double Mean2 = (Mean + Max) / 2;

                    if (Value < Mean1) {
                        col = InterpColor(Color.Magenta, Color.Blue, Min, Mean1, Value);
                    } else if (Value < Mean){
                        col = InterpColor(Color.Blue, Color.Cyan, Mean1, Mean, Value);
                    } else if (Value < Mean2) {
                        col = InterpColor(Color.Lime, Color.Yellow, Mean, Mean2, Value);
                    } else {
                        col = InterpColor(Color.Yellow, Color.Red, Mean2, Max, Value);
                    }
                }
                else
                {
                    if (Value < Mean)
                    {
                        col = InterpColor(Color.Lime, Color.Yellow, Min, Mean, Value);
                    }
                    else
                    {
                        col = InterpColor(Color.Yellow, Color.Red, Mean, Max, Value);
                    }
                }
                return col;
            }
        }

        protected Color InterpColor(Color colMin, Color colMax, double min, double max, double val)
        {
            val = Math.Max(min, val);
            val = Math.Min(max, val);
            int a = (int) Interpolate((double)colMin.A, (double)colMax.A, min, max, val);
            int r = (int)Interpolate((double)colMin.R, (double)colMax.R, min, max, val);
            int g = (int)Interpolate((double)colMin.G, (double)colMax.G, min, max, val);
            int b = (int)Interpolate((double)colMin.B, (double)colMax.B, min, max, val);
            return Color.FromArgb(a, r, g, b);
        }

        protected string ValueString
        {
            get
            {
                return Index.ToString() + ": " + Value.ToString();
            }
        }

        protected void DrawVerticalRect(Graphics surf)
        {
            Color col = FillColor;
            Brush b = new SolidBrush(col);

            
            int top = (int)Interpolate(m_Rect.Bottom, m_Rect.Top, Min, Max, Value);
            int h = m_Rect.Bottom - top;
            Rectangle rc = new Rectangle(m_Rect.X, top, m_Rect.Width, h);
            surf.FillRectangle(b, rc);
            string str = ValueString;
            SizeF sz = surf.MeasureString(str, m_fontValue);
            surf.DrawString(str, m_fontValue, m_brushfontValue, (float)m_Rect.X, (float)(m_Rect.Bottom - sz.Height));
            surf.DrawRectangle(m_penBorder, m_Rect);

        }

        protected void DrawVerticalBidirectional(Graphics surf)
        {
            Color col = FillColor;
            Brush b = new SolidBrush(col);
            Rectangle rc;
            int h2 = m_Rect.Height / 2;
            int midx = m_Rect.X + m_Rect.Width / 2;
            int midy = m_Rect.Y + m_Rect.Height / 2;

            double Mean = (Max + Min) / 2;

            if (Value < Mean)
            {
                //direzione negativa
                int h = (int)Interpolate(h2, 0, Min, Mean, Value);
                rc = new Rectangle(m_Rect.X, midy, m_Rect.Width, h);
            }
            else 
            {
                //direzione positiva
                int h = (int)Interpolate(0, h2, Mean, Max, Value);
                rc = new Rectangle(m_Rect.X, midy - h, m_Rect.Width, h);
            }
            surf.FillRectangle(b, rc);
            string str = ValueString;
            SizeF sz = surf.MeasureString(str, m_fontValue);
            surf.DrawString(str, m_fontValue, m_brushfontValue, (float)(midx - sz.Width / 2), (float)(midy - sz.Height/2));
            surf.DrawRectangle(m_penBorder, m_Rect);

        }

        protected void DrawHorizontalRect(Graphics surf, bool bLeft)
        {
            Color col = FillColor;
            Brush b = new SolidBrush(col);
            Rectangle rc;
            if (bLeft)
            {
                int left = (int)Interpolate(m_Rect.Right, m_Rect.Left, Min, Max, Value);
                int w = m_Rect.Right - left;
                rc = new Rectangle(left, m_Rect.Y, w, m_Rect.Height);
            }
            else 
            {
                int right = (int)Interpolate(m_Rect.Left, m_Rect.Right, Min, Max, Value);
                int w =  right - m_Rect.Left;
                rc = new Rectangle(m_Rect.X, m_Rect.Y, w, m_Rect.Height);
            }
            surf.FillRectangle(b, rc);
            string str = ValueString;
            SizeF sz = surf.MeasureString(str, m_fontValue);
            surf.DrawString(str, m_fontValue, m_brushfontValue, (float)m_Rect.X, (float)(m_Rect.Bottom - sz.Height));
            surf.DrawRectangle(m_penBorder, m_Rect);

        }


        protected void DrawHorizontalBidirectionalRect(Graphics surf, bool bLeft)
        {
            Color col = FillColor;
            Brush b = new SolidBrush(col);
            Rectangle rc;
            int w2 = m_Rect.Width / 2;
            int midx = m_Rect.X + m_Rect.Width / 2;
            int midy = m_Rect.Y + m_Rect.Height / 2;

            double v = Value;
            if (bLeft)
                v = Min + Max - Value;

            double Mean = (Max + Min) / 2;

            if (v < Mean)
            {
                //direzione negativa
                int w = (int)Interpolate(w2, 0, Min, Mean, v);
                rc = new Rectangle(midx - w, m_Rect.Y, w, m_Rect.Height);
            }
            else
            {
                //direzione positiva
                int w = (int)Interpolate(0, w2, Mean, Max, v);
                rc = new Rectangle(midx, m_Rect.Y, w, m_Rect.Height);
            }

            surf.FillRectangle(b, rc);
            string str = ValueString;
            SizeF sz = surf.MeasureString(str, m_fontValue);
            surf.DrawString(str, m_fontValue, m_brushfontValue, (float)(midx - sz.Width / 2), (float)(midy - sz.Height/2));
            surf.DrawRectangle(m_penBorder, m_Rect);

        }


        protected void DrawCircle(Graphics surf)
        {
            Color col = FillColor;
            Brush b = new SolidBrush(col);
            int maxrad = Math.Max(m_Rect.Width/2, m_Rect.Height/2);
            int rad = (int)Interpolate(0, maxrad , Min, Max, Value);
            Point pt = Position;
            surf.FillEllipse(b, pt.X - rad, pt.Y - rad, 2 * rad, 2 * rad);
            string str = ValueString;
            SizeF sz = surf.MeasureString(str, m_fontValue);
            surf.DrawString(str, m_fontValue, m_brushfontValue, (float)(pt.X - sz.Width / 2), (float)(pt.Y - sz.Height / 2));
            surf.DrawEllipse(m_penBorder, pt.X - maxrad, pt.Y - maxrad, 2 * maxrad, 2 * maxrad);

        }

        protected void DrawEllipse(Graphics surf)
        {
            Color col = FillColor;
            Brush b = new SolidBrush(col);
            int w = (int)Interpolate(0, m_Rect.Width, Min, Max, Value);
            int h = (int)Interpolate(0, m_Rect.Height, Min, Max, Value);
            Point pt = Position;
            surf.FillEllipse(b, pt.X - w/2, pt.Y - h/2, w, h);
            string str = ValueString;
            SizeF sz = surf.MeasureString(str, m_fontValue);
            surf.DrawString(str, m_fontValue, m_brushfontValue, (float)(pt.X - sz.Width / 2), (float)(pt.Y - sz.Height / 2));
            surf.DrawEllipse(m_penBorder, m_Rect);

        }

    }
}
