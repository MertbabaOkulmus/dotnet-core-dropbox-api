using System;
using System.Text;
using System.Windows.Forms;
using Nemiro.OAuth;
using Nemiro.OAuth.LoginForms;
using System.IO;
using System.Net;
using System.Collections.Specialized;
using System.Linq;
namespace DropBox
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        private HttpAuthorization Authorization = null;
        static string clientId = "**********";
        static string clientSecret = "**********";
        private string CurrentPath = "";
        private void Form1_Load(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(Properties.Settings.Default.AccessToken))
            {
                this.GetAccesToken();
            }
            else
            {
                this.Authorization = new HttpAuthorization(AuthorizationType.Bearer, Properties.Settings.Default.AccessToken);
                this.GetFiles();
            }
        }
        private void GetAccesToken()
        {
            var login = new DropboxLogin(clientId, clientSecret, "https://oauthproxy.nemiro.net/", false, false);
            login.Owner = this;
            login.ShowDialog();

            if (login.IsSuccessfully)
            {
                Properties.Settings.Default.AccessToken = login.AccessTokenValue;
                Properties.Settings.Default.Save();
                this.Authorization = new HttpAuthorization(AuthorizationType.Bearer, login.AccessTokenValue);
                this.GetFiles();
            }
            else
            {
                MessageBox.Show("Error...");
            }
        }
        private void GetFiles()
        {
            OAuthUtility.PostAsync
             (
               "https://api.dropboxapi.com/2/files/list_folder",
               new HttpParameterCollection
               {
          new
          {
            path = this.CurrentPath,
            include_media_info = true
          }
               },
               contentType: "application/json",
               authorization: this.Authorization,
               callback: this.GetFiles_Result
             );
        }
        private void GetFiles_Result(RequestResult result)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<RequestResult>(this.GetFiles_Result), result);
                return;
            }
        }
        private void Upload_Result(RequestResult result)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<RequestResult>(this.Upload_Result), result);
                return;
            }

            if (result.StatusCode == 200)
            {
                this.GetFiles();
            }
            else
            {
                if (result["error"].HasValue)
                {
                    MessageBox.Show(result["error"].ToString().ToString());
                }
                else
                {
                    MessageBox.Show(result.ToString());
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (this.openFileDialog1.ShowDialog() != System.Windows.Forms.DialogResult.OK) { return; }

            // send file
            var fs = new FileStream(this.openFileDialog1.FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            var fileInfo = UniValue.Empty;
            fileInfo["path"] = (String.IsNullOrEmpty(this.CurrentPath) ? "/" : "") + Path.Combine(this.CurrentPath, Path.GetFileName(this.openFileDialog1.FileName)).Replace("\\", "/");
            fileInfo["mode"] = "add";
            fileInfo["autorename"] = true;
            fileInfo["mute"] = false;

            OAuthUtility.PutAsync
            (
              "https://api-content.dropbox.com/1/files_put/auto/",
              new HttpParameterCollection
              {
                  {"acces_token",Properties.Settings.Default.AccessToken},
                  {"path",Path.Combine(this.CurrentPath,Path.GetFileName(openFileDialog1.FileName)).Replace("\\","/") },
                  {"overwrite","true"},
                  {"autorename","true"},
                  {openFileDialog1.OpenFile()}
              },
              callback: Upload_Result
            );
        }
    }
}
