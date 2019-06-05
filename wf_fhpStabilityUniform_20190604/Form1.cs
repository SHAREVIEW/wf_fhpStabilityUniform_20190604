using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Timers;

using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace wf_fhpStabilityUniform_20190604
{


    public partial class Form1 : Form
    {
        #region definitions
        private SerialPort sp = new SerialPort(); //声明一个串口类
        private StringBuilder Rev_builder = new StringBuilder();//避免在事件处理方法中反复的创建，定义到外面。
        private StringBuilder Send_builder = new StringBuilder();
        private long received_count = 0;//接收计数  
        private long send_count = 0;//发送计数
                                    //串口发送接收多线程使用
        private Queue<string> SerialSendQueue; // 串口发送命令队列
        private List<byte> SerialRevList;      // 串口接收数据集合
        private ManualResetEvent SerialSendWaiter;// 串口发送线程启动信号
        private ManualResetEvent SerialRevWaiter; // 串口接收数据处理线程启动信号

        #endregion

        public USB usb;
        public Form1()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;   //avoid multiply threads cause error

            usb = new USB();

            refreshPortnameTimer.Elapsed += new System.Timers.ElapsedEventHandler(RefreshPortname);
            refreshPortnameTimer.Start();
        }
        #region MenuItem event
        //graphToolStripMenuItem_Click show
        private void graphDataToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Width = (this.Width == 814) && (tabControl1.SelectedTab == tabPage1) ? 560 : 814;
            tabControl1.SelectedTab = tabPage1;
        }


        #endregion

        #region Form load and close
        // Form1_Load show
        private void Form1_Load(object sender, EventArgs e)
        {
            this.MaximizeBox = false;
            this.Width = 560;

        }
        // Form1_FormClosing 
        private void Form1_FormClosing(object sender, EventArgs e)
        {

        }
        #endregion

        #region  serial port name
        //refresh portname
        System.Timers.Timer refreshPortnameTimer = new System.Timers.Timer(1000);
        private void RefreshPortname(Object sender, System.Timers.ElapsedEventArgs args)
        {
            string[] portnames = SerialPort.GetPortNames();  //获取串口名列表
            ComboBox[] portnameBoxes = { comboBox_SP1, comboBox_SP2, comboBox_SP3, comboBox_SP4, comboBox_SP5, comboBox_SP6, comboBox_SP7, comboBox_SP8 };
            //App.Current.Dispatcher.Invoke(new Action(() => {
            //    foreach (ComboBox combo in portnameBoxes)
            //    {
            //        combo.ItemsSource = portnames;
            //        InitCombBox(combo);
            //    }   // from otdr check red light 
            Invoke(new Action(() =>
            {
                foreach (ComboBox combo in portnameBoxes)
                {
                    InitComboBox(combo);
                }
            }));
        }

        //initial combobox
        private void InitComboBox(ComboBox combo)
        {
            try
            {
                string[] ports = USB.GetPorts(); //SerialPort.GetPortNames();//
                combo.Items.Clear();
                for (int i = 0; i < ports.Length; i++)
                {
                    combo.Items.Add(ports[i]);
                }

                if (ports.Length > 0)
                {
                    // combo.SelectedIndex = ports.Length - 1;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        #endregion

        #region serial open and close
        //check port setting
        private bool CheckPortSetting()//检查串口参数是否非法
        {
            if (comboBox_SP1.Text.Trim() == "") return false;
          //  comboBox_SP1.Enabled = false;
            return true;
        }

        //
        private void SetPortProperty()//设置串口的属性
        {
            //sp = new SerialPort
            //{
            //sp.PortName = (string)comboBox_SP1.SelectedValue;   //
            sp.PortName = comboBox_SP1.Text.Trim();
            sp.BaudRate = 9600;
            sp.StopBits = StopBits.One;
            //sp.Open();
          //  sp.DataReceived += new SerialDataReceivedEventHandler(post_DataReceived);//串口接收处理函数
        }

        //btn open and close event 
        private void btnSP_Click(object sender, EventArgs e)
        {
            if (sp.IsOpen == false)//串口未打开，按钮打开
            {
                if (!CheckPortSetting())//检查串口参数是否非法
                {
                    MessageBox.Show("串口未设置！", "错误提示");
                    return;
                }
               SetPortProperty();//设置串口参数
                try
                {
                    sp.Open();//打开串口
                    this.SerialSendQueue.Clear(); //发送命令队列清空
                    this.SerialSendWaiter.Set();  //启动串口发送线程
                    this.SerialRevWaiter.Set();   //启动串口接收线程
                }
                catch (Exception ex)//串口打开异常处理
                {
                    //捕获到异常信息，创建一个新的sp对象，之前的不能用了
                    sp = new SerialPort();
                    //MessageBox.Show("串口无效或已被占用！", "错误提示");
                    MessageBox.Show(ex.Message, "串口无效或已被占用！", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else//关闭串口
            {
                try
                {
                    sp.Close();//关闭串口
                    //pictureBox1.BackColor = System.Drawing.Color.Red;
                    this.SerialSendQueue.Clear(); //发送命令队列清空
                    this.SerialRevList.Clear();   //接收数据清空
                    this.SerialSendWaiter.Reset();  //停止串口发送线程
                    this.SerialRevWaiter.Reset();   //停止串口接收线程
                }
                catch (Exception ex)//关闭串口异常
                {
                    MessageBox.Show(ex.Message, "串口关闭失败！", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            //设置按钮、图标的状态  
            btnSP.Text = sp.IsOpen ? "Close" : "Open";
            pictureBox1.BackgroundImage = sp.IsOpen ? Properties.Resources.green2015 : Properties.Resources.red2015;
            comboBox_SP1.Enabled = !sp.IsOpen; //串口打开则串口号锁定
            btnSend.Enabled = sp.IsOpen; //串口未打开禁止发送
            cbTimeSend.Enabled = sp.IsOpen;//串口未打开禁止定时发送
        }
        #endregion

        System.Timers.Timer timerSerial = new System.Timers.Timer();
        #region timerSerial
        private void timerSerial_Tick(object sender, EventArgs e)
        {
            try
            {
                timerSerial.Interval = int.Parse(tbxSendTime.Text);//根据定时文本设置定时时间
                btnSend.PerformClick();//生成btnSend按钮的 Click 事件
            }
            catch (Exception)
            {
                timerSerial.Enabled = false;
                MessageBox.Show("错误的定时输入！", "错误提示");
            }
        }


        //Period checkbox
        private void cbTimeSend_CheckedChanged(object sender, EventArgs e)
        {
            timerSerial.Enabled = cbTimeSend.Checked;//选中则打开定时器
        }

        //tbxSendTime_KeyPress
        private void tbxSendTime_KeyPress(object sender, KeyPressEventArgs e)
        {
            //通过正则匹配输入，仅支持数字和退格
            string patten = "[0-9]|\b"; //“\b”：退格键
            Regex r = new Regex(patten);
            Match m = r.Match(e.KeyChar.ToString());

            if (m.Success)
            {
                e.Handled = false;   //没操作“过”，系统会处理事件    
            }
            else
            {
                e.Handled = true;//cancel the KeyPress event
            }
        }
        #endregion

        #region send and receive
        //btnSend event
        private void btnSend_Click(object sender, EventArgs e)
        {

        }
        #endregion
    }
}

