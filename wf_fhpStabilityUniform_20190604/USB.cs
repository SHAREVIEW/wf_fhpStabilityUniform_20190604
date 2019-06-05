using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;
using System.Runtime.InteropServices;

namespace wf_fhpStabilityUniform_20190604
{
    public class USB
    {
        //set serial ports
        private System.IO.Ports.SerialPort _spPot1;



        public Boolean SetCom(String comName)
        {

            try
            {
                _spPot1 = new SerialPort(comName, 9600, Parity.None, 8, StopBits.One)
                {
                    ReadTimeout = 2000,
                    WriteTimeout = 1000
                };
                if (!_spPot1.IsOpen)
                {
                    _spPot1.Open();
                }
                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }
        public static string[] GetPorts()
        {
            return SerialPort.GetPortNames();
        }

        public void CloseCom()
        {
            try
            {
                if (_spPot1 != null && _spPot1.IsOpen)
                {
                    _spPot1.Close();
                }
                _spPot1 = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }


        public bool SendData(byte[] send)
        {
            bool flag = false;
            try
            {
                Console.WriteLine(send);
                _spPot1.Write(send, 0, send.Length);
                flag = true;
            }
            catch (Exception)
            {
                MessageBox.Show("串口未打开", "提示");
            }
            return flag;
        }

        public String ReadData()
        {
            try
            {

                int n = _spPot1.BytesToRead;
                byte[] buf = new byte[n + 1];
                //  received_count += n;
                _spPot1.Read(buf, 0, n);     //读取数据
                                             // String SerialIn = System.Text.Encoding.ASCII.GetString(buf, 0, n);    //转码

                StringBuilder recBuffer16 = new StringBuilder();//定义16进制接收缓存  
                for (int i = 0; i < buf.Length; i++)
                {
                    recBuffer16.AppendFormat("{0:X2}" + " ", buf[i]);     //recBuffer16.AppendFormat( "0x" +"{0:X2}" + " ", buf[i]);       X2表示十六进制格式（大写），域宽2位，不足的左边填0。  
                }

                return recBuffer16.ToString();//加显到接收区  

                //return SerialIn;
                //builder.Clear();
                //this.Invoke((EventHandler)(delegate
                //{
                //    //直接按ASCII规则转换成字符串
                //    builder.Append(Encoding.ASCII.GetString(buf));
                //    //}
                //    //追加的形式添加到文本框末端，并滚动到最后。
                //    tB1.AppendText(builder.ToString());
                //    series.Points.AddY(builder.ToString());
                //    //修改接收计数
                //    //label1.Text = "Get:" + received_count.ToString();
                //}));
                //builder.Clear();
                //this.Invoke((EventHandler)(delegate
                //{
                //    //直接按ASCII规则转换成字符串
                //    builder.Append(Encoding.ASCII.GetString(buf));
                //    //}
                //    //追加的形式添加到文本框末端，并滚动到最后。
                //    tB1.AppendText(builder.ToString());
                //    series.Points.AddY(builder.ToString());
                //    //修改接收计数
                //    //label1.Text = "Get:" + received_count.ToString();
                //}));
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                return "";
            }
        }
    }
}
