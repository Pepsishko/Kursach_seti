using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Security.Cryptography;
using System.IO;

namespace Server
{
    public class User
    {
        private string Decrypt(byte[] cipherBytes, string EncryptionKey = "123")
        {
            string cipherText = "";
            using (Aes encryptor = Aes.Create())
            {
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKey, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(cipherBytes, 0, cipherBytes.Length);
                       // cs.Close();
                    }
                    cipherText = Encoding.UTF8.GetString(ms.ToArray());
                }
            }
            return cipherText;
        }
        private Thread _userThread;
        private string _userName;
        private bool AuthSuccess = false;
        public string Username
        {
            get { return _userName; }
        }
        private Socket _userHandle;
        public User(Socket handle)
        {
            _userHandle = handle;
            _userThread = new Thread(listner);
            _userThread.IsBackground = true;
            _userThread.Start();
        }
        private void listner()
        {
            try
            {
                while (_userHandle.Connected)
                {
                    byte[] buffer = new byte[2048];
                    int bytesReceive = _userHandle.Receive(buffer);
                    handleCommand(Encoding.Unicode.GetString(buffer, 0, bytesReceive));
                }
            }
            catch { Server.EndUser(this); }
        }
        private bool setName(string Name)
        {
            //Тут можно добавить различные проверки
            _userName = Name;
            Server.NewUser(this);
            AuthSuccess = true;
            return true;
        }
        private void handleCommand(string cmd)
        {
            cmd = Decrypt(Encoding.Unicode.GetBytes(cmd));
            try
            {
                string[] commands = cmd.Split('#');
                int countCommands = commands.Length;
                for (int i = 0; i < countCommands; i++)
                {
                    string currentCommand = commands[i];
                    if (string.IsNullOrEmpty(currentCommand))
                        continue;
                    if (!AuthSuccess)
                    {
                        if (currentCommand.Contains("setname"))
                        {
                            if (setName(currentCommand.Split('|')[1]))
                                Send("#setnamesuccess");
                            else
                                Send("#setnamefailed");
                        }
                        continue;
                    }
                    if(currentCommand.Contains("yy"))
                    {
                        string id = currentCommand.Split('|')[1];
                        Server.FileD file = Server.GetFileByID(int.Parse(id));
                        if (file.ID == 0)
                        {
                            SendMessage("Ошибка при передаче файла...", "1");
                            continue;
                        }
                        Send(file.fileBuffer);
                        Server.Files.Remove(file);
                    }
                    if (currentCommand.Contains("message"))
                    {
                        string[] Arguments = currentCommand.Split('|');
                        Server.SendGlobalMessage($"[{_userName}]: {Arguments[1]}","Black");

                        continue;
                    }//sENDfile
                    //...
                    if(currentCommand.Contains("endsession"))
                    {
                        Server.EndUser(this);
                        return;
                    }
                    if(currentCommand.Contains("sendfileto"))
                    {
                        string[] Arguments = currentCommand.Split('|');
                        string TargetName = Arguments[1];
                        int FileSize = int.Parse(Arguments[2]);
                        string FileName = Arguments[3];
                        byte[] fileBuffer = new byte[FileSize];
                        _userHandle.Receive(fileBuffer);
                        User targetUser = Server.GetUser(TargetName);
                        if(targetUser == null)
                        {
                            SendMessage($"Пользователь {FileName} не найден!","Black");
                            continue;
                        }
                        Server.FileD newFile = new Server.FileD()
                        {
                            ID = Server.Files.Count+1,
                            FileName = FileName,
                            From = Username,
                            fileBuffer = fileBuffer,
                            FileSize = fileBuffer.Length
                        };
                        Server.Files.Add(newFile);
                        targetUser.SendFile(newFile,targetUser);
                    }
                    if(currentCommand.Contains("private"))
                    {
                        string[] Arguments = currentCommand.Split('|');
                        string TargetName = Arguments[1];
                        string Content = Arguments[2];
                        User targetUser = Server.GetUser(TargetName);
                        if(targetUser == null)
                        {
                            SendMessage($"Пользователь {TargetName} не найден!","Red");
                            continue;
                        }
                        SendMessage($"-[Отправлено][{TargetName}]: {Content}","Black");
                        targetUser.SendMessage($"-[Получено][{Username}]: {Content}","Black");
                        continue;
                    }

                }

            }
            catch (Exception exp) { Console.WriteLine("Error with handleCommand: " + exp.Message); }
        }
        public void SendFile(Server.FileD fd, User To)
        {
            byte[] answerBuffer = new byte[48];
            Console.WriteLine($"Sending {fd.FileName} from {fd.From} to {To.Username}");
            To.Send($"#gfile|{fd.FileName}|{fd.From}|{fd.fileBuffer.Length}|{fd.ID}");
        }
        public void SendMessage(string content,string clr)
        {
            Send($"#msg|{content}|{clr}");
        }
        public void Send(byte[] buffer)
        {
            try
            {
                _userHandle.Send(buffer);
            }
            catch { }
        }
        public void Send(string Buffer)
        {
            try
            {
                _userHandle.Send(Encoding.Unicode.GetBytes(Buffer));
            }
            catch { }
        }
        public void End()
        {
            try
            {
                _userHandle.Close();
            }
            catch { }

        }
    }
}
