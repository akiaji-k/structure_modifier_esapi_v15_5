using Reactive.Bindings;
using Reactive.Bindings.Disposables;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace structures_modifier_esapi_v15_5.Models
{
    internal class Model
    {
        public class SelectableStructure : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;
            private CompositeDisposable _disposables { get; } = new CompositeDisposable();


            public Structure st { get; set; }
            public bool is_selected { get; set; } = false;

            public SelectableStructure(in Structure st)
            {
                this.st = st;
                this.is_selected = false;
            }
        }

        public ReactiveCollection<SelectableStructure> selectable_st { get; } = new ReactiveCollection<SelectableStructure>();
        private ScriptContext context { get; set; }

        public void SetScriptContext(ScriptContext _context)
        {
            context = _context;

            foreach (var st in context.StructureSet.Structures)
            {
                selectable_st.Add(new SelectableStructure(st));
            }

        }

        private Int32 GetSlice(double z, in StructureSet ss)
        {
            return Convert.ToInt32((z - ss.Image.Origin.z) / ss.Image.ZRes);
        }

        private IEnumerable<int> GetMeshBounds(in Structure st, in StructureSet ss)
        {
            var mesh = st.MeshGeometry.Bounds;
            Int32 mesh_low = GetSlice(mesh.Z, ss);
            Int32 mesh_up = GetSlice(mesh.Z + mesh.SizeZ, ss) + 1;

            return Enumerable.Range(mesh_low, mesh_up);
        }

        public string ClearStructureFromAllPlanes(in Structure st)
        {
            string res = "";
            bool is_error = false;
            string error_buf = "";

            if (st.IsEmpty)
            {
//                res += "Structure '" + st.Id + " is empty.\n";
                res += String.Format("Structure '{0}' は空です。\n", st.Id);
            }
            else
            {
                context.Patient.BeginModifications();

//                res += "Clearing structure '" + st.Id + " from all planes...\n";
                res += String.Format("Structure '{0}' を全スライスから消去しています・・・。\n", st.Id);
//                foreach (var plane in GetMeshBounds(st, context.ExternalPlanSetup.StructureSet))
                foreach (var plane in GetMeshBounds(st, context.StructureSet))
                {
                    try
                    {
                        st.ClearAllContoursOnImagePlane(plane);
                    }
                    catch(Exception e)
                    {
                        is_error = true;
                        error_buf = e.Message;
                    }
                }

                if (is_error)
                {
                    //res += "An error occured while clearing structure '" + st.Id + " from all planes.\n";
                    res += String.Format("Structure '{0}' の消去中にエラーが発生しました。\nエラー内容: {1}\n", st.Id, error_buf);
                }
                else { }
            }


            return res;
        }

        //Ref: https://www.reddit.com/r/esapi/comments/104sd02/highres_to_defres_for_rinds/
        public string CreateDefaultResContour(in Structure high_st)
        {
            string res = "";
            bool is_error = false;
            string error_buf = "";

            if(!high_st.IsHighResolution)
            {
                res += String.Format("Structure '{0}' は既に Default resolution です。\n", high_st.Id);
            }
            else
            {
                string old_name = high_st.Id;
                string new_name = "";
                const string POSTFIX = "_HR";
//                    const Int32 MAX_LENGTH = 16;

                /* Create name */
                if (old_name.Length < 14)
                {
                    new_name = old_name + POSTFIX;
                }
                else 
                {
                    new_name = old_name.Substring(0, 13) + POSTFIX;
                }

                /* Convert to default resolution */
                try
                {
                    context.Patient.BeginModifications();

                    //update the name of the current structure
                    high_st.Id = new_name;

                    var ss = context.StructureSet;
                    if (ss.CanAddStructure(high_st.DicomType, old_name))
                    {
                        //add a new structure (default resolution by default)
                        Structure low_res = ss.AddStructure(high_st.DicomType, old_name);
                        //get the high res structure mesh geometry
                        MeshGeometry3D mesh = high_st.MeshGeometry;
                        //get the start and stop image planes for this structure
                        int startSlice = (int)((mesh.Bounds.Z - ss.Image.Origin.z) / ss.Image.ZRes);
                        int stopSlice = (int)(((mesh.Bounds.Z + mesh.Bounds.SizeZ) - ss.Image.Origin.z) / ss.Image.ZRes) + 1;

                        //foreach slice that contains contours, get the contours, and determine if you need to add or subtract the contours on the given image plane for the new low resolution structure. You need to subtract contours if the points lie INSIDE the current structure contour.
                        //We can sample three points (first, middle, and last points in array) to see if they are inside the current contour. If any of them are, subtract the set of contours from the image plane. Otherwise, add the contours to the image plane. NOTE: THIS LOGIC ASSUMES
                        //THAT YOU DO NOT OBTAIN THE CUTOUT CONTOUR POINTS BEFORE THE OUTER CONTOUR POINTS (it seems that ESAPI generally passes the main structure contours first before the cutout contours, but more testing is needed)
                        //string data = "";
                        for (int slice = startSlice; slice < stopSlice; slice++)
                        {
                            VVector[][] points = high_st.GetContoursOnImagePlane(slice);
                            for (int i = 0; i < points.GetLength(0); i++)
                            {
                                if (low_res.IsPointInsideSegment(points[i][0])
                                    || low_res.IsPointInsideSegment(points[i][points[i].GetLength(0) - 1])
                                    || low_res.IsPointInsideSegment(points[i][(int)(points[i].GetLength(0) / 2)]))
                                {
                                    low_res.SubtractContourOnImagePlane(points[i], slice);
                                }
                                else
                                {
                                    low_res.AddContourOnImagePlane(points[i], slice);
                                }
                            }
                        }
                        res += String.Format("Structure '{0}' をDefault resolutionに変換しました。\n", high_st.Id);
                    }
                    else
                    {
                        res += "Structure '" + new_name + " は追加できません。\n";
                    }
                }
                catch(Exception e)
                {
                    is_error = true;
                    error_buf = e.Message;
                }
            }


            if (is_error)
            {
                //res += "An error occured while clearing structure '" + st.Id + " from all planes.\n";
                res += String.Format("Structure '{0}' をDefault resolutionに変換中にエラーが発生しました。\nエラー内容: {1}\n", high_st.Id, error_buf);
            }
            else { }

            return res;
        }
    }
}
