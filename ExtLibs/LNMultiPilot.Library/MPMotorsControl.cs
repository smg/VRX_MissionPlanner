using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace LNMultiPilot.Library
{
    public class MPMotorsControl : System.Windows.Forms.Control
    {

        Dictionary<string, MPMotorConfig> m_tblConfigs = new Dictionary<string, MPMotorConfig>();
        MPData m_lastData = null;
        MPMotorConfig m_currConfig = null;

        public MPMotorsControl()
        {
            //InitializeComponent();
            // Double bufferisation
            SetStyle(ControlStyles.DoubleBuffer | ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint, true);
        }

        public int LoadConfig(string path)
        {
            m_tblConfigs.Clear();
            if (System.IO.Directory.Exists(path))
            {
                System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(path);
                foreach (System.IO.FileInfo fi in di.GetFiles("*.mcf"))
                {
                    MPMotorConfig cfg = new MPMotorConfig(fi.FullName);
                    cfg.Resize(ClientRectangle);
                    m_tblConfigs.Add(cfg.Code, cfg);
                }
            }
            return m_tblConfigs.Count;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (m_currConfig != null)
            {
                m_currConfig.Draw(e.Graphics, m_lastData);
            }
            else 
            {
                e.Graphics.DrawString("Motor graphic configuration not valid", this.Font, System.Drawing.Brushes.Black, new PointF(0, 0));
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (m_currConfig != null)
            {
                m_currConfig.Resize(this.ClientRectangle);
            }
        }

        public void Update(MPData data)
        {
            m_lastData = data;
            if (m_tblConfigs.ContainsKey(m_lastData.motorConfig))
            {
                MPMotorConfig cfg = m_tblConfigs[m_lastData.motorConfig];
                if (cfg != m_currConfig)
                {
                    cfg.Resize(this.ClientRectangle);
                    m_currConfig = cfg;
                }
            }
            else 
            {
                m_currConfig = null;
            }
            this.Refresh();
        }
    }
}
