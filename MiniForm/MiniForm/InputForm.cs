using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Reflection;
using System.Data.OleDb;
using System.Diagnostics;

namespace MiniForm
{
    public partial class InputForm : Form
    {
        OutputForm of = null;
        private static Dictionary<string, OleDbConnection> m_dbConnectionPool = new Dictionary<string, OleDbConnection>();
        public static string m_Octopusconnstring;

        public InputForm()
        {
            InitializeComponent();
        }

        private void HideTimer_Tick(object sender, EventArgs e)
        {
            int a = Control.MousePosition.Y;//光标的在屏幕中的 Y 坐标
            int b = Control.MousePosition.X;//光标的在屏幕中的 X 坐标
            int height = Screen.PrimaryScreen.WorkingArea.Height;//屏幕的高
            int width = Screen.PrimaryScreen.WorkingArea.Width;//屏幕的宽
            int x = this.Left;//窗体的在屏幕中的X坐标
            int y = this.Top;//窗体的在屏幕中的Y坐标
            
            //判断光标是否在窗体内
            if (b >= x && b <= (this.Width + x) && a >= y && a <= (this.Top + this.Height))
            {
                return;
            }
            else
            {   //隐藏窗体
                //if ((x + this.Width) >= width)
                //{
                //    this.Top = this.Top;
                //    this.Left = width - 5;
                //}
                //else if (x <= 0)
                //{
                //    this.Top = this.Top;
                //    this.Left = 5 - this.Width;
                //}
                //else
                if (y <= 0)     //仅贴上边沿时隐藏窗体
                {
                    //this.Left = this.Left;
                    this.Top = 5 - this.Height;
                }
                else
                {
                    return;
                }
            }
        }

        //光标离开窗体
        private void InputForm_MouseLeave(object sender, EventArgs e)
        {
            HideTimer.Start();

        }

        //光标进入窗体
        private void InputForm_MouseEnter(object sender, EventArgs e)
        {
            int height = Screen.PrimaryScreen.WorkingArea.Height;//屏幕的高
            int width = Screen.PrimaryScreen.WorkingArea.Width;//屏幕的宽

            //if (this.Left < 0)
            //{
            //    this.Left = 0;
            //    this.Top = this.Top;
            //    HideTimer.Stop();
            //}
            //else if (this.Left > width - this.Width)
            //{
            //    this.Left = width - this.Width;
            //    this.Top = this.Top;
            //    HideTimer.Stop();
            //}
            //else 
            if (this.Top <= 0)
            {
                this.Left = this.Left;
                this.Top = 0;
                HideTimer.Stop();
            }
            else
            { return; }
        }

        private void button1_Click(object sender, EventArgs e)
        {

            m_Octopusconnstring = "Provider=OraOLEDB.Oracle.1;User ID=XXXX;Password=XXXX;Data Source=(DESCRIPTION = (ADDRESS_LIST= (ADDRESS = (PROTOCOL = TCP)(HOST = xxx.xxx.xxx.xxx)(PORT = xxxx))) (CONNECT_DATA = (SERVICE_NAME = xx)))";

            Point inputFormLocation = this.Location;

            string fundName = textBox1.Text.Trim();

            string sql = @"select l_fundid, vc_code, vc_fullname 
                             from tgcbsnew.tfundinfo@zctg_reader 
                            where vc_fullname like ? ";

            List<OleDbParameter> parameters = new List<OleDbParameter>();

            OleDbParameter fundname1 = new OleDbParameter("fundname1", OleDbType.VarChar, 60);
            fundname1.Value = "%" + fundName + "%";
            parameters.Add(fundname1);

            DataTable dt = GetDBData(m_Octopusconnstring, sql, parameters);
            
            if (dt.Rows.Count > 0)
            {
                of = new OutputForm(dt, inputFormLocation, this.Size.Height);
                of.Show();
            }
        }

        /// <summary>
        /// 回车查询
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="keyData"></param>
        /// <returns></returns>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Enter:
                    {
                        button1_Click(null, null);
                        return true;
                    }

                default:
                    {
                        return base.ProcessCmdKey(ref msg, keyData);
                    }
            }
        }

        //主窗体移动时关闭结果窗口（outputForm）
        private void InputForm_LocationChanged(object sender, EventArgs e)
        {
            if (of != null)
            {
                of.Close();

                of = null;
            }
        }

        public static DataTable GetDBData(string connstring, string sql, List<OleDbParameter> parameters, bool alert = true)
        {
            DataTable dt = new DataTable();
            DataSet ds = new DataSet();

            do
            {
                OleDbConnection conn = GetDBConnection(connstring);
                if (conn == null)
                    break;

                try
                {
                    using (OleDbCommand cmd = new OleDbCommand(sql, conn))
                    {
                        foreach (OleDbParameter para in parameters)
                        {
                            cmd.Parameters.Add(para);
                        }
                        OleDbDataAdapter myCommand = new OleDbDataAdapter(cmd);
                        myCommand.Fill(dt);
                        myCommand.Dispose();
                    }
                    break;
                }
                catch (Exception e)
                {
                    if (conn.State == ConnectionState.Broken || conn.State == ConnectionState.Closed)
                    {
                        bool hr = ResetDBConnection(connstring);
                        if (hr == false)
                            break;
                        else
                            continue;
                    }
                    else
                    {
                        if (alert)
                        {
                            MessageBox.Show(e.ToString() + ",sql:" + sql);
                        }
                        break;
                    }
                }

            } while (true);

            return dt;
        }

        //数据库连接缓存
        public static OleDbConnection GetDBConnection(string connString)
        {
            Debug.Assert(!string.IsNullOrEmpty(connString));

            if (!m_dbConnectionPool.ContainsKey(connString))
            {
                m_dbConnectionPool[connString] = new OleDbConnection(connString);
                try
                {
                    m_dbConnectionPool[connString].Open();
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                    return null;
                }
            }

            return m_dbConnectionPool[connString];
        }
        
        //连接异常时重新建立连接
        public static bool ResetDBConnection(string connString)
        {
            System.Diagnostics.Debug.Assert(!string.IsNullOrEmpty(connString));

            if (!m_dbConnectionPool.ContainsKey(connString))
                return false;

            try
            {
                m_dbConnectionPool[connString].Close();
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.Print(ex.ToString());
            }

            m_dbConnectionPool[connString] = new OleDbConnection(connString);

            try
            {
                m_dbConnectionPool[connString].Open();
            }
            catch (System.Exception ex)
            {
                //MessageBox.Show(ex.ToString());
                MessageBox.Show("数据库连接不上，请关闭后重新打开程序");
                return false;
            }

            return true;
        }
    }
}
