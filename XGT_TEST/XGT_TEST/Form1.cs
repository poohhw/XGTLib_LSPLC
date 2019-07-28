using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using XGTLib;

namespace XGT_TEST
{
    public partial class Form1 : Form
    {
        private XGTClass xgt;
        public Form1()
        {
            InitializeComponent();
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            Connect();
        }

        private void Connect()
        {
            string vIP = txtIP.Text.ToString();
            int vPort = Convert.ToInt32(txtPort.Text);

            xgt = new XGTClass(vIP, vPort);
            xgt.Connect();

            if (xgt.Connected)
            {
                txtLog.Text = txtLog.Text.Insert(0, "연결 되었습니다." + Environment.NewLine);
            }
        }

        private void DisConnect()
        {
            if(xgt != null)
            {
                xgt.Disconnect();
            }
        }

        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            if (xgt != null) xgt.Disconnect();

            if (!xgt.Connected)
            {
                txtLog.Text = txtLog.Text.Insert(0, "연결 종료 되었습니다." + Environment.NewLine);
            }

        }

        private void btnASCII_Click(object sender, EventArgs e)
        {
            string[] hexs = txtText.Text.Split(new string[] { "-" }, StringSplitOptions.None);

            foreach (string hex in hexs)
            {
                int value = Convert.ToInt32(hex, 16);

                string stringValue = Char.ConvertFromUtf32(value);
                char charValue = (char)value;

                txtResult.Text += charValue.ToString();
            }

        }



        private void btnRead_Click(object sender, EventArgs e)
        {
            Connect();

            XGTData vData = new XGTData();

            List<XgtAddressData> lstAdress = new List<XgtAddressData>();
            string[] ArrayAdress = txtAdress.Text.Split(new string[] { ";", "," }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string item in ArrayAdress)
            {               
                
                XgtAddressData addrData = new XgtAddressData();
                addrData.Address = item;
                lstAdress.Add(addrData);
            }

            if (lstAdress.Count == 0)
            {
                txtLog.Text = "메모리 주소를 입력하세요.";
                txtAdress.Text = string.Empty;
                txtAdress.Focus();
                return;
            }

            if (rd1.Checked)  //개별
            {
                vData = xgt.Read(XGT_DataType.Word, lstAdress, XGT_MemoryType.DataRegister, 1);

                txtTX.Text = vData.TXstring;
                txtRX.Text = vData.RXstring;
                txtLog.Text = vData.Message;

            }

            if (rd2.Checked)  //연속
            {
                if (lstAdress.Count > 1)
                {
                    txtLog.Text = "연속읽기는 메모리 시작 번지 1개만 입력하세요.";
                    txtAdress.Text = string.Empty;
                    txtAdress.Focus();
                    return;
                }

                int vDataCount = 0;
                if (!int.TryParse(txtCount.Text, out vDataCount))
                {
                    txtLog.Text = "카운터는 숫자형식 이여야 합니다.";
                    txtCount.Text = string.Empty;
                    txtCount.Focus();
                    return;
                }

                vData = xgt.Read(XGT_DataType.Continue, lstAdress, XGT_MemoryType.DataRegister, 1, vDataCount);
                txtTX.Text = vData.TXstring;
                txtRX.Text = vData.RXstring;
                txtLog.Text = vData.Message;

            }


            txtRead.Text = vData.BlockCount.ToString() + Environment.NewLine;
            txtRead.Text += vData.DataType.ToString() + Environment.NewLine;
            txtRead.Text += vData.ResponseStatus.ToString() + Environment.NewLine;
            txtRead.Text += vData.ResponseType.ToString() + Environment.NewLine;
            txtRead.Text += vData.NAK_ErrorCotent.ToString() + Environment.NewLine;
            txtRead.Text += vData.Message.ToString() + Environment.NewLine;
            txtRead.Text += "<데이터 정보>"+ Environment.NewLine;
            foreach (var item in vData.DataList)
            {
                txtRead.Text += item.Data.ToString() + Environment.NewLine;
            }

            DisConnect();

        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            txtCount.Enabled = false;
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            txtCount.Enabled = true;
        }

        private void button2_Click(object sender, EventArgs e)
        {

        }

        private void button3_Click(object sender, EventArgs e)
        {
            xgt.Read2();
        }

        private void btnWrite_Click(object sender, EventArgs e)
        {
            Write(txtInputValue.Text);
        }

        private void Write(string pContent)
        {
            Connect();
            XGTData vData = new XGTData();
            List<XgtAddressData> lstAdress = new List<XgtAddressData>();
            string[] ArrayAdress = txtAdress.Text.Split(new string[] { ";", "," }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string item in ArrayAdress)
            {
                XgtAddressData addrData = new XgtAddressData();
                addrData.Address = item;
                addrData.Data = pContent;  //입력데이터

                lstAdress.Add(addrData);
            }


            string vMessage = string.Empty;

            if (rd1.Checked)
            {
                vData = xgt.Write(XGT_DataType.Word, lstAdress, XGT_MemoryType.DataRegister, 1);

                txtTX.Text = vData.TXstring;
                txtRX.Text = vData.RXstring;
                txtLog.Text = vData.Message;
            }

            if (rd2.Checked)
            {
                if (lstAdress.Count > 1)
                {
                    txtLog.Text = "연속쓰기는 메모리 시작 번지 1개만 입력하세요.";
                    txtAdress.Text = string.Empty;
                    txtAdress.Focus();
                    return;
                }

                int vDataCount = 0;
                if (!int.TryParse(txtCount.Text, out vDataCount))
                {
                    txtLog.Text = "카운터는 숫자형식 이여야 합니다.";
                    txtCount.Text = string.Empty;
                    txtCount.Focus();
                    return;
                }

                vData = xgt.Write(XGT_DataType.Continue, lstAdress, XGT_MemoryType.DataRegister, 1, vDataCount);
                txtTX.Text = vData.TXstring;
                txtRX.Text = vData.RXstring;
                txtLog.Text = vData.Message;

            }

            txtWrite.Text = vData.BlockCount.ToString() + Environment.NewLine;
            txtWrite.Text += vData.DataType.ToString() + Environment.NewLine;
            txtWrite.Text += vData.ResponseStatus.ToString() + Environment.NewLine;
            txtWrite.Text += vData.ResponseType.ToString() + Environment.NewLine;
            txtWrite.Text += vData.NAK_ErrorCotent.ToString() + Environment.NewLine;
            txtWrite.Text += vData.Message.ToString() + Environment.NewLine;
            txtWrite.Text += "<데이터 정보>" + Environment.NewLine;
            foreach (var item in vData.DataList)
            {
                txtWrite.Text += item.Data.ToString() + Environment.NewLine;
            }

            DisConnect();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //if (xgt == null)
            //{
            //    string vIP = txtIP.Text.ToString();
            //    int vPort = Convert.ToInt32(txtPort.Text);

            //    xgt = new XGTClass(vIP, vPort);
            //    xgt.Connect();
            //}
            //if (!xgt.Connected)
            //{
            //    xgt = null;
            //    string vIP = txtIP.Text.ToString();
            //    int vPort = Convert.ToInt32(txtPort.Text);
            //    xgt = new XGTClass(vIP, vPort);
            //    xgt.Connect();
            //}

            //int nAddress = 0;

            //if (!int.TryParse(txtAdress.Text, out nAddress))
            //{
            //    txtLog.Text = "주소값은 숫자형식 이여야 합니다.";
            //    txtAdress.Text = string.Empty;
            //    txtAdress.Focus();
            //    return;
            //}


            //string vMessage = string.Empty;

            //if (rd1.Checked)
            //{
            //    for (int i = 0; i < 20; i++)
            //    {

            //         byte[] result = xgt.Write(XGT_DataType.Word, nAddress, XGT_MemoryType.DataRegister, 1, out vMessage,0,txtInputValue.Text);

            //        txtLog.Text = BitConverter.ToString(result);

            //        if (!vMessage.Equals("OK"))
            //        {
            //            txtLog.Text += vMessage + Environment.NewLine;
            //        }

            //        nAddress++;
            //    }
            //}

            //if (rd2.Checked)
            //{
            //    int vDataCount = 0;
            //    if (!int.TryParse(txtCount.Text, out vDataCount))
            //    {
            //        txtLog.Text = "카운터는 숫자형식 이여야 합니다.";
            //        txtCount.Text = string.Empty;
            //        txtCount.Focus();
            //        return;
            //    }

            //    byte[] result = xgt.Read(XGT_DataType.ContinueRead, nAddress, XGT_MemoryType.DataRegister, 1, out vMessage, vDataCount);

            //    txtLog.Text = BitConverter.ToString(result);

            //    if (!vMessage.Equals("OK"))
            //    {
            //        txtLog.Text = vMessage;
            //    }
            //}

            //xgt.Read1();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            //비트읽기
            Connect();
            XGTData vData = new XGTData();

            List<XgtAddressData> lstAdress = new List<XgtAddressData>();
            string[] ArrayAdress = txtAdress.Text.Split(new string[] { ";", "," }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string item in ArrayAdress)
            {                
                XgtAddressData addrData = new XgtAddressData();
                addrData.Address = item;
                lstAdress.Add(addrData);
            }

            if (lstAdress.Count == 0)
            {
                txtLog.Text = "메모리 주소를 입력하세요.";
                txtAdress.Text = string.Empty;
                txtAdress.Focus();
                return;
            }

            if (rd1.Checked)  //개별
            {
                vData = xgt.Read(XGT_DataType.Bit, lstAdress, XGT_MemoryType.SubRelay, 1);

                txtTX.Text = vData.TXstring;
                txtRX.Text = vData.RXstring;
                txtLog.Text = vData.Message;

            }

            if (rd2.Checked)  //연속
            {
                if (lstAdress.Count > 1)
                {
                    txtLog.Text = "연속읽기는 메모리 시작 번지 1개만 입력하세요.";
                    txtAdress.Text = string.Empty;
                    txtAdress.Focus();
                    return;
                }

                int vDataCount = 0;
                if (!int.TryParse(txtCount.Text, out vDataCount))
                {
                    txtLog.Text = "카운터는 숫자형식 이여야 합니다.";
                    txtCount.Text = string.Empty;
                    txtCount.Focus();
                    return;
                }

                vData = xgt.Read(XGT_DataType.Continue, lstAdress, XGT_MemoryType.DataRegister, 1, vDataCount);
                txtTX.Text = vData.TXstring;
                txtRX.Text = vData.RXstring;
                txtLog.Text = vData.Message;

            }

            DisConnect();

            txtRead.Text = vData.BlockCount.ToString() + Environment.NewLine;
            txtRead.Text += vData.DataType.ToString() + Environment.NewLine;
            txtRead.Text += vData.ResponseStatus.ToString() + Environment.NewLine;
            txtRead.Text += vData.ResponseType.ToString() + Environment.NewLine;
            txtRead.Text += vData.NAK_ErrorCotent.ToString() + Environment.NewLine;
            txtRead.Text += vData.Message.ToString() + Environment.NewLine;
            txtRead.Text += "<데이터 정보>" + Environment.NewLine;
            foreach (var item in vData.DataList)
            {
                txtRead.Text += item.Data.ToString() + Environment.NewLine;
            }

        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            //BIT 쓰기
            Connect();
            XGTData vData = new XGTData();
            List<XgtAddressData> lstAdress = new List<XgtAddressData>();
            string[] ArrayAdress = txtAdress.Text.Split(new string[] { ";", "," }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string item in ArrayAdress)
            {
                XgtAddressData addrData = new XgtAddressData();
                addrData.Address = item;
                addrData.Data = txtInputValue.Text.ToString();  //입력데이터

                lstAdress.Add(addrData);
            }


            string vMessage = string.Empty;

            if (rd1.Checked)
            {
                vData = xgt.Write(XGT_DataType.Bit, lstAdress, XGT_MemoryType.SubRelay, 1);

                txtTX.Text = vData.TXstring;
                txtRX.Text = vData.RXstring;
                txtLog.Text = vData.Message;
            }

            if (rd2.Checked)
            {
                if (lstAdress.Count > 1)
                {
                    txtLog.Text = "연속쓰기는 메모리 시작 번지 1개만 입력하세요.";
                    txtAdress.Text = string.Empty;
                    txtAdress.Focus();
                    return;
                }

                int vDataCount = 0;
                if (!int.TryParse(txtCount.Text, out vDataCount))
                {
                    txtLog.Text = "카운터는 숫자형식 이여야 합니다.";
                    txtCount.Text = string.Empty;
                    txtCount.Focus();
                    return;
                }

                vData = xgt.Write(XGT_DataType.Continue, lstAdress, XGT_MemoryType.DataRegister, 1, vDataCount);
                txtTX.Text = vData.TXstring;
                txtRX.Text = vData.RXstring;
                txtLog.Text = vData.Message;

            }

            DisConnect();

            txtWrite.Text = vData.BlockCount.ToString() + Environment.NewLine;
            txtWrite.Text += vData.DataType.ToString() + Environment.NewLine;
            txtWrite.Text += vData.ResponseStatus.ToString() + Environment.NewLine;
            txtWrite.Text += vData.ResponseType.ToString() + Environment.NewLine;
            txtWrite.Text += vData.NAK_ErrorCotent.ToString() + Environment.NewLine;
            txtWrite.Text += vData.Message.ToString() + Environment.NewLine;
            txtWrite.Text += "<데이터 정보>" + Environment.NewLine;
            foreach (var item in vData.DataList)
            {
                txtWrite.Text += item.Data.ToString() + Environment.NewLine;
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < 1000; i++)
            {
                Write(i.ToString());
                //Thread.Sleep(500);
            }
        }
    }
}
