using System;
using System.Collections.Generic;
using System.Globalization;
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
using System.Windows.Shapes;

namespace SpawnScriptGenerator
{
    /// <summary>
    /// Interaction logic for ShowCode.xaml
    /// </summary>
    public partial class ShowCode : Window
    {
        public ShowCode(string initScriptCode, string descriptionScriptCode)
        {
            InitializeComponent();

            txtInitScriptCode.Text = initScriptCode;
            txtDescriptionScriptCode.Text = descriptionScriptCode;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }        
    }
}
