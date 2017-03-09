using FirstFloor.ModernUI.Windows.Controls;
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

namespace _365Drive.Office365.UI.WebClientSupport
{
    /// <summary>
    /// Interaction logic for About.xaml
    /// </summary>
    public partial class WebClientSupport : ModernDialog
    {
        public WebClientSupport()
        {
            InitializeComponent();

            // define the dialog buttons
            this.Buttons = new Button[] { this.OkButton };

            this.OkButton.Click += OkButton_Click;

            //set the help tex
            webClientHelp.Text = string.Format(Globalization.Globalization.webClientHelp, Environment.NewLine);
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            //Hide because we are getting DialogResult error
            this.DialogResult = true;
            this.Hide();

            //Close afterwards
            this.Close();
        }
    }
}
