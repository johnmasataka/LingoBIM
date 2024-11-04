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
using Autodesk.Revit.DB.Structure;

//20241104_Work on GitHub

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

        private async Task<string> SendToChatGPTAsync(string command)
        {
            // Modify the command to include detailed instructions for ChatGPT
            string modifiedCommand = command + " Return the dimensions as a JSON without escape sequences.";

            using (HttpClient client = new HttpClient())
            {
                var requestData = new
                {
                    command = modifiedCommand, // Send the modified command
                    prompt = ""  // This remains empty or can be used for different purposes
                };

                var content = new StringContent(JsonConvert.SerializeObject(requestData), Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PostAsync("http://localhost:5000/chatgpt", content);

                if (response.IsSuccessStatusCode)
                {
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

                    CreateRoom(length, width); //room length and width

                    trans.Commit();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Parsing incorrect or invalid responses: {ex.Message}");
            }
        }

        public void CreateRoom(double length, double width)
        {
            Autodesk.Revit.DB.Document doc = _commandData.Application.ActiveUIDocument.Document;

            // Global position of the room
            XYZ roomGlobalPosition = new XYZ(0, 0, 0);

            // Create the walls
            List<Wall> walls = new List<Wall>();

            double offset = 4;
            XYZ p0 = new XYZ(0, 0, 0);
            XYZ p1 = new XYZ(0, width, 0);
            XYZ p2 = new XYZ(length, width, 0);
            XYZ p3 = new XYZ(length, 0, 0);

            XYZ r0 = p0 + new XYZ(-offset, -offset, 0);
            XYZ r1 = p1 + new XYZ(-offset, offset, 0);
            XYZ r2 = p2 + new XYZ(offset, offset, 0);
            XYZ r3 = p3 + new XYZ(offset, -offset, 0);


            walls.Add(CreateWall(doc, roomGlobalPosition, p0, p1, 102)); // Wall1
            walls.Add(CreateWall(doc, roomGlobalPosition, p1, p2, 102)); // Wall2
            walls.Add(CreateWall(doc, roomGlobalPosition, p2, p3, 102)); // Wall3
            walls.Add(CreateWall(doc, roomGlobalPosition, p3, p0, 102)); // Wall4

            List<XYZ> points = new List<XYZ>
            {
            p0,   // Wall1 start
            p1,   // Wall1 end, Wall2 start
            p2,   // Wall2 end, Wall3 start
            p3,   // Wall3 end, Wall4 start
            p0    // Wall4 end (closing the loop)
            };

            List<XYZ> roof_points = new List<XYZ>
            {
            r0,   // Wall1 start
            r1,   // Wall1 end, Wall2 start
            r2,   // Wall2 end, Wall3 start
            r3,   // Wall3 end, Wall4 start
            r0    // Wall4 end (closing the loop)
            };

            CreateFloor(doc, points);

            CreateRoof(doc, roof_points, 0.25);

            // Create door and window on Wall1
            CreateDoorAndWindow(doc, walls[0], walls[2]);  // Pass Wall1 for door and window placement

            // Create the columns
            List<FamilyInstance> columns = new List<FamilyInstance>();
            //columns.Add(CreateColumn(doc, roomGlobalPosition, new XYZ(0, 0, 0), 0.1, 3)); // Column1
            //columns.Add(CreateColumn(doc, roomGlobalPosition, new XYZ(2, 0, 0), 0.1, 3)); // Column2
            //columns.Add(CreateColumn(doc, roomGlobalPosition, new XYZ(2, 2, 0), 0.1, 3)); // Column3
            //columns.Add(CreateColumn(doc, roomGlobalPosition, new XYZ(0, 2, 0), 0.1, 3)); // Column4
        }

        private Wall CreateWall(Autodesk.Revit.DB.Document doc, XYZ roomPosition, XYZ startPosition, XYZ endPosition, double height)
        {
            Curve wallLine = Line.CreateBound(
                roomPosition + startPosition,
                roomPosition + endPosition
            ) as Curve;

            //level
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            ICollection<Element> collection = collector.OfClass(typeof(Level)).ToElements();
            var level = collection.First();

            Wall wall = Wall.Create(doc, wallLine, level.Id, false);
            wall.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM).Set(height);
            return wall;
        }

        public void CreateFloor(Autodesk.Revit.DB.Document doc, List<XYZ> points)
        {

            ElementId floorTypeId = Floor.GetDefaultFloorType(doc, false);
            // Create a CurveArray to define the floor boundary
            CurveLoop profile = new CurveLoop();
            for (int i = 0; i < points.Count - 1; i++)
            {
                Line line = Line.CreateBound(points[i], points[i + 1]);
                profile.Append(line);
            }

            // Get the default floor type from the document
            FilteredElementCollector collector = new FilteredElementCollector(doc)
                .OfClass(typeof(FloorType));
            FloorType floorType = collector.FirstElement() as FloorType;


            //level
            FilteredElementCollector collector_floor = new FilteredElementCollector(doc);
            ICollection<Element> collection = collector_floor.OfClass(typeof(Level)).ToElements();
            var level = collection.First();

            // Create the floor using the CurveArray
            Floor.Create(doc, new List<CurveLoop> { profile }, floorTypeId, level.Id);
        }

        public void CreateRoof(Autodesk.Revit.DB.Document doc, List<XYZ> points, double slope)
        {

            Autodesk.Revit.ApplicationServices.Application application = doc.Application;
            // Define the footprint for the roof based on user selection
            CurveArray footprint = application.Create.NewCurveArray();
            for (int i = 0; i < points.Count - 1; i++)
            {
                Curve line = Line.CreateBound(points[i], points[i + 1]) as Curve;
                footprint.Append(line);
            }

            // Get the default floor type from the document
            //level
            FilteredElementCollector collector_roof = new FilteredElementCollector(doc);
            ICollection<Element> collection = collector_roof.OfClass(typeof(Level)).ToElements();
            var level = collection.Last() as Level;

            FilteredElementCollector collector = new FilteredElementCollector(doc);
            collector.OfClass(typeof(RoofType));
            RoofType roofType = collector.FirstElement() as RoofType;

            ModelCurveArray footPrintToModelCurveMapping = new ModelCurveArray();

            FootPrintRoof footprintRoof = doc.Create.NewFootPrintRoof(footprint, level, roofType, out footPrintToModelCurveMapping);

            ModelCurveArrayIterator iterator = footPrintToModelCurveMapping.ForwardIterator();

            iterator.Reset();
            while (iterator.MoveNext())
            {
                ModelCurve modelCurve = iterator.Current as ModelCurve;
                footprintRoof.set_DefinesSlope(modelCurve, true);
                footprintRoof.set_SlopeAngle(modelCurve, slope);
            }

        }

        private void CreateDoorAndWindow(Autodesk.Revit.DB.Document doc, Wall wall1, Wall wall3)
        {
            // Get door and window family symbols (assuming they are loaded in the project)
            FilteredElementCollector doorCollector = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_Doors);
            FamilySymbol doorSymbol = doorCollector.FirstElement() as FamilySymbol;

            FilteredElementCollector windowCollector = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_Windows);

            //FamilySymbol windowSymbol = windowCollector.ToList().Where(x => x.Name.Equals("34\" x 36\"")).First() as FamilySymbol;
            FamilySymbol windowSymbol = windowCollector.ToList().Where(x => (x.Name.Equals("34\" x 36\"") && ((FamilySymbol)x).Family.Name.Equals("Window-Casement-Double"))).First() as FamilySymbol;
            //FamilySymbol windowSymbol = windowCollector.ToList()[2] as FamilySymbol;
            //FamilySymbol windowSymbol = windowCollector.FirstElement() as FamilySymbol;

            // Ensure the door and window symbols are active
            if (!doorSymbol.IsActive)
            {
                doorSymbol.Activate();
                doc.Regenerate();
            }
            if (!windowSymbol.IsActive)
            {
                windowSymbol.Activate();
                doc.Regenerate();
            }

            // Place the door on Wall1
            LocationCurve wallLocation = wall1.Location as LocationCurve;
            XYZ doorLocation = wallLocation.Curve.Evaluate(0.6, true); // Place the door at the midpoint of Wall1
            Level level = doc.GetElement(wall1.LevelId) as Level;

            doc.Create.NewFamilyInstance(doorLocation, doorSymbol, wall1, level, StructuralType.NonStructural);

            // Place windows on Wall1
            double[] windowPositionsWall1 = { 0.2, 0.4, 0.85 }; // Example positions for windows along Wall1

            foreach (double position in windowPositionsWall1)
            {
                XYZ windowLocation = wallLocation.Curve.Evaluate(position, true); // Place windows at specified positions along Wall1
                FamilyInstance windowInstance = doc.Create.NewFamilyInstance(windowLocation, windowSymbol, wall1, level, StructuralType.NonStructural);

                // Set the window sill height (level + 3 feet)
                Parameter sillHeightParam = windowInstance.get_Parameter(BuiltInParameter.INSTANCE_SILL_HEIGHT_PARAM);
                if (sillHeightParam != null && !sillHeightParam.IsReadOnly)
                {
                    double sillHeight = UnitUtils.ConvertToInternalUnits(3, UnitTypeId.Feet); // Convert 3 feet to internal units
                    sillHeightParam.Set(sillHeight);
                }
            }

            // Place windows on Wall3
            LocationCurve wallLocation3 = wall3.Location as LocationCurve;
            double[] windowPositionsWall3 = { 0.3, 0.75 }; // Example positions for windows along Wall3

            foreach (double position in windowPositionsWall3)
            {
                XYZ windowLocation = wallLocation3.Curve.Evaluate(position, true); // Place windows at specified positions along Wall3
                FamilyInstance windowInstance = doc.Create.NewFamilyInstance(windowLocation, windowSymbol, wall3, level, StructuralType.NonStructural);

                // Set the window sill height (level + 3 feet)
                Parameter sillHeightParam = windowInstance.get_Parameter(BuiltInParameter.INSTANCE_SILL_HEIGHT_PARAM);
                if (sillHeightParam != null && !sillHeightParam.IsReadOnly)
                {
                    double sillHeight = UnitUtils.ConvertToInternalUnits(3, UnitTypeId.Feet); // Convert 3 feet to internal units
                    sillHeightParam.Set(sillHeight);
                }
            }



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
            FamilyInstance column = doc.Create.NewFamilyInstance(columnPosition, columnType, level, Autodesk.Revit.DB.Structure.StructuralType.Column);
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