using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;

namespace Chatting_Form
{
    public partial class FormChatting : Form
    {
        Socket mainSock;
        delegate void AppendTextDelegate(Control ctrl, string s);
        AppendTextDelegate _textAppender;

        public FormChatting()
        {
            InitializeComponent();
            mainSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
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
        void DataReceived(IAsyncResult ar)
        {
            // BeginReceive에서 추가적으로 넘어온 데이터를 AsyncObject 형식으로 변환한다.
            AsyncObject obj = (AsyncObject)ar.AsyncState;

            // 데이터 수신을 끝낸다.
            int received = obj.WorkingSocket.EndReceive(ar);

            // 받은 데이터가 없으면(연결끊어짐) 끝낸다.
            if (received <= 0)
            {
                obj.WorkingSocket.Close();
                return;
            }

            // 텍스트로 변환한다.
            string text = Encoding.UTF8.GetString(obj.Buffer);

            // 0x01 기준으로 짜른다.
            // tokens[0] - 보낸 사람 IP
            // tokens[1] - 보낸 메세지
            string[] tokens = text.Split('\x01');
            string ip = tokens[0];
            string msg = tokens[1];

            // 텍스트박스에 추가해준다.
            // 비동기식으로 작업하기 때문에 폼의 UI 스레드에서 작업을 해줘야 한다.
            // 따라서 대리자를 통해 처리한다.
            AppendText(textBoxHistoryChat, string.Format("[받음]{0}: {1}", ip, msg));

            // 클라이언트에선 데이터를 전달해줄 필요가 없으므로 바로 수신 대기한다.
            // 데이터를 받은 후엔 다시 버퍼를 비워주고 같은 방법으로 수신을 대기한다.
            obj.ClearBuffer();

            // 수신 대기
            obj.WorkingSocket.BeginReceive(obj.Buffer, 0, 4096, 0, DataReceived, obj);
        }

        private async void FormChatting_Load(object sender, EventArgs e)
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
        }

        private void buttonConnect_Click(object sender, EventArgs e)
        {
            if (mainSock.Connected)
            {
                MessageBox.Show("이미 연결되어 있습니다.", "오류");
                return;
            }

            int port;
            if (!int.TryParse(textBoxPort.Text, out port))
            {
                MessageBox.Show("포트 번호가 잘못 입력되었거나 입력되지 않았습니다.", "오류");
                return;
            }

            try { mainSock.Connect(textBoxAddress.Text, port); }
            catch (Exception ex)
            {
                MessageBox.Show("연결에 실패했습니다!", "오류");
                return;
            }

            // 연결 완료되었다는 메세지를 띄워준다.
            AppendText(textBoxHistoryChat, "서버와 연결되었습니다.");

            // 연결 완료, 서버에서 데이터가 올 수 있으므로 수신 대기한다.
            AsyncObject obj = new AsyncObject(4096);
            obj.WorkingSocket = mainSock;
            mainSock.BeginReceive(obj.Buffer, 0, obj.BufferSize, 0, DataReceived, obj);

        }
        private void buttonSend_Click(object sender, EventArgs e)
        {
            // 서버가 대기중인지 확인한다.
            if (!mainSock.IsBound)
            {
                MessageBox.Show("서버가 실행되고 있지 않습니다!", "오류");
                return;
            }

            // 보낼 텍스트
            string tts = textBoxSend.Text.Trim();
            if (string.IsNullOrEmpty(tts))
            {
                MessageBox.Show("텍스트가 입력되지 않았습니다.", "오류");
                textBoxSend.Focus();
                return;
            }

            // 서버 ip 주소와 메세지를 담도록 만든다.
            IPEndPoint ip = (IPEndPoint)mainSock.LocalEndPoint;
            string addr = ip.Address.ToString();

            // 문자열을 utf8 형식의 바이트로 변환한다.
            byte[] bDts = Encoding.UTF8.GetBytes(addr + '\x01' + tts);

            // 서버에 전송한다.
            mainSock.Send(bDts);

            // 전송 완료 후 텍스트박스에 추가하고, 원래의 내용은 지운다.
            AppendText(textBoxHistoryChat, string.Format("[보냄]{0}: {1}", addr, tts));
            textBoxSend.Clear();
        }
        
    }
}
