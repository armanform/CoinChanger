using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;

namespace MonetaPriemnik
{
    public partial class Form1 : Form
    {
        //eger kelgen byte status bolsa flag = true
        static bool isStatus = false;

        //poll jibergen kezde kate wiksa fl;ag kosiladi
        bool errorOccured = false;
        
        //keletin byte-tar
        static byte[] strByte;
        
        // VAJNO!!!bul bukil tusken akwa, osi zat sizge kerek boladi
        static int totalMoney = 0;
        
        // arbir tube-tin indexi boladi, sol indexti real'ni akwaga auistradi
        // sosin osi massivke infoni saktaidi
        // 5 10 20 50
        int[] coinsInTubes = new int[5];

        // status errorlardi text box-ka wigaradi
        static string textBoxStr = null;

        // previous bytes, to check double arrival
        static byte[] prevBytes = null;

        public Form1()
        {
            InitializeComponent();
        }


        // this button opens connection with COM port and run the timer
        private void button1_Click(object sender, EventArgs e)
        {
            serialPort1.PortName = "COM13";
            serialPort1.BaudRate = 38400;
            serialPort1.DataBits = 8;
            serialPort1.StopBits = System.IO.Ports.StopBits.One;
            serialPort1.Parity = System.IO.Ports.Parity.None;

            serialPort1.Open();
            if (serialPort1.IsOpen)
            {
                button1.Enabled = false;
                textBox1.ReadOnly = false;
            }

            //coin changerdi activizuruitetuge
            reInstall();
            Thread.Sleep(2000);
            timer1.Enabled = true;
        }

        //stop serial port and timer and close the app
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (serialPort1.IsOpen) serialPort1.Close();
            if (timer1.Enabled) timer1.Enabled = false;
            if (timer2.Enabled) timer2.Enabled = false;
        }

        // process received data from serial port
        private void serialPort1_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            strByte = new byte[serialPort1.BytesToRead];
            serialPort1.Read(strByte, 0, strByte.Length);

            if (strByte.Length == 0 || strByte == null)
            {
                prevBytes = strByte;
                return;
            }

            if (strByte.Length == 1 && strByte[0] == 0)
            {
                prevBytes = strByte;
                return;
            }
            else if (strByte != null && prevBytes != null)
            {
                if (strByte.Length == prevBytes.Length)
                {
                    if (strByte[0] == prevBytes[0] && strByte[strByte.Length - 1] == prevBytes[prevBytes.Length - 1])
                    {
                        //get the status of coin tubes
                        isStatus = true;
                        byte[] hex = new byte[1] { 0x0A };
                        sendHex(hex, 1);
                    }
                }
            }

            if (strByte.Length != 8)
            {
                isStatus = false;
            }

            if (isStatus == true)
            {
                this.Invoke(new EventHandler(DisplayText));
                isStatus = false;
                checkStatus();
            }
            else
            {
                checkPoll();
                isStatus = true;
                //get the status of coin tubes
                //byte[] hex = new byte[1] { 0x0A };
                //sendHex(hex, 1);
            }
            prevBytes = strByte;
        }

        //check status of each tube,i.e. how much coin on each of them
        private void checkStatus()
        {
            #region checksum
            int checkSum = 0;
            for (int i = 0; i < strByte.Length-1; i++)
            {
                checkSum += strByte[i];
            }

            if (checkSum != strByte[strByte.Length - 1])
            {
                return;
            }
            if (strByte.Length != 8) return;
            #endregion

            checkFirstTwoBytes();

            // tube-tin iwindegi akwalardi sanaidi
            int[] income = { 5,10,20,50,100 };
            for (int i = 0; i <= 4; i++)
            {
                totalMoney += (strByte[i + 2] - coinsInTubes[i]) * income[i];
                coinsInTubes[i] = strByte[i + 2];
            }

            byte[] hex = new byte[1];
            hex[0] = 0x08;
            sendHex(hex, 1);

            this.Invoke(new EventHandler(DisplayLabel));

        }

        //checks the first two bytes of Status bytes
        private void checkFirstTwoBytes()
        {
            if (strByte[0] != 0 || strByte[1] != 0)
            {
                textBox1.AppendText("One of the tube is full! \n");
            }
        }

        //check status of poll to control if coin accepted, error state, etc...
        private void checkPoll()
        {
            
            //convert 1st byte to binary number
            string firstByte = Convert.ToString(strByte[0], 2);
            if (firstByte.Length == 7)
            {
                textBoxStr = "coin accepted";
            }
            else if(firstByte.Length == 8)
            {
                textBoxStr = "coin dispensed";
            }

            string byteToBin = null;
            for (int i = 2; i < strByte.Length; i++)
            {
                byteToBin = Convert.ToString(strByte[i], 2);

                #region error validation
                if (byteToBin == "1")
                {
                    textBoxStr = "Escrow request, hren poimi chto eto";
                    errorOccured = true;
                }
                else if(byteToBin == "10")
                {
                    textBoxStr = "Changer is busy, sdachi berip jatr koroche ";
                    //errorOccured = true;
                }
                else if (byteToBin == "11")
                {
                    textBoxStr = "coin was validated but did not get place ";
                    //errorOccured = true;
                }
                else if (byteToBin == "100")
                {
                    textBoxStr = "Defective tube sensor";
                    errorOccured = true;
                }
                else if (byteToBin == "101")
                {
                    textBoxStr = "double arrival, bir tormz eki tiindi srazu laktirip jiberdi \n";
                    errorOccured = true;
                }
                else if (byteToBin == "110")
                {
                    textBoxStr ="acceptor unplaged, birdene kosilmagan duris\n";
                    errorOccured = true;
                }
                else if (byteToBin == "111")
                {
                    textBoxStr ="zaklinilo, tiin turip kaldi bir jerde \n";
                    errorOccured = true;
                }
                else if (byteToBin == "1000")
                {
                    textBoxStr = "cheksum error \n";
                    errorOccured = true;
                }
                else if (byteToBin == "1001")
                {
                    textBoxStr = "coin routing error \n";
                    errorOccured = true;
                }
                else if (byteToBin == "1010")
                {
                    textBoxStr ="changer busy, prosto busy \n";
                    //errorOccured = true;
                }
                else if (byteToBin == "1011")
                {
                    textBoxStr = "changer was reset \n";
                    //errorOccured = true;
                }
                else if (byteToBin == "1100")
                {
                    textBoxStr ="coin jammed, zastryal \n";
                    errorOccured = true;
                }
                else if (byteToBin == "1101")
                {
                    textBoxStr ="Possible credited coin removal \n";
                    //errorOccured = true;
                }
                #endregion
            }

            this.Invoke(new EventHandler(DisplayText));
        }

        private void reInstall()
        {
            byte[] hex = new byte[1];
            hex[0] = 0x08;
            sendHex(hex, 1);
            Thread.Sleep(1000);
            sendCoinType();
        }

        private void DisplayLabel(object sender, EventArgs e)
        {
            label1.Text = totalMoney.ToString();
        }

        //display received data in text box
        private void DisplayText(object sender, EventArgs e)
        {
            if (strByte != null)
            {
                for (int i = 0; i < strByte.Length; i++)
                {
                    textBox1.AppendText(strByte[i].ToString() + ' ');
                }
            }
            
            textBox1.AppendText("\n");

            if (textBoxStr != null)
            {
                textBox1.AppendText(textBoxStr + "\n");
                textBoxStr = null;
            }
        }

        object e = new object();
        private void sendHex(byte[] hex1, int size)
        {
            lock (e)
            {
                if (serialPort1.IsOpen)
                {
                    serialPort1.Write(hex1, 0, size);
                }
            }
        }

        //clear the text box
        private void button4_Click(object sender, EventArgs e)
        {
            textBox1.Clear();
        }

        //when timer's seconds elapsed, this event handle is activates
        private void timer1_Tick(object sender, EventArgs e)
        {
            byte[] hex = new byte[1] { 0x0B };
            sendHex(hex, 1);
        }


        private void timer2_Tick(object sender, EventArgs e)
        {
            sendCoinType();
        }

        //total moneydi obnulyaitetedi
        private void button3_Click(object sender, EventArgs e)
        {
            totalMoney = 0;
            label1.Text = "";
        }

        //coin typ-ti accept rejimine auistiradi
        private void sendCoinType()
        {
            byte[] hex1 = new byte[5] { 0x0C, 0xFF, 0xFF, 0x00, 0x00 };
            sendHex(hex1, 5);
        }

        //sdachi beredi
        private void button5_Click(object sender, EventArgs e)
        {
            //50 tenge sdachi beredi 0x32 degen 50
            byte[] hex = new byte[3] { 0x0F, 0x02, 0x32 };
            sendHex(hex, 3);
            Thread.Sleep(1000);
            sendCoinType();
        }

    }
}
