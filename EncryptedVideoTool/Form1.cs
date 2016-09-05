using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

using System.Security.Cryptography;

namespace EncryptedVideoTool
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string message = openFileDialog1.FileName;
                richTextBox1.Text = message;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {

        }

        private void output_btn_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                string message = folderBrowserDialog1.SelectedPath;
                richTextBox2.Text = message;
            }
        }

        private void generate_btn_Click(object sender, System.EventArgs e)
        {
            if (richTextBox1.TextLength == 0 || richTextBox2.TextLength == 0 || textBox1.TextLength == 0)
            {
                MessageBox.Show("请将信息填写完整！", "VideoTool");
            }

            else
            {
                byte[] position_array = new byte[8];
                byte[] mask_array = new byte[8];
                byte[] key_array = new byte[56];

                Random rand = new Random();

                generateRandomPositions(position_array, rand);
                generateRandomMasks(mask_array, rand);
                generateRandomKey(key_array, rand);

                for (int i = 0; i < position_array.Length; i++) {
			        Console.Write(position_array[i] + " ");
		        }
                Console.WriteLine();
		        for (int i = 0; i < mask_array.Length; i++) {
			        Console.Write(mask_array[i] + " ");
		        }
                Console.WriteLine();

                int transform_length = 256;

                FileStream sFile = new FileStream(richTextBox1.Text, FileMode.Open);
                byte[] byte_array = new byte[transform_length];
                sFile.Read(byte_array, 0, transform_length);
                for (int i = 0; i < byte_array.Length; i++)
                {
                    Console.Write(byte_array[i] + " ");
                }
                Console.WriteLine();

                transformData(byte_array, transform_length, position_array, mask_array);

                for (int i = 0; i < byte_array.Length; i++)
                {
                    Console.Write(byte_array[i] + " ");
                }
                Console.WriteLine();

                string md5 = getMD5ByHashAlgorithm(byte_array, 0, transform_length).ToLower();
                Console.Write(md5 + "\n");

                FileStream dos = new FileStream(richTextBox2.Text + "/" + textBox1.Text + ".cglvr", FileMode.Create);

                BinaryWriter bw = new BinaryWriter(dos);

                bw.Seek(0, SeekOrigin.Begin);
                long header_length = 116;
                byte[] header_length_array = System.BitConverter.GetBytes((long)header_length);
                Array.Reverse(header_length_array);
                bw.Write(header_length_array);
                byte[] transform_length_array = System.BitConverter.GetBytes((int)transform_length);
                Array.Reverse(transform_length_array);
                bw.Write(transform_length_array);

                byte[] md5_array = System.Text.Encoding.Default.GetBytes(md5);
                bw.Write(md5_array);

                for (int i = 0; i < position_array.Length; i++)
                {
                    bw.Write(position_array[i]);
                    bw.Write(mask_array[i]);
                }

                bw.Write(key_array);

                bw.Write(byte_array);

                sFile.Seek(transform_length, SeekOrigin.Begin);

                int iBufferSize = 1024000;
                byte[] buffer = new byte[iBufferSize];
                int readLength = 0;//每次读取长度
                while ((readLength = sFile.Read(buffer, 0, buffer.Length)) > 0)
                {
                    bw.Write(buffer, 0, readLength);
                }

                bw.Close();
                dos.Close();
                sFile.Close();

                MessageBox.Show("生成视频成功！", "VideoTool");
            }
        }

        private static void generateRandomMasks(byte[] mask_array, Random rand)
        {
            // TODO Auto-generated method stub
            int length = mask_array.Length;
            for (int i = 0; i < length; i++)
            {
                mask_array[i] = (byte)rand.Next(256);
            }
        }

        private static void generateRandomKey(byte[] key_array, Random rand)
        {
            // TODO Auto-generated method stub
            int length = key_array.Length;
            for (int i = 0; i < length; i++)
            {
                key_array[i] = (byte)rand.Next(256);
            }
        }

        private static void generateRandomPositions(byte[] position_array, Random rand)
        {
            // TODO Auto-generated method stub
            int length = position_array.Length;
            int i = 0;
            while (i < length)
            {
                int r = rand.Next(256);
                int j = 0;
                while (j < i)
                {
                    if (position_array[j] == r)
                    {
                        break;
                    }
                    j++;
                }

                if (j == i)
                {
                    position_array[i] = (byte)r;
                    i++;
                }
            }
        }

        private static void transformData(byte[] byte_array, int transform_length,
            byte[] position_array, byte[] mask_array)
        {

            // 对随机抽取的单个字节进行位转换
            for (int i = 0; i < position_array.Length; i++)
            {
                int position = position_array[i] & 0xff;
                byte mask = mask_array[i];

                byte current_byte = byte_array[position];
                byte masked = (byte)(current_byte ^ mask);

                byte temp1 = (byte)(masked << 7);
                byte temp2 = (byte)(masked >> 1);
                if (temp2 < 0)
                {
                    temp2 = (byte)(temp2 + 128);
                }
                byte transformed = (byte)(temp1 ^ temp2);

                byte_array[position] = transformed;
            }

            // 对256个字节进行转换加密
            for (int i = 0; i < (transform_length / 2); i++)
            {
                byte temp = byte_array[i];
                byte_array[i] = byte_array[transform_length - 1 - i];
                byte_array[transform_length - 1 - i] = temp;
            }

            byte tem = byte_array[transform_length - 1];
            for (int i = transform_length - 1; i > 0; i--)
            {
                byte_array[i] = byte_array[i - 1];
            }
            byte_array[0] = tem;
        }

        //计算单个小文件的整个Md5
        public static string getMD5ByHashAlgorithm(byte[] byte_array, int offset, int transform_length)
        {
            int bufferSize = transform_length;//自定义缓冲区大小16K
            HashAlgorithm hashAlgorithm = new MD5CryptoServiceProvider();
            var output = new byte[bufferSize];
            hashAlgorithm.TransformBlock(byte_array, offset, transform_length, output, 0);
            //完成最后计算，必须调用(由于上一部循环已经完成所有运算，所以调用此方法时后面的两个参数都为0)
            hashAlgorithm.TransformFinalBlock(byte_array, 0, 0);
            string md5 = BitConverter.ToString(hashAlgorithm.Hash);
            hashAlgorithm.Clear();
            md5 = md5.Replace("-", "");
            return md5;
        }
    }
}
