using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SocketIOClient;
using WebSocket4Net;

namespace Chatting_Form
{
    public partial class FormChattingRoom : Form
    {
        public FormChattingRoom()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            FormChatting formChatting = new FormChatting();

            formChatting.Show();
           
        }

        private void listBoxChattingList_SelectedIndexChanged(object sender, EventArgs e)
        {
            FormChatting formChatting = new FormChatting();

            formChatting.Show();
        }
    }
}
