using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.ComponentModel;
using System.Drawing;

namespace MultiChatClient {
    public partial class Client : Form {
        delegate void AppendTextDelegate(Control ctrl, string s);
        Socket mainSock;
        Paint_Game pg;
        bool ServerConnected = false;
        public string recvDt = "";
        public string pictureDt = "";
        public int sendkind;

        public Client()
        {
            InitializeComponent();
            mainSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            AppendTextDelegate Appender = new AppendTextDelegate(AppendText);
        }
        // 실행
        public void OnFormLoaded(object sender, EventArgs e)
        {
            IPHostEntry he = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress defaultHostAddress = null;
            foreach (IPAddress addr in he.AddressList)
            {
                if (addr.AddressFamily == AddressFamily.InterNetwork)
                {
                    defaultHostAddress = addr;
                    break;
                }
            }
            if (defaultHostAddress == null)
                defaultHostAddress = IPAddress.Loopback;
            txtAddress.Text = defaultHostAddress.ToString();
        }
        // 연결 버튼
        public void OnConnectToServer(object sender, EventArgs e)
        {
            if (ServerConnected)
            {
                MsgBoxHelper.Error("이미 연결되어 있습니다!");
                return;
            }
            int port;
            if (!int.TryParse(txtPort.Text, out port))
            {
                MsgBoxHelper.Error("포트 번호가 잘못 입력되었거나 입력되지 않았습니다.");
                txtPort.Focus();
                txtPort.SelectAll();
                return;
            }
            try
            {
                mainSock.Connect(txtAddress.Text, port);
                AppendText(txtHistory, "서버와 연결되었습니다.");
                ServerConnected = true;
                AsyncObject obj = new AsyncObject(50000);
                obj.WorkingSocket = mainSock; 
                mainSock.BeginReceive(obj.Buffer, 0, obj.BufferSize, 0, DataReceived, obj);
            }
            catch (Exception ex)
            {
                MsgBoxHelper.Error("연결에 실패했습니다!\n오류 내용: {0}", MessageBoxButtons.OK, ex.Message);
                return;
            }
        }
        // 보내기 버튼
        public void OnSendData(object sender, EventArgs e)
        {
            if (!mainSock.IsBound)
                return;
            string tts = txtTTS.Text.Trim();
            if (string.IsNullOrEmpty(tts))
            {
                txtTTS.Focus();
                return;
            }
            IPEndPoint ip = (IPEndPoint) mainSock.LocalEndPoint;
            byte[] bDts = Encoding.UTF8.GetBytes("1" + '\x01' + ip.ToString() + '\x01' + tts);
            mainSock.Send(bDts);
            AppendText(txtHistory, string.Format("[보냄]{0}: {1}", ip.ToString(), tts));
            txtTTS.Clear();
        }
        // 보내기 버튼 실행키
        private void QuickSend(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) btnSend.PerformClick();
        }
        // 그림 맞추기방 버튼
        private void OpenPaintGame(object sender, EventArgs e)
        {

            if (ClientList.CheckedItems.Count == 0)
            {
                MsgBoxHelper.Error("사용자를 선택해주세요.");
                return;
            }
            for (int i = ClientList.CheckedItems.Count - 1; i >= 0; i--)
            {
                if (mainSock.LocalEndPoint.ToString().Equals(ClientList.CheckedItems[i].ToString()))
                {
                    MsgBoxHelper.Error("본인을 제외한 사용자를 선택해주세요.");
                    return;
                }
            }
            pg = new Paint_Game(this, mainSock.LocalEndPoint.ToString());
            pg.Show();
        }
        // 종료 시 연결 종료
        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            if (mainSock.Connected)
            {
                byte[] closeInfo = Encoding.UTF8.GetBytes("9" + '\x01' + mainSock.LocalEndPoint.ToString() + '\x01');
                mainSock.Send(closeInfo);
                return;
            }
        }
        // 다른 창에서 메세지 보내는 함수
        public void TalkSend()
        {
            if (sendkind == 1)
            {
                byte[] bDts = Encoding.UTF8.GetBytes("2" + '\x01' + mainSock.LocalEndPoint.ToString() + '\x01' + recvDt);
                recvDt = "";
                mainSock.Send(bDts);
            }
            else if (sendkind == 2)
            {
                Byte[] bDts = Encoding.UTF8.GetBytes("3" + '\x01' + pictureDt + '\x01' + recvDt);
                pictureDt = "";
                recvDt = "";
                mainSock.Send(bDts);
            }
            else if (sendkind == 3)
            {
                byte[] bDts = Encoding.UTF8.GetBytes("4" + '\x01' + mainSock.LocalEndPoint.ToString() + '\x01' + recvDt);
                recvDt = "";
                mainSock.Send(bDts);
            }
            else if (sendkind == 4)
            {
                byte[] bDts = Encoding.UTF8.GetBytes("5" + '\x01' + mainSock.LocalEndPoint.ToString() + '\x01' + recvDt);
                recvDt = "";
                mainSock.Send(bDts);
            }
        }
        // 텍스트 붙이는 함수
        void AppendText(Control ctrl, string s)
        {
            this.Invoke(new MethodInvoker(delegate ()
            {
                ctrl.Text += Environment.NewLine + s;
                txtHistory.SelectionStart = txtHistory.Text.Length;
                txtHistory.ScrollToCaret();
            }));
        }
        // 데이터 수신 함수
        void DataReceived(IAsyncResult ar)
        {
            AsyncObject obj = (AsyncObject)ar.AsyncState;
            string text = Encoding.UTF8.GetString(obj.Buffer);
            string[] arrDts = text.Split('\x01');
            if (arrDts[0] == "1") // 전체 채팅
                AppendText(txtHistory, string.Format("[받음]{0}: {1}", arrDts[1], arrDts[2]));
            else if (arrDts[0] == "2") // 그림방 채팅
            {
                int cnt = int.Parse(arrDts[4]);
                this.Invoke(new MethodInvoker(delegate ()
                {
                    foreach (Form OpenForm in Application.OpenForms)
                    {
                        if (OpenForm.Text == "Drawing Catch ( " + cnt + " 명 )")
                        {
                            pg.recvChat = text;
                            return;
                        }
                    }  
                    pg = new Paint_Game(this, mainSock.LocalEndPoint.ToString());
                    recvDt = text;
                    pg.Show();
                }));
            }
            else if (arrDts[0] == "3") // 그림 받기
            {
                int cnt = int.Parse(arrDts[2]);
                this.Invoke(new MethodInvoker(delegate ()
                {
                    foreach (Form OpenForm in Application.OpenForms)
                    {
                        if (OpenForm.Text == "Drawing Catch ( " + cnt + " 명 )")
                        {
                            Byte[] Img = Convert.FromBase64String(arrDts[1]);
                            TypeConverter tc = TypeDescriptor.GetConverter(typeof(Bitmap));
                            Bitmap Imgbit = (Bitmap)tc.ConvertFrom(Img);
                            pg.recvPicture = Imgbit;
                            return;
                        }
                    }
                    pg = new Paint_Game(this, mainSock.LocalEndPoint.ToString());
                    pg.Show();
                }));
            }
            else if (arrDts[0] == "4")
            {
                int cnt = int.Parse(arrDts[3]);
                this.Invoke(new MethodInvoker(delegate ()
                {
                    foreach (Form OpenForm in Application.OpenForms)
                    {
                        if (OpenForm.Text == "Drawing Catch ( " + cnt + " 명 )")
                        {
                            if(arrDts[4] == "1" || arrDts[4] == "2")
                            {
                                pg.recvState = text;
                                return;
                            }
                            else
                            {
                                pg.recvStart = text;
                                return;
                            }
                        }
                    }
                    pg = new Paint_Game(this, mainSock.LocalEndPoint.ToString());
                    recvDt = text;
                    pg.Show();
                }));
            }
            else if (arrDts[0] == "5")
            {
                int cnt = int.Parse(arrDts[4]);
                this.Invoke(new MethodInvoker(delegate ()
                {
                    foreach (Form OpenForm in Application.OpenForms)
                    {
                        if (OpenForm.Text == "Drawing Catch ( " + cnt + " 명 )")
                        {
                            pg.recvCollect = text;
                            return;
                        }
                    }
                    pg = new Paint_Game(this, mainSock.LocalEndPoint.ToString());
                    recvDt = text;
                    pg.Show();
                }));
            }
            else if (arrDts[0] == "6")
            {
                int cnt = int.Parse(arrDts[4]);
                this.Invoke(new MethodInvoker(delegate ()
                {
                    foreach (Form OpenForm in Application.OpenForms)
                    {
                        if (OpenForm.Text == "Drawing Catch ( " + cnt + " 명 )")
                        {
                            pg.recvEnd = text;
                            return;
                        }
                    }
                    pg = new Paint_Game(this, mainSock.LocalEndPoint.ToString());
                    recvDt = text;
                    pg.Show();
                }));
            }
            else if (arrDts[0] == "9") // 클라이언트 참여 및 퇴장
            {
                this.Invoke(new MethodInvoker(delegate () { ClientList.Items.Clear(); }));
                for (int i = arrDts.Length - 2; i >= 1; i--)
                    this.Invoke(new MethodInvoker(delegate () { ClientList.Items.Add(arrDts[i]); }));
            }
            else if (arrDts[0] == "0") // 서버 종료
            {
                mainSock.Disconnect(true);
                this.Invoke(new MethodInvoker(delegate () { ClientList.Items.Clear(); }));
                AppendText(txtHistory, string.Format("서버와의 연결이 종료되었습니다."));
                ServerConnected = false;
                obj.ClearBuffer();
                mainSock.Close();
                mainSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
                return;
            }
            obj.ClearBuffer();
            obj.WorkingSocket.BeginReceive(obj.Buffer, 0, 50000, 0, DataReceived, obj);
        }
    }
}