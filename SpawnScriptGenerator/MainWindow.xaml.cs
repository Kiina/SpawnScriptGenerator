using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using SQMImportExport.Common;
using SQMImportExport.Import;
using System.IO;

namespace SpawnScriptGenerator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private FileContainer _sqmFile;
        private FileContainer _sqfFile;
        private readonly List<string> _unitsTypeMan;
        private const string EastHq = "_east";
        private const string WestHq = "_west";
        private const string IndHq = "_guer";
        private const string CivHq = "_civ";

        public MainWindow()
        {
            InitializeComponent();

            if (File.Exists("configUnitMan.txt"))
                _unitsTypeMan = LoadExclFile("configUnitMan.txt");
            else
                MessageBox.Show("Could not load unit type list!\nThis might result into errors in the script!", "Error loading file");
        }

        private void btnSQMFile_Click(object sender, RoutedEventArgs e)
        {
            var file = OpenDialog("mission", ".sqm", "SQM Datein (*.sqm)|*.sqm");
            _sqmFile = file;
            if (file != null)
                TxtSqmFile.Text = file.ToString();
        }

        private void btnSQFFile_Click(object sender, RoutedEventArgs e)
        {
            var file = SaveDialog("script", ".sqf", "SQF Datei (*.sqf)|*.sqf|Text Datei (*.txt)|*.txt|All files (*.*)|*.*");
            _sqfFile = file;
            if (file != null)
                TxtSqfFile.Text = file.ToString();         
        }

        private void btnCreateScript_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckFiles())
                return;

            var scriptCode = new StringBuilder();
            var sTime = DateTime.Now;

            var sqmContents = LoadSqmFile();
            if (sqmContents == null)
                return;

            const string fileVersionString = "Arma 3";

            if (sqmContents.Version == 11)
            {
                MessageBox.Show("Only Arma 3 supported atm!");
                return;
            }

            scriptCode.Append(GenerateSqfHeader(_sqmFile.ToString(), sqmContents.Version.ToString(), fileVersionString, (bool)!ChkExlComments.IsChecked));
            scriptCode.Append(generateSQFUnits(sqmContents, ChkOpfor.IsChecked != null && (bool)ChkOpfor.IsChecked, ChkBlufor.IsChecked != null && (bool)ChkBlufor.IsChecked, ChkIndependent.IsChecked != null && (bool)ChkIndependent.IsChecked, ChkCivilian.IsChecked != null && (bool)ChkCivilian.IsChecked, ChkExlPlayer.IsChecked != null && (bool)ChkExlPlayer.IsChecked, ChkExlPlayable.IsChecked != null && (bool)ChkExlPlayable.IsChecked, (bool)!ChkExlComments.IsChecked));

            SaveFile(_sqfFile.ToString(), scriptCode.ToString());

            var eTime = DateTime.Now;
            Title = "SQM Scriptifyer - Finished in: " + (eTime - sTime).TotalSeconds.ToString(CultureInfo.InvariantCulture);
        }

        private void btnCreateInitFiles_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckFiles())
                return;

            var scriptCode = "if !(hasInterface or isServer) then\n"
                                + "{\n"
                                + "\tHeadlessVariable = true;\n"
                                + "\tpublicVariable \"HeadlessVariable\";\n"
                                + "\texecVM \"" + _sqfFile.FileName + "\";\n"
                                + "};";

            var initHcFile = new FileContainer(_sqfFile.FileName, _sqfFile.FilePath) {FileName = "initHC.sqf"};

            SaveFile(initHcFile.ToString(), scriptCode);

            var initCode = "if (isServer) then\n"
                            + "{\n"
                            + "\tif (isNil \"HeadlessVariable\") then\n"
                            + "\t{\n"
                            + "\t\texecVM \"" +_sqfFile.FileName + "\";\n"
                            + "\t};\n"
                            + "};";

            const string descriptionCode = "class CfgFunctions\n"
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

            var sc = new ShowCode(initCode, descriptionCode);
            sc.Show();
        }

        private bool CheckFiles()
        {
            var result = true;

            if (_sqmFile == null || _sqmFile.ToString() == "")
            {
                MessageBox.Show("Please specify the Source Mission file (.sqm)", "No mission file specified");
                result = false;
            }

            if (_sqfFile == null || _sqfFile.ToString() == "")
            {
                MessageBox.Show("Please specify the spawn script file (.sqf)", "No spawn script file specified");
                result = false;
            }

            return result;
        }

        private static string GenerateSqfHeader(string sourceFilePath, string fileVersionNum, string fileVersionString, bool comments)
        {
            var code = new StringBuilder();

            code.Append("/*\n");
            code.Append(" * Created with HCSQMtoSQF Converter\n");
            code.Append(" *\n");
            code.Append(" * Source: " + sourceFilePath + "\n");
            code.Append(" * File Version: " + fileVersionNum + " | " + fileVersionString + "\n");
            code.Append(" * Date: " + DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString() + "\n");
            code.Append(" */\n\n");
            if (comments)
            {
                code.Append(WestHq + " = createCenter west;\t\t\t\t// BLUFOR (NATO)\n");
                code.Append(EastHq + " = createCenter east;\t\t\t\t// OPFOR (CSAT)\n");
                code.Append(IndHq + " = createCenter resistance;\t\t// Independent (AAF)\n");
                code.Append(CivHq + "  = createCenter civilian;\t\t\t// Civilians\n\n\n");
            }
            else
            {
                code.Append(WestHq + " = createCenter west;\n");
                code.Append(EastHq + " = createCenter east;\n");
                code.Append(IndHq + " = createCenter resistance;\n");
                code.Append(CivHq + "  = createCenter civilian;\n\n\n");
            }            

            return code.ToString();
        }

        private string generateSQFUnits(SqmContentsBase sqm, bool opfor, bool blufor, bool independent, bool civilian, bool exclPlayer, bool exclPlayable, bool comments)
        {
            string result = "", opforCode = "", bluforCode = "", independentCode = "", civilianCode = "";

            result += "/******************\n"
                      + " * UNITS & GROUPS *\n"
                      + " ******************/\n\n";

            var groups = (List<SQMImportExport.ArmA3.Vehicle>)sqm.Mission.Groups;

            for (var i = 0; i < groups.Count; i++)
            {
                var content = (List<SQMImportExport.ArmA3.Vehicle>)groups[i].Vehicles;
                var code = new StringBuilder();

                var groupName = "_group_" + groups[i].Side.ToLower() + "_" + (i + 1);

                if (comments)
                    code.Append("// Begin of Group " + groupName + "\n");
                
                code.Append(groupName + " = createGroup _" + groups[i].Side.ToLower() + ";\n");

                for (var j = 0; j < content.Count; j++)
                {
                    var unitName = groupName + "_unit_" + (j + 1);

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

                        if (_unitsTypeMan.Any(s => s.Equals(content[j].VehicleName, StringComparison.OrdinalIgnoreCase)))
                        {
                            code.Append("\t\t" + unitName + " = " + groupName + " createUnit [\"" + content[j].VehicleName + "\", [" + content[j].Position.X.ToString(CultureInfo.InvariantCulture).Replace(',', '.') + ", " + content[j].Position.Y.ToString(CultureInfo.InvariantCulture).Replace(',', '.') + ", " + (content[j].OffsetY ?? 0).ToString(CultureInfo.InvariantCulture).Replace(',', '.') + "], [], " + (content[j].Placement ?? 0) + ", \"" + (content[j].Special ?? "CAN_COLLIDE") + "\"];\n");
                        }
                        else
                        {
                            code.Append("\t\t" + unitName + " = createVehicle [\"" + content[j].VehicleName + "\"" + ", [" + content[j].Position.X.ToString(CultureInfo.InvariantCulture).Replace(',', '.') + ", " + content[j].Position.Y.ToString(CultureInfo.InvariantCulture).Replace(',', '.') + ", " + (content[j].OffsetY ?? 0).ToString(CultureInfo.InvariantCulture).Replace(',', '.') + "], [], " + (content[j].Placement ?? 0) + ", \"" + (content[j].Special ?? "CAN_COLLIDE") + "\"];\n");
                            code.Append("\t\t" + "createVehicleCrew " + unitName + ";\n");
                            code.Append("\t\t" + "[" + unitName + "] joinSilent " + groupName + ";\n");
                        }

                        if (content[j].Azimut.HasValue)
                            code.Append("\t\t" + unitName + " setDir " + content[j].Azimut.ToString().Replace(',', '.') + ";\n");
                        if (content[j].Skill.HasValue)
                            code.Append("\t\t" + unitName + " setUnitAbility " + content[j].Skill.ToString().Replace(',', '.') + ";\n");
                        if (!string.IsNullOrEmpty(content[j].Rank))
                            code.Append("\t\t" + unitName + " setRank \"" + content[j].Rank + "\";\n");
                        if (content[j].Health.HasValue)
                            code.Append("\t\t" + unitName + " setDamage " + (1 - content[j].Health).ToString().Replace(',', '.') + ";\n");
                        if (!string.IsNullOrEmpty(content[j].Text))
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
                    for (var k = 0; k < groups[i].Waypoints.Count; k++)
                    {
                        code.Append("\t" + "// Waypoint #" + (k + 1) + "\n");
                        code.Append("\t" + "_wp = " + groupName + " addWaypoint[[" + groups[i].Waypoints[k].Position.X.ToString(CultureInfo.InvariantCulture).Replace(',', '.') + ", " + groups[i].Waypoints[k].Position.Y.ToString(CultureInfo.InvariantCulture).Replace(',', '.') + ", 0], " + (groups[i].Waypoints[k].Placement ?? 0) + ", " + (k + 1) + "];\n");
                        code.Append("\t" + "[" + groupName + ", " + (k + 1) + "] setWaypointBehaviour \"" + (groups[i].Waypoints[k].Combat ?? "UNCHANGED").ToUpper() + "\";\n");
                        code.Append("\t" + "[" + groupName + ", " + (k + 1) + "] setWaypointCombatMode \"" + (groups[i].Waypoints[k].CombatMode ?? "NO CHANGE").ToUpper() + "\";\n");
                        code.Append("\t" + "[" + groupName + ", " + (k + 1) + "] setWaypointCompletionRadius " + (groups[i].Waypoints[k].CompletitionRadius ?? 0) + ";\n");
                        code.Append("\t" + "[" + groupName + ", " + (k + 1) + "] setWaypointFormation \"" + (groups[i].Waypoints[k].Formation ?? "NO CHANGE").ToUpper() + "\";\n");
                        code.Append("\t" + "[" + groupName + ", " + (k + 1) + "] setWaypointSpeed \"" + (groups[i].Waypoints[k].Speed ?? "UNCHANGED").ToUpper() + "\";\n");
                        code.Append("\t" + "[" + groupName + ", " + (k + 1) + "] setWaypointStatements [\"true\", \"\"];\n");
                        code.Append("\t" + "[" + groupName + ", " + (k + 1) + "] setWaypointType \"" + (groups[i].Waypoints[k].Type ?? "MOVE").ToUpper() + "\";\n");
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

        private static void SaveFile(string filePath, string content)
        {
            try
            {
                var sw = new StreamWriter(filePath);

                sw.Write(content);

                sw.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error writing file!");
            }            
        }

        private static FileContainer OpenDialog(string filename = "mission", string extension = ".txt", string filter = "Text Datei (*.txt)|*txt")
        {
            var ofd = new Microsoft.Win32.OpenFileDialog
            {
                FileName = filename,
                DefaultExt = extension,
                Filter = filter
            };

            var result = ofd.ShowDialog();

            return result == true ? new FileContainer(ofd.SafeFileName, ofd.FileName.Replace(ofd.SafeFileName, "")) : null;
        }

        private static FileContainer SaveDialog(string filename = "script", string extension = ".txt", string filter = "Text Datei (*.txt)|*.txt")
        {
            var sfd = new Microsoft.Win32.SaveFileDialog
            {
                FileName = filename,
                DefaultExt = extension,
                Filter = filter
            };

            var result = sfd.ShowDialog();

            return result == true ? new FileContainer(sfd.SafeFileName, sfd.FileName.Replace(sfd.SafeFileName, "")) : null;
        }

        private SqmContentsBase LoadSqmFile()
        {
            try
            {
                using (var importStream = new FileStream(_sqmFile.ToString(), FileMode.Open))
                {
                    var imp = new SqmImporter();
                    var sqmContents = imp.Import(importStream);
                    return sqmContents;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.InnerException.Message);
                return null;
            }
        }

        private static List<string> LoadExclFile(string path)
        {
            var l = new List<string>();
            try
            {
                var sr = new StreamReader(path);
            
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
