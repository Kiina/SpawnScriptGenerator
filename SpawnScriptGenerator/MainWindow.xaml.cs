using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SQMImportExport.Common;
using SQMImportExport.Import;
using System.IO;

namespace SpawnScriptGenerator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private FileContainer sqmFile;
        private FileContainer sqfFile;
        private List<string> unitsTypeMan;
        private string eastHQ = "_east";
        private string westHQ = "_west";
        private string indHQ = "_guer";
        private string civHQ = "_civ";

        public MainWindow()
        {
            InitializeComponent();

            if (File.Exists("configUnitMan.txt"))
                unitsTypeMan = loadExclFile("configUnitMan.txt");
            else
                MessageBox.Show("Could not load unit type list!\nThis might result into errors in the script!", "Error loading file");
        }

        private void btnSQMFile_Click(object sender, RoutedEventArgs e)
        {
            FileContainer file = openDialog("mission", ".sqm", "SQM Datein (*.sqm)|*.sqm");
            sqmFile = file;
            if (file != null)
                txtSQMFile.Text = file.ToString();
        }

        private void btnSQFFile_Click(object sender, RoutedEventArgs e)
        {
            FileContainer file = saveDialog("script", ".sqf", "SQF Datei (*.sqf)|*.sqf|Text Datei (*.txt)|*.txt|All files (*.*)|*.*");
            sqfFile = file;
            if (file != null)
                txtSQFFile.Text = file.ToString();         
        }

        private void btnCreateScript_Click(object sender, RoutedEventArgs e)
        {
            if (!checkFiles())
                return;

            SqmContentsBase sqmContents;
            StringBuilder scriptCode = new StringBuilder();
            DateTime sTime, eTime;

            sTime = DateTime.Now;

            sqmContents = loadSQMFile(sqmFile.ToString());
            if (sqmContents == null)
                return;

            string fileVersionString = "Arma 3";

            if (sqmContents.Version == 11)
            {
                MessageBox.Show("Only Arma 3 supported atm!");
                return;
            }

            scriptCode.Append(generateSQFHeader(sqmFile.ToString(), sqmContents.Version.ToString(), fileVersionString, (bool)!chkExlComments.IsChecked));
            scriptCode.Append(generateSQFUnits(sqmContents, (bool)chkOpfor.IsChecked, (bool)chkBlufor.IsChecked, (bool)chkIndependent.IsChecked, (bool)chkCivilian.IsChecked, (bool)chkExlPlayer.IsChecked, (bool)chkExlPlayable.IsChecked, (bool)!chkExlComments.IsChecked));

            saveFile(sqfFile.ToString(), scriptCode.ToString());

            eTime = DateTime.Now;
            this.Title = "SQM Scriptifyer - Finished in: " + (eTime - sTime).TotalSeconds.ToString();
        }

        private void btnCreateInitFiles_Click(object sender, RoutedEventArgs e)
        {
            if (!checkFiles())
                return;

            string scriptCode = "";

            scriptCode = "if !(hasInterface or isServer) then\n"
                            + "{\n"
                            + "\tHeadlessVariable = true;\n"
                            + "\tpublicVariable \"HeadlessVariable\";\n"
                            + "\texecVM \"" + sqfFile.fileName + "\";\n"
                            + "};";

            FileContainer initHCFile = new FileContainer(sqfFile.fileName, sqfFile.filePath);
            initHCFile.fileName = "initHC.sqf";

            saveFile(initHCFile.ToString(), scriptCode);

            string initCode = "if (isServer) then\n"
                            + "{\n"
                            + "\tif (isNil \"HeadlessVariable\") then\n"
                            + "\t{\n"
                            + "\t\texecVM \"" +sqfFile.fileName + "\";\n"
                            + "\t};\n"
                            + "};";

            string descriptionCode = "class CfgFunctions\n"
                                        + "{\n"
                                        + "\tclass myTag\n"
                                        + "\t{\n"
                                        + "\t\tclass myCategory\n"
                                        + "\t\t{\n"
                                        + "\t\t\tclass myFunction\n"
                                        + "\t\t\t{\n"
                                        + "\t\t\t\tpostInit = 1;\n"
                                        + "\t\t\t\tfile = \"initHC.sqf\";\n"
                                        + "\t\t\t};\n"
                                        + "\t\t};\n"
                                        + "\t};\n"
                                        + "};\n";

            ShowCode sc = new ShowCode(initCode, descriptionCode);
            sc.Show();
        }

        private bool checkFiles()
        {
            bool result = true;

            if (sqmFile == null || sqmFile.ToString() == "")
            {
                MessageBox.Show("Please specify the Source Mission file (.sqm)", "No mission file specified");
                result = false;
            }

            if (sqfFile == null || sqfFile.ToString() == "")
            {
                MessageBox.Show("Please specify the spawn script file (.sqf)", "No spawn script file specified");
                result = false;
            }

            return result;
        }

        private string generateSQFHeader(string sourceFilePath, string fileVersionNum, string fileVersionString, bool comments)
        {
            StringBuilder code = new StringBuilder();

            code.Append("/*\n");
            code.Append(" * Created with HCSQMtoSQF Converter\n");
            code.Append(" *\n");
            code.Append(" * Source: " + sourceFilePath + "\n");
            code.Append(" * File Version: " + fileVersionNum + " | " + fileVersionString + "\n");
            code.Append(" * Date: " + DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString() + "\n");
            code.Append(" */\n\n");
            if (comments)
            {
                code.Append(westHQ + " = createCenter west;\t\t\t\t// BLUFOR (NATO)\n");
                code.Append(eastHQ + " = createCenter east;\t\t\t\t// OPFOR (CSAT)\n");
                code.Append(indHQ + " = createCenter resistance;\t\t// Independent (AAF)\n");
                code.Append(civHQ + "  = createCenter civilian;\t\t\t// Civilians\n\n\n");
            }
            else
            {
                code.Append(westHQ + " = createCenter west;\n");
                code.Append(eastHQ + " = createCenter east;\n");
                code.Append(indHQ + " = createCenter resistance;\n");
                code.Append(civHQ + "  = createCenter civilian;\n\n\n");
            }            

            return code.ToString();
        }

        private string generateSQFUnits(SqmContentsBase sqm, bool opfor, bool blufor, bool independent, bool civilian, bool exclPlayer, bool exclPlayable, bool comments)
        {
            string result = "", opforCode = "", bluforCode = "", independentCode = "", civilianCode = "";

            result += "/******************\n"
                      + " * UNITS & GROUPS *\n"
                      + " ******************/\n\n";

            List<SQMImportExport.ArmA3.Vehicle> groups = (List<SQMImportExport.ArmA3.Vehicle>)sqm.Mission.Groups;

            for (int i = 0; i < groups.Count; i++)
            {
                List<SQMImportExport.ArmA3.Vehicle> content = (List<SQMImportExport.ArmA3.Vehicle>)groups[i].Vehicles;
                StringBuilder code = new StringBuilder();
                string groupName = "";

                groupName = "_group_" + groups[i].Side.ToLower() + "_" + (i + 1).ToString();

                if (comments)
                    code.Append("// Begin of Group " + groupName + "\n");
                
                code.Append(groupName + " = createGroup _" + groups[i].Side.ToLower() + ";\n");

                for (int j = 0; j < content.Count; j++)
                {
                    string unitName = groupName + "_unit_" + (j + 1).ToString();

                    if (exclPlayer && content[j].Player == "PLAYER COMMANDER")
                    {
                        if (content.Count >= 1 && content.Count > (j+1) && content[j].Leader.HasValue && content[j].Leader.Value == 1)
                            content[j + 1].Leader = 1;
                    }
                    else if (exclPlayable && content[j].Player == "PLAY CDG")
                    {
                        if (content.Count >= 1 && content.Count > (j+1) && content[j].Leader.HasValue && content[j].Leader.Value == 1)
                            content[j + 1].Leader = 1;
                    }
                    else
                    {
                        if (comments)
                            code.Append("\t" + "// Begin of Unit " + unitName + "\n");

                        code.Append("\t" + "if (true) then\n\t{\n");

                        if (unitsTypeMan.Any(s => s.Equals(content[j].VehicleName, StringComparison.OrdinalIgnoreCase)))
                        {
                            code.Append("\t\t" + unitName + " = " + groupName + " createUnit [\"" + content[j].VehicleName + "\", [" + content[j].Position.X.ToString().Replace(',', '.') + ", " + content[j].Position.Y.ToString().Replace(',', '.') + ", " + (content[j].OffsetY ?? 0).ToString().Replace(',', '.') + "], [], " + (content[j].Placement ?? 0).ToString() + ", \"" + (content[j].Special ?? "CAN_COLLIDE") + "\"];\n");
                        }
                        else
                        {
                            code.Append("\t\t" + unitName + " = createVehicle [\"" + content[j].VehicleName + "\"" + ", [" + content[j].Position.X.ToString().Replace(',', '.') + ", " + content[j].Position.Y.ToString().Replace(',', '.') + ", " + (content[j].OffsetY ?? 0).ToString().Replace(',', '.') + "], [], " + (content[j].Placement ?? 0).ToString() + ", \"" + (content[j].Special ?? "CAN_COLLIDE") + "\"];\n");
                            code.Append("\t\t" + "createVehicleCrew " + unitName + ";\n");
                            code.Append("\t\t" + "[" + unitName + "] joinSilent " + groupName + ";\n");
                        }

                        if (content[j].Azimut.HasValue)
                            code.Append("\t\t" + unitName + " setDir " + content[j].Azimut.ToString().Replace(',', '.') + ";\n");
                        if (content[j].Skill.HasValue)
                            code.Append("\t\t" + unitName + " setUnitAbility " + content[j].Skill.ToString().Replace(',', '.') + ";\n");
                        if (content[j].Rank != null && content[j].Rank != "")
                            code.Append("\t\t" + unitName + " setRank \"" + content[j].Rank + "\";\n");
                        if (content[j].Health.HasValue)
                            code.Append("\t\t" + unitName + " setDamage " + (1 - content[j].Health).ToString().Replace(',', '.') + ";\n");
                        if (content[j].Text != null && content[j].Text != "")
                            code.Append("\t\t" + unitName + " setVehicleVarName \"" + content[j].Text + "\";\n");
                        if (content[j].Leader.HasValue)
                            code.Append("\t\t" + groupName + " selectLeader " + unitName + ";\n");

                        code.Append("\t};\n");

                        if (comments)
                            code.Append("\t// End of Unit " + unitName + "\n");
                    }              
                }

                if (groups[i].Waypoints.Count > 0)
                {
                    code.Append("\t" + "// Waypoints for " + groupName + "\n");
                    for (int k = 0; k < groups[i].Waypoints.Count; k++)
                    {
                        code.Append("\t" + "// Waypoint #" + (k + 1).ToString() + "\n");
                        code.Append("\t" + "_wp = " + groupName + " addWaypoint[[" + groups[i].Waypoints[k].Position.X.ToString().Replace(',', '.') + ", " + groups[i].Waypoints[k].Position.Y.ToString().Replace(',', '.') + ", 0], " + (groups[i].Waypoints[k].Placement ?? 0).ToString() + ", " + (k + 1).ToString() + "];\n");
                        code.Append("\t" + "[" + groupName + ", " + (k + 1).ToString() + "] setWaypointBehaviour \"" + (groups[i].Waypoints[k].Combat ?? "UNCHANGED").ToUpper() + "\";\n");
                        code.Append("\t" + "[" + groupName + ", " + (k + 1).ToString() + "] setWaypointCombatMode \"" + (groups[i].Waypoints[k].CombatMode ?? "NO CHANGE").ToUpper() + "\";\n");
                        code.Append("\t" + "[" + groupName + ", " + (k + 1).ToString() + "] setWaypointCompletionRadius " + (groups[i].Waypoints[k].CompletitionRadius ?? 0).ToString() + ";\n");
                        code.Append("\t" + "[" + groupName + ", " + (k + 1).ToString() + "] setWaypointFormation \"" + (groups[i].Waypoints[k].Formation ?? "NO CHANGE").ToUpper() + "\";\n");
                        code.Append("\t" + "[" + groupName + ", " + (k + 1).ToString() + "] setWaypointSpeed \"" + (groups[i].Waypoints[k].Speed ?? "UNCHANGED").ToUpper() + "\";\n");
                        code.Append("\t" + "[" + groupName + ", " + (k + 1).ToString() + "] setWaypointStatements [\"true\", \"\"];\n");
                        code.Append("\t" + "[" + groupName + ", " + (k + 1).ToString() + "] setWaypointType \"" + (groups[i].Waypoints[k].Type ?? "MOVE").ToUpper() + "\";\n");
                    }
                }

                if (comments && i < groups.Count-1)
                    code.Append("// End of Group " + groupName + "\n");
                else if (i == groups.Count-1)
                    code.Append("// End of Group " + groupName);
                
                if (i < groups.Count-1)
                    code.Append("\n");

                switch(groups[i].Side)
                {
                    case "WEST":
                        bluforCode += code;
                        break;
                    case "EAST":
                        opforCode += code;
                        break;
                    case "GUER":
                        independentCode += code;
                        break;
                    case "CIV":
                        civilianCode += code;
                        break;
                }
            }

            if (opfor)
                result += opforCode;
            if (blufor)
                result += bluforCode;
            if (independent)
                result += independentCode;
            if (civilian)
                result += civilianCode;

            return result;
        }

        private void saveFile(string filePath, string content)
        {
            try
            {
                StreamWriter sw = new StreamWriter(filePath);

                sw.Write(content);

                sw.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error writing file!");
                return;
            }            
        }

        private FileContainer openDialog(string filename = "mission", string extension = ".txt", string filter = "Text Datei (*.txt)|*txt")
        {
            Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog();
            ofd.FileName = filename;
            ofd.DefaultExt = extension;
            ofd.Filter = filter;

            bool? result = ofd.ShowDialog();

            if (result == true)
                return new FileContainer(ofd.SafeFileName, ofd.FileName.Replace(ofd.SafeFileName, ""));
            else
                return null;
        }

        private FileContainer saveDialog(string filename = "script", string extension = ".txt", string filter = "Text Datei (*.txt)|*.txt")
        {
            Microsoft.Win32.SaveFileDialog sfd = new Microsoft.Win32.SaveFileDialog();
            sfd.FileName = filename;
            sfd.DefaultExt = extension;
            sfd.Filter = filter;

            bool? result = sfd.ShowDialog();

            if (result == true)
                return new FileContainer(sfd.SafeFileName, sfd.FileName.Replace(sfd.SafeFileName, ""));
            else 
                return null;
        }

        private SqmContentsBase loadSQMFile(string path)
        {
            try
            {
                using (FileStream importStream = new FileStream(sqmFile.ToString(), FileMode.Open))
                {
                    SqmImporter imp = new SqmImporter();
                    SqmContentsBase sqmContents = imp.Import(importStream);
                    return sqmContents;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.InnerException.Message.ToString());
                return null;
            }
        }

        private List<string> loadExclFile(string path)
        {
            List<string> l = new List<string>();
            try
            {
                StreamReader sr = new StreamReader(path);
            
                while(!sr.EndOfStream)
                {
                    l.Add(sr.ReadLine());
                }

                sr.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error reading " + path + " file!");
            }   

            return l;
        }
    }
}
