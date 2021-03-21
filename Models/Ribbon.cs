using System;
using System.IO;
using System.Windows.Media.Imaging;
using Autodesk.Revit.UI;

namespace Entools.Model
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    [Autodesk.Revit.Attributes.Regeneration(Autodesk.Revit.Attributes.RegenerationOption.Manual)]
    [Autodesk.Revit.Attributes.Journaling(Autodesk.Revit.Attributes.JournalingMode.NoCommandData)]

    public class Mark : IExternalApplication
    {
        static string AddInPath = typeof(Entools).Assembly.Location;
        static string ButtonIconsFolder = Path.GetDirectoryName(AddInPath);

        public Autodesk.Revit.UI.Result OnStartup(UIControlledApplication application)
        {
            try
            {
                RibbonPanel panel = application.CreateRibbonPanel("Test");
                PushButtonData list = new PushButtonData("Mark", "Mark", AddInPath, "Entools.Model.Entools")
                {
                    ToolTip = "Mark"
                };

                //list.LongDescription =
                // "This tool is designed for quick rename of a view. \n" +
                // "It includes adding a prefix, adding a suffix, search \n" +
                // "and replace characters in a view name."
                // ;

                // Context (F1) Help - new in 2013 
                //string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData); 

                string path = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

                ContextualHelp contextHelp = new ContextualHelp(ContextualHelpType.ChmFile, path + @"\Help.htm"); // hard coding for simplicity. 

                list.SetContextualHelp(contextHelp);

                PushButton billButton = panel.AddItem(list) as PushButton;
                //billButton.LargeImage = new BitmapImage(new Uri(Path.Combine(ButtonIconsFolder, "icons\\iconlarge.png"), UriKind.Absolute));
                //billButton.Image = new BitmapImage(new Uri(Path.Combine(ButtonIconsFolder, "icons\\iconsmall.png"), UriKind.Absolute));
                //CreateRibbonEntools(application);
                return Autodesk.Revit.UI.Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("EnToolsLt Sample", ex.ToString());
                return Autodesk.Revit.UI.Result.Failed;
            }
        }

        public Autodesk.Revit.UI.Result OnShutdown(UIControlledApplication application)
        {
            //application.ControlledApplication.DocumentOpened -= OnDocOpened;
            return Autodesk.Revit.UI.Result.Succeeded;
        }
    }
}