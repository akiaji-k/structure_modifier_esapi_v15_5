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

using structures_modifier_esapi_v15_5.ViewModels;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;
// TODO: Uncomment the following line if the script requires write access.
[assembly: ESAPIScript(IsWriteable = true)]

namespace VMS.TPS 
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class Script : UserControl
    {
        public Script()
        {
            InitializeComponent();
        }

        public void Execute(ScriptContext context, System.Windows.Window window)
        {
            window.Height = 800;
            window.Width = 600;
            window.Content = this;
            window.SizeChanged += (sender, args) =>
            {
                this.Height = window.ActualHeight * 0.9;
                this.Width = window.ActualWidth * 0.95;
            };

            var view_model = this.DataContext as ViewModel;
            view_model.SetScriptContextToModel(context);

        }
    }
}
