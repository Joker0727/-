using Aspose.Words;
using Aspose.Words.Replacing;
using Aspose.Words.Saving;
using HtmlAgilityPack;
using RestSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Tool;
using Font = Aspose.Words.Font;

namespace ArticleConversionTool
{
    public partial class Form1 : Form
    {
        public string basePath = AppDomain.CurrentDomain.BaseDirectory;
        private static readonly string workId = "ww-0008";
        public MyUtils myUtils = null;
        public List<string> specialWordList = new List<string>();
        public List<string> wordPathList = new List<string>();
        public string folderPath = string.Empty;
        public string targetPath = string.Empty;
        public string wordPath = string.Empty;
        public string htmPath = string.Empty;
        public string txtPath = string.Empty;
        public Thread th1 = null;
        public Thread th2 = null;
        public int folderTotalCount = 0;
        public int wordTotalCount = 0;
        public int currentfolderCount = 0;
        public int currentWordCount = 0;
        public string errorFolder1 = AppDomain.CurrentDomain.BaseDirectory + @"ErrorFolder1\";
        public string errorFolder2 = AppDomain.CurrentDomain.BaseDirectory + @"ErrorFolder2\";

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.MaximizeBox = false;
            myUtils = new MyUtils();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (!IsAuthorised())
            {
                MessageBox.Show("网络异常！", "Article Conversion Tool");
                return;
            }
            string btnText = this.button1.Text;
            if (btnText == "开始")
            {
                this.button1.Text = "暂停";
                folderPath = this.textBox1.Text;
                targetPath = this.textBox2.Text;

                if (string.IsNullOrEmpty(folderPath) || string.IsNullOrEmpty(targetPath))
                {
                    MessageBox.Show("目录不能为空！", "Article Conversion Tool");
                    return;
                }
                if (th1 == null)
                {
                    th1 = new Thread(ReadAllFolder);
                    th1.IsBackground = true;
                    th1.Start();
                }
                else
                {
                    th1?.Resume();
                }
            }
            else
            {
                this.button1.Text = "开始";
                th1?.Suspend();
            }
        }

        public void ReadAllFolder()
        {
            string[] folderArr = Directory.GetDirectories(folderPath);
            folderTotalCount = folderArr.Length;
            foreach (var folder in folderArr)
            {
                try
                {
                    TxtToWord(folder);
                }
                catch (Exception ex)
                {
                    myUtils.WriteLog(ex);
                }
            }
            MessageBox.Show("转换结束", "Article Conversion Tool");
        }
        /// <summary>
        /// txt转换成word
        /// </summary>
        /// <param name="folder"></param>
        public void TxtToWord(string folder)
        {
            string contentPath = string.Empty, imgPath = string.Empty;
            contentPath = folder + @"\内容.txt";
            if (!Directory.Exists(folder))
                return;
            DirectoryInfo dir = new DirectoryInfo(folder);
            string wordTitle = dir.Name;
            try
            {
                List<string> imgNameList = myUtils.GetImgs(folder);
                string contentStr = File.ReadAllText(contentPath);
                string newcontent = string.Empty;
                //newcontent = UseApi(contentStr);
                //if (string.IsNullOrEmpty(newcontent))
                newcontent = contentStr;
                string[] contentArr = myUtils.SplitByStr(newcontent, "\r\n");

                Document doc = new Document();
                DocumentBuilder builder = new DocumentBuilder(doc);
                Font font = builder.Font;
                if (wordTitle.Length > 30 || wordTitle.Length < 6)
                    font.Color = Color.Red;
                builder.Writeln(wordTitle);
                font.Color = Color.Black;
                foreach (var content in contentArr)
                {
                    try
                    {
                        string resStr = CheckIsImage(imgNameList, content);
                        if (string.IsNullOrEmpty(resStr))
                        {
                            builder.Writeln(content);
                        }
                        else
                        {
                            imgPath = folder + @"\" + resStr;
                            builder.InsertImage(imgPath);
                            builder.Write("\r\n");
                        }
                    }
                    catch (Exception ex)
                    {
                        myUtils.WriteLog("插入文字或者图片时出错" + ex);
                    }
                }
                string savePath = targetPath + @"\" + wordTitle + ".docx";
                doc.Save(savePath);
                ChangeWordColor(savePath);
                this.dataGridView1.Invoke(new Action(() =>
                {
                    this.dataGridView1.Rows.Add(wordTitle, "成功");
                    this.dataGridView1.Rows[dataGridView1.Rows.Count - 2].Cells["Column2"].Style.ForeColor = Color.Blue;
                    this.dataGridView1.FirstDisplayedScrollingRowIndex = dataGridView1.Rows[dataGridView1.Rows.Count - 2].Index;
                }));
                currentfolderCount++;
                this.label9.Invoke(new Action(() =>
                {
                    this.label9.Text = currentfolderCount + "/" + folderTotalCount;
                }));
            }
            catch (Exception ex)
            {
                this.dataGridView1.Invoke(new Action(() =>
                {
                    this.dataGridView1.Rows.Add(wordTitle, "失败");
                    this.dataGridView1.Rows[dataGridView1.Rows.Count - 2].Cells["Column2"].Style.ForeColor = Color.Red;
                    this.dataGridView1.FirstDisplayedScrollingRowIndex = dataGridView1.Rows[dataGridView1.Rows.Count - 2].Index;
                }));
                myUtils.WriteLog(ex);
                if (!Directory.Exists(errorFolder1))
                    Directory.CreateDirectory(errorFolder1);
                dir.MoveTo(errorFolder1 + dir.Name);
            }
        }
        /// <summary>
        /// 修改敏感词的颜色
        /// </summary>
        public void ChangeWordColor(string wordPath)
        {
            Microsoft.Office.Interop.Word._Application word;
            Microsoft.Office.Interop.Word._Document document;
            word = new Microsoft.Office.Interop.Word.Application();
            word.Visible = false;//默认位true，打开word文档可见窗口，当为false时，窗口不可见
            document = word.Documents.Open(wordPath);  //打开word
            document.Activate();
            //object findStr = "社交圈"; //待查找的文字
            //foreach (var specialWord in specialWordList)
            //{
            // findStr = specialWord;
            //while (word.Selection.Find.Execute(ref findStr))  //查找文字
            //{
            //change font and format of matched words
            //word.Selection.Font.Name = "Tahoma"; //替换字体
            // word.Selection.Font.ColorIndex = Microsoft.Office.Interop.Word.WdColorIndex.wdRed;  //替换颜色
            // }
            //}
            foreach (var specialWord in specialWordList)
            {
                Microsoft.Office.Interop.Word.Range rang = document.Range(word.Selection.Start, word.Selection.End);//从word的开始到结束的范围内查找替换
                ReplaceFont(rang, specialWord);//新方法，很好用
            }
            document.Save(); //保存更改
            document.Close();  //关闭当前打开的文档
            word.Application.Quit();
        }
        /// <summary>
        /// 修改文字，修改文字样式
        /// </summary>
        /// <param name="rng"></param>
        /// <param name="findWhat"></param>
        /// <returns></returns>
        public bool ReplaceFont(Microsoft.Office.Interop.Word.Range rng, string findWhat)
        {
            bool hasFound = false;
            rng.Find.ClearFormatting();
            rng.Find.Replacement.ClearFormatting();
            rng.Find.Replacement.Font.ColorIndex = Microsoft.Office.Interop.Word.WdColorIndex.wdRed;
            rng.Find.Text = findWhat;//需要被替换掉的文字
            rng.Find.Replacement.Text = findWhat;//需要替换成的新文字
            rng.Find.Forward = true;
            rng.Find.Wrap = Microsoft.Office.Interop.Word.WdFindWrap.wdFindStop;

            //change this property to true as we want to replace format
            rng.Find.Format = true;

            hasFound = rng.Find.Execute(Replace: Microsoft.Office.Interop.Word.WdReplace.wdReplaceAll);
            return hasFound;
        }
        /// <summary>
        /// 检测是否是图片
        /// </summary>
        /// <param name="imgNameList"></param>
        /// <param name="str"></param>
        /// <returns></returns>
        public string CheckIsImage(List<string> imgNameList, string str)
        {
            foreach (var imageName in imgNameList)
            {
                try
                {
                    if (str.Contains(imageName))
                        return imageName;
                }
                catch (Exception ex)
                {
                    myUtils.WriteLog("检查是否图片时出错" + ex);
                }
            }
            return null;
        }
        /// <summary>
        /// 授权
        /// </summary>
        /// <param name="workId"></param>
        /// <returns></returns>
        public bool IsAuthorised()
        {
            string conStr = "Server=111.230.149.80;DataBase=MyDB;uid=sa;pwd=1add1&one";
            bool bo = false;
            try
            {
                using (SqlConnection con = new SqlConnection(conStr))
                {
                    string sql = string.Format("select count(*) from MyWork Where IsAuth = 1 and WorkId ='{0}'", workId);
                    using (SqlCommand cmd = new SqlCommand(sql, con))
                    {
                        con.Open();
                        int count = int.Parse(cmd.ExecuteScalar().ToString());
                        if (count > 0)
                            bo = true;
                    }
                }
            }
            catch (Exception)
            {
                bo = false;
            }

            return bo;
        }

        private void textBox_Click(object sender, EventArgs e)
        {
            TextBox textBox = sender as TextBox;
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.Description = "请选择资源所在目录";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                if (string.IsNullOrEmpty(dialog.SelectedPath))
                {
                    MessageBox.Show(this, "文件夹路径不能为空", "提示");
                    return;
                }
                else
                {
                    textBox.Text = dialog.SelectedPath;
                }
            }
        }

        private void textBox3_Click(object sender, EventArgs e)
        {
            TextBox textBox = sender as TextBox;
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Multiselect = true;//该值确定是否可以选择多个文件
            dialog.Title = "请选择文件夹";
            dialog.Filter = "txt(*.txt*)|*.txt*";
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string file = dialog.FileName;
                string allWords = File.ReadAllText(file, Encoding.Default);
                textBox.Text = allWords;
                string[] wordsArr = allWords.Split('，');
                specialWordList = wordsArr.ToList();
            }
        }

        private string UseApi(string content)
        {
            Thread.Sleep(5000);
            string api = "http://nlp.baebos.com/test1.php?v=1&key=531af6f4";
            try
            {
                var client = new RestClient(api);
                var request = new RestRequest(Method.POST);
                request.Timeout = 30000;
                request.AddHeader("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8");
                request.AddHeader("Accept-Encoding", "gzip, deflate");
                request.AddHeader("Cache-Control", "max-age=0");
                request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
                request.AddParameter("wenzhang", content);
                IRestResponse response = client.Execute(request);
                return response.Content; //返回的结果
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (!IsAuthorised())
            {
                MessageBox.Show("网络异常！", "Article Conversion Tool");
                return;
            }
            string btnText = this.button2.Text;
            if (btnText == "开始")
            {
                this.button2.Text = "暂停";
                folderPath = this.textBox5.Text;
                wordPath = this.textBox4.Text;
                htmPath = this.textBox6.Text;
                txtPath = this.textBox7.Text;
                if (string.IsNullOrEmpty(wordPath) || string.IsNullOrEmpty(folderPath))
                {
                    MessageBox.Show("目录不能为空！", "Article Conversion Tool");
                    return;
                }
                if (th2 == null)
                {
                    th2 = new Thread(ReadAllWordPath);
                    th2.IsBackground = true;
                    th2.Start();
                }
                else
                {
                    th2?.Resume();
                }
            }
            else
            {
                this.button2.Text = "开始";
                th2?.Suspend();
            }
        }

        public void ReadAllWordPath()
        {
            string[] wordPathArr = Directory.GetFiles(wordPath);
            foreach (var wordPath in wordPathArr)
            {
                FileInfo fileInfo = new FileInfo(wordPath);
                if (fileInfo.Extension.ToLower() == ".docx" || fileInfo.Extension.ToLower() == ".doc")
                {
                    if (fileInfo.Name.Contains("~$"))
                        continue;
                    wordPathList.Add(wordPath);
                }
            }
            wordTotalCount = wordPathList.Count();
            DealWord();
        }

        public void DealWord()
        {
            foreach (var wordPath in wordPathList)
            {
                Document doc = new Document(wordPath);
                DocumentBuilder builder = new DocumentBuilder(doc);
                NodeCollection shapes = doc.GetChildNodes(NodeType.Shape, true);
                FileInfo fileInfo = new FileInfo(wordPath);
                string wordTitle = fileInfo.Name.Replace(fileInfo.Extension, "");
                try
                {

                    string oldFolder = folderPath + @"\" + wordTitle;
                    List<string> imgList = myUtils.GetImgs(oldFolder, 2);
                    WordToHtm(wordPath, oldFolder, wordTitle, imgList);
                    int index = 0;
                    foreach (Aspose.Words.Drawing.Shape item in shapes)
                    {
                        if (item.HasImage)
                        {
                            //Document imgDoc = item.Document as Document;
                            //builder = new DocumentBuilder(imgDoc);

                            //将光标移动到指定节点，移动到这个节点才可以把内容插入到这里
                            builder.MoveTo(item);
                            builder.Writeln(imgList[index]);
                            item.Remove();
                            index++;
                        }
                    }
                    doc.Save(wordPath);
                    WordToTxt(wordPath, oldFolder, wordTitle);
                    this.dataGridView2.Invoke(new Action(() =>
                    {
                        this.dataGridView2.Rows.Add(wordTitle, "成功");
                        this.dataGridView2.Rows[dataGridView2.Rows.Count - 2].Cells["Column4"].Style.ForeColor = Color.Blue;
                        this.dataGridView2.FirstDisplayedScrollingRowIndex = dataGridView2.Rows[dataGridView2.Rows.Count - 2].Index;
                    }));
                    currentWordCount++;
                    this.label10.Invoke(new Action(() =>
                    {
                        this.label10.Text = currentWordCount + "/" + wordTotalCount;
                    }));
                }
                catch (Exception ex)
                {
                    this.dataGridView2.Invoke(new Action(() =>
                    {
                        this.dataGridView2.Rows.Add(wordTitle, "失败");
                        this.dataGridView2.Rows[dataGridView2.Rows.Count - 2].Cells["Column4"].Style.ForeColor = Color.Blue;
                        this.dataGridView2.FirstDisplayedScrollingRowIndex = dataGridView2.Rows[dataGridView2.Rows.Count - 2].Index;
                    }));
                    myUtils.WriteLog(ex);
                    if (!Directory.Exists(errorFolder2))
                        Directory.CreateDirectory(errorFolder2);
                    fileInfo.MoveTo(errorFolder2 + fileInfo.Name);
                }
            }
            MessageBox.Show("htm已经转换完毕！", "Article Conversion Tool");
        }
        public void WordToTxt(string newWordPath, string oldFolder, string wordTitle)
        {
            Document doc = new Document(newWordPath);
            string newContent = doc.GetText();
            newContent = newContent.Replace("\r", "\r\n");

            string parentPath = txtPath + $@"\{wordTitle}\";
            if (!Directory.Exists(parentPath))
                Directory.CreateDirectory(parentPath);
            string[] oldPathArr = Directory.GetFiles(oldFolder);
            foreach (var oldPath in oldPathArr)
            {
                FileInfo fileInfo = new FileInfo(oldPath);
                string newFullPath = Path.Combine(parentPath, fileInfo.Name);
                if (File.Exists(newFullPath))
                    File.Delete(newFullPath);
                fileInfo.CopyTo(newFullPath);
            }
            File.WriteAllText(parentPath + "内容.txt", newContent);
        }

        public void WordToHtm(string wordPath, string oldFolder, string wordTitle, List<string> imgPathList)
        {
            var fi = new FileInfo(wordPath);
            var doc = new Document(fi.FullName);
            var options = new HtmlSaveOptions(SaveFormat.Html)
            {
                ExportTextInputFormFieldAsText = false,
                ExportImagesAsBase64 = true
            };
            string parentFolder = htmPath + $@"\{wordTitle}\";
            if (!Directory.Exists(parentFolder))
                Directory.CreateDirectory(parentFolder);
            string newHtmPath = parentFolder + wordTitle + ".htm";
            doc.Save(newHtmPath, options);
            string htmStr = File.ReadAllText(newHtmPath);
            int index = 0;
            HtmlAgilityPack.HtmlDocument doch = new HtmlAgilityPack.HtmlDocument();
            doch.LoadHtml(htmStr);

            HtmlNodeCollection imgList = doch.DocumentNode.SelectNodes("//img");

            foreach (var imgPath in imgPathList)
            {
                index++;
                FileInfo fileInfo = new FileInfo(imgPath);
                string newFullPath = Path.Combine(parentFolder, $"{wordTitle}_{index}.jpg");
                if (File.Exists(newFullPath))
                    File.Delete(newFullPath);
                fileInfo.CopyTo(newFullPath);
                imgList[index - 1].SetAttributeValue("src", newFullPath);
            }
            HtmlNode divNode = doch.DocumentNode.SelectSingleNode("//div");
            string newHtmStr = divNode.InnerHtml;
            File.WriteAllText(newHtmPath, newHtmStr,Encoding.GetEncoding("GB2312"));
        }
    }
}
