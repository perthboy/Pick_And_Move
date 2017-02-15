#region Imported Namespaces

//.NET common used namespaces
using System;
using System.Windows.Forms;
using System.Collections.Generic;

//Revit.NET common used namespaces
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;


#endregion

namespace Pick_And_Move
    {
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    [Autodesk.Revit.Attributes.Regeneration(Autodesk.Revit.Attributes.RegenerationOption.Manual)]
    //[Regeneration(RegenerationOption.Manual)]
    public class Commands : IExternalCommand
        {
        /// <summary>
        /// The one and only method required by the IExternalCommand interface,
        /// the main entry point for every external command.
        /// </summary>
        /// <param name="commandData">Input argument providing access to the Revit application and its documents and their properties.</param>
        /// <param name="message">Return argument to display a message to the user in case of error if Result is not Succeeded.</param>
        /// <param name="elements">Return argument to highlight elements on the graphics screen if Result is not Succeeded.</param>
        /// <returns>Cancelled, Failed or Succeeded Result code.</returns>
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
            {
            //TODO: Add your code here
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Autodesk.Revit.ApplicationServices.Application app = uiapp.Application; ;
            Document doc = uidoc.Document;

            Transaction documentTransaction = new Transaction(commandData.Application.ActiveUIDocument.Document, "Document");
            try
                {
                documentTransaction.Start();
                //add shared parameter to file

                LoopAllCategories(doc);


                #region Selection
                //access selection

                Selection sel = uidoc.Selection;

                //iterate


                foreach (ElementId eleId in sel.GetElementIds())
                    {
                    //get element by ID
                    Element ele = doc.GetElement(eleId);
                    if (ele.Category.Name == "Walls")
                        Filter_Wall(doc);
                    //show it 
                    TaskDialog.Show(ele.Category.Name, ele.Name);
                    }

                # endregion
                #region Filter


                //let's get all these members
                Filter_Steel(doc);
                Filter_Wall(doc);

                //Must return some code
                documentTransaction.Commit();
                //   documentTransaction.Dispose();
                TaskDialog.Show("ID Update", "Completed");
                return Result.Succeeded;
                }
            catch (Exception ex)
                {
                TaskDialog.Show("Error", ex.ToString());
                return Result.Failed;
                }
                #endregion
            }
        private static void Filter_Steel(Document doc)
            {
            FilteredElementCollector colSteel = new FilteredElementCollector(doc);
            colSteel.OfCategory(BuiltInCategory.OST_StructuralFraming);
            colSteel.OfClass(typeof(FamilyInstance));


            IterateThruCollection(colSteel);
            }
        private static void Filter_Wall(Document doc)
            {
            FilteredElementCollector coll = new FilteredElementCollector(doc);
            coll.OfCategory(BuiltInCategory.OST_Walls);
            // coll.OfClass(typeof(FamilyInstance));
            coll.OfClass(typeof(Wall));

            IterateThruCollection(coll);
            }

        private static void LoopAllCategories(Document doc)
            {
            int n = 1;
            try
                {
                Categories categories = doc.Settings.Categories;


                List<string> li = new List<string>();


                foreach (Category c in categories)
                    {
                    Category p = c.Parent;
                    int a;
                    // Debug.Print("  {0} ({1}), parent {2}",
                    li.Add(c.Name + " ," + c.Id.IntegerValue);
                    BuiltInCategory BIC = (BuiltInCategory)c.Id.IntegerValue;

                    switch (BIC.ToString())
                        {
                        case "OST_Walls":
                            FilteredElementCollector Coll = new FilteredElementCollector(doc);
                            Coll.OfCategory(BIC);
                            Coll.OfClass(typeof(FamilyInstance));
                            IterateThruCollection(Coll);
                            break;
                        }
                    }
                }
            catch
                {

                n++;
                }
            }

        private static void IterateThruCollection(FilteredElementCollector coll)
            {
            //iterate through collection
            ElementId ID;

            foreach (Element elem in coll)
                {
                ID = elem.Id;

                CurvedWallRadiusAngles(elem);
                System.Text.StringBuilder paramText = new System.Text.StringBuilder();
                foreach (Parameter param in elem.Parameters)
                    {

                    //get parameter name
                    paramText.AppendFormat("{0}: ", param.Definition.Name);
                    if (param.Definition.Name == "ID")
                        try
                            {
                            param.Set(ID.IntegerValue);
                            }
                        catch (Exception)
                            {

                            throw;
                            }

                    //get information
                    switch (param.StorageType)
                        {
                        case StorageType.String:
                            paramText.Append(param.AsString());
                            //param.Set();
                            break;
                        case StorageType.Integer:
                            paramText.Append(param.AsString());
                            break;
                        case StorageType.ElementId:
                            paramText.Append(param.AsString());
                            break;

                        }
                    paramText.AppendLine();

                    }
                // TaskDialog.Show(elem.Id.ToString(), paramText.ToString());

                }
            }

        private static void createParameter(Autodesk.Revit.ApplicationServices.Application app, Document doc)
            {
            // get the shared parameter file
            DefinitionFile file = app.OpenSharedParameterFile();

            // if our group is not there, create it

            DefinitionGroup group = file.Groups.get_Item("elementID");

            if (group == null) group = file.Groups.Create("elementID");

            // add our parameter to the group
            //2014
            // Definition def = group.Definitions.Create("ID", ParameterType.Integer, true);

            //2015
            ExternalDefinitonCreationOptions opt = new ExternalDefinitonCreationOptions("ID", ParameterType.Integer);

            Definition def = group.Definitions.Create(opt); // 2015

            // now if we want it in the project, we need to bind it to categories
            CategorySet cats = app.Create.NewCategorySet();
            cats.Insert(doc.Settings.Categories.get_Item(BuiltInCategory.OST_StructuralFraming));

            // create a binding - instance or type:
            InstanceBinding bind = app.Create.NewInstanceBinding(cats);
            doc.ParameterBindings.Insert(def, bind, BuiltInParameterGroup.PG_DATA);

            }
        private void setFamilyParams(Document doc, ElementId ID)
            {
            FamilyManager manager = doc.FamilyManager;

            // lookup the family parameters
            //FamilyParameter manufacturer = lookupFamilyParam(manager, "Manufacturer");
            //FamilyParameter partNumber = lookupFamilyParam(manager, "Model");
            FamilyParameter id = lookupFamilyParam(manager, "ID");

            // set them
            //manager.Set(partNumber, "BR18054-1");
            //manager.SetFormula(manufacturer, "\"VIKING\"");
            manager.Set(id, ID);

            }

        private FamilyParameter lookupFamilyParam(FamilyManager fm, string name)
            {
            // lookup the family parameter
            foreach (FamilyParameter fp in fm.Parameters)
                {
                if (fp.Definition.Name.ToUpper() == name.ToUpper()) return fp;
                }

            throw new ApplicationException("Unable to find parameter: " + name);

            }
        private static void Filter_Wall()
            {
            //FilteredElementCollector coll = new FilteredElementCollector(doc);
            //coll.OfCategory(BuiltInCategory.OST_Walls);
            //coll.OfClass(typeof(FamilyInstance));
            //IterateThruCollection(coll);
            }

        public static void CurvedWallRadiusAngles(Element e)
            {
            Parameter p = e.get_Parameter(BuiltInParameter.CURVE_ELEM_ARC_RADIUS);
            double radius = p.AsDouble();

            p = e.get_Parameter(BuiltInParameter.CURVE_ELEM_ARC_START_ANGLE);
            double startAngle = p.AsDouble();

            p = e.get_Parameter(BuiltInParameter.CURVE_ELEM_ARC_RANGE);
            double angleRangle = p.AsDouble();

            p = e.get_Parameter(BuiltInParameter.CURVE_ELEM_ARC_END_ANGLE);
            double endAngle = p.AsDouble();

            MessageBox.Show(string.Format("Radius: {0}\nStart Angle: {1}\nAngle Range: {2}\nEnd Angle: {3}", radius, startAngle, angleRangle, endAngle), "Curved Wall Infomation");
            }
        public static bool CollectDataInput1(string title, out string ret)
            {
            System.Windows.Forms.Form dc = new System.Windows.Forms.Form();
            dc.Text = title;

            dc.HelpButton = dc.MinimizeBox = dc.MaximizeBox = false;
            dc.ShowIcon = dc.ShowInTaskbar = false;
            dc.TopMost = true;

            dc.Height = 100;
            dc.Width = 300;
            dc.MinimumSize = new System.Drawing.Size(dc.Width, dc.Height);

            int margin = 5;
            System.Drawing.Size size = dc.ClientSize;

            System.Windows.Forms.TextBox tb = new System.Windows.Forms.TextBox();
            tb.TextAlign = HorizontalAlignment.Right;
            tb.Height = 20;
            tb.Width = size.Width - 2 * margin;
            tb.Location = new System.Drawing.Point(margin, margin);
            tb.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            dc.Controls.Add(tb);

            System.Windows.Forms.Button ok = new System.Windows.Forms.Button();
            ok.Text = "Ok";
            ok.Click += new EventHandler(ok_Click);
            ok.Height = 23;
            ok.Width = 75;
            ok.Location = new System.Drawing.Point(size.Width / 2 - ok.Width / 2, size.Height / 2);
            ok.Anchor = AnchorStyles.Bottom;
            dc.Controls.Add(ok);
            dc.AcceptButton = ok;

            dc.ShowDialog();

            ret = tb.Text;

            return true;
            }

        private static void ok_Click(object sender, EventArgs e)
            {
            System.Windows.Forms.Form form = (sender as System.Windows.Forms.Control).Parent as System.Windows.Forms.Form;
            form.DialogResult = DialogResult.OK;
            form.Close();
            }
        }
    }
