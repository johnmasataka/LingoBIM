using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RevitPluginDemo
{
    [Transaction(TransactionMode.Manual)]
    public class MyRevitPlugin : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // 打开 WPF 窗口并将 commandData 传递给它
            MainWindow window = new MainWindow(commandData);
            window.ShowDialog();  // 显示 WPF 窗口

            return Result.Succeeded;
        }
    }
}
