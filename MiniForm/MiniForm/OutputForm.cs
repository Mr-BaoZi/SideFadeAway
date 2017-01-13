using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MiniForm
{
    public partial class OutputForm : Form
    {
        public OutputForm()
        {
            InitializeComponent();
        }


        public OutputForm(DataTable dt, Point location, int height)
        {
            InitializeComponent();
            
            this.LostFocus += new System.EventHandler(OutputForm_LostFocus);

            location.Y += height;
            this.StartPosition = FormStartPosition.Manual;
            this.Location = location;

            DrawListView(dt);
            
            this.Show();
        }

        public void DrawListView(DataTable dt)
        {
            ListViewItem lvItem = null;

            foreach (DataRow dr in dt.Rows)
            {
                lvItem = lvResult.Items.Add(dr["l_fundid"].ToString());
                lvItem.SubItems.Add(dr["vc_code"].ToString());
                lvItem.SubItems.Add(dr["vc_fullname"].ToString());
            }
        }

        void OutputForm_LostFocus(object sender, System.EventArgs e)
        {
            this.Hide();
        }
                    
    }
}
