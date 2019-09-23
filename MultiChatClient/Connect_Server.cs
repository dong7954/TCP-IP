using System;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;

namespace MultiChatClient
{
    public partial class MultiChat : Form
    {
        delegate void AppendTextDelegate(Control ctrl, string s);
        AppendTextDelegate _textAppender;
        Socket mainSock;

        public MultiChat()
        {
            InitializeComponent();
            mainSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            _textAppender = new AppendTextDelegate(AppendText);
        }

        void AppendText(Control ctrl, string s)
        {
            if (ctrl.InvokeRequired) ctrl.Invoke(_textAppender, ctrl, s);
            else
            {
                string source = ctrl.Text;
                ctrl.Text = source + Environment.NewLine + s;
            }
        }

        private void Client_Start_Load(object sender, EventArgs e)
        {
            IPHostEntry he = Dns.GetHostEntry(Dns.GetHostName());

            // 처음으로 발견되는 ipv4 주소를 사용한다.
            IPAddress defaultHostAddress = null;
            foreach (IPAddress addr in he.AddressList)
            {
                if (addr.AddressFamily == AddressFamily.InterNetwork)
                {
                    defaultHostAddress = addr;
                    break;
                }
            }

            // 주소가 없다면..
            if (defaultHostAddress == null)
                // 로컬호스트 주소를 사용한다.
                defaultHostAddress = IPAddress.Loopback;

            txtAddress.Text = defaultHostAddress.ToString();

        }

        private void button1_Click(object sender, EventArgs e)
        {
            foreach (Form frmm in Application.OpenForms)
            {
                if (frmm.Name == "Client")
                {
                    frmm.Activate();
                    MessageBox.Show("이미 실행중입니다.", "경고", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            if (mainSock.Connected)
            {
                MsgBoxHelper.Error("이미 연결되어 있습니다!");
                return;
            }

            //닉네임칸이 빈칸이라면 메시지 박스
            string name = txtNick.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                MsgBoxHelper.Warn("닉네임을 입력해 주세요");
                txtNick.Focus();
                return;
            }

            //클라에게 닉네임을 넘겨준다
            //Client.nick = txtNick.Text;
            //클라에게 ip주소를 넘겨준다
            //Client.s_ip = txtAddress.Text;

            Client frm = new Client();
            frm.Show();
        }
    }
}