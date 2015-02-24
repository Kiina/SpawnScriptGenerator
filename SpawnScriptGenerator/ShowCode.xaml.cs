using System.Windows;

namespace SpawnScriptGenerator
{
    /// <summary>
    /// Interaction logic for ShowCode.xaml
    /// </summary>
    public partial class ShowCode
    {
        public ShowCode(string initScriptCode, string descriptionScriptCode)
        {
            InitializeComponent();

            TxtInitScriptCode.Text = initScriptCode;
            TxtDescriptionScriptCode.Text = descriptionScriptCode;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }        
    }
}
