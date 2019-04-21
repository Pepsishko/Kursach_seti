using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Client
{
    public partial class SignIn : Form
    {
        public string _host="";
        public SignIn()
        {
            InitializeComponent();
        }
        
        private void Sign_Click(object sender, EventArgs e)
        {
            ChatForm chat = new ChatForm(textBox1.Text);
            _host = textBox1.Text;
            chat.Show();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
