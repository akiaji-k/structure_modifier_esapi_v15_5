using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Reactive.Disposables;
using System.ComponentModel;
using structures_modifier_esapi_v15_5.Models;
using System.Windows;

using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;
using System.Windows.Data;

namespace structures_modifier_esapi_v15_5.ViewModels
{
    internal class ViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private CompositeDisposable _disposables { get; } = new CompositeDisposable();

        public ReactivePropertySlim<string> Id { get; } = new ReactivePropertySlim<string>("some ID");
        public ReactivePropertySlim<string> Name { get; } = new ReactivePropertySlim<string>("some Name");
        public ReactivePropertySlim<string> Date { get; } = new ReactivePropertySlim<string>("some Date");
        public ReactivePropertySlim<string> StructureSetName { get; } = new ReactivePropertySlim<string>("some SS");
        public bool IsClearButtonChecked { get; set; } = true;
        public bool IsResolutionButtonChecked { get; set; } = false;
        public Model InstModel { get; } = new Model();
        //        public ReactiveProperty<string> SelectedStructure { get; } = new ReactiveProperty<string>();
        public CollectionViewSource StructuresViewSource { get; } = new CollectionViewSource();


        public ReactiveCommand ClickCommand { get; set; }
        private ReactivePropertySlim<bool> can_execute = new ReactivePropertySlim<bool>(true);

        public ViewModel() {

            ClickCommand = can_execute
                            .ToReactiveCommand()
                            .WithSubscribe(() => ClickEvents())
                            .AddTo(_disposables);
        }


        public void Dispose() => _disposables.Dispose();

        public void SetScriptContextToModel(in ScriptContext context)
        {
            Id.Value = context.Patient.Id;
            Name.Value = context.Patient.FirstName + ", " + context.Patient.LastName;
            Date.Value = context.Image.Series.Study.CreationDateTime.ToString();
            StructureSetName.Value = context.StructureSet.Id;

            StructuresViewSource.Source = InstModel.selectable_st;
            StructuresViewSource.View.SortDescriptions.Add(new SortDescription("st.Id", ListSortDirection.Ascending));

            InstModel.SetScriptContext(context);

        }

        public void ClickEvents()
        {
            string buf = "";

            if (IsClearButtonChecked)
            {
//                buf = "Start clearing structures from all planes.\n\n";
                buf = "ストラクチャーを全スライスから消去します。\n\n";
//                buf += "Clear is checked.\n";
                foreach (var st in InstModel.selectable_st)
                {
                    if (st.is_selected)
                    {
    //                    buf += st.st.Id + "is selected.\n";
                        buf += InstModel.ClearStructureFromAllPlanes(st.st);
                    }
                    else {
    //                    buf += st.st.Id + "is not selected.\n";
                    }

                }
                buf += "\n消去が終了しました。\n";
            }
            else if (IsResolutionButtonChecked)
            {
//                buf = "Converting structures from High resolution to Default resolution...\n\n";
                buf = "ストラクチャーをHigh resolutionからDefault resolutionに変換します。\n\n";
                foreach (var st in InstModel.selectable_st)
                {
                    if (st.is_selected)
                    {
                        buf += InstModel.CreateDefaultResContour(st.st);
                    }
                    else {
                    }

                }
//                buf += "\nConverted.\nBe aware that the shape of structure might be different from that in High Resolution.\n";
                buf += "\n変換が終了しました。\nHigh Resolution時とは形状が異なる場合があります。ご注意ください。\n";
            }
            else
            {
                buf += "Undifined situation for radio buttons.\n";
            }


            MessageBox.Show(buf);
        }


    }
}
