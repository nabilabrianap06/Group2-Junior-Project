using System;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace w7junpro
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void label1_Click(object sender, EventArgs e)
        {
        }

        // Event tombol Close
        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        // Event tombol Login
        private void button1_Click(object sender, EventArgs e)
        {
            Employee employee = new Employee(textBox1.Text, textBox2.Text);

            if (employee.Login())
            {
                MessageBox.Show("Login Berhasil, ID anda adalah " + employee.EmployeeID.ToString());

                // Membuka Dashboard dan menyembunyikan form login
                Dashboard dashboard = new Dashboard();
                dashboard.Show();
                this.Hide();
            }
            else
            {
                MessageBox.Show("Login Gagal");
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }
    }
}