using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace LNMultiPilot.Library
{
    public class MPMotorConfig
    {
        string m_Code;
        string m_ImageFile;
        List<MPMotor> m_lstMotors = new List<MPMotor>();
        System.Drawing.Bitmap m_imgBackground = null;
        System.Drawing.Rectangle m_ContainerRect;
        System.Drawing.Rectangle m_Rect;
        System.Drawing.Brush m_bckBrush = System.Drawing.Brushes.White;
        /*
        public MPMotorConfig()
        {
            m_Code = "4X";
            m_imgBackground = null;
            m_bckBrush = System.Drawing.Brushes.Black;
            m_lstMotors = new List<MPMotor>();


            //provo a costruire la 4X
            m_Code = "4X";
            MPMotor mtr = new MPMotor();
            mtr.Index = 0;
            mtr.RectPerc = new System.Drawing.RectangleF((float)0, (float)0, (float)0.25, (float)0.25);
            m_lstMotors.Add(mtr);
            mtr = new MPMotor();
            mtr.Index = 1;
            mtr.RectPerc = new System.Drawing.RectangleF((float)0.75, (float)0.75, (float)0.25, (float)0.25);
            m_lstMotors.Add(mtr);
            mtr = new MPMotor();
            mtr.Index = 2;
            mtr.RectPerc = new System.Drawing.RectangleF((float)0.75, (float)0, (float)0.25, (float)0.25);
            m_lstMotors.Add(mtr);
            mtr = new MPMotor();
            mtr.Index = 3;
            mtr.RectPerc = new System.Drawing.RectangleF((float)0, (float)0.75, (float)0.25, (float)0.25);
            m_lstMotors.Add(mtr);

            SaveConfig(@"C:\MultiPilot4X.mcf");


        }
        */
        public MPMotorConfig(string filepath)
        {
            XmlDocument doc = new XmlDocument();
            string startingpath = "";
            if (System.IO.File.Exists(filepath))
            {
                doc.Load(filepath);
                System.IO.FileInfo fi = new System.IO.FileInfo(filepath);
                startingpath = fi.Directory.FullName;
            }

            XmlNode nd = doc.DocumentElement;

            m_imgBackground = null;
            m_Code = nd.Attributes["code"].Value;
            m_ImageFile = nd.Attributes["background"].Value;
            if ((m_ImageFile != null) && (m_ImageFile.Length > 0))
            {
                string path = System.IO.Path.Combine(startingpath, m_ImageFile);
                if  (System.IO.File.Exists(path))
                    m_imgBackground = new System.Drawing.Bitmap(path);
            }
            foreach (XmlNode ch in nd.ChildNodes)
            {
                MPMotor mtr = new MPMotor(ch);
                m_lstMotors.Add(mtr);
            }
        }

        public void SaveConfig(string filepath)
        {
            XmlDocument doc = new XmlDocument();
            XmlDeclaration decl = doc.CreateXmlDeclaration("1.0", "", "");
            doc.PrependChild(decl);
            XmlNode nd = doc.CreateElement("motorconfig");
            XmlAttribute a1 = doc.CreateAttribute("code");
            a1.Value = m_Code;
            XmlAttribute a2 = doc.CreateAttribute("background");
            a2.Value = m_ImageFile;
            nd.Attributes.Append(a1);
            nd.Attributes.Append(a2);
            doc.AppendChild(nd);

            foreach (MPMotor mtr in m_lstMotors)
            {
                mtr.AppendXml(doc, nd);
            }

            doc.Save(filepath);

        }

        public void Resize(System.Drawing.Rectangle rect)
        {
            m_ContainerRect = rect;
            if (m_imgBackground != null)
            {
                //conservo le proporzioni dell'immagine
                double ratioImage = (double) m_imgBackground.Width / m_imgBackground.Height;
                double ratioCtrl = (double) rect.Width / rect.Height;
                if (ratioCtrl < ratioImage)
                {
                    //devo basarmi sulla larghezza
                    int w = rect.Width;
                    int h = (int)( w / ratioImage );
                    int y = (rect.Height - h) / 2;
                    m_Rect = new System.Drawing.Rectangle(0, y, w, h);
                }
                else 
                {
                    //devo basarmi sull'altezza
                    int h = rect.Height;
                    int w = (int) (h * ratioImage);
                    int x = (rect.Width - w) / 2;
                    m_Rect = new System.Drawing.Rectangle(x, 0, w, h);
                }

            }
            else
            {
                if (rect.Width < rect.Height)
                {
                    int y = rect.Y + (rect.Height - rect.Width) / 2;
                    m_Rect = new System.Drawing.Rectangle(rect.X, y, rect.Width, rect.Width);
                }
                else 
                {
                    int x = rect.X + (rect.Width - rect.Height) / 2;
                    m_Rect = new System.Drawing.Rectangle(x, rect.Y, rect.Height, rect.Height);
                }
            }
            foreach (MPMotor mtr in m_lstMotors)
            {
                mtr.Reposition(m_Rect);
            }
        }

        public bool Draw(System.Drawing.Graphics surf, MPData data)
        {
            bool bRet = false;
            //surf.Clear(System.Drawing.SystemColors.Control);
            if (m_imgBackground != null)
            {
                surf.DrawImage(m_imgBackground, m_Rect);
            }
            else
            {
                surf.FillRectangle(m_bckBrush, m_Rect);
            }
            if (data != null)
                bRet = DrawMotors(surf, data);
            return bRet;
        }

        protected bool DrawMotors(System.Drawing.Graphics surf, MPData data)
        {
            bool bRet = false;
            if ((data.motorConfig == m_Code) && (data.motors != null))
            {
                if (data.motors.Length == m_lstMotors.Count)
                {
                    foreach (MPMotor mtr in m_lstMotors)
                    {
                        mtr.Value = data.motors[mtr.Index];
                        mtr.Draw(surf);
                    }
                    bRet = true;
                }
            }
            return bRet;
        }

        public string Code
        {
            get
            {
                return m_Code;
            }
        }


    }
}
