using System;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Drawing;
using System.ComponentModel;

namespace MultiChatServer {
    public partial class Server : Form
    {
        delegate void AppendTextDelegate(Control ctrl, string s);
        Socket mainSock;
        IPAddress thisAddress;
        List<Socket> connectedClients = new List<Socket>();
        string[] problem = File.ReadAllLines(@"C:\Users\zv961\Desktop\Paint_Game.txt", Encoding.Default);
        string[] anslist = new string[12];
        string ans;

        public Server()
        {
            InitializeComponent();
            mainSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            AppendTextDelegate Appender = new AppendTextDelegate(AppendText);
        }
        // 실행
        private void OnFormLoaded(object sender, EventArgs e)
        {
            IPHostEntry he = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress addr in he.AddressList)
            {
                if (addr.AddressFamily == AddressFamily.InterNetwork) {
                    thisAddress = addr;
                    break;
                }    
            }
            if (thisAddress == null) thisAddress = IPAddress.Loopback;
            txtAddress.Text = thisAddress.ToString();
        }
        // 시작 버튼
        private void BeginStartServer(object sender, EventArgs e)
        {
            for(int i = 0; i < problem.Length; i++)
            {
                AppendText(txtHistory, string.Format("{0}", problem[i]));
            }
            int port;
            if (!int.TryParse(txtPort.Text, out port))
            {
                MsgBoxHelper.Error("포트 번호가 잘못 입력되었거나 입력되지 않았습니다.");
                txtPort.Focus();
                txtPort.SelectAll();
                return;
            }
            if (mainSock.IsBound)
            {
                MsgBoxHelper.Warn("서버가 실행중입니다.");
                return;
            }
            IPEndPoint serverEP = new IPEndPoint(thisAddress, port);
            mainSock.Bind(serverEP);
            mainSock.Listen(10);
            mainSock.BeginAccept(AcceptCallback, null);
            AppendText(txtHistory, string.Format("서버가 실행되었습니다."));
        }
        // 보내기 버튼
        private void OnSendData(object sender, EventArgs e)
        {
            if (!mainSock.IsBound)
            {
                MsgBoxHelper.Warn("서버가 실행되고 있지 않습니다!");
                return;
            }
            string tts = txtTTS.Text.Trim();
            if (string.IsNullOrEmpty(tts))
            {
                txtTTS.Focus();
                return;
            }
            byte[] bDts = Encoding.UTF8.GetBytes("1" + '\x01' + "[서버]" + '\x01' + tts);
            for (int i = connectedClients.Count - 1; i >= 0; i--)
            {
                Socket socket = connectedClients[i];
                try { socket.Send(bDts); } catch
                {
                    try { socket.Dispose(); } catch { }
                    connectedClients.RemoveAt(i);
                }
            }
            AppendText(txtHistory, string.Format("[보냄][서버]: {0}", tts));
            txtTTS.Clear();
        }
        // 보내기 버튼 실행키
        private void QuickSend(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) btnSend.PerformClick();
        }
        // 서버 종료시 클라와 연결 끊기
        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            if (mainSock.IsBound)
            {
                byte[] bDts = Encoding.UTF8.GetBytes("0" + '\x01');
                for (int i = connectedClients.Count - 1; i >= 0; i--)
                {
                    Socket socket = connectedClients[i];
                    try { socket.Send(bDts); }
                    catch
                    {
                        try { socket.Dispose(); } catch { }
                        connectedClients.RemoveAt(i);
                    }
                }
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
        // 클라 연결 함수
        void AcceptCallback(IAsyncResult ar)
        {
            Socket client = mainSock.EndAccept(ar);
            mainSock.BeginAccept(AcceptCallback, null);
            AsyncObject obj = new AsyncObject(81920);
            obj.WorkingSocket = client;
            connectedClients.Add(client);
            this.Invoke(new MethodInvoker(delegate () { ClientList.Items.Add(client.RemoteEndPoint); }));
            string iptext = "9" + '\x01';
            for (int i = connectedClients.Count - 1; i >= 0; i--)
                iptext += connectedClients[i].RemoteEndPoint.ToString() + '\x01';
            byte[] lbip = Encoding.UTF8.GetBytes(iptext);
            for (int i = connectedClients.Count - 1; i >= 0; i--)
            {
                Socket socket = connectedClients[i];
                try { socket.Send(lbip); }
                catch
                {
                    try { socket.Dispose(); } catch { }
                    connectedClients.RemoveAt(i);
                }
            }
            AppendText(txtHistory, string.Format("클라이언트 (@ {0})가 연결되었습니다.", client.RemoteEndPoint));
            client.BeginReceive(obj.Buffer, 0, 50000, 0, DataReceived, obj);
        }
        // 데이터 수신 함수
        void DataReceived(IAsyncResult ar)
        {
            AsyncObject obj = (AsyncObject)ar.AsyncState;
            string text = Encoding.UTF8.GetString(obj.Buffer);
            string[] arrDts = text.Split('\x01');
            Random rand = new Random();
            if (arrDts[0] == "1") // 전체톡
            {
                AppendText(txtHistory, string.Format("[받음]{0}: {1}", arrDts[1], arrDts[2]));
                for (int i = connectedClients.Count - 1; i >= 0; i--)
                {
                    Socket socket = connectedClients[i];
                    if (socket != obj.WorkingSocket)
                    {
                        try { socket.Send(obj.Buffer); }
                        catch
                        {
                            try { socket.Dispose(); } catch { }
                            connectedClients.RemoveAt(i);
                        }
                    }
                }
            }
            else if (arrDts[0] == "2") // 게임톡
            {
                int cnt = int.Parse(arrDts[4]);
                int x = 0;
                string sendDt = "";
                if (arrDts[3] == ans)
                {
                    if(anslist[10] != null)
                    {
                        arrDts[0] = "6";
                        arrDts[2] = ans;
                        foreach (string str in arrDts)
                        {
                            sendDt += arrDts[x] + '\x01';
                            x++;
                            if(x == 5)
                            {
                                sendDt += "!" + '\x01';
                            }
                        }
                        byte[] sendByt = Encoding.UTF8.GetBytes(sendDt);
                        for (int j = cnt + 4; j >= 5; j--)
                        {
                            for (int i = connectedClients.Count - 1; i >= 0; i--)
                            {
                                if (arrDts[j].Equals(connectedClients[i].RemoteEndPoint.ToString()))
                                {
                                    Socket socket = connectedClients[i];
                                    try { socket.Send(sendByt); }
                                    catch
                                    {
                                        try { socket.Dispose(); } catch { }
                                        connectedClients.RemoveAt(i);
                                    }
                                    break;
                                }
                            }
                        }
                        ans = "";
                        return;
                    }
                    int ranpbm = 0;
                    int endi = 1;
                    while (endi != 0)
                    {
                        endi = 0;
                        ranpbm = rand.Next(0, problem.Length);
                        for (int j = 1; j < anslist.Length; j++)
                        {
                            if (anslist[j] == null) break;
                            if (problem[ranpbm] == anslist[j]) endi++;
                        }
                    }
                    arrDts[0] = "5";
                    arrDts[2] = ans;
                    arrDts[3] = problem[ranpbm];
                    for (int i = 0; i < anslist.Length; i++)
                    {
                        if (anslist[i] == null)
                        {
                            anslist[i] = arrDts[3];
                            break;
                        }
                    }
                    ans = arrDts[3];
                    foreach (string str in arrDts)
                    {
                        sendDt += arrDts[x] + '\x01';
                        x++;
                    }
                    byte[] sendBt = Encoding.UTF8.GetBytes(sendDt);
                    for (int j = cnt + 4; j >= 5; j--)
                    {
                        for (int i = connectedClients.Count - 1; i >= 0; i--)
                        {
                            if (arrDts[j].Equals(connectedClients[i].RemoteEndPoint.ToString()))
                            {
                                Socket socket = connectedClients[i];
                                try { socket.Send(sendBt); }
                                catch
                                {
                                    try { socket.Dispose(); } catch { }
                                    connectedClients.RemoveAt(i);
                                }
                                break;
                            }
                        }
                    }
                }
                else
                {
                    for (int j = cnt + 4; j >= 5; j--)
                    {
                        for (int i = connectedClients.Count - 1; i >= 0; i--)
                        {
                            if (arrDts[j].Equals(connectedClients[i].RemoteEndPoint.ToString()))
                            {
                                Socket socket = connectedClients[i];
                                if (socket != obj.WorkingSocket)
                                {
                                    try { socket.Send(obj.Buffer); }
                                    catch
                                    {
                                        try { socket.Dispose(); } catch { }
                                        connectedClients.RemoveAt(i);
                                    }
                                }
                                break;
                            }
                        }
                    }
                }
            }
            else if (arrDts[0] == "3") // 그림전송
            {
                int cnt = int.Parse(arrDts[2]);
                for (int j = cnt + 2; j >= 3; j--)
                {
                    for (int i = connectedClients.Count - 1; i >= 0; i--)
                    {
                        if (arrDts[j].Equals(connectedClients[i].RemoteEndPoint.ToString()))
                        {
                            Socket socket = connectedClients[i];
                            if (socket != obj.WorkingSocket)
                            {
                                try { socket.Send(obj.Buffer); }
                                catch
                                {
                                    try { socket.Dispose(); } catch { }
                                    connectedClients.RemoveAt(i);
                                }
                            }
                            break;
                        }
                    }
                }
            }
            else if (arrDts[0] == "4")
            {
                int cnt = int.Parse(arrDts[3]);
                if (arrDts[4] == "3")
                {
                    anslist[0] = cnt + "";
                    int ran = rand.Next(5, cnt + 5);
                    int ranpbm = rand.Next(0, problem.Length);
                    while (arrDts[ran].Equals(arrDts[2]))
                    {
                        ran = rand.Next(5, cnt + 5);
                    }
                    arrDts[2] = arrDts[ran];
                    arrDts[4] = problem[ranpbm];
                    anslist[1] = arrDts[4];
                    ans = arrDts[4];
                    string sendDt = "";
                    int x = 0;
                    foreach (string str in arrDts)
                    {
                        sendDt += arrDts[x] + '\x01';
                        x ++;
                    }
                    byte[] sendBt = Encoding.UTF8.GetBytes(sendDt);
                    for (int j = cnt + 4; j >= 5; j--)
                    {
                        for (int i = connectedClients.Count - 1; i >= 0; i--)
                        {
                            if (arrDts[j].Equals(connectedClients[i].RemoteEndPoint.ToString()))
                            {
                                Socket socket = connectedClients[i];
                                try { socket.Send(sendBt); }
                                catch
                                {
                                    try { socket.Dispose(); } catch { }
                                    connectedClients.RemoveAt(i);
                                }
                                break;
                            }
                        }
                    }
                }
                else
                {
                    for (int j = cnt + 4; j >= 5; j--)
                    {
                        for (int i = connectedClients.Count - 1; i >= 0; i--)
                        {
                            if (arrDts[j].Equals(connectedClients[i].RemoteEndPoint.ToString()))
                            {
                                Socket socket = connectedClients[i];
                                if (socket != obj.WorkingSocket)
                                {
                                    try { socket.Send(obj.Buffer); }
                                    catch
                                    {
                                        try { socket.Dispose(); } catch { }
                                        connectedClients.RemoveAt(i);
                                    }
                                }
                                break;
                            }
                        }
                    }
                }
            }
            else if (arrDts[0] == "5")
            {
                int cnt = int.Parse(arrDts[4]);
                string sendDt = "";
                int x = 0;
                if (anslist[10] != null)
                {
                    arrDts[0] = "6";
                    arrDts[2] = ans;
                    foreach (string str in arrDts)
                    {
                        sendDt += arrDts[x] + '\x01';
                        x++;
                    }
                    byte[] sendByt = Encoding.UTF8.GetBytes(sendDt);
                    for (int j = cnt + 5; j >= 6; j--)
                    {
                        for (int i = connectedClients.Count - 1; i >= 0; i--)
                        {
                            if (arrDts[j].Equals(connectedClients[i].RemoteEndPoint.ToString()))
                            {
                                Socket socket = connectedClients[i];
                                try { socket.Send(sendByt); }
                                catch
                                {
                                    try { socket.Dispose(); } catch { }
                                    connectedClients.RemoveAt(i);
                                }
                                break;
                            }
                        }
                    }
                    ans = "";
                    return;
                }
                int ran = rand.Next(6, cnt + 6);
                int ranpbm = rand.Next(0, problem.Length);
                while (arrDts[ran].Equals(arrDts[1]))
                {
                    ran = rand.Next(6, cnt + 6);
                }
                int endi = 1;
                while (endi != 0)
                {
                    endi = 0;
                    ranpbm = rand.Next(0, problem.Length);
                    for (int j = 1; j < anslist.Length; j++)
                    {
                        if (anslist[j] == null) break;
                        if (problem[ranpbm] == anslist[j]) endi++;
                    }
                }
                arrDts[1] = arrDts[ran];
                arrDts[2] = ans;
                arrDts[3] = problem[ranpbm];
                for(int i = 0; i < anslist.Length; i ++)
                {
                    if (anslist[i] == null)
                    {
                        anslist[i] = arrDts[3];
                        break;
                    }
                }
                ans = arrDts[3];
                foreach (string str in arrDts)
                {
                    sendDt += arrDts[x] + '\x01';
                    x++;
                }
                byte[] sendBt = Encoding.UTF8.GetBytes(sendDt);
                for (int j = cnt + 5; j >= 6; j--)
                {
                    for (int i = connectedClients.Count - 1; i >= 0; i--)
                    {
                        if (arrDts[j].Equals(connectedClients[i].RemoteEndPoint.ToString()))
                        {
                            Socket socket = connectedClients[i];
                            try { socket.Send(sendBt); }
                            catch
                            {
                                try { socket.Dispose(); } catch { }
                                connectedClients.RemoveAt(i);
                            }
                            break;
                        }
                    }
                }
            }
            else if (arrDts[0] == "9") // 클라이언트 리스트 갱신
            {
                this.Invoke(new MethodInvoker(delegate ()
                {
                    for (int i = ClientList.Items.Count - 1; i >= 0; i--)
                    {
                        if (ClientList.Items[i].ToString().Equals(arrDts[1]))
                        {
                            ClientList.Items.Remove(ClientList.Items[i]);
                            connectedClients.Remove(connectedClients[i]);
                            break;
                        }
                    }
                    string iptext = "9" + '\x01';
                    for (int i = connectedClients.Count - 1; i >= 0; i--)
                        iptext += connectedClients[i].RemoteEndPoint.ToString() + '\x01';
                    byte[] lbip = Encoding.UTF8.GetBytes(iptext);
                    for (int i = connectedClients.Count - 1; i >= 0; i--)
                    {
                        Socket socket = connectedClients[i];
                        try { socket.Send(lbip); }
                        catch
                        {
                            try { socket.Dispose(); } catch { }
                            connectedClients.RemoveAt(i);
                        }
                    }
                }));
                obj.WorkingSocket.Close();
                return;
            }
            obj.ClearBuffer();
            obj.WorkingSocket.BeginReceive(obj.Buffer, 0, 50000, 0, DataReceived, obj);
        }
    }
}