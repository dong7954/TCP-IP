using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing.Drawing2D; //pen 색깔, 굵기, 시작부분, 끝부분 관련 네임스페이스
using System.IO;

namespace MultiChatClient
{
    public partial class Paint_Game : Form
    {
        Client cliForm;
        Image recvpic;
        string recvcht, recvchk, recvsta, recvcol, recved;
        string connectUser;
        int lx, ly;
        Color pColor = Color.Black; //시작 색 검정색
        int pSize = 5; //시작 펜 굵기
        int pBar = 0;

        public Paint_Game(Client clF, string recvip)
        {
            InitializeComponent();
            this.cliForm = clF;
            connectUser = recvip;
            label1.BackColor = pColor;
            button1.Enabled = false;
        }
        // 실행
        private void Paint_Game_Load(object sender, EventArgs e)
        {
            Answer.Text = "";
            if (cliForm.recvDt == "")
            {
                int cnt = cliForm.ClientList.CheckedItems.Count;
                ClientBox.Items.Add(connectUser);
                this.Text = "Drawing Catch ( " + (cnt + 1) + " 명 )";
                for (int i = cnt - 1; i >= 0; i--)
                    ClientBox.Items.Add(cliForm.ClientList.CheckedItems[i].ToString());
                for (int i = cliForm.ClientList.Items.Count - 1; i >= 0; i--)
                    cliForm.ClientList.SetItemCheckState(i, CheckState.Unchecked);
                string sendDt = connectUser + '\x01' + "게임방이 생성되었습니다." + '\x01' + ClientBox.Items.Count.ToString() + '\x01';
                foreach (string cliList in ClientBox.Items)
                    sendDt += cliList + '\x01';
                cliForm.recvDt = sendDt;
                AppendText(txtHistory, string.Format("게임방이 생성되었습니다."));
                cliForm.sendkind = 1;
                cliForm.TalkSend();
            }
            else
            {
                string[] arrDts = cliForm.recvDt.Split('\x01');;
                int cnt = int.Parse(arrDts[4]);
                this.Text = "Drawing Catch ( " + cnt + " 명 )";
                for (int i = cnt + 4; i >= 5; i--)
                    ClientBox.Items.Add(arrDts[i]);
                AppendText(txtHistory, string.Format("{0}", arrDts[3]));
                cliForm.recvDt = "";
            }
        }
        // 보내기 실행 기능
        private void answerBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) btnSend.PerformClick();
        }
        // 타이머
        private void timer1_Tick(object sender, EventArgs e)
        {
            pBar++;
            progressBar1.Value = pBar;
            if (pBar == 120)
            {
                if(Answer.Text != "")
                {
                    string sendDt = connectUser + '\x01' + Answer.Text + '\x01' + ClientBox.Items.Count.ToString() + '\x01' + "~" + '\x01';
                    foreach (string cliList in ClientBox.Items)
                        sendDt += cliList + '\x01';
                    cliForm.recvDt = sendDt;
                    cliForm.sendkind = 4;
                    cliForm.TalkSend();
                    Answer.Text = "";
                    pBar = 0;
                }
            }
        }
        // 펜 버튼
        private void button1_Click(object sender, EventArgs e)
        {
            pColor = colorDialog1.Color;//펜 색을 현재 칼라 다이얼로그 색으로 변환
            button1.Enabled = false;    //펜 버튼 비활성화
            button2.Enabled = true;     //지우개 버튼 비활성화

        }
        // 지우개 버튼
        private void button2_Click(object sender, EventArgs e)
        {
            pColor = Color.White;       //펜 색을 흰색으로 바꿈(지우개)
            button2.Enabled = false;    //지우개 버튼 비활성화
            button1.Enabled = true;     //펜 버튼 활성화
        }
        // 리셋 버튼
        private void btn_Reset_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = null;   //리셋 버튼 누르면 픽쳐박스 이미지 초기화
        }
        // 색 선택
        private void pen_Color_Click(object sender, EventArgs e)
        {
            ColorDialog colorDialog = new ColorDialog(); //ColorDialog에서 선택한 값을 pc에 저장함
            if (colorDialog.ShowDialog() == DialogResult.OK)
            {
                pColor = colorDialog.Color; //사용자들이 현재 색을 알수있게 지정해줌
                label1.BackColor = pColor;
            }
        }
        // 펜 사이즈 조절
        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            pSize = trackBar1.Value;    //트랙바 값에 따라서 펜 사이즈(굵기)변경
        }
        // 그릴 때 시작 점 찾기
        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) // 마우스 어느 버튼이 눌러 졌는지 체크 (왼쪽)
            {
                lx = e.X; // 마우스 클릭한 상태에서 이동하는 실시간 좌표 값 저장
                ly = e.Y;
            }
        }
        // 그리기 기능
        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && Answer.Text != "")
            {
                Pen P = new Pen(pColor, pSize); // Pen의 색깔 및 굵기 설정
                P.StartCap = P.EndCap = LineCap.Round; // 선의 시작 및 끝 부분 처리, Capture 현재 작업영역 내부에 있는지를 실시간으로 감지 하여 이상한 현상을 방지 한다.
                Graphics G = pictureBox1.CreateGraphics();
                G.DrawLine(P, lx, ly, e.X, e.Y); // 시작과 끝점을 저장
                lx = e.X;
                ly = e.Y;
                G.Dispose();
            }
        }
        // 그린거 이미지화
        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            if (Answer.Text != "")
            {
                var bitmap = new Bitmap(500, 408);
                Graphics g = Graphics.FromImage(bitmap);
                g.CopyFromScreen(new Point(this.Location.X + 21, this.Location.Y + 106), new Point(0, 0), this.pictureBox1.Size);
                this.pictureBox1.Image = bitmap;
                var Imstr = new MemoryStream();
                this.pictureBox1.Image.Save(Imstr, System.Drawing.Imaging.ImageFormat.Gif);
                Imstr.Dispose();
                cliForm.pictureDt = Convert.ToBase64String(Imstr.ToArray());
                string sendDt = ClientBox.Items.Count.ToString() + '\x01';
                foreach (string cliList in ClientBox.Items)
                    sendDt += cliList + '\x01';
                cliForm.recvDt = sendDt;
                cliForm.sendkind = 2;
                cliForm.TalkSend();
            }
        }
        // 보내기 버튼
        private void btn_Send_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtTTS.Text.Trim()) || Answer.Text != "")
            {
                txtTTS.Focus();
                return;
            }
            string sendDt = connectUser + '\x01' + txtTTS.Text + '\x01' + ClientBox.Items.Count.ToString() + '\x01';
            foreach (string cliList in ClientBox.Items)
                sendDt += cliList + '\x01';
            cliForm.recvDt = sendDt;
            AppendText(txtHistory, string.Format("[보냄]{0} < {1}", connectUser, txtTTS.Text));
            txtTTS.Clear();
            txtTTS.Focus();
            cliForm.sendkind = 1;
            cliForm.TalkSend();
        }
        // 준비 버튼
        private void Game_Ready(object sender, EventArgs e)
        {
            int chk;
            for (int i = ClientBox.Items.Count - 1; i >= 0; i--)
            {
                if (connectUser == ClientBox.Items[i].ToString())
                {
                    if (ClientBox.GetItemCheckState(i) != CheckState.Checked)
                    {
                        ClientBox.SetItemCheckState(i, CheckState.Checked);
                        chk = 1;
                    }
                    else
                    {
                        ClientBox.SetItemCheckState(i, CheckState.Unchecked);
                        chk = 2;
                    }
                    if (ClientBox.CheckedItems.Count == ClientBox.Items.Count)
                    {
                        chk = 3;
                    }
                    string sendDt = connectUser + '\x01' + ClientBox.Items.Count.ToString() + '\x01' + chk + '\x01';
                    foreach (string cliList in ClientBox.Items)
                        sendDt += cliList + '\x01';
                    cliForm.recvDt = sendDt;
                    cliForm.sendkind = 3;
                    cliForm.TalkSend();
                    break;
                }
            }
        }
        // 이미지 받아오기
        public Image recvPicture
        {
            get { return recvpic; }
            set
            {
                recvpic = value;
                pictureBox1.Image = recvpic;
            }
        }
        // 대화 받아오기
        public string recvChat
        {
            get { return recvcht; }
            set
            {
                recvcht = value;
                string[] arrDts = recvcht.Split('\x01');
                txtHistory.Text += Environment.NewLine + string.Format("[받음]{0} > {1}", arrDts[1], arrDts[3]);
                txtHistory.SelectionStart = txtHistory.Text.Length;
                txtHistory.ScrollToCaret();
            }
        }
        // 준비 상태 받아오기
        public string recvState
        {
            get { return recvchk; }
            set
            {
                recvchk = value;
                string[] arrDts = recvchk.Split('\x01');
                for (int i = ClientBox.Items.Count - 1; i >= 0; i--)
                {
                    if (arrDts[2] == ClientBox.Items[i].ToString())
                    {
                        if (arrDts[4] == "1")
                            ClientBox.SetItemCheckState(i, CheckState.Checked);
                        else
                            ClientBox.SetItemCheckState(i, CheckState.Unchecked);
                        break;
                    }
                }
            }
        }
        // 게임 시작
        public string recvStart
        {
            get { return recvsta; }
            set
            {
                recvsta = value;
                string[] arrDts = recvsta.Split('\x01');
                for(int j = ClientBox.Items.Count -1; j >= 0; j--)
                {
                    if (arrDts[1] == ClientBox.Items[j].ToString())
                    {
                        ClientBox.SetItemCheckState(j, CheckState.Checked);
                        break;
                    }
                }
                for (int i = ClientBox.Items.Count - 1; i >= 0; i--)
                {
                    if (arrDts[2] == connectUser)
                    {
                        Answer.Text = arrDts[4];
                        button2.Enabled = true;
                        btn_Reset.Enabled = true;
                        break;
                    }
                }
                AppendText(txtHistory, string.Format("[게임 시작]"));
                AppendText(txtHistory, string.Format("[차례: {0}]", arrDts[2]));
                button3.Enabled = false;
                timer1.Start();
            }
        }
        // 정답 및 시간초과
        public string recvCollect
        {
            get { return recvcol; }
            set
            {
                recvcol = value;
                Answer.Text = "";
                string[] arrDts = recvcol.Split('\x01');
                for (int i = ClientBox.Items.Count - 1; i >= 0; i--)
                {
                    if (arrDts[1] == connectUser)
                    {
                        Answer.Text = arrDts[3];
                        button2.Enabled = true;
                        btn_Reset.Enabled = true;
                        pictureBox1.Image = null;
                        break;
                    }
                    else
                    {
                        button1.Enabled = false;
                        button2.Enabled = false;
                        btn_Reset.Enabled = false;
                        pictureBox1.Image = null;
                    }
                }
                if (arrDts[5] != "~" && arrDts[1] != connectUser)
                {
                    txtHistory.Text += Environment.NewLine + string.Format("[받음]{0} > {1}", arrDts[1], arrDts[2]);
                    txtHistory.SelectionStart = txtHistory.Text.Length;
                    txtHistory.ScrollToCaret();
                }
                else if (arrDts[5] == "~")
                    AppendText(txtHistory, string.Format("[시간 초과!]"));
                AppendText(txtHistory, string.Format("[정답: {0}]", arrDts[2]));
                AppendText(txtHistory, string.Format("[차례: {0}]", arrDts[1]));
                pBar = 0;
            }
        }
        // 게임 종료
        public string recvEnd
        {
            get { return recved; }
            set
            {
                recved = value;
                string[] arrDts = recved.Split('\x01');
                if (arrDts[5] != "~" && arrDts[1] != connectUser)
                {
                    txtHistory.Text += Environment.NewLine + string.Format("[받음]{0} > {1}", arrDts[1], arrDts[2]);
                    txtHistory.SelectionStart = txtHistory.Text.Length;
                    txtHistory.ScrollToCaret();
                }
                else if (arrDts[5] == "~")
                    AppendText(txtHistory, string.Format("[시간 초과!]"));
                AppendText(txtHistory, string.Format("[정답: {0}]", arrDts[2]));
                AppendText(txtHistory, string.Format("[게임 종료]"));
                for(int i = ClientBox.Items.Count - 1; i >= 0; i++)
                    ClientBox.SetItemCheckState(i, CheckState.Unchecked);
                pBar = 0;
                Answer.Text = "";
                button1.Enabled = false;
                button2.Enabled = false;
                button3.Enabled = true;
                btn_Reset.Enabled = false;
                pictureBox1.Image = null;
                timer1.Stop();
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
    }
}
