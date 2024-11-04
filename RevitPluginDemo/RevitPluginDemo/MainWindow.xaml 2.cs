using System.Windows;
using System.Windows.Media;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System;
using System.Linq;
using Autodesk.Revit.Creation;
using System.Collections.Generic;
using System.Xml.Linq;

namespace RevitPluginDemo
{
    public partial class MainWindow : Window
    {
        private ExternalCommandData _commandData;

        public MainWindow(ExternalCommandData commandData)
        {
            InitializeComponent();
            _commandData = commandData;  // 保存 commandData，供后续使用

            // 初始化 TextBox 占位符
            CommandInput.Text = "Please enter modeling instructions...";
            CommandInput.Foreground = Brushes.Gray;  // 设置占位符文本颜色
        }

        // 处理 TextBox 获得焦点事件
        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (CommandInput.Text == "Please enter modeling instructions...")
            {
                CommandInput.Text = "";  // 清空占位符文本
                CommandInput.Foreground = Brushes.Black;  // 设置输入文本颜色为黑色
            }
        }

        // 处理 TextBox 失去焦点事件
        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(CommandInput.Text))
            {
                CommandInput.Text = "Please enter modeling instructions...";  // 恢复占位符文本
                CommandInput.Foreground = Brushes.Gray;  // 设置占位符文本颜色
            }
        }

        // 如果你需要处理 TextChanged 事件，你可以保留这个方法
        // 否则可以忽略这个方法，不需要定义 TextChanged 事件处理逻辑
        private void CommandInput_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            // 在这里添加 TextChanged 事件的处理逻辑（如果需要的话）
        }

        // 发送指令按钮点击事件处理
        private async void SendCommand_Click(object sender, RoutedEventArgs e)
        {
            string userInput = CommandInput.Text;  // 获取用户输入的文本

            if (!string.IsNullOrWhiteSpace(userInput) && userInput != "Please enter modeling instructions...")
            {
                // 调用处理函数，将用户输入发送给 ChatGPT 后端
                string response = await SendToChatGPTAsync(userInput);

                // 显示 ChatGPT 返回的结果
                MessageBox.Show($"ChatGPT original response: {response}");

                // 进一步处理 ChatGPT 的结果，转换为 Revit API 调用
                ExecuteRevitCommand(response);
            }
            else
            {
                MessageBox.Show("Please enter a valid instruction");
            }
        }

        // 通过 HTTP 发送指令到 ChatGPT 后端服务
        private async Task<string> SendToChatGPTAsync(string command)
        {
            using (HttpClient client = new HttpClient())
            {
                var requestData = new
                {
                    command = command,
                    prompt = "Please return the response in a structured JSON format with dimensions and material for a 10 x 10 meters floor."
                };

                var content = new StringContent(JsonConvert.SerializeObject(requestData), Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PostAsync("http://localhost:5000/chatgpt", content);

                if (response.IsSuccessStatusCode)
                {
                    // 读取并返回 ChatGPT 的响应
                    return await response.Content.ReadAsStringAsync();
                }
                else
                {
                    throw new Exception("Unable to connect to ChatGPT backend service");
                }
            }
        }


        // 处理 ChatGPT 返回的结果，并使用 Revit API 进行建模
        private void ExecuteRevitCommand(string chatGPTResponse)
        {
            try
            {
                // 打印并检查 JSON 响应
                MessageBox.Show($"ChatGPT Response: {chatGPTResponse}");

                // 解析外层 JSON 响应
                var outerResponse = JsonConvert.DeserializeObject<dynamic>(chatGPTResponse);

                // 获取 "response" 字段中的字符串，并将其转换为 JSON 对象
                string innerResponseString = outerResponse.response.ToString();

                // 修复 JSON 格式问题，确保正确的 JSON 结构
                innerResponseString = innerResponseString.Replace("\n", "").Replace("\\", "").Replace("\" ", "\"").Trim();

                // 打印修复后的 JSON 字符串
                MessageBox.Show($"Inner JSON after cleanup: {innerResponseString}");

                // 将清理后的字符串再解析为 JSON 对象
                var innerResponse = JsonConvert.DeserializeObject<dynamic>(innerResponseString);

                // 提取长度、宽度和材料
                double length = innerResponse.length;
                double width = innerResponse.width;
                string material = innerResponse.material;

                // 使用 Autodesk.Revit.DB.Document 获取当前文档
                Autodesk.Revit.DB.Document doc = _commandData.Application.ActiveUIDocument.Document;

                using (Transaction trans = new Transaction(doc, "Creating floor slabs"))
                {
                    trans.Start();

                    CreateRoom();

                    trans.Commit();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Parsing incorrect or invalid responses: {ex.Message}");
            }
        }

        public void CreateRoom()
        {
            Autodesk.Revit.DB.Document doc = _commandData.Application.ActiveUIDocument.Document;

            // Global position of the room
            XYZ roomGlobalPosition = new XYZ(0, 0, 0);

            // Create the walls
            List<Wall> walls = new List<Wall>();
            walls.Add(CreateWall(doc, roomGlobalPosition, new XYZ(1, 0, 0), 2, 3)); // Wall1
            walls.Add(CreateWall(doc, roomGlobalPosition, new XYZ(2, 1, 0), 2, 3)); // Wall2
            walls.Add(CreateWall(doc, roomGlobalPosition, new XYZ(1, 2, 0), 2, 3)); // Wall3
            walls.Add(CreateWall(doc, roomGlobalPosition, new XYZ(0, 1, 0), 2, 3)); // Wall4

            // Create the columns
            List<FamilyInstance> columns = new List<FamilyInstance>();
            //columns.Add(CreateColumn(doc, roomGlobalPosition, new XYZ(0, 0, 0), 0.1, 3)); // Column1
            //columns.Add(CreateColumn(doc, roomGlobalPosition, new XYZ(2, 0, 0), 0.1, 3)); // Column2
            //columns.Add(CreateColumn(doc, roomGlobalPosition, new XYZ(2, 2, 0), 0.1, 3)); // Column3
            //columns.Add(CreateColumn(doc, roomGlobalPosition, new XYZ(0, 2, 0), 0.1, 3)); // Column4
        }

        private Wall CreateWall(Autodesk.Revit.DB.Document doc, XYZ roomPosition, XYZ localPosition, double length, double height)
        {
            Curve wallLine = Line.CreateBound(
                roomPosition + localPosition,
                roomPosition + localPosition + new XYZ(length, 0, 0)
            ) as Curve;

            FilteredElementCollector collector = new FilteredElementCollector(doc);
            ICollection<Element> collection = collector.OfClass(typeof(Level)).ToElements();
            var level = collection.First();

            Wall wall = Wall.Create(doc, wallLine, level.Id, false);
            wall.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM).Set(height);
            return wall;
        }

        private FamilyInstance CreateColumn(Autodesk.Revit.DB.Document doc, XYZ roomPosition, XYZ localPosition, double diameter, double height)
        {
            FamilySymbol columnType = GetColumnType(doc);

            if (!columnType.IsActive)
            {
                columnType.Activate();
                doc.Regenerate();
            }

            FilteredElementCollector collector = new FilteredElementCollector(doc);
            ICollection<Element> collection = collector.OfClass(typeof(Level)).ToElements();
            var level = collection.First();

            XYZ columnPosition = roomPosition + localPosition;
            FamilyInstance column = doc.Create.NewFamilyInstance(columnPosition, columnType, level,Autodesk.Revit.DB.Structure.StructuralType.Column);
            column.LookupParameter("Height").Set(height);
            column.LookupParameter("Diameter").Set(diameter);
            return column;
        }

        private FamilySymbol GetColumnType(Autodesk.Revit.DB.Document doc)
        {
            // Retrieve the column family type (assuming it's a default loaded family type)
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            collector.OfClass(typeof(FamilySymbol))
                     .OfCategory(BuiltInCategory.OST_StructuralColumns);

            foreach (FamilySymbol symbol in collector)
            {
                if (symbol.Name.Contains("Column"))
                {
                    return symbol;
                }
            }

            throw new InvalidOperationException("No column family type found in the document.");
        }






    }
}
